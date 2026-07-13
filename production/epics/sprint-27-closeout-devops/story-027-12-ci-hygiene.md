---
id: S27-12
status: Complete
Last Updated: 2026-06-18
type: Config
priority: nice-to-have
graphite_branch: stack/sprint27/closeout-gitnexus
estimate_days: 0.5
dependencies:
  - S27-01 green baseline
owner: c-sharp-devops-engineer
sprint: 27
req_trace: GHA billing open since S16
producer_approved: 2026-06-18 permanent local-gate advisory
---

# Story 027-12 — CI Hygiene / GHA Billing Documentation

> **Epic:** sprint-27-closeout-devops  
> **Producer decision:** Permanent local-gate advisory; non-blocking

## Summary

Document CI merge policy: Buildkite required; GHA CodeQL advisory; refresh `verify-ci-local.ps1` evidence protocol if needed.

## Acceptance Criteria

- [x] Evidence doc: `production/qa/sprint-27-ci-hygiene-*.md`
- [x] Buildkite = merge authority documented
- [x] Local gate fallback path documented
- [x] Non-blocking for sprint closeout

## References

- S16 triage: `production/qa/pr-69-ci-triage-2026-06-04.md`
- QA plan: `production/qa/qa-plan-sprint-27-2026-06-18.md`

## Completion Notes
**Completed**: 2026-06-18
**Criteria**: 4/4 passing
**Deviations**: None
**Test Evidence**: Config/Data — `production/qa/sprint-27-ci-hygiene-2026-06-18.md`
**Code Review**: Skipped (lean mode); documentation-only