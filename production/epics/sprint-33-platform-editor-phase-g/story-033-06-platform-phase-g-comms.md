---
id: S33-06
status: Complete
type: Integration
priority: must-have
graphite_branch: stack/sprint33/platform-phase-g-comms
estimate_days: 2
dependencies:
  - S33-01 green baseline
owner: team-unity
sprint: 33
req_trace: Req 21 Comms/LinkCatalog Phase A*; ADR-011
sprint_gate: true
---

# Story 033-06 — Platform Editor Phase G — Comms/Datalink Unity

> **Epic:** sprint-33-platform-editor-phase-g

## Summary

Extend `PlatformCatalogViewerHost` with comms fitting rows (`LinkId`, `Role`, `SatcomCapable`); import staging diff for comms edits; headless propose→approve round-trip. Schema-only — no sim datalink behavior.

## Acceptance Criteria

- [x] Viewer shows comms fittings per platform
- [x] Staging diff surfaces comms field deltas
- [x] Headless propose→approve tests PASS
- [x] Writes via `PlatformImportPanelHost` → `PlatformWorkbookWriteBridge` only
- [x] No new migrations; ZERO touch `DelegationBridge.cs`

## Verify Commands

```bash
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlatformImport|PlatformCatalogViewer|PlatformComms" -v minimal
npx gitnexus impact PlatformCatalogViewerHost
```