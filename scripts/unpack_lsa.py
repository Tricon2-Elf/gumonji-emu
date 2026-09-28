#!/usr/bin/env python3
"""Unpack Gumonji .lsa archives into ./extracted/.

Accepts one archive or a directory of them. The format is the one read by
sub_4DE540 / sub_5324D0: AES-256 ECB with key 0xA8965F75, a padded directory,
then zlib chunks. See lsa_archives.md.
"""

from __future__ import annotations

import argparse
import struct
import sys
import zlib
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

ARCHIVE_KEY = 0xA8965F75
ENTRY_SIZE = 1076
BLOCK = 16

# Legacy VCE 1.x DH group embedded at gumonji.exe.c aF488fd584e49db.
DH_P_HEX = (
    "f488fd584e49dbcd20b49de49107366b336c380d451d0f7c88b31c7c5b2d8ef6"
    "f3c923c043f0a55b188d8ebb558cb85d38d334fd7c175743a31d186cde33212cb"
    "52aff3ce1b1294018118d7c84a70a72d686c40319c807297aca950cd9969fabd"
    "00a509b0246d3083d66a45d419f9c7cbd894b221926baaba25ec355e92f78c7"
)
DH_P = int(DH_P_HEX, 16)


def _gf_mul(a: int, b: int) -> int:
    r = 0
    for _ in range(8):
        if b & 1:
            r ^= a
        a = ((a << 1) ^ 0x11B) if a & 0x80 else (a << 1)
        b >>= 1
    return r & 0xFF


def _gf_pow(a: int, n: int) -> int:
    r = 1
    while n:
        if n & 1:
            r = _gf_mul(r, a)
        a = _gf_mul(a, a)
        n >>= 1
    return r


def _rotl8(x: int, n: int) -> int:
    return ((x << n) | (x >> (8 - n))) & 0xFF


def _make_sbox() -> tuple[list[int], list[int]]:
    s, inv = [], [0] * 256
    for x in range(256):
        y = 0 if x == 0 else _gf_pow(x, 254)
        z = y ^ _rotl8(y, 1) ^ _rotl8(y, 2) ^ _rotl8(y, 3) ^ _rotl8(y, 4) ^ 0x63
        s.append(z)
        inv[z] = x
    return s, inv


SBOX, INV_SBOX = _make_sbox()
RCON = [0, 1]
for _ in range(1, 10):
    RCON.append(_gf_mul(RCON[-1], 2))


