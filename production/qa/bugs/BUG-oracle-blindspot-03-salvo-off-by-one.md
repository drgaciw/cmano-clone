# BUG-oracle-blindspot-03-salvo-off-by-one

| Field | Value |
|---|---|
| **Found by** | `/qa-gauntlet-calibrate` first run (`production/qa/gauntlet/calibration-2026-07-28/report.md`) |
| **Class** | oracle blind spot (ladder coverage gap) |
| **Severity** | Medium |
| **Status** | OPEN |

## What survived

Mutant `03-salvo-off-by-one` (`PolicyEvaluator` salvo WRA comparison `>` → `>=`)
survived the full anchor ladder subset (tiers 1/3/5 × seeds 42,7,123) **and**
ReplayGolden: no oracle fired, including goldens (fingerprints byte-identical).

## Root cause

The boundary is never approached. `EffectivePolicy.MaxSalvo` defaults to **8**
(`src/ProjectAegis.Sim/Policy/EffectivePolicy.cs:6`), no ladder policy overrides it,
and ladder scenarios fire `salvoSize: 1`. At salvo=1 vs limit=8, `>` and `>=` are
indistinguishable — `WRA_SALVO` occurs **0 times** run-wide (see
`tools/qa-gauntlet/expected-tokens.json` derivation counts).

## Fix direction

Add a ladder (or corpus) scenario that fires at `salvoSize == MaxSalvo` (boundary
value), so the `WRA_SALVO` deny path executes and `WRA_SALVO` can move from
"0 occurrences" to a required token. The 2026-07-27 variability plan's
`gauntlet-t2-strike-salvo-boundary` scenario is exactly this — landing it closes
this blind spot. Re-run `/qa-gauntlet-calibrate` after; mutant 03 must flip to caught.
