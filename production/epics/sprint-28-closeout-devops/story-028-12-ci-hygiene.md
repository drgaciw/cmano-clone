---
id: S28-12
status: Complete
Last Updated: 2026-06-18
type: Config
priority: nice-to-have
graphite_branch: stack/sprint28/closeout
estimate_days: 0.25
dependencies:
  - S28-01 green baseline
owner: c-sharp-devops-engineer
sprint: 28
req_trace: S27-12 carryover; GHA billing advisory
producer_approved: 2026-06-18 permanent local-gate advisory
---

# Story 028-12 — CI/Local Gate Refresh

> **Epic:** sprint-28-closeout-devops

## Summary

Update `verify-ci-local.ps1` evidence for 787+ baseline (wave-2 trunk). Doc-only; non-blocking. Documents permanent local-gate advisory per producer decision (GHA billing).

## Acceptance Criteria

- [x] `verify-ci-local.ps1` or equivalent local gate doc updated for 787+ baseline
- [x] Evidence doc: `production/qa/sprint-28-ci-hygiene-*.md`
- [x] Buildkite merge authority + local gate advisory documented
- [x] Non-blocking (does not gate sprint closeout)
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Local gate doc accuracy
  - Given: post-S28-01 baseline count
  - When: local gate script/doc reviewed
  - Then: test count threshold ≥787; replay golden step included
  - Edge cases: PATH to dotnet; filter strings match sprint plan

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
# Doc refresh validation — run local gate once for evidence
pwsh -File tools/verify-ci-local.ps1 2>/dev/null || bash tools/buildkite/dotnet-ci.sh
dotnet test ProjectAegis.sln -v minimal | tail -5
```

## References

- S27-12 pattern: `production/epics/sprint-27-closeout-devops/story-027-12-ci-hygiene.md`
- S27 evidence: `production/qa/sprint-27-ci-hygiene-2026-06-18.md`
- S28 evidence: `production/qa/sprint-28-ci-hygiene-2026-06-18.md`
- Kickoff: `production/sprints/sprint-28-corpus-write-combat-v2.md` (S28-12)
- QA plan: `production/qa/qa-plan-sprint-28-2026-09-18.md`

## Completion Notes
**Completed**: 2026-06-18
**Criteria**: 5/5 passing
**Deviations**: `pwsh` unavailable on verification host; bash parity `dotnet-ci.sh` used for evidence
**Test Evidence**: Config/Data — `production/qa/sprint-28-ci-hygiene-2026-06-18.md`
**Code Review**: Skipped (lean mode); documentation-only