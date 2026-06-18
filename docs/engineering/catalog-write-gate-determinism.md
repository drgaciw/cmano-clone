# Catalog Write-Gate & Determinism Contract

> **Scope:** `ProjectAegis.Data` catalog mutation + ordering.
> **Authoritative ADR:** [ADR-006 — Data Layer Boundary](../architecture/adr-006-data-layer-boundary.md)
> **Requirements:** Req 06 — Database Intelligence (`DBI-1.1`, `DBI-1.2`, `DBI-7.3`), Req 11 (`PLE-1.3`)
> **Last updated:** 2026-06-18

This runbook is the developer-facing companion to ADR-006. It explains **how catalog rows
are written and why ordering is deterministic** — the contract every importer, agent, and
test must honour. It exists because Sprint 22 QA flagged a determinism gap on the new
`Catalog*` types, and Sprint 23 (`S23-06`) hardens it.

## Why determinism matters here

`ProjectAegis.Data` feeds the simulation and replay/AAR pipelines. If catalog iteration,
batch insert order, or snapshot hashing varied between runs, two effects would break:

- **Golden-hash tests** would flake (snapshot/validation hashes are pinned in CI).
- **Scenario ↔ DB binding** (ADR-006 §3, immutable `dbSnapshotId`) would resolve to a
  different content hash for identical data, breaking replay reproducibility.

The contract: **identical input → identical SQLite write order → identical content hash**,
regardless of the order callers supply rows or the wall-clock time of the run.

## The two invariants

| # | Invariant | Enforced by |
|---|-----------|-------------|
| 1 | **Stable composite sort keys** — every `Catalog*` type has a fixed `OrderBy` key applied with `StringComparer.Ordinal` before staging, committing, exporting, and hashing. | `CatalogWriteGate.Propose*Batch`, `CatalogSnapshotHasher`, `CmoMarkdownImportProposer` |
| 2 | **No wall-clock in write/export paths** — timestamps come from an injectable `ICatalogClock`, never `DateTime.Now`/`DateTime.UtcNow`. | [`ICatalogClock`](../../src/ProjectAegis.Data/WriteGate/ICatalogClock.cs) (default `FixedCatalogClock(0)`) |

> `DBI-7.3`: canonical `PlatformId` / `SensorId` are stable across releases; **aliases never
> rewrite canonical keys** — alias resolution happens upstream in `CatalogEntityResolutionAgent`,
> not in the write gate.

## Write-gate flow (propose → approve → commit)

All catalog mutation goes through [`IWriteGate`](../../src/ProjectAegis.Data/WriteGate/IWriteGate.cs);
nothing else opens the live tables for writes (ADR-006 §2). The shape is staged-then-gated:

```text
caller rows ──► Propose*Batch ──► catalog_staging_* (sorted)   returns batchId
                                        │
                  ┌─────────────────────┴─────────────────────┐
            ApproveBatch                                   RejectBatch
       partition (import gate)                        purge staging rows
       quarantine? → reject                           mark 'rejected'
       commit to live table
       append catalog_change_log
       auto-record snapshot (best-effort)
```

- `Propose*Batch` **sorts before inserting** and returns a deterministic `batchId` of the
  form `batch[-<entity>]-<count>-<clock.UtcTicks>` (e.g. `batch-mount-12-0` under the default
  fixed clock). It writes only to `catalog_staging_*` — never the live table.
- `ApproveBatch` re-reads staging in sorted order, runs `CatalogImportGate.PartitionForImport`,
  and **aborts the whole batch if any row quarantines** (returns `WriteGateDecision` with
  `quarantine:<key>:<reason>` errors — no partial commit).
- `RejectBatch` purges **all** staging tables for the `batchId` so no orphan rows survive
  (`DBI-1.4`).
- On a successful approve, a stable snapshot is auto-recorded via `DbSnapshotStore`
  (best-effort; snapshot failure does not fail the commit).

### ⚠️ Current commit scope

