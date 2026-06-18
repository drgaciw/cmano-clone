# ProjectAegis.Data — Database Intelligence Layer

The catalog and scenario data layer for Project Aegis. Implements **requirement 06
(Database Intelligence)** — [`06-Database-Intelligence.md`](../../Game-Requirements/requirements/06-Database-Intelligence.md) —
under the data-layer boundary set by [ADR-006](../../docs/architecture/adr-006-data-layer-boundary.md).

This assembly owns how authoritative platform/weapon/sensor data and scenarios are
**read, validated, staged, and committed**. It does not run the simulation; the sim
consumes the snapshots this layer produces.

## Core invariant: the write gate

**Every catalog mutation flows through `WriteGate.IWriteGate` as a staged
`propose → approve → commit` batch.** No subsystem writes catalog rows directly.

```
importer / agent / OSINT / editor
        │  Propose*Batch(...)            → returns batchId, rows land in *_quarantine
        ▼
   IWriteGate  ── ListPendingBatches()   → human/TL review
        │  ApproveBatch(batchId, ...)    → commits into live tables
        │  RejectBatch(batchId, ...)     → drops the batch
        ▼
   committed catalog (SQLite)
```

- Proposers (`CmoMarkdownImportProposer`, `OsintCatalogMapper`, `PlatformWorkbookImporter`,
  the agents) **stage** rows; they never call SQLite write paths themselves.
- `CatalogWriteGate` is the SQLite-backed implementation. Batches are sorted by
  canonical keys before insert, so staging is order-independent and deterministic.
- Supported batch kinds today: sensors, mounts, loadouts, magazines, comms, platforms,
  weapons (see `IWriteGate`).

When you add a new staged entity, extend `IWriteGate` with a `Propose*Batch` method —
do **not** add a side-channel write.

## Subsystem map

| Directory | Responsibility | Key entry points |
|-----------|----------------|------------------|
| `WriteGate/` | The propose→approve→commit gate (ADR-006 / req-06). | `IWriteGate`, `CatalogWriteGate`, `ICatalogClock` |
| `Catalog/` | Catalog records, bindings, readers, and import/quarantine plumbing. | `ICatalogReader`, `SqliteCatalogReader`, `CatalogQuarantinePromoter`, `CatalogBulkImporter` |
| `Import/` | Parse CMO markdown (cmano-db.com exports) and stage rows. | `CmoMarkdownImporter`, `CmoMarkdownImportProposer` |
| `Osint/` | OSINT discovery → catalog proposals with confidence + TRL/doc routing. | `OsintCatalogMapper`, `OsintProposalGate`, `OsintDigestRunner`, `Connectors/` |
| `Platform/` | Excel/workbook round-trip for the platform editor (ADR-011). | `PlatformWorkbookExporter`, `PlatformWorkbookImporter`, `PlatformWorkbookDiff`, `PlatformWorkbookValidator` — see [`Platform/README.md`](Platform/README.md) |
| `Validation/` | Deterministic scenario validation engine (ADR-008) + catalog rule pipeline. | `ScenarioValidationEngine`, `ValidationPipeline`, `Rules/` |
| `Agents/` | Headless, deterministic req-06 agent pipeline (no LLM in the tick path). | `IDatabaseIntelligenceAgent`, `DatabaseIntelligenceOrchestrator` |
| `Scenario/` | Load and resolve scenario packages, policy catalogs, readiness maps. | `ScenarioPackageLoader`, `ScenarioPolicyJsonCatalog` |
| `Snapshots/` | Bind, hash, and read deterministic catalog snapshots + release-train metadata. | `DbSnapshotStore`, `CatalogSnapshotBinder`, `CatalogSnapshotHasher` |
| `Telemetry/` | Advisory win-rate balance drift detection (DBI-5). | `IBalanceTelemetrySink` — see [`Telemetry/README.md`](Telemetry/README.md) |

## Cross-cutting rules

- **Determinism.** Readers, hashers, and exporters sort by canonical keys and format
  with `CultureInfo.InvariantCulture`. Iteration order must not depend on insertion
  order — CI pins golden hashes (`*GoldenHashes`, e.g. `ValidationGoldenHashes`,
  `BalanceTelemetryGoldenHashes`). If you intentionally change state shape or hashing,
  re-run the matching golden test and update the pinned constant.
- **No LLM / no wall-clock in the deterministic path.** The agent pipeline
  (`DatabaseIntelligenceOrchestrator`) is headless and reproducible; time comes from
  `ICatalogClock`, not `DateTime.UtcNow`.
- **Advisory vs. authoritative.** Telemetry and OSINT findings are advisory — they
  produce proposals/flags only. Promotion to the live catalog is always a separate,
  human/TL-approved write-gate batch.
- **Provenance & gating.** Staged rows carry provenance (`CatalogProvenanceTier`,
  source fact ids) and review state; OSINT rows additionally route by TRL and target
  doc (`OsintCatalogMapper.ResolveTrlLevel` / `ResolveBranchTag`).

## Tests

`src/ProjectAegis.Data.Tests/` mirrors this layout (`Catalog/`, `Import/`, `Osint/`,
`Platform/`, `Scenario/`, `Snapshots/`, `Agents/`, `Telemetry/`). Run from repo root:

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj -v minimal
```

## See also

- [ADR-006 — data-layer boundary](../../docs/architecture/adr-006-data-layer-boundary.md)
- [ADR-008 — mission-editor validation engine](../../docs/architecture/adr-008-mission-editor-validation-engine.md)
- [ADR-011 — platform editor Excel round-trip](../../docs/architecture/adr-011-platform-editor-excel-roundtrip.md)
- [Requirement 06 — Database Intelligence](../../Game-Requirements/requirements/06-Database-Intelligence.md)
- [Balance telemetry sink (DBI-5)](Telemetry/README.md)
