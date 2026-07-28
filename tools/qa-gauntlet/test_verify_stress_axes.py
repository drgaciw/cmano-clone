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


def test_verify_axis_weapons_uses_fingerprint_mode():
    axes = load_axes(CATALOG)
    result = verify_axis(axes["weapons"], {"fingerprints": ["x NO_AMMO y"]})

    assert result["mode"] == "fingerprint-token"
    assert result["proven"] is True


def test_verify_axis_ew_uses_differential_mode():
    axes = load_axes(CATALOG)
    result = verify_axis(axes["ew"], {"stressed": [6, 8, 8], "control": [10, 10, 8]})

    assert result["mode"] == "differential-aggregate"
    assert result["proven"] is True


def test_verify_axis_logistics_is_never_reported_as_proven():
    axes = load_axes(CATALOG)
    result = verify_axis(axes["logistics"], {"fingerprints": ["anything"]})

    assert result["mode"] == "config-only"
    assert result["proven"] is False
    assert "GAP-13" in result["detail"]
