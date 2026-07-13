---
id: S33-12
status: Complete
Last Updated: 2026-06-19
type: Config
priority: nice-to-have
graphite_branch: stack/sprint33/ci-hygiene
estimate_days: 0.25
dependencies:
  - S33-01
owner: c-sharp-devops-engineer
sprint: 33
req_trace: DevOps hygiene; S32-12 carryover; ≥1046 baseline / ≥1086 closeout
---

# Story 033-12 — CI/Local Gate Refresh

> **Epic:** sprint-33-closeout-devops

## Summary

Update `verify-ci-local.ps1` evidence for **≥1086 closeout** baseline (day-1 ≥1046). Doc-only; non-blocking. Documents permanent local-gate advisory per producer decision (GHA billing). Fourth deferral acceptable per sprint cut line.

## Acceptance Criteria

- [x] `verify-ci-local.ps1` or equivalent local gate doc updated for ≥1046 baseline / ≥1086 closeout
- [x] Evidence doc: `production/qa/sprint-33-ci-hygiene-*.md`
- [x] Buildkite merge authority + local gate advisory documented
- [x] Non-blocking (does not gate sprint closeout)
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Local gate doc accuracy
  - Given: post-S33-01 baseline count (≥1073) and closeout target (≥1086)
  - When: local gate script/doc reviewed
  - Then: test count threshold ≥1046 day-1 / ≥1086 closeout; replay golden step included
  - Edge cases: PATH to dotnet; filter strings match sprint plan

- **AC-2**: S32-12 carryover documented
  - Given: S32-12 completed status
  - When: S33-12 evidence doc reviewed
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

- S32-12 pattern: `production/epics/sprint-32-closeout-devops/story-032-12-ci-hygiene.md`
- S32 evidence: `production/qa/sprint-32-ci-hygiene-2026-06-19.md`
- Kickoff: `production/sprints/sprint-33-kill-chain-intelligence-comms-integration.md` (S33-12)
- Parallel kickoff: `production/agentic/sprint-33-parallel-kickoff-2026-06-19.md`
- QA plan: `production/qa/qa-plan-sprint-33-2026-11-27.md`