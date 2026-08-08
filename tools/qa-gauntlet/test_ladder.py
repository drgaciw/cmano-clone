#!/usr/bin/env python3
"""Unit tests for tools/qa-gauntlet/ladder.yaml (Wave 1 Agent C)."""

from __future__ import annotations

from pathlib import Path

import pytest
import yaml

LADDER_PATH = Path(__file__).resolve().parent / "ladder.yaml"

# Behavior-preserving contract vs pre-remediation run-gauntlet.sh case arms.
EXPECTED_TICKS = {
    "1": 6,
    "2": 10,
    "3": 16,
    "4": 24,
    "5": 40,
    "extra": 12,
}
EXPECTED_SCENARIOS = {
    "1": [
        "gauntlet-t1-patrol-a",
        "gauntlet-t1-patrol-b",
        "gauntlet-t1-patrol-c",
        "gauntlet-t1-patrol-d",
    ],
    "2": [
        "gauntlet-t2-escort-a",
        "gauntlet-t2-escort-passive",
        "gauntlet-t2-strike-a",
        "gauntlet-t2-strike-event",
        "gauntlet-t2-strike-salvo-boundary",
        "gauntlet-t2-ordnance-shotgun-winchester",
        "gauntlet-t2-shotgun-salvo-deny",
    ],
    "3": [
        "gauntlet-t3-escort-strike",
        "gauntlet-t3-emcon-phases",
        "gauntlet-t3-id-roe",
        "gauntlet-t3-event-chain",
        "gauntlet-t3-strike-salvo-boundary",
        "gauntlet-t3-logistics-contact-lifecycle",
        "gauntlet-t3-emcon-engage-block",
        "gauntlet-t3-logistics-joker-bingo",
    ],
    "4": [
        "gauntlet-t4-multi-mission",
        "gauntlet-t4-weighted",
        "gauntlet-t4-asymm-roe",
        "gauntlet-t4-random-inject",
    ],
    "5": [
        "gauntlet-t5-cascade",
        "gauntlet-t5-theater",
        "gauntlet-t5-dynamic-obj",
        "gauntlet-t5-roe-change",
    ],
    "extra": [
        "gauntlet-joint-orbat-smoke",
        "gauntlet-multidomain-shooters",
    ],
}


def test_ladder_manifest_exists():
    assert LADDER_PATH.is_file(), f"missing ladder manifest: {LADDER_PATH}"


def test_ladder_schema_and_tier_contract():
    data = yaml.safe_load(LADDER_PATH.read_text(encoding="utf-8"))
    assert data["version"] == 1
    assert data["defaultAnchorSeeds"] == [42, 7, 123] or data["defaultAnchorSeeds"] == [
        "42",
        "7",
        "123",
    ]
    tiers = data["tiers"]
    assert set(tiers) == set(EXPECTED_TICKS)
    for tier_id, ticks in EXPECTED_TICKS.items():
        entry = tiers[tier_id]
        assert entry["ticks"] == ticks, f"tier {tier_id} ticks"
        assert entry["scenarios"] == EXPECTED_SCENARIOS[tier_id], f"tier {tier_id} scenarios"
        assert len(entry["scenarios"]) == len(EXPECTED_SCENARIOS[tier_id])


def test_load_ladder_helper_scenarios_and_ticks():
    from evaluate_run import load_ladder, ladder_scenarios, ladder_ticks

    ladder = load_ladder(LADDER_PATH)
    assert ladder_ticks(ladder, "1") == 6
    assert ladder_ticks(ladder, "extra") == 12
    assert ladder_scenarios(ladder, "1") == EXPECTED_SCENARIOS["1"]
    with pytest.raises(KeyError):
        ladder_ticks(ladder, "99")
