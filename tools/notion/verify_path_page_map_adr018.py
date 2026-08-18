#!/usr/bin/env python3
"""Structural verification of ADR-018 Notion path-page-map entry (no API secrets)."""
from __future__ import annotations

import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
MAP = ROOT / "docs/engineering/notion/path-page-map.json"
EXPECTED_PATH = "docs/architecture/adr-018-target-os-and-cpu-architectures.md"
EXPECTED_UID = "ADR-018"


def main() -> int:
    data = json.loads(MAP.read_text(encoding="utf-8"))
    hits = [
        e
        for e in data.get("entries", [])
        if e.get("path") == EXPECTED_PATH and e.get("uid") == EXPECTED_UID
    ]
    if len(hits) != 1:
        print(f"FAIL: expected 1 map entry for {EXPECTED_UID} @ {EXPECTED_PATH}, got {len(hits)}")
        return 1
    e = hits[0]
    page_id = e.get("page_id") or ""
    page_url = e.get("page_url") or ""
    if not page_id or "-" not in page_id:
        print(f"FAIL: invalid page_id: {page_id!r}")
        return 1
    compact = page_id.replace("-", "")
    if compact not in page_url:
        print(f"FAIL: page_url {page_url!r} does not contain compact page_id {compact}")
        return 1
    if "ADR-018" not in (e.get("title") or "") and "ADR-018" not in (e.get("page_title") or ""):
        print("FAIL: title does not contain ADR-018")
        return 1
    if e.get("category") != "adrs":
        print(f"FAIL: category {e.get('category')!r} != adrs")
        return 1
    print("PASS:")
    print(f"  path={e['path']}")
    print(f"  uid={e['uid']}")
    print(f"  page_id={page_id}")
    print(f"  page_url={page_url}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
