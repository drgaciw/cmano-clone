#!/usr/bin/env python3
"""Unit tests for tools/qa-gauntlet/saboteur.py pure logic (no worktrees in unit tests)."""

from __future__ import annotations

import sys
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "qa-gauntlet"))

from saboteur import (  # noqa: E402
    REPLAY_GOLDEN_PROJECT,
    VALID_ROLES,
    blocking_dirty_paths,
    exit_code_for,
    load_catalog,
    render_report,
    summarize,
)


def _mutant_yaml(
    *,
    mid: str = "01-x",
    patch: str = "m.patch",
    target: str = "src/Foo.cs",
    role: str | None = "defect",
) -> str:
    role_line = f'    role: "{role}"\n' if role is not None else ""
    return (
        "mutants:\n"
        f'  - id: "{mid}"\n'
        f'    patch: "{patch}"\n'
        f'    target: "{target}"\n'
        '    description: "d"\n'
        f"{role_line}"
        '    expectedOracles: ["goldens"]\n'
        '    impactRecorded: "LOW"\n'
    )


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


def test_load_catalog_parses_yaml_with_role(tmp_path):
    (tmp_path / "m.patch").write_text("--- a\n+++ b\n")
    (tmp_path / "catalog.yaml").write_text(_mutant_yaml(role="defect"))
    cat = load_catalog(tmp_path / "catalog.yaml")
    assert cat[0]["id"] == "01-x" and (tmp_path / cat[0]["patch"]).exists()
    assert cat[0]["role"] == "defect"


def test_load_catalog_rejects_missing_role(tmp_path):
    (tmp_path / "m.patch").write_text("x")
    (tmp_path / "catalog.yaml").write_text(_mutant_yaml(role=None))
    with pytest.raises(ValueError, match="role"):
        load_catalog(tmp_path / "catalog.yaml")


def test_load_catalog_rejects_unknown_role(tmp_path):
    (tmp_path / "m.patch").write_text("x")
    (tmp_path / "catalog.yaml").write_text(_mutant_yaml(role="noise"))
    with pytest.raises(ValueError, match="role"):
        load_catalog(tmp_path / "catalog.yaml")


def test_load_catalog_rejects_missing_patch(tmp_path):
    (tmp_path / "catalog.yaml").write_text(
        _mutant_yaml(patch="absent.patch", role="defect").replace(
            'expectedOracles: ["goldens"]', "expectedOracles: []"))
    with pytest.raises(ValueError, match="absent.patch"):
        load_catalog(tmp_path / "catalog.yaml")


def test_load_catalog_rejects_locked_target(tmp_path):
    (tmp_path / "m.patch").write_text("x")
    (tmp_path / "catalog.yaml").write_text(
        _mutant_yaml(
            target="src/ProjectAegis.Data/Catalog/GauntletOracleEvaluator.cs",
            role="defect",
        ).replace('expectedOracles: ["goldens"]', "expectedOracles: []"))
    with pytest.raises(ValueError, match="locked"):
        load_catalog(tmp_path / "catalog.yaml")


def test_load_catalog_real_catalog_roles():
    """Production catalog: every mutant has an allowed role; locked targets refused."""
    cat = load_catalog(ROOT / "tools/qa-gauntlet/mutants/catalog.yaml")
    by_id = {m["id"]: m["role"] for m in cat}
    assert by_id["00-noop-comment"] == "control"
    assert by_id["06-emcon-engage-bypass"] == "expected-miss"
    for mid in (
        "01-pd-weakened",
        "02-roe-tight-inverted",
        "03-salvo-off-by-one",
        "04-rng-seed-ignored",
        "05-contact-lifecycle-skip",
        "07-magazine-not-decremented",
    ):
        assert by_id[mid] == "defect", mid
    assert set(by_id.values()) <= VALID_ROLES


