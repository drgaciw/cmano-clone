# Platform Editor — Excel round-trip pipeline

The headless authoring surface for platform configuration (platforms, sensors,
mounts, loadouts, magazines, comms). Implements requirement
**[21-Platform-Editor](../../../Game-Requirements/requirements/21-Platform-Editor.md)**
under **[ADR-011](../../../docs/architecture/adr-011-platform-editor-excel-roundtrip.md)**,
on top of the data-layer boundary from
[ADR-006](../../../docs/architecture/adr-006-data-layer-boundary.md).

A bound catalog snapshot is exported to a multi-sheet workbook, edited offline
(in Excel or any equivalent tool), then re-imported. The importer **diffs the
edited workbook against its source snapshot and stages only the changes** through
the [write gate](../README.md#core-invariant-the-write-gate) — it is never a blind
overwrite, and it never reaches live tables without a separate `ApproveBatch`.

Everything in this directory is **engine-free and deterministic**: it operates on
an in-memory [`PlatformWorkbook`](PlatformWorkbook.cs) model and never references a
spreadsheet library. The real `.xlsx` serialization lives behind the
[`IPlatformWorkbookIo`](IPlatformWorkbookIo.cs) port in the separate
[`ProjectAegis.Data.Excel`](../../ProjectAegis.Data.Excel/README.md) adapter, so
the ClosedXML dependency stays out of the deterministic core (ADR-006).

## Round-trip workflow

```
   bound snapshot (PlatformCatalogExportData)
        │  PlatformWorkbookExporter.Export(data, snapshotId, clock)
        ▼
   PlatformWorkbook  ── IPlatformWorkbookIo.Write ──►  .xlsx  ──►  edit offline
        │                                                              │
        │                                          IPlatformWorkbookIo.Read
        ▼                                                              ▼
   PlatformWorkbookImporter.Plan(edited)
        │   1. read _Meta.SourceSnapshotId  → re-export that snapshot
        │   2. PlatformWorkbookDiff.Compare(source, edited)
        │   3. PlatformWorkbookValidator.Validate(edited)
        │   4. split changes → supported (Sensors/Mounts/Loadouts/Magazines/Comms)
        │                       vs unsupported (Platforms)
        ▼
   PlatformImportPlan ── Stage(gate, actor…) ──► IWriteGate.Propose*Batch
        ▼
   pending batch  ── human/TL ApproveBatch ──►  committed catalog
```

An **unedited** round-trip must produce an empty diff and therefore stage nothing
(ADR-011 §1, PLE-2.1). That property is what makes "export, change three cells,
re-import" safe.

## Types

| Type | Role |
|------|------|
| [`PlatformWorkbook`](PlatformWorkbook.cs) / `PlatformWorkbookSheet` | Engine-free in-memory model: ordered sheets, each an ordered header + ordered rows of cell strings. |
| [`PlatformCatalogExportData`](PlatformCatalogExportData.cs) | The catalog rows the exporter consumes — decoupled from `ICatalogReader` so the exporter stays a pure function of its input. |
| [`PlatformWorkbookExporter`](PlatformWorkbookExporter.cs) | Pure, deterministic catalog → workbook. Builds one sheet per domain plus the `_Meta` sheet. |
| [`IPlatformWorkbookIo`](IPlatformWorkbookIo.cs) | Port that isolates serialization. Contract: `Read(Write(wb)) == wb`. |
| [`CanonicalTextWorkbookIo`](CanonicalTextWorkbookIo.cs) | Dependency-free reference `IPlatformWorkbookIo` (ASCII control delimiters). Runs in CI without ClosedXML and is the executable spec the `.xlsx` adapter must satisfy. |
| [`PlatformWorkbookDiff`](PlatformWorkbookDiff.cs) | Structural diff between two exporter-ordered workbooks (`SheetAdded/Removed`, `HeaderChanged`, `RowAdded/Removed`, `CellChanged`). Excludes `_Meta`. |
| [`PlatformWorkbookValidator`](PlatformWorkbookValidator.cs) | Deterministic cross-sheet fitting rules that SQLite CHECK constraints can't express at edit time. |
| [`PlatformWorkbookHash`](PlatformWorkbookHash.cs) | SHA-256 over data sheets (excluding `_Meta`), locale-independent. Feeds `_Meta.WorkbookHash` and golden tests. |
| [`PlatformWorkbookImporter`](PlatformWorkbookImporter.cs) | Workbook → write-gate batches. `Plan` (pure analysis) and `Stage` (side-effecting propose). |
| [`PlatformImportPlan`](PlatformImportPlan.cs) / `PlatformImportResult` | Pure analysis result and the staging outcome (batch ids + notes). |

## Sheet schema (Phase A)

One worksheet per entity domain, plus a trailing read-only `_Meta` sheet. Schema
version is `PlatformWorkbookExporter.SchemaVersion` (`"007"`). Columns are written
in exporter order; the diff compares headers by ordinal sequence, so reordering or
renaming a column registers as a `HeaderChanged` (and suppresses cell-level diff
for that sheet).

| Sheet | Columns |
|-------|---------|
| `Platforms` | `PlatformId, LatDeg, LonDeg, CombatRadiusNm` |
| `Sensors` | `PlatformId, SensorId, BasePd, ReviewState, TrlLevel, ValueTier, CitationRef` |
| `Mounts` | `PlatformId, MountId, MountType, ArcDeg, Capacity, ReviewState` |
| `Loadouts` | `PlatformId, LoadoutId, LoadoutName, Role, IsDefault` |
| `Magazines` | `PlatformId, LoadoutId, MountId, WeaponId, Quantity, ReloadTimeSec, Depth` |
| `Comms` | `PlatformId, LinkId, Role, SatcomCapable, ReviewState, TrlLevel, ValueTier, CitationRef` |
| `_Meta` | `Key, Value` rows: `SourceSnapshotId`, `SchemaVersion`, `ExportUtcTicks`, `WorkbookHash` |

The `_Meta` sheet binds the workbook to the snapshot it was exported from. The
importer reads `SourceSnapshotId` to re-export the original and diff against it; a
missing or unresolvable snapshot makes a safe diff impossible, so the plan returns
`SnapshotResolved = false` and stages nothing (PLE-2.2).

## Governance: supported vs. unsupported changes

`PlatformWorkbookImporter.Plan` classifies every diff entry:

- **Supported** (staged through the gate today): `Sensors`, `Mounts`, `Loadouts`,
  `Magazines`, `Comms` — each proposed via the matching `IWriteGate.Propose*Batch`.
- **Unsupported** (reported, never staged): `Platforms`. The P0 write gate excludes
  platform-core changes; they surface as `PlatformImportPlan.UnsupportedChanges` and
  a note, pending a gate extension. The importer **never invents a commit path that
  bypasses `IWriteGate`** (DBI-8.3 guardrail).

Staging is refused when:

- the source snapshot did not resolve (`SnapshotResolved == false`), or
- validation produced any **error**-severity finding (`PlatformImportPlan.Blocked`).

Commits affecting more than `PlatformWorkbookImporter.HumanApprovalRecordThreshold`
(10) rows set `RequiresHumanApproval` (DBI-2.4). Even below that, `Stage` only
**proposes** — a separate human/TL `ApproveBatch` is always required to commit
(PLE-3.1).

> **Provenance caveat.** `Stage` rebuilds rows from the editor-surfaced columns
> only. Unsurfaced provenance (`SourceFactId`, `Confidence`, `ImportBatchId`, …)
> currently takes record defaults; a full implementation merges it from the source
> row. Treat round-tripped provenance on *changed* rows as not-yet-authoritative.

## Validation rules

`PlatformWorkbookValidator` emits [`ValidationFinding`](../Validation) entries,
sorted by `(Code, Message)` for golden-hash stability:

| Code | Severity | Condition |
|------|----------|-----------|
| `PLE-MOUNT-RANGE` | Error | Mount `ArcDeg` outside `[0, 360]` or negative `Capacity`. |
| `PLE-MAG-LOADOUT` | Error | A magazine references a `(PlatformId, LoadoutId)` with no matching `Loadouts` row. |
| `PLE-MAG-MOUNT` | Error | A magazine references a `(PlatformId, MountId)` with no matching `Mounts` row. |
| `PLE-MAG-CAPACITY` | Error | A magazine's `Quantity` exceeds the referenced mount's `Capacity`. |

## Determinism contract

- Exporter sorts every sheet by canonical keys (`PlatformId`, then domain id) with
  `StringComparer.Ordinal`, and formats all cells with
  `CultureInfo.InvariantCulture` (`"R"` for doubles, invariant ints, `true`/`false`
  for bools). Locale/number drift is the most likely correctness bug — keep parsing
  invariant too.
- `PlatformWorkbookHash` and the diff exclude `_Meta` because its hash and timestamp
  are derived, not authored.
- CI pins golden hashes for round-trip fidelity, the empty-diff property, and the
  validation report. If you intentionally change a sheet's shape, the schema version,
  or hashing, re-run the matching golden test and update the pinned constant.

## CLI

The exporter/importer/diff are driven headlessly by the mission-editor CLI
(`platform_export_xlsx`, `platform_import_xlsx`, `platform_diff_xlsx`) and the
matching MCP tools — see the
[CLI README](../../ProjectAegis.MissionEditor.Cli/README.md#platform-excel-round-trip-adr-011).

## Tests

`src/ProjectAegis.Data.Tests/Platform/` mirrors this layout. Run from repo root:

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj -v minimal
```

## See also

- [ADR-011 — platform editor Excel round-trip](../../../docs/architecture/adr-011-platform-editor-excel-roundtrip.md)
- [ADR-006 — data-layer boundary](../../../docs/architecture/adr-006-data-layer-boundary.md)
- [Requirement 21 — Platform Editor](../../../Game-Requirements/requirements/21-Platform-Editor.md)
- [ProjectAegis.Data README — the write gate](../README.md#core-invariant-the-write-gate)
- [ProjectAegis.Data.Excel — ClosedXML adapter](../../ProjectAegis.Data.Excel/README.md)
