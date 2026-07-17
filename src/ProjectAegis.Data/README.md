# ProjectAegis.Data

The **data / catalog layer** for Project Aegis — the auditable source of truth for platform,
sensor, weapon, and scenario data that `ProjectAegis.Sim` and `ProjectAegis.Delegation` consume
read-only. It owns catalog storage (SQLite), the staged **write gate**, immutable **snapshots**,
**scenario ↔ database version binding**, headless validation agents, and the workbook
import/export pipeline. Pure C# with **no `UnityEngine` reference** (targets
`net8.0;netstandard2.1`, package deps: `Microsoft.Data.Sqlite`, plus `System.Text.Json` on
`netstandard2.1`), so the whole layer runs headless under `dotnet test`.

> **Architecture rule (ADR-006).** Catalog mutations *only* flow through `IWriteGate`
> (propose → validate → approve → commit). Snapshots are immutable; scenarios reference a
> `dbSnapshotId` and **fail load on resolve errors**. Sim and Delegation never open SQLite
> directly — they take `ICatalogReader` and DTO exports. See
> [`adr-006-data-layer-boundary.md`](../../docs/architecture/adr-006-data-layer-boundary.md).

---

## Subsystem map

| Folder | Purpose | Key types |
|--------|---------|-----------|
| `Catalog/` | Row DTOs, read seam, sort/provenance/TL-tier rules, in-memory & SQLite readers | `ICatalogReader`, `SqliteCatalogReader`, `InMemoryCatalogReader`, `CatalogReaderFactory`, `CatalogPlatformEntry`, `CatalogSensorBinding`, `CatalogMount`, `CatalogLoadout`, `CatalogMagazineEntry`, `CatalogWeaponRecord`, `CatalogProvenanceTier`, `CatalogTlTier`, `CatalogSortKeyComparer` |
| `WriteGate/` | Staged catalog writes (propose → approve → commit), audit change log | `IWriteGate`, `CatalogWriteGate`, `ICatalogClock`, `WriteGateDecision`, `CatalogStagingBatchSummary` |
| `Snapshots/` | Immutable snapshot store, content hashing, release-train manifests/diffs | `DbSnapshotStore`, `CatalogSnapshotHasher`, `CatalogSnapshotBinder`, `CatalogExportManifest`, `CatalogReleaseTrainResolver`, `UnifiedReleaseTrainManifest` |
| `Scenario/` | Scenario package (policy + db binding), loader, path seams, policy JSON catalog | `ScenarioPackage`, `ScenarioPackageLoader`, `ScenarioDataPaths`, `ScenarioPolicyJsonCatalog` |
| `Validation/` | Deterministic validation pipeline + rules, kill-chain / export gates | `ValidationPipeline`, `ScenarioValidationEngine`, `ValidationReport`, `ValidationSeverity`, `KillChainCommitGate`, `IScenarioValidationRule` |
| `Agents/` | Headless "database intelligence" agents (no LLM in tick path) + orchestrator | `IDatabaseIntelligenceAgent`, `DatabaseIntelligenceOrchestrator`, `CatalogConsistencyAgent`, `CatalogRulesValidationAgent`, `CatalogEntityResolutionAgent` |
| `Import/` | CMO markdown importers + quarantine/proposer flow ([guide](../../docs/engineering/cmo-markdown-import.md)) | `CmoMarkdownImporter`, `CmoMarkdownImportProposer`, `CmoMarkdownQuarantineReportEntry` |
| `Platform/` | Platform-editor workbook round-trip (export → edit → diff → import); ClosedXML `.xlsx` adapter lives in [`ProjectAegis.Data.Excel`](../ProjectAegis.Data.Excel/README.md) | `IPlatformWorkbookIo`, `CanonicalTextWorkbookIo`, `PlatformWorkbookExporter`, `PlatformWorkbookImporter`, `PlatformWorkbookDiff`, `PlatformWorkbookWriteService`, `PlatformWorkbookHash` |
| `Osint/` | OSINT digest → proposal gate (staging only; extend-only) | `OsintDigestRunner`, `OsintCatalogMapper`, `OsintProposalGate`, `OsintDiscoveryRecord` |
| `Telemetry/` | Balance-drift telemetry accumulation + deterministic state hashing | `BalanceTelemetryAccumulator`, `CatalogBalanceDriftPipelineEvaluator`, `IBalanceTelemetrySink` |
| `Polyfills/` | `netstandard2.1` shims | `IsExternalInit` |

