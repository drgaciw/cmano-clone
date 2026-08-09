#!/usr/bin/env python3
"""Unit tests for tools/qa-gauntlet/forge_scorecard.py (FORGE-03 Logic)."""

from __future__ import annotations

import json
import sys
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "qa-gauntlet"))

from forge_scorecard import (  # noqa: E402
    assert_counts_consistent,
    infer_cell,
    read_oracle_passed,
    rebuild_counts,
    score_candidate,
)

FIXTURES = Path(__file__).parent / "fixtures"


def _minimal_policy(
    *,
    scenario_id: str = "gauntlet-forge-test",
    intent: str = "Forge test patrol under weapons free",
    expect: dict | None = None,
    units: list | None = None,
) -> dict:
    return {
        "friendlyRoe": "WeaponsFree",
        "opposingRoe": "WeaponsTight",
        "gauntlet": {
            "intent": intent,
            "tier": 1,
            "expect": expect if expect is not None else {"side": "BLUE", "minKills": 0},
            "units": units
            or [
                {"platformId": "k-31-visby-2009", "domain": "surface"},
            ],
            "catalogRefs": ["k-31-visby-2009"],
        },
    }


def _write_policy(tmp_path: Path, name: str, policy: dict) -> Path:
    path = tmp_path / f"{name}.policy.json"
    path.write_text(json.dumps(policy), encoding="utf-8")
    return path


def _empty_coverage() -> dict:
    return {"cellCount": 0, "cells": [], "counts": {"platformId": {}}}


def test_read_oracle_passed_list_scenarios_no_wildcard(tmp_path: Path) -> None:
    tier_dir = tmp_path / "tier-1"
    tier_dir.mkdir()
    (tier_dir / "oracle-eval.json").write_text(
        json.dumps(
            {
                "ok": True,
                "allPassed": True,
                "scenarios": [
                    {"scenario": "gauntlet-t1-patrol-a", "passed": True, "rows": 3},
                    {"scenario": "gauntlet-t1-patrol-b", "passed": True, "rows": 3},
                ],
            }
        ),
        encoding="utf-8",
    )
    oracle_map = read_oracle_passed(tier_dir)
    assert oracle_map.get("gauntlet-t1-patrol-a") is True
    assert oracle_map.get("gauntlet-t1-patrol-b") is True
    assert "*" not in oracle_map
    assert oracle_map.get("gauntlet-t1-patrol-b-candidate-underused") is None


def test_oracle_none_blocks_promotion(tmp_path: Path) -> None:
    policy = _minimal_policy(intent="Unique forge mission never seen AAA")
    path = _write_policy(tmp_path, "gauntlet-forge-unique-aaa", policy)
    result = score_candidate(
        path,
        _empty_coverage(),
        index_hashes=set(),
        oracle_map={},  # no oracle → None
        tier=1,
        useful_fail_ids=set(),
    )
    assert result["oraclePassed"] is None
    assert result["hardGates"]["oracleKnown"] is False
    assert result["hardGatesPass"] is False
    assert result["recommendPromote"] is False


def test_oracle_pass_new_cell_promotes(tmp_path: Path) -> None:
    policy = _minimal_policy(intent="Brand new forge inject cascade theater")
    sid = "gauntlet-forge-brand-new"
    path = _write_policy(tmp_path, sid, policy)
    result = score_candidate(
        path,
        _empty_coverage(),
        index_hashes=set(),
        oracle_map={sid: True},
        tier=1,
        useful_fail_ids=set(),
    )
    assert result["hardGatesPass"] is True
    assert result["noveltyScore"] > 0
    assert result["recommendPromote"] is True


def test_oracle_fail_no_useful_no_promote(tmp_path: Path) -> None:
    policy = _minimal_policy(intent="Failing oracle no useful fail BBB")
    sid = "gauntlet-forge-fail-bbb"
    path = _write_policy(tmp_path, sid, policy)
    result = score_candidate(
        path,
        _empty_coverage(),
        index_hashes=set(),
        oracle_map={sid: False},
        tier=1,
        useful_fail_ids=set(),
    )
    assert result["hardGatesPass"] is False
    assert result["recommendPromote"] is False


def test_useful_fail_promotes_with_oracle_fail(tmp_path: Path) -> None:
    policy = _minimal_policy(intent="Useful fail cascade inject novel CCC")
    sid = "gauntlet-forge-useful-ccc"
    path = _write_policy(tmp_path, sid, policy)
    result = score_candidate(
        path,
        _empty_coverage(),
        index_hashes=set(),
        oracle_map={sid: False},
        tier=1,
        useful_fail_ids={sid},
    )
    assert result["usefulFail"] is True
    assert result["hardGatesPass"] is True
    assert result["recommendPromote"] is True


