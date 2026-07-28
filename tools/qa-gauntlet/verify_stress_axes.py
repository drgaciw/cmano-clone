#!/usr/bin/env python3
"""Verifies that each stress axis actually changed simulation behaviour.

Each axis is checked by the mode it declared, because the axes are not equally
observable. A config-only axis is reported unproven by construction — that is
the point, not a limitation to be worked around.
"""

from __future__ import annotations

from typing import Any

from stress_axes import Axis


def verify_fingerprint_token(fingerprints: list[str], token: str) -> tuple[bool, str]:
    """Proven when the discrete token appears in at least one run fingerprint."""
    hits = sum(1 for f in fingerprints if token in f)
    if hits:
        return True, f"token {token} present in {hits}/{len(fingerprints)} fingerprints"
    return False, f"token {token} absent from all {len(fingerprints)} fingerprints"


def verify_differential_aggregate(stressed: list[int], control: list[int]) -> tuple[bool, str]:
    """Proven when the aggregate stressed total is strictly below the control total.

    Aggregate, never per-seed: jamming is probabilistic, and at least one seed
    routinely shows a zero delta while the totals are unambiguous.
    """
    s, c = sum(stressed), sum(control)
    if s < c:
        return True, f"aggregate {s} vs control {c} across {len(stressed)} seeds"
    return False, f"no aggregate reduction: {s} vs control {c}"


def verify_axis(axis: Axis, evidence: dict[str, Any]) -> dict[str, Any]:
    """Verifies one axis against collected evidence."""
    if axis.proof == "fingerprint-token":
        proven, detail = verify_fingerprint_token(
            evidence.get("fingerprints", []), axis.signal or ""
        )
    elif axis.proof == "differential-aggregate":
        proven, detail = verify_differential_aggregate(
            evidence.get("stressed", []), evidence.get("control", [])
        )
    else:
        proven = False
        gap = axis.gap or "unmodelled"
        detail = (
            f"config-only axis: configuration varies and schema is validated, but the "
            f"simulation cannot demonstrate it ({gap}). Not counted as proven."
        )

    return {"axis": axis.id, "proven": proven, "mode": axis.proof, "detail": detail}