class AES128:
    """Minimal AES-128 ECB implementation, matching Rijndael's 16-byte block."""

    def __init__(self, key: bytes):
        if len(key) != 16:
            raise ValueError("AES-128 requires a 16-byte key")
        words = [list(key[i:i + 4]) for i in range(0, 16, 4)]
        for i in range(4, 44):
            t = words[i - 1][:]
            if i % 4 == 0:
                t = t[1:] + t[:1]
                t = [SBOX[x] for x in t]
                t[0] ^= RCON[i // 4]
            words.append([words[i - 4][j] ^ t[j] for j in range(4)])
        self.round_keys = [sum(words[4*r:4*r+4], []) for r in range(11)]

    @staticmethod
    def _add_key(s: list[int], k: list[int]) -> None:
        for i in range(16):
            s[i] ^= k[i]

    @staticmethod
    def _shift_rows(s: list[int]) -> list[int]:
        # State is stored column-major: index = 4*column + row.
        return [s[0], s[5], s[10], s[15],
                s[4], s[9], s[14], s[3],
                s[8], s[13], s[2], s[7],
                s[12], s[1], s[6], s[11]]

    @staticmethod
    def _inv_shift_rows(s: list[int]) -> list[int]:
        return [s[0], s[13], s[10], s[7],
                s[4], s[1], s[14], s[11],
                s[8], s[5], s[2], s[15],
                s[12], s[9], s[6], s[3]]

    @staticmethod
    def _mix_columns(s: list[int], inverse: bool = False) -> list[int]:
        out = s[:]
        for c in range(4):
            i = 4 * c
            a = s[i:i+4]
            if not inverse:
                out[i:i+4] = [
                    _gf_mul(a[0], 2) ^ _gf_mul(a[1], 3) ^ a[2] ^ a[3],
                    a[0] ^ _gf_mul(a[1], 2) ^ _gf_mul(a[2], 3) ^ a[3],
                    a[0] ^ a[1] ^ _gf_mul(a[2], 2) ^ _gf_mul(a[3], 3),
                    _gf_mul(a[0], 3) ^ a[1] ^ a[2] ^ _gf_mul(a[3], 2)]
            else:
                out[i:i+4] = [
                    _gf_mul(a[0], 14) ^ _gf_mul(a[1], 11) ^ _gf_mul(a[2], 13) ^ _gf_mul(a[3], 9),
                    _gf_mul(a[0], 9) ^ _gf_mul(a[1], 14) ^ _gf_mul(a[2], 11) ^ _gf_mul(a[3], 13),
                    _gf_mul(a[0], 13) ^ _gf_mul(a[1], 9) ^ _gf_mul(a[2], 14) ^ _gf_mul(a[3], 11),
                    _gf_mul(a[0], 11) ^ _gf_mul(a[1], 13) ^ _gf_mul(a[2], 9) ^ _gf_mul(a[3], 14)]
        return out

    def decrypt_block(self, block: bytes) -> bytes:
        s = list(block)
        self._add_key(s, self.round_keys[10])
        for r in range(9, 0, -1):
            s = self._inv_shift_rows(s)
            s = [INV_SBOX[x] for x in s]
            self._add_key(s, self.round_keys[r])
            s = self._mix_columns(s, inverse=True)
        s = self._inv_shift_rows(s)
        s = [INV_SBOX[x] for x in s]
        self._add_key(s, self.round_keys[0])
        return bytes(s)

    def encrypt_block(self, block: bytes) -> bytes:
        if len(block) != BLOCK:
            raise ValueError("AES block must be 16 bytes")
        s = list(block)
        self._add_key(s, self.round_keys[0])
        for r in range(1, 10):
            s = [SBOX[x] for x in s]
            s = self._shift_rows(s)
            s = self._mix_columns(s)
            self._add_key(s, self.round_keys[r])
        s = [SBOX[x] for x in s]
        s = self._shift_rows(s)
        self._add_key(s, self.round_keys[10])
        return bytes(s)

    def decrypt_ecb(self, data: bytes) -> bytes:
        if len(data) % BLOCK:
            raise ValueError("ciphertext is not block aligned")
        return b"".join(self.decrypt_block(data[i:i+BLOCK]) for i in range(0, len(data), BLOCK))

    def encrypt_ecb(self, data: bytes) -> bytes:
        if len(data) % BLOCK:
            raise ValueError("plaintext is not block aligned")
        return b"".join(self.encrypt_block(data[i:i+BLOCK]) for i in range(0, len(data), BLOCK))


def bn_hex(value: int) -> bytes:
    """sub_5CE100 emits big-endian whole-byte hex without leading zero bytes."""
    return value.to_bytes(max(1, (value.bit_length() + 7) // 8), "big").hex().upper().encode("ascii")


def derive_aes_key(shared: int) -> bytes:
    # sub_5A8510 -> sub_5AD0C0 takes the FIRST 16 bytes of BN_bn2hex(shared).
    raw = bytes.fromhex(bn_hex(shared).decode())[:16]
    if len(raw) != 16:
        raise ValueError("DH shared secret is too short")
    # sub_5C76B0 uses lowercase %02x; sub_5C7640 then maps each ASCII
    # character modulo 16 (!): 'a'..'f' become 1..6 rather than 10..15.
    digits = raw.hex().encode("ascii")
    return bytes(((digits[i] & 15) << 4) | (digits[i + 1] & 15)
                 for i in range(0, 32, 2))


class AES256:
    """AES-256 ECB, using the same block operations as the PoC's AES-128."""

    def __init__(self, key: bytes):
        if len(key) != 32:
            raise ValueError("AES-256 requires a 32-byte key")
        words = [list(key[i:i + 4]) for i in range(0, 32, 4)]
        for i in range(8, 60):
            temp = words[i - 1][:]
            if i % 8 == 0:
                temp = temp[1:] + temp[:1]
                temp = [SBOX[x] for x in temp]
                temp[0] ^= RCON[i // 8]
            elif i % 8 == 4:
                temp = [SBOX[x] for x in temp]
            words.append([words[i - 8][j] ^ temp[j] for j in range(4)])
        self.round_keys = [sum(words[4 * r:4 * r + 4], []) for r in range(15)]

    def decrypt(self, data: bytes) -> bytes:
        if len(data) % 16:
            raise ValueError("ciphertext is not block aligned")
        return b"".join(self._decrypt_block(data[i:i + 16]) for i in range(0, len(data), 16))

    def _decrypt_block(self, block: bytes) -> bytes:
        state = list(block)
        AES128._add_key(state, self.round_keys[14])
        for rnd in range(13, 0, -1):
            state = AES128._inv_shift_rows(state)
            state = [INV_SBOX[x] for x in state]
            AES128._add_key(state, self.round_keys[rnd])
            state = AES128._mix_columns(state, inverse=True)
        state = AES128._inv_shift_rows(state)
        state = [INV_SBOX[x] for x in state]
        AES128._add_key(state, self.round_keys[0])
        return bytes(state)


def archive_cipher() -> AES256:
    """Eight little-endian copies of the key, then the VCE hex-nibble map."""
    raw = struct.pack("<I", ARCHIVE_KEY) * 8
    digits = raw.hex().encode()
    key = bytes(((digits[i] & 15) << 4) | (digits[i + 1] & 15) for i in range(0, len(digits), 2))
    return AES256(key)


def padded_size(count: int) -> int:
    """sub_4DE180 reads 32 * (n >> 5) + 32 ciphertext bytes for n plaintext bytes."""
    return 32 * (count >> 5) + 32


def read_plain(data: bytes, cipher: AES256, offset: int, count: int) -> tuple[bytes, int]:
    size = padded_size(count)
    if offset < 0 or count < 0 or offset + size > len(data):
        raise ValueError(f"read of {count} bytes at {offset} exceeds archive ({len(data)} bytes)")
    return cipher.decrypt(data[offset:offset + size])[:count], size


def inflate_chunk(body: bytes) -> bytes:
    """Inflate one chunk. The client treats a truncated zlib trailer as success."""
    decoder = zlib.decompressobj()
    produced = bytearray()
    view = body
    while view:
        try:
            produced += decoder.decompress(view)
        except zlib.error:
            break
        view = decoder.unconsumed_tail
        if decoder.eof or not view:
            break
    try:
        produced += decoder.flush()
    except zlib.error:
        pass
    if not produced:
        raise ValueError("zlib chunk produced no bytes")
    return bytes(produced)


def entry_name(raw: bytes) -> Path:
    text = raw.split(b"\x00", 1)[0].decode("utf-8", "replace").replace("\\", "/").strip()
    parts = [part for part in text.split("/") if part not in ("", ".")]
    if not parts or any(part == ".." for part in parts):
        raise ValueError(f"unsafe archive entry name {text!r}")
    return Path(*parts)


def iter_entries(data: bytes, cipher: AES256):
    count_bytes, step = read_plain(data, cipher, 0, 4)
    count = struct.unpack(">I", count_bytes)[0]
    directory, directory_step = read_plain(data, cipher, step, ENTRY_SIZE * count)
    payload_start = step + directory_step
    for index in range(count):
        base = index * ENTRY_SIZE
        offset, size, chunks = struct.unpack_from(">III", directory, base)
        name = entry_name(directory[base + 52:base + 52 + 1024])
        yield name, payload_start + offset, size, chunks


def extract_entry(data: bytes, cipher: AES256, offset: int, chunks: int) -> bytes:
    output = bytearray()
    cursor = offset
    for _ in range(chunks):
        header, step = read_plain(data, cipher, cursor, 4)
        cursor += step
        length = struct.unpack(">I", header)[0]
        body, step = read_plain(data, cipher, cursor, length)
        cursor += step
        output += inflate_chunk(body)
    return bytes(output)


def extract_archive(path: Path, output_dir: Path, cipher: AES256) -> tuple[int, int]:
    data = path.read_bytes()
    written = 0
    empty = 0
    destination = output_dir / path.stem
    for name, offset, size, chunks in iter_entries(data, cipher):
        if chunks == 0:
            empty += 1
            continue
        payload = extract_entry(data, cipher, offset, chunks)
        if size and len(payload) != size:
            raise ValueError(f"{path.name}:{name} inflated to {len(payload)} bytes, directory says {size}")
        target = destination / name
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(payload)
        written += 1
        print(f"  {name} ({len(payload)} bytes)")
    return written, empty


def archive_paths(source: Path) -> list[Path]:
    if source.is_file():
        return [source]
    if source.is_dir():
        found = [path for path in source.rglob("*") if path.is_file() and path.suffix.lower() == ".lsa"]
        return sorted(found)
    raise ValueError(f"not a file or directory: {source}")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Unpack Gumonji .lsa archives into ./extracted/")
    parser.add_argument("source", type=Path, help="one .lsa file, or a directory containing them")
    parser.add_argument("-o", "--output", type=Path, default=Path("extracted"),
                        help="output directory (default: ./extracted)")
    args = parser.parse_args(argv)
    source = args.source
    archives = archive_paths(source)
    if not archives:
        print(f"no .lsa files in {source}", file=sys.stderr)
        return 1
    cipher = archive_cipher()
    args.output.mkdir(parents=True, exist_ok=True)
    failures = 0
    for archive in archives:
        print(f"{archive}")
        try:
            written, empty = extract_archive(archive, args.output, cipher)
        except (OSError, ValueError, struct.error, zlib.error) as exc:
            failures += 1
            print(f"  failed: {exc}", file=sys.stderr)
            continue
        extra = f", {empty} empty" if empty else ""
        print(f"  {written} files{extra} -> {args.output / archive.stem}")
    return 1 if failures else 0


if __name__ == "__main__":
    raise SystemExit(main())
