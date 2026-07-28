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