`ApproveBatch` today commits **sensor** rows to the live `sensor` table (plus
`catalog_change_log` for `base_pd`). Mount / loadout / magazine / comms / platform / weapon
rows can be *staged* (and are sorted deterministically) but their live-table commit path is
delivered by **`S23-04` (ApproveBatch multi-entity)**. Do not assume non-sensor proposals
land in live tables yet — verify against
[`CatalogWriteGate.ApproveBatch`](../../src/ProjectAegis.Data/WriteGate/CatalogWriteGate.cs).

## Sort keys per entity type

These are the canonical composite keys. **Export sort keys must match reader sort keys**
(`PLE-1.3`); if you add a column to a key, update the staging read, the export, and the
golden hash together.

| Entity type | `Propose*Batch` | Ordinal composite key |
|-------------|-----------------|------------------------|
| `CatalogSensorBinding` | `ProposeSensorBatch` | `PlatformId, SensorId` |
| `CatalogMount` | `ProposeMountBatch` | `PlatformId, MountId` |
| `CatalogLoadout` | `ProposeLoadoutBatch` | `PlatformId, LoadoutId` |
| `CatalogMagazineEntry` | `ProposeMagazineBatch` | `PlatformId, LoadoutId, MountId, WeaponId` |
| `CatalogCommsBinding` | `ProposeCommsBatch` | `PlatformId, LinkId` |
| `CatalogPlatformBinding` | `ProposePlatformBatch` | `PlatformId` |
| `CatalogWeaponRecord` | `ProposeWeaponBatch` | `WeaponId` |

Reader side: [`ICatalogReader.GetSortedSensorBindings()`](../../src/ProjectAegis.Data/Catalog/ICatalogReader.cs)
returns `(platform_id, sensor_id)` ordinal order, and `DbSnapshotStore` lists snapshots /
releases with `ORDER BY ... ASC`. Pending-batch listing uses
`ORDER BY proposed_utc_ticks ASC, batch_id ASC` for a stable queue.

## Snapshots & golden hashes

- [`CatalogSnapshotHasher.ComputeSha256Hex`](../../src/ProjectAegis.Data/Snapshots/CatalogSnapshotHasher.cs)
  produces a SHA-256 over **sorted** sensor rows. It re-sorts internally, so it is
  **order-independent by construction** — feeding the same rows in any order yields the same
  hash. Floating-point fields use `"R"` round-trip formatting with `InvariantCulture`.
- [`DbSnapshotStore`](../../src/ProjectAegis.Data/Snapshots/DbSnapshotStore.cs) records
  immutable `catalog_snapshot` / `db_release` rows. `RecordApprovedImport` hashes the
  ordinal-sorted approved id list + source + batch id, so re-running an identical approve
  yields the same snapshot id.
- Pinned CI hashes live in
  [`ValidationGoldenHashes`](../../src/ProjectAegis.Data/Validation/ValidationGoldenHashes.cs)
  (`StrikeUnreachable`, `CleanPatrol`). **Changing data that flows into these scenarios means
  re-pinning the constant in the same PR** — a hash diff in CI is a signal, not noise.

## Workbook round-trip (Req 21 / ADR-011)

[`CanonicalTextWorkbookIo`](../../src/ProjectAegis.Data/Platform/CanonicalTextWorkbookIo.cs)
is the dependency-free reference implementation of `IPlatformWorkbookIo`. It is the executable
spec for the production ClosedXML `.xlsx` adapter: **`Read(Write(wb))` must reconstruct the
workbook exactly**. It writes deterministic UTF-8 (no BOM) using an ASCII unit-separator
(`0x1F`) delimiter so round-trip and golden tests run in CI without a spreadsheet library.

## Importer entry point (no direct SQLite)

Bulk import never touches SQLite directly — it stages through the gate. See
[`CmoMarkdownImportProposer`](../../src/ProjectAegis.Data/Import/CmoMarkdownImportProposer.cs):

