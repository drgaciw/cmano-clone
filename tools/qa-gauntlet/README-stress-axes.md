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

On the shipped catalog, this plan produces `configs=15, estimatedRuns=75,
dropped=0` at `tiers=[1,2,3,4,5], seeds=3, max_configs=24` — well under the
budget guard below.

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
