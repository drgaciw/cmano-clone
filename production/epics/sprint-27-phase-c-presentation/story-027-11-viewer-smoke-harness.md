---
id: S27-11
status: Complete
Last Updated: 2026-06-18
type: Integration
priority: should-have
graphite_branch: stack/sprint27/platform-viewer-harness
estimate_days: 0.5
dependencies:
  - S27-08 complete
owner: team-unity
sprint: 27
req_trace: ADR-010 headless proxy
---

# Story 027-11 — Platform Viewer Smoke Harness

> **Epic:** sprint-27-phase-c-presentation

## Summary

Add PlayMode smoke row or scene integration binding Baltic fixture via `ICatalogReader`; headless proxy validates sorted rows, filter behavior, and write-gate bypass grep.

## Acceptance Criteria

- [x] `PlayModeSmoke` or harness row for platform catalog viewer
- [x] Sorted rows + filter narrows list (headless)
- [x] `rg` grep: no `CatalogWriteGate` in viewer host
- [x] Full sln regression green

## QA Test Cases

- **AC-1**: Harness row PASS
  - Given: headless PlayMode harness
  - When: platform catalog smoke runs
  - Then: bind + filter assertions PASS

## Verify Commands

```bash
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlatformCatalogViewer|PlayModeSmoke" -v minimal
```

## References

- QA plan: `production/qa/qa-plan-sprint-27-2026-06-18.md`

## Completion Notes
**Completed**: 2026-06-18
**Criteria**: 4/4 passing
**Deviations**: None
**Test Evidence**: Integration — `PlayModeSmokeHarnessTests` + `PlatformCatalogViewer` 22/22 filter
**Code Review**: Skipped (lean mode)