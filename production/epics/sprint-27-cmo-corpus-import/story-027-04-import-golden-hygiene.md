---
id: S27-04
status: Complete
type: Integration
priority: must-have
graphite_branch: stack/sprint27/import-golden-hygiene
estimate_days: 1
dependencies:
  - S27-03 complete
owner: team-data
sprint: 27
req_trace: Req 06 DBI-4.4 empty-diff; Req 21 PLE-2.3
---

# Story 027-04 — Import E2E + Golden Hygiene

> **Epic:** sprint-27-cmo-corpus-import  
> **Sprint gate:** Sprint 27 **fails** if this story does not land.

## Summary

Extend `CmoMarkdownImportGoldenTests` with loadout/magazine rows; pin golden hashes; full WriteGate regression; ReplayGolden 6/6 unchanged.

## Acceptance Criteria

- [x] `CmoMarkdownImportGoldenTests` updated with non-empty loadouts/magazines + hash pin
- [x] Re-import identical slice → stable ordering hash
- [x] WriteGate regression: sensor + Phase A/B + damage PASS
- [x] `ReplayGoldenSuiteTests` — 6/6 unchanged
- [x] Sensor-only CLI path regression unchanged

## QA Test Cases

- **AC-1**: Golden hash stable
  - Given: approved Baltic import state
  - When: re-import same markdown slice
  - Then: empty diff or identical hash
  - Edge cases: hash drift from sort key change

## Verify Commands

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "CmoMarkdown|WriteGate|CatalogSortKey" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
```

## References

- S26-04 pattern: `production/agentic/stacks/sprint26/S26-04-DONE.md`
- QA plan: `production/qa/qa-plan-sprint-27-2026-06-18.md`

## Completion Notes
**Completed**: 2026-06-18
**Criteria**: 5/5 passing
**Deviations**: None
**Test Evidence**: `src/ProjectAegis.Data.Tests/Import/CmoMarkdownImportGoldenTests.cs`
**Code Review**: Skipped (lean mode)