- `ProposeFromMarkdown` parses, partitions (quarantine), seeds the Baltic DB if missing,
  then `ChunkBindings` (sorted, 500/chunk) → `ProposeSensorBatch`.
- `ProposePlatformWeaponMounts` (`S22-04`) stages platform/weapon/mount batches — **propose
  only, no auto-commit**.
- Both accept an optional `ICatalogClock` (defaults to `FixedCatalogClock(0)`).

## How to add or change a `Catalog*` type without breaking determinism

1. **Define the composite key first.** Pick the minimal column set that is unique and stable
   across releases; never include a timestamp or DB rowid.
2. **Apply it everywhere with `StringComparer.Ordinal`:** the `Propose*Batch` sort, the
   staging read-back, any export, and the hash input — all four must use the *same* key.
3. **Inject the clock.** Take `ICatalogClock` (default `FixedCatalogClock(0)`); never call
   `DateTime.Now`/`UtcNow` in commit or export code.
4. **Add a determinism test** (see below) and pin a golden hash if the type flows into a CI
   golden scenario.
5. **Run GitNexus impact** on the touched symbol before editing (`CatalogWriteGate` is
   CRITICAL extend-only per the Sprint 23 epic), and `detect_changes` before commit.

## Testing & verification

Existing reference tests:

| Test | Asserts |
|------|---------|
| `CatalogWriteGateTests.Propose_approve_writes_sensor_and_change_log` | propose → approve commit + change log |
| `CatalogWriteGateTests.Reject_batch_discards_staging_without_commit` | reject purges staging, no commit |
| `CatalogWriteGateTests.ApproveBatch_AfterPropose_RecordsStableSnapshotHash` | snapshot recorded on approve |
| `DbSnapshotBindingTests.Content_hash_stable_across_two_identical_approve_cycles` | hash stable across runs |
| `DbSnapshotBindingTests.CatalogSnapshotHasher_is_order_independent` | hash invariant to input order |
| `PlatformWorkbookRoundTripTests` | `Read(Write(wb))` exact reconstruction |

Run the determinism-relevant slice (matches the `S23-06` story):

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "WriteGate|Platform|Canonical" -v minimal
```

Full data-layer suite:

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj -v minimal
```

## Common pitfalls

| Pitfall | Symptom | Fix |
|---------|---------|-----|
| Sorting with culture-aware comparer | Golden hash differs across OS/locale | Always `StringComparer.Ordinal` |
| `DateTime.Now` in a commit/export path | Snapshot hash or `batchId` changes per run | Inject `ICatalogClock`; default `FixedCatalogClock(0)` in tests |
| Export key ≠ reader key | Round-trip / golden drift (`PLE-1.3`) | Keep both keys identical when changing columns |
| Assuming non-sensor `ApproveBatch` commits to live tables | Rows staged but not in live table | Sensor-only today; multi-entity is `S23-04` |
| Data change without re-pinning golden | CI red on `ValidationGoldenTests` | Re-pin `ValidationGoldenHashes` in the same PR |
| Aliases rewriting canonical ids | Canonical key churn across releases (`DBI-7.3`) | Resolve aliases upstream; never in the gate |

## See also

- [Catalog Ingestion Pipeline (CMO Markdown + OSINT)](catalog-ingestion-pipeline.md) — the parse half that feeds `Propose*Batch` (markdown format, inference rules, quarantine, OSINT TL routing)
- [Balance Drift Telemetry (Advisory)](balance-drift-telemetry.md) — advisory win-rate drift sink; never writes the catalog
- [ADR-006 — Data Layer Boundary](../architecture/adr-006-data-layer-boundary.md)
- [ADR-011 — Platform Editor Excel Round-trip](../architecture/adr-011-platform-editor-excel-roundtrip.md)
- `Game-Requirements/requirements/06-Database-Intelligence.md` (`DBI-1.1`, `DBI-1.2`, `DBI-7.3`)
- Determinism scanner skill: `.claude/skills/determinism-audit/SKILL.md`
- Replay verification: `.claude/skills/replay-verify/SKILL.md`
