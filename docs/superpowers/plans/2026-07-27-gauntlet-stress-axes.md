# Gauntlet Orthogonal Stress Axes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Keep the 5 complexity tiers as-is and add three independent stress axes — EW pressure, logistics pressure, weapons-extremes — that can be layered onto any tier, with scenario count bounded by a pairwise covering array rather than a full cross-product.

**Architecture:** A declarative axis catalog (`stress-axes.yaml`) defines each axis, its levels, the exact policy-JSON mutation each level applies, and — critically — the *proof mode* by which that axis can be mechanically demonstrated. A planner builds a pairwise covering array over `(tier, ew, logistics, weapons)` and emits a bounded work list. An applicator derives scenario policies from tier base scenarios. A verifier checks each axis actually did something, using the proof mode appropriate to it. Everything is data-and-tooling only: policy JSON, YAML, and Python under `tools/qa-gauntlet/`. No engine changes, so no determinism or replay-golden risk.

**Tech Stack:** Python 3 (stdlib + PyYAML + pytest) under `tools/qa-gauntlet/`; gauntlet policy JSON; existing `GauntletOracleEvaluator` CLI as the locked eval.

## Global Constraints

- **Scope is data-only.** Policy JSON, YAML catalogs, and Python tooling. No C# engine changes.
- **Locked eval is untouchable** (`qa-gauntlet-forge` four-box): never edit `GauntletOracleEvaluator`, the Demo batch harness, ReplayGolden fixtures, `.github/workflows/gauntlet-oracle.yml`, `DelegationBridge.cs`, or the Baltic v2 golden hash `17144800277401907079`.
- **Tier tick budgets are fixed:** T1=6, T2=10, T3=16, T4=24, T5=40. Never calibrate a tier from CI's 10-tick smoke.
- **Seeds are `42,7,123`** unless a step says otherwise.
- **Never invent `gauntlet.expect`** — regenerate per `tools/qa-gauntlet/README-expect-regen.md` at tier-tick boundaries after a first successful batch.
- **Dual envelopes:** scenarios in the CI fixture list need `gauntlet.expectCi` (10-tick smoke) *and* `gauntlet.expect` (ladder authority). Do not widen `expect` to make CI pass.
- Catalog IDs only from the tier roster / catalog DB.
- Python tooling follows the existing convention: module under `tools/qa-gauntlet/`, pytest tests as `tools/qa-gauntlet/test_<module>.py`, importing via `sys.path.insert` as `test_forge_scorecard.py` does.

## The honesty constraint (read before Task 1)

The three axes are **not** equally provable, and the plan must not pretend otherwise. This project has a documented defect class — VOCABULARY-ONLY — where a capability exists as an authoring label with no simulation behaviour behind it (`missionCode`, Tanker/AEW support roles, EMCON prose at `gauntlet.emcon`). An axis that varies config while proving nothing is exactly that defect.

Established by evidence during run `gauntlet-20260727-1455`:

| Axis | Policy surface | Can it be proven at runtime? |
|---|---|---|
| **weapons-extremes** | `engage.defaultMagazineRounds`, `salvoSize`, `pkBase`, `pkKill`, `rangeMeters`, `envelopeMin/MaxMeters` | **Yes** — magazine exhaustion emits a `NO_AMMO` fingerprint token. |
| **EW pressure** | `jammers[].jamStrength`, `jammers[].activeFromTick` | **Yes, but only differentially.** Kills, score, missiles and denials are all *unchanged* by jamming, because engagement is driven by explicit engage config and mission triggers rather than gated on detection success. The only valid signal is `ContactChange`/`Detected` counts against a control sibling, **aggregated across seeds** — per-seed deltas were +4, +2 and 0 for seeds 42/7/123, so a per-seed assertion reports working jamming as broken. |
| **logistics pressure** | `logistics.{jokerSimSeconds,bingoSimSeconds,fuelCapacityKg,burnRateKgPerSecond,jokerFuelFraction}` | **No.** `FuelStateProjection` is UI-only: no engagement gating, no fingerprint emission. Recorded as backlog **GAP-13**. |

