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
    resolve_jam_target,
)
from stress_axes import load_axes  # noqa: E402

CATALOG = ROOT / "production" / "qa" / "gauntlet" / "corpus" / "stress-axes.yaml"


def base_policy() -> dict:
    # A detection block is not decoration: the EW axis resolves its jam target
    # from it, and a policy without one cannot be jammed observably at all.
    return {
        "id": "gauntlet-t3-escort-strike",
        "engage": {"defaultMagazineRounds": 4, "salvoSize": 1, "pkKill": 0.25},
        "gauntlet": {"tier": 3, "intent": "escort + strike"},
        "detection": [
            {
                "observerId": "f-230-norfolk-type-23-duke",
                "sensorId": "radar-1",
                "targetId": "skr-admiral-sergey-gorshkov-pr-2235-0",
                "contactId": "c-t3s5-1",
                "basePd": 1.0,
            },
            {
                "observerId": "p-960-skjold",
                "sensorId": "radar-1",
                "targetId": "801-rais-hamidou-pr-1234e-nanuchka-ii-1981",
                "contactId": "c-t3s5-4",
                "basePd": 0.7,
            },
        ],
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

    # targetId is filled in from the base policy's detection block — a jammer
    # without one is skipped by ScenarioJamResolver and jams nothing.
    assert result["jammers"] == [
        {
            "jamStrength": 0.9,
            "activeFromTick": 0,
            "targetId": "skr-admiral-sergey-gorshkov-pr-2235-0",
        }
    ]


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


# --- EW jam-target resolution (Critical-1 fix) --------------------------------


def test_resolve_jam_target_prefers_an_existing_jammer_target():
    policy = base_policy()
    policy["jammers"] = [{"targetId": "already-aimed-unit", "jamStrength": 0.3}]

    assert resolve_jam_target(policy) == "already-aimed-unit"


def test_apply_ew_reuses_an_existing_jammers_target_id():
    axes = load_axes(CATALOG)
    policy = base_policy()
    policy["jammers"] = [{"targetId": "already-aimed-unit", "jamStrength": 0.3}]

    result = apply_level(policy, axes["ew"], "extreme")

    # The axis replaces the jammers block wholesale, but the author's aim is
    # recovered from the ORIGINAL policy, not from the overwritten copy.
    assert result["jammers"] == [
        {"jamStrength": 0.9, "activeFromTick": 0, "targetId": "already-aimed-unit"}
    ]


def test_apply_ew_falls_back_to_the_first_detection_target():
    axes = load_axes(CATALOG)
    policy = base_policy()
    assert "jammers" not in policy

    result = apply_level(policy, axes["ew"], "moderate")

    assert result["jammers"][0]["targetId"] == "skr-admiral-sergey-gorshkov-pr-2235-0"


def test_resolve_jam_target_uses_first_detection_entry_in_order():
    policy = base_policy()

    # Deterministic: always the first detection entry, never an arbitrary one.
    assert resolve_jam_target(policy) == "skr-admiral-sergey-gorshkov-pr-2235-0"


def test_apply_ew_raises_when_there_is_nothing_to_jam():
    axes = load_axes(CATALOG)
    policy = base_policy()
    del policy["detection"]

    with pytest.raises(ValueError) as excinfo:
        apply_level(policy, axes["ew"], "extreme")

    message = str(excinfo.value)
    assert "gauntlet-t3-escort-strike" in message
    assert "detection" in message


def test_resolve_jam_target_ignores_blank_target_ids():
    policy = base_policy()
    policy["jammers"] = [{"targetId": "   ", "jamStrength": 0.3}]

    # A blank id matches no target either, so it must not satisfy rule 1.
    assert resolve_jam_target(policy) == "skr-admiral-sergey-gorshkov-pr-2235-0"


def test_regression_derived_ew_jammer_always_has_a_nonempty_target_id():
    """Guard against the Critical-1 no-op returning.

    ScenarioJamResolver skips every jammer whose TargetId does not match the
    target under evaluation, so a jammer with a missing or blank targetId
    resolves 0 jam strength and the whole EW axis becomes inert. Every derived
    EW scenario must carry a real target on every jammer.
    """
    axes = load_axes(CATALOG)

    for level in ("moderate", "extreme"):
        result = apply_level(base_policy(), axes["ew"], level)
        jammers = result["jammers"]
        assert jammers, f"ew/{level} emitted no jammers at all"
        for jammer in jammers:
            target = jammer.get("targetId")
            assert isinstance(target, str) and target.strip(), (
                f"ew/{level} produced a jammer with no usable targetId: {jammer!r}"
            )

    # And through the full selection path, not just a single level application.
    selected = apply_selection(base_policy(), axes, {"ew": "extreme", "weapons": "moderate"})
    assert selected["jammers"][0]["targetId"].strip()
