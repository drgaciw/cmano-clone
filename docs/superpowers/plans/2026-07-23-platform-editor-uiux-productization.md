# Platform Editor UI/UX Productization Plan

> **Date:** 2026-07-23  
> **Governing docs:** [21-Platform-Editor.md](../../../Game-Requirements/requirements/21-Platform-Editor.md), [ADR-011](../../architecture/adr-011-platform-editor-excel-roundtrip.md), [interaction-patterns.md](../../../design/ux/interaction-patterns.md) (P-PE-01…P-PE-04), [art-bible.md](../../../design/art/art-bible.md)  
> **Scope:** Unity catalog viewer + import staging chrome polish only. Excel remains the write workbench. No WYSIWYG, no `CatalogWriteGate` write-path edits, no `DelegationBridge` hotpath.

## Goal

Close curator cognitive-load gaps (unified damage/comms/link UX) by fixing token aliases, colored staging diffs, section filters, progressive catalog disclosure, and wiring the half-implemented dependency graph ListView — while preserving ADR-011 Excel-primary authoring.

## Waves

| Wave | Content |
|------|---------|
| **PE-UX-W0** | Plan doc + `AegisTokens.uss` `--aegis-*` aliases + diff tokens; P-PE-04 interaction pattern |
| **PE-UX-W1** | `PlatformImportStagingRow` Section/DiffKind; Import UXML section filter; colored rows; blocked status class |
| **PE-UX-W2** | Catalog section bar; mounts/sensors detail; demote Lat/Lon; wire graph ListView; Export/Diff status |
| **PE-UX-W3** | Headless Platform* tests + gate |

## Invariants

- ReplayGolden 6/6; hash `17144800277401907079`
- C2 proxy ≥20/20; suite floor ≥1638 monotonic
- `CatalogWriteGate` extend-only / no write-path edits in this program
- `DelegationBridge.cs` ZERO hotpath

## Architecture

```
Excel → PlatformImportPanelHost → PlatformImportStagingProjection → PlatformWorkbookWriteBridge → CatalogWriteGate
ICatalogReader → Catalog*Projection → PlatformCatalogViewerHost (read-only)
```

Hosts stay thin; presentation kinds live in `ProjectAegis.Delegation.Projection`.

## Success criteria

1. `--aegis-*` tokens resolve; diff row colors match art bible  
2. Import: section filter + colored kinds + blocked class toggled  
3. Catalog: sections; graph ListView bound; mounts/sensors in detail; Lat/Lon demoted  
4. Export/Diff/Propose feedback visible in-panel  
5. Headless Platform* tests green; no WriteGate/Bridge production diffs  
6. **PE-UX-W4:** `PlatformEditorShellHost` Catalog|Import tabs; staging preserved across switch  
7. **PE-UX-W5:** Comms→Links jump; health strip; graph focus/search beyond cap  
8. **PE-UX-W6:** text tags on diffs; scale + `.reduced-motion` USS hooks; arrow-key section chips  

## Wave status (2026-07-23 execution)

| Wave | Status |
|------|--------|
| PE-UX-W0…W3 | Green (existing dirty tree + Platform* tests) |
| PE-UX-W4 shell | Shipped (`PlatformEditorShell*` + shell projection) |
| PE-UX-W5 curator | Shipped (graph projection, health strip, comms→links) |
| PE-UX-W6 a11y | Shipped (`C2AccessibilitySettings`, text tags, keyboard chips) |
