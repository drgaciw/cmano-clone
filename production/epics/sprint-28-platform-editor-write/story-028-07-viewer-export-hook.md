---
id: S28-07
status: Complete
type: Integration
priority: should-have
graphite_branch: stack/sprint28/platform-write-ui
estimate_days: 1
dependencies:
  - S28-04 complete
owner: team-unity
sprint: 28
req_trace: Req 21 Platform Editor; ADR-011 Phase C→D bridge
---

# Story 028-07 — Platform Viewer Export/Diff Hook

> **Epic:** sprint-28-platform-editor-write  
> **ADR:** ADR-011 (viewer read-only export trigger), ADR-010 (headless-first)

## Summary

Read-only export trigger from Phase C platform catalog viewer; defers import/write UI to CLI. Headless export path test; no write-gate bypass in viewer host.

## Acceptance Criteria

- [x] Viewer panel exposes export/diff trigger (read-only — no direct writes)
- [x] Export invokes CLI or data API path (not raw SQLite)
- [x] Headless export path test PASS
- [x] No `CatalogWriteGate` / `IWriteGate` bypass in viewer host
- [x] Import UI deferred to CLI authority (ADR-011 Excel-primary)
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Headless export trigger
  - Given: PlatformCatalogViewerHost with Baltic fixture
  - When: export trigger invoked
  - Then: workbook or diff artifact produced via approved API
  - Edge cases: empty catalog; filter-active browse state

- **AC-2**: No write path in viewer
  - Given: viewer host source
  - When: grep for write-gate or SQLite direct calls
  - Then: export/diff only; writes remain CLI/Phase D path
  - Edge cases: accidental propose call from UI button

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlatformCatalog|Excel|PlayModeSmoke" -v minimal
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "Platform" -v minimal
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `CatalogPlatformBrowseProjection` | HIGH |
| `CatalogWriteGate` | CRITICAL — no bypass in viewer |
| `DelegationBridge.cs` | ZERO touch |

## References

- S27-08 pattern: `production/epics/sprint-27-phase-c-presentation/story-027-08-platform-viewer-panel.md`
- S26-10 spike: `production/qa/sprint-26-platform-viewer-spike-2026-06-18.md`
- Kickoff: `production/sprints/sprint-28-corpus-write-combat-v2.md` (S28-07)
- QA plan: `production/qa/qa-plan-sprint-28-2026-09-18.md` *(create before implementation)*

## Completion Notes

**Completed**: 2026-06-18  
**Criteria**: All AC passing  
**Deviations**: Optional Editor screenshot not captured (headless export path satisfies merge gate)  
**Test Evidence**: Integration — `PlatformCatalogExportBridgeTests`, `PlatformCatalogViewerTests`; `production/agentic/stacks/sprint28/S28-07-DONE.md`  
**Code Review**: Skipped (lean mode)