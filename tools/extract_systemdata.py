#!/usr/bin/env python3
"""Extract only the security/organization sections used by ZArchitecture.

Usage:
    python tools/extract_systemdata.py systemdata.hk2026.json data/systemdata.seed.json

The source is ~47 MB; Python's standard json module is sufficient for a one-off
build-time extraction and keeps the project dependency-free.
"""
from __future__ import annotations

import json
import sys
from pathlib import Path

SECTIONS = (
    "users",
    "groups",
    "grouptypes",
    "roles",
    "roletypes",
    "memberships",
    "groupmemberships",
)


def main() -> int:
    if len(sys.argv) != 3:
        print("usage: extract_systemdata.py INPUT.json OUTPUT.json", file=sys.stderr)
        return 2

    source = Path(sys.argv[1])
    target = Path(sys.argv[2])
    with source.open("r", encoding="utf-8") as f:
        payload = json.load(f)

    compact = {name: payload[name] for name in SECTIONS if name in payload}
    target.parent.mkdir(parents=True, exist_ok=True)
    with target.open("w", encoding="utf-8") as f:
        json.dump(compact, f, ensure_ascii=False, separators=(",", ":"))

    for name, rows in compact.items():
        print(f"{name}: {len(rows) if isinstance(rows, list) else '?'}")
    print(f"written: {target}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
