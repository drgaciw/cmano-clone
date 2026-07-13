---
id: S33-01
status: Complete
last_updated: 2026-06-19
completed: 2026-06-19
type: Config
priority: must-have
graphite_branch: stack/sprint33/full-sln-gate
estimate_days: 1
dependencies:
  - S32 merged + S32-13 closeout
owner: c-sharp-devops-engineer
sprint: 33
req_trace: Sprint hygiene; kickoff DoD; ≥1046 baseline
---

# Story 033-01 — Full-Solution Re-Baseline

> **Epic:** sprint-33-closeout-devops

## Summary

Day-1 gate: `dotnet build` + `dotnet test ProjectAegis.sln` @ trunk; record **≥1073** in `sprint-status.yaml` and smoke evidence; GitNexus analyze @ HEAD. Blocks S33-02+ until green.

## Acceptance Criteria

- [x] `dotnet build ProjectAegis.sln` — 0 errors
- [x] `dotnet test ProjectAegis.sln` — ≥1073/1073 PASS
- [x] `ReplayGoldenSuiteTests` — 6/6 PASS
- [x] Evidence: `production/qa/smoke-sprint-33-baseline-2026-06-19.md`
- [x] ZERO touch `DelegationBridge.cs`

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet build ProjectAegis.sln -v minimal
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
```