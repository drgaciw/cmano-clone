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

Host bridge: `PlatformDesignAssistantBridge` (UnityAdapter).  
CLI verb: `platform_design_propose`.

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

## Invariants

- **No** edits to `CatalogWriteGate` write paths from this feature (consume Propose* only).
- **No** `DelegationBridge` touch.
- Unedited workbook empty-diff golden remains green.
- Live catalog unchanged until explicit ApproveBatch.
