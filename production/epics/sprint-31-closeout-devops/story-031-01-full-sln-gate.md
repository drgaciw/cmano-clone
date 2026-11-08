---
id: S31-01
status: Complete
type: Config
priority: must-have
graphite_branch: stack/sprint31/full-sln-gate
estimate_days: 1
dependencies:
  - S30 complete @ 3406bc4
owner: c-sharp-devops-engineer
sprint: 31
req_trace: Sprint hygiene; kickoff DoD
last_updated: 2026-06-18
---

# Story 031-01 — Full-Solution Re-Baseline

> **Epic:** sprint-31-closeout-devops  
> **Sprint:** 31 — Corpus Complete, Combat Phase 5 & Presentation Polish

## Summary

Day-1 gate: `dotnet build` + `dotnet test ProjectAegis.sln` @ trunk; record ≥956 in `sprint-status.yaml` and smoke evidence; GitNexus analyze @ HEAD. Blocks S31-02+ feature work until green.

## Acceptance Criteria

- [x] `dotnet build ProjectAegis.sln` — 0 errors
- [x] `dotnet test ProjectAegis.sln` — ≥956/956 PASS
- [x] `ReplayGoldenSuiteTests` — 6/6 PASS
- [x] `npx gitnexus analyze . --force` completes (nodes/edges recorded)
- [x] Evidence: `production/qa/smoke-sprint-31-baseline-2026-06-18.md`
- [x] `sprint-status.yaml`: `tests_passed_sprint31_baseline`, indexed commit
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Full solution green
  - Given: trunk @ post-S30 merge (`3406bc4` / stack tip)
  - When: build + test + replay golden + gitnexus
  - Then: 0 failures; count ≥956; replay 6/6
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

- Kickoff: `production/sprints/sprint-31-corpus-combat-polish.md` (S31-01)
- S30-01 pattern: `production/epics/sprint-30-closeout-devops/story-030-01-full-sln-gate.md`
- QA plan: `production/qa/qa-plan-sprint-31-2026-10-30.md` *(create before implementation)*