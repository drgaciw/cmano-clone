#!/usr/bin/env python3
"""DRG-61: t3-s1/s2/s3 carry gauntlet.tier == 3 (Lane A tier tags)."""

from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SCENARIOS = ROOT / "data" / "scenarios"

T3_POLICIES = (
    "gauntlet-20260727-1455-t3-s1.policy.json",
    "gauntlet-20260727-1455-t3-s2.policy.json",
    "gauntlet-20260727-1455-t3-s3.policy.json",
)


def test_t3_s1_s2_s3_have_gauntlet_tier_3() -> None:
    for name in T3_POLICIES:
        path = SCENARIOS / name
        assert path.is_file(), f"missing policy: {path}"
        policy = json.loads(path.read_text(encoding="utf-8"))
        gauntlet = policy.get("gauntlet")
        assert isinstance(gauntlet, dict), f"{name}: gauntlet object required"
        assert gauntlet.get("tier") == 3, f"{name}: expected gauntlet.tier==3, got {gauntlet.get('tier')!r}"
