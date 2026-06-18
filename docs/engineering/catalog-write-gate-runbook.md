# Catalog write gate — staged write runbook

Developer/operator guide for the **catalog write gate** — the single governed
path by which any catalog mutation reaches live tables. The architectural
decision lives in [`ADR-006`](../architecture/adr-006-data-layer-boundary.md)
(req-06); this doc is the **how-to**: the propose → approve → commit lifecycle,
the per-domain propose surface, the quarantine gate, the CLI/MCP verbs, and the
constraints you must not break.

The headless service lives in `src/ProjectAegis.Data/WriteGate/` (namespace
`ProjectAegis.Data.WriteGate`); the CLI verbs are in
`src/ProjectAegis.MissionEditor.Cli/`.

## Intent

Per ADR-006, **all catalog mutations go through `IWriteGate`** —
staging → validate → commit → optional new snapshot. There is no direct-SQL or
auto-commit path into the live `sensor` (and future catalog) tables. Every other
front door (the CMO markdown importer, the OSINT proposal pipeline, the Excel
platform editor) stages through this same gate rather than writing a parallel
path.

The gate enforces three invariants:

- **Propose-only.** A `Propose*Batch` call writes to **staging** tables only and
  returns a `batchId`. Nothing reaches live tables until a separate `ApproveBatch`.
- **Human-gated commit.** `ApproveBatch` is the only commit path; rejected or
  quarantined rows never land.
- **Deterministic + auditable.** Batches sort by ordinal key, use an injectable
  clock (no wall-clock), and every committed field change is appended to
  `catalog_change_log` with actor and provenance.

## Lifecycle

```
Propose*Batch  →  (proposed)  →  ApproveBatch  →  (approved, committed + change-log + snapshot)
                              ↘  RejectBatch   →  (rejected, staging purged)
```

`catalog_staging_batch.approval_state` moves through `proposed` →
`approved` | `rejected`. `ListPendingBatches()` returns only `proposed`
batches, ordered by `proposed_utc_ticks` then `batch_id`.

`batchId` is `batch[-{domain}]-{recordCount}-{utcTicks}` (e.g.
`batch-mount-3-2000`). It is derived from the `ICatalogClock` tick, so with a
`FixedCatalogClock` the id is reproducible — handy for tests and replay binding.

## Propose surface

`IWriteGate` (`src/ProjectAegis.Data/WriteGate/IWriteGate.cs`) exposes one
propose method per catalog domain. Each rejects an empty list
(`ArgumentException`), sorts rows by ordinal canonical key, writes a batch header
plus staging rows in a **single transaction**, and returns the `batchId`.

| Domain | Propose method | Record type | Sort key |
|--------|----------------|-------------|----------|
| Sensors | `ProposeSensorBatch` | `CatalogSensorBinding` | platform, sensor |
| Mounts | `ProposeMountBatch` | `CatalogMount` | platform, mount |
| Loadouts | `ProposeLoadoutBatch` | `CatalogLoadout` | platform, loadout |
| Magazines | `ProposeMagazineBatch` | `CatalogMagazineEntry` | platform, loadout, mount, weapon |
| Comms | `ProposeCommsBatch` | `CatalogCommsBinding` | platform, link |
| Platforms | `ProposePlatformBatch` (S22-04) | `CatalogPlatformBinding` | platform |
| Weapons | `ProposeWeaponBatch` (S22-04) | `CatalogWeaponRecord` | weapon |

Every method takes `actorType`, `actorId`, and an optional `rationale`, all
persisted on the batch header for the audit trail.

## Approve and the quarantine gate

`ApproveBatch(batchId, actorType, actorId)` returns a `WriteGateDecision`
(`Committed`, `BatchId`, `Errors`):

1. Loads the batch's **sensor** staging rows. An unknown batch returns
   `Committed = false` with `["staging_batch_not_found"]`.
2. Runs `CatalogImportGate.PartitionForImport` into `(approved, quarantined)`.
   The gate is **all-or-nothing**: if any row is quarantined, the whole batch is
   rejected, nothing commits, and `Errors` lists each as
   `quarantine:{platform}/{sensor}:{reason}`.
3. Otherwise, in one transaction, upserts each approved row into the live
   `sensor` table, appends a `catalog_change_log` entry for `base_pd`
   (previous → new value, actor, `approval_state = approved`), and marks the
   batch `approved`.
4. After commit, records a stable snapshot via
   `DbSnapshotStore.RecordApprovedImport` (best-effort: a snapshot failure is
   swallowed so it never fails an otherwise-valid approve).

### Quarantine reasons

`PartitionForImport` (`src/ProjectAegis.Data/Catalog/CatalogImportGate.cs`)
applies these defaults; the first failing check wins:

| Check | Default | Rejection reason |
|-------|---------|------------------|
| Confidence | `>= 0.5` (`DefaultMinimumConfidence`) | `confidence_below_minimum` |
| TRL level | `>= 4` (`DefaultMinimumTrl`) | `trl_below_minimum` |
| Review state | `== approved` (`requireApproved`) | `review_state_{state}` |

