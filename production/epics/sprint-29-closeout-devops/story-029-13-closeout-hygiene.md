---
id: S29-13
status: Not Started
type: Config
priority: should-have
graphite_branch: stack/sprint29/closeout
estimate_days: 0.5
dependencies:
  - S29-03+ (must-have gate landed)
owner: c-sharp-devops-engineer
sprint: 29
req_trace: Sprint closeout DoD
---

# Story 029-13 — Closeout Hygiene

> **Epic:** sprint-29-closeout-devops

## Summary

Replay 6/6; GitNexus @ stack tip; tracker rows 06/18/21; prune `stack/sprint28/*`. Full sln ≥801 closeout evidence.

## Acceptance Criteria

- [ ] `ReplayGoldenSuiteTests` — 6/6 PASS
- [ ] `dotnet test ProjectAegis.sln` — ≥801 @ closeout
- [ ] `production/qa/sprint-29-gitnexus-*.md` with nodes/edges
- [ ] `production/qa/smoke-sprint-29-closeout-*.md`
- [ ] Tracker rows 06, 18, 21 updated
- [ ] `stack/sprint28/*` prune documented (merged branches only)
- [ ] `sprint-status.yaml` closeout counters + evidence list
- [ ] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Full closeout gate
  - Given: all must-have stories merged
  - When: full sln + replay + gitnexus
  - Then: ≥801 PASS; replay 6/6; GitNexus indexed
  - Edge cases: should-have cut line applied; partial merge state

- **AC-2**: Stack prune hygiene
  - Given: merged `stack/sprint28/*` branches
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

- S28-13 pattern: `production/epics/sprint-28-closeout-devops/story-028-13-closeout-hygiene.md`
- Kickoff: `production/sprints/sprint-29-operationalize-data-fight-loop.md` (S29-13)
- Parallel kickoff: `production/agentic/sprint-29-parallel-kickoff-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-29-2026-10-02.md` *(create before implementation)*