#!/usr/bin/env python3
"""Applies stress-axis levels to a base gauntlet policy.

Pure and deterministic: the same (policy, selection) always yields the same
derived policy and the same scenario id, so derived scenarios are reproducible
and diffable.
"""

from __future__ import annotations

import copy
from typing import Any

from stress_axes import Axis

# Axis order is fixed so derived ids are stable regardless of caller dict order.
AXIS_ORDER = ("ew", "logistics", "weapons")


def _set_dotted(target: dict[str, Any], path: str, value: Any) -> None:
    """Sets target["a"]["b"] for path "a.b", creating intermediate dicts."""
    parts = path.split(".")
    node = target
    for part in parts[:-1]:
        node = node.setdefault(part, {})
    node[parts[-1]] = value


def _nonempty_target(entry: Any) -> str | None:
    """Returns entry["targetId"] when it is a non-blank string, else None."""
    if not isinstance(entry, dict):
        return None
    target = entry.get("targetId")
    if isinstance(target, str) and target.strip():
        return target
    return None


def resolve_jam_target(policy: dict[str, Any]) -> str:
    """Picks, deterministically, the unit a jammer should target.

    Resolution order:

    1. the first non-empty ``targetId`` among the policy's own ``jammers`` —
       an author who already aimed a jammer keeps that aim;
    2. otherwise the first non-empty ``targetId`` in the policy's ``detection``
       block;
    3. otherwise ``ValueError``.

    Rule 2 is the semantically important one. ``ScenarioJamResolver`` matches a
    jammer to a detection trial by target id and skips every jammer whose
    ``TargetId`` differs, so jamming is only *observable* when it suppresses a
    detection somebody is actually attempting. A jammer aimed at a unit that no
    observer is trying to detect contributes nothing to any fingerprint, and the
    EW axis would then vary configuration while proving nothing — exactly the
    failure the proof modes exist to prevent. Targeting a unit that appears in
    the ``detection`` block is what makes the axis provable.
    """
    for entry in policy.get("jammers") or []:
        target = _nonempty_target(entry)
        if target is not None:
            return target

    for entry in policy.get("detection") or []:
        target = _nonempty_target(entry)
        if target is not None:
            return target

    raise ValueError(
        f"policy {policy.get('id', '<unknown>')!r}: cannot apply the EW axis to a "
        f"scenario with no detection targets. Neither the 'jammers' nor the "
        f"'detection' block supplies a non-empty 'targetId', so any jammer emitted "
        f"by the axis would never match a detection trial and jam strength would "
        f"always resolve to 0 — the axis would be a guaranteed no-op."
    )


def apply_level(policy: dict[str, Any], axis: Axis, level: str) -> dict[str, Any]:
    """Returns a new policy with one axis level applied. Does not mutate the input."""
    if level not in axis.levels:
        raise KeyError(f"axis {axis.id!r} has no level {level!r}")

    result = copy.deepcopy(policy)
    for key, value in (axis.levels[level] or {}).items():
        if "." in key:
            _set_dotted(result, key, copy.deepcopy(value))
        else:
            result[key] = copy.deepcopy(value)

        # Explicit special case for the EW axis — deliberately not a general
        # rule. The catalog declares jammers by strength and activation tick
        # only; a jammer with no targetId never matches any target in
        # ScenarioJamResolver, so the derived scenario would be inert. Resolve
        # a real target from the *original* policy: `result` may already have
        # had its own jammers block overwritten by the assignment above.
        if key == "jammers":
            target: str | None = None
            for jammer in result.get("jammers") or []:
                if not isinstance(jammer, dict):
                    continue
                if _nonempty_target(jammer) is not None:
                    continue
                if target is None:
                    target = resolve_jam_target(policy)
                jammer["targetId"] = target

    return result


def derive_scenario_id(base_id: str, selection: dict[str, str]) -> str:
    """Builds a stable derived id. Axes set to 'off' contribute nothing."""
    parts = [base_id]
    for axis_id in AXIS_ORDER:
        level = selection.get(axis_id, "off")
        if level != "off":
            parts.append(f"{axis_id}-{level}")
    return "-".join(parts)


def apply_selection(
    policy: dict[str, Any], axes: dict[str, Axis], selection: dict[str, str]
) -> dict[str, Any]:
    """Applies every axis in the selection and rewrites the policy id."""
    result = copy.deepcopy(policy)
    for axis_id in AXIS_ORDER:
        if axis_id not in selection:
            continue
        result = apply_level(result, axes[axis_id], selection[axis_id])

    result["id"] = derive_scenario_id(policy.get("id", "scenario"), selection)
    return result
