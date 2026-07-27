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

## Phase `final` — 2026-07-27T15:50Z

**Run halted after Tier 1** (human decision) — no Tier 2-5 forge activity occurred.

- **Promotes this run**: 0. Coverage-map cell count: unchanged at 20 (24 scenarios) — no new cells added, no regression either.
- **Weight deltas**: none. `platform-swap-underused` (the only tier-1-eligible recipe used) was NOT down-weighted despite 4/4 candidate discards, since the failure cause was an engine defect common to all 8 Tier 1 policies (main + candidates), not a recipe-quality problem. Down-weighting it would have been a false signal against the only viable tier-1 recipe.
- **Stuck families**: none flagged — a single round of discards for one recipe, with a known non-recipe cause, does not meet the ≥5-consecutive-discard stuck threshold, and the cause is understood (not a mystery requiring escalation via this mechanism specifically — though the underlying engine defects ARE escalated to the human via the two filed bug reports).
- **Hard-case pool**: 2 new signatures added to this run's findings (see `post-oracle` section above) — `dead-shooter-fires-next-tick` and `enemy-kill-credited-to-own-side`. These are NOT copied into the committed `corpus/hard-cases/` pool yet, since that pool is meant for scenario-level replay signatures tied to promotable content, and these signatures are engine-defect-level, already fully captured in the two bug reports instead. Recommend the human decide whether to also mirror them into `corpus/hard-cases/` once the defects are fixed and a regression scenario can be promoted.
- **Corpus commits**: none this run — no promotes, no weight changes, nothing to commit under `production/qa/gauntlet/corpus/`.
- **Hindsight retain**: skipped for the whole run (server unreachable at `pre`, confirmed still down at `final` — not re-checked, assumed same outage). On-disk promote-log is the complete record.
- **Graphite**: not submitting a separate forge-only PR — no forge changes were made to `corpus/` this run. The two quarantined bug reports (real gauntlet-orchestrator output, not forge output) ride along in the main qa-gauntlet PR submission.
