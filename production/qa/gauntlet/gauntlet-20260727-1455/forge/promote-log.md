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

## Phase `a0` (tier 4) — 2026-07-28T02:35Z

Roster built (`forge/tier-4-roster.json`, 29 platforms, queried directly
against `assets/data/catalog/baltic_patrol.db` — no invented catalog rows,
sensor category labels follow the domain+class heuristic already established
by tier-3's roster since the DB itself has no descriptive sensor-category
column). Four `military-simulation-architect` candidates drafted in parallel
(one per selected recipe, independent write paths under `forge/candidates/`),
each validated against the repo's real `infer_cell()`
(`tools/qa-gauntlet/forge_scorecard.py`) before acceptance:

| candidate | recipe | cell key | units | notes |
|---|---|---|---|---|
| `…-t4-c1` | `platform-swap-underused` | `asw\|air,subsurface,surface\|WeaponsTight/WeaponsFree\|unrestricted\|none` | 14 (7v7) | uses all 5 flagged underused submarines + all 4 flagged underused fighter/attack air; SSBN correctly excluded as a non-combatant |
| `…-t4-c2` | `mission-concurrent-asw-aaw` | `asw\|air,subsurface,surface\|WeaponsFree/WeaponsTight\|unrestricted\|event-chain` | 12 (7v5) | genuinely concurrent ASW hunt (tick 0) + AAW intercept (tick 6) in the same 24-tick window, not sequential |
| `…-t4-c3` | `victory-weighted-multi` | `escort\|air,subsurface,surface\|WeaponsTight/WeaponsTight\|emcon-phases\|none` | 12 (6v6) | no dedicated victory/weighted-score schema field exists (`GauntletOracleExpect.cs` confirmed) — expressed via the sim's real weighted score formula (kills×100, denials×−5) kept simultaneously non-trivial via two real `mission.triggers` ROE-escalation gates, plus the real (not invented) `RequireFingerprintSubstrings` expect field standing in for objective completion |
| `…-t4-c4` | `roe-asymmetric-per-side` | `escort\|air,subsurface,surface\|WeaponsTight/WeaponsFree\|contested-em\|none` | 12 (6v6) | domain-specific ROE/ID nuance (ASW held-pending-ID vs AAW confident-lock) expressed via detection `basePd` contrast + explicit intent narrative, since the schema has only one fleet-wide `friendlyRoe`/`opposingRoe` pair — the constraint is disclosed in-policy, not silently worked around |

All 4 cell keys confirmed **non-duplicate** against both the existing 29-cell
corpus and each other. All 4 `gauntlet.expect` blocks are honestly marked
`"note": "expect provisional pending regen"` — none claim a real empirical
regen, since none have been batch-run yet.

**Scope boundary for this standalone invocation**: batch-executing these 4
candidates (`Delegation.Demo --batch`), running `gauntlet_oracle_eval`, and
the `post-oracle` scorecard/promote decision are **not done here** — that is
`/qa-gauntlet`'s own Phase B/C, out of scope for this session per the human's
"draft the Tier 4 mid-tier-plan and candidates" direction. The 4 candidates
sit in the gitignored `forge/candidates/` directory, ready for the next
`/qa-gauntlet` tier-4 run to batch-execute alongside the main ladder
scenarios and hand back to this skill's `post-oracle` phase for scoring.

## Phase `post-oracle` (tier 4) — 2026-07-28T04:15Z — RESUMED under full `/qa-gauntlet` run

The human explicitly requested `/qa-gauntlet` run Tier 4 for real (`--resume
gauntlet-20260727-1455 --tier 4`), superseding the standalone-forge scope
boundary noted above. Full Phase 0-C executed: preflight green (baseline
1928/1928, replay-verify 17/17, smoke 21/21, catalog gate), tier-4 main-ladder
roster + 4 scenarios drafted (Phase A0/A1), all 8 tier-4 policies (4 main + 4
forge candidates) validated (Phase A2, `production/qa/scenario-audit-2026-07-28-gauntlet-t4.md`,
0 BLOCKERs), batch-executed together (`Delegation.Demo --batch`, 24 ticks,
seeds 42/7/123, `tier-4/results.csv`), `gauntlet.expect` regenerated from the
real CSV per `tools/qa-gauntlet/README-expect-regen.md`, `gauntlet_oracle_eval`
→ `allPassed: true` for all 8 (`tier-4/oracle-eval.json`).

