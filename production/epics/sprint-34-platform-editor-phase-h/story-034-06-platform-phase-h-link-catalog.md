---
id: S34-06
status: Complete
Last Updated: 2026-06-19
type: Integration
priority: must-have
graphite_branch: stack/sprint34/platform-phase-h-link-catalog
estimate_days: 2
dependencies:
  - S34-01 green baseline
  - S34-02 merged
  - S34-03 merged
owner: team-unity
sprint: 34
req_trace: Req 21 LinkCatalog Phase A*; ADR-011; ADR-010
sprint_gate: true
---

# Story 034-06 — Platform Editor Phase H — LinkCatalog Unity

> **Epic:** sprint-34-platform-editor-phase-h

## Summary

Extend `PlatformCatalogViewerHost` with LinkCatalog list (`LinkId`, `DisplayName`, `LinkType`, `LatencyMsNominal`); import staging diff for link edits; headless propose→approve round-trip. Comms rows resolve link `DisplayName` when present. Schema-only.

## Acceptance Criteria

- [x] Viewer shows LinkCatalog reference rows
- [x] Staging diff surfaces link field deltas
- [x] Headless propose→approve tests PASS (`PlatformLinkCatalogTests`)
- [x] Writes via `PlatformImportPanelHost` → `PlatformWorkbookWriteBridge` only
- [x] ZERO touch `DelegationBridge.cs`