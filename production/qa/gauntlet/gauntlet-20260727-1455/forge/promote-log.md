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

---

## Phase `pre` (tier 2) — 2026-07-27T16:20Z — RUN RESUMED

Run resumed at tier 2 after both tier-1 CRITICAL defects were fixed (PR #361) and the
corpus re-baselined to 24/24. Oracle output is now trustworthy, which is what makes
tier 2+ worth running at all — pre-fix, every tier would have re-surfaced the same two
engine defects rather than new signal.

- Hindsight recall: **SKIPPED** again — server still unreachable. On-disk corpus only.
- Tier-2-eligible recipes grew from 1 to **5** (`tierMin <= 2`): `hard-case-replay` (1.3),
  `platform-swap-underused` (1.2), `orbat-asymmetric-ratio` (1.0),
  `mission-combo-escort-strike` (1.0), `geometry-detection-lane-shift` (1.0).
- `hard-case-replay` is newly **actionable**: the two tier-1 signatures
  (`dead-shooter-fires-next-tick`, `enemy-kill-credited-to-own-side`) are now fixed, so
  reproducing those conditions is regression coverage rather than re-finding a known break.
  Both injected into this tier's plan.
- New coverage this tier: **air domain** (first time this run), **passive-only EMCON one
  side**, **asymmetric ROE**, and **a scripted timed event** — tier 1 had none of these.
- Plan written: `forge/mid-tier-plan.yaml` (tier-1 plan superseded in place; its summary
  is preserved in the tier-1 `pre` entry above).

## Phase `post-oracle` + `e` (tier 2) — 2026-07-27T16:40Z

**4 promotes, 0 discards** — the first promotions of this run.

| candidate | recipe | cell | novelty | oracle |
|---|---|---|---|---|
| `…-t2-c1` | `hard-case-replay` | `emcon\|air,surface\|WeaponsFree/WeaponsFree\|emcon-phases\|none` | 4.5 | PASS |
| `…-t2-c2` | `hard-case-replay` | (same cell) | 4.0 | PASS |
| `…-t2-c3` | `orbat-asymmetric-ratio` | `unknown\|air,surface\|WeaponsTight/WeaponsFree\|unrestricted\|none` | 4.5 | PASS |
| `…-t2-c4` | `mission-combo-escort-strike` | `strike\|air,surface\|WeaponsFree/WeaponsTight\|emcon-phases\|event-chain` | 4.0 | PASS |

**c1 is the standout**: purpose-built to reproduce the `dead-shooter-fires-next-tick` signature, it now emits **9 `SHOOTER_DESTROYED` denials per seed** — live proof the Bug-1 fix holds. It is now a permanent regression test for that defect rather than a record of it.

### Tooling defect found and worked around

The first three scorecard runs reported `promote=0 discard=4` with `oracleKnown=false` on every candidate. Cause: `forge_scorecard.py:170` derives the scenario id from the **filename** (`sid = policy_path.name.replace(".policy.json","")`) but `oracle-eval.json` is keyed by the policy's **`id` field**. The forge skill's own convention (`candidate-N.policy.json` + `gauntlet-forge-…-cN` id) guarantees a mismatch, so **following the documented convention silently blocks all promotion**. Renaming the candidate files to `<policy id>.policy.json` — changing nothing else — flipped the result to `promote=4 discard=0`.

Filed as `production/qa/bugs/BUG-forge-scorecard-filename-vs-policy-id.md`. Recipe weights were **not** down-weighted for those spurious discards.

### Weight + coverage deltas

- `hard-case-replay` 1.3 → **1.7192** (2 promotes)
- `orbat-asymmetric-ratio` 1.0 → **1.15**
- `mission-combo-escort-strike` 1.0 → **1.15**
- coverage-map: **20 → 23 cells**, scenarioCount 24 → 28
- `corpus/index.yaml`: 4 entries appended

### Corrected finding (supersedes a claim in the tier-1 AAR)

Tier 1 documentation asserted that denial counts rose corpus-wide "because dead shooters' blocked attempts are now recorded as denials instead of launches". **That mechanism is wrong.** `SHOOTER_DESTROYED` is an `EngagementAbortReason` written to the **OrderLog**; `DecisionLog.PolicyDenials` is appended only in the pre-resolver guard path (`SimulationSession.cs:170`, comms/ROE gates). Candidate c1 proves it: **9 `SHOOTER_DESTROYED`, 0 denials**. The real cause of the corpus denial rise is second-order — with dead shooters no longer killing, more units survive more ticks and generate more ROE-gate denials. Corrected in the bug report and AAR.

### Stuck families

None. No recipe family has consecutive discards this run (the 4 apparent discards were the tooling defect above, and have been zeroed rather than counted).

---

## Phase E (tier 3 close-out) + Phase `pre` (tier 4) — 2026-07-28T02:15Z — standalone invocation

Invoked directly (not chained from a live `/qa-gauntlet` run). Reconstructed run
state from disk: Tier 3's **main** scenarios had already executed and passed
oracle (`allPassed: true`, 6/6), but no forge candidates were drafted for
Tier 3 — `mid-tier-plan.yaml` and `scorecard.json` were still frozen at the
tier-2 state. Per explicit human direction this session, the Tier 3 forge gap
is accepted as missed (no retroactive backfill); this phase closes out Tier 3
adaptively and moves straight to Tier 4.

**Phase E (tier 3):**
- Weight deltas: **none** — no forge candidates ran in tier 3, so there is no
  candidate-level signal to bump or down-weight on. Not a discard; nothing was
  attempted.
- Stuck families: none — `consecutiveDiscards` unchanged (0 for
  `hard-case-replay`, `orbat-asymmetric-ratio`, `mission-combo-escort-strike`).
- Hard-case pool: still empty on disk (`corpus/hard-cases/` = `.gitkeep` +
  `README.md` only). The two tier-1 signatures remain un-materialised, keeping
  `hard-case-replay` (weight 1.7192, highest in the catalog) ineligible again
  this tier on its `hard-cases-nonempty` precondition.

**Phase `pre` (tier 4):**
- Hindsight recall: **SKIPPED** — server unreachable (`curl http://localhost:8888` → exit 000), consistent with every prior phase this run.
- Corpus loaded: `coverage-map.json` now 29 cells / 43 scenarios (grew from the
  tier-2 forge state of 23/28 via unrelated, already-merged stress-axis work —
  PR #365 — not forge activity from this run), `recipe-weights.json` (17
  recipes, unchanged from tier-2 post-oracle), `hard-cases/` (still empty),
  `index.yaml` (43 promoted policies).
- Tier-4-eligible recipes (`tierMin <= 4`, `hard-case-replay` excluded on
  precondition): ranked `platform-swap-underused` (1.2) >
  `roe-asymmetric-per-side` (1.1) > `mission-concurrent-asw-aaw` (1.0, newly
  eligible, on-theme) > `victory-weighted-multi` (1.0, newly eligible, new
  dim) > `emcon-timed-phases` (1.1, already exercised at tier 2, deprioritized)
  > others.
- Selected 4 for this wave: `platform-swap-underused`,
  `mission-concurrent-asw-aaw`, `victory-weighted-multi`,
  `roe-asymmetric-per-side` — prioritizing the tier's own theme
  (ASW/AAW mission) and an entirely untouched dimension (victory) over
  repeating an already-covered dim (EMCON phases).
- `underusedPlatformHint` (15 IDs) queried directly against
  `assets/data/catalog/baltic_patrol.db`: 10 subsurface (SSK/SSN/SSGN/SSBN) + 5
  air — strongly ASW/AAW-shaped, feeding directly into the tier-4 roster ask.
- Plan written: `forge/mid-tier-plan.yaml` (tier-2 plan superseded in place;
  its summary is preserved in the tier-2 entries above).
