# Catalog Snapshots & Release Train

`ProjectAegis.Data.Snapshots` — binds an approved catalog state to a **content
hash** and a **release row** so simulations and replays can pin the exact data
they ran against. After the write gate commits a batch, this subsystem fingerprints
the live sensor rows (deterministic SHA-256), records a `catalog_snapshot`, and
appends a `db_release` entry.

**Requirement trace:** Req-06 P0 / P2-3 (snapshot + release train)
(`Game-Requirements/requirements/06-Database-Intelligence.md`); ADR-006 (read/write
boundary). **Posture:** *deterministic fingerprint* — no `DateTime.Now`, no LLM, so
the same catalog always yields the same hash and the same replay binding.

## Intent

A scenario references catalog data by `dbSnapshotId` / `dbRef` (ADR-008). For that
reference to be reproducible, the catalog content behind it must be pinned:

1. The write gate approves a batch and commits sensor rows to the live table.
2. `CatalogSnapshotBinder.BindAfterApprove` reads the **sorted** sensor bindings,
   hashes them, upserts a `catalog_snapshot` row (id + content hash), and inserts
   a `db_release` row tying a release version → snapshot id → schema version.
3. Later, `DbSnapshotStore.TryGetContentHash` lets a consumer verify the catalog
   behind a snapshot id is byte-for-byte what was approved.

Because the hash is computed over an **ordinally sorted** projection of the rows,
it is independent of row insertion order — re-approving identical data produces an
identical hash.

## Architecture

```
WriteGate.ApproveBatch(batchId)  ─ commits sensor rows ─►  live `sensor` table
        │
        ▼
CatalogSnapshotBinder.BindAfterApprove(db, batchId, clock, [snapshotId], [release])
        │   SqliteCatalogReader.GetSortedSensorBindings()
        │   CatalogSnapshotHasher.ComputeSha256Hex(bindings)   (sorted, tab-joined)
        ▼
DbSnapshotStore.RecordRelease(release, snapshotId, hash, utcTicks, schema, notes)
        │   one transaction:
        ├─ UPSERT catalog_snapshot(snapshot_id, content_hash_sha256)
        └─ INSERT OR REPLACE db_release(release_version, snapshot_id, schema_version,
                                        created_utc_ticks, notes="batch=<id>")
        ▼
BindResult(ReleaseVersion, SnapshotId, ContentHashSha256, SensorRowCount)
```

| Type | Role |
|------|------|
| `CatalogSnapshotHasher` | Pure, static. `ComputeSha256Hex(IReadOnlyList<CatalogSensorBinding>)` sorts by `(PlatformId, SensorId)` ordinally, tab-joins `platformId, sensorId, basePd, confidence, importBatchId, sourceFile` per row (floats via `"R"`/invariant culture), and returns lower-case hex SHA-256. Order-independent. |
| `CatalogSnapshotBinder` | Static facade called after approve. Resolves defaults (`CatalogValidationDefaults.BalticSnapshotId`; release `catalog-approve-<sanitized batchId>`), hashes the live catalog, and records the release. Returns `BindResult`. |
| `DbSnapshotStore` | SQLite-backed reader/writer (`IDisposable`, `Pooling=false`, clears all pools on dispose). `RecordRelease`, `GetSortedReleases`, `GetSortedSnapshotIds`, `TryGetContentHash`, and `RecordApprovedImport` (gate auto-record). |
| `DbSnapshotRecord` | `(Id, ContentHash, SourceFile, ImportBatchId)` — returned by `RecordApprovedImport`. |
| `DbReleaseRecord` | `(ReleaseVersion, SnapshotId, SchemaVersion, CreatedUtcTicks, Notes)` — declared in `Catalog/`, returned by `GetSortedReleases`. |

## Usage

```csharp
using ProjectAegis.Data.Snapshots;
using ProjectAegis.Data.WriteGate;

// After WriteGate.ApproveBatch(...) returns Committed == true:
var bind = CatalogSnapshotBinder.BindAfterApprove(
    databasePath,
    batchId,
    new FixedCatalogClock(2000));   // injected clock — no wall-clock reads

Console.WriteLine(bind.SnapshotId);          // default "baltic_patrol"
Console.WriteLine(bind.ReleaseVersion);      // e.g. "catalog-approve-batch-1-1000"
Console.WriteLine(bind.ContentHashSha256);   // 64-char lower-case hex
Console.WriteLine(bind.SensorRowCount);

// Verify later that the catalog behind a snapshot matches what was approved:
using var store = new DbSnapshotStore(databasePath);
if (store.TryGetContentHash(bind.SnapshotId, out var stored))
    Debug.Assert(stored == bind.ContentHashSha256);

foreach (var r in store.GetSortedReleases())
    Console.WriteLine($"{r.ReleaseVersion} -> {r.SnapshotId} ({r.SchemaVersion})");
```

## CLI / operational runbook

Snapshot binding happens **automatically inside `catalog_write_approve`** — there
is no separate snapshot verb. The headless mission editor approves a batch and binds
the snapshot in one call (`--snapshot-id` / `--release-version` override the
deterministic defaults):

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  catalog_write_approve --db <catalog.db> --batch batch-1-1000
# → { "ok": true, "batchId": ..., "releaseVersion": ...,
#     "snapshotId": ..., "contentHashSha256": "<64 hex>", "sensorRowCount": N }
```

See `tools/mission-editor/README.md` and `WriteGate/README.md` for the full
propose → approve loop.

## Constraints & gotchas

- **Sensor rows only.** The hash covers the live `sensor` table (the P0 commit
  surface). Mount/loadout/magazine/comms/platform/weapon rows are not part of the
  fingerprint yet — consistent with the write gate's sensor-only commit path.
- **Snapshot binding is best-effort in the gate.** The write gate also records a
  snapshot internally after a sensor commit, and swallows failures there so a bad
  snapshot write never rolls back an otherwise-valid commit. When you need the
  `BindResult` (hash, release), call `BindAfterApprove` explicitly (as the CLI does).
- **`db_release` is upsert-by-version.** `INSERT OR REPLACE` keys on
  `release_version`; reusing a release version overwrites the prior row. Batch ids
  are sanitized into the default release token (`:` → `-`, truncated to 64 chars).
- **Default schema version is `006`.** `RecordRelease` defaults `schema_version`
  to `"006"`; pass it explicitly when recording against a newer schema.
- **Graceful on fresh DBs.** `GetSortedSnapshotIds` / `GetSortedReleases` return the
  Baltic default (or empty) when the snapshot/release tables do not exist yet, so a
  reader never throws on an un-seeded catalog. `TryGetContentHash` returns `false`
  if the table or `content_hash_sha256` column is absent.
- **One connection, dispose it.** `DbSnapshotStore` wraps a single non-pooled
  SQLite connection; use one instance per logical writer.

## Tests

| Area | Test |
|------|------|
| Release train reads sorted releases + Baltic snapshot id | `ProjectAegis.Data.Tests/Snapshots/DbSnapshotStoreTests` |
| Bind records content hash + release row after approve | `Snapshots/DbSnapshotBindingTests.BindAfterApprove_records_content_hash_and_release_row` |
| Hash stable across identical approve cycles | `DbSnapshotBindingTests.Content_hash_stable_across_two_identical_approve_cycles` |
| Hash is order-independent | `DbSnapshotBindingTests.CatalogSnapshotHasher_is_order_independent` |

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "FullyQualifiedName~Snapshots" -v minimal
```
