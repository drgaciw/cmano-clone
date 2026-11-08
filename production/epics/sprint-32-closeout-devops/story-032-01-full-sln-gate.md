---
id: S32-01
status: Complete
type: Config
priority: must-have
graphite_branch: stack/sprint32/full-sln-gate
estimate_days: 1
dependencies:
  - S31 merged @ 3406bc4
owner: c-sharp-devops-engineer
sprint: 32
req_trace: Sprint hygiene; kickoff DoD; ≥1006 baseline
---

# Story 032-01 — Full-Solution Re-Baseline

> **Epic:** sprint-32-closeout-devops  
> **Sprint:** 32 — Release Train Ops, Combat Phase 6 & Platform Editor Phase F

## Summary

Day-1 gate: `dotnet build` + `dotnet test ProjectAegis.sln` @ trunk; record **≥1006** in `sprint-status.yaml` and smoke evidence; GitNexus analyze @ HEAD. Blocks S32-02+ feature work until green.

## Acceptance Criteria

- [x] `dotnet build ProjectAegis.sln` — 0 errors
- [x] `dotnet test ProjectAegis.sln` — ≥1006/1006 PASS
- [x] `ReplayGoldenSuiteTests` — 6/6 PASS
- [x] `npx gitnexus analyze . --force` completes (nodes/edges recorded)
- [x] Evidence: `production/qa/smoke-sprint-32-baseline-*.md`
- [x] `sprint-status.yaml`: `tests_passed_sprint32_baseline`, indexed commit
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Full solution green
  - Given: trunk @ post-S31 merge (`3406bc4` / stack tip)
  - When: build + test + replay golden + gitnexus
  - Then: 0 failures; count ≥1006; replay 6/6
  - Edge cases: GHA CodeQL advisory vs Buildkite green; local-gate advisory documented

- **AC-2**: GitNexus index recorded
  - Given: clean worktree @ HEAD
  - When: `npx gitnexus analyze . --force`
  - Then: nodes/edges recorded in smoke evidence

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet build ProjectAegis.sln -v minimal
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
npx gitnexus analyze . --force
git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `DelegationBridge.cs` | ZERO touch |
| `CatalogWriteGate` | CRITICAL — no edits on baseline story |

## References

- Kickoff: `production/sprints/sprint-32-release-train-combat-phase6-platform-phase-f.md` (S32-01)
- S31-01 pattern: `production/epics/sprint-31-closeout-devops/story-031-01-full-sln-gate.md`
- QA plan: `production/qa/qa-plan-sprint-32-*.md` *(create before implementation)*