def test_summarize_kill_rate_excludes_control_and_expected_miss():
    results = [
        {"id": "00-noop", "role": "control", "outcome": "survived",
         "firedOracles": [], "expectedOracles": []},
        {"id": "01-x", "role": "defect", "outcome": "caught",
         "firedOracles": ["goldens"], "expectedOracles": ["goldens"]},
        {"id": "02-y", "role": "defect", "outcome": "survived",
         "firedOracles": [], "expectedOracles": ["victory_roe"]},
        {"id": "03-z", "role": "defect", "outcome": "invalid-mutant",
         "firedOracles": [], "expectedOracles": []},
        {"id": "06-emcon", "role": "expected-miss", "outcome": "survived",
         "firedOracles": [], "expectedOracles": []},
    ]
    s = summarize(results)
    assert s["caught"] == 1 and s["survived"] == 3 and s["invalid"] == 1
    # kill rate = caught_defects / (caught_defects + survived_defects); invalid + non-defects out
    assert s["killRate"] == "1/2"
    assert s["caughtDefects"] == 1 and s["survivedDefects"] == 1
    md = render_report(s, results)
    assert "02-y" in md and "SURVIVED" in md and "1/2" in md
    assert "role" in md.lower() or "defect" in md.lower()


def test_exit_code_role_matrix():
    """Exit contracts driven by catalog role, not id prefix magic."""
    control_survived = [
        {"id": "00-noop", "role": "control", "outcome": "survived",
         "firedOracles": [], "expectedOracles": []}]
    assert exit_code_for(summarize(control_survived), control_survived) == 0

    expected_miss_survived = [
        {"id": "06-emcon", "role": "expected-miss", "outcome": "survived",
         "firedOracles": [], "expectedOracles": []}]
    assert exit_code_for(summarize(expected_miss_survived), expected_miss_survived) == 0

    defect_survived = [
        {"id": "03-salvo", "role": "defect", "outcome": "survived",
         "firedOracles": [], "expectedOracles": ["goldens"]}]
    assert exit_code_for(summarize(defect_survived), defect_survived) == 1

    control_caught = [
        {"id": "00-noop", "role": "control", "outcome": "caught",
         "firedOracles": ["goldens"], "expectedOracles": []}]
    assert exit_code_for(summarize(control_caught), control_caught) == 1

    expected_miss_caught = [
        {"id": "06-emcon", "role": "expected-miss", "outcome": "caught",
         "firedOracles": ["goldens"], "expectedOracles": []}]
    assert exit_code_for(summarize(expected_miss_caught), expected_miss_caught) == 1

    defect_caught = [
        {"id": "01-pd", "role": "defect", "outcome": "caught",
         "firedOracles": ["goldens"], "expectedOracles": ["goldens"]}]
    assert exit_code_for(summarize(defect_caught), defect_caught) == 0

    invalid = [
        {"id": "01-pd", "role": "defect", "outcome": "invalid-mutant",
         "firedOracles": [], "expectedOracles": []}]
    assert exit_code_for(summarize(invalid), invalid) == 1

    # Today's catalog shape: 06 expected-miss survive OK; defect survivors (03/05) fail run
    todayish = [
        {"id": "00-noop-comment", "role": "control", "outcome": "survived",
         "firedOracles": [], "expectedOracles": []},
        {"id": "01-pd-weakened", "role": "defect", "outcome": "caught",
         "firedOracles": ["goldens"], "expectedOracles": ["goldens"]},
        {"id": "03-salvo-off-by-one", "role": "defect", "outcome": "survived",
         "firedOracles": [], "expectedOracles": ["goldens"]},
        {"id": "05-contact-lifecycle-skip", "role": "defect", "outcome": "survived",
         "firedOracles": [], "expectedOracles": ["goldens"]},
        {"id": "06-emcon-engage-bypass", "role": "expected-miss", "outcome": "survived",
         "firedOracles": [], "expectedOracles": []},
        {"id": "07-magazine-not-decremented", "role": "defect", "outcome": "caught",
         "firedOracles": ["token_coverage"], "expectedOracles": ["token_coverage"]},
    ]
    s = summarize(todayish)
    assert s["killRate"] == "2/4"  # 2 caught defects / (2 caught + 2 survived defects)
    assert exit_code_for(s, todayish) == 1  # solely because defect survivors 03/05


def test_replay_golden_project_is_unity_adapter_suite():
    """Baltic ReplayGolden (AGENTS.md) lives in UnityAdapter.Tests, not Delegation.Tests."""
    assert "UnityAdapter.Tests" in REPLAY_GOLDEN_PROJECT
    assert "Delegation.Tests" not in REPLAY_GOLDEN_PROJECT or "UnityAdapter" in REPLAY_GOLDEN_PROJECT
    assert Path(REPLAY_GOLDEN_PROJECT).as_posix().endswith(
        "ProjectAegis.Delegation.UnityAdapter.Tests")
