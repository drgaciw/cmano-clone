---
id: S30-12
status: Complete
type: Config
priority: nice-to-have
graphite_branch: stack/sprint30/closeout
estimate_days: 0.25
dependencies:
  - S30-01 green baseline
owner: c-sharp-devops-engineer
sprint: 30
req_trace: S29-12 carryover; GHA billing advisory
producer_approved: 2026-06-18 permanent local-gate advisory
last_updated: 2026-06-18
---

# Story 030-12 — CI/Local Gate Refresh

> **Epic:** sprint-30-closeout-devops

## Summary

Update `verify-ci-local.ps1` evidence for 918+ closeout baseline. Doc-only; non-blocking. Documents permanent local-gate advisory per producer decision (GHA billing).

## Acceptance Criteria

- [x] `verify-ci-local.ps1` or equivalent local gate doc updated for ≥918 baseline
- [x] Evidence doc: `production/qa/sprint-30-ci-hygiene-2026-06-18.md`
- [x] Buildkite merge authority + local gate advisory documented
- [x] Non-blocking (does not gate sprint closeout)
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Local gate doc accuracy
  - Given: post-S30-01 baseline count (878) and closeout target (≥918)
  - When: local gate script/doc reviewed
  - Then: test count threshold ≥918; replay golden step included
  - Edge cases: PATH to dotnet; filter strings match sprint plan

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
# Doc refresh validation — run local gate once for evidence
pwsh -File tools/verify-ci-local.ps1 2>/dev/null || bash tools/buildkite/dotnet-ci.sh
dotnet test ProjectAegis.sln -v minimal | tail -5
```

## References

- S29-12 pattern: `production/epics/sprint-29-closeout-devops/story-029-12-ci-hygiene.md`
- S29 evidence: `production/qa/sprint-29-ci-hygiene-2026-06-18.md`
- Kickoff: `production/sprints/sprint-30-tl-bind-corpus-scale.md` (S30-12)
- Parallel kickoff: `production/agentic/sprint-30-parallel-kickoff-2026-06-18.md`
- QA plan: `production/qa/qa-plan-sprint-30-2026-10-16.md`