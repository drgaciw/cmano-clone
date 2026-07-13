# Epic: Sprint 33 — Platform Editor Phase G (Comms/Datalink)

> **Status:** Ready  
> **Sprint:** 33  
> **GDD:** Req 21 Platform Editor Phase A* (`Comms`/`LinkCatalog` sheets)

## Goal

Surface **comms/datalink fitting rows** in Unity catalog viewer and import staging diff with headless propose→approve round-trip (schema-only; behavior in sim track).

## Stories

| # | Story | ID | Priority | Est. | Status |
|---|-------|-----|----------|------|--------|
| 06 | [Phase G comms Unity surfacing](story-033-06-platform-phase-g-comms.md) | S33-06 | must-have | 2d | Not Started |
| 10 | [Presentation evidence](story-033-10-presentation-evidence.md) | S33-10 | should-have | 1.5d | Not Started |

## Sprint gate

**S33-06** — comms workbook round-trip in Unity.

## GitNexus

- **HIGH:** `PlatformCatalogViewerHost`, `PlatformImportPanelHost`, `PlatformWorkbookWriteBridge`
- **CRITICAL extend-only:** `CatalogWriteGate`