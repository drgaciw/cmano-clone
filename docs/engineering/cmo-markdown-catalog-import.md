# CMO markdown → catalog import subsystem

> **Engineering reference + runbook.** This page documents how the CMO markdown import path (`src/ProjectAegis.Data/Import/`) behaves today and how to drive it. The editable schema it stages into is specified in [Req 21 — Platform Editor](../../Game-Requirements/requirements/21-Platform-Editor.md); the staged-write contract it rides on is [ADR-006 — Data layer boundary](../architecture/adr-006-data-layer-boundary.md) and [Req 06 — Database Intelligence](../../Game-Requirements/requirements/06-Database-Intelligence.md). For the spreadsheet authoring path that shares the same write gate, see [`platform-editor-excel-roundtrip.md`](platform-editor-excel-roundtrip.md).

This subsystem turns **cmano-db.com markdown exports** (sensors, platforms, weapons) into catalog rows and **stages them through the write gate** — it never writes SQLite directly and never auto-commits. The parser is pure text-to-record; the proposer opens a [`CatalogWriteGate`](../../src/ProjectAegis.Data/WriteGate/CatalogWriteGate.cs) and emits one or more pending batches that a human (or the `catalog_write_approve` verb) must approve.

## Intent

| Goal | How it is met |
|------|---------------|
| Reuse public reference data | `CmoMarkdownImporter` parses the cmano-db markdown structure (`### <title>` sections, `/sensor/…/`, `/ship/…/`, `/weapon/…/` paths, `\| Key \| Value \|` rows) |
| Never write blind | The proposer only ever calls `IWriteGate.Propose*Batch`; there is no path from markdown to a live table that bypasses the gate (ADR-006, DBI-1) |
| Reject low-quality rows | Sensor imports pass through `CatalogImportGate.PartitionForImport` (confidence + TRL + review-state gate); rejected rows are **quarantined**, not staged (DBI / S19-02) |
| Deterministic + CI-testable | Every reader sorts output by canonical IDs (`StringComparer.Ordinal`); all number parsing uses `InvariantCulture`; fixtures drive golden tests |
| Engine-free (ADR-006) | Lives in `ProjectAegis.Data` with no spreadsheet/engine dependency; the only I/O is `File.ReadAllText` plus SQLite via the gate |

## Architecture at a glance

```
cmano-db markdown (.md) ──► CmoMarkdownImporter   (pure parse, no DB)
        │                        ├─ ReadSensorBindings    ──► CatalogSensorBinding[]
        │                        ├─ ReadPlatformBindings   ──► CatalogPlatformBinding[]
        │                        ├─ ReadWeaponBindings     ──► CatalogWeaponRecord[]
        │                        └─ ReadPlatformMounts     ──► CatalogMount[]
        ▼
CmoMarkdownImportProposer
   ├─ ProposeFromMarkdown (sensors)
   │     ├─ CatalogImportGate.PartitionForImport ──► (approved, quarantined)
   │     ├─ ChunkBindings(approved, 500)
   │     ├─ gate.ProposeSensorBatch(chunk) ─────────► catalog_staging_sensor   (pending)
   │     └─ CatalogJsonImporter.WriteQuarantineRows(quarantined)
   │
   └─ ProposePlatformWeaponMounts (S22-04)
         ├─ gate.ProposePlatformBatch ──► catalog_staging_platform   (pending)
         ├─ gate.ProposeWeaponBatch   ──► catalog_staging_weapon     (pending)
         └─ gate.ProposeMountBatch    ──► catalog_staging_mount      (pending)
                                              │
                       catalog_write_approve ──► IWriteGate.ApproveBatch   (human commit; sensors today — see below)
```

The proposer seeds a Baltic-patrol catalog DB (`CatalogSeedBootstrap.SeedBalticPatrol`) if the target file does not exist, then opens the gate with a `FixedCatalogClock(0)` by default so batch IDs and timestamps are reproducible in tests.