Therefore the logistics axis ships as `proof: config-only` — it varies configuration and asserts schema validity, and the verifier **refuses to report it as proven**. It becomes runtime-provable only when GAP-13 is implemented. Do not "fix" this by inventing a fuel assertion.

## Cost model (the reason for pairwise)

Factors: `tier` (5 levels) × `ew` (3) × `logistics` (3) × `weapons` (3).

| Design | Configs | Runs (×3 seeds, +EW control twins) |
|---|---|---|
| Full factorial | 5 × 27 = **135** | ~500+ |
| **Pairwise (2-way) covering array** | **~15–18** | **~75–85** |
| Base ladder today (39 scenarios) | 39 | 117 |

Pairwise guarantees every *pair* of factor levels appears together at least once; its size is bounded below by the largest pair product (5 × 3 = 15), not by the product of all factors. Empirically most defects are triggered by one or two interacting factors, which is what makes this the right trade. Task 3 enforces a hard config cap so the run can never silently explode.

---

### Task 1: Stress axis catalog

Declares the three axes, their levels, the exact policy mutation per level, and the proof mode. This file is the single source of truth; every later task reads it rather than hardcoding axis knowledge.

**Files:**
- Create: `production/qa/gauntlet/corpus/stress-axes.yaml`
- Create: `tools/qa-gauntlet/stress_axes.py`
- Test: `tools/qa-gauntlet/test_stress_axes.py`

**Interfaces:**
- Consumes: nothing (first task).
- Produces:
  - `load_axes(path: Path) -> dict[str, Axis]`
  - `@dataclass Axis: id: str; proof: str; levels: dict[str, dict]`
  - `PROOF_MODES = {"fingerprint-token", "differential-aggregate", "config-only"}`
  - `validate_axes(axes: dict[str, Axis]) -> list[str]` returning error strings (empty = valid)

- [ ] **Step 1: Write the failing test**

Create `tools/qa-gauntlet/test_stress_axes.py`:

```python
#!/usr/bin/env python3
"""Unit tests for tools/qa-gauntlet/stress_axes.py (stress-axis catalog)."""

from __future__ import annotations

import sys
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "qa-gauntlet"))

from stress_axes import (  # noqa: E402
    PROOF_MODES,
    Axis,
    load_axes,
    validate_axes,
)

CATALOG = ROOT / "production" / "qa" / "gauntlet" / "corpus" / "stress-axes.yaml"


def test_catalog_file_exists():
    assert CATALOG.exists(), f"missing catalog: {CATALOG}"


def test_catalog_declares_the_three_axes():
    axes = load_axes(CATALOG)
    assert set(axes) == {"ew", "logistics", "weapons"}


def test_every_axis_has_an_off_level():
    axes = load_axes(CATALOG)
    for name, axis in axes.items():
        assert "off" in axis.levels, f"{name} has no 'off' level"


def test_proof_modes_are_recognised_and_correctly_assigned():
    axes = load_axes(CATALOG)
    for axis in axes.values():
        assert axis.proof in PROOF_MODES

    assert axes["weapons"].proof == "fingerprint-token"
    assert axes["ew"].proof == "differential-aggregate"
    # GAP-13: FuelStateProjection is UI-only, so logistics cannot be runtime-proven.
    assert axes["logistics"].proof == "config-only"


def test_validate_accepts_the_shipped_catalog():
    assert validate_axes(load_axes(CATALOG)) == []


def test_validate_rejects_unknown_proof_mode():
    bad = {"ew": Axis(id="ew", proof="vibes", levels={"off": {}})}
    errors = validate_axes(bad)
    assert any("proof" in e for e in errors)


def test_validate_rejects_axis_without_off_level():
    bad = {"ew": Axis(id="ew", proof="config-only", levels={"extreme": {}})}
    errors = validate_axes(bad)
    assert any("off" in e for e in errors)
```

