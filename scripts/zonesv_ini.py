"""Decrypt and encrypt the original zone server's encrypted INI files.

Requires: python -m pip install pycryptodome
The original executable uses Blowfish ECB with a 36-byte key (35 ASCII bytes
plus the NUL terminator), and writes fixed-size 256- or 2048-byte files.
"""

from __future__ import annotations

import argparse
from pathlib import Path

from Crypto.Cipher import Blowfish


KEY = b"GUMONJI" * 5 + b"\0"
ZONE_SIZE = 2048


def crypt(data: bytes, decrypt: bool) -> bytes:
    if len(data) % Blowfish.block_size:
        raise ValueError("zonesv INI length must be a multiple of 8 bytes")
    cipher = Blowfish.new(KEY, Blowfish.MODE_ECB)
    return cipher.decrypt(data) if decrypt else cipher.encrypt(data)


def plaintext(data: bytes) -> bytes:
    return crypt(data, decrypt=True).rstrip(b"\0")


def encrypted(data: bytes, size: int) -> bytes:
    if size <= 0 or size % Blowfish.block_size:
        raise ValueError("output size must be a positive multiple of 8")
    if len(data) > size:
        raise ValueError(f"INI text is {len(data)} bytes; exceeds {size}-byte file")
    return crypt(data.ljust(size, b"\0"), decrypt=False)


def write_new(path: Path, data: bytes, force: bool) -> None:
    with path.open("wb" if force else "xb") as output:
        output.write(data)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest="command", required=True)
    for name in ("decrypt", "encrypt"):
        command = sub.add_parser(name)
        command.add_argument("input", type=Path)
        command.add_argument("output", type=Path)
        command.add_argument("--force", action="store_true", help="allow overwriting the output file")
        if name == "encrypt":
            command.add_argument("--size", type=int, default=ZONE_SIZE)
    args = parser.parse_args()
    if args.input.resolve() == args.output.resolve():
        parser.error("output must be a different path from input")
    source = args.input.read_bytes()
    if args.command == "decrypt":
        result = plaintext(source)
    elif args.command == "encrypt":
        result = encrypted(source, args.size)
    write_new(args.output, result, args.force)
    print(f"Wrote {args.output} ({len(result)} bytes)")


if __name__ == "__main__":
    main()