### Key types (`src/ProjectAegis.Data/Import/`)

| Type | Responsibility |
|------|----------------|
| `CmoMarkdownImporter` | Static parser. `Read*Bindings[FromText]` for sensors, platforms, weapons, mounts; plus the inference helpers (`InferBasePd`, `InferDomain`, `InferMountType`, `SlugPlatformId`, `SlugWeaponMountId`). No DB access |
| `CmoMarkdownImportProposer` | Parses + stages through the gate. `ProposeFromMarkdown` (sensors, with quarantine) and `ProposePlatformWeaponMounts` (platform/weapon/mount). `ChunkBindings` splits sensors into ≤500-row batches |
| `CmoMarkdownImportResult` | Sensor run summary: `ParsedCount`, `ApprovedCount`, `QuarantinedCount`, `Batches`, `QuarantineReport` |
| `CmoMarkdownPlatformImportResult` | S22-04 run summary: per-domain counts and the three nullable batch IDs (`null` when that domain produced no rows) |
| `CmoMarkdownQuarantineReportEntry` | One structured quarantine row (`PlatformId`, `SensorId`, `Reason`, `SourceFile`) for the JSON report |

### Records produced (`src/ProjectAegis.Data/Catalog/`)

| Record | Notes |
|--------|-------|
| `CatalogSensorBinding` | Sensors land **Approved**, TRL 9, `InterpretedValue` tier, `cmo-sensor-<id>` ID, `base_pd` inferred from range |
| `CatalogPlatformBinding` | Platform metadata (`display_name`, `domain`, `platform_class`, `nationality`); lands **Provisional**. Distinct from scenario-position `CatalogPlatformEntry` |
| `CatalogWeaponRecord` | `cmo-weapon-<id>` ID, `max_range_meters` from the largest `… Max: N km` row; lands **Provisional** |
| `CatalogMount` | One mount per weapon line on a platform; `mount_type` inferred from the weapon type; lands **Provisional** |

## Parsing & inference rules

The parser is a line scanner with a per-section accumulator that **flushes** on each `### ` heading (and at EOF). Rules, verified against the code:

- **Section key.** A record is only emitted once its numeric ID path is found: `/sensor/<n>/`, `/(ship|aircraft|submarine|facility)/<n>/`, or `/weapon/<n>/`. A section with a title but no ID path is dropped.
- **Entity ID slugs.** `SlugPlatformId` takes the text before the first comma, lower-cases it, replaces non-word runs with `-`, trims, and caps at 64 chars (empty → `unknown-platform`). `SlugWeaponMountId` is the same over a weapon name.
- **`InferBasePd(range, unit)`** maps a sensor's max range to a detection probability, clamped to `[0.05, 1.0]`: `nm → /200`, `km → /300`, `m → /370400`; unknown unit → `0.5`.
- **`InferDomain(class)`**: `aircraft`/`helicopter` → `air`, `submarine` → `subsurface`, `facility`/`land` → `land`, else `surface`.
- **`InferMountType(weaponType)`**: contains `gun` → `gun`, `torpedo`/`tube` → `tube`, `vls` → `vls`, else `rail`.
- **Sensor confidence** defaults to `0.85` per section and is overridden by a `\| Confidence \| N \|` row.
- **Weapon range** is only read inside the `**Weapons**` block, taking the max of any `(Air\|Surface\|Land\|Sub) Max: N km` line and converting km→m.
- **`maxRecords`** caps the number of emitted records (checked at each heading flush); `mapBalticIds` remaps three known fixture slugs (`patrol-frigate-u1` → `u1`, etc.) so platform/mount imports line up with the seeded Baltic scenario.

All readers return arrays **sorted by canonical ID** (`PlatformId`, then `SensorId`/`MountId`; `WeaponId` for weapons) using `StringComparer.Ordinal`.

