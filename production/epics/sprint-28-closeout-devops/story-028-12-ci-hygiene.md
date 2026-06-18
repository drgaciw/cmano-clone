---
id: S28-12
status: Not Started
type: Config
priority: nice-to-have
graphite_branch: stack/sprint28/closeout
estimate_days: 0.25
dependencies:
  - S28-01 green baseline
owner: c-sharp-devops-engineer
sprint: 28
req_trace: S27-12 carryover; GHA billing advisory
---

# Story 028-12 — CI/Local Gate Refresh

> **Epic:** sprint-28-closeout-devops

## Summary

Update `verify-ci-local.ps1` evidence for 741+ baseline. Doc-only; non-blocking. Documents permanent local-gate advisory per producer decision (GHA billing).

## Acceptance Criteria

- [ ] `verify-ci-local.ps1` or equivalent local gate doc updated for 741+ baseline
- [ ] Evidence doc: `production/qa/sprint-28-ci-hygiene-*.md`
- [ ] Buildkite merge authority + local gate advisory documented
- [ ] Non-blocking (does not gate sprint closeout)
- [ ] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Local gate doc accuracy
  - Given: post-S28-01 baseline count
  - When: local gate script/doc reviewed
  - Then: test count threshold ≥741; replay golden step included
  - Edge cases: PATH to dotnet; filter strings match sprint plan

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
# Doc refresh validation — run local gate once for evidence
pwsh -File tools/verify-ci-local.ps1 2>/dev/null || echo "verify script path per repo"
dotnet test ProjectAegis.sln -v minimal | tail -5
```

## References

- S27-12 pattern: `production/epics/sprint-27-closeout-devops/story-027-12-ci-hygiene.md`
- S27 evidence: `production/qa/sprint-27-ci-hygiene-2026-06-18.md`
- Kickoff: `production/sprints/sprint-28-corpus-write-combat-v2.md` (S28-12)
- QA plan: `production/qa/qa-plan-sprint-28-2026-09-18.md` *(create before implementation)*