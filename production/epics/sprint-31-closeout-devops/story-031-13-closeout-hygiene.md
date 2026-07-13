---
id: S31-13
status: Complete
type: Config
priority: should-have
graphite_branch: stack/sprint31/closeout
estimate_days: 0.5
dependencies:
  - S31-03+ (must-have gate landed)
owner: c-sharp-devops-engineer
sprint: 31
req_trace: Sprint closeout DoD
last_updated: 2026-06-18
---

# Story 031-13 — Closeout Hygiene

> **Epic:** sprint-31-closeout-devops

## Summary

Replay 6/6; GitNexus @ stack tip; tracker rows 06/18/21; prune `stack/sprint30/*`. Full sln ≥996 closeout evidence.

## Acceptance Criteria

- [x] `ReplayGoldenSuiteTests` — 6/6 PASS
- [x] `dotnet test ProjectAegis.sln` — ≥996 @ closeout (**1006/1006**)
- [x] `production/qa/sprint-31-gitnexus-*.md` with nodes/edges (14,160 / 28,928)
- [x] `production/qa/smoke-sprint-31-closeout-*.md`
- [x] Tracker rows 06, 18, 21 updated
- [x] `stack/sprint30/*` prune documented (merged branches only; **0 local refs**)
- [x] `sprint-status.yaml` closeout counters + evidence list
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Full closeout gate
  - Given: all must-have stories merged
  - When: full sln + replay + gitnexus
  - Then: ≥996 PASS; replay 6/6; GitNexus indexed
  - Edge cases: should-have cut line applied; partial merge state

- **AC-2**: Stack prune hygiene
  - Given: merged `stack/sprint30/*` branches
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

- S30-13 pattern: `production/epics/sprint-30-closeout-devops/story-030-13-closeout-hygiene.md`
- Kickoff: `production/sprints/sprint-31-corpus-combat-polish.md` (S31-13)
- Parallel kickoff: `production/agentic/sprint-31-parallel-kickoff-2026-06-18.md`
- QA plan: `production/qa/qa-plan-sprint-31-2026-10-30.md`
- Done stack: `production/agentic/stacks/sprint31/S31-13-DONE.md`