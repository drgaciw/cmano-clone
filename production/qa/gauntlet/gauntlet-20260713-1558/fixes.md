# Gauntlet Fixes — gauntlet-20260713-1558

## Defects

| ID | Class | Tier | Scenario | Resolution |
|---|---|---|---|---|
| ORACLE-T3+ | oracle | 3–5 | all joint-ORBAT scenarios | Recalibrated `gauntlet.expect` after multi-unit catalog ORBAT changed denial/score envelopes vs synthetic u1 baseline. Justification: `oracle-expect-recalibration.json`. |

## No sim-code defects

Stability, determinism, and joint ORBAT path (CATALOG_UNIT + MAGAZINE_SEED + catalog ContactChange) were green without production code changes in this run.

## Prior infrastructure (this branch, earlier commits)

- `GauntletOracleEvaluator` fail-closed post-batch evaluator (P1)
- Joint ORBAT registration with platformId-as-unitId + magazine seed (P2, ffa01a2)
