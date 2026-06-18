# Platform Editor — Excel round-trip runbook

Developer/operator guide for the **headless platform editor** (requirement 21): export a bound
catalog snapshot to a workbook, edit it offline, then re-import the **diff** through the staged
write gate. The architectural decision and its rejected alternatives live in
[ADR-011](../architecture/adr-011-platform-editor-excel-roundtrip.md); this page covers *how the
pieces fit, how to drive them, and what is and is not wired today.*

| Question | Answer |
|----------|--------|
| What is the editing surface? | A multi-sheet workbook (one sheet per entity domain) that round-trips through the catalog write gate. |
| Where does the code live? | Engine-free core in `src/ProjectAegis.Data/Platform/`; the `.xlsx` adapter in `src/ProjectAegis.Data.Excel/`. |
| How do I drive it? | CLI verbs `platform_export_xlsx` / `platform_diff_xlsx` / `platform_import_xlsx`, plus matching MCP tools. |
| What guarantees safety? | Every edit is a *diff* against the source snapshot, validated, then **proposed** to `IWriteGate` — never auto-committed. |

## Why it exists

The catalog (`ProjectAegis.Data`) already has deterministic governance — a staged write gate,
immutable snapshots, per-field provenance, and an append-only change log — but no friendly
**authoring surface** for full platform configuration. ADR-011 chose Excel as that surface because
designers can bulk-edit with dropdowns and multi-sheet ergonomics, while every change still flows
through the same propose → approve → commit gate. The workbook is a *front door*, not a parallel
write path.

Two boundaries are load-bearing:

- **ADR-006 (data layer is engine-free):** the in-memory workbook model, exporter, diff, validator,
  and hash carry **no spreadsheet dependency**, so they stay pure and unit-testable. Excel
  serialization sits behind the `IPlatformWorkbookIo` port.
- **DBI-8.3 (no bypass):** the importer never invents a commit path. It can only call
  `IWriteGate.Propose*Batch`; committing always requires a separate `ApproveBatch`.

## Architecture at a glance

```
catalog snapshot
      │  PlatformWorkbookExporter.Export(data, snapshotId, clock)
      ▼
PlatformWorkbook  ──Write──►  file        (IPlatformWorkbookIo adapter)
 (in-memory model)             │
      ▲                         edit offline (Excel / text)
      │  Read                   ▼
PlatformWorkbook  ◄────────  edited file
      │
      │  PlatformWorkbookImporter.Plan(edited)
      ▼
PlatformImportPlan  =  Diff(source↔edited) + Validate(edited) + classify supported/unsupported
      │  .Stage(edited, gate, actor)        (only if not Blocked and snapshot resolves)
      ▼
IWriteGate.Propose{Sensor,Mount,Loadout,Magazine,Comms}Batch  ──►  staged batch id(s)
      │  IWriteGate.ApproveBatch(batchId, …)   ← separate, human-gated step
      ▼
committed catalog rows
```

| Type (`ProjectAegis.Data.Platform`) | Role |
|-------------------------------------|------|
| `PlatformWorkbook` / `PlatformWorkbookSheet` | Engine-free model: ordered header + deterministically ordered rows of cell strings. |
| `PlatformWorkbookExporter` | Pure export of catalog rows to a workbook; appends the `_Meta` sheet (snapshot id, schema version, hash). |
| `IPlatformWorkbookIo` | Port for serialization. Contract: `Read(Write(wb)) == wb` for any exporter-produced workbook. |
| `CanonicalTextWorkbookIo` | Dependency-free reference adapter; used by golden tests and as the spec the `.xlsx` adapter must match. |
| `ClosedXmlPlatformWorkbookIo` (`ProjectAegis.Data.Excel`) | Production `.xlsx` adapter (ClosedXML). Writes cells as text (`@` number-format) so numeric-looking values round-trip byte-for-byte. |
| `PlatformWorkbookDiff` | Structural diff between two exporter-ordered workbooks; `_Meta` excluded (its hash/timestamp are derived). |
| `PlatformWorkbookValidator` | Deterministic cross-sheet fitting rules (capacity/referential) the SQLite CHECKs can't express at edit time. |
| `PlatformWorkbookHash` | SHA-256 over data sheets (locale-independent); feeds `_Meta.WorkbookHash` and golden tests. |
| `PlatformWorkbookImporter` | `Plan` (pure) and `Stage` (proposes batches through the gate). |
| `PlatformImportPlan` / `PlatformImportResult` | Plan = diff + findings + supported/unsupported split; result = staged batch ids + notes. |

