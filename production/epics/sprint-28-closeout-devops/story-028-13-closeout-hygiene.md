---
id: S28-13
status: Complete
type: Config
priority: should-have
graphite_branch: stack/sprint28/closeout
estimate_days: 0.5
dependencies:
  - S28-03+ (must-have gate landed)
owner: c-sharp-devops-engineer
sprint: 28
req_trace: Sprint closeout DoD
---

# Story 028-13 — Closeout Hygiene

> **Epic:** sprint-28-closeout-devops

## Summary

Replay 6/6; GitNexus @ stack tip; tracker rows 06/18/21; prune `stack/sprint27/*`. Full sln ≥741 closeout evidence.

## Acceptance Criteria

- [x] `ReplayGoldenSuiteTests` — 6/6 PASS
- [x] `dotnet test ProjectAegis.sln` — ≥741 @ closeout (**801/801**)
- [x] `production/qa/sprint-28-gitnexus-*.md` with nodes/edges
- [x] `production/qa/smoke-sprint-28-closeout-*.md`
- [x] Tracker rows 06, 18, 21 updated
- [x] `stack/sprint27/*` prune documented (merged branches only)
- [x] `sprint-status.yaml` closeout counters + evidence list (`tests_passed_sprint28_closeout: 801`)
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Full closeout gate
  - Given: all must-have stories merged
  - When: full sln + replay + gitnexus
  - Then: ≥741 PASS; replay 6/6; GitNexus indexed
  - Edge cases: should-have cut line applied; partial merge state

- **AC-2**: Stack prune hygiene
  - Given: merged `stack/sprint27/*` branches
  - When: closeout doc reviewed
  - Then: prune list documented; no stale local refs
  - Edge cases: open PR branches excluded from prune

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

- S27-13 pattern: `production/epics/sprint-27-closeout-devops/story-027-13-closeout-hygiene.md`
- Kickoff: `production/sprints/sprint-28-corpus-write-combat-v2.md` (S28-13)
- Parallel kickoff: `production/agentic/sprint-28-parallel-kickoff-2026-06-18.md`
- QA plan: `production/qa/qa-plan-sprint-28-2026-09-18.md` *(create before implementation)*

## Completion Notes

**Completed**: 2026-06-18  
**Criteria**: 8/8 passing  
**Deviations**: None  
**Test Evidence**: Config — `production/qa/smoke-sprint-28-closeout-2026-06-18.md`, `production/qa/sprint-28-gitnexus-2026-06-18.md`, `production/agentic/stacks/sprint28/S28-13-DONE.md`  
**Code Review**: Skipped (lean mode); closeout hygiene only