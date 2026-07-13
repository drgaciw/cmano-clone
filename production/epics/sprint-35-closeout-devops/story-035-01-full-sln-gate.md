---
id: S35-01
status: Complete
completed: 2026-06-19
type: Config
priority: must-have
graphite_branch: stack/sprint35/full-sln-gate
estimate_days: 1
dependencies:
  - S34-13 closeout complete @ 8de98b1
owner: c-sharp-devops-engineer
sprint: 35
req_trace: Sprint hygiene; Polish Phase 1 kickoff; ≥1193 baseline
governing_adrs: N/A — sprint gate ritual
---

# Story 035-01 — Full-Solution Re-Baseline

> **Epic:** sprint-35-closeout-devops

## Summary

Day-1 gate: `dotnet build` + `dotnet test ProjectAegis.sln` @ trunk; record **1193** in `sprint-status.yaml` and smoke evidence; GitNexus analyze @ HEAD. Blocks S35-02+ until green.

## Acceptance Criteria

- [x] `dotnet build ProjectAegis.sln` — 0 errors
- [x] `dotnet test ProjectAegis.sln` — **1193/1193** PASS
- [x] `ReplayGoldenSuiteTests` — **6/6** PASS
- [x] Evidence: `production/qa/smoke-sprint-35-baseline-YYYY-MM-DD.md`
- [x] `sprint-status.yaml` — `tests_passed_sprint35_current` updated
- [x] ZERO touch `DelegationBridge.cs`
- [x] Production Baltic hash `17144800277401907079` recorded unchanged

## QA Test Cases

```
Test: Full solution green @ trunk
  Given: clean tree @ main indexed commit
  When: dotnet build + dotnet test ProjectAegis.sln
  Then: 0 errors; 1193/1193 PASS; ReplayGolden 6/6
```

## Test Evidence Path

- `production/qa/smoke-sprint-35-baseline-YYYY-MM-DD.md`
- `src/ProjectAegis.*.Tests` (full sln)

## Out of Scope

- Feature implementation; stage advance (S35-13)