# Catalog write gate — propose → approve → commit

> **Subsystem:** `ProjectAegis.Data.WriteGate`
> **Decision of record:** [ADR-006 — Data layer boundary](../architecture/adr-006-data-layer-boundary.md)
> **Requirements:** Req 06 (Database Intelligence — auditable, agent-safe writes), Req 21 (Platform Editor)

The write gate is the **only** sanctioned path for mutating the SQLite catalog. No
importer, MCP command, mission editor, or agent writes a live catalog table
directly — every change is **staged**, **reviewed**, and then **committed** under a
human/agent decision with a full audit trail. Sim and Delegation never open SQLite;
they read snapshots and DTOs (ADR-006 §4).

This page documents what the gate actually does today (verified against source),
how to drive it from the CLI, and the known gaps a developer will hit.

## Why it exists

Catalog rows (sensor `basePd`, platform metadata, weapons, mounts, loadouts,
magazines, comms) drive simulation outcomes, so unreviewed writes are a balance and
provenance risk. The gate enforces three guarantees:

1. **Two-phase commit** — a `Propose*Batch` call writes to `catalog_staging_*` tables
   only. Live tables change exclusively inside `ApproveBatch`.
2. **Quarantine on import** — proposals below the TRL/confidence/review thresholds are
   partitioned out and never reach the live table.
3. **Auditability** — every committed field change is appended to `catalog_change_log`
   with actor, previous value, new value, and batch id.

## Core contract

`IWriteGate` (`src/ProjectAegis.Data/WriteGate/IWriteGate.cs`) is implemented by
`CatalogWriteGate`, a `SQLite`-backed, `IDisposable` gate constructed with a database
path and an **injectable** `ICatalogClock` (determinism — no wall-clock in the commit
path, ADR-006 compliance).

| Method | Stages into | Returns |
|--------|-------------|---------|
| `ProposeSensorBatch` | `catalog_staging_sensor` | `batchId` |
| `ProposePlatformBatch` | `catalog_staging_platform` | `batchId` |
| `ProposeWeaponBatch` | `catalog_staging_weapon` | `batchId` |
| `ProposeMountBatch` | `catalog_staging_mount` | `batchId` |
| `ProposeLoadoutBatch` | `catalog_staging_loadout` | `batchId` |
| `ProposeMagazineBatch` | `catalog_staging_magazine` | `batchId` |
| `ProposeCommsBatch` | `catalog_staging_comms` | `batchId` |
| `ApproveBatch` | commits staged rows → live tables + `catalog_change_log` | `WriteGateDecision` |
| `RejectBatch` | purges **all** staging tables for the batch | `WriteGateDecision` |
| `ListPendingBatches` | reads `catalog_staging_batch` where `approval_state = 'proposed'` | summaries |

`WriteGateDecision(bool Committed, string BatchId, IReadOnlyList<string> Errors)` is the
result type. `Committed = false` plus a populated `Errors` list signals a blocked
commit (batch not found, or quarantined rows).

### Batch id shape

`Propose*` methods return a deterministic batch id derived from the entity, row count,
and clock ticks — e.g. `batch-12-2000` (sensor), `batch-platform-3-2000`,
`batch-mount-5-2000`. Rows are sorted by ordinal key before insert so the staged order
is reproducible.

## End-to-end flow

```
Propose*Batch(rows, actorType, actorId, rationale)
        │  INSERT OR REPLACE into catalog_staging_<entity>
        │  INSERT header into catalog_staging_batch (state = 'proposed')
        ▼
ListPendingBatches()            ← reviewer sees pending work
        ▼
ApproveBatch(batchId, actor)    ← LoadStagingRows → PartitionForImport
        │  quarantined rows present?  → Committed=false, Errors=[quarantine:…]
        │  else: UpsertSensor + AppendChangeLog per row, MarkBatchState('approved')
        │  then: best-effort DbSnapshot.RecordApprovedImport (non-fatal)
        ▼
   live `sensor` table + catalog_change_log updated
```

`RejectBatch` marks the header `rejected` and calls `DeleteStagingRows`, which deletes
the batch from **all seven** staging tables — this is the DBI-1.4 orphan guard, so a
rejected mixed-entity batch never leaves dangling staging rows.

### Import gate (quarantine) thresholds

`ApproveBatch` runs staged rows through `CatalogImportGate.PartitionForImport`
(`src/ProjectAegis.Data/Catalog/CatalogImportGate.cs`). A row is **quarantined** (and
the whole approve is blocked) when any of:

| Check | Default | Rejection reason |
|-------|---------|------------------|
| Confidence | `>= 0.5` | `confidence_below_minimum` |
| TRL level | `>= 4` | `trl_below_minimum` |
| Review state | must be `approved` | `review_state_<state>` |

On a bulk import path (`CmoMarkdownImportProposer`), quarantined rows are diverted to a
quarantine table and reported, while only the approved partition is staged.

## ⚠️ Known gap: `ApproveBatch` is sensor-only (as of Sprint 22)