## Workbook schema

One sheet per domain, plus a read-only `_Meta` sheet. Rows are sorted by canonical keys and all
cells are formatted with `InvariantCulture`, so an unedited round-trip produces an **empty diff**.

| Sheet | Columns | Key (sort) order |
|-------|---------|------------------|
| `Platforms` | `PlatformId, LatDeg, LonDeg, CombatRadiusNm` | `PlatformId` |
| `Sensors` | `PlatformId, SensorId, BasePd, ReviewState, TrlLevel, ValueTier, CitationRef` | `PlatformId, SensorId` |
| `Mounts` | `PlatformId, MountId, MountType, ArcDeg, Capacity, ReviewState` | `PlatformId, MountId` |
| `Loadouts` | `PlatformId, LoadoutId, LoadoutName, Role, IsDefault` | `PlatformId, LoadoutId` |
| `Magazines` | `PlatformId, LoadoutId, MountId, WeaponId, Quantity, ReloadTimeSec, Depth` | `PlatformId, LoadoutId, MountId, WeaponId` |
| `Comms` | `PlatformId, LinkId, Role, SatcomCapable, ReviewState, TrlLevel, ValueTier, CitationRef` | `PlatformId, LinkId` |
| `_Meta` | `Key, Value` (`SourceSnapshotId`, `SchemaVersion`, `ExportUtcTicks`, `WorkbookHash`) | — |

The importer reads `_Meta.SourceSnapshotId` to find the workbook's origin snapshot, re-exports that
snapshot, and diffs against it. **Do not edit, rename, reorder, or delete the `_Meta` sheet** — a
missing/unknown snapshot id means the import cannot be safely diffed and nothing is staged.

## Validation rules

`PlatformWorkbookValidator.Validate` runs over the in-memory workbook and emits sorted
`ValidationFinding`s. Any `Error` finding marks the plan `Blocked` and refuses staging (PLE-4.2).

| Code | Severity | Meaning |
|------|----------|---------|
| `PLE-MOUNT-RANGE` | Error | A mount has `ArcDeg` outside `0..360` or negative `Capacity`. |
| `PLE-MAG-LOADOUT` | Error | A magazine references a `(PlatformId, LoadoutId)` not present in `Loadouts`. |
| `PLE-MAG-MOUNT` | Error | A magazine references a `(PlatformId, MountId)` not present in `Mounts`. |
| `PLE-MAG-CAPACITY` | Error | A magazine `Quantity` exceeds its referenced mount's `Capacity`. |

## Governance: propose → approve → commit

`PlatformWorkbookImporter.Stage` proposes one batch per changed domain via `IWriteGate`. It returns
the staged batch ids but **commits nothing**. Approval is a separate, human-gated step:

- A plan whose change count exceeds `PlatformWorkbookImporter.HumanApprovalRecordThreshold`
  (default **10**, DBI-2.4) sets `RequiresHumanApproval`.
- `IWriteGate.ApproveBatch(batchId, actorType, actorId)` returns a `WriteGateDecision`
  (`Committed`, `BatchId`, `Errors`). Until then the rows stay staged.
