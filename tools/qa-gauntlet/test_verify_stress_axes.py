#!/usr/bin/env python3
"""Unit tests for tools/qa-gauntlet/verify_stress_axes.py."""

from __future__ import annotations

import sys
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "qa-gauntlet"))

from stress_axes import load_axes  # noqa: E402
from verify_stress_axes import (  # noqa: E402
    verify_axis,
    verify_differential_aggregate,
    verify_differential_token,
    verify_fingerprint_token,
)

CATALOG = ROOT / "production" / "qa" / "gauntlet" / "corpus" / "stress-axes.yaml"


def test_fingerprint_token_present_is_proven():
    proven, detail = verify_fingerprint_token(["...NO_AMMO...", "..."], "NO_AMMO")
    assert proven is True
    assert "NO_AMMO" in detail


def test_fingerprint_token_absent_is_not_proven():
    proven, _ = verify_fingerprint_token(["...", "..."], "NO_AMMO")
    assert proven is False


def test_differential_aggregate_uses_totals_not_per_seed():
    # Per-seed deltas +4, +2, 0 — a per-seed rule would fail on the third seed,
    # wrongly reporting working jamming as broken. Aggregate 22 vs 28 is decisive.
    proven, detail = verify_differential_aggregate(stressed=[6, 8, 8], control=[10, 10, 8])
    assert proven is True
    assert "22" in detail and "28" in detail


def test_differential_aggregate_rejects_a_null_result():
    proven, _ = verify_differential_aggregate(stressed=[10, 10, 10], control=[10, 10, 10])
    assert proven is False


def test_differential_aggregate_rejects_wrong_direction():
    proven, _ = verify_differential_aggregate(stressed=[12, 12, 12], control=[10, 10, 10])
    assert proven is False


def test_verify_axis_weapons_uses_differential_token_mode():
    axes = load_axes(CATALOG)
    result = verify_axis(
        axes["weapons"],
        {"stressed": ["NO_AMMO NO_AMMO NO_AMMO"], "control": ["NO_AMMO"]},
    )

    assert result["mode"] == "differential-token"
    assert result["proven"] is True


def test_verify_axis_ew_uses_differential_mode():
    axes = load_axes(CATALOG)
    result = verify_axis(axes["ew"], {"stressed": [6, 8, 8], "control": [10, 10, 8]})

    assert result["mode"] == "differential-aggregate"
    assert result["proven"] is True


def test_regression_baseline_abundant_token_at_parity_is_not_proven():
    """Critical-2 regression: presence of NO_AMMO is not evidence of stress.

    NO_AMMO occurs 106 times in the unstressed tier-1 baseline. Under the old
    presence check a weapons-`off` run verified as proven — the exact failure
    the proof-mode design exists to prevent. Equal counts must never prove.
    """
    baseline_like = ["NO_AMMO " * 10, "NO_AMMO " * 8]
    proven, detail = verify_differential_token(
        stressed=baseline_like, control=list(baseline_like), token="NO_AMMO"
    )

    assert proven is False
    assert "18" in detail  # both counts reported, stressed and control alike


def test_differential_token_strict_increase_is_proven():
    proven, detail = verify_differential_token(
        stressed=["NO_AMMO NO_AMMO", "NO_AMMO NO_AMMO NO_AMMO"],
        control=["NO_AMMO", "NO_AMMO"],
        token="NO_AMMO",
    )

    assert proven is True
    assert "5" in detail and "2" in detail


def test_differential_token_fewer_under_stress_is_not_proven():
    proven, _ = verify_differential_token(
        stressed=["NO_AMMO"], control=["NO_AMMO NO_AMMO NO_AMMO"], token="NO_AMMO"
    )

    assert proven is False


def test_differential_token_empty_inputs_are_not_proven():
    proven, _ = verify_differential_token(stressed=[], control=[], token="NO_AMMO")

    assert proven is False


def test_verify_axis_logistics_is_never_reported_as_proven():
    axes = load_axes(CATALOG)
    result = verify_axis(axes["logistics"], {"fingerprints": ["anything"]})

    assert result["mode"] == "config-only"
    assert result["proven"] is False
    assert "GAP-13" in result["detail"]


def test_differential_token_nonempty_stressed_empty_control_is_not_proven():
    """Critical: missing 'control' evidence must never default to proven."""
    proven, detail = verify_differential_token(
        stressed=["NO_AMMO NO_AMMO"], control=[], token="NO_AMMO"
    )

    assert proven is False
    assert "insufficient evidence" in detail


def test_differential_token_empty_stressed_nonempty_control_is_not_proven():
    """Critical: missing 'stressed' evidence must never default to proven."""
    proven, detail = verify_differential_token(
        stressed=[], control=["NO_AMMO NO_AMMO"], token="NO_AMMO"
    )

    assert proven is False
    assert "insufficient evidence" in detail


def test_differential_aggregate_empty_stressed_nonempty_control_is_not_proven():
    """Critical: a stressed run that produced no data at all must not 'prove' a reduction."""
    proven, detail = verify_differential_aggregate(stressed=[], control=[10, 10, 10])

    assert proven is False
    assert "insufficient evidence" in detail


def test_differential_aggregate_nonempty_stressed_empty_control_is_not_proven():
    """Critical: missing control evidence must never default to proven."""
    proven, detail = verify_differential_aggregate(stressed=[10, 10, 10], control=[])

    assert proven is False
    assert "insufficient evidence" in detail


def test_verify_axis_weapons_stressed_only_evidence_is_not_proven():
    """End-to-end regression: a missing 'control' key must not prove the weapons axis.

    Reproduces the exact Critical finding: verify_axis(weapons, {"stressed": [...]})
    with no "control" key at all previously returned proven=True.
    """
    axes = load_axes(CATALOG)
    result = verify_axis(axes["weapons"], {"stressed": ["NO_AMMO " * 106]})

    assert result["proven"] is False


def test_verify_axis_ew_control_only_evidence_is_not_proven():
    """End-to-end regression: a stressed run with no data must not prove the EW axis.

    Reproduces the exact Critical finding: verify_axis(ew, {"control": [10, 10, 10]})
    with no "stressed" key at all previously returned proven=True — a run that
    produced no data "proving" that jamming works.
    """
    axes = load_axes(CATALOG)
    result = verify_axis(axes["ew"], {"control": [10, 10, 10]})

    assert result["proven"] is False
