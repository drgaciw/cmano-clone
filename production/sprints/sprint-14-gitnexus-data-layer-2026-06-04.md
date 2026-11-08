# Sprint 14 — GitNexus ProjectAegis.Data layer (doc-only)

**Date:** 2026-06-04  
**Scope:** `src/ProjectAegis.Data` — catalog read path, import pipeline, provenance tiers, write gate, snapshots  
**Action:** **No C# edits** — symbol map for `gitnexus impact` before any future data-layer PR.

## Risk

| Sprint type | Risk |
|-------------|------|
| Doc-only (this sprint) | **LOW** — no runtime or schema changes |
| Future PR touching SQLite schema / `CatalogEntityMap` / importers | **MEDIUM–HIGH** — consumers include scenario validation, Baltic seed bootstrap, intelligence agents |

## GitNexus CLI note

Sprint 13 reported `gitnexus impact` failures when multiple repos are indexed without a repo label. For this **cmano-clone** worktree, prefer:

```bash
npx gitnexus impact <Symbol> -d upstream -r cmano-clone
```

From the worktree root (or pass an explicit repo path if your CLI supports `--repo`):

```text
C:\Users\Username01\.grok\worktrees\mycode-cmano-clone\2026-06-04-bb909adc
```

Refresh index after large data-layer moves: `npx gitnexus analyze` (per `production/epics/wave5-engage-cyber-logistics-slice/EPIC.md`).

## Grep summary

- **Repository pattern:** No `*Repository` types; read access is `ICatalogReader` + SQLite/in-memory/null implementations.
- **Catalog:** Bindings, platform entries, entity→table map, archetype/near-future gates, quarantine.
- **Import:** JSON → SQLite (`CatalogJsonImporter`, `CatalogBulkImporter`, `CatalogImportGate`, `CatalogSeedBootstrap`).
- **Provenance:** `CatalogProvenanceTier` + `value_tier` on sensor rows (`CatalogSensorBinding.ValueTier`).
- **Write path:** Staged batches via `CatalogWriteGate` / `IWriteGate` (not direct table writes from importers in MVP flow).

## Key public symbols

