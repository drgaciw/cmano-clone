---
id: S32-12
status: Complete
Last Updated: 2026-06-19
type: Config
priority: nice-to-have
graphite_branch: stack/sprint32/closeout
estimate_days: 0.25
dependencies:
  - S32-01 green baseline
  - S31-12 carryover
owner: c-sharp-devops-engineer
sprint: 32
req_trace: S31-12 carryover; GHA billing advisory; ≥1006 baseline / ≥1046 closeout
---

# Story 032-12 — CI/Local Gate Refresh

> **Epic:** sprint-32-closeout-devops

## Summary

Update `verify-ci-local.ps1` evidence for **≥1046 closeout** baseline (day-1 ≥1006). Doc-only; non-blocking. Documents permanent local-gate advisory per producer decision (GHA billing). Third deferral acceptable per sprint cut line.

## Acceptance Criteria

- [x] `verify-ci-local.ps1` or equivalent local gate doc updated for ≥1006 baseline / ≥1046 closeout
- [x] Evidence doc: `production/qa/sprint-32-ci-hygiene-*.md`
- [x] Buildkite merge authority + local gate advisory documented
- [x] Non-blocking (does not gate sprint closeout)
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Local gate doc accuracy
  - Given: post-S32-01 baseline count (≥1006) and closeout target (≥1046)
  - When: local gate script/doc reviewed
  - Then: test count threshold ≥1006 day-1 / ≥1046 closeout; replay golden step included
  - Edge cases: PATH to dotnet; filter strings match sprint plan

- **AC-2**: S31-12 carryover documented
  - Given: S31-12 deferred backlog status
  - When: S32-12 evidence doc reviewed
  - Then: carryover rationale + deferral count recorded; non-blocking flag explicit
  - Edge cases: script unavailable in sandbox; bash fallback documented

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
# Doc refresh validation — run local gate once for evidence
pwsh -File tools/verify-ci-local.ps1 2>/dev/null || bash tools/buildkite/dotnet-ci.sh
dotnet test ProjectAegis.sln -v minimal | tail -5
```

## References

- S31-12 pattern: `production/epics/sprint-31-closeout-devops/story-031-12-ci-hygiene.md`
- S31 evidence: `production/qa/sprint-31-ci-hygiene-2026-06-18.md`
- Kickoff: `production/sprints/sprint-32-release-train-combat-phase6-platform-phase-f.md` (S32-12)
- Parallel kickoff: `production/agentic/sprint-32-parallel-kickoff-2026-06-18.md`
- QA plan: `production/qa/qa-plan-sprint-32-*.md` *(create before implementation)*