# Catalog Write Gate (propose → approve → commit)

`ProjectAegis.Data.WriteGate` — the **only sanctioned path** for changing catalog
rows. Agents and importers *propose* batches into staging tables; a reviewer
*approves* (or *rejects*) them; only on approval are rows committed to the live
catalog with a full change-log entry. Nothing writes the live tables directly.

**Requirement trace:** DBI-1.x, DBI-4.x, DBI-7.x, DBI-8.4
(`Game-Requirements/requirements/06-Database-Intelligence.md`); ADR-006.
**Posture:** *propose, never auto-merge* — agents are research assistants, the
human reviewer is the authority.

## Intent

Military reference data is noisy and contradictory. Rather than let agents mutate
the catalog inline, every change is staged and auditable:

1. A producer calls a `Propose*Batch(...)` verb and gets back a deterministic
   `batchId`. Rows land in `catalog_staging_*` tables in `proposed` state.
2. A reviewer (human or trusted actor) calls `ApproveBatch` or `RejectBatch`.
3. `ApproveBatch` re-runs the import gate, commits surviving rows to the live
   table, and appends `catalog_change_log` rows. `RejectBatch` purges staging and
   leaves live tables untouched (DBI-4.4).

This keeps the commit loop deterministic (no LLM, no wall-clock) so catalog
exports and replays are reproducible.

## Architecture

```
Propose*Batch(rows, actorType, actorId, rationale)
        │  (sorted ordinally, single transaction)
        ▼
  catalog_staging_batch  +  catalog_staging_{sensor,platform,weapon,
                            mount,loadout,magazine,comms}   state = "proposed"
        │
        │  ListPendingBatches()  ──►  CatalogStagingBatchSummary[]
        │
   ApproveBatch(batchId)                     RejectBatch(batchId)
        │                                          │
   CatalogImportGate.PartitionForImport       MarkBatchState("rejected")
   (confidence ≥ 0.5, TRL ≥ 4, approved)      + DeleteStagingRows (all tables)
        │ any quarantined → Committed=false        │
        ▼ else                                      ▼
   UPSERT sensor + AppendChangeLog          live tables unchanged
   MarkBatchState("approved")               WriteGateDecision(Committed=false, [])
   (+ non-fatal snapshot record)
        ▼
   WriteGateDecision(Committed=true, [])
```

| Type | Role |
|------|------|
| `IWriteGate` | Public surface: seven `Propose*Batch` verbs, `ApproveBatch`, `RejectBatch`, `ListPendingBatches`. |
| `CatalogWriteGate` | SQLite-backed implementation. Opens one non-pooled connection, bootstraps schema via `SqliteCatalogReader`, and is `IDisposable` (clears all SQLite pools on dispose). |
| `ICatalogClock` / `FixedCatalogClock` | Injectable time source (`UtcTicks`). Default is `FixedCatalogClock(0)` — **no `DateTime.Now` in the commit path** (DBI-1.2). |
| `WriteGateDecision` | `(bool Committed, string BatchId, IReadOnlyList<string> Errors)`. |
| `CatalogStagingBatchSummary` | `(BatchId, ActorType, ActorId, RecordCount, ApprovalState, ProposedUtcTicks)` — one row per pending batch. |

The approve-time TRL/review gate lives in
`ProjectAegis.Data.Catalog.CatalogImportGate.PartitionForImport` (default
minimum confidence `0.5`, minimum TRL `4`, `ReviewState == approved`).

## Usage

```csharp
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.WriteGate;

using var gate = new CatalogWriteGate(databasePath, new FixedCatalogClock(1000));

// 1. Propose — returns a deterministic batch id, e.g. "batch-1-1000".
var batchId = gate.ProposeSensorBatch(
    new[] { sensorBinding },          // CatalogSensorBinding[]
    actorType: "agent",
    actorId:  "database-intelligence",
    rationale: "MCP catalog_write_propose");

// 2. Inspect what is awaiting review.
foreach (var b in gate.ListPendingBatches())
    Console.WriteLine($"{b.BatchId} {b.ActorId} ({b.RecordCount} rows) {b.ApprovalState}");

// 3a. Approve — commits surviving rows and writes the change log.
WriteGateDecision approve = gate.ApproveBatch(batchId, actorType: "human", actorId: "reviewer");
if (!approve.Committed)
    foreach (var e in approve.Errors) Console.WriteLine(e); // e.g. "quarantine:..." or "staging_batch_not_found"

// 3b. …or reject — purges staging, live tables unchanged.
gate.RejectBatch(batchId, "human", "reviewer", rationale: "superseded");
```

