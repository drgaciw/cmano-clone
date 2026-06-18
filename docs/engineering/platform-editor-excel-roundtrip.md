# Platform editor — Excel round-trip

> **Subsystem:** `ProjectAegis.Data.Platform` (engine-free core) + `ProjectAegis.Data.Excel` (ClosedXML adapter)
> **Decision of record:** [ADR-011 — Platform editor Excel round-trip](../architecture/adr-011-platform-editor-excel-roundtrip.md)
> **Requirements:** Req 21 (Platform Editor), Req 06 (Database Intelligence)

The platform editor is the **authoring surface** that sits *in front of* the
[catalog write gate](catalog-write-gate.md). Designers export a bound catalog snapshot to a
multi-sheet workbook, edit it offline, and re-import. The importer **diffs the edited
workbook against its source snapshot** and stages only the changes through
`IWriteGate.Propose*Batch` — it never overwrites blindly and never reaches a live table
without a separate `ApproveBatch` (ADR-011 §4, PLE-3.1).

This page documents the round-trip exactly as it behaves in source today, including the
gap between the deterministic core library (complete) and the CLI verbs (S22 scaffolds).

## Pipeline at a glance

```
export snapshot → PlatformWorkbook (one sheet per domain + _Meta)
        │  PlatformWorkbookExporter.Export(data, snapshotId, clock)
        ▼
   write .xlsx / .platform.txt   ← IPlatformWorkbookIo.Write
        ▼
   edit offline in Excel (cells are text; numbers round-trip byte-for-byte)
        ▼
   read back                     ← IPlatformWorkbookIo.Read
        ▼
PlatformWorkbookImporter.Plan(edited)
        │  re-export bound snapshot → diff → validate → classify changes
        ▼
   .Stage(edited, gate, actor)   → Propose*Batch per changed entity (no commit)
        ▼
   catalog_write_approve --batch <id>   ← write gate commits (see write-gate runbook)
```

The unedited round-trip is the load-bearing invariant: `Read(Write(wb)) == wb`, so a
workbook exported and re-imported with **no edits produces an empty diff** and stages
nothing (`PlatformWorkbookRoundTripTests.Unedited_round_trip_yields_empty_diff`).

## The workbook model

`PlatformWorkbook` (`src/ProjectAegis.Data/Platform/PlatformWorkbook.cs`) is a pure,
spreadsheet-library-free record: an ordered list of `PlatformWorkbookSheet`, each with a
`Header` and deterministically ordered `Rows` of cell strings. `PlatformWorkbookExporter`
emits **six entity sheets plus a `_Meta` sheet** (schema version `007`):

| Sheet | Header columns | Sort key |
|-------|----------------|----------|
| `Platforms` | `PlatformId, LatDeg, LonDeg, CombatRadiusNm` | `PlatformId` |
| `Sensors` | `PlatformId, SensorId, BasePd, ReviewState, TrlLevel, ValueTier, CitationRef` | `PlatformId, SensorId` |
| `Mounts` | `PlatformId, MountId, MountType, ArcDeg, Capacity, ReviewState` | `PlatformId, MountId` |
| `Loadouts` | `PlatformId, LoadoutId, LoadoutName, Role, IsDefault` | `PlatformId, LoadoutId` |
| `Magazines` | `PlatformId, LoadoutId, MountId, WeaponId, Quantity, ReloadTimeSec, Depth` | `PlatformId, LoadoutId, MountId, WeaponId` |
| `Comms` | `PlatformId, LinkId, Role, SatcomCapable, ReviewState, TrlLevel, ValueTier, CitationRef` | `PlatformId, LinkId` |
| `_Meta` | `Key, Value` | n/a |

All rows are sorted with `StringComparer.Ordinal`; numbers are formatted with
`CultureInfo.InvariantCulture` (`"R"` round-trip format for doubles). This ordinal sort +
invariant culture is what makes the export reproducible (`Export_is_deterministic`).

### The `_Meta` sheet

The `_Meta` sheet binds the workbook to its origin and lets the importer detect tampering:

| Key | Value |
|-----|-------|
| `SourceSnapshotId` | the `dbSnapshotId` this workbook was exported from |
| `SchemaVersion` | `007` (`PlatformWorkbookExporter.SchemaVersion`) |
| `ExportUtcTicks` | injected clock ticks (no wall-clock) |
| `WorkbookHash` | content hash of all entity sheets (`PlatformWorkbookHash.Compute`) |

`_Meta` is **excluded from the diff** because its hash and timestamp are derived, not
authored (`PlatformWorkbookDiff` skips `PlatformWorkbookHash.MetaSheetName`). Do not hand-edit
`SourceSnapshotId`: if it does not resolve to a known snapshot the importer refuses to stage
(see *Snapshot guard* below).

