#!/usr/bin/env python3
"""Unit tests for tools/qa-gauntlet/stress_axes.py (stress-axis catalog)."""

from __future__ import annotations

import sys
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "qa-gauntlet"))

from stress_axes import (  # noqa: E402
    PROOF_MODES,
    Axis,
    load_axes,
    validate_axes,
)

CATALOG = ROOT / "production" / "qa" / "gauntlet" / "corpus" / "stress-axes.yaml"


def test_catalog_file_exists():
    assert CATALOG.exists(), f"missing catalog: {CATALOG}"


def test_catalog_declares_the_three_axes():
    axes = load_axes(CATALOG)
    assert set(axes) == {"ew", "logistics", "weapons"}


def test_every_axis_has_an_off_level():
    axes = load_axes(CATALOG)
    for name, axis in axes.items():
        assert "off" in axis.levels, f"{name} has no 'off' level"


def test_proof_modes_are_recognised_and_correctly_assigned():
    axes = load_axes(CATALOG)
    for axis in axes.values():
        assert axis.proof in PROOF_MODES

    assert axes["weapons"].proof == "fingerprint-token"
    assert axes["ew"].proof == "differential-aggregate"
    # GAP-13: FuelStateProjection is UI-only, so logistics cannot be runtime-proven.
    assert axes["logistics"].proof == "config-only"


def test_validate_accepts_the_shipped_catalog():
    assert validate_axes(load_axes(CATALOG)) == []


def test_validate_rejects_unknown_proof_mode():
    bad = {"ew": Axis(id="ew", proof="vibes", levels={"off": {}})}
    errors = validate_axes(bad)
    assert any("proof" in e for e in errors)


def test_validate_rejects_axis_without_off_level():
    bad = {"ew": Axis(id="ew", proof="config-only", levels={"extreme": {}})}
    errors = validate_axes(bad)
    assert any("off" in e for e in errors)
