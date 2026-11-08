# Smoke — Sprint 35 Full Closeout (S35-14)

**Date:** 2026-06-19  
**Sprint:** 35 — Polish Phase 1 Entry (UX foundation, sim perf P0/P1, C2/Platform Editor hardening)  
**Stories:** S35-01..17 complete (17/17); **S35-13 stage advance executed 2026-06-19**  
**Branch:** `main` @ `8de98b150da515b205358106852eb75376ddba5f` (+ uncommitted sprint deltas verified @ closeout tip)

## Verdict: **PASS**

## Gate results

| Gate | Result |
|------|--------|
| `dotnet build ProjectAegis.sln` | **PASS** — 0 errors, 0 warnings |
| `dotnet test ProjectAegis.sln` | **PASS** — **1204/1204** (closeout target ≥1193; +11 vs S35-01 baseline) |
| `ReplayGoldenSuiteTests` | **PASS** — **6/6** (195 ms) |
| C2 headless proxy checks 1–13 | **PASS** — filter **85/85** |
| C2 headless proxy checks 14–18 | **PASS** — filter **58/58** |
| C2 headless proxy combined | **PASS** — **18/18** (85+58 tests) |
| `npx gitnexus analyze` | **PASS** — 16,794 nodes \| 33,811 edges |
| `DelegationBridge.cs` diff | **PASS** — ZERO touch |
| Production Baltic world hash | **PASS** — unchanged `17144800277401907079` |

## Per-project counts

| Project | Passed |
|---------|--------|
| ProjectAegis.Data.Tests | 398 |
| ProjectAegis.Sim.Tests | 279 |
| ProjectAegis.Delegation.Tests | 235 |
| ProjectAegis.Delegation.UnityAdapter.Tests | 245 |
| ProjectAegis.MissionEditor.Cli.Tests | 42 |
| ProjectAegis.Data.Excel.Tests | 5 |
| **Total** | **1204** |

## Baseline delta

| Ref | Tests | Notes |
|-----|-------|-------|
| S34 closeout | 1193 | @ `d3db76db` |
| S35-01 day-1 baseline | 1193 | ReplayGolden 6/6; Baltic hash pin |
| S35-05 sim P0 | 1197 | +4 Sim.Tests (PdDetection/DeterministicDetection) |
| S35-10 sim P1 | 1204 | +7 (DecisionLog/DatalinkSidePicture) |
| **S35 closeout** | **1204** | +11 from S35-01; no regression |

## Sprint tier summary

| Tier | Stories | Verdict |
|------|---------|---------|
| Must-have | S35-01..07, S35-14 | **PASS** |
| Should-have | S35-08..12 | **PASS** |
| Should-have | S35-13 stage advance | **PASS** — Production → Polish @ 2026-06-19 |
| Nice-to-have | S35-15, S35-16, S35-17 | **PASS** (S35-15 CI hygiene doc; S35-16 plan-only; S35-17 perf appendix) |

## Carry-forward log (deferred / PASS WITH NOTES)

| Item | Story | Disposition | Evidence |
|------|-------|-------------|----------|
| Live Editor PNG capture (12) | S35-09 | **LEAN ACCEPTED** — headless proxy 58/58; placeholders mapped | `production/agentic/sprint-35-presentation-evidence-2026-06-19.md` |
| Unity C2 frame p95 Profiler | S35-04 | **DEFERRED** — panel-bind p95 0.013 ms headless; Editor Profiler host required | `production/perf/unity-c2-frame-baseline-s35-2026-06-19.md` |
| Stage advance Production → Polish | S35-13 | **COMPLETE** — user ack CONCERNS uplift; `stage.txt` → Polish | `story-035-13-stage-advance-polish.md` |
| `tests/unit/` studio layout migration | S35-15 | **DEFERRED** (6th deferral) — gate thresholds refreshed to ≥1204 | `production/qa/sprint-35-ci-hygiene-2026-06-19.md` |
| Buildkite vs local gate authority | S35-15 | **ADVISORY** — Buildkite merge authority; `verify-ci-local.ps1` fallback | same |

## Commands executed

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet build ProjectAegis.sln -v minimal
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests \
  --filter "ReplayGoldenSuiteTests" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests \
  --filter "PlayModeSmoke|C2Selection|OobTree|LossesScoring|BalticReplay|FuelState|AttackMenu" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests \
  --filter "PlatformImport|Doctrine|C2TopBar|PlatformCatalogViewer|PlatformComms|PlatformLinkCatalog" -v minimal
npx gitnexus analyze
git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
```

## S35-13 User Acknowledgment — Gate CONCERNS Uplift

**Date:** 2026-06-19  
**Gate:** `production/gate-checks/production-to-polish-2026-06-19-r2.md` — **CONCERNS (uplifted)**  
**User decision:** Explicit confirmation to advance `production/stage.txt` Production → **Polish** despite residual CONCERNS (not clean PASS).

**Residual items acknowledged (Polish Sprint 0 carry-forward):**

| Item | Status |
|------|--------|
| Unity C2 frame p95 vs 16.67 ms budget | UNKNOWN — Editor Profiler host required |
| `tests/unit/` studio layout | DEFERRED (6th) — gate thresholds at ≥1204 in `src/*.Tests` |
| AD-ART-BIBLE formal sign-off | PENDING — `design/art/art-bible.md` exists; AD sign-off line open |
| Live Editor PNG capture (12) | LEAN ACCEPTED — headless proxy 58/58 |
| Accessibility + interaction patterns | ADDRESSED S35-03 — ongoing Polish validation |
| Platform Editor full UX spec coverage | PARTIAL — C2 specs present; Platform Editor thin |

**Stage file:** `production/stage.txt` → `Polish` (effective 2026-06-19)

## References

- Day-1 baseline: `production/qa/smoke-sprint-35-baseline-2026-06-19.md`
- GitNexus closeout: `production/agentic/sprint-35-gitnexus-closeout-2026-06-19.md`
- QA plan: `production/qa/qa-plan-sprint-35-2026-06-19.md`
- Authority: `production/polish-scope-boundary-2026-06-19.md`