## Reject

`RejectBatch(batchId, actorType, actorId, rationale)` marks the batch `rejected`
and **purges that batch's rows from all seven staging tables** (sensor, platform,
weapon, mount, loadout, magazine, comms — the S22-04 / DBI-1.4 no-orphan guard).
It deliberately returns `Committed = false` (a reject is not a commit); an
unknown batch returns `["staging_batch_not_found"]`.

## CLI / MCP

| Verb | Purpose | Required flags |
|------|---------|----------------|
| `catalog_write_propose` | Stage a single sensor row | `--db --platform --sensor --base-pd` |
| `catalog_write_approve` | Approve + commit a batch, then bind snapshot | `--db --batch` (opt. `--snapshot-id`, `--release-version`) |

```bash
# Propose a sensor row (stages a batch, prints batchId)
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  catalog_write_propose --db <catalog.db> --platform P --sensor S --base-pd 0.7

# Approve it (commits to the live sensor table + change log + snapshot)
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  catalog_write_approve --db <catalog.db> --batch <batchId>
```

`catalog_write_propose` stages the row as `actorType = agent`,
`actorId = database-intelligence` with `ReviewState = approved`;
`catalog_write_approve` commits as `actorType = human`, `actorId = reviewer-mcp`
and then runs `CatalogSnapshotBinder.BindAfterApprove` to emit the release
version, snapshot id, content hash, and sensor row count.

Other front doors reuse the same gate: `platform_import_xlsx` stages workbook
edits (see [`platform-editor-excel-roundtrip.md`](platform-editor-excel-roundtrip.md))
and `osint_staging_review --approve <batchId>` commits OSINT proposals (see
[`osint-ingestion-runbook.md`](osint-ingestion-runbook.md)). `RejectBatch` and
`ListPendingBatches` are part of the `IWriteGate` API but are exercised through
those flows / tests, not standalone CLI verbs.

## Determinism & provenance

- **Injectable clock.** `ICatalogClock` (default `FixedCatalogClock(0)`; CLI
  approve uses `FixedCatalogClock(2000)`) drives batch ids, change-log ticks, and
  `revised_utc_ticks`. No `DateTime.UtcNow` in the commit path (ADR-006).
- **Ordinal ordering.** All propose sorts and the staging read-back use
  `StringComparer.Ordinal`, so batch contents and the change log are byte-stable
  across machines and locales.
- **Provenance tier.** Sensor and platform staging/commit normalize the value
  tier via `CatalogProvenanceTier.Normalize`; TRL, confidence, review state, and
  citation ride along on each row for the gate and the audit trail.

## Constraints / pitfalls

- **Approve commits sensors only (today).** `ApproveBatch` loads and upserts
  `catalog_staging_sensor` rows into the live `sensor` table. The platform /
  weapon / mount / loadout / magazine / comms domains can be **staged** by their
  `Propose*Batch` methods, but approve does not yet materialize them into live
  tables. Don't assume an approved platform batch is queryable until that commit
  path is wired.
- **Quarantine is all-or-nothing.** One low-confidence / low-TRL / non-approved
  row fails the entire batch. Split clean rows into their own batch, or fix the
  failing rows, rather than expecting partial commit.
- **No auto-commit, ever.** A `Propose*Batch` only stages. Nothing reaches live
  tables without `ApproveBatch`. A write-gate parity test enforces this for the
  importer path — keep it that way.
- **Reject returns `Committed = false` by design.** That is success for a
  rejection, not an error; check `Errors` to distinguish a real failure
  (`staging_batch_not_found`) from a normal discard.
- **Snapshot binding is best-effort on approve.** The post-commit snapshot is
  wrapped in a swallow so it never fails a valid approve — if you depend on the
  snapshot, verify it via `DbSnapshotStore` rather than the approve return value.
- **Empty batches throw.** Every `Propose*Batch` rejects a zero-row list with
  `ArgumentException`; guard upstream so the gate isn't called with nothing.
- **Keep the clock injected.** Introducing a wall-clock read anywhere in
  propose/approve breaks determinism and the reproducible `batchId`.

## Tests

- `src/ProjectAegis.Data.Tests/WriteGate/CatalogWriteGateTests.cs` — propose →
  approve writes sensor + change log, reject discards staging without commit, and
  approve records a stable snapshot hash.
- `src/ProjectAegis.Data.Tests/Catalog/CatalogImportGateTests.cs` — the
  confidence / TRL / review-state partition thresholds.
- `src/ProjectAegis.Data.Tests/Platform/PlatformWorkbookImporterTests.cs` —
  write-gate parity for the Excel importer (no auto-commit).

## Related docs

- [`ADR-006`](../architecture/adr-006-data-layer-boundary.md) — data-layer
  boundary and the write-gate decision.
- [`platform-editor-excel-roundtrip.md`](platform-editor-excel-roundtrip.md) —
  the Excel front door onto this gate.
- [`osint-ingestion-runbook.md`](osint-ingestion-runbook.md) — the OSINT proposal
  pipeline that stages through this gate.
