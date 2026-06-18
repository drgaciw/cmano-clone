# Platform Editor Workbook (export → edit → diff → stage)

`ProjectAegis.Data.Platform` — the **spreadsheet round-trip** for catalog platform
data. It exports the bound catalog snapshot to an engine-free workbook model,
re-imports an edited workbook, **diffs** it against a faithful re-export of the
source, validates fitting rules, and stages the supported changes through the
[Catalog Write Gate](../WriteGate/README.md). **Nothing here writes the live catalog
directly** — editing a spreadsheet still ends in propose → approve.

**Requirement trace:** Req-21 (platform editor), ADR-011 (Excel round-trip,
Phase A); PLE-2.* / PLE-3.* / PLE-4.* acceptance criteria; DBI-2.4 (large-batch
human approval), DBI-8.3 (no commit path that bypasses the gate). Story trace:
S22-01 (write-gate extension), S22-02 (CLI verbs), S22-04 (staging). **Posture:**
*pure analysis, then propose* — the importer never invents a commit path.

## Intent

Reviewers want to edit reference data in Excel, not SQL. To keep that safe and
reproducible the subsystem is split into pure, deterministic stages:

1. **Export** catalog rows to an in-memory `PlatformWorkbook` — one sheet per entity
   domain, rows sorted by canonical keys, all cells formatted with
   `InvariantCulture`. A trailing `_Meta` sheet binds the workbook to its source
   snapshot and carries a content hash.
2. **Serialize** via an `IPlatformWorkbookIo` adapter. The shipped
   `CanonicalTextWorkbookIo` is dependency-free (used by golden tests + headless CLI);
   the production `.xlsx` (ClosedXML) adapter is **deferred per ADR-011** but must
   satisfy the same contract: `Read(Write(wb)) == wb`.
3. **Diff** the edited workbook against a fresh re-export of its bound snapshot, so an
   *unedited* round-trip yields zero changes (PLE-2.1).
4. **Validate** cross-sheet fitting rules the SQLite `CHECK` constraints can't express.
5. **Stage** supported changes as write-gate batches — still requiring a separate
   `ApproveBatch` to reach the catalog.

## Architecture

```
bound snapshot ─► PlatformWorkbookExporter.Export ─► PlatformWorkbook (+ _Meta hash)
                                                          │  IPlatformWorkbookIo.Write
                                                          ▼
                                                    .platform.txt / .xlsx  ──► reviewer edits
                                                          │  IPlatformWorkbookIo.Read
                                                          ▼
   PlatformWorkbookImporter.Plan:  re-export source ─► PlatformWorkbookDiff.Compare
                                   PlatformWorkbookValidator.Validate
                                          │  split by sheet
                                          ├─ supported (Sensors/Mounts/Loadouts/Magazines/Comms)
                                          └─ unsupported (Platforms)  → reported, not staged
                                          ▼
   PlatformWorkbookImporter.Stage ─► IWriteGate.Propose*Batch ─► (await catalog_write_approve)
```

| Type | Role |
|------|------|
| `PlatformWorkbook` / `PlatformWorkbookSheet` | Engine-free in-memory model: ordered sheets, each an ordered header + deterministically ordered rows of cell strings. No spreadsheet library. |
| `IPlatformWorkbookIo` | Serialization port. Contract: `Read(Write(wb)) == wb`. |
| `CanonicalTextWorkbookIo` | Dependency-free reference adapter (ASCII unit-separator text); the executable spec the `.xlsx` adapter must match. |
| `PlatformCatalogExportData` | The catalog rows an export turns into a workbook (platforms, sensors, mounts, loadouts, magazines, comms). Decoupled from `ICatalogReader` so the exporter stays a pure function. |
| `PlatformWorkbookExporter` | Pure export to a workbook; `SchemaVersion = "007"`; time injected via `ICatalogClock`. |
| `PlatformWorkbookHash` | Content hash over data sheets (excludes `_Meta`) for tamper/round-trip detection. |
| `PlatformWorkbookDiff` / `PlatformWorkbookChange` | Structural diff (`SheetAdded/Removed`, `HeaderChanged`, `RowAdded/Removed`, `CellChanged`); `_Meta` excluded since its hash/time are derived. |
| `PlatformWorkbookValidator` | Deterministic fitting rules (`PLE-MAG-MOUNT`, `PLE-MAG-LOADOUT`, `PLE-MAG-CAPACITY`, `PLE-MOUNT-RANGE`); findings sorted for golden-hash stability. |
| `PlatformWorkbookImporter` | Orchestrator: `Plan` (pure) → `Stage` (proposes batches). `HumanApprovalRecordThreshold = 10` (DBI-2.4). |
| `PlatformImportPlan` / `PlatformImportResult` | The analysis result (`Blocked`, `SupportedChanges`, `UnsupportedChanges`, `RequiresHumanApproval`) and the staging outcome (per-entity batch ids + notes). |

