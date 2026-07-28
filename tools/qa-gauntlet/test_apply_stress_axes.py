#!/usr/bin/env python3
"""Unit tests for tools/qa-gauntlet/apply_stress_axes.py."""

from __future__ import annotations

import copy
import sys
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "qa-gauntlet"))

from apply_stress_axes import (  # noqa: E402
    apply_level,
    apply_selection,
    derive_scenario_id,
)
from stress_axes import load_axes  # noqa: E402

CATALOG = ROOT / "production" / "qa" / "gauntlet" / "corpus" / "stress-axes.yaml"


def base_policy() -> dict:
    return {
        "id": "gauntlet-t3-escort-strike",
        "engage": {"defaultMagazineRounds": 4, "salvoSize": 1, "pkKill": 0.25},
        "gauntlet": {"tier": 3, "intent": "escort + strike"},
    }


def test_apply_level_off_is_a_no_op():
    axes = load_axes(CATALOG)
    policy = base_policy()

    result = apply_level(policy, axes["weapons"], "off")

    assert result == policy


def test_apply_level_does_not_mutate_input():
    axes = load_axes(CATALOG)
    policy = base_policy()
    snapshot = copy.deepcopy(policy)

    apply_level(policy, axes["weapons"], "extreme")

    assert policy == snapshot


def test_apply_level_sets_dotted_paths():
    axes = load_axes(CATALOG)

    result = apply_level(base_policy(), axes["weapons"], "extreme")

    assert result["engage"]["defaultMagazineRounds"] == 1
    assert result["engage"]["salvoSize"] == 4
    assert result["engage"]["pkKill"] == 0.1


def test_apply_level_sets_whole_block_for_list_valued_axis():
    axes = load_axes(CATALOG)

    result = apply_level(base_policy(), axes["ew"], "extreme")

    assert result["jammers"] == [{"jamStrength": 0.9, "activeFromTick": 0}]


def test_derive_scenario_id_is_deterministic_and_order_independent():
    a = derive_scenario_id("gauntlet-t3-escort-strike", {"ew": "extreme", "weapons": "off"})
    b = derive_scenario_id("gauntlet-t3-escort-strike", {"weapons": "off", "ew": "extreme"})

    assert a == b
    assert a == "gauntlet-t3-escort-strike-ew-extreme"


def test_derive_scenario_id_omits_off_axes_entirely():
    assert derive_scenario_id("base", {"ew": "off", "logistics": "off", "weapons": "off"}) == "base"


def test_apply_selection_applies_every_axis_and_rewrites_id():
    axes = load_axes(CATALOG)

    result = apply_selection(
        base_policy(), axes, {"ew": "moderate", "weapons": "extreme", "logistics": "off"}
    )

    assert result["id"] == "gauntlet-t3-escort-strike-ew-moderate-weapons-extreme"
    assert result["engage"]["defaultMagazineRounds"] == 1
    assert result["jammers"][0]["jamStrength"] == 0.5
    assert "logistics" not in result


def test_apply_selection_rejects_unknown_level():
    axes = load_axes(CATALOG)

    with pytest.raises(KeyError):
        apply_selection(base_policy(), axes, {"ew": "catastrophic"})
