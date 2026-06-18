# Platform Editor — Excel round-trip subsystem

> **Engineering reference + runbook.** Decision rationale lives in [ADR-011](../architecture/adr-011-platform-editor-excel-roundtrip.md); the editable schema is specified in [Req 21 — Platform Editor](../../Game-Requirements/requirements/21-Platform-Editor.md). This page documents how the code actually behaves today and how to drive it.

The Platform Editor lets designers author full platform configuration (sensors, mounts, loadouts, magazines, comms — Phase A) in a **multi-sheet workbook**, edit it offline, and re-import the changes. Every edit is **diffed against its source snapshot** and staged through the catalog **write gate** — never blind-overwritten and never auto-committed.

## Intent

| Goal | How it is met |
|------|---------------|
| Familiar bulk authoring | One worksheet per entity domain; designers edit in Excel |
| No silent overwrites | Import diffs the edited workbook against the bound snapshot and stages **only the changes** |
| Auditable + reversible | Changes go through `IWriteGate.Propose*Batch`; commit requires a separate `ApproveBatch` |
| Deterministic + CI-testable | The model and exporter carry no spreadsheet library; `InvariantCulture` everywhere; round-trip is a golden test |
| Engine-free (ADR-006) | Excel I/O is isolated behind the `IPlatformWorkbookIo` port; `ProjectAegis.Data` never references a spreadsheet lib |

## Architecture at a glance

```
catalog snapshot ──► PlatformWorkbookExporter ──► PlatformWorkbook (in-memory model)
                                                        │
                                       IPlatformWorkbookIo.Write
                                                        ▼
                              CanonicalTextWorkbookIo (.platform.txt)   ← headless / CI default
                              ClosedXmlPlatformWorkbookIo (.xlsx)       ← production adapter (S23-01)
                                                        │  (designer edits offline)
                                       IPlatformWorkbookIo.Read
                                                        ▼
edited PlatformWorkbook ──► PlatformWorkbookImporter.Plan
                              ├─ re-export bound snapshot (via snapshotProvider)
                              ├─ PlatformWorkbookDiff.Compare(source, edited)
                              ├─ PlatformWorkbookValidator.Validate(edited)
                              └─ classify changes: supported (sensors/mounts/loadouts/magazines/comms) vs unsupported (platforms)
                                                        │
                              PlatformWorkbookImporter.Stage ──► IWriteGate.Propose*Batch  (no commit)
                                                        │
                                       catalog_write_approve ──► IWriteGate.ApproveBatch   (human commit)
```

### Key types (`src/ProjectAegis.Data/Platform/`)

| Type | Responsibility |
|------|----------------|
| `PlatformWorkbook` / `PlatformWorkbookSheet` | Engine-free model: ordered sheets, each an ordered header + deterministically ordered rows of cell **strings** |
| `IPlatformWorkbookIo` | Port: `Write(workbook, path)` / `Read(path)`. Contract: `Read(Write(wb)) == wb` |
| `CanonicalTextWorkbookIo` | Dependency-free reference adapter (ASCII control-char delimited text). Runs in CI without ClosedXML and is the executable spec the `.xlsx` adapter must satisfy |
| `ClosedXmlPlatformWorkbookIo` | Production `.xlsx` adapter (in `ProjectAegis.Data.Excel`, backed by ClosedXML/MIT) |
| `PlatformWorkbookExporter` | Pure export of `PlatformCatalogExportData` → workbook; appends the `_Meta` sheet |
| `PlatformWorkbookDiff` | Structural diff between two exporter-ordered workbooks (`_Meta` excluded) |
| `PlatformWorkbookValidator` | Deterministic cross-sheet fitting rules (capacity, referential integrity) |
| `PlatformWorkbookHash` | SHA-256 over data sheets (excludes `_Meta`); locale-independent |
| `PlatformWorkbookImporter` | `Plan` (pure analysis) and `Stage` (propose through the gate) |
| `PlatformImportPlan` / `PlatformImportResult` | Plan = diff + findings + supported/unsupported split; Result = staged batch IDs + notes |

## Workbook layout

One data sheet per domain, plus a trailing read-only `_Meta` sheet. Rows are sorted by canonical keys; numbers are written with `InvariantCulture` (`"R"` for doubles).

