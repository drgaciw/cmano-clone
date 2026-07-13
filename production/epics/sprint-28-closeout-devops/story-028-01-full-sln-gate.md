---
id: S28-01
status: Complete
type: Config
priority: must-have
graphite_branch: stack/sprint28/full-sln-gate
estimate_days: 1
dependencies:
  - S27 complete @ a93b55e
owner: c-sharp-devops-engineer
sprint: 28
req_trace: Sprint hygiene; kickoff DoD
---

# Story 028-01 — Full-Solution Re-Baseline

> **Epic:** sprint-28-closeout-devops  
> **Sprint:** 28 — CMO Corpus v2 + Platform Write Path + Combat Phase 2

## Summary

Day-1 gate: `dotnet build` + `dotnet test ProjectAegis.sln` @ trunk; record ≥741 in `sprint-status.yaml` and smoke evidence; GitNexus analyze @ HEAD. Blocks S28-02+ feature work until green.

## Acceptance Criteria

- [x] `dotnet build ProjectAegis.sln` — 0 errors
- [x] `dotnet test ProjectAegis.sln` — ≥741/741 PASS (741/741)
- [x] `ReplayGoldenSuiteTests` — 6/6 PASS
- [x] `npx gitnexus analyze . --force` completes
- [x] Evidence: `production/qa/smoke-sprint-28-*.md`
- [x] `sprint-status.yaml`: `tests_passed_sprint28_baseline`, indexed commit

## QA Test Cases

- **AC-1**: Full solution green
  - Given: trunk @ post-S27 merge (`a93b55e`)
  - When: build + test + replay golden + gitnexus
  - Then: 0 failures; count ≥741; replay 6/6
  - Edge cases: GHA CodeQL advisory vs Buildkite green; local-gate advisory documented

- **AC-2**: GitNexus index recorded
  - Given: clean worktree @ HEAD
  - When: `npx gitnexus analyze . --force`
  - Then: nodes/edges recorded in smoke evidence
  - Edge cases: multi-repo CLI limitations documented per S27 pattern

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

- Kickoff: `production/sprints/sprint-28-corpus-write-combat-v2.md` (S28-01)
- S27-01 pattern: `production/epics/sprint-27-cmo-corpus-import/story-027-01-full-sln-gate.md`
- Parallel kickoff: `production/agentic/sprint-28-parallel-kickoff-2026-06-18.md`
- QA plan: `production/qa/qa-plan-sprint-28-2026-09-18.md` *(create before implementation)*

## Completion Notes
**Completed**: 2026-06-18
**Criteria**: 6/6 passing
**Deviations**: None
**Test Evidence**: `production/qa/smoke-sprint-28-baseline-2026-06-18.md`
**Code Review**: Skipped (lean mode)