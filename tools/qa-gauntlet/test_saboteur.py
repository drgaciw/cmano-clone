#!/usr/bin/env python3
"""Unit tests for tools/qa-gauntlet/saboteur.py pure logic (no worktrees in unit tests)."""

from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "qa-gauntlet"))

from saboteur import blocking_dirty_paths, exit_code_for, load_catalog, render_report, summarize  # noqa: E402


def test_blocking_dirty_paths_scopes_to_calibration_relevant_dirs():
    porcelain = (
        " M docs/architecture/architecture.md\n"
        " M AGENTS.md\n"
        " M src/ProjectAegis.Sim/Sensors/DetectionProbability.cs\n"
        " M data/scenarios/gauntlet-t1-patrol-a.policy.json\n"
        " M tools/qa-gauntlet/evaluate_run.py\n")
    blocking = blocking_dirty_paths(porcelain)
    assert "src/ProjectAegis.Sim/Sensors/DetectionProbability.cs" in blocking
    assert "data/scenarios/gauntlet-t1-patrol-a.policy.json" in blocking
    assert "tools/qa-gauntlet/evaluate_run.py" in blocking
    assert not any("docs/" in b or "AGENTS" in b for b in blocking)
    assert blocking_dirty_paths(" M docs/notes.md\n") == []


def test_load_catalog_parses_yaml(tmp_path):
    (tmp_path / "m.patch").write_text("--- a\n+++ b\n")
    (tmp_path / "catalog.yaml").write_text(
        "mutants:\n"
        "  - id: \"01-x\"\n"
        "    patch: \"m.patch\"\n"
        "    target: \"src/Foo.cs\"\n"
        "    description: \"d\"\n"
        "    expectedOracles: [\"goldens\"]\n"
        "    impactRecorded: \"LOW\"\n")
    cat = load_catalog(tmp_path / "catalog.yaml")
    assert cat[0]["id"] == "01-x" and (tmp_path / cat[0]["patch"]).exists()


def test_load_catalog_rejects_missing_patch(tmp_path):
    (tmp_path / "catalog.yaml").write_text(
        "mutants:\n  - id: \"01-x\"\n    patch: \"absent.patch\"\n    target: \"t\"\n"
        "    description: \"d\"\n    expectedOracles: []\n    impactRecorded: \"LOW\"\n")
    try:
        load_catalog(tmp_path / "catalog.yaml")
        assert False, "expected ValueError"
    except ValueError as e:
        assert "absent.patch" in str(e)


def test_load_catalog_rejects_locked_target(tmp_path):
    (tmp_path / "m.patch").write_text("x")
    (tmp_path / "catalog.yaml").write_text(
        "mutants:\n  - id: \"01-x\"\n    patch: \"m.patch\"\n"
        "    target: \"src/ProjectAegis.Data/Catalog/GauntletOracleEvaluator.cs\"\n"
        "    description: \"d\"\n    expectedOracles: []\n    impactRecorded: \"LOW\"\n")
    try:
        load_catalog(tmp_path / "catalog.yaml")
        assert False, "expected ValueError"
    except ValueError as e:
        assert "locked" in str(e)


def test_summarize_and_report():
    results = [
        {"id": "01-x", "outcome": "caught", "firedOracles": ["goldens"], "expectedOracles": ["goldens"]},
        {"id": "02-y", "outcome": "survived", "firedOracles": [], "expectedOracles": ["victory_roe"]},
        {"id": "03-z", "outcome": "invalid-mutant", "firedOracles": [], "expectedOracles": []},
    ]
    s = summarize(results)
    assert s["caught"] == 1 and s["survived"] == 1 and s["invalid"] == 1
    assert s["killRate"] == "1/2"  # invalid mutants excluded from denominator
    md = render_report(s, results)
    assert "02-y" in md and "SURVIVED" in md and "1/2" in md


def test_exit_code_excludes_control_survivors():
    # 00-* mutants are behavior-neutral controls: they MUST survive and must not fail the run.
    control_only = [{"id": "00-noop", "outcome": "survived", "firedOracles": [], "expectedOracles": []}]
    assert exit_code_for(summarize(control_only), control_only) == 0
    real_survivor = control_only + [
        {"id": "02-y", "outcome": "survived", "firedOracles": [], "expectedOracles": ["goldens"]}]
    assert exit_code_for(summarize(real_survivor), real_survivor) == 1
    control_caught = [{"id": "00-noop", "outcome": "caught", "firedOracles": ["goldens"], "expectedOracles": []}]
    assert exit_code_for(summarize(control_caught), control_caught) == 1  # false positive!
