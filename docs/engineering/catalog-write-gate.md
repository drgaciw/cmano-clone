# Catalog write gate — developer reference

Operator/developer reference for **`IWriteGate`** and its SQLite implementation
**`CatalogWriteGate`** (`src/ProjectAegis.Data/WriteGate/`) — the single governed path for mutating
the `ProjectAegis.Data` catalog. Every catalog write is *staged* as a batch, *reviewed*, then
*approved* (committed) or *rejected*. The architectural decision lives in
[ADR-006](../architecture/adr-006-data-layer-boundary.md); requirement coverage is
[req-06 — Database Intelligence](../../Game-Requirements/requirements/06-Database-Intelligence.md).
The platform editor's Excel round-trip ([runbook](platform-editor-excel-roundtrip.md)) is one client
of this gate.

| Question | Answer |
|----------|--------|
| What does it guarantee? | No direct table mutation. All writes go `propose → approve → commit`; nothing commits without a separate `ApproveBatch`. |
| Where does the code live? | `src/ProjectAegis.Data/WriteGate/{IWriteGate,CatalogWriteGate,ICatalogClock}.cs`. |
| What backs it? | SQLite staging + audit tables (migrations `005` and `007`). |
| How do I drive it from the CLI? | `catalog_write_propose` / `catalog_write_approve` (sensor today); `platform_import_xlsx` stages via the gate. |
| What is committable **today**? | **Only sensor batches.** Other domains stage and can be reviewed/rejected, but `ApproveBatch` does not yet materialize them — see [Commit asymmetry](#commit-asymmetry-read-this). |

## Lifecycle

```
caller (agent / cli / editor)
      │  Propose{Sensor,Mount,Loadout,Magazine,Comms,Platform,Weapon}Batch(rows, actorType, actorId, rationale)
      ▼
catalog_staging_batch (approval_state = 'proposed')  +  catalog_staging_<domain> rows   ── one transaction ──►  batchId
      │
      ├─ ListPendingBatches()  ──►  summaries of every 'proposed' batch (oldest first)
      │
      ├─ ApproveBatch(batchId, actorType, actorId)
      │     └─ load sensor staging rows → quarantine partition → UPSERT `sensor`
      │        → append `catalog_change_log` → mark batch 'approved' → record snapshot (auxiliary)
      │        → WriteGateDecision(Committed = true, …)
      │
      └─ RejectBatch(batchId, actorType, actorId, rationale)
            └─ mark batch 'rejected' → purge **all** staging tables for the batch (orphan guard)
               → WriteGateDecision(Committed = false, …)
```

Construction opens the database with pooling disabled and runs migrations via a bootstrap reader, so
a freshly created/upgraded catalog is schema-ready before the first propose:

```csharp
using var gate = new CatalogWriteGate(databasePath, clock); // clock defaults to FixedCatalogClock(0)
var batchId = gate.ProposeSensorBatch(rows, actorType: "agent", actorId: "database-intelligence",
                                      rationale: "phase-2 import");
var decision = gate.ApproveBatch(batchId, actorType: "human", actorId: "reviewer");
// decision.Committed, decision.BatchId, decision.Errors
```

`ICatalogClock` is injected so timestamps and batch ids are **deterministic** (no `DateTime.UtcNow`
on the commit path — ADR-006 verification rule). `Dispose` closes the connection and clears the
SQLite pool.

## Propose methods

All `Propose*Batch` methods share the same shape: reject an empty list with `ArgumentException`,
sort rows by their canonical key(s) (`StringComparer.Ordinal`), insert a `catalog_staging_batch`
header plus the staging rows in **one transaction**, and return the new batch id.

| Method | Row type (`ProjectAegis.Data.Catalog`) | Staging table | Sort key(s) | Batch id prefix | Added |
|--------|----------------------------------------|---------------|-------------|-----------------|-------|
| `ProposeSensorBatch` | `CatalogSensorBinding` | `catalog_staging_sensor` | `PlatformId, SensorId` | `batch-` | P0 |
| `ProposeMountBatch` | `CatalogMount` | `catalog_staging_mount` | `PlatformId, MountId` | `batch-mount-` | S22-01 |
| `ProposeLoadoutBatch` | `CatalogLoadout` | `catalog_staging_loadout` | `PlatformId, LoadoutId` | `batch-loadout-` | S22-01 |
| `ProposeMagazineBatch` | `CatalogMagazineEntry` | `catalog_staging_magazine` | `PlatformId, LoadoutId, MountId, WeaponId` | `batch-magazine-` | S22-01 |
| `ProposeCommsBatch` | `CatalogCommsBinding` | `catalog_staging_comms` | `PlatformId, LinkId` | `batch-comms-` | S22-01 |
| `ProposePlatformBatch` | `CatalogPlatformBinding` | `catalog_staging_platform` | `PlatformId` | `batch-platform-` | S22-04 |
| `ProposeWeaponBatch` | `CatalogWeaponRecord` | `catalog_staging_weapon` | `WeaponId` | `batch-weapon-` | S22-04 |

The batch id is `"{prefix}{recordCount}-{clock.UtcTicks}"` (e.g. `batch-mount-3-2000`). Staging rows
use `INSERT OR REPLACE`, so re-proposing within a batch is idempotent on the primary key.

> The gate itself enforces no row-count ceiling. The **`PlatformWorkbookImporter`** flags plans whose
> change count exceeds `HumanApprovalRecordThreshold` (**10**, DBI-2.4) as `RequiresHumanApproval`;
> that policy lives in the importer, not in `CatalogWriteGate`.

## Approve, reject, list

| Method | Returns | Behavior |
|--------|---------|----------|
| `ApproveBatch(batchId, actorType, actorId)` | `WriteGateDecision` | Loads **sensor** staging rows, partitions via `CatalogImportGate.PartitionForImport`. Any quarantined row → `Committed = false` with `quarantine:…` errors and **no** commit. Otherwise upserts `sensor`, appends `catalog_change_log` (`base_pd` field), marks the batch `approved`, commits, then records an approval snapshot (auxiliary, never fails the commit). |
| `RejectBatch(batchId, actorType, actorId, rationale)` | `WriteGateDecision` | Missing batch → `["staging_batch_not_found"]`. Otherwise marks the batch `rejected` and **deletes the batch's rows from every staging table** (DBI-1.4 orphan guard). `Committed` is `false` by design — reject never commits. |
| `ListPendingBatches()` | `IReadOnlyList<CatalogStagingBatchSummary>` | Every batch with `approval_state = 'proposed'`, ordered by `proposed_utc_ticks` then `batch_id`. |

`WriteGateDecision(bool Committed, string BatchId, IReadOnlyList<string> Errors)` — always check
`Committed`, **not** just the absence of an exception. `CatalogStagingBatchSummary` carries
`BatchId, ActorType, ActorId, RecordCount, ApprovalState, ProposedUtcTicks`.

Approval state machine: `proposed → approved` (commit) or `proposed → rejected` (purge). There is no
un-approve; correct a mistaken approval with a new compensating batch.

## Commit asymmetry (read this)

`ApproveBatch` materializes **sensor rows only**. Internally it calls `LoadStagingRows`, which queries
`catalog_staging_sensor` exclusively, then upserts the live `sensor` table. The mount / loadout /
magazine / comms / platform / weapon `Propose*` paths exist and **stage correctly**, but `ApproveBatch`
does not yet read or commit those tables.

Consequence: approving a **non-sensor** batch finds zero sensor staging rows and returns

```json
{ "ok": false, "batchId": "batch-mount-3-2000", "errors": ["staging_batch_not_found"] }
```

even though the batch and its staged rows exist. Until the gate's approve path is extended:

- Non-sensor domains are **proposable and reviewable** (visible in `ListPendingBatches`) and
  **rejectable** (reject purges them cleanly), but **not committable**.
- The platform editor surfaces this as `UnsupportedChanges` for the `Platforms` sheet and stages the
  supported domains as separate batches; only the sensor batch will commit on approve today.

This is the single most surprising operational fact about the gate — do not assume a green
`Propose*Batch` plus `ApproveBatch` means non-sensor rows reached the catalog.

## Schema

| Table | Migration | Role |
|-------|-----------|------|
| `catalog_staging_batch` | `005` | Batch header: `batch_id, actor_type, actor_id, proposed_utc_ticks, approval_state, record_count, rationale`. |
| `catalog_staging_sensor` | `005` | Staged sensor rows (the only domain `ApproveBatch` commits today). |
| `catalog_change_log` | `005` | Append-only audit: `batch_id, table_name, entity_key, field_name, previous_value, new_value, actor_*, approval_state, revised_utc_ticks, release_version`. |
| `catalog_staging_{mount,loadout,magazine,comms,platform,weapon}` | `007` | Staged rows for the extended domains; FK to `catalog_staging_batch`. |
| `sensor` | `001`–`005` | Live catalog table written by `ApproveBatch`. |
| `db_release` | `005` | Release-train metadata bound after approve. |

Staging tables added in `007` are guarded by `SqliteCatalogReader.ShouldSkipMigration`
(idempotent re-run). A catalog must be at **schema 005+** to propose sensors and **007+** to propose
any extended domain; otherwise the relevant `INSERT` fails.

## CLI / MCP

```bash
# Propose a single sensor edit (seeds a Baltic-patrol catalog if --db is missing)
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  catalog_write_propose --db <catalog.db> --platform <P> --sensor <S> --base-pd 0.7

# Approve (commit) a staged batch — sensor batches only today
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  catalog_write_approve --db <catalog.db> --batch <batchId>
```

`catalog_write_approve` calls `ApproveBatch` then binds a release snapshot, printing
`releaseVersion`, `snapshotId`, `contentHashSha256`, and `sensorRowCount`. `platform_import_xlsx`
(see the [Excel runbook](platform-editor-excel-roundtrip.md)) stages edits through the same gate and
emits a `nextStep` pointing at `catalog_write_approve`.

## Common pitfalls

- **`Committed` is the source of truth.** A non-throwing `ApproveBatch` can still be `Committed = false`
  (quarantine, or non-sensor batch). Always branch on `decision.Committed` and surface `decision.Errors`.
- **Non-sensor batches don't commit yet.** See [Commit asymmetry](#commit-asymmetry-read-this).
  `staging_batch_not_found` from a freshly-proposed mount/comms/etc. batch is *expected*, not a missing row.
- **Empty proposals throw.** Every `Propose*Batch` rejects an empty list with `ArgumentException`; guard
  before calling if the change set may be empty.
- **Reject purges, approve doesn't.** `RejectBatch` deletes the batch from all staging tables; approved
  batches keep their staging rows. Don't rely on staging emptiness to infer approval state — read
  `approval_state` / `ListPendingBatches`.
- **Determinism.** Construct with a fixed/injected `ICatalogClock` in tests; the default
  `FixedCatalogClock(0)` makes every batch id collapse to `…-0`, which collides across batches in one run.
- **Schema floor.** Extended-domain proposes need migration `007`; sensor proposes need `005`. Point
  `--db` at a migrated catalog.

## Related

- [ADR-006 — Data layer boundary](../architecture/adr-006-data-layer-boundary.md)
- [Platform editor — Excel round-trip runbook](platform-editor-excel-roundtrip.md)
- Requirement [06 — Database Intelligence](../../Game-Requirements/requirements/06-Database-Intelligence.md)
- Code: `src/ProjectAegis.Data/WriteGate/`,
  `assets/data/catalog/migrations/{005_req06_provenance_audit_staging,007_platform_editor_phase_a}.sql`,
  `src/ProjectAegis.MissionEditor.Cli/CatalogWrite{Propose,Approve}Command.cs`
