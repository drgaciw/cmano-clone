#!/usr/bin/env python3
"""Unit tests for the production stress-axis proof gate (DRG-63 / S110-02)."""

from __future__ import annotations

import json
import subprocess
import sys
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "qa-gauntlet"))

from gate_stress_proof import (  # noqa: E402
    is_hard_fail_result,
    main as gate_main,
    run_gate,
    verify_axes,
)
from stress_axes import load_axes  # noqa: E402
from verify_stress_axes import main as verify_main  # noqa: E402

CATALOG = ROOT / "production" / "qa" / "gauntlet" / "corpus" / "stress-axes.yaml"
GATE_PY = ROOT / "tools" / "qa-gauntlet" / "gate_stress_proof.py"
VERIFY_PY = ROOT / "tools" / "qa-gauntlet" / "verify_stress_axes.py"
SHELL_GATE = ROOT / "tools" / "qa-gauntlet" / "run-stress-proof-gate.sh"


def _axes():
    return load_axes(CATALOG)


def _proven_evidence() -> dict:
    return {
        "weapons": {
            "stressed": ["NO_AMMO NO_AMMO NO_AMMO"],
            "control": ["NO_AMMO"],
        },
        "ew": {"stressed": [6, 8, 8], "control": [10, 10, 8]},
        "logistics": {},
    }


def _unproven_weapons_evidence() -> dict:
    return {
        "weapons": {
            "stressed": ["NO_AMMO"],
            "control": ["NO_AMMO NO_AMMO"],
        },
        "ew": {"stressed": [6, 8, 8], "control": [10, 10, 8]},
        "logistics": {},
    }


def test_gate_passes_when_non_config_axes_proven_and_logistics_unproven():
    report = verify_axes(_axes(), _proven_evidence())

    assert report["pass"] is True
    assert "weapons" in report["proven"]
    assert "ew" in report["proven"]
    assert "logistics" in report["unproven"]
    assert "logistics" in report["config_only_unproven"]
    assert report["hard_failures"] == []


def test_gate_fails_when_weapons_unproven():
    report = verify_axes(_axes(), _unproven_weapons_evidence())

    assert report["pass"] is False
    assert "weapons" in report["hard_failures"]
    assert "logistics" in report["config_only_unproven"]
    # logistics must still not appear as a hard failure
    assert "logistics" not in report["hard_failures"]


def test_gate_fails_when_weapons_evidence_missing():
    """TC-63-2: incomplete weapons evidence → non-zero gate."""
    evidence = {
        "ew": {"stressed": [6, 8, 8], "control": [10, 10, 8]},
        "logistics": {},
    }
    report = verify_axes(_axes(), evidence)

    assert report["pass"] is False
    assert "weapons" in report["hard_failures"]


def test_config_only_never_hard_fails(tmp_path: Path):
    """TC-63-3: logistics config-only does not fail the gate by design."""
    evidence = {
        "weapons": {
            "stressed": ["NO_AMMO NO_AMMO"],
            "control": ["NO_AMMO"],
        },
        "ew": {"stressed": [1, 1, 1], "control": [5, 5, 5]},
        # logistics omitted or empty — still unproven, still not a hard fail
    }
    path = tmp_path / "evidence.json"
    path.write_text(json.dumps(evidence), encoding="utf-8")
    report = run_gate(path, CATALOG)

    assert report["pass"] is True
    logistics = next(r for r in report["results"] if r["axis"] == "logistics")
    assert logistics["proven"] is False
    assert logistics["mode"] == "config-only"
    assert is_hard_fail_result(logistics) is False


def test_is_hard_fail_result_rules():
    assert is_hard_fail_result({"proven": True, "mode": "differential-token"}) is False
    assert is_hard_fail_result({"proven": False, "mode": "differential-token"}) is True
    assert is_hard_fail_result({"proven": False, "mode": "config-only"}) is False


def test_cli_exit_0_on_pass(tmp_path: Path):
    path = tmp_path / "ok.json"
    path.write_text(json.dumps(_proven_evidence()), encoding="utf-8")
    out = tmp_path / "report.json"

    code = gate_main(["--evidence", str(path), "--axes", str(CATALOG), "--out", str(out)])

    assert code == 0
    report = json.loads(out.read_text(encoding="utf-8"))
    assert report["pass"] is True


def test_cli_exit_1_on_unproven_non_config(tmp_path: Path):
    path = tmp_path / "bad.json"
    path.write_text(json.dumps(_unproven_weapons_evidence()), encoding="utf-8")

    code = verify_main(["--evidence", str(path), "--axes", str(CATALOG)])

    assert code == 1


def test_cli_exit_2_on_missing_evidence(tmp_path: Path):
    code = gate_main(["--evidence", str(tmp_path / "nope.json"), "--axes", str(CATALOG)])
    assert code == 2


def test_subprocess_gate_stress_proof_py(tmp_path: Path):
    path = tmp_path / "ok.json"
    path.write_text(json.dumps(_proven_evidence()), encoding="utf-8")
    proc = subprocess.run(
        [sys.executable, str(GATE_PY), "--evidence", str(path), "--axes", str(CATALOG)],
        cwd=str(ROOT),
        capture_output=True,
        text=True,
        check=False,
    )
    assert proc.returncode == 0
    payload = json.loads(proc.stdout)
    assert payload["pass"] is True


def test_subprocess_verify_stress_axes_py_fail(tmp_path: Path):
    path = tmp_path / "bad.json"
    path.write_text(json.dumps({"weapons": {"stressed": ["x"], "control": ["x x"]}}), encoding="utf-8")
    proc = subprocess.run(
        [
            sys.executable,
            str(VERIFY_PY),
            "--evidence",
            str(path),
            "--axes",
            str(CATALOG),
            "--axis",
            "weapons",
        ],
        cwd=str(ROOT),
        capture_output=True,
        text=True,
        check=False,
    )
    assert proc.returncode == 1


def test_shell_wrapper_gate(tmp_path: Path):
    path = tmp_path / "ok.json"
    out = tmp_path / "out.json"
    path.write_text(json.dumps(_proven_evidence()), encoding="utf-8")
    proc = subprocess.run(
        [
            "bash",
            str(SHELL_GATE),
            "--evidence",
            str(path),
            "--axes",
            str(CATALOG),
            "--out",
            str(out),
        ],
        cwd=str(ROOT),
        capture_output=True,
        text=True,
        check=False,
    )
    assert proc.returncode == 0
    assert out.is_file()
    assert json.loads(out.read_text(encoding="utf-8"))["pass"] is True


def test_logistics_only_axis_filter_passes(tmp_path: Path):
    """Restricting the gate to logistics alone must pass (config-only)."""
    path = tmp_path / "empty.json"
    path.write_text(json.dumps({}), encoding="utf-8")
    code = gate_main(
        ["--evidence", str(path), "--axes", str(CATALOG), "--axis", "logistics"]
    )
    assert code == 0