- Use `ListPendingBatches()` (or the `platform_import_xlsx` output's `pending_batches`) to see
  what is awaiting approval.

## CLI / MCP reference

Run from the repo root. The MCP tools in `tools/mission-editor/mcp-tools.json` wrap these verbs
1:1 (`platform_export_xlsx`, `platform_import_xlsx`, `platform_diff_xlsx`).

```bash
# Export a snapshot to a workbook file
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  platform_export_xlsx [--db <catalog.db>] --out <path> [--snapshot <id>]

# Diff a base workbook against an edited one (no side effects)
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  platform_diff_xlsx [--db <catalog.db>] [--base <path>] [--edited <path>]

# Import (propose) edits — stages batches via the write gate, never commits
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  platform_import_xlsx --db <catalog.db> [--in <workbook>] \
  [--actor-type cli] [--actor-id mission-editor]

# Approve a staged batch (the commit step)
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  catalog_write_approve --db <catalog.db> --batch <batchId>
```

Each verb prints a `McpToolResult`-style JSON object (`ok`, `verb`, …). `platform_import_xlsx`
includes a `nextStep` pointing at `catalog_write_approve`. `--db` must be an existing catalog file
with **schema 007+** (the mounts/loadouts/magazines/comms staging tables); otherwise the gate fails
to open and the verb returns `ok: false`.

## Current implementation state (read before relying on `.xlsx`)

The **engine-free core is complete and tested** through `CanonicalTextWorkbookIo`: export, diff,
validation, hashing, and importer planning/staging all work and are covered by golden tests. Three
caveats matter operationally:

1. **The `.xlsx` adapter is not wired into the CLI yet.** `ProjectAegis.Data.Excel`
   (`ClosedXmlPlatformWorkbookIo`) exists, but it is **not in `ProjectAegis.sln`** and **not
   referenced by the CLI**. To activate it locally:
   ```bash
   dotnet sln ProjectAegis.sln add src/ProjectAegis.Data.Excel/ProjectAegis.Data.Excel.csproj
   dotnet restore
   ```
   Until then `platform_export_xlsx` writes via `CanonicalTextWorkbookIo` (a `.platform.txt` file),
   and `platform_import_xlsx`/`platform_diff_xlsx` exercise the gate/diff path against exporter data
   rather than loading a real `.xlsx`. The verb output notes this.
2. **Platform-core changes are not stageable yet.** The P0 write gate supports
   `Sensors, Mounts, Loadouts, Magazines, Comms`. Edits to the `Platforms` sheet are reported as
   `UnsupportedChanges` and skipped, pending a gate extension.
3. **Provenance columns are partial on import.** The importer reconstructs only editor-surfaced
   columns; unsurfaced provenance (e.g. `SourceFactId`, `Confidence`, `ImportBatchId`) takes record
   defaults today and would be merged from the source row in a full implementation.

## Common pitfalls

- **Locale drift is the #1 correctness bug.** All formatting/parsing uses `InvariantCulture`. Editing
  a workbook in a tool that reformats numbers (thousands separators, decimal commas) can produce a
  spurious non-empty diff. The `.xlsx` adapter pins columns to text format (`@`) to prevent Excel
  coercing `"57"` into a number.
- **Empty round-trip must diff to zero.** If exporting and re-importing an *unedited* workbook stages
  changes, something reformatted the cells or touched `_Meta`. Treat a non-empty unedited diff as a
  bug, not a no-op.
- **Sheet names are capped at 31 chars** and forbid `: \ / ? * [ ]`; the adapter sanitizes, but keep
  domain sheet names short and clean if you extend the schema.
- **Approval is mandatory.** Seeing batch ids in `platform_import_xlsx` output does **not** mean the
  edits are live. Run `catalog_write_approve` and check `WriteGateDecision.Committed`.

## Related

- [ADR-011 — Platform editor Excel round-trip](../architecture/adr-011-platform-editor-excel-roundtrip.md)
- [ADR-006 — Data layer boundary](../architecture/adr-006-data-layer-boundary.md)
- Requirement [21 — Platform editor](../../Game-Requirements/requirements/21-Platform-Editor.md),
  [06 — Database intelligence](../../Game-Requirements/requirements/06-Database-Intelligence.md)
- Code: `src/ProjectAegis.Data/Platform/`, `src/ProjectAegis.Data.Excel/`,
  `src/ProjectAegis.MissionEditor.Cli/Platform*XlsxCommand.cs`
