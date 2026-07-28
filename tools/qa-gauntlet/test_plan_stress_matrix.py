#!/usr/bin/env python3
"""Unit tests for tools/qa-gauntlet/plan_stress_matrix.py."""

from __future__ import annotations

import itertools
import sys
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "qa-gauntlet"))

from plan_stress_matrix import estimate_runs, pairwise, plan_matrix  # noqa: E402
from stress_axes import load_axes  # noqa: E402

CATALOG = ROOT / "production" / "qa" / "gauntlet" / "corpus" / "stress-axes.yaml"
FACTORS = {
    "tier": ["1", "2", "3", "4", "5"],
    "ew": ["off", "moderate", "extreme"],
    "logistics": ["off", "moderate", "extreme"],
    "weapons": ["off", "moderate", "extreme"],
}


def all_pairs(factors):
    for (fa, la), (fb, lb) in itertools.combinations(factors.items(), 2):
        for va in la:
            for vb in lb:
                yield (fa, va), (fb, vb)


def test_pairwise_covers_every_pair_of_levels():
    configs = pairwise(FACTORS)

    for (fa, va), (fb, vb) in all_pairs(FACTORS):
        assert any(c[fa] == va and c[fb] == vb for c in configs), f"uncovered: {fa}={va}, {fb}={vb}"


def test_pairwise_is_far_smaller_than_full_factorial():
    configs = pairwise(FACTORS)

    full_factorial = 5 * 3 * 3 * 3  # 135
    assert len(configs) < full_factorial / 4
    # Lower bound is the largest pair product: 5 tiers x 3 levels.
    assert len(configs) >= 15


def test_pairwise_configs_are_complete_and_valid():
    for config in pairwise(FACTORS):
        assert set(config) == set(FACTORS)
        for factor, level in config.items():
            assert level in FACTORS[factor]


def test_estimate_runs_counts_seeds_and_ew_control_twins():
    axes = load_axes(CATALOG)
    configs = [
        {"tier": "3", "ew": "off", "logistics": "off", "weapons": "off"},
        {"tier": "3", "ew": "extreme", "logistics": "off", "weapons": "off"},
    ]

    # 2 configs x 3 seeds = 6, plus a control twin for the one EW-on config x 3 seeds = 3.
    assert estimate_runs(configs, axes, seeds=3) == 9


def test_plan_matrix_respects_the_hard_cap_and_reports_drops():
    axes = load_axes(CATALOG)

    plan = plan_matrix(axes, tiers=[1, 2, 3, 4, 5], seeds=3, max_configs=10)

    assert len(plan["configs"]) == 10
    assert plan["dropped"] > 0, "truncation must be reported, never silent"
    assert plan["estimatedRuns"] > 0


def test_plan_matrix_reports_zero_drops_when_under_cap():
    axes = load_axes(CATALOG)

    plan = plan_matrix(axes, tiers=[1, 2, 3, 4, 5], seeds=3, max_configs=1000)

    assert plan["dropped"] == 0