- [ ] **Step 2: Run test to verify it fails**

Run: `python3 -m pytest tools/qa-gauntlet/test_stress_axes.py -v`
Expected: FAIL — `ModuleNotFoundError: No module named 'stress_axes'`.

- [ ] **Step 3: Write the catalog**

Create `production/qa/gauntlet/corpus/stress-axes.yaml`:

```yaml
# Orthogonal stress axes, layerable onto any complexity tier.
# Tiers own mission/platform complexity; axes own pressure. They compose.
#
# proof modes:
#   fingerprint-token      — assert a discrete token appears in the run fingerprint
#   differential-aggregate — compare against a control sibling, summed across seeds
#   config-only            — configuration varies but the sim cannot demonstrate it;
#                            MUST NOT be reported as proven
version: 1
updated: 2026-07-27

axes:
  - id: weapons
    proof: fingerprint-token
    # Magazine exhaustion emits NO_AMMO; verified live in tier 3 (28 occurrences).
    signal: "NO_AMMO"
    levels:
      off: {}
      moderate:
        engage.defaultMagazineRounds: 2
        engage.salvoSize: 2
      extreme:
        engage.defaultMagazineRounds: 1
        engage.salvoSize: 4
        engage.pkKill: 0.1

  - id: ew
    proof: differential-aggregate
    # Jamming changes ContactChange/Detected counts only. Kills/score/missiles/denials
    # are identical because engagement is driven by explicit engage config, not by
    # detection success. Requires a control sibling and seed aggregation.
    signal: "Detected"
    requires_control_sibling: true
    levels:
      off: {}
      moderate:
        jammers:
          - jamStrength: 0.5
            activeFromTick: 0
      extreme:
        jammers:
          - jamStrength: 0.9
            activeFromTick: 0

  - id: logistics
    proof: config-only
    # GAP-13: FuelStateProjection is UI-only — no engagement gating, no fingerprint
    # emission. Config varies and schema is validated; runtime pressure is NOT proven.
    gap: "GAP-13"
    levels:
      off: {}
      moderate:
        logistics.fuelCapacityKg: 4000
        logistics.burnRateKgPerSecond: 8
      extreme:
        logistics.fuelCapacityKg: 2000
        logistics.burnRateKgPerSecond: 14
        logistics.jokerFuelFraction: 0.6
```

- [ ] **Step 4: Write the loader**

Create `tools/qa-gauntlet/stress_axes.py`:

```python
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

PROOF_MODES = {"fingerprint-token", "differential-aggregate", "config-only"}


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
    return errors
```

- [ ] **Step 5: Run test to verify it passes**

Run: `python3 -m pytest tools/qa-gauntlet/test_stress_axes.py -v`
Expected: PASS — 7 tests.

- [ ] **Step 6: Commit**

```bash
git add production/qa/gauntlet/corpus/stress-axes.yaml \
        tools/qa-gauntlet/stress_axes.py \
        tools/qa-gauntlet/test_stress_axes.py
git commit -m "feat(qa-gauntlet): declare orthogonal stress axes with proof modes

Task 1 of docs/superpowers/plans/2026-07-27-gauntlet-stress-axes.md"
```

---

### Task 2: Axis applicator

Derives a stressed policy from a base tier scenario by applying axis levels. Deterministic: same inputs always produce the same output policy and id.

**Files:**
- Create: `tools/qa-gauntlet/apply_stress_axes.py`
- Test: `tools/qa-gauntlet/test_apply_stress_axes.py`

**Interfaces:**
- Consumes: `Axis`, `load_axes` (Task 1).
- Produces:
  - `apply_level(policy: dict, axis: Axis, level: str) -> dict` — returns a new policy, does not mutate input
  - `derive_scenario_id(base_id: str, selection: dict[str, str]) -> str`
  - `apply_selection(policy: dict, axes: dict[str, Axis], selection: dict[str, str]) -> dict`

- [ ] **Step 1: Write the failing test**

Create `tools/qa-gauntlet/test_apply_stress_axes.py`:

