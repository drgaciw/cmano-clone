# Stress axes runbook

Tiers own mission/platform complexity. Stress axes own pressure and layer onto
any tier. They are orthogonal: tier 1 with weapons-extremes is a valid, cheap
configuration that tests something tier 5 does not.

## Axes

| Axis | Levels | Proof mode | Control sibling | Notes |
|---|---|---|---|---|
| `weapons` | off / moderate / extreme | `differential-token` (`NO_AMMO`) | **Required** | Count `NO_AMMO` occurrences stressed vs control; proven only on a strict increase. |
| `ew` | off / moderate / extreme | `differential-aggregate` (`Detected`) | **Required** | Compare `Detected` counts summed across seeds. |
| `logistics` | off / moderate / extreme | `config-only` | — | **Not runtime-provable** — `FuelStateProjection` is UI-only (GAP-13). |

### Why `weapons` is differential, not a presence check

`NO_AMMO` occurs **106 times in the unstressed tier-1 baseline** of
`gauntlet-20260727-1455`. A presence assertion ("the token appears in ≥1
fingerprint") is therefore satisfied by a run with the weapons axis set to
`off` — it would report the axis proven while proving nothing at all. Only a
strict increase over a control sibling isolates the axis's own contribution.

The `fingerprint-token` mode remains in the vocabulary, but it is valid
**only** for a token that never occurs in an unstressed baseline run. Confirm
that absence against a real baseline before using it; `NO_AMMO` does not
qualify.

### EW jam targets

The catalog declares EW jammers by `jamStrength` and `activeFromTick` only.
`ScenarioJamResolver` skips any jammer whose `TargetId` does not match the
target under evaluation, so a jammer with no target resolves 0 strength and the
derived scenario is inert. `apply_stress_axes.resolve_jam_target` fills the gap
deterministically: an existing jammer's `targetId` first, then the first
`targetId` in the policy's `detection` block, else `ValueError`. Jamming is only
observable when it suppresses a detection somebody is actually attempting, so a
scenario with no `detection` entries cannot carry the EW axis and is rejected
rather than silently derived.

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

On the shipped catalog, this plan produces `configs=15, estimatedRuns=105,
dropped=0` at `tiers=[1,2,3,4,5], seeds=3, max_configs=24`.

`estimatedRuns` rose from 75 to 105 when `weapons` became
`differential-token`: `estimate_runs` adds one block of seed runs per axis that
both requires a control sibling and is elevated in a config, and `weapons` now
qualifies alongside `ew`. That extra cost buys the control twins without which
the weapons axis cannot be proven at all — it is the price of the fix, not
overhead.

## Budget guard

Report `estimatedRuns` before executing. Two reference costs, which are **not**
the same number and were previously conflated:

| Reference | Scenarios | Runs (3 seeds) |
|---|---|---|
| Default ladder run (`--scenarios-per-tier 4` x 5 tiers) | 20 | **60** |
| Accumulated-corpus regression (tiered scenarios only) | 42 | **126** |

43 gauntlet policies sit on disk, but only the **42** carrying a
`gauntlet.tier` are executed by the per-tier corpus regression — the other 1
has no tier and is skipped. Count tiered scenarios, not files.

At `configs=15, seeds=3` the matrix costs **105 runs**. Stated honestly, that
is **~1.75x a default ladder run** and about **0.83x** a full-corpus
regression: the matrix is *not* cheaper than running the ladder, and must not
be sold as if it were. It is cheaper than the 135-scenario full cross-product
it replaces, and only marginally cheaper than a corpus regression.

Treat the corpus-regression cost (126) as the ceiling. If `estimatedRuns`
exceeds it, lower `max_configs` rather than letting the run expand. Truncation
is always reported via `dropped`; a stress run must never silently narrow its
own coverage.

## Two failure modes worth knowing

1. **A null EW result usually means bad scenario config, not a broken engine.**
   `activeFromTick` set after contacts are already established leaves jamming
   nothing to suppress. Keep the control sibling byte-identical apart from
   `jamStrength` and `id`.
2. **Never assert EW per-seed.** Observed deltas were +4, +2 and 0 for seeds
   42/7/123. Only the aggregate is decisive.