| Symbol | Kind | File path |
|--------|------|-----------|
| `ICatalogReader` | interface | `src/ProjectAegis.Data/Catalog/ICatalogReader.cs` |
| `SqliteCatalogReader` | class | `src/ProjectAegis.Data/Catalog/SqliteCatalogReader.cs` |
| `InMemoryCatalogReader` | class | `src/ProjectAegis.Data/Catalog/InMemoryCatalogReader.cs` |
| `NullCatalogReader` | class | `src/ProjectAegis.Data/Catalog/NullCatalogReader.cs` |
| `CatalogReaderFactory` | static | `src/ProjectAegis.Data/Catalog/CatalogReaderFactory.cs` |
| `CatalogSensorBinding` | record | `src/ProjectAegis.Data/Catalog/CatalogSensorBinding.cs` |
| `CatalogPlatformEntry` | record | `src/ProjectAegis.Data/Catalog/CatalogPlatformEntry.cs` |
| `CatalogEntityMap` | static | `src/ProjectAegis.Data/Catalog/CatalogEntityMap.cs` |
| `CatalogValidationDefaults` | static | `src/ProjectAegis.Data/Catalog/CatalogValidationDefaults.cs` |
| `CatalogProvenanceTier` | static | `src/ProjectAegis.Data/Catalog/CatalogProvenanceTier.cs` |
| `CatalogChangeLogEntry` | record | `src/ProjectAegis.Data/Catalog/CatalogChangeLogEntry.cs` |
| `DbReleaseRecord` | record | `src/ProjectAegis.Data/Catalog/DbReleaseRecord.cs` |
| `CatalogJsonImporter` | static | `src/ProjectAegis.Data/Catalog/CatalogJsonImporter.cs` |
| `CatalogBulkImporter` | static | `src/ProjectAegis.Data/Catalog/CatalogBulkImporter.cs` |
| `CatalogImportGate` | static | `src/ProjectAegis.Data/Catalog/CatalogImportGate.cs` |
| `CatalogReviewStates` | static | `src/ProjectAegis.Data/Catalog/CatalogReviewStates.cs` |
| `CatalogSeedBootstrap` | static | `src/ProjectAegis.Data/Catalog/CatalogSeedBootstrap.cs` |
| `QuarantinedCatalogBinding` | record | `src/ProjectAegis.Data/Catalog/QuarantinedCatalogBinding.cs` |
| `CatalogQuarantinePromoter` | static | `src/ProjectAegis.Data/Catalog/CatalogQuarantinePromoter.cs` |
| `DetectionBindingKey` | *(file)* | `src/ProjectAegis.Data/Catalog/DetectionBindingKey.cs` |
| `IWriteGate` | interface | `src/ProjectAegis.Data/WriteGate/IWriteGate.cs` |
| `CatalogWriteGate` | class | `src/ProjectAegis.Data/WriteGate/CatalogWriteGate.cs` |
| `WriteGateDecision` | record | `src/ProjectAegis.Data/WriteGate/IWriteGate.cs` |
| `CatalogStagingBatchSummary` | record | `src/ProjectAegis.Data/WriteGate/IWriteGate.cs` |
| `ICatalogClock` | interface | `src/ProjectAegis.Data/WriteGate/ICatalogClock.cs` |
| `FixedCatalogClock` | class | `src/ProjectAegis.Data/WriteGate/ICatalogClock.cs` |
| `DbSnapshotStore` | class | `src/ProjectAegis.Data/Snapshots/DbSnapshotStore.cs` |
| `IDatabaseIntelligenceAgent` | interface | `src/ProjectAegis.Data/Agents/IDatabaseIntelligenceAgent.cs` |
| `DatabaseIntelligenceOrchestrator` | class | `src/ProjectAegis.Data/Agents/DatabaseIntelligenceOrchestrator.cs` |
| `CatalogConsistencyAgent` | class | `src/ProjectAegis.Data/Agents/CatalogConsistencyAgent.cs` |
| `CatalogEntityResolutionAgent` | class | `src/ProjectAegis.Data/Agents/CatalogEntityResolutionAgent.cs` |
| `CatalogRulesValidationAgent` | class | `src/ProjectAegis.Data/Agents/CatalogRulesValidationAgent.cs` |
| `CatalogDiffProposalAgent` | class | `src/ProjectAegis.Data/Agents/CatalogDiffProposalAgent.cs` |
| `IScenarioValidationEngine` | interface | `src/ProjectAegis.Data/Validation/ScenarioValidationEngine.cs` |
| `ScenarioValidationEngine` | class | `src/ProjectAegis.Data/Validation/ScenarioValidationEngine.cs` |
| `ScenarioValidationExportGate` | static | `src/ProjectAegis.Data/Validation/ScenarioValidationExportGate.cs` |

### Catalog — archetype / near-future (adjacent)

| Symbol | Kind | File path |
|--------|------|-----------|
| `CatalogArchetypeBinding` | record | `src/ProjectAegis.Data/Catalog/CatalogArchetypeBinding.cs` |
| `CatalogArchetypeGate` | static | `src/ProjectAegis.Data/Catalog/CatalogArchetypeGate.cs` |
| `NearFutureArchetypeCatalog` | static | `src/ProjectAegis.Data/Catalog/NearFutureArchetypeCatalog.cs` |
| `NearFutureArchetypeRuntime` | static | `src/ProjectAegis.Data/Catalog/NearFutureArchetypeRuntime.cs` |
| `SwarmTier` | enum | `src/ProjectAegis.Data/Catalog/SwarmTier.cs` |

### Scenario authoring (downstream of catalog)

| Symbol | Kind | File path |
|--------|------|-----------|
| `ScenarioDocumentDto` | class | `src/ProjectAegis.Data/Scenario/Authoring/ScenarioDocumentDto.cs` |
| `ScenarioMetadataDto` | class | `src/ProjectAegis.Data/Scenario/Authoring/ScenarioMetadataDto.cs` |
| `ScenarioDocumentEditor` | class | `src/ProjectAegis.Data/Scenario/Authoring/ScenarioDocumentEditor.cs` |

## Recommended impact targets (before code PR)

1. `ICatalogReader` / `SqliteCatalogReader` — scenario validation + sim consumers  
2. `CatalogJsonImporter` / `CatalogBulkImporter` — Baltic seed + file drops  
3. `CatalogWriteGate` — agent diff proposals  
4. `CatalogProvenanceTier` — sensor `value_tier` normalization  
5. `DbSnapshotStore` — `DbRef` / snapshot resolution (ADR-008)

## Sprint 14 action

Document-only symbol inventory. Re-run `gitnexus impact` with `-r cmano-clone` (or equivalent `--repo`) before merging any PR that changes public data-layer APIs or SQLite layout.