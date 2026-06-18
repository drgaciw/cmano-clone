# Platform Editor — Excel round-trip runbook

Developer/operator guide for authoring catalog platform data through the
Excel-style workbook round-trip. The design rationale lives in
[`ADR-011`](../architecture/adr-011-platform-editor-excel-roundtrip.md); this doc
is the **how-to**: the workflow, the CLI/MCP verbs, the workbook layout, and the
current delivery status so you don't expect behavior that isn't wired yet.

The headless services live in `src/ProjectAegis.Data/Platform/` (namespace
`ProjectAegis.Data.Platform`); the CLI verbs are in
`src/ProjectAegis.MissionEditor.Cli/`.

## Intent

Requirement 21 (Platform Editor) wants designers to bulk-edit full platform
configurations in a familiar spreadsheet, then commit those edits through the
**existing** catalog governance — the staged write gate (`IWriteGate`),
provenance, and approval — instead of a parallel write path. The editor is a new
**front door** onto the write gate, not a new system. Per ADR-011:

- **Edit-by-diff, not overwrite** — an unedited round-tripped workbook produces an
  *empty* diff and stages nothing. Only changed rows become a staged batch.
- **No auto-commit** — every workbook-originated change goes through
  `IWriteGate.Propose*Batch` and commits only via `ApproveBatch`.
- **Engine-free** — Excel I/O sits behind the `IPlatformWorkbookIo` port so the
  Data assembly keeps no `UnityEngine` dependency (ADR-006) and stays unit-testable.

## Delivery status (read before using)

> The xlsx file adapter (ClosedXML) is **deferred** (tracked as Sprint 23 S23-01).
> Until it lands the verbs exercise the real exporter/diff/gate paths but use the
> dependency-free `CanonicalTextWorkbookIo` reference format, **not** `.xlsx` files.

| Capability | Status |
|------------|--------|
| Deterministic export → workbook model (`PlatformWorkbookExporter`) | Working |
| Snapshot-bound `_Meta` sheet + content hash (`PlatformWorkbookHash`) | Working |
| Deterministic diff (`PlatformWorkbookDiff.Compare`) | Working |
| Write-gate staging for the edited rows (`Propose*Batch`) | Working (Phase A sheets) |
| Real `.xlsx` read/write (`IPlatformWorkbookIo` ClosedXML adapter) | Deferred (S23-01) |
| Live snapshot read in `platform_export_xlsx` | Scaffold — exports `PlatformCatalogExportData.Empty` today |

**Phase A** sheets (delivered): `Platforms`, `Sensors`, `Mounts`, `Loadouts`,
`Magazines`, `Comms`. **Phase B** (signatures, mobility, EMCON, damage) is
specified in requirement 21 but not yet exported.

## Workbook layout

`PlatformWorkbookExporter.Export(...)` emits one `PlatformWorkbookSheet` per entity
domain plus a trailing `_Meta` sheet. Schema version is pinned at `007`
(migration `assets/data/catalog/migrations/007_platform_editor_phase_a.sql`).

| Sheet | Contents |
|-------|----------|
| `Platforms` | Platform core rows (`CatalogPlatformEntry`). |
| `Sensors` | Sensor suite bindings (`CatalogSensorBinding`). |
| `Mounts` | Weapon/sensor mounts (`CatalogMount`). |
| `Loadouts` | Mount loadout assignments (`CatalogLoadout`). |
| `Magazines` | Magazine capacity rows (`CatalogMagazineEntry`). |
| `Comms` | Comms/datalink bindings (`CatalogCommsBinding`). |
| `_Meta` | Read-only: source `snapshotId`, export timestamp, and the content hash of all data sheets. |

Rows are sorted by canonical keys and every cell is formatted with
`CultureInfo.InvariantCulture`, so the export is byte-stable across machines and
locales. The `_Meta` hash binds the workbook to its source snapshot: re-import
compares against it to detect tampering and to compute the diff.

## Workflow

```
export  →  edit offline  →  diff  →  import (stage)  →  approve  →  commit
```

1. **Export** a snapshot to a workbook.

   ```bash
   dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
     platform_export_xlsx --db <catalog.db> --out <workbook> [--snapshot <id>]
   ```

   Returns JSON `{ ok, verb, snapshotId, outPath, note }`. With no `--snapshot`
   the export id defaults to `cli-s22-export`.