| Sheet | Header columns |
|-------|----------------|
| `Platforms` | `PlatformId, LatDeg, LonDeg, CombatRadiusNm` |
| `Sensors` | `PlatformId, SensorId, BasePd, ReviewState, TrlLevel, ValueTier, CitationRef` |
| `Mounts` | `PlatformId, MountId, MountType, ArcDeg, Capacity, ReviewState` |
| `Loadouts` | `PlatformId, LoadoutId, LoadoutName, Role, IsDefault` |
| `Magazines` | `PlatformId, LoadoutId, MountId, WeaponId, Quantity, ReloadTimeSec, Depth` |
| `Comms` | `PlatformId, LinkId, Role, SatcomCapable, ReviewState, TrlLevel, ValueTier, CitationRef` |
| `_Meta` | `Key, Value` — `SourceSnapshotId`, `SchemaVersion` (`007`), `ExportUtcTicks`, `WorkbookHash` |

The `_Meta` sheet binds the workbook to the snapshot it was exported from. The importer reads `SourceSnapshotId` to know what to diff against; if it cannot resolve that snapshot, **nothing is staged** (`SnapshotResolved == false`). `WorkbookHash` is a SHA-256 over the data sheets (not `_Meta` itself), so tampering with content is detectable.

## The round-trip contract

`IPlatformWorkbookIo` implementations MUST satisfy `Read(Write(wb)) == wb` for any exporter-produced workbook, and `PlatformWorkbookDiff.Compare(source, faithfulRoundTrip)` MUST be empty. This is what makes "export → import unedited → zero staged changes" hold (PLE-2.1).

Two consequences for the `.xlsx` adapter:

- **Cells are written as text.** `ClosedXmlPlatformWorkbookIo` pins each column's number format to `"@"` (text) so a numeric-looking value such as `"57"` round-trips byte-for-byte instead of being coerced to a number.
- **`_Meta` is excluded from the diff.** Its hash/timestamp are derived, not authored, so they never count as edits.

## CLI / MCP verbs

Three verbs on `ProjectAegis.MissionEditor.Cli` (also exposed as MCP tools in `tools/mission-editor/mcp-tools.json`; see the full verb surface in the [Mission Editor CLI / MCP reference](mission-editor-cli-reference.md)):

```bash
# Export the bound snapshot to a workbook
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  platform_export_xlsx [--db <catalog.db>] --out <path> [--snapshot <id>]

# Stage edits from a workbook through the write gate (requires an existing db with schema 007+)
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  platform_import_xlsx --db <catalog.db> [--in <workbook>] [--actor-type cli] [--actor-id user]

# Diff a base workbook against an edited one
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  platform_diff_xlsx [--db <catalog.db>] [--base <path>] [--edited <path>]
```

