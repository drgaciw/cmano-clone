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


def verify_differential_token(
    stressed: list[str], control: list[str], token: str
) -> tuple[bool, str]:
    """Proven when the token occurs strictly more often under stress than in control.

    Presence alone is not evidence for any token the unstressed ladder can also
    emit — NO_AMMO occurs 106 times in the unstressed tier-1 baseline, so a
    presence check would pass for a weapons axis set to "off". Counting total
    occurrences across each group and demanding a strict increase is what makes
    the axis's own contribution visible.
    """
    # A differential proof is meaningless without both sides: if either group
    # is empty there is nothing to compare against, and the safe default for
    # a verifier that exists to refuse unevidenced claims is not-proven.
    if not stressed or not control:
        return False, (
            f"insufficient evidence: {len(stressed)} stressed / {len(control)} control "
            f"samples — a differential proof requires both"
        )

    s = sum(f.count(token) for f in stressed)
    c = sum(f.count(token) for f in control)
    if s > c:
        return True, (
            f"token {token} occurs {s}x stressed vs {c}x control "
            f"({len(stressed)} stressed / {len(control)} control fingerprints)"
        )
    return False, (
        f"no increase in {token}: {s}x stressed vs {c}x control "
        f"({len(stressed)} stressed / {len(control)} control fingerprints)"
    )


def verify_differential_aggregate(stressed: list[int], control: list[int]) -> tuple[bool, str]:
    """Proven when the aggregate stressed total is strictly below the control total.

    Aggregate, never per-seed: jamming is probabilistic, and at least one seed
    routinely shows a zero delta while the totals are unambiguous.
    """
    # As above: a differential proof is meaningless without both sides, and a
    # run that produced no data at all must never "prove" a reduction — the
    # safe default when either group is empty is not-proven.
    if not stressed or not control:
        return False, (
            f"insufficient evidence: {len(stressed)} stressed / {len(control)} control "
            f"samples — a differential proof requires both"
        )

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
    elif axis.proof == "differential-token":
        proven, detail = verify_differential_token(
            evidence.get("stressed", []), evidence.get("control", []), axis.signal or ""
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
