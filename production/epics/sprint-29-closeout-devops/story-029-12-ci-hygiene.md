---
id: S29-12
status: Not Started
type: Config
priority: nice-to-have
graphite_branch: stack/sprint29/closeout
estimate_days: 0.25
dependencies:
  - S29-01 green baseline
owner: c-sharp-devops-engineer
sprint: 29
req_trace: S28-12 carryover; GHA billing advisory
producer_approved: 2026-06-18 permanent local-gate advisory
---

# Story 029-12 — CI/Local Gate Refresh

> **Epic:** sprint-29-closeout-devops

## Summary

Update `verify-ci-local.ps1` evidence for 801+ baseline. Doc-only; non-blocking. Documents permanent local-gate advisory per producer decision (GHA billing).

## Acceptance Criteria

- [ ] `verify-ci-local.ps1` or equivalent local gate doc updated for 801+ baseline
- [ ] Evidence doc: `production/qa/sprint-29-ci-hygiene-*.md`
- [ ] Buildkite merge authority + local gate advisory documented
- [ ] Non-blocking (does not gate sprint closeout)
- [ ] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Local gate doc accuracy
  - Given: post-S29-01 baseline count
  - When: local gate script/doc reviewed
  - Then: test count threshold ≥801; replay golden step included
  - Edge cases: PATH to dotnet; filter strings match sprint plan

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
# Doc refresh validation — run local gate once for evidence
pwsh -File tools/verify-ci-local.ps1 2>/dev/null || bash tools/buildkite/dotnet-ci.sh
dotnet test ProjectAegis.sln -v minimal | tail -5
```

## References

- S28-12 pattern: `production/epics/sprint-28-closeout-devops/story-028-12-ci-hygiene.md`
- S28 evidence: `production/qa/sprint-28-ci-hygiene-2026-06-18.md`
- Kickoff: `production/sprints/sprint-29-operationalize-data-fight-loop.md` (S29-12)
- QA plan: `production/qa/qa-plan-sprint-29-2026-10-02.md` *(create before implementation)*