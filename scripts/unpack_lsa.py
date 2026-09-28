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
from gumonji_vce_poc import AES128, INV_SBOX, RCON, SBOX

ARCHIVE_KEY = 0xA8965F75
ENTRY_SIZE = 1076


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
