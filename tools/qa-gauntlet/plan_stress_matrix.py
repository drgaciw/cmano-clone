#!/usr/bin/env python3
"""Builds a bounded pairwise work list for tier x stress-axis combinations.

A full cross-product of 5 tiers and three 3-level axes is 135 scenarios, which
at tier tick budgets and 3 seeds is not affordable. Pairwise (2-way) covering
guarantees every pair of factor levels appears together at least once, which is
where the large majority of interaction defects live, at roughly a tenth of the
cost. Truncation is always reported, never silent.
"""

from __future__ import annotations

import itertools
from typing import Any

from stress_axes import Axis


def _uncovered_pairs(factors: dict[str, list[str]]) -> set:
    pairs = set()
    for (fa, la), (fb, lb) in itertools.combinations(sorted(factors.items()), 2):
        for va in la:
            for vb in lb:
                pairs.add(((fa, va), (fb, vb)))
    return pairs


def _covered_by(config: dict[str, str], factors: dict[str, list[str]]) -> set:
    covered = set()
    for fa, fb in itertools.combinations(sorted(factors), 2):
        covered.add(((fa, config[fa]), (fb, config[fb])))
    return covered


def pairwise(factors: dict[str, list[str]]) -> list[dict[str, str]]:
    """Greedy pairwise covering array over the supplied factors.

    Deterministic: factors and levels are processed in sorted order, so the same
    input always yields the same array.
    """
    remaining = _uncovered_pairs(factors)
    names = sorted(factors)
    configs: list[dict[str, str]] = []

    # Deterministic candidate order: full factorial enumerated in sorted order.
    candidates = [
        dict(zip(names, combo))
        for combo in itertools.product(*(factors[n] for n in names))
    ]

    while remaining:
        best = None
        best_gain = -1
        for candidate in candidates:
            gain = len(_covered_by(candidate, factors) & remaining)
            if gain > best_gain:
                best_gain = gain
                best = candidate

        if best is None or best_gain <= 0:
            break

        configs.append(best)
        remaining -= _covered_by(best, factors)

    return configs


def estimate_runs(configs: list[dict[str, str]], axes: dict[str, Axis], seeds: int) -> int:
    """Total batch runs, counting a control sibling for every axis that needs one."""
    total = 0
    for config in configs:
        total += seeds
        # This function guards a budget gate (a downstream runbook refuses to run
        # matrices above a threshold), so over-estimating cost is safe but
        # under-estimating is not. Each control-sibling axis needs its own twin
        # scenario to isolate that axis's effect, so every axis that both
        # requires a sibling and is elevated in this config adds its own block
        # of seed runs — no break, so multiple qualifying axes each count.
        for axis_id, axis in axes.items():
            if axis.requires_control_sibling and config.get(axis_id, "off") != "off":
                total += seeds
    return total


def plan_matrix(
    axes: dict[str, Axis],
    tiers: list[int],
    seeds: int,
    max_configs: int,
) -> dict[str, Any]:
    """Plans the stress matrix, capped at max_configs, reporting any truncation."""
    factors: dict[str, list[str]] = {"tier": [str(t) for t in tiers]}
    for axis_id in sorted(axes):
        factors[axis_id] = sorted(axes[axis_id].levels)

    configs = pairwise(factors)
    dropped = max(0, len(configs) - max_configs)
    kept = configs[:max_configs]

    return {
        "configs": kept,
        "estimatedRuns": estimate_runs(kept, axes, seeds),
        "dropped": dropped,
    }