## Governance: what gets staged vs committed

`CmoMarkdownImportProposer` only ever *proposes*. The two entry points behave differently:

### Sensors (`ProposeFromMarkdown`) — fully wired

1. Parse → `CatalogImportGate.PartitionForImport`. A row is **quarantined** (not staged) when its `GetRejectionReason` fires:
   - `confidence_below_minimum` — confidence `< 0.5` (`DefaultMinimumConfidence`).
   - `trl_below_minimum` — TRL `< 4` (`DefaultMinimumTrl`).
   - `review_state_<state>` — review state is not `Approved` (CMO sensors are imported Approved, so this gate is mainly relevant to re-imports / other sources).
2. Approved rows are chunked at `DefaultChunkSize` (**500**) and each chunk is staged via `ProposeSensorBatch` into `catalog_staging_sensor`.
3. Quarantined rows are persisted via `CatalogJsonImporter.WriteQuarantineRows` and surfaced in `QuarantineReport`.
4. `ApproveBatch` later partitions the staged rows again (defence in depth), upserts into `sensor`, writes a `base_pd` change-log entry, and records an approval snapshot (`DbSnapshotStore`, non-fatal).

### Platforms / weapons / mounts (`ProposePlatformWeaponMounts`) — staged, commit not yet wired

These are staged into `catalog_staging_platform` / `_weapon` / `_mount` (schema in [`007_platform_editor_phase_a.sql`](../../assets/data/catalog/migrations/007_platform_editor_phase_a.sql)) but **`ApproveBatch` does not yet commit them**: its `LoadStagingRows` reads only `catalog_staging_sensor`, so approving a platform/weapon/mount batch returns `staging_batch_not_found`. The rows are durable and visible via `ListPendingBatches`, and `RejectBatch` cleans them up (`DeleteStagingRows` purges all seven staging tables — the DBI-1.4 orphan guard). Wiring the platform/weapon/mount **commit** path (upsert into the live `platform` / `weapon_catalog` / `platform_mount` tables) is the remaining step.

> **Bottom line:** sensor import is end-to-end (propose → approve → commit); platform/weapon/mount import is propose-only today. Treat staged platform batches as a review artifact, not a committed change, until the approve path lands.

## Determinism constraints (read before editing this code)

This subsystem feeds golden/fixture tests; non-determinism breaks them.

- **Stable sort keys.** Every reader and `ChunkBindings` orders by canonical IDs with `StringComparer.Ordinal`. Keep new fields ordered the same way or fixture assertions drift.
- **Always `InvariantCulture`.** Every `int.Parse`/`double.Parse` uses `CultureInfo.InvariantCulture`; locale drift is the most likely correctness bug.
- **No wall clock.** Time enters through `ICatalogClock`; the proposer defaults to `FixedCatalogClock(0)` so batch IDs (`batch-platform-<count>-<ticks>`, etc.) are reproducible. Pass a real clock only in production.
- **Regexes are `Compiled` and anchored.** Changing a heading/path/row pattern changes what sections flush — re-run the importer tests after any regex edit.

## CLI / MCP verb

The wired production caller is `catalog_import_markdown` on `ProjectAegis.MissionEditor.Cli` (sensor path):

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  catalog_import_markdown --db <catalog.db> --markdown <sensor.md> \
  [--max-records N] [--chunk-size 500] [--report-out report.json]
```

It prints `McpToolResult`-style JSON (`{ ok, parsedCount, approvedCount, quarantinedCount, batchCount, batches[], nextStep, quarantineReport[]? }`) and never auto-commits. The reported `nextStep` is:

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  catalog_write_approve --db <catalog.db> --batch <batchId>
```

`ProposePlatformWeaponMounts` (platform/weapon/mount) is exercised by tests and `CatalogIntelligenceRunCommand`; it does not yet have a dedicated top-level CLI verb.

### Programmatic usage

