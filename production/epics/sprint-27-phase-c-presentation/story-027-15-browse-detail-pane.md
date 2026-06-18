---
id: S27-15
status: Ready
type: UI
priority: nice-to-have
graphite_branch: stack/sprint27/platform-viewer-detail
estimate_days: 0.5
dependencies:
  - S27-08 complete
owner: team-unity
sprint: 27
req_trace: Req 21 Phase C detail
---

# Story 027-15 — Platform Browse Detail Pane

> **Epic:** sprint-27-phase-c-presentation

## Summary

Row select shows `LatDeg`, `LonDeg`, `CombatRadiusNm`, `MaxHp`, `MaxSpeedKnots` from `CatalogPlatformBrowseRow`. Still read-only.

## Acceptance Criteria

- [ ] Detail pane binds selected row fields
- [ ] Headless test for selected-row projection bind
- [ ] No write-gate calls

## QA Test Cases

- **AC-1**: Detail bind
  - Given: row selected in viewer host test harness
  - When: projection updates
  - Then: detail fields match browse row values

## References

- Deferred from S26-10 spike
- QA plan: `production/qa/qa-plan-sprint-27-2026-06-18.md`