```python
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
```

- [ ] **Step 2: Run test to verify it fails**

Run: `python3 -m pytest tools/qa-gauntlet/test_apply_stress_axes.py -v`
Expected: FAIL — `ModuleNotFoundError: No module named 'apply_stress_axes'`.

- [ ] **Step 3: Write the implementation**

Create `tools/qa-gauntlet/apply_stress_axes.py`:

```python
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
```

- [ ] **Step 4: Run test to verify it passes**

Run: `python3 -m pytest tools/qa-gauntlet/test_apply_stress_axes.py -v`
Expected: PASS — 8 tests.

- [ ] **Step 5: Commit**

```bash
git add tools/qa-gauntlet/apply_stress_axes.py \
        tools/qa-gauntlet/test_apply_stress_axes.py
git commit -m "feat(qa-gauntlet): apply stress-axis levels to base policies

Task 2 of docs/superpowers/plans/2026-07-27-gauntlet-stress-axes.md"
```

---

### Task 3: Pairwise covering-array planner with hard budget cap

Turns four factors into a bounded work list. This is the task that answers "scenario count and run time grow fast".

**Files:**
- Create: `tools/qa-gauntlet/plan_stress_matrix.py`
- Test: `tools/qa-gauntlet/test_plan_stress_matrix.py`

**Interfaces:**
- Consumes: `Axis`, `load_axes` (Task 1); `derive_scenario_id` (Task 2).
- Produces:
  - `pairwise(factors: dict[str, list[str]]) -> list[dict[str, str]]`
  - `estimate_runs(configs, axes, seeds) -> int`
  - `plan_matrix(axes, tiers, seeds, max_configs) -> dict` with keys `configs`, `estimatedRuns`, `dropped`

- [ ] **Step 1: Write the failing test**

Create `tools/qa-gauntlet/test_plan_stress_matrix.py`:

```python
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
```

- [ ] **Step 2: Run test to verify it fails**

Run: `python3 -m pytest tools/qa-gauntlet/test_plan_stress_matrix.py -v`
Expected: FAIL — `ModuleNotFoundError: No module named 'plan_stress_matrix'`.

- [ ] **Step 3: Write the implementation**

Create `tools/qa-gauntlet/plan_stress_matrix.py`:

```python
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
        for axis_id, axis in axes.items():
            if axis.requires_control_sibling and config.get(axis_id, "off") != "off":
                total += seeds
                break
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
```

- [ ] **Step 4: Run test to verify it passes**

Run: `python3 -m pytest tools/qa-gauntlet/test_plan_stress_matrix.py -v`
Expected: PASS — 6 tests.

- [ ] **Step 5: Print the real matrix and record its size**

```bash
python3 - <<'PY'
import sys, json
from pathlib import Path
ROOT = Path.cwd()
sys.path.insert(0, str(ROOT / "tools" / "qa-gauntlet"))
from stress_axes import load_axes
from plan_stress_matrix import plan_matrix
axes = load_axes(ROOT / "production/qa/gauntlet/corpus/stress-axes.yaml")
plan = plan_matrix(axes, tiers=[1,2,3,4,5], seeds=3, max_configs=24)
print("configs:", len(plan["configs"]), "estimatedRuns:", plan["estimatedRuns"], "dropped:", plan["dropped"])
PY
```

