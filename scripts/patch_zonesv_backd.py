"""Retarget the original zone server's built-in backd address in a COPY.

Usage:
    python scripts/patch_zonesv_backd.py zonesv_win.exe zonesv_local.exe \
        --host 127.0.0.1 --port 12422 --domain orange

This changes only the unique built-in `host,port,domain` string. The source
executable is never overwritten. It does not alter the separate registration
dialog's hardcoded address.
"""

from __future__ import annotations

import argparse
from pathlib import Path


ORIGINAL = b"210.255.51.229,12421,orange\0"


def patch(data: bytes, host: str, port: int, domain: str) -> bytes:
    if not 1 <= port <= 65535:
        raise ValueError("port must be between 1 and 65535")
    if not host or not domain or any(c in host + domain for c in ",\r\n\0 "):
        raise ValueError("host and domain must be nonempty and contain no commas or whitespace")
    replacement = f"{host},{port},{domain}".encode("ascii")
    if len(replacement) >= len(ORIGINAL):
        raise ValueError(f"address must be at most {len(ORIGINAL) - 1} ASCII bytes")
    if data.count(ORIGINAL) != 1:
        raise ValueError("expected exactly one original backd address in the executable")
    return data.replace(ORIGINAL, replacement.ljust(len(ORIGINAL), b"\0"), 1)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=12422)
    parser.add_argument("--domain", default="orange")
    args = parser.parse_args()
    if args.source.resolve() == args.output.resolve():
        parser.error("output must be a different path from source")
    result = patch(args.source.read_bytes(), args.host, args.port, args.domain)
    with args.output.open("xb") as output:
        output.write(result)
    print(f"Wrote {args.output}; original executable preserved")


if __name__ == "__main__":
    main()
