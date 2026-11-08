# Platform Design Assistant

**Status:** Headless production path (DRG-73 / PDA-04)  
**Gate:** ADR-011 Excel-primary + CatalogWriteGate extend-only  
**Namespace:** `ProjectAegis.Data.PlatformAssistant`

## Purpose

Draft **future / hypothetical** platform archetypes **relative to existing catalog peers**, then stage them as **extend-only** write-gate batches. This is a **proposal agent**, not a WYSIWYG Platform Editor.

## Skills

| Skill | Role |
| --- | --- |
| catalog-grounding | Peers from `ICatalogReader.LoadExportData()` |
| archetype-schema | `CatalogPlatformBinding` + damage + mobility |
| relative-scaling | Role weight light 0.25 / standard 0.5 / heavy 0.75 |
| provenance | `CitationRef = assistant:{peerIds}` |
| gate-policy | New ids only; stage via Propose*Batch |
| workbook-emit | Optional Excel append via `PlatformDesignWorkbookEmitter` |
| what-if | TRL 5 + staged until ApproveBatch |

## API

```csharp
var assistant = new PlatformDesignAssistant();
var draft = assistant.Draft(catalog, brief);
var staged = assistant.Propose(dbPath, catalog, brief, clock);
// Approve platform batch BEFORE damage/mobility (FK).
writeService.ApproveBatches(dbPath, [staged.PlatformBatchId], clock, "human", "reviewer");
writeService.ApproveBatches(dbPath, [staged.DamageBatchId], clock, "human", "reviewer");
```

Scaled `LatDeg` / `LonDeg` / `CombatRadiusNm` ride on `CatalogPlatformBinding` with
`ApplyCorePosition: true` so `ApproveBatch` writes them into live `platform` rows
(migration `015_platform_staging_core_position.sql`). Callers that leave
`ApplyCorePosition` false keep the historical UpsertPlatform behavior
(existing lat/lon/radius, else `0/0/1.0` for new rows).

Host bridge: `PlatformDesignAssistantBridge` (UnityAdapter).  
CLI verb: `platform_design_propose` (also registered in `tools/mission-editor/mcp-tools.json`).

## CLI

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- platform_design_propose \
  --db data/baltic.db \
  --id opv-scout \
  --name "OPV Baltic Scout" \
  --domain surface \
  --role light \
  --concept "coastal patrol" \
  --what-if
```

Flags: `--draft-only`, `--no-what-if`, `--peer <id>` (repeatable), `--actor-type`, `--actor-id`, `--clock`.

`--clock <UtcTicks>` is optional and intended for **deterministic tests**. When omitted,
the CLI uses `DateTime.UtcNow.Ticks` so consecutive proposes get unique batch ids
(avoids FixedCatalogClock(0) overwrite collisions).

## Invariants

- Core position fields are opt-in via `ApplyCorePosition` (minimal gate extension only).
- **No** `DelegationBridge` touch.
- Unedited workbook empty-diff golden remains green.
- Live catalog unchanged until explicit ApproveBatch.