## Usage

```csharp
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.WriteGate;

var clock = new FixedCatalogClock(0);

// 1. Export the bound snapshot to a workbook and write it for editing.
var workbook = new PlatformWorkbookExporter().Export(exportData, snapshotId: "rel-2026-06", clock);
new CanonicalTextWorkbookIo().Write(workbook, "platform.platform.txt");

// 2. After editing, read the workbook back and analyse it (pure — no writes).
PlatformWorkbook edited = new CanonicalTextWorkbookIo().Read("platform.edited.txt");
var importer = new PlatformWorkbookImporter(
    snapshotProvider: id => LookupSnapshot(id),   // string -> PlatformCatalogExportData?
    clock);
PlatformImportPlan plan = importer.Plan(edited);
if (plan.Blocked) { /* resolve validation errors first */ }

// 3. Stage supported changes through the write gate (still needs ApproveBatch).
using var gate = new CatalogWriteGate(databasePath, clock);
PlatformImportResult result = importer.Stage(edited, gate, actorType: "human", actorId: "reviewer");
foreach (var note in result.Notes) Console.WriteLine(note);
// e.g. "Proposed 3 sensor row(s) as batch 'batch-3-0'." then run catalog_write_approve.
```

## CLI / operational runbook

The headless mission-editor exposes the round-trip as three MCP/CLI verbs
(`platform_export_xlsx`, `platform_diff_xlsx`, `platform_import_xlsx`). Full flag and
JSON-payload reference lives in [`tools/mission-editor/README.md`](../../../tools/mission-editor/README.md):

```bash
# Export → edit → diff → stage (no auto-commit; approve is a separate step).
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  platform_export_xlsx --out platform.platform.txt --snapshot rel-2026-06
#   …reviewer edits platform.platform.txt → platform.edited.txt…
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  platform_diff_xlsx --base platform.platform.txt --edited platform.edited.txt   # diffCount: 0 if unedited
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  platform_import_xlsx --db catalog.db --in platform.edited.txt                   # → { staged, nextStep: catalog_write_approve }
```

## Constraints & gotchas

- **Sensors are the only entity the gate commits today.** `Stage` proposes Sensors,
  Mounts, Loadouts, Magazines, and Comms batches, but per the
  [Write Gate](../WriteGate/README.md), only sensor batches have a live-table commit
  path on approve — non-sensor batches stage but return `staging_batch_not_found` on
  approve. `Platforms` changes aren't stageable at all and are surfaced in
  `UnsupportedChanges`.
- **Unresolved / stale snapshot ⇒ nothing staged.** If the `_Meta` `SourceSnapshotId`
  can't be resolved by the injected provider, `Plan` returns `SnapshotResolved = false`
  and `Stage` refuses (PLE-2.2) — it never diffs against a guessed baseline.
- **Validation errors block staging.** Any `ValidationSeverity.Error` finding sets
  `Plan.Blocked`; `Stage` refuses until the workbook is fixed (PLE-4.2). Warnings
  don't block.
- **Large batches require human approval.** More than `HumanApprovalRecordThreshold`
  (10) changes sets `RequiresHumanApproval` (DBI-2.4).
- **Round-trip must be exact.** An unedited `Read(Write(wb))` must diff to empty; the
  `_Meta` sheet is excluded from both the diff and the content hash because its hash
  and timestamp are derived, not authored. Keep adapters byte-faithful.
- **Determinism is required.** Exporter ordering, `InvariantCulture` formatting,
  injected `ICatalogClock`, and sorted findings exist so workbooks and golden hashes
  reproduce — don't introduce wall-clock or culture-sensitive formatting.
- **Importer reconstructs only editor-surfaced columns.** Unsurfaced provenance
  (e.g. `SourceFactId`, `Confidence`, `ImportBatchId`) takes record defaults today;
  a full implementation would merge it from the source row.

## Tests

| Area | Test |
|------|------|
| Round-trip + golden hash (`Read(Write(wb)) == wb`) | `ProjectAegis.Data.Tests/Platform/PlatformWorkbookRoundTripTests` |
| Diff, plan, supported/unsupported split, staging | `PlatformWorkbookImporterTests` |
| Fitting/validation rules | `PlatformWorkbookValidatorTests` |
| CLI export/import/diff round-trip | `ProjectAegis.MissionEditor.Cli.Tests` (platform verbs) |

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "FullyQualifiedName~Platform" -v minimal
```