### Batch id scheme (deterministic)

`Propose*Batch` returns `batch[-<kind>]-<count>-<utcTicks>`:

| Verb | Batch id prefix | Staging table |
|------|-----------------|---------------|
| `ProposeSensorBatch` | `batch-` | `catalog_staging_sensor` |
| `ProposeMountBatch` | `batch-mount-` | `catalog_staging_mount` |
| `ProposeLoadoutBatch` | `batch-loadout-` | `catalog_staging_loadout` |
| `ProposeMagazineBatch` | `batch-magazine-` | `catalog_staging_magazine` |
| `ProposeCommsBatch` | `batch-comms-` | `catalog_staging_comms` |
| `ProposePlatformBatch` | `batch-platform-` | `catalog_staging_platform` |
| `ProposeWeaponBatch` | `batch-weapon-` | `catalog_staging_weapon` |

Rows are sorted ordinally before insert, so the same input always yields the same
staging order and batch id (given the same clock).

## CLI / operational runbook

The headless mission-editor exposes the gate as two MCP/CLI verbs
(`src/ProjectAegis.MissionEditor.Cli`, see `tools/mission-editor/README.md`):

```bash
# Propose a single sensor row (seeds the Baltic catalog if the db is missing).
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  catalog_write_propose --db <catalog.db> --platform P --sensor S --base-pd 0.7
# → { "ok": true, "batchId": "batch-1-1000", "recordCount": 1 }

# Approve it; also binds a snapshot / release version for replay (DATA P2-3).
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  catalog_write_approve --db <catalog.db> --batch batch-1-1000
# → { "ok": true, "batchId": ..., "releaseVersion": ..., "snapshotId": ..., "sensorRowCount": N }
```

Bulk producers that stage through the gate (never write SQLite directly):

- `Import/CmoMarkdownImportProposer` — parses CMO markdown into sensor batches
  (chunked at 500 rows) and, for S22-04, platform/weapon/mount batches.
- `Osint/OsintDigestRunner`, `Agents/CatalogDiffProposalAgent`,
  `Platform/PlatformWorkbookImporter` — propose batches, then hand the `batchId`
  to `catalog_write_approve`.

## Constraints & gotchas

- **Approve currently commits sensor rows only.** `ApproveBatch` loads from
  `catalog_staging_sensor` and upserts the live `sensor` table. Mount, loadout,
  magazine, comms, platform, and weapon batches *stage* successfully, appear in
  `ListPendingBatches`, and are purged by `RejectBatch` — but there is **no
  live-table commit path for them yet** (P0 was scoped to sensors per DBI-1.1).
  Approving such a batch returns `staging_batch_not_found` because no sensor rows
  exist for it. Do not assume non-sensor proposals reach the catalog on approve.
- **Empty batches throw.** Every `Propose*Batch` throws `ArgumentException` on an
  empty list (DBI-7.1).
- **Quarantine blocks the whole batch.** If *any* row fails the import gate
  (confidence `< 0.5`, TRL `< 4`, or `ReviewState != approved`), `ApproveBatch`
  commits **nothing** and returns `Committed == false` with `quarantine:<key>:<reason>`
  errors. Fix or split the batch and re-propose.
- **`RejectBatch` always reports `Committed == false`.** That is success for a
  reject (the batch was *not* committed); an empty `Errors` list means it worked,
  `["staging_batch_not_found"]` means the id was unknown.
- **Change log is sensor-specific today.** `AppendChangeLog` records the `base_pd`
  field transition (`previous` → `new`) per `platformId/sensorId` with actor and
  approval state (DBI-4.1).
- **Snapshot binding is non-fatal.** After a sensor commit the gate records a
  stable snapshot; failures there are swallowed so they never roll back an
  otherwise-valid commit.
- **One connection, not thread-safe.** `CatalogWriteGate` wraps a single SQLite
  connection with `Pooling=false`; use one instance per logical writer and dispose it.

## Tests

| Area | Test |
|------|------|
| Propose → approve commit + stable order | `ProjectAegis.Data.Tests/WriteGate/CatalogWriteGateTests` |
| Snapshot hash recorded on approve | `CatalogWriteGateTests.ApproveBatch_AfterPropose_RecordsStableSnapshotHash` |
| CLI propose/approve round-trip + missing-db error | `ProjectAegis.MissionEditor.Cli.Tests/CatalogWriteCommandTests` |

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "FullyQualifiedName~WriteGate" -v minimal
```
