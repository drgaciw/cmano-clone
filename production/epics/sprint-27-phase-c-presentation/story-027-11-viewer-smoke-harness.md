---
id: S27-11
status: Ready
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

- [ ] `PlayModeSmoke` or harness row for platform catalog viewer
- [ ] Sorted rows + filter narrows list (headless)
- [ ] `rg` grep: no `CatalogWriteGate` in viewer host
- [ ] Full sln regression green

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