---

## The write gate — propose → approve → commit

All catalog mutations are staged and audited; nothing is written straight to the live tables.
`CatalogWriteGate` is SQLite-backed and takes an injectable `ICatalogClock` so the commit path
stays deterministic (no `DateTime.UtcNow`).

```csharp
using var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(utcTicks: 0));

// 1. Propose — rows are sorted by stable key and written to staging tables, returns a batch id.
string batchId = gate.ProposeSensorBatch(rows, actorType: "agent", actorId: "osint-digest",
                                         rationale: "S-band Pd revision");

// 2. Approve — validates (import gate / quarantine partition), upserts live rows,
//    appends an audit change-log entry, and records a stable snapshot for replay binding.
WriteGateDecision decision = gate.ApproveBatch(batchId, "reviewer", "tl-lead");
if (!decision.Committed) { /* decision.Errors, e.g. "quarantine:.." or "ambiguous_staging_batch" */ }

// Reject drains the staging rows (no orphans); pending batches are listable.
IReadOnlyList<CatalogStagingBatchSummary> pending = gate.ListPendingBatches();
```

**Extend-only contract.** `IWriteGate` grows by *adding* new `Propose*Batch` overloads (sensors,
mounts, loadouts, magazines, comms, links, platforms, weapons, mobility, signatures, EMCON,
damage). Existing propose/approve paths are never altered — this is a hard invariant (see
[`AGENTS.md`](../../AGENTS.md#hard-invariants--never-break-these)). A staging batch must resolve
to a single row kind; a mixed batch is rejected with `ambiguous_staging_batch`.

> **Deep-dive.** For the full propose batch-kind table, per-kind approve validation, the
> machine-readable error-code catalog (`quarantine:*`, `orphan_platform:*`, `kill_chain:*`, …),
> the CLI verbs, and the runbook for adding a new row kind, see
> [`docs/engineering/catalog-write-gate.md`](../../docs/engineering/catalog-write-gate.md).

### Provenance & TL tiers

Every persisted value carries a provenance classification so downstream tooling can separate
raw facts from gameplay tuning:

| Constant (`CatalogProvenanceTier`) | Meaning |
|------------------------------------|---------|
| `source_fact` | Value taken directly from a source (spec sheet, OSINT) |
| `interpreted_value` | Derived/estimated from source facts |
| `gameplay_abstraction` | Balance/gameplay tuning value (default when unknown) |

Export and release-train filtering use **TL tiers** (`CatalogTlTier.Tl0`…`Tl5`, default `TL-0`)
so a snapshot can be sliced to a maximum disclosure tier. `CatalogTlTier.CatalogSchemaVersion`
tracks the latest applied migration id (currently `010`).

---

## Snapshots & scenario binding

Snapshots are **immutable** and content-hashed (`CatalogSnapshotHasher`); an approved write-gate
batch records a stable snapshot so a scenario can pin exactly the catalog it ran against.

`ScenarioPackage` is the runtime manifest that ties a scenario to its data (`scenarioId`,
`policyId`, `dbSnapshotId`, optional `dbRef`, `tlBranch`, `seed`, `editVersion`). Binding
resolution (ADR-008) prefers an explicit `dbSnapshotId`, then a `dbRef` resolved via the catalog
reader, then a TL-branch lookup, falling back to the Baltic default snapshot:

```csharp
ScenarioPackage pkg = ScenarioPackageLoader.LoadFromFile(scenarioPath); // canonical JSON
// pkg.DbSnapshotId is guaranteed resolved; sim loads the matching immutable catalog.
```

---

## Read path (consumers)

`ICatalogReader` is the **only** seam sim/delegation use. It exposes deterministically-sorted
accessors (e.g. `GetSortedSensorBindings()` sorted by `platform_id` then `sensor_id`) plus
`TryGet*` lookups for Pd, weapon envelopes, mobility, signatures, EMCON, positions, and link
latency. Most methods have default-interface implementations returning empty/absent, so a
minimal reader (`NullCatalogReader`, `InMemoryCatalogReader`) is cheap to construct in tests.

```csharp
ICatalogReader catalog = CatalogReaderFactory.ResolveForScenario("baltic-patrol");
foreach (var s in catalog.GetSortedSensorBindings()) { /* deterministic iteration */ }
catalog.TryGetWeaponEnvelope(weaponId, out var env);
```

`CatalogReaderFactory` resolves the on-disk Baltic Patrol / Baltic v3 SQLite DB for the headless
harness and CI, or returns an override reader for isolation. When the DB is missing it is
seeded on demand by `CatalogSeedBootstrap` (the deterministic Baltic fixture + the `u1` engage
chain). The full resolution/seed flow — including how the committed `baltic_patrol.db`,
`sensors_baltic.json`, and the in-memory fixtures interact — is documented in
[`docs/engineering/catalog-seeding.md`](../../docs/engineering/catalog-seeding.md).

---

## Validation & database-intelligence agents

`ValidationPipeline.Run(catalog, dbPath?)` (req-06 P0) delegates to
`DatabaseIntelligenceOrchestrator`, which runs a **fixed, documented order** of headless agents —
schema/referential/sanity checks only, fully deterministic, **no LLM in the tick path**. Each
`IDatabaseIntelligenceAgent.Run(...)` returns a `DatabaseAgentReport` of severity-coded findings;
`ScenarioValidationEngine` (ADR-008) produces the scenario-facing `ValidationReport`, and
`KillChainCommitGate` blocks commits that would break kill-chain dependency edges.

```csharp
DatabaseIntelligenceRunResult result = ValidationPipeline.RunBalticDefault();
```

---

## Schema & migrations

The SQLite schema is defined by ordered migrations in
[`assets/data/catalog/migrations/`](../../assets/data/catalog/migrations/)
(`001_sensor_base_pd.sql` … `011_link_catalog_staging.sql`). `CatalogWriteGate` calls
`EnsureSchema()` on construction so a fresh dev/CI DB is self-bootstrapping. When adding a
migration, bump `CatalogTlTier.CatalogSchemaVersion` so export manifests report the right
`schemaVersion`.

---

## Build & test

```bash
dotnet build src/ProjectAegis.Data/ProjectAegis.Data.csproj
dotnet test  src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj -v minimal
```

`ProjectAegis.Data.Tests` (~406 tests) mirrors this folder layout and is part of the ≥1232-test
solution baseline. Golden-hash guards (`CatalogSortKeyGoldenHashes`, `ValidationGoldenHashes`,
`KillChainGoldenHashes`, `BalanceTelemetryGoldenHashes`, `LinkCatalogGoldenHashes`) pin
deterministic outputs — regenerate them intentionally, never silently.

## See also

| Topic | Doc |
|-------|-----|
| Data-layer boundary decision | [`adr-006-data-layer-boundary.md`](../../docs/architecture/adr-006-data-layer-boundary.md) |
| Scenario ↔ DB version binding / validation engine | [`adr-008-mission-editor-validation-engine.md`](../../docs/architecture/adr-008-mission-editor-validation-engine.md) |
| Workbook round-trip CLI (import/export/diff) | [`docs/engineering/mission-editor-cli.md`](../../docs/engineering/mission-editor-cli.md) |
| CMO markdown import pipeline (parse → propose → approve) | [`docs/engineering/cmo-markdown-import.md`](../../docs/engineering/cmo-markdown-import.md) |
| Catalog seeding & reader resolution (headless/CI bootstrap) | [`docs/engineering/catalog-seeding.md`](../../docs/engineering/catalog-seeding.md) |
| Production `.xlsx` (ClosedXML) adapter | [`../ProjectAegis.Data.Excel/README.md`](../ProjectAegis.Data.Excel/README.md) |
| Simulation core (read-only consumer) | [`../ProjectAegis.Sim/README.md`](../ProjectAegis.Sim/README.md) |
| Delegation framework (read-only consumer) | [`../ProjectAegis.Delegation/README.md`](../ProjectAegis.Delegation/README.md) |
| Determinism rules (clock, ordering, hashing) | [`docs/engineering/determinism-and-replay.md`](../../docs/engineering/determinism-and-replay.md) |
| Hard invariants (extend-only write gate, snapshots) | [`AGENTS.md`](../../AGENTS.md#hard-invariants--never-break-these) |
