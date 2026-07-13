---
id: S32-13
status: Complete
Last Updated: 2026-06-19
type: Config
priority: should-have
graphite_branch: stack/sprint32/closeout
estimate_days: 0.5
dependencies:
  - S32-02+ (must-have gate landed)
owner: c-sharp-devops-engineer
sprint: 32
req_trace: Sprint closeout DoD; ≥1046 closeout
---

# Story 032-13 — Closeout Hygiene

> **Epic:** sprint-32-closeout-devops

## Summary

Replay 6/6; GitNexus @ stack tip; tracker rows 06/18/20/21; prune `stack/sprint31/*`. Full sln **≥1046** closeout evidence.

## Acceptance Criteria

- [x] `ReplayGoldenSuiteTests` — 6/6 PASS
- [x] `dotnet test ProjectAegis.sln` — ≥1046 @ closeout (1073/1073)
- [x] `production/qa/sprint-32-gitnexus-*.md` with nodes/edges
- [x] `production/qa/smoke-sprint-32-closeout-*.md`
- [x] Tracker rows 06, 18, 20, 21 updated
- [x] `stack/sprint31/*` prune documented (merged branches only; **0 local refs**)
- [x] `sprint-status.yaml` closeout counters + evidence list
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Full closeout gate
  - Given: all must-have stories merged
  - When: full sln + replay + gitnexus
  - Then: ≥1046 PASS; replay 6/6; GitNexus indexed
  - Edge cases: should-have cut line applied; partial merge state

- **AC-2**: Stack prune hygiene
  - Given: merged `stack/sprint31/*` branches
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

- S31-13 pattern: `production/epics/sprint-31-closeout-devops/story-031-13-closeout-hygiene.md`
- Kickoff: `production/sprints/sprint-32-release-train-combat-phase6-platform-phase-f.md` (S32-13)
- Parallel kickoff: `production/agentic/sprint-32-parallel-kickoff-2026-06-18.md`
- QA plan: `production/qa/qa-plan-sprint-32-*.md` *(create before implementation)*