## Excel I/O adapters

Excel serialization sits behind the `IPlatformWorkbookIo` port so the `ProjectAegis.Data`
assembly stays engine- and spreadsheet-library-free (ADR-006 boundary). Two implementations
exist:

| Adapter | Project | Format | Used by |
|---------|---------|--------|---------|
| `CanonicalTextWorkbookIo` | `ProjectAegis.Data` | dependency-free text (ASCII unit-separator delimited) | golden tests, **today's CLI** |
| `ClosedXmlPlatformWorkbookIo` | `ProjectAegis.Data.Excel` | real `.xlsx` (ClosedXML, MIT) | production Excel authoring (S23-01) |

Both satisfy the same contract: `Read(Write(wb))` must reconstruct the workbook exactly.
`ClosedXmlPlatformWorkbookIo` pins every column's number format to text (`"@"`) so a
numeric-looking value such as `57` survives the round-trip as the string `"57"` instead of
being coerced to a number — the most likely correctness bug per ADR-011.

## Diff semantics

`PlatformWorkbookDiff.Compare(source, edited)` returns an ordered list of
`PlatformWorkbookChange(Sheet, Kind, RowIndex, Detail)`. Kinds:

| Kind | Meaning |
|------|---------|
| `SheetAdded` / `SheetRemoved` | a whole entity sheet appeared or disappeared |
| `HeaderChanged` | column set changed (cell-level diff is then skipped for that sheet) |
| `RowAdded` / `RowRemoved` | a row exists on only one side at that index |
| `CellChanged` | same row index, one or more cells differ (`Header: 'old' -> 'new'`) |

Because both workbooks are exporter-ordered, an authored change maps to a single
deterministic diff entry — e.g. editing one magazine quantity yields exactly one
`CellChanged` on `Magazines` (`Edited_cell_is_detected_as_a_single_change`).

## Validation (fitting rules)

Before staging, `PlatformWorkbookValidator.Validate` runs cross-sheet referential and
capacity checks that SQLite `CHECK` constraints cannot express at edit time. Findings are
sorted by `(Code, Message)` for golden-hash stability. **Any `Error`-severity finding
blocks staging** (`PlatformImportPlan.Blocked`):

| Code | Rule |
|------|------|
| `PLE-MOUNT-RANGE` | mount `ArcDeg` must be `0..360` and `Capacity >= 0` |
| `PLE-MAG-LOADOUT` | a magazine's `(PlatformId, LoadoutId)` must exist in `Loadouts` |
| `PLE-MAG-MOUNT` | a magazine's `(PlatformId, MountId)` must exist in `Mounts` |
| `PLE-MAG-CAPACITY` | magazine `Quantity` must not exceed the referenced mount's `Capacity` |

## Importer: plan vs stage

`PlatformWorkbookImporter` (constructed with a snapshot provider + injectable
`ICatalogClock`) has two entry points:

- **`Plan(edited)`** — pure, side-effect-free. Resolves the source snapshot, re-exports it,
  diffs, validates, and classifies each change as **supported** or **unsupported**. Returns a
  `PlatformImportPlan`.
- **`Stage(edited, gate, actorType, actorId, rationale)`** — turns an unblocked plan into
  staged write-gate batches via `Propose*Batch`. Returns a `PlatformImportResult` with the
  per-entity batch ids. **It never commits** — commit is a separate `ApproveBatch`.

### Snapshot guard

If `SourceSnapshotId` (read from `_Meta`) does not resolve through the snapshot provider,
`Plan` returns `SnapshotResolved = false` with an empty change list and `Stage` refuses
(`Plan_unknown_snapshot_is_unresolved`). This is PLE-2.2: you cannot safely diff against an
unknown or stale source.

### Supported vs unsupported entities

The importer stages five entity domains and reports the sixth:

| Sheet | Importer behavior |
|-------|-------------------|
| `Sensors`, `Mounts`, `Loadouts`, `Magazines`, `Comms` | **staged** — `Propose{Sensor,Mount,Loadout,Magazine,Comms}Batch` for added/changed rows |
| `Platforms` | **reported only** — surfaced in `UnsupportedChanges`; the P0 write gate has no platform commit path |

`Stage` builds rows only from `CellChanged` / `RowAdded` diff entries, so an unchanged row is
never re-proposed. Editing across several sheets stages one batch per entity type
(`Stage_multiple_entity_types_proposes_all_batches`).

### Bulk-author threshold

`PlatformWorkbookImporter.HumanApprovalRecordThreshold = 10` (DBI-2.4). When the total diff
exceeds 10 changes, `PlatformImportPlan.RequiresHumanApproval` is set so a reviewer cannot
fast-path a large changeset.

