#!/usr/bin/env python3
"""Verifies that each stress axis actually changed simulation behaviour.

Each axis is checked by the mode it declared, because the axes are not equally
observable. A config-only axis is reported unproven by construction — that is
the point, not a limitation to be worked around.

Production gate (DRG-63 / S110-02)
----------------------------------
``verify_axis`` is the pure per-axis check. Call ``verify_axes`` / ``run_gate``
(or the CLI) to evaluate a full evidence map and fail the process only when a
**non-config-only** axis is unproven. Logistics (GAP-13) stays unproven without
hard-failing the gate.

CLI::

    python3 tools/qa-gauntlet/verify_stress_axes.py \\
        --evidence path/to/evidence.json \\
        [--axes production/qa/gauntlet/corpus/stress-axes.yaml] \\
        [--out path/to/report.json]
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Any

from stress_axes import Axis, load_axes

DEFAULT_AXES_PATH = (
    Path(__file__).resolve().parents[2]
    / "production"
    / "qa"
    / "gauntlet"
    / "corpus"
    / "stress-axes.yaml"
)

# Proof modes that must never hard-fail the production gate when unproven.
# Config-only means "we deliberately cannot demonstrate this at runtime".
CONFIG_ONLY_PROOF = "config-only"


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


def is_hard_fail_result(result: dict[str, Any]) -> bool:
    """True when an unproven result must fail the production gate.

    Config-only axes (logistics / GAP-13) are always unproven by design and must
    not hard-fail. Any other unproven axis fails the gate.
    """
    if result.get("proven"):
        return False
    return result.get("mode") != CONFIG_ONLY_PROOF


def verify_axes(
    axes: dict[str, Axis],
    evidence_by_axis: dict[str, Any],
    *,
    axis_ids: list[str] | None = None,
) -> dict[str, Any]:
    """Verify every requested axis against an evidence map.

    ``evidence_by_axis`` maps axis id → evidence dict consumed by ``verify_axis``.
    Missing evidence for an axis is treated as empty evidence (unproven for
    non-config-only modes).

    Returns a JSON-serialisable report with per-axis results, proven/unproven
    lists, and a ``pass`` flag that is False iff any non-config-only axis is
    unproven.
    """
    if axis_ids is None:
        ordered = sorted(axes.keys())
    else:
        ordered = list(axis_ids)
        unknown = [a for a in ordered if a not in axes]
        if unknown:
            raise KeyError(f"unknown axis id(s): {', '.join(unknown)}")

    results: list[dict[str, Any]] = []
    for axis_id in ordered:
        raw = evidence_by_axis.get(axis_id)
        if raw is None:
            evidence: dict[str, Any] = {}
        elif isinstance(raw, dict):
            evidence = raw
        else:
            raise TypeError(
                f"evidence for axis {axis_id!r} must be a dict, got {type(raw).__name__}"
            )
        results.append(verify_axis(axes[axis_id], evidence))

    proven = [r["axis"] for r in results if r["proven"]]
    unproven = [r["axis"] for r in results if not r["proven"]]
    hard_failures = [r["axis"] for r in results if is_hard_fail_result(r)]
    config_only_unproven = [
        r["axis"] for r in results if (not r["proven"] and r.get("mode") == CONFIG_ONLY_PROOF)
    ]

    return {
        "pass": len(hard_failures) == 0,
        "results": results,
        "proven": proven,
        "unproven": unproven,
        "hard_failures": hard_failures,
        "config_only_unproven": config_only_unproven,
    }


def load_evidence(path: Path) -> dict[str, Any]:
    """Load axis_id → evidence dict mapping from JSON."""
    data = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(data, dict):
        raise ValueError(f"evidence root must be a JSON object, got {type(data).__name__}")
    # Allow optional wrapper {"evidence": {...}} for future nesting.
    if "evidence" in data and isinstance(data["evidence"], dict) and not any(
        k in data for k in ("weapons", "ew", "logistics")
    ):
        return data["evidence"]
    return data


def run_gate(
    evidence_path: Path,
    axes_path: Path | None = None,
    *,
    axis_ids: list[str] | None = None,
    out_path: Path | None = None,
) -> dict[str, Any]:
    """Load catalog + evidence, verify axes, optionally write the report."""
    catalog = axes_path or DEFAULT_AXES_PATH
    axes = load_axes(catalog)
    evidence = load_evidence(evidence_path)
    report = verify_axes(axes, evidence, axis_ids=axis_ids)
    report["evidence"] = str(evidence_path)
    report["axes_catalog"] = str(catalog)
    if out_path is not None:
        out_path.parent.mkdir(parents=True, exist_ok=True)
        out_path.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
        report["out"] = str(out_path)
    return report


def main(argv: list[str] | None = None) -> int:
    """CLI entrypoint for the stress-axis proof gate.

    Exit codes:
      0 — all non-config-only axes proven (config-only may still be unproven)
      1 — one or more non-config-only axes unproven
      2 — usage / I/O error
    """
    parser = argparse.ArgumentParser(
        prog="verify_stress_axes.py",
        description=(
            "Production gate for stress-axis proof. Fails (exit 1) when a declared "
            "non-config-only axis is unproven. Config-only axes (e.g. logistics / "
            "GAP-13) are reported unproven but do not hard-fail."
        ),
    )
    parser.add_argument(
        "--evidence",
        required=True,
        type=Path,
        help="JSON map of axis_id → evidence dict (see README-stress-axes.md)",
    )
    parser.add_argument(
        "--axes",
        type=Path,
        default=DEFAULT_AXES_PATH,
        help=f"stress-axes.yaml catalog (default: {DEFAULT_AXES_PATH})",
    )
    parser.add_argument(
        "--axis",
        action="append",
        dest="axis_ids",
        default=None,
        help="limit verification to this axis id (repeatable); default: all catalog axes",
    )
    parser.add_argument(
        "--out",
        type=Path,
        default=None,
        help="write full JSON report to this path (also printed to stdout)",
    )
    args = parser.parse_args(argv)

    try:
        report = run_gate(
            args.evidence,
            args.axes,
            axis_ids=args.axis_ids,
            out_path=args.out,
        )
    except (OSError, ValueError, TypeError, KeyError, json.JSONDecodeError) as exc:
        print(json.dumps({"pass": False, "error": str(exc)}), file=sys.stderr)
        return 2

    print(json.dumps(report, indent=2))
    return 0 if report["pass"] else 1


if __name__ == "__main__":
    sys.exit(main())
