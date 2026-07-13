---
id: S33-13
status: Complete
type: Config
priority: should-have
graphite_branch: stack/sprint33/closeout
estimate_days: 0.5
dependencies:
  - S33-02+ must-have landed
owner: c-sharp-devops-engineer
sprint: 33
req_trace: Sprint closeout; tracker rows 06/18/20/21
---

# Story 033-13 — Closeout Hygiene

> **Epic:** sprint-33-closeout-devops

## Summary

ReplayGolden 6/6; GitNexus @ tip; tracker rows 06/18/20/21; smoke ≥1086; prune `stack/sprint32/*`.

## Acceptance Criteria

- [x] `production/qa/smoke-sprint-33-closeout-*.md` @ ≥1086 — **1143/1143**
- [x] `production/qa/sprint-33-gitnexus-*.md` — 15,638 nodes / 32,132 edges
- [x] `stack/sprint32/*` prune documented (0 local refs)
- [x] Tracker row 06: DBI-1.5 + DBI-3.5 updated

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
npx gitnexus analyze . --force
```