## ⚠️ Two gaps a developer will hit

These are the surprises in this subsystem — both are real and verified against source.

1. **`ApproveBatch` is sensor-only.** The importer correctly *proposes* mount / loadout /
   magazine / comms batches, **but the write gate only commits sensor rows today.** Approving
   a non-sensor batch returns `Committed = false, Errors = ["staging_batch_not_found"]`. This
   is the same asymmetry documented in the [write-gate runbook](catalog-write-gate.md#-known-gap-approvebatch-is-sensor-only-as-of-sprint-22)
   and is the scope of story S23-04. Until it lands, only sensor edits round-trip all the way
   to a live table.

2. **The CLI verbs are S22 scaffolds, not the full pipeline.** The three `platform_*_xlsx`
   verbs exercise the gate/exporter/diff surface but do **not** yet load a real workbook or
   call `PlatformWorkbookImporter`:
   - `platform_export_xlsx` exports an **empty** dataset (`PlatformCatalogExportData.Empty`)
     via `CanonicalTextWorkbookIo` (default output `platform-export.platform.txt`), **not**
     `.xlsx` — the ClosedXML adapter is not wired into the CLI.
   - `platform_import_xlsx` opens the write gate and reports the pending-batch count; it does
     **not** read `--in` (`Io.Read` deferred) and does not stage. It points you at
     `catalog_write_approve` as the next step.
   - `platform_diff_xlsx` runs `PlatformWorkbookDiff.Compare` over empty exporter data, so its
     `diffCount` is `0` until real file loading is wired.

   The **library** (`PlatformWorkbookExporter` / `Diff` / `Validator` / `Importer` and
   `ClosedXmlPlatformWorkbookIo`) is complete and unit-tested; the CLI wiring is the remaining
   integration work. Drive the round-trip in tests/code, not yet end-to-end from the CLI.

## Operational reference (headless CLI)

All commands run through `ProjectAegis.MissionEditor.Cli` and emit camelCase JSON.

```bash
# Export (current scaffold: empty data → canonical text, not .xlsx)
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  platform_export_xlsx --db catalog.db --out platform.platform.txt --snapshot baltic_patrol

# Diff two workbooks (scaffold: compares exporter data)
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  platform_diff_xlsx --db catalog.db --base base.txt --edited edited.txt

# Import (scaffold: opens gate, reports pending; commit via write-gate approve)
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  platform_import_xlsx --db catalog.db --in edited.txt \
  --actor-type human --actor-id drgamtd
```

Flags: `--snapshot` (aliases `--snapshot-id`), `--out`/`--output`, `--base`/`--source`,
`--edited`/`--in`/`--input`, `--actor-type` (default `cli`), `--actor-id` (default
`mission-editor`). After a real import stages batches, commit each through the write gate:
`catalog_write_approve --db <path> --batch <batchId>` (see the
[write-gate runbook](catalog-write-gate.md#operational-runbook-headless-cli)).

## Constraints & gotchas

- **Determinism:** construct exporter/importer with a `FixedCatalogClock` in tests; parse and
  format with `InvariantCulture`; never rely on locale or wall-clock.
- **Engine-free boundary:** keep all spreadsheet code behind `IPlatformWorkbookIo`. No
  `UnityEngine` or ClosedXML types may enter `ProjectAegis.Data` (ADR-001/006).
- **Empty diff is the contract:** if an unedited round-trip ever produces a non-empty diff,
  the IO adapter has broken `Read(Write(wb)) == wb` — fix the adapter, not the diff.
- **No bypass:** the importer only ever calls `Propose*Batch`; there is no direct-SQL or
  auto-commit path (DBI-8.3).

## Verify

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "Platform" -v minimal
```

Covers `PlatformWorkbookRoundTripTests` (determinism, empty-diff, single-cell change),
`PlatformWorkbookImporterTests` (snapshot guard, per-entity staging, validation block,
approval threshold), and `PlatformWorkbookValidatorTests` (fitting rules).

## See also

- [Catalog write gate runbook](catalog-write-gate.md) — the propose → approve → commit path this feeds
- [ADR-011 — Platform editor Excel round-trip](../architecture/adr-011-platform-editor-excel-roundtrip.md)
- [ADR-006 — Data layer boundary](../architecture/adr-006-data-layer-boundary.md)
- [ADR-008 — Mission editor validation engine](../architecture/adr-008-mission-editor-validation-engine.md)
- Source — `src/ProjectAegis.Data/Platform/`, `src/ProjectAegis.Data.Excel/`, `src/ProjectAegis.MissionEditor.Cli/Platform*XlsxCommand.cs`
