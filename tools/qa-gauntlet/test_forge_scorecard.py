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
    infer_cell,
    read_oracle_passed,
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
