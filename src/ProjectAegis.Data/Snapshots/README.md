# Snapshots — deterministic catalog snapshots & release train

This subsystem turns the live catalog into **reproducible, hashable snapshots**
and records the **release-train metadata** (req-06 P0 / P2-3) that lets a scenario
bind to an exact catalog state for replay. It is the determinism anchor between
the [write gate](../WriteGate/README.md) and scenario packaging.

Three pieces:

| Type | Responsibility |
|------|----------------|
| `CatalogSnapshotHasher` | Deterministic SHA-256 fingerprint over sorted catalog rows. |
| `DbSnapshotStore` | Reads/writes `catalog_snapshot` + `db_release` tables (the release train). |
| `CatalogSnapshotBinder` | After a write-gate approve, hash the catalog and record a release row. |

## Content hashing (`CatalogSnapshotHasher`)

`ComputeSha256Hex(bindings)` produces a lowercase SHA-256 hex digest over the
catalog's sensor bindings:

1. Sort rows by `PlatformId` then `SensorId` (ordinal) — order-independent.
2. Serialize each row as tab-separated fields (`PlatformId`, `SensorId`,
   `BasePd`, `Confidence`, `ImportBatchId`, `SourceFile`), one row per line.
   Doubles use round-trip (`"R"`) formatting with `CultureInfo.InvariantCulture`.
3. SHA-256 the UTF-8 bytes and hex-encode (lowercase).

Identical catalog content always yields the same hash on any machine. This hash
is what `db_release.content_hash_sha256` stores and what replay binding compares.

## Release train (`DbSnapshotStore`)

A disposable wrapper over a SQLite connection (`Pooling=false`; schema
bootstrapped via `SqliteCatalogReader` on construction). All reads tolerate a
missing table/column and fall back gracefully.

| Member | Behavior |
|--------|----------|
| `GetSortedSnapshotIds()` | `snapshot_id`s ascending; falls back to `CatalogValidationDefaults.BalticSnapshotId` when the table is absent/empty. |
| `TryGetContentHash(snapshotId, out hash)` | Reads `content_hash_sha256`; `false` if the table/column is missing or the value is empty. |
| `RecordRelease(version, snapshotId, hash, createdUtcTicks, schemaVersion="006", notes="")` | Upserts `catalog_snapshot` and `INSERT OR REPLACE` into `db_release`, in one transaction. Throws `ArgumentException` on blank version/snapshot/hash. |
| `GetSortedReleases()` | `DbReleaseRecord`s ordered by `release_version` ascending; `[]` if no `db_release` table. |
| `RecordApprovedImport(approvedIds, sourceFile, importBatchId)` | Derives a deterministic `snap-{batchId}-{shortHash}` id from the sorted approved ids + source + batch, inserts it (`INSERT OR IGNORE`), and returns a `DbSnapshotRecord`. |

> **Time is injected, never read.** `createdUtcTicks` is supplied by the caller
> (an `ICatalogClock`), so release rows are reproducible under a fixed clock.

`DbReleaseRecord(ReleaseVersion, SnapshotId, SchemaVersion, CreatedUtcTicks, Notes)`
and `DbSnapshotRecord(Id, ContentHash, SourceFile, ImportBatchId)` are the public
record shapes.

## Binding after approve (`CatalogSnapshotBinder`)

`BindAfterApprove(databasePath, batchId, clock, snapshotId?, releaseVersion?)` is
the glue called once a write-gate batch commits:

1. Resolve the snapshot id (default `CatalogValidationDefaults.BalticSnapshotId`)
   and release version (default `catalog-approve-{sanitizedBatchId}`).
2. Read the now-committed sensor bindings via `SqliteCatalogReader` and hash them
   with `CatalogSnapshotHasher`.
3. `DbSnapshotStore.RecordRelease(...)` with `notes = "batch={batchId}"`.
4. Return `BindResult(ReleaseVersion, SnapshotId, ContentHashSha256, SensorRowCount)`.

This is invoked from `catalog_write_approve` in the mission-editor CLI, so an
approve produces both a live-catalog commit and a bound, hashed release row. See
the [WriteGate approve flow](../WriteGate/README.md) and the
[CLI verb reference](../../ProjectAegis.MissionEditor.Cli/README.md).

## Determinism

- Snapshot ids and content hashes depend only on catalog content + the injected
  clock, not on insertion order or wall-clock time.
- All sorting is ordinal; all numeric formatting is `InvariantCulture`.
- Because the write-gate approve auto-binds a snapshot, a fixed clock + identical
  catalog input reproduces identical snapshot ids and hashes across runs — the
  basis for replay binding.

## Tests

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter Snapshot -v minimal
```

`src/ProjectAegis.Data.Tests/Snapshots/` covers store read/write
(`DbSnapshotStoreTests`) and approve-time binding (`DbSnapshotBindingTests`).

## See also

- [ProjectAegis.Data overview](../README.md)
- [WriteGate — staged catalog writes](../WriteGate/README.md)
- [Catalog — records, readers & quarantine](../Catalog/README.md)
- [ADR-006 — data-layer boundary](../../../docs/architecture/adr-006-data-layer-boundary.md)