def test_duplicate_intent_no_promote(tmp_path: Path) -> None:
    policy = _minimal_policy(intent="Dup intent patrol")
    sid = "gauntlet-forge-dup"
    path = _write_policy(tmp_path, sid, policy)
    cell = infer_cell(policy, sid)
    result = score_candidate(
        path,
        _empty_coverage(),
        index_hashes={cell["intentHash"]},
        oracle_map={sid: True},
        tier=1,
        useful_fail_ids=set(),
    )
    assert result["duplicateIntent"] is True
    assert result["recommendPromote"] is False


def test_oracle_lookup_uses_policy_id_when_filename_differs(tmp_path: Path) -> None:
    """Regression: BUG-forge-scorecard-filename-vs-policy-id.

    The qa-gauntlet-forge skill writes candidates as candidate-1.policy.json
    etc. with an internal `id` like gauntlet-forge-<RUN_ID>-t2-c1. oracle-eval.json
    is keyed by that `id` (what gauntlet_oracle_eval emits), not by the filename
    stem. score_candidate must resolve the oracle result via policy id, falling
    back to the filename stem only when no id is present.
    """
    policy_id = "gauntlet-forge-run123-t2-c1"
    policy = _minimal_policy(intent="Filename vs policy id mismatch regression")
    policy["id"] = policy_id
    path = _write_policy(tmp_path, "candidate-1", policy)  # filename stem != policy_id
    result = score_candidate(
        path,
        _empty_coverage(),
        index_hashes=set(),
        oracle_map={policy_id: True},  # keyed by policy id, as gauntlet_oracle_eval emits
        tier=1,
        useful_fail_ids=set(),
    )
    assert result["scenarioId"] == policy_id
    assert result["oraclePassed"] is True
    assert result["oracleLookupMissed"] is False
    assert result["hardGatesPass"] is True
    assert result["recommendPromote"] is True


def test_oracle_lookup_missed_flag_when_never_evaluated(tmp_path: Path) -> None:
    """Missing oracle entry must be visibly flagged, distinct from a failed
    oracle result, while still blocking promotion (locked-eval contract)."""
    policy = _minimal_policy(intent="Never evaluated oracle lookup missed flag")
    sid = "gauntlet-forge-never-evaluated"
    path = _write_policy(tmp_path, sid, policy)
    result = score_candidate(
        path,
        _empty_coverage(),
        index_hashes=set(),
        oracle_map={},
        tier=1,
        useful_fail_ids=set(),
    )
    assert result["oraclePassed"] is None
    assert result["oracleLookupMissed"] is True
    assert result["hardGatesPass"] is False
    assert result["recommendPromote"] is False


def test_duplicate_scenario_id_no_promote(tmp_path: Path) -> None:
    policy = _minimal_policy(intent="Already promoted by id DDD")
    sid = "gauntlet-t1-patrol-a"
    path = _write_policy(tmp_path, sid, policy)
    result = score_candidate(
        path,
        _empty_coverage(),
        index_hashes=set(),
        oracle_map={sid: True},
        tier=1,
        useful_fail_ids=set(),
        index_scenario_ids={sid},
    )
    assert result["duplicateIntent"] is True
    assert result["recommendPromote"] is False


def test_infer_cell_reproducibility() -> None:
    policy = _minimal_policy(intent="Single patrol Blue survives")
    a = infer_cell(policy, "gauntlet-t1-patrol-a")
    b = infer_cell(policy, "gauntlet-t1-patrol-a")
    assert a == b
    assert a["key"]
    assert a["intentHash"]


def test_infer_cell_event_chain_not_unknown() -> None:
    policy = _minimal_policy(intent="Timed event chain escort strike")
    cell = infer_cell(policy, "gauntlet-t3-event-chain")
    assert cell["missionClass"] == "event-chain"


def test_coverage_map_bootstrap_consistency() -> None:
    coverage_path = ROOT / "production/qa/gauntlet/corpus/coverage-map.json"
    assert coverage_path.is_file()
    coverage = json.loads(coverage_path.read_text(encoding="utf-8"))
    by_id = {c["scenarioId"]: c for c in coverage["cells"]}
    mismatches = []
    for path in sorted((ROOT / "data/scenarios").glob("gauntlet-*.policy.json")):
        sid = path.name.replace(".policy.json", "")
        policy = json.loads(path.read_text(encoding="utf-8"))
        cell = infer_cell(policy, sid)
        stored = by_id.get(sid)
        assert stored is not None, f"missing coverage cell for {sid}"
        if stored["key"] != cell["key"]:
            mismatches.append((sid, stored["key"], cell["key"]))
        if stored.get("intentHash") and stored["intentHash"] != cell["intentHash"]:
            mismatches.append((sid, "intentHash", stored["intentHash"], cell["intentHash"]))
    assert mismatches == [], f"coverage/infer_cell drift: {mismatches}"