2. **Edit** the data sheets offline. Do **not** touch `_Meta` — it is the diff
   anchor.

3. **Diff** the edited workbook against its baseline to preview the change set.

   ```bash
   dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
     platform_diff_xlsx [--db <catalog.db>] [--base <path>] [--edited <path>]
   ```

   Returns JSON `{ ok, verb, diffCount, note }`. An unedited round-trip yields
   `diffCount = 0` — that empty-diff property is asserted by a golden test.

4. **Import** to stage the changed rows through the write gate. This *proposes*
   a batch; it never commits.

   ```bash
   dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
     platform_import_xlsx --db <catalog.db> [--in <workbook>] \
     [--actor-type <type>] [--actor-id <id>]
   ```

   The DB must already exist with schema `007+`. Returns JSON whose `nextStep`
   points at the approval verb.

5. **Approve** the staged batch to commit it.

   ```bash
   dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
     catalog_write_approve --db <catalog.db> --batch <batchId>
   ```

   A proposed batch only becomes live when it is approved here. Per ADR-011
   governance intent, large bulk imports (over the configured record threshold)
   or any `balanceCritical` field still require explicit human `ApproveBatch`
   (DBI-2.4) — there is no direct-SQL or auto-commit path for the importer.

### MCP

The same three verbs are registered in
[`tools/mission-editor/mcp-tools.json`](../../tools/mission-editor/mcp-tools.json)
(`platform_export_xlsx`, `platform_import_xlsx`, `platform_diff_xlsx`) and route
through `Invoke-MissionEditorMcp.ps1`. `dbRef` defaults to `baltic_patrol`;
`platform_export_xlsx` requires `outPath`; `platform_import_xlsx` requires `dbRef`.

## Write-gate surface

The importer stages edits via `IWriteGate.Propose*Batch` (see
`src/ProjectAegis.Data/WriteGate/IWriteGate.cs`). Phase A exposes one propose
method per sheet domain:

| Sheet | Propose method | Record type |
|-------|----------------|-------------|
| `Sensors` | `ProposeSensorBatch` | `CatalogSensorBinding` |
| `Mounts` | `ProposeMountBatch` | `CatalogMount` |
| `Loadouts` | `ProposeLoadoutBatch` | `CatalogLoadout` |
| `Magazines` | `ProposeMagazineBatch` | `CatalogMagazineEntry` |
| `Comms` | `ProposeCommsBatch` | `CatalogCommsBinding` |
| `Platforms` | `ProposePlatformBatch` | `CatalogPlatformBinding` |

Each `Propose*Batch` returns a `batchId`; `ApproveBatch` / `RejectBatch` resolve
it. `ListPendingBatches()` returns the open `CatalogStagingBatchSummary` set.

## Constraints / pitfalls

- **Not real `.xlsx` yet.** The verbs write/read the canonical text reference
  format until the ClosedXML adapter ships (S23-01). Wire production behavior to
  `IPlatformWorkbookIo`, never to a concrete format.
- **`platform_export_xlsx` exports an empty baseline today.** It serializes
  `PlatformCatalogExportData.Empty`, not a live snapshot read — treat the current
  verb as a path/contract exerciser, not a data dump.
- **No auto-commit, ever.** `platform_import_xlsx` only stages. Nothing reaches
  live tables without `catalog_write_approve`. A write-gate parity test enforces
  this; keep it that way.
- **Locale is the likely correctness bug.** Round-trip determinism depends on
  strict `InvariantCulture` parsing/formatting and stable sort keys. Don't
  introduce locale-sensitive number/date formatting into the exporter/importer.
- **Don't edit `_Meta`.** It carries the snapshot binding and content hash the
  importer diffs against; editing it breaks tamper detection and the diff.
- **Empty diff must stage nothing.** If an unedited round-trip ever produces a
  non-zero diff, the exporter/IO is non-deterministic — fix that before shipping.

## Tests

- `src/ProjectAegis.Data.Tests/Platform/` — `PlatformWorkbookRoundTripTests`
  (export → import determinism / empty-diff), `PlatformWorkbookImporterTests`
  (write-gate parity), and `PlatformWorkbookValidatorTests`.
- `src/ProjectAegis.MissionEditor.Cli.Tests/McpToolsManifestTests.cs` — asserts the
  three verbs are registered in the MCP manifest.