This is the single most common surprise in this subsystem, so document it loudly.

The `Propose*Batch` methods exist for platform, weapon, mount, loadout, magazine, and
comms, **but `ApproveBatch` only commits sensor rows.** Internally `ApproveBatch`
calls `LoadStagingRows`, which reads `catalog_staging_sensor` exclusively. Consequences:

- Proposing a **platform / weapon / mount / loadout / magazine / comms** batch stages
  the rows correctly, but calling `ApproveBatch` on that batch id returns
  `Committed = false, Errors = ["staging_batch_not_found"]` because zero sensor rows
  are found — nothing is committed to the live table.
- `RejectBatch` already handles every entity (it purges all staging tables), so the
  reject path is symmetric even though the approve path is not.

Closing this asymmetry — making `LoadStagingRows` + `ApproveBatch` entity-aware and
adding upsert helpers for the remaining entities — is the scope of **story S23-04**
(`production/epics/sprint-23-platform-phase-b/story-023-04-approve-batch.md`). Until
that lands, treat non-sensor propose paths as "stage and inspect", not "commit".

> **Extend-only rule:** `CatalogWriteGate`, `ApproveBatch`, and `LoadStagingRows` are
> flagged **CRITICAL** in GitNexus impact analysis. Run
> `impact({target: "CatalogWriteGate", direction: "upstream"})` before editing, and keep
> the sensor commit path regression-clean.

## Operational runbook (headless CLI)

All commands run through `ProjectAegis.MissionEditor.Cli`
(`dotnet run --project src/ProjectAegis.MissionEditor.Cli -- <command> …`) and emit
camelCase JSON. If the `--db` file is absent, propose/import commands seed the Baltic
patrol fixture first.

### Propose a single sensor change

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  catalog_write_propose --db catalog.db --platform DDG-51 --sensor SPY-1D --base-pd 0.82
# → { "ok": true, "batchId": "batch-1-1000", "recordCount": 1 }
```

The proposed row defaults to `Confidence 0.9`, `TRL 9`, `ReviewState approved` so it
clears the import gate on approve.

### List pending batches and approve one

```bash
# List
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  osint_staging_review --db catalog.db
# → { "ok": true, "pending": [ { "batchId": "batch-1-1000", "recordCount": 1, … } ] }

# Approve (commits + binds a snapshot)
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  catalog_write_approve --db catalog.db --batch batch-1-1000
# → { "ok": true, "batchId": "batch-1-1000", "releaseVersion": …, "snapshotId": …, "sensorRowCount": N }
```

`catalog_write_approve` also accepts `--snapshot-id` and `--release-version` to control
the post-commit snapshot binding (`CatalogSnapshotBinder.BindAfterApprove`).
`osint_staging_review --approve <batchId>` is an equivalent approve proxy that mirrors
what a Unity staging UI would call.

### Bulk import from CMO markdown

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  catalog_import_markdown --db catalog.db --markdown sensors.md \
  --chunk-size 500 --report-out quarantine.json
```

Parses the markdown, partitions through the import gate, and stages the approved
partition as one or more sensor batches (chunked). **It does not auto-commit** — you
must still `catalog_write_approve` each returned batch (PLE-3.1: no path commits without
an explicit approve).

## Schema touchpoints

Staging tables live in `assets/data/catalog/migrations/007_platform_editor_phase_a.sql`
(`catalog_staging_{platform,weapon,mount,loadout,magazine,comms}`); the sensor staging
table and `catalog_staging_batch` header / `catalog_change_log` come from earlier P0
migrations. `CatalogWriteGate.EnsureSchema()` bootstraps the schema via
`SqliteCatalogReader` on construction.

## Constraints & gotchas

- **Determinism:** always construct the gate with a `FixedCatalogClock` in tests; never
  rely on wall-clock. The snapshot binding after approve is **best-effort** and is
  swallowed on failure so it cannot break a valid commit.
- **All-or-nothing approve:** if *any* staged row quarantines, the entire batch is
  blocked — fix or reject, then re-propose.
- **Connection pooling is disabled** (`Pooling=false`) and `Dispose` clears all pools;
  expect one gate per logical operation.
- **Sim/Delegation isolation:** do not add catalog SQLite access outside
  `ProjectAegis.Data` (ADR-006 validation criteria).

## Verify

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "WriteGate|Platform|CmoMarkdown" -v minimal
```

## See also

- [Platform editor Excel round-trip runbook](platform-editor-excel-roundtrip.md) — the authoring surface that stages batches into this gate
- [ADR-006 — Data layer boundary](../architecture/adr-006-data-layer-boundary.md)
- [ADR-011 — Platform editor Excel round-trip](../architecture/adr-011-platform-editor-excel-roundtrip.md)
- Story S23-04 — `production/epics/sprint-23-platform-phase-b/story-023-04-approve-batch.md`
- Source — `src/ProjectAegis.Data/WriteGate/`, `src/ProjectAegis.MissionEditor.Cli/CatalogWrite*Command.cs`
