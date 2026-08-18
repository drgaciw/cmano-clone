# QA Gauntlet — After Action Review

**Run:** `gauntlet-20260728-2016`
**Date:** 2026-07-28
**Base SHA:** `5605a972` (branch `feat/platform-editor-uiux-productization`) — same HEAD as `gauntlet-20260728-2000`
**Config:** 5 tiers · 4 scenarios/tier · seeds `42,7,123` · max-fix-attempts 3

---

## Verdict

**Ladder PASS (5/5 tiers + extra). Zero new defects. Zero code changes. Zero commits.**

This run executed ~20 minutes after `gauntlet-20260728-2000` completed on the same HEAD, in a
fresh session. Its distinguishing result is **cross-session reproducibility**: every tier's full
CSV — scores, kills, missiles, denials, and complete fingerprints — is **byte-identical** to the
prior run. The sim's determinism holds not just within a run (repeat batches) and across a week
(vs `gauntlet-20260720-2000`), but across sessions and orchestrators on the same commit.

## Preflight

| Gate | Result |
|---|---|
| Build | PASS — 0 warnings, 0 errors |
| Baseline suite | PASS — **1912 tests, 0 failures** (floor held) |
| Replay determinism (`ReplayGoldenTests`) | PASS — 4/4 |
| PlayMode smoke (`PlayModeSmokeHarnessTests`) | PASS — 38/38 |
| Catalog (`baltic_patrol.db`) | PASS — 30 tables, 79 platforms |
| GitNexus index | Current — rebuilt earlier this evening at this exact HEAD |

## Ladder results

| Tier | Ticks | Oracle | Within-run determinism | Cross-run vs 20:00 run |
|---|---|---|---|---|
| 1 | 6  | PASS | PASS | **byte-identical** |
| 2 | 10 | PASS | PASS | **byte-identical** |
| 3 | 16 | PASS | PASS | **byte-identical** |
| 4 | 24 | PASS | PASS | **byte-identical** |
| 5 | 40 | PASS | PASS | **byte-identical** |
| extra | 12 | PASS | PASS | **byte-identical** |

132 executions (22 scenarios × 3 seeds × 2 batches). Zero exceptions. Sanity clean.
Because the CSVs are identical to the prior run's, its supplementary checks transfer by identity:
seed sensitivity 22/22, joint-ORBAT `CATALOG_UNIT` air+subsurface+surface tokens in every
tier ≥3 scenario, and 60/60 regression-anchor agreement with `gauntlet-20260720-2000`.

## Defects

**New: none.**

**Reproduced (still OPEN):** `BUG-gauntlet-emcon-dimension-not-exercised` — high, `scenario-data`.
Re-verified this run: **0** `EMCON_OFF` tokens across the entire ladder, exactly as documented in
`../gauntlet-20260728-2000/bugs/BUG-gauntlet-emcon-dimension-not-exercised.md`. Not re-filed;
remediation remains owned by the unlanded plan
`docs/superpowers/plans/2026-07-27-gauntlet-variability.md`. The full evidence chain (DTO field
analysis, positive control, retrofit probe) lives in the prior run's bug report and was not
duplicated here.

The recommended follow-ups from the 20:00 AAR stand unchanged, in priority order:
1. Land the variability plan (closes the three EMCON retrofits at the root).
2. Make `Invoke-ScenarioValidate` fail on unknown keys under `gauntlet.*` (root-cause guard).
3. Reconcile the T3–T5 EMCON ladder wording with actual engine capability.
4. Fix the `dotnet` PATH assumption.
5. Add a dimension-vacuity gate to the ladder.

## Sign-off

| Item | Value |
|---|---|
| Baseline suite | 1912 / 0 failures |
| Final suite | 1912 / 0 failures (no code changed) |
| Test-count delta | 0 |
| Replay golden | PASS 4/4 |
| Cross-run reproducibility | PASS — 6/6 tiers byte-identical to `gauntlet-20260728-2000` |
| Defects found / fixed / quarantined | 0 new / 0 / 0 (1 prior defect reproduced, OPEN) |
| `QUARANTINED-CRITICAL` | none |
| Code changes | none (`detect_changes`: 0 C# symbols, 0 affected processes, low risk) |
| Commits / Graphite submit | none — nothing to commit or submit |

Working tree untouched outside `production/qa/gauntlet/gauntlet-20260728-2016/`:
`data/scenarios/` and `src/` unmodified; the user's pre-existing WIP files unmodified.
