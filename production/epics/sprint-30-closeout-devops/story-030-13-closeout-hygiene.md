---
id: S30-13
status: Complete
last_updated: 2026-06-18
type: Config
priority: should-have
graphite_branch: stack/sprint30/closeout
estimate_days: 0.5
dependencies:
  - S30-03+ (must-have gate landed)
owner: c-sharp-devops-engineer
sprint: 30
req_trace: Sprint closeout DoD
---

# Story 030-13 — Closeout Hygiene

> **Epic:** sprint-30-closeout-devops

## Summary

Replay 6/6; GitNexus @ stack tip; tracker rows 06/18/21; prune `stack/sprint29/*`. Full sln ≥918 closeout evidence.

## Acceptance Criteria

- [x] `ReplayGoldenSuiteTests` — 6/6 PASS
- [x] `dotnet test ProjectAegis.sln` — ≥918 @ closeout (**956/956**)
- [x] `production/qa/sprint-30-gitnexus-*.md` with nodes/edges
- [x] `production/qa/smoke-sprint-30-closeout-*.md`
- [x] Tracker rows 06, 18, 21 updated
- [x] `stack/sprint29/*` prune documented (merged branches only)
- [x] `sprint-status.yaml` closeout counters + evidence list
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Full closeout gate
  - Given: all must-have stories merged
  - When: full sln + replay + gitnexus
  - Then: ≥918 PASS; replay 6/6; GitNexus indexed
  - Edge cases: should-have cut line applied; partial merge state

- **AC-2**: Stack prune hygiene
  - Given: merged `stack/sprint29/*` branches
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

- S29-13 pattern: `production/epics/sprint-29-closeout-devops/story-029-13-closeout-hygiene.md`
- Kickoff: `production/sprints/sprint-30-tl-bind-corpus-scale.md` (S30-13)
- Parallel kickoff: `production/agentic/sprint-30-parallel-kickoff-2026-06-18.md`
- QA plan: `production/qa/qa-plan-sprint-30-2026-10-16.md`