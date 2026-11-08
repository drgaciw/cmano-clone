---
id: S27-01
status: Complete
type: Config
priority: must-have
graphite_branch: stack/sprint27/full-sln-gate
estimate_days: 1
dependencies:
  - S26 complete @ ab30d35
owner: c-sharp-devops-engineer
sprint: 27
req_trace: Sprint hygiene; kickoff DoD
---

# Story 027-01 — Full-Solution Re-Baseline

> **Epic:** sprint-27-cmo-corpus-import  
> **Sprint:** 27 — CMO Corpus Pipeline + Combat Bounded + Phase C Viewer

## Summary

Day-1 gate: `dotnet build` + `dotnet test ProjectAegis.sln` @ trunk; record ≥698 in `sprint-status.yaml` and smoke evidence; GitNexus analyze @ HEAD. Blocks S27-02+ feature work until green.

## Acceptance Criteria

- [x] `dotnet build ProjectAegis.sln` — 0 errors
- [x] `dotnet test ProjectAegis.sln` — ≥698/698 PASS (701/701)
- [x] `ReplayGoldenSuiteTests` — 6/6 PASS
- [x] `npx gitnexus analyze . --force` completes
- [x] Evidence: `production/qa/smoke-sprint-27-*.md`
- [x] `sprint-status.yaml`: `tests_passed_sprint27_baseline`, indexed commit

## QA Test Cases

- **AC-1**: Full solution green
  - Given: trunk @ post-S26 merge
  - When: build + test + replay golden + gitnexus
  - Then: 0 failures; count ≥698; replay 6/6
  - Edge cases: GHA CodeQL advisory vs Buildkite green

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet build ProjectAegis.sln -v minimal
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
npx gitnexus analyze . --force
```

## References

- Kickoff: `production/sprints/sprint-27-cmo-corpus-combat-bounded.md` (S27-01)
- QA plan: `production/qa/qa-plan-sprint-27-2026-06-18.md`

## Completion Notes
**Completed**: 2026-06-18
**Criteria**: 6/6 passing
**Deviations**: None
**Test Evidence**: `production/qa/smoke-sprint-27-baseline-2026-06-18.md`
**Code Review**: Skipped (lean mode)