```csharp
// Sensors: parse, gate, and stage in ≤500-row batches
CmoMarkdownImportResult sensors = CmoMarkdownImportProposer.ProposeFromMarkdown(
    databasePath: "catalog.db",
    markdownPath: CmoMarkdownImporter.ResolveMiniFixturePath());

foreach (var batch in sensors.Batches)
    Console.WriteLine($"{batch.BatchId}: {batch.RecordCount} rows pending");
foreach (var q in sensors.QuarantineReport)
    Console.WriteLine($"quarantined {q.PlatformId}/{q.SensorId}: {q.Reason}");

// Platform + weapon + mount (staged; commit path not yet wired)
CmoMarkdownPlatformImportResult pwm = CmoMarkdownImportProposer.ProposePlatformWeaponMounts(
    databasePath: "catalog.db",
    platformMarkdownPath: CmoMarkdownImporter.ResolveBalticPlatformFixturePath(),
    weaponMarkdownPath: CmoMarkdownImporter.ResolveMiniWeaponFixturePath(),
    mapBalticPlatformIds: true);
```

## Common pitfalls

- **A section silently disappears.** The parser only emits a record once it finds the numeric ID path (`/sensor/…/`, `/ship/…/`, `/weapon/…/`). A section heading with no recognizable path is dropped — check the markdown matches the cmano-db structure.
- **All sensor rows quarantined.** Confidence below `0.5`, TRL below `4`, or a non-`Approved` review state. Inspect `QuarantineReport[*].Reason`; the JSON `quarantineReport` block only appears when `quarantinedCount > 0`.
- **Approving a platform batch fails with `staging_batch_not_found`.** Expected today — only sensor batches commit. The platform/weapon/mount rows are staged and visible via `ListPendingBatches`, but their approve→upsert path is not yet implemented.
- **Platform IDs don't match the scenario.** Pass `mapBalticIds: true` (readers) / `mapBalticPlatformIds: true` (proposer) to remap fixture slugs onto the seeded Baltic platform IDs.
- **Weapon range reads as 0.** Range is only parsed inside the `**Weapons**` block from `… Max: N km` lines; a weapon section without that block yields `MaxRangeMeters == 0`.
- **Non-reproducible batch IDs.** Batch IDs embed `clock.UtcTicks`. Tests must pass `FixedCatalogClock(0)` (the default) — a real clock makes IDs and snapshots vary per run.

## Where things live

| Path | Content |
|------|---------|
| `src/ProjectAegis.Data/Import/CmoMarkdownImporter.cs` | Pure markdown parser + inference helpers |
| `src/ProjectAegis.Data/Import/CmoMarkdownImportProposer.cs` | Gate-staging entry points, chunking, result records |
| `src/ProjectAegis.Data/Catalog/CatalogImportGate.cs` | Confidence/TRL/review-state partition + quarantine reasons |
| `src/ProjectAegis.Data/WriteGate/CatalogWriteGate.cs` | `Propose{Sensor,Platform,Weapon,Mount}Batch`, `ApproveBatch`, `RejectBatch`, staging-table orphan guard |
| `assets/data/catalog/migrations/007_platform_editor_phase_a.sql` | `catalog_staging_{platform,weapon,mount,…}` schema |
| `tools/cmano-db-crawler/fixtures/` | `sensor-mini.md`, `baltic-platform-mini.md`, `weapon-mini.md` import fixtures |
| `src/ProjectAegis.MissionEditor.Cli/CatalogImportMarkdownCommand.cs` | `catalog_import_markdown` verb handler |
| `Game-Requirements/requirements/21-Platform-Editor.md` | Editable platform/weapon/mount schema |

## Verify

```bash
# Parser + proposer + bulk import (engine-free; no ClosedXML needed)
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "CmoMarkdown" -v minimal

# CLI surface for the sensor import verb
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "CatalogImportMarkdown" -v minimal
```
