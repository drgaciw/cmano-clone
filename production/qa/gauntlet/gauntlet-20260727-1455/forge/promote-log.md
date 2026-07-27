# Forge Promote Log — gauntlet-20260727-1455

## Phase `pre` (tier 1) — 2026-07-27T14:55Z

- Hindsight recall: **SKIPPED** — bank server unreachable (`curl http://localhost:8888` → HTTP 000). Proceeding with on-disk corpus only per forge contract.
- Corpus loaded: `coverage-map.json` (20 cells / 24 scenarios), `recipes/recipe-weights.json` (17 recipes), `hard-cases/` (empty — no prior failure signatures yet), `index.yaml` (24 promoted policies).
- Tier 1 ranked recipes: `platform-swap-underused` (weight 1.2, tierMin 1) — the only live tier-1-eligible recipe (`bootstrap-seed` is provenance-only, not selectable for new candidates).
- No hard-case replays injected this tier (pool empty).
- Plan written: `forge/mid-tier-plan.yaml`.

## Phase `post-oracle` (tier 1) — 2026-07-27T15:45Z

- Batch executed for all 4 candidates (`gauntlet-forge-20260727-1455-t1-c1..c4`) alongside the 4 main scenarios, seeds 42/7/123, 6 ticks.
- **Hard gates: FAIL** for all 4 candidates — `gauntlet_oracle_eval` reported score exceeding `maxScore` for every candidate, same root cause as the main scenarios (BUG-engagement-resolver-shooter-liveness + BUG-losses-scoring-side-unaware, both QUARANTINED-CRITICAL — see production/qa/bugs/). This is NOT a candidate-design/novelty failure; it's the underlying engine defect surfacing identically across all 8 Tier 1 policies (main + candidates).
- **Disposition**: DISCARD all 4 candidates. Not promoted to `data/scenarios/`. No recipe down-weighting applied — `platform-swap-underused` (the only recipe used) is not at fault; down-weighting it would be a false signal. Retained as `FAILED:` with reason `engine-defect-blocked`, not `recipe-quality`, so future runs don't unfairly penalize this recipe once the underlying bugs are fixed.
- Ephemeral candidate files remain gitignored under `forge/candidates/` (never committed); temporary copies placed in `data/scenarios/` for batch execution were removed after scoring.

Hindsight retain (server unreachable this run, same as `pre` phase — logged here instead, on-disk only):
```
[OUTCOME: discard] scenario=gauntlet-forge-20260727-1455-t1-c1 recipes=platform-swap-underused reason=engine-defect-blocked FAILED: oracle score>max, root cause BUG-engagement-resolver-shooter-liveness + BUG-losses-scoring-side-unaware, not a candidate quality issue
[OUTCOME: discard] scenario=gauntlet-forge-20260727-1455-t1-c2 recipes=platform-swap-underused reason=engine-defect-blocked FAILED: same root cause
[OUTCOME: discard] scenario=gauntlet-forge-20260727-1455-t1-c3 recipes=platform-swap-underused reason=engine-defect-blocked FAILED: same root cause
[OUTCOME: discard] scenario=gauntlet-forge-20260727-1455-t1-c4 recipes=platform-swap-underused reason=engine-defect-blocked FAILED: same root cause
[OUTCOME: hard-case] defect=BUG-engagement-resolver-shooter-liveness scenario=gauntlet-20260727-1455-t1-s1 signature=dead-shooter-fires-next-tick
[OUTCOME: hard-case] defect=BUG-losses-scoring-side-unaware scenario=gauntlet-20260727-1455-t1-s1 signature=enemy-kill-credited-to-own-side
```
