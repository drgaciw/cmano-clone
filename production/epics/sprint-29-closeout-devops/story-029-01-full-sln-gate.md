---
id: S29-01
status: Complete
type: Config
priority: must-have
graphite_branch: stack/sprint29/full-sln-gate
estimate_days: 1
dependencies:
  - S28 complete @ 1d93e86
owner: c-sharp-devops-engineer
sprint: 29
req_trace: Sprint hygiene; kickoff DoD
---

# Story 029-01 — Full-Solution Re-Baseline

> **Epic:** sprint-29-closeout-devops  
> **Sprint:** 29 — Operationalize Data-to-Fight Loop

## Summary

Day-1 gate: `dotnet build` + `dotnet test ProjectAegis.sln` @ trunk; record ≥801 in `sprint-status.yaml` and smoke evidence; GitNexus analyze @ HEAD. Blocks S29-02+ feature work until green.

## Acceptance Criteria

- [x] `dotnet build ProjectAegis.sln` — 0 errors
- [x] `dotnet test ProjectAegis.sln` — ≥801/801 PASS (801/801)
- [x] `ReplayGoldenSuiteTests` — 6/6 PASS
- [x] `npx gitnexus analyze . --force` completes (12,240 / 24,819)
- [x] Evidence: `production/qa/smoke-sprint-29-baseline-2026-06-18.md`
- [x] `sprint-status.yaml`: `tests_passed_sprint29_baseline`, indexed commit

## QA Test Cases

- **AC-1**: Full solution green
  - Given: trunk @ post-S28 merge (`1d93e86`)
  - When: build + test + replay golden + gitnexus
  - Then: 0 failures; count ≥801; replay 6/6
  - Edge cases: GHA CodeQL advisory vs Buildkite green; local-gate advisory documented

- **AC-2**: GitNexus index recorded
  - Given: clean worktree @ HEAD
  - When: `npx gitnexus analyze . --force`
  - Then: nodes/edges recorded in smoke evidence
  - Edge cases: multi-repo CLI limitations documented per S28 pattern

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet build ProjectAegis.sln -v minimal
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
npx gitnexus analyze . --force
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `DelegationBridge.cs` | ZERO touch |
| `CatalogWriteGate` | CRITICAL — no edits on baseline story |

## References

- Kickoff: `production/sprints/sprint-29-operationalize-data-fight-loop.md` (S29-01)
- S28-01 pattern: `production/epics/sprint-28-closeout-devops/story-028-01-full-sln-gate.md`
- Parallel kickoff: `production/agentic/sprint-29-parallel-kickoff-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-29-2026-10-02.md`

## Completion Notes

**Completed**: 2026-06-18  
**Criteria**: 6/6 passing  
**Deviations**: None  
**Test Evidence**: `production/qa/smoke-sprint-29-baseline-2026-06-18.md`, `production/agentic/stacks/sprint29/S29-01-DONE.md`  
**Code Review**: Skipped (lean mode)