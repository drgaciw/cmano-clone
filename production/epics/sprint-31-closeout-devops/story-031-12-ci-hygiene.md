---
id: S31-12
status: Not Started
type: Config
priority: nice-to-have
graphite_branch: stack/sprint31/closeout
estimate_days: 0.25
dependencies:
  - S31-01 green baseline
owner: c-sharp-devops-engineer
sprint: 31
req_trace: S30-12 carryover; GHA billing advisory
producer_approved: 2026-06-18 permanent local-gate advisory
last_updated: 2026-06-18
---

# Story 031-12 — CI/Local Gate Refresh

> **Epic:** sprint-31-closeout-devops

## Summary

Update `verify-ci-local.ps1` evidence for 996 closeout baseline (day-1 ≥956). Doc-only; non-blocking. Documents permanent local-gate advisory per producer decision (GHA billing).

## Acceptance Criteria

- [ ] `verify-ci-local.ps1` or equivalent local gate doc updated for ≥956 baseline / ≥996 closeout
- [ ] Evidence doc: `production/qa/sprint-31-ci-hygiene-2026-06-18.md`
- [ ] Buildkite merge authority + local gate advisory documented
- [ ] Non-blocking (does not gate sprint closeout)
- [ ] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Local gate doc accuracy
  - Given: post-S31-01 baseline count (≥956) and closeout target (≥996)
  - When: local gate script/doc reviewed
  - Then: test count threshold ≥956 day-1 / ≥996 closeout; replay golden step included
  - Edge cases: PATH to dotnet; filter strings match sprint plan

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
# Doc refresh validation — run local gate once for evidence
pwsh -File tools/verify-ci-local.ps1 2>/dev/null || bash tools/buildkite/dotnet-ci.sh
dotnet test ProjectAegis.sln -v minimal | tail -5
```

## References

- S30-12 pattern: `production/epics/sprint-30-closeout-devops/story-030-12-ci-hygiene.md`
- S30 evidence: `production/qa/sprint-30-ci-hygiene-2026-06-18.md`
- Kickoff: `production/sprints/sprint-31-corpus-combat-polish.md` (S31-12)
- Parallel kickoff: `production/agentic/sprint-31-parallel-kickoff-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-31-2026-10-30.md` *(create before implementation)*