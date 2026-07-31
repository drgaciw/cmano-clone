# BUG-oracle-blindspot-03-salvo-off-by-one

| Field | Value |
|---|---|
| **Found by** | `/qa-gauntlet-calibrate` first run (`production/qa/gauntlet/calibration-2026-07-28/report.md`) |
| **Class** | oracle blind spot (ladder coverage gap) |
| **Severity** | Medium |
| **Status** | **CLOSED** (2026-07-31) — ladder scenarios at `salvoSize == maxSalvo` |

## Resolution

Added `gauntlet-t2-strike-salvo-boundary` and `gauntlet-t3-strike-salvo-boundary`
with `engage.salvoSize: 2` and `engage.maxSalvo: 2`. Healthy evaluator allows
(`salvo > max` is false); mutant 03 (`>=`) denies. Goldens / victory diverge on
the subset ladder (tier 3 is in saboteur `SUBSET_TIERS`).

## Historical root cause

`MaxSalvo` defaulted to 8 with ladder `salvoSize: 1`, so `>` vs `>=` never diverged.