def test_novelty_score_floor_existing_cell(tmp_path: Path) -> None:
    policy = _minimal_policy(intent="Existing cell patrol")
    sid = "gauntlet-forge-existing"
    path = _write_policy(tmp_path, sid, policy)
    cell = infer_cell(policy, sid)
    coverage = {
        "cellCount": 1,
        "cells": [{"key": cell["key"]}],
        "counts": {"platformId": {"k-31-visby-2009": 10}},
    }
    result = score_candidate(
        path,
        coverage,
        index_hashes=set(),
        oracle_map={sid: True},
        tier=1,
        useful_fail_ids=set(),
    )
    assert result["newCoverageCell"] is False
    assert result["noveltyScore"] <= 0 or result["recommendPromote"] is False
    # With no new cell / rare / useful fail, novelty should be 0 → no promote
    assert result["noveltyScore"] == 0.0
    assert result["recommendPromote"] is False


def test_infer_cell_reports_stress_axes_from_policy():
    policy = {
        "id": "gauntlet-t3-escort-strike-weapons-extreme",
        "engage": {"defaultMagazineRounds": 1, "salvoSize": 4},
        "gauntlet": {"tier": 3, "intent": "escort + strike under weapons-extremes"},
    }

    cell = infer_cell(policy, policy["id"])

    assert cell["stressAxes"] == "ew:off|logistics:off|weapons:extreme"


def test_infer_cell_detects_ew_axis_from_jammers_block():
    policy = {
        "id": "gauntlet-t3-s5",
        "jammers": [{"targetId": "x", "jamStrength": 0.9, "activeFromTick": 0}],
        "gauntlet": {"tier": 3, "intent": "escort"},
    }

    cell = infer_cell(policy, policy["id"])

    assert cell["stressAxes"].startswith("ew:extreme")


def test_infer_cell_stress_axes_default_to_off():
    policy = {"id": "gauntlet-t1-patrol-a", "gauntlet": {"tier": 1, "intent": "patrol"}}

    cell = infer_cell(policy, policy["id"])

    assert cell["stressAxes"] == "ew:off|logistics:off|weapons:off"


def test_infer_cell_key_is_unchanged_by_the_axis_extension():
    policy = {"id": "gauntlet-t1-patrol-a", "gauntlet": {"tier": 1, "intent": "patrol"}}

    cell = infer_cell(policy, policy["id"])

    # The historical 5-part key must stay intact so existing cells remain comparable.
    assert cell["key"].count("|") == 4
    assert "stressAxes" not in cell["key"]


def test_coverage_map_counts_consistency() -> None:
    """DRG-60: counts block must match rebuild from registered cells + policies.

    Single-valued dims (missionClass/eventClass/roePair/emconClass) sum to
    cellCount. Multi-valued dims (domain/platformId) never exceed scenarioCount
    per key. platformId drives rare_hits → noveltyScore in score_candidate.
    """
    coverage_path = ROOT / "production/qa/gauntlet/corpus/coverage-map.json"
    coverage = json.loads(coverage_path.read_text(encoding="utf-8"))
    scenarios_dir = ROOT / "data/scenarios"
    assert_counts_consistent(coverage, scenarios_dir=scenarios_dir)


def test_rebuild_counts_single_valued_dims_sum_to_cell_count() -> None:
    cells = [
        {
            "scenarioId": "a",
            "missionClass": "patrol",
            "eventClass": "none",
            "roePair": "WeaponsFree/WeaponsTight",
            "emconClass": "unrestricted",
            "domains": ["surface"],
        },
        {
            "scenarioId": "b",
            "missionClass": "strike",
            "eventClass": "inject",
            "roePair": "WeaponsFree/WeaponsFree",
            "emconClass": "emcon-phases",
            "domains": ["air", "surface"],
        },
    ]
    policies = {
        "a": {"gauntlet": {"catalogRefs": ["plat-a"], "units": [{"platformId": "plat-a"}]}},
        "b": {
            "gauntlet": {
                "catalogRefs": ["plat-b", "plat-a"],
                "units": [{"platformId": "plat-b"}],
            }
        },
    }
    out = rebuild_counts(cells, policies_by_sid=policies)
    counts = out["counts"]
    assert sum(counts["missionClass"].values()) == 2
    assert counts["missionClass"] == {"patrol": 1, "strike": 1}
    assert counts["domain"] == {"surface": 2, "air": 1}
    assert counts["platformId"] == {"plat-a": 2, "plat-b": 1}
    assert "plat-b" in out["underusedPlatformHint"]


def test_assert_counts_consistent_detects_stale_mission_class() -> None:
    coverage = {
        "cellCount": 1,
        "scenarioCount": 1,
        "cells": [
            {
                "scenarioId": "x",
                "missionClass": "patrol",
                "eventClass": "none",
                "roePair": "WeaponsFree/WeaponsTight",
                "emconClass": "unrestricted",
                "domains": ["surface"],
            }
        ],
        "counts": {
            "missionClass": {"patrol": 99},  # stale
            "eventClass": {"none": 1},
            "roePair": {"WeaponsFree/WeaponsTight": 1},
            "emconClass": {"unrestricted": 1},
            "domain": {"surface": 1},
            "platformId": {},
        },
        "underusedPlatformHint": [],
    }
    with pytest.raises(AssertionError, match="missionClass"):
        assert_counts_consistent(coverage, policies_by_sid={})

