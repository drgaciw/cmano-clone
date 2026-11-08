---
id: S27-13
status: Complete
type: Config
priority: should-have
graphite_branch: stack/sprint27/closeout-gitnexus
estimate_days: 0.5
dependencies:
  - S27-04+ (must-have gate landed)
owner: c-sharp-devops-engineer
sprint: 27
req_trace: Sprint closeout DoD
---

# Story 027-13 — Closeout Hygiene

> **Epic:** sprint-27-closeout-devops

## Summary

Replay 6/6; GitNexus @ stack tip; tracker rows 06/18/21; prune merged `stack/sprint26/*`; full sln ≥698 closeout evidence.

## Acceptance Criteria

- [x] `ReplayGoldenSuiteTests` — 6/6 PASS
- [x] `dotnet test ProjectAegis.sln` — ≥698 @ closeout (741/741)
- [x] `production/qa/sprint-27-gitnexus-*.md` with nodes/edges
- [x] `production/qa/smoke-sprint-27-closeout-*.md`
- [x] Tracker rows 06, 18, 21 updated
- [x] `stack/sprint26/*` prune documented (merged branches only)
- [x] `sprint-status.yaml` closeout counters + evidence list

## Verify Commands

```bash
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
npx gitnexus analyze . --force
```

## References

- S26-11 pattern: `production/agentic/stacks/sprint26/S26-11-DONE.md`
- QA plan: `production/qa/qa-plan-sprint-27-2026-06-18.md`