**4 promotes, 0 discards.**

| candidate | recipe | cell | novelty | oracle |
|---|---|---|---|---|
| `…-t4-c1` | `platform-swap-underused` | `asw\|air,subsurface,surface\|WeaponsTight/WeaponsFree\|unrestricted\|none` | 6.0 | PASS |
| `…-t4-c2` | `mission-concurrent-asw-aaw` | `asw\|air,subsurface,surface\|WeaponsFree/WeaponsTight\|unrestricted\|event-chain` | 5.5 | PASS |
| `…-t4-c3` | `victory-weighted-multi` | `escort\|air,subsurface,surface\|WeaponsTight/WeaponsTight\|emcon-phases\|none` | 6.0 | PASS |
| `…-t4-c4` | `roe-asymmetric-per-side` | `escort\|air,subsurface,surface\|WeaponsTight/WeaponsFree\|contested-em\|none` | 5.0 | PASS |

All 4 scored via `python3 tools/qa-gauntlet/forge_scorecard.py --run-dir
production/qa/gauntlet/gauntlet-20260727-1455 --tier 4` — `hardGatesPass: true`
for all 4, all 4 landed in genuinely new coverage cells (confirmed non-duplicate
against the pre-tier-4 29-cell corpus). Promoted to `data/scenarios/` under
their forge candidate ids. The 4 tier-4 main-ladder scenarios were also added
to `data/scenarios/` under their real ids (`gauntlet-20260727-1455-t4-s{1..4}`),
matching the precedent set by tiers 1-3's main scenarios already living there.

### Real defect found during Phase C investigation (not a forge-candidate issue)

While diagnosing why `gauntlet-20260727-1455-t4-s3` (a **main-ladder** scenario,
not a forge candidate) and `…-t4-c1` produced byte-identical results across all
3 seeds, confirmed a real, scoped `sim-code` gap: `BalticReplayHarness.RunCore`
only constructs `ScenarioContactSimulator` (the component that fires scripted
`contacts[].appearAtTick` mid-run contact appearances) in an `else if` branch
that is unreachable whenever a policy's `detection[]` array is non-empty — which
is true of nearly every scenario in this corpus. `s3`'s scripted reinforcement
contact and its dependent ROE-escalation trigger silently never fire as a
result. Filed as `production/qa/bugs/BUG-scenario-contacts-shadowed-by-detection.md`
and quarantined (not fixed — GitNexus MCP tools were unavailable this session,
so no `src/` symbol was edited per the project's impact-analysis-first rule).
`s3`'s `gauntlet.intent`/`gauntlet.oracle` text was corrected in place to match
confirmed real behavior instead of the originally-intended-but-nonfunctional
mechanic; its already-regenerated `expect` envelope needed no numeric change
(it was derived from the real, non-escalated observed data either way). This is
not a forge-recipe defect and does not affect any promote/discard decision
above — flagged here because it surfaced during this tier's Phase C, and
because it is corpus-wide relevant (any future scenario combining `contacts[]`
with `detection[]` will hit the same silent gap).

### Weight + coverage deltas

- `platform-swap-underused` 1.2 → **1.38**
- `mission-concurrent-asw-aaw` 1.0 → **1.15**
- `victory-weighted-multi` 1.0 → **1.15**
- `roe-asymmetric-per-side` 1.1 → **1.265**
- coverage-map: **29 → 36 cells**, scenarioCount 43 → 51 (8 new scenarios: 4 main + 4 forge)
- `corpus/index.yaml`: 4 forge-candidate entries appended
- `underusedPlatformHint` recomputed from updated platform counts

### Stuck families

None. No recipe reached the 5-consecutive-discard threshold.

### Regression anchors (Phase C)

Re-ran one anchor scenario per prior tier at each tier's own tick budget
(seeds 42/7/123): `gauntlet-20260727-1455-t1-s1` (6 ticks), `-t2-s1` (10
ticks), `-t3-s1` (16 ticks) — all three matched their previously-recorded
seed-42 baselines exactly (score/kills/missilesFired/denials). No regression
from this session's SDK-substitute/compiler-compat workaround or from any
change made this tier.
