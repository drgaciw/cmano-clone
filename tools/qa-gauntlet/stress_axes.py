#!/usr/bin/env python3
"""Stress-axis catalog loader and validator.

Axes are orthogonal to the 5 complexity tiers: a tier sets mission/platform
complexity, an axis layers pressure on top. Each axis declares how it can be
mechanically proven, so an axis that cannot be demonstrated at runtime is
structurally prevented from claiming it was.
"""

from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path
from typing import Any

import yaml

# WARNING on "fingerprint-token": it asserts only that a token is PRESENT, so it
# is valid exclusively for a token that does NOT occur in unstressed baseline
# runs. "NO_AMMO" is not such a token — it appears 106 times in the unstressed
# tier-1 baseline of gauntlet-20260727-1455, so a presence assertion on it is
# satisfied by an axis level of "off" and proves nothing. Any token that the
# baseline can emit belongs in "differential-token", which compares stressed
# against a control sibling. The mode is kept in the vocabulary for a genuinely
# stress-exclusive token; confirm absence from a baseline before using it.
PROOF_MODES = {
    "fingerprint-token",
    "differential-token",
    "differential-aggregate",
    "config-only",
}


@dataclass
class Axis:
    """One orthogonal stress axis and its levels."""

    id: str
    proof: str
    levels: dict[str, dict[str, Any]]
    signal: str | None = None
    requires_control_sibling: bool = False
    gap: str | None = None


def load_axes(path: Path) -> dict[str, Axis]:
    """Loads the axis catalog, keyed by axis id."""
    raw = yaml.safe_load(path.read_text(encoding="utf-8"))
    axes: dict[str, Axis] = {}
    for entry in raw.get("axes", []):
        axes[entry["id"]] = Axis(
            id=entry["id"],
            proof=entry.get("proof", ""),
            levels=entry.get("levels", {}) or {},
            signal=entry.get("signal"),
            requires_control_sibling=bool(entry.get("requires_control_sibling", False)),
            gap=entry.get("gap"),
        )
    return axes


def validate_axes(axes: dict[str, Axis]) -> list[str]:
    """Returns a list of validation errors; empty means the catalog is valid."""
    errors: list[str] = []
    for name, axis in axes.items():
        if axis.proof not in PROOF_MODES:
            errors.append(f"{name}: unknown proof mode {axis.proof!r}")
        if "off" not in axis.levels:
            errors.append(f"{name}: missing required 'off' level")
        if axis.proof == "differential-aggregate" and not axis.requires_control_sibling:
            errors.append(f"{name}: differential-aggregate proof requires a control sibling")
        if axis.proof == "fingerprint-token" and not axis.signal:
            errors.append(f"{name}: fingerprint-token proof requires a signal token")
        if axis.proof == "differential-token":
            if not axis.signal:
                errors.append(f"{name}: differential-token proof requires a signal token")
            if not axis.requires_control_sibling:
                errors.append(f"{name}: differential-token proof requires a control sibling")
    return errors