All three return `McpToolResult`-style JSON (`{ ok, verb, ... }`) and never auto-commit. After a successful import that stages a batch, the reported `nextStep` is:

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  catalog_write_approve --db <catalog.db> --batch <batchId>
```

### Current behavior vs target (important)

The CLI verbs were scaffolded in Sprint 22 (S22-02) and the binary `.xlsx` wiring is finished in Sprint 23 (S23-01). Read the verb's `note` field — it states which adapter ran.

| Verb | Today (headless default) | After S23-01 |
|------|--------------------------|--------------|
| `platform_export_xlsx` | Writes via `CanonicalTextWorkbookIo` (text, e.g. `platform-export.platform.txt`); export data is `PlatformCatalogExportData.Empty` | Writes a real `.xlsx` via `ClosedXmlPlatformWorkbookIo` |
| `platform_import_xlsx` | Opens the gate and reports `pending_batches`; full `xlsx → PlatformWorkbook` load is deferred (`Io.Read`) | Loads the workbook and stages the diff |
| `platform_diff_xlsx` | Diffs exporter output against itself (empty baseline ⇒ `diffCount = 0`) | Diffs real base/edited files via `Io.Read` |

> The headless default is deliberate: `ProjectAegis.Data` stays engine-free and CI-runnable without the ClosedXML NuGet package. `ProjectAegis.Data.Excel` carries that dependency — run `dotnet restore` after the project is added to the solution.

## Governance: what gets staged

`PlatformWorkbookImporter` only ever proposes — it has no path to live tables that bypasses `IWriteGate`.

- **Supported sheets** (staged via `Propose{Sensor,Mount,Loadout,Magazine,Comms}Batch`): `Sensors`, `Mounts`, `Loadouts`, `Magazines`, `Comms`.
- **Unsupported sheets** (reported, not staged): `Platforms` — the P0 write gate excludes platform-core changes pending a gate extension.
- **Staging is refused** when the source snapshot does not resolve, or when validation produces any `Error` finding (`PlatformImportPlan.Blocked`).
- **Human approval threshold:** a plan touching more than `HumanApprovalRecordThreshold` (10) rows sets `RequiresHumanApproval` (DBI-2.4). Commit always requires an explicit `ApproveBatch` regardless.

### Validation rules (`PlatformWorkbookValidator`)

Cross-sheet rules SQLite `CHECK` constraints cannot express at edit time. Findings are sorted by `(Code, Message)` for golden-hash stability.

| Code | Severity | Meaning |
|------|----------|---------|
| `PLE-MOUNT-RANGE` | Error | Mount `ArcDeg` outside `[0, 360]` or negative `Capacity` |
| `PLE-MAG-LOADOUT` | Error | Magazine references a `(PlatformId, LoadoutId)` not in `Loadouts` |
| `PLE-MAG-MOUNT` | Error | Magazine references a `(PlatformId, MountId)` not in `Mounts` |
| `PLE-MAG-CAPACITY` | Error | Magazine `Quantity` exceeds the referenced mount's `Capacity` |

## Determinism constraints (read before editing this code)

This subsystem feeds golden tests; non-determinism breaks them.

- **Always `InvariantCulture`.** Locale/number drift is the most likely correctness bug (ADR-011). Parse with `NumberStyles` + `CultureInfo.InvariantCulture`; format doubles with `"R"`.
- **No wall clock.** Time enters through `ICatalogClock` (e.g. `FixedCatalogClock(0)` in CLI/tests), recorded as `_Meta.ExportUtcTicks`.
- **Stable sort keys.** Exporter rows are `OrderBy`/`ThenBy` on canonical IDs with `StringComparer.Ordinal`. Keep new sheets ordered the same way or the empty-diff golden fails.
- **Hash excludes `_Meta`.** Anything you add to `_Meta` must not enter `PlatformWorkbookHash`.

## Common pitfalls

- **Numeric coercion on `.xlsx`.** If `"57"` comes back as `57`/`57.0`, the column number-format was not pinned to `"@"`. The diff will then show spurious cell changes.
- **"nothing staged" with no error.** The workbook's `_Meta.SourceSnapshotId` did not resolve via the `snapshotProvider`. Confirm the snapshot exists and the `_Meta` sheet survived the round-trip.
- **Import refuses on a clean-looking sheet.** A validation `Error` blocks the whole plan. Inspect `Plan.Findings`; capacity/referential errors are the usual cause.
- **Platform edits don't apply.** `Platforms` is intentionally unsupported in P0; the change is reported under `UnsupportedChanges`, not staged.
- **Excel sheet-name limits.** Names are capped at 31 chars and forbid `: \ / ? * [ ]`; `ClosedXmlPlatformWorkbookIo.SafeSheetName` guards an extended schema.

## Where things live

| Path | Content |
|------|---------|
| `src/ProjectAegis.Data/Platform/` | Engine-free model, exporter, diff, validator, hash, importer, canonical text adapter |
| `src/ProjectAegis.Data.Excel/ClosedXmlPlatformWorkbookIo.cs` | Production `.xlsx` adapter (ClosedXML) |
| `src/ProjectAegis.MissionEditor.Cli/Platform{Export,Import,Diff}XlsxCommand.cs` | CLI verb handlers |
| `tools/mission-editor/mcp-tools.json` | MCP tool manifest for the three verbs |
| `docs/architecture/adr-011-platform-editor-excel-roundtrip.md` | Decision record |
| `Game-Requirements/requirements/21-Platform-Editor.md` | Full editable schema + open questions |

## Verify

```bash
# Engine-free core (no ClosedXML needed)
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "Platform|WriteGate" -v minimal

# Production .xlsx adapter (restore ClosedXML first)
dotnet restore src/ProjectAegis.Data.Excel/ProjectAegis.Data.Excel.csproj
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "ClosedXml" -v minimal

# CLI surface
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "Mcp|Platform" -v minimal
```
