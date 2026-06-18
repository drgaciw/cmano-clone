# WriteGate — staged catalog writes

The write gate is the **only** sanctioned path for mutating the authoritative
catalog. It enforces the core invariant of the data layer
([ADR-006](../../../docs/architecture/adr-006-data-layer-boundary.md) /
[requirement 06](../../../Game-Requirements/requirements/06-Database-Intelligence.md)):

> Every catalog mutation is a staged **propose → approve → commit** batch.
> No subsystem writes catalog rows directly.

Importers, agents, OSINT, and the platform editor *stage* rows into
`*_quarantine`/`catalog_staging_*` tables. A human or TL reviewer then approves
or rejects the batch; only approval commits rows into the live catalog.

## Contract (`IWriteGate`)

| Member | Purpose |
|--------|---------|
| `Propose{Sensor,Mount,Loadout,Magazine,Comms,Platform,Weapon}Batch(rows, actorType, actorId, rationale)` | Stage a batch of one kind. Returns a `batchId`. Throws `ArgumentException` if `rows` is empty. |
| `ListPendingBatches()` | Batches still in `proposed` state, ordered by `(proposedUtcTicks, batchId)` — the reviewer queue. |
| `ApproveBatch(batchId, actorType, actorId)` | Validate + commit staged rows to the live catalog. Returns `WriteGateDecision`. |
| `RejectBatch(batchId, actorType, actorId, rationale)` | Mark the batch `rejected` and purge **all** staging rows for it. |

Return/summary records:

- `WriteGateDecision(bool Committed, string BatchId, IReadOnlyList<string> Errors)`
- `CatalogStagingBatchSummary(BatchId, ActorType, ActorId, RecordCount, ApprovalState, ProposedUtcTicks)`

## Implementation (`CatalogWriteGate`)

SQLite-backed (`Microsoft.Data.Sqlite`, `Pooling=false`). The schema is created
on construction by bootstrapping a `SqliteCatalogReader`, so a fresh DB path is
valid. Every propose/approve/reject runs inside a single transaction.

### Propose

1. Reject an empty batch (`ArgumentException`).
2. Build the batch id — `batch-{kind}-{count}-{ticks}` for most kinds, and
   `batch-{count}-{ticks}` for sensors (no kind segment, for historical
   compatibility).
3. **Sort rows by their canonical keys** (e.g. sensors by
   `PlatformId, SensorId`; magazines by
   `PlatformId, LoadoutId, MountId, WeaponId`). Staging is therefore
   order-independent and deterministic.
4. Insert a batch header (`catalog_staging_batch`, state `proposed`) and the rows
   into the matching `catalog_staging_*` table.

### Approve

`ApproveBatch` today commits **sensor** staging rows into the live `sensor`
table:

1. Load the batch's sensor staging rows. If none exist, return
   `Committed=false, Errors=["staging_batch_not_found"]`.
2. Partition via `CatalogImportGate.PartitionForImport`. If any row is
   quarantined, **nothing commits** — the decision returns
   `quarantine:{platform}/{sensor}:{reason}` errors.
3. For each approved row, read the previous `base_pd`, upsert the live `sensor`
   row, and append a `catalog_change_log` entry (old/new value, actor,
   `approved`).
4. Mark the batch `approved` and commit the transaction.
5. **Auxiliary, non-fatal:** record a deterministic snapshot of the approved
   sensor ids via `DbSnapshotStore` (enables replay binding / scenario
   packaging). Snapshot failures never fail the approve commit.

> **Current scope.** The commit-to-live path is wired for sensor rows. Platform,
> mount, loadout, magazine, comms, and weapon batches can be *proposed*,
> *listed*, and *rejected* (with full orphan purge), but their commit-to-live
> wiring is staged-only at present — approving such a batch returns
> `staging_batch_not_found` because no sensor rows back it. Extend `ApproveBatch`
> (load + upsert per kind) when promoting a new kind to live.

### Reject

Marks the batch `rejected` and deletes its rows from **all** staging tables
(`DeleteStagingRows`), so a rejected batch never leaves orphan rows behind
(DBI-1.4). `RejectBatch` returns `Committed=false` by design.

## Determinism

- Time comes from `ICatalogClock`, never `DateTime.UtcNow`. Tests inject
  `FixedCatalogClock(ticks)`; the default is `FixedCatalogClock(0)`.
- Rows are sorted by canonical keys before insert, and all formatting uses
  `CultureInfo.InvariantCulture`.
- Because batch ids embed the clock ticks and row count, identical inputs under
  a fixed clock produce identical batch ids and snapshot hashes.

## Usage

```csharp
using var gate = new CatalogWriteGate("catalog.db", new FixedCatalogClock(0));

// 1. Propose (importer / agent / OSINT)
string batchId = gate.ProposeSensorBatch(sensorRows, actorType: "agent", actorId: "cmo-importer",
                                         rationale: "import:baltic-patrol");

// 2. Review queue
foreach (var b in gate.ListPendingBatches())
    Console.WriteLine($"{b.BatchId} {b.ActorId} {b.RecordCount} {b.ApprovalState}");

// 3. Approve (human / TL) or reject
WriteGateDecision decision = gate.ApproveBatch(batchId, actorType: "human", actorId: "qa-reviewer");
if (!decision.Committed)
    foreach (var err in decision.Errors) Console.Error.WriteLine(err);
```

From the mission-editor CLI / MCP surface:

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_write_propose --db catalog.db ...
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_write_approve --db catalog.db --batch <batchId>
```

## Adding a new staged entity

1. Add a `Propose{Kind}Batch` method to `IWriteGate` and `CatalogWriteGate`
   (validate non-empty, sort by canonical keys, insert into a
   `catalog_staging_{kind}` table). **Never** add a side-channel write that
   bypasses the gate.
2. Add the staging table to the purge list in `DeleteStagingRows` so rejects stay
   orphan-free.
3. To promote the kind to live, extend `ApproveBatch` with a load + upsert +
   change-log path for that kind.

## Tests

`src/ProjectAegis.Data.Tests/WriteGate/CatalogWriteGateTests.cs` plus
`Import/` and `Snapshots/` suites cover propose ordering, empty-batch rejection,
quarantine blocking, the reject orphan guard, and stable snapshot hashing.

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter WriteGate -v minimal
```

## See also

- [ProjectAegis.Data overview](../README.md)
- [ADR-006 — data-layer boundary](../../../docs/architecture/adr-006-data-layer-boundary.md)
- [Requirement 06 — Database Intelligence](../../../Game-Requirements/requirements/06-Database-Intelligence.md)
- [OSINT ingestion pipeline](../Osint/README.md) — a write-gate proposer
