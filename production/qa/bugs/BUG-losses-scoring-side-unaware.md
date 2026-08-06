# Bug Report

## Summary
**Title**: Kill scoring is side-unaware — enemy kills against your own units are credited to your own kill tally
**ID**: BUG-losses-scoring-side-unaware
**Severity**: S2-Major (scoring/oracle correctness broken; sim does not crash)
**Priority**: P2 — CRITICAL blast radius on the naive fix surface; fixed via a narrow route that avoided it entirely
**Status**: **FIXED** (2026-07-27) — was QUARANTINED-CRITICAL
**Reported**: 2026-07-27
**Reporter**: QA Gauntlet run `gauntlet-20260727-1455` (Tier 1), root-caused by c-sharp-architect investigation

## Classification
- **Category**: Gameplay (combat scoring)
- **System**: `ProjectAegis.Delegation` scoring projection
- **Frequency**: Always (any scenario where both sides land at least one kill)
- **Regression**: No — long-standing. No recent commits touch this formula. Several of the 24 already-promoted gauntlet corpus scenarios have `gauntlet.expect.minKills` values (4–5) that only make sense if calibrated against this same behavior (own-side kill tally inflated by enemy-scored kills) — the corpus appears tuned around the bug.

## Environment
- **Build**: commit `fa4db95c` (trunk base) + gauntlet branch `07-27-qa_gauntlet_gauntlet-20260727-1455`
- **Scenario**: Any gauntlet scenario where both BLUE and RED score at least one kill, e.g. `gauntlet-20260727-1455-t1-s1`, seed 42

## Reproduction Steps
**Preconditions**: A 3-vs-3 scenario where RED kills at least one BLUE unit.

1. Run: `dotnet run --project src/ProjectAegis.Delegation.Demo -- --batch --scenarios gauntlet-20260727-1455-t1-s1 --seeds 42 --ticks 6 --csv-out out.csv`
2. Inspect BLUE's reported `kills` in the CSV (=4) against the fingerprint trace.
3. Of the 4 `Kill`-coded `EngagementOutcome` events in the log, 3 are BLUE-fired (against RED units `421-orkan-pr-660-2015`, `70-rauma-helsinki-ii-1990`, `mrk-buyan-mod-pr-21631-buyan-m-2014`) and 1 is RED-fired (`421-orkan-pr-660-2015` killing BLUE's `f-341-absalon-2020`).

**Expected Result**: BLUE's kill count should only include kills BLUE's own units scored against the enemy (3, not 4).
**Actual Result**: `LossesScoringProjection.Project` counts every `Kill`-coded outcome in the shared `DecisionLog` regardless of which side fired it, crediting RED's kill of a BLUE unit toward BLUE's own tally.

## Technical Context
- **Root cause file:line**: `src/ProjectAegis.Delegation/Projection/LossesScoringProjection.cs:12` — `log.EngagementOutcomes.Count(o => o.OutcomeCode == EngagementOutcomeCodes.Kill)` has no side/faction filter.
- **Confirmed by grep**: no `side` reference anywhere in `LossesScoringProjection.cs`.
- **Compounds with BUG-engagement-resolver-shooter-liveness** (companion defect, filed separately) — that bug means the "extra" kill events aren't just miscredited, some are the direct result of dead units still being allowed to fire.
- **Recommended fix path** (per c-sharp-architect investigation, not implemented): thread a `side` field through `DecisionLog`/`EngagementOutcomeRecord` (likely sourced from `BalticV3SideRegistry`), then filter `LossesScoringProjection.Project`'s count to only outcomes fired by units on the side being scored.

## Evidence
- GitNexus `impact({target: "EngagementOutcomeRecord", direction: "upstream", kind: "Record", repo: "/home/username01/cmano-clone"})` on the constructor candidate (the one that would need a new `side` parameter):
  - **risk: CRITICAL**
  - impactedCount: **381** (larger than the companion shooter-liveness defect's 42)
  - This is the natural fix surface because `EngagementOutcomeRecord` is a shared record type; adding/threading a side field ripples through everywhere it's constructed or consumed.

## Related Issues
- Companion defect: `production/qa/bugs/BUG-engagement-resolver-shooter-liveness.md` — same root-cause investigation surfaced both in the same session.

## Notes

**Why this is quarantined rather than fixed immediately**: same autonomy boundary as the companion defect — CRITICAL GitNexus impact on the actual fix surface (`EngagementOutcomeRecord`, 381 impacted symbols) means this is not safe for unsupervised autonomous remediation. A human-supervised fix should:
1. Design the side-threading change deliberately (it touches a widely-shared record type — 381 impacted symbols is large enough that a naive constructor-parameter addition could break many call sites; consider whether a narrower approach exists, e.g. a side lookup keyed by unit id rather than a new record field).
2. Fix in tandem with BUG-engagement-resolver-shooter-liveness — both should be verified together since correct scoring depends on correct engagement/kill semantics first.
3. Re-run the full test suite, `replay-verify`, and the entire 24-scenario gauntlet corpus, recalibrating `gauntlet.expect` per the expect-regen runbook (`tools/qa-gauntlet/README-expect-regen.md`) since corpus-wide expected kill/score values will shift once both bugs are fixed.

---

## Resolution (2026-07-27)

**Fixed** — and notably **without** touching the 381-symbol CRITICAL surface the original triage feared.

- **Key realisation**: `EngagementOutcomeRecord` already carries `ShooterTargetId`, and `TargetId` is `readonly record struct TargetId(string Value)` — a *string* unit id, which is exactly the key `BalticV3SideRegistry.GetSideForUnit(string)` takes. No new record field, no constructor change, so the 381-symbol blast radius was never incurred. The real fix surface was 3 callers, not 381 symbols.
- **Change**: `LossesScoringProjection.Project` takes an optional `scoredSide` and filters kills by shooter side; `LossesScoringCsvExporter` passes the `side` it already received.
- **Deliberate rule**: a kill counts unless the shooter is *positively known* to be on a different side. Unknown/unregistered shooters still count, so legacy synthetic scenarios that never call `RegisterScenarioSide` do not silently score zero.
- **Known gap (intentional)**: `C2TopBarProjection` still calls the null (count-all) path — it has no side context, and guessing the player's side would be a behaviour change beyond this defect. Follow-up: give the C2 top bar a side source.
- **Regression tests**: 3 new in `LossesScoringProjectionTests` — opposing-side exclusion, mirrored red-side scoring, and unregistered-unit preservation.
- **Verification**: full solution 1928 passed / 0 failed / 0 skipped. Tier-1 oracle `allPassed: true` (was 0/4). Seed-42 kills dropped from an impossible **4** (only 3 enemy units existed) to 2–3, and per-scenario results now genuinely differ instead of being identical across all 4 ORBATs — the original tell that outcomes were scenario-independent.
- **Corpus**: all 24 pre-existing scenarios re-baselined and passing at correct per-tier tick budgets (see `BUG-scoring-penalises-roe-correct-refusals.md` for a design question this surfaced).
- **Commit**: `48c648e1`