Expected: roughly 15–18 configs and 75–85 estimated runs, `dropped: 0`. If `estimatedRuns` exceeds 120 (the base ladder's own cost), lower `max_configs` before proceeding.

- [ ] **Step 6: Commit**

```bash
git add tools/qa-gauntlet/plan_stress_matrix.py \
        tools/qa-gauntlet/test_plan_stress_matrix.py
git commit -m "feat(qa-gauntlet): bound stress matrix with pairwise covering array

Task 3 of docs/superpowers/plans/2026-07-27-gauntlet-stress-axes.md"
```

---

### Task 4: Per-axis proof verifier

Checks each axis actually did something, by the mode it declared. This is what stops a stress axis becoming another VOCABULARY-ONLY capability.

**Files:**
- Create: `tools/qa-gauntlet/verify_stress_axes.py`
- Test: `tools/qa-gauntlet/test_verify_stress_axes.py`

**Interfaces:**
- Consumes: `Axis`, `load_axes` (Task 1).
- Produces:
  - `verify_fingerprint_token(fingerprints: list[str], token: str) -> tuple[bool, str]`
  - `verify_differential_aggregate(stressed: list[int], control: list[int]) -> tuple[bool, str]`
  - `verify_axis(axis: Axis, evidence: dict) -> dict` with keys `axis`, `proven`, `mode`, `detail`

- [ ] **Step 1: Write the failing test**

Create `tools/qa-gauntlet/test_verify_stress_axes.py`:

```python
#!/usr/bin/env python3
"""Unit tests for tools/qa-gauntlet/verify_stress_axes.py."""

from __future__ import annotations

import sys
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "qa-gauntlet"))

from stress_axes import load_axes  # noqa: E402
from verify_stress_axes import (  # noqa: E402
    verify_axis,
    verify_differential_aggregate,
    verify_fingerprint_token,
)

CATALOG = ROOT / "production" / "qa" / "gauntlet" / "corpus" / "stress-axes.yaml"


def test_fingerprint_token_present_is_proven():
    proven, detail = verify_fingerprint_token(["...NO_AMMO...", "..."], "NO_AMMO")
    assert proven is True
    assert "NO_AMMO" in detail


def test_fingerprint_token_absent_is_not_proven():
    proven, _ = verify_fingerprint_token(["...", "..."], "NO_AMMO")
    assert proven is False


def test_differential_aggregate_uses_totals_not_per_seed():
    # Per-seed deltas +4, +2, 0 — a per-seed rule would fail on the third seed,
    # wrongly reporting working jamming as broken. Aggregate 22 vs 28 is decisive.
    proven, detail = verify_differential_aggregate(stressed=[6, 8, 8], control=[10, 10, 8])
    assert proven is True
    assert "22" in detail and "28" in detail


def test_differential_aggregate_rejects_a_null_result():
    proven, _ = verify_differential_aggregate(stressed=[10, 10, 10], control=[10, 10, 10])
    assert proven is False


def test_differential_aggregate_rejects_wrong_direction():
    proven, _ = verify_differential_aggregate(stressed=[12, 12, 12], control=[10, 10, 10])
    assert proven is False


def test_verify_axis_weapons_uses_fingerprint_mode():
    axes = load_axes(CATALOG)
    result = verify_axis(axes["weapons"], {"fingerprints": ["x NO_AMMO y"]})

    assert result["mode"] == "fingerprint-token"
    assert result["proven"] is True


def test_verify_axis_ew_uses_differential_mode():
    axes = load_axes(CATALOG)
    result = verify_axis(axes["ew"], {"stressed": [6, 8, 8], "control": [10, 10, 8]})

    assert result["mode"] == "differential-aggregate"
    assert result["proven"] is True


def test_verify_axis_logistics_is_never_reported_as_proven():
    axes = load_axes(CATALOG)
    result = verify_axis(axes["logistics"], {"fingerprints": ["anything"]})

    assert result["mode"] == "config-only"
    assert result["proven"] is False
    assert "GAP-13" in result["detail"]
```

- [ ] **Step 2: Run test to verify it fails**

Run: `python3 -m pytest tools/qa-gauntlet/test_verify_stress_axes.py -v`
Expected: FAIL — `ModuleNotFoundError: No module named 'verify_stress_axes'`.

- [ ] **Step 3: Write the implementation**

Create `tools/qa-gauntlet/verify_stress_axes.py`:

```python
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
```

- [ ] **Step 4: Run test to verify it passes**

Run: `python3 -m pytest tools/qa-gauntlet/test_verify_stress_axes.py -v`
Expected: PASS — 9 tests.

- [ ] **Step 5: Commit**

```bash
git add tools/qa-gauntlet/verify_stress_axes.py \
        tools/qa-gauntlet/test_verify_stress_axes.py
git commit -m "feat(qa-gauntlet): verify stress axes by declared proof mode

Task 4 of docs/superpowers/plans/2026-07-27-gauntlet-stress-axes.md"
```

---

### Task 5: Coverage-map axis cells

Extends the coverage map so axis pressure is tracked alongside tier complexity, keeping the existing cell key intact.

**Files:**
- Modify: `tools/qa-gauntlet/forge_scorecard.py` (extend `infer_cell`)
- Test: `tools/qa-gauntlet/test_forge_scorecard.py` (add cases)

**Interfaces:**
- Consumes: `AXIS_ORDER` (Task 2).
- Produces: `infer_cell` output gains a `stressAxes` key of the form `ew:off|logistics:off|weapons:extreme`; the existing `key` is unchanged so historical cells stay comparable.

- [ ] **Step 1: Write the failing test**

Append to `tools/qa-gauntlet/test_forge_scorecard.py`:

```python
def test_infer_cell_reports_stress_axes_from_policy():
    policy = {
        "id": "gauntlet-t3-escort-strike-weapons-extreme",
        "engage": {"defaultMagazineRounds": 1, "salvoSize": 4},
        "gauntlet": {"tier": 3, "intent": "escort + strike under weapons-extremes"},
    }

    cell = infer_cell(policy, policy["id"])

    assert cell["stressAxes"] == "ew:off|logistics:off|weapons:extreme"


def test_infer_cell_detects_ew_axis_from_jammers_block():
    policy = {
        "id": "gauntlet-t3-s5",
        "jammers": [{"targetId": "x", "jamStrength": 0.9, "activeFromTick": 0}],
        "gauntlet": {"tier": 3, "intent": "escort"},
    }

    cell = infer_cell(policy, policy["id"])

    assert cell["stressAxes"].startswith("ew:extreme")


def test_infer_cell_stress_axes_default_to_off():
    policy = {"id": "gauntlet-t1-patrol-a", "gauntlet": {"tier": 1, "intent": "patrol"}}

    cell = infer_cell(policy, policy["id"])

    assert cell["stressAxes"] == "ew:off|logistics:off|weapons:off"


def test_infer_cell_key_is_unchanged_by_the_axis_extension():
    policy = {"id": "gauntlet-t1-patrol-a", "gauntlet": {"tier": 1, "intent": "patrol"}}

    cell = infer_cell(policy, policy["id"])

    # The historical 5-part key must stay intact so existing cells remain comparable.
    assert cell["key"].count("|") == 4
    assert "stressAxes" not in cell["key"]
```

- [ ] **Step 2: Run test to verify it fails**

Run: `python3 -m pytest tools/qa-gauntlet/test_forge_scorecard.py -v -k stress`
Expected: FAIL — `KeyError: 'stressAxes'`.

- [ ] **Step 3: Extend `infer_cell`**

In `tools/qa-gauntlet/forge_scorecard.py`, add this helper above `infer_cell`:

```python
def _infer_stress_axes(policy: dict[str, Any]) -> str:
    """Derives the stress-axis signature from policy content.

    Reads the policy itself rather than the scenario id, so a hand-authored
    scenario that applies pressure without the derived naming is still counted.
    """
    engage = policy.get("engage") or {}
    rounds = engage.get("defaultMagazineRounds")
    salvo = engage.get("salvoSize")
    if rounds is not None and rounds <= 1:
        weapons = "extreme"
    elif (rounds is not None and rounds <= 2) or (salvo is not None and salvo >= 2):
        weapons = "moderate"
    else:
        weapons = "off"

    jammers = policy.get("jammers") or []
    strength = max((j.get("jamStrength", 0) for j in jammers), default=0)
    if strength >= 0.8:
        ew = "extreme"
    elif strength > 0:
        ew = "moderate"
    else:
        ew = "off"

    logistics_block = policy.get("logistics") or {}
    burn = logistics_block.get("burnRateKgPerSecond", 0)
    if burn >= 14:
        logistics = "extreme"
    elif burn > 0:
        logistics = "moderate"
    else:
        logistics = "off"

    return f"ew:{ew}|logistics:{logistics}|weapons:{weapons}"
```

Then, immediately before `infer_cell` returns its cell dict, add the key:

```python
    cell["stressAxes"] = _infer_stress_axes(policy)
```

Do **not** add `stressAxes` to the `key` string — the 5-part key must stay stable so historical cells remain comparable.

- [ ] **Step 4: Run test to verify it passes**

Run: `python3 -m pytest tools/qa-gauntlet/test_forge_scorecard.py -v`
Expected: PASS — 13 existing tests plus 4 new = 17.

- [ ] **Step 5: Commit**

```bash
git add tools/qa-gauntlet/forge_scorecard.py tools/qa-gauntlet/test_forge_scorecard.py
git commit -m "feat(qa-gauntlet): track stress-axis pressure in coverage cells

Task 5 of docs/superpowers/plans/2026-07-27-gauntlet-stress-axes.md"
```

---

### Task 6: Ladder wiring, runbook and budget guard

Documents how axes are run and wires the budget guard so a stress run cannot silently balloon.

**Files:**
- Create: `tools/qa-gauntlet/README-stress-axes.md`
- Modify: `.claude/skills/qa-gauntlet/SKILL.md` (add a stress-axes section after the complexity ladder)

**Interfaces:**
- Consumes: everything from Tasks 1–5.
- Produces: no new code symbols; documentation and skill wiring only.

- [ ] **Step 1: Write the runbook**

Create `tools/qa-gauntlet/README-stress-axes.md`:

```markdown
# Stress axes runbook

Tiers own mission/platform complexity. Stress axes own pressure and layer onto
any tier. They are orthogonal: tier 1 with weapons-extremes is a valid, cheap
configuration that tests something tier 5 does not.

## Axes

| Axis | Levels | Proof mode | Notes |
|---|---|---|---|
| `weapons` | off / moderate / extreme | `fingerprint-token` (`NO_AMMO`) | Directly observable. |
| `ew` | off / moderate / extreme | `differential-aggregate` | Needs a control sibling; compare `Detected` counts summed across seeds. |
| `logistics` | off / moderate / extreme | `config-only` | **Not runtime-provable** — `FuelStateProjection` is UI-only (GAP-13). |

## Running

```bash
# 1. Plan the bounded matrix (pairwise, capped)
python3 - <<'PY'
import sys, json
from pathlib import Path
sys.path.insert(0, "tools/qa-gauntlet")
from stress_axes import load_axes
from plan_stress_matrix import plan_matrix
axes = load_axes(Path("production/qa/gauntlet/corpus/stress-axes.yaml"))
plan = plan_matrix(axes, tiers=[1,2,3,4,5], seeds=3, max_configs=24)
print(json.dumps(plan, indent=2))
PY

# 2. Derive policies, batch at TIER ticks (T1=6 T2=10 T3=16 T4=24 T5=40),
#    then evaluate with the shipped CLI. Never calibrate from the 10-tick CI smoke.
```

## Budget guard

Report `estimatedRuns` before executing. If it exceeds the base ladder's own
cost (39 scenarios x 3 seeds = 117 runs), lower `max_configs` rather than
letting the run expand. Truncation is always reported via `dropped`; a stress
run must never silently narrow its own coverage.

## Two failure modes worth knowing

1. **A null EW result usually means bad scenario config, not a broken engine.**
   `activeFromTick` set after contacts are already established leaves jamming
   nothing to suppress. Keep the control sibling byte-identical apart from
   `jamStrength` and `id`.
2. **Never assert EW per-seed.** Observed deltas were +4, +2 and 0 for seeds
   42/7/123. Only the aggregate is decisive.
```

- [ ] **Step 2: Wire into the skill**

In `.claude/skills/qa-gauntlet/SKILL.md`, immediately after the complexity-ladder matrix table, insert:

```markdown
## Orthogonal stress axes

The 5 tiers escalate mission/platform complexity. Three **independent** axes
layer pressure onto any tier and are selected by a bounded pairwise matrix, not
a cross-product: `ew`, `logistics`, `weapons` (see
[`tools/qa-gauntlet/README-stress-axes.md`](../../../tools/qa-gauntlet/README-stress-axes.md)).

- Plan with `plan_stress_matrix.plan_matrix(...)`; report `estimatedRuns` and
  `dropped` before executing. A run that exceeds the base ladder cost (117 runs)
  must lower `max_configs`, not expand.
- Verify with `verify_stress_axes.verify_axis(...)` using each axis's declared
  proof mode. `logistics` is `config-only` (GAP-13) and is **never** reported as
  proven — do not add a fuel assertion to make it look green.
- An EW axis level requires a control sibling identical apart from `jamStrength`
  and `id`, compared on aggregate `Detected` counts across all seeds.
```

- [ ] **Step 3: Run the whole tooling suite**

Run: `python3 -m pytest tools/qa-gauntlet/ -v`
Expected: PASS — 17 (scorecard) + 7 + 8 + 6 + 9 = 47 tests, 0 failed.

- [ ] **Step 4: Confirm no scenario or engine drift**

```bash
git status --short
```

Expected: only `tools/qa-gauntlet/`, `production/qa/gauntlet/corpus/stress-axes.yaml`, and `.claude/skills/qa-gauntlet/SKILL.md`. No changes under `src/`, `data/scenarios/`, or `tests/regression/`.

- [ ] **Step 5: Commit**

```bash
git add tools/qa-gauntlet/README-stress-axes.md .claude/skills/qa-gauntlet/SKILL.md
git commit -m "docs(qa-gauntlet): stress-axes runbook and ladder wiring

Task 6 of docs/superpowers/plans/2026-07-27-gauntlet-stress-axes.md"
```

---

## Self-Review

**Source coverage.** The source note asks for three things and warns about one. Tiers kept as-is for mission/platform complexity: no tier definition is modified anywhere in this plan. Independent axes layerable onto any tier: Tasks 1–2, with `tier` treated as just another factor in Task 3. More combinatorial coverage: Task 5 extends the coverage map with an axis signature while leaving the historical key intact. Scenario count and run time growing fast: Task 3 is dedicated to it — pairwise instead of factorial, an `estimatedRuns` figure that counts EW control twins, a hard `max_configs` cap, and mandatory reporting of `dropped`.

**Placeholder scan.** No TBD/TODO markers. Every code step carries complete, runnable code. No "similar to Task N" cross-references.

**Type consistency.** `Axis`, `load_axes`, `validate_axes`, `PROOF_MODES` (Task 1); `apply_level`, `apply_selection`, `derive_scenario_id`, `AXIS_ORDER` (Task 2); `pairwise`, `estimate_runs`, `plan_matrix` (Task 3); `verify_fingerprint_token`, `verify_differential_aggregate`, `verify_axis` (Task 4) are used with identical names and signatures wherever they cross task boundaries. Axis ids are `ew` / `logistics` / `weapons` throughout, in `AXIS_ORDER` sequence.

**Known limitations, stated rather than hidden.**

1. **The logistics axis proves nothing at runtime** and is reported unproven by construction. It earns its place by exercising schema validity and by being ready the moment GAP-13 lands — but a reader should not mistake it for coverage.
2. **`pairwise` is a greedy covering array, not a minimal one.** It is deterministic and correct (Task 3 Step 1 asserts full pair coverage) but may emit one or two more configs than an optimal solver would. That trade buys reproducibility and zero dependencies.
3. **`_infer_stress_axes` classifies by threshold**, so a hand-authored scenario sitting near a boundary may land in an adjacent bucket. Thresholds are chosen to match the catalog's own level values; if the catalog levels change, these thresholds must change with them.
