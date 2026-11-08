# Platform-editor workbook round-trip & governance

The `ProjectAegis.Data/Platform/` pipeline is the **engine-free core** of the
platform editor (Req-21 / ADR-011): it turns a bound catalog snapshot into a
deterministic in-memory workbook, diffs an edited copy back against it, validates
cross-sheet fitting rules the database `CHECK` constraints cannot express, and
stages the resulting row changes through the extend-only `CatalogWriteGate` —
with TRL / orphan / human-approval **governance** gates in between. Nothing here
references a spreadsheet library or `UnityEngine`, and every step is a pure
function of its input.

> **Scope.** This page covers the *core* pipeline (`Platform/` in
> `ProjectAegis.Data`). The binary `.xlsx` serializer that satisfies the same
> round-trip contract lives in a separate edge assembly —
> [`ProjectAegis.Data.Excel`](../../src/ProjectAegis.Data.Excel/README.md); the
> `platform_export_xlsx` / `platform_import_xlsx` / `platform_diff_xlsx` **CLI
> verbs** are in [mission-editor-cli.md](mission-editor-cli.md); the write-gate
> propose→approve→commit machinery these stage into is in
> [catalog-write-gate.md](catalog-write-gate.md); the immutable release/snapshot
> layer above commit is in [catalog-release-train.md](catalog-release-train.md).
> Requirement source: `Game-Requirements/requirements/21-Platform-Editor.md`
> (Req-21) and [ADR-011](../architecture/adr-011-platform-editor-excel-roundtrip.md).

---

## The round-trip, end to end

```
DB snapshot ──Export──► PlatformWorkbook ──IPlatformWorkbookIo──► .xlsx / .txt
                                                                       │
                                                                    (human edits)
                                                                       ▼
   PlatformCatalogExportData ◄─re-export source─┐            edited PlatformWorkbook
              │                                 │                     │
              ▼                                 │                     ▼
   PlatformWorkbookExporter ──► sourceWorkbook ─┴──► PlatformWorkbookDiff.Compare
                                                                     │  (changes)
                                                                     ▼
                                                 PlatformWorkbookValidator.Validate
                                                                     │  (findings)
                                                                     ▼
                                       PlatformImportPlan (supported vs unsupported,
                                        Blocked?, RequiresHumanApproval?, quarantine)
                                                                     │  Stage
                                                                     ▼
                          CatalogImportGate (TRL) + orphan-FK filter ─► quarantine
                                                                     │
                                                                     ▼
                              IWriteGate.Propose*Batch  (one batch per entity kind)
                                                                     │  Approve
                                                                     ▼
                        CatalogWriteGate commit → CatalogSnapshotBinder release (PLE-3.5)
```

The load-bearing invariant is **empty-diff on unedited round-trip**: exporting a
snapshot, serializing, reading it straight back, and diffing against a fresh
re-export of the same snapshot yields *zero* changes, so an untouched workbook
stages nothing (`PlatformWorkbookDiff.IsEmpty`, PLE-2.1).

---

## `PlatformWorkbookExporter` — snapshot → workbook

`public sealed class PlatformWorkbookExporter`, `SchemaVersion = "010"`. `Export`
takes a `PlatformCatalogExportData` (a plain record of catalog row lists,
deliberately decoupled from `ICatalogReader` so the exporter stays a pure
function — see `PlatformCatalogExportData`), a `snapshotId`, and an
`ICatalogClock` (time is **injected**, never read from the wall clock).

It builds one `PlatformWorkbookSheet` per domain, always in this order:

| # | Sheet | Key columns (sort order) |
|---|-------|--------------------------|
| 1 | `Platforms` | `PlatformId` — plus damage columns `MaxHp` / `WithdrawThresholdPct` / `CriticalFlags` folded in from `Damage` |
| 2 | `Sensors` | `PlatformId, SensorId` — carries `ReviewState`, `TrlLevel`, `ValueTier`, `CitationRef` |
| 3 | `Mounts` | `PlatformId, MountId` |
| 4 | `Loadouts` | `PlatformId, LoadoutId` |
| 5 | `Magazines` | `PlatformId, LoadoutId, MountId, WeaponId` |
| 6 | `Comms` | `PlatformId, LinkId` — carries `ReviewState`, `TrlLevel`, `ValueTier`, `CitationRef` |
| 7 | `LinkCatalog` | `LinkId` |
| 8 | `Mobility` | `PlatformId` |
| 9 | `Signatures` | `PlatformId` |
| 10 | `Emcon` | `PlatformId, Condition, EmitterId` |
| — | `_Meta` | `Key`/`Value` binding rows (see below) |

Rows are sorted by their canonical keys with `StringComparer.Ordinal`; every
numeric cell is formatted with `CultureInfo.InvariantCulture` (`"R"` round-trip
format for doubles) so output is locale-independent. Missing platform damage
defaults to `MaxHp = 100`, `WithdrawThresholdPct = 0`, `CriticalFlags = 0`.

### The `_Meta` sheet & content hash

After the data sheets are built, the exporter computes a content hash **over the
data sheets only** and appends a trailing `_Meta` sheet that binds the workbook
to its source and lets re-import detect tampering:

| `_Meta` key | Value |
|-------------|-------|
| `SourceSnapshotId` | the snapshot this workbook was exported from (the importer reads it back to resolve the source) |
| `SchemaVersion` | exporter schema (`"010"`) |
| `ExportUtcTicks` | `clock.UtcTicks` (injected, not wall-clock) |
| `WorkbookHash` | `PlatformWorkbookHash.Compute(dataSheets)` |
| `DbVersion` / `TlTier` / `CatalogSchemaVersion` / `ContentHash` / `ExportSchemaVersion` | from the `CatalogExportManifest` |

`PlatformWorkbookHash.Compute` is a `SHA-256` (lower-case hex) over
`sheetName · header · rows`, joined with the ASCII **unit/record/group
separators** (`0x1F`/`0x1E`/`0x1D`) that never appear in catalog IDs. The `_Meta`
sheet is **excluded** from the hash — it carries the hash, so it cannot be part
of it.

---

## `PlatformWorkbookDiff` — structural change list

`PlatformWorkbookDiff.Compare(source, edited)` returns an ordered
`IReadOnlyList<PlatformWorkbookChange>`, each `(Sheet, Kind, RowIndex, Detail)`
with `Kind ∈ { SheetAdded, SheetRemoved, HeaderChanged, RowAdded, RowRemoved,
CellChanged }`. Two rules keep it deterministic and meaningful:

- The `_Meta` sheet is **never diffed** (its hash/ticks are derived, not
  authored).
- A `HeaderChanged` short-circuits cell comparison for that sheet — comparing
  cells across mismatched columns is meaningless, so it reports the header delta
  and moves on.

`IsEmpty(source, edited)` is the PLE-2.1 predicate the importer relies on.

---

## `PlatformWorkbookValidator` — fitting rules & error codes

`PlatformWorkbookValidator.Validate(workbook)` runs the cross-sheet referential
and capacity checks (PLE-4.*) that SQLite `CHECK` constraints cannot express at
edit time. It is **pure** and returns `ValidationFinding`s sorted by
`(Code, Message)` for golden-hash stability. The stable machine-readable codes:

| Code (const) | Severity | Raised when |
|--------------|----------|-------------|
| `PLE-MAG-MOUNT` | Error | magazine references a mount not present on the platform |
| `PLE-MAG-LOADOUT` | Error | magazine references a loadout not present on the platform |
| `PLE-MAG-CAPACITY` | Error | **cumulative** rounds in a `(platform, loadout, mount)` group exceed mount capacity |
| `PLE-MAG-QTY-NEGATIVE` | Error | magazine `Quantity < 0` |
| `PLE-MOUNT-RANGE` | Error | mount `ArcDeg ∉ [0,360]` or `Capacity < 0` |
| `PLE-PLT-HEADER` / `PLE-MOB-HEADER` / `PLE-SIG-HEADER` / `PLE-EMC-HEADER` | Error | sheet missing, or header diverges from the Phase-B export contract |
| `PLE-PHB-ORPHAN` | Error | a Mobility/Signatures/Emcon row references a `PlatformId` with no `Platforms` row |
| `PLE-EMCON-CONDITION` / `PLE-EMCON-POSTURE` | Error | EMCON `Condition ∉ {silent,restricted,free}` / `Posture ∉ {off,standby,active}` |
| `PLE-MOB-SPEED` / `PLE-MOB-RANGE` | Error | negative `MaxSpeedKnots` / `RangeNm` |
| `PLE-DMG-HP` | Error | `MaxHp ≤ 0` |
| `PLE-DMG-HP-CEIL` | Error | `MaxHp > MaxHpCeiling` (`100_000`, the DBI-2.2 gameplay ceiling) |
| `PLE-DMG-WITHDRAW` | Error | `WithdrawThresholdPct ∉ [0, MaxHp]` |
| `PLE-DMG-FLAGS` | Error | `CriticalFlags < 0` |

> **Why capacity is cumulative.** A single mount can carry several weapon types
> under one loadout (a mixed VLS cell loaded with both ESSM and Tomahawk rounds),
> so capacity is checked against the **sum** of quantities sharing a mount+loadout,
> not each magazine row in isolation. A negative `Quantity` is both flagged
> (`PLE-MAG-QTY-NEGATIVE`) *and* clamped to `0` in the sum, so it can never
> silently discount a genuinely over-capacity mount.

Any `Error`-severity finding sets `PlatformImportPlan.Blocked` — staging is
refused until the workbook is clean.

---

## `PlatformWorkbookImporter` — plan & stage

`public sealed class PlatformWorkbookImporter(Func<string,
PlatformCatalogExportData?> snapshotProvider, ICatalogClock clock, …)`. The
`snapshotProvider` maps the `_Meta.SourceSnapshotId` back to the source rows so
the importer can re-export the *original* workbook and diff against it.

### `Plan(edited)` — pure analysis

1. Read `SourceSnapshotId`; if it does not resolve, return a plan with
   `SnapshotResolved = false` (nothing stageable).
2. Re-export the source snapshot and `Compare` against `edited`.
3. `Validate(edited)` → findings (`Blocked` if any error).
4. Partition each change into **supported** vs **unsupported**:
   - Supported sheets: `Sensors, Mounts, Loadouts, Magazines, Comms, LinkCatalog,
     Mobility, Signatures, Emcon`.
   - On `Platforms`, only **cell changes** to the damage columns (`MaxHp`,
     `WithdrawThresholdPct`, `CriticalFlags`) are supported; core geo/range edits
     (`LatDeg`/`LonDeg`/`CombatRadiusNm`) and row-adds are **unsupported — pending
     gate extension**.
5. `RequiresHumanApproval = changes.Count > HumanApprovalRecordThreshold` (`10`,
   DBI-2.4).
6. Emit plan-time quarantine entries for unresolved FK findings
   (`PLE-PHB-ORPHAN`, `PLE-MAG-MOUNT`, `PLE-MAG-LOADOUT`).

`PlatformImportPlan` is side-effect-free: `Changes`, `Findings`,
`SupportedChanges`, `UnsupportedChanges`, `Blocked`, `RequiresHumanApproval`,
`QuarantineEntries`.

### `Stage(edited, gate, actorType, actorId, rationale)` — propose batches

Refuses to stage if the snapshot did not resolve **or** the plan is `Blocked`.
Otherwise it maps each supported sheet's changed rows to catalog DTOs and
proposes one **batch per entity kind** through `IWriteGate.Propose*Batch`
(`sensor`, `mount`, `loadout`, `magazine`, `comms`, `link`, `mobility`,
`signature`, `emcon`, `damage`). Two governance filters run **before** propose:

- **TRL / review gate (PLE-4.4).** Sensor rows go through
  `CatalogImportGate.PartitionForImport` — anything below the minimum TRL (`4`)
  or not `Approved` is **quarantined**, not proposed. New sensor rows default to
  `ReviewState = Provisional`, `TrlLevel = 9`, and a `ValueTier` normalized via
  `CatalogProvenanceTier`.
- **Orphan-platform FK filter.** Mobility / Signatures / Emcon / damage rows for
  a `PlatformId` not in the source snapshot are rejected and quarantined
  (`PLE-PHB-ORPHAN`).

Quarantine entries (`PlatformImportQuarantineEntry`: `EntityKind, PlatformId,
EntityId, Reason, SourceSheet, Detail`) are sorted deterministically by
`(EntityKind, PlatformId, EntityId, Reason)` (PLE-2.3) and **never** proposed to
the gate. `PlatformImportResult` returns the per-kind batch ids, human-readable
`Notes`, and the merged quarantine list.

---

## `PlatformWorkbookWriteService` — headless export→propose→approve

`public sealed class PlatformWorkbookWriteService` is the orchestration facade
(ADR-011 Phase D). **All catalog mutations route through `CatalogWriteGate` — no
direct SQLite writes.**

| Method | Does |
|--------|------|
| `ExportFromDatabase(db, snapshotId, clock)` | resolves the snapshot via `PlatformCatalogExportResolver` and exports a workbook (throws if the snapshot does not resolve) |
| `Propose(db, edited, clock, actor…)` / `ProposeFromFile(db, path, io, …)` | opens a `CatalogWriteGate`, stages via the importer, and returns a `PlatformWorkbookWriteResult` (batch ids + optional balance-drift advisory) |
| `ApproveBatches(db, batchIds, clock, actor…)` | approves each batch, then binds a release |
| `RejectBatches(db, batchIds, …, rationale)` | rejects each batch |

### Approve is idempotent + records a release (PLE-3.5)

`ProcessBatches` snapshots the **pending** batch set *before* processing. Only a
batch that was pending and commits counts as committed; re-approving an
already-approved batch returns `errors[batchId] = ["batch_already_committed_or_not_pending"]`
instead of double-committing. After **at least one** batch commits, it records an
immutable release through `CatalogSnapshotBinder.BindAfterApprove` (release token
`platform-workbook-<batchId>`), surfacing `ReleaseVersion`, `SnapshotId`, and
`ContentHashSha256` on `PlatformWorkbookWriteDecisionResult`. A
`CatalogBalanceDriftPipelineEvaluator` advisory (disabled by default) is folded
in as advisory notes only — it never blocks a commit.

---

## I/O adapter selection (canonical vs `.xlsx`)

`PlatformWorkbookIoSelection.Resolve(path, ioFlag, closedXmlFactory)` picks the
serializer without pulling ClosedXML into the engine-free core:

- explicit `--io` flag (`canonical`/`text` vs `closedxml`/`xlsx`) →
  `PLATFORM_WORKBOOK_IO` env var → path extension (`.txt`/`.platform.txt` ⇒
  canonical, `.xlsx` ⇒ ClosedXML) → binary default.
- `CanonicalTextWorkbookIo` is the dependency-free deterministic fallback (the
  headless/CI path). Requesting ClosedXML **without** supplying the factory
  throws `InvalidOperationException` — headless callers with no Excel adapter must
  pass `--io canonical`. The binary adapter and its EMCON dropdown validation are
  documented in the [`ProjectAegis.Data.Excel` README](../../src/ProjectAegis.Data.Excel/README.md).

---

## CLI / MCP surface

The [Mission Editor CLI](mission-editor-cli.md) exposes the round-trip as three
JSON-emitting verbs (also MCP tools):

```bash
# export a snapshot to an editable workbook
platform_export_xlsx --out platform.xlsx [--db catalog.db] [--snapshot <id>] \
  [--tl-tier TL-0..TL-5] [--io closedxml|canonical]

# stage an edited workbook through the write gate (NO auto-commit)
platform_import_xlsx --db catalog.db [--in platform.xlsx] [--io …]
#   → next step: catalog_write_approve <batchId>

# deterministic diff report (no writes)
platform_diff_xlsx [--db catalog.db] [--base <path>] [--edited <path>] [--io …]
```

Import stages only; approving the proposed batches is a separate write-gate step.

---

## Determinism & replay safety

The whole pipeline lives in `ProjectAegis.Data` on the **authoring/export path**,
not the simulation tick hotpath — it never touches `DelegationOrchestrator.Tick`
or the engagement/replay loop, so **nothing here affects the Baltic replay
goldens or the v2 hash `17144800277401907079`**. Within its own scope it is
strictly deterministic: injected `ICatalogClock` (no `DateTime.UtcNow`), ordinal
sorting everywhere, invariant-culture formatting, and a stable SHA-256 content
hash — which is what lets the round-trip golden and validator tests assert on
exact bytes.

## Extending safely

- **New sheet / column.** Add the builder in `PlatformWorkbookExporter` *and*
  extend the matching `Expected*Header` in `PlatformWorkbookValidator` (a header
  drift is a hard `PLE-*-HEADER` error), bump `SchemaVersion`, and regenerate the
  round-trip golden **intentionally**.
- **New fitting rule.** Add a `PLE-*` const + finding; keep the validator pure and
  its output sorted by `(Code, Message)`.
- **New stageable entity.** Add a `Propose*Batch` overload to the write gate
  (extend-only — see [catalog-write-gate.md](catalog-write-gate.md)), then a
  `BuildChanged*Rows` mapper in the importer; run orphan/TRL governance before
  proposing.
- **Never** reference ClosedXML from the core — keep binary serialization in the
  `ProjectAegis.Data.Excel` edge assembly (ADR-006).

## Verify against source

| Concern | Source of truth |
|---------|-----------------|
| Export, sheet order, `_Meta` | `src/ProjectAegis.Data/Platform/PlatformWorkbookExporter.cs` |
| Content hash | `src/ProjectAegis.Data/Platform/PlatformWorkbookHash.cs` |
| Structural diff | `src/ProjectAegis.Data/Platform/PlatformWorkbookDiff.cs` |
| Fitting validation / error codes | `src/ProjectAegis.Data/Platform/PlatformWorkbookValidator.cs` |
| Plan / stage / quarantine | `src/ProjectAegis.Data/Platform/PlatformWorkbookImporter.cs`, `PlatformImportPlan.cs`, `PlatformImportQuarantineEntry.cs` |
| Propose / approve / release | `src/ProjectAegis.Data/Platform/PlatformWorkbookWriteService.cs`, `PlatformWorkbookWriteResult.cs` |
| TRL / review gate | `src/ProjectAegis.Data/Catalog/CatalogImportGate.cs` |
| Adapter selection / fallback | `src/ProjectAegis.Data/Platform/PlatformWorkbookIoSelection.cs`, `CanonicalTextWorkbookIo.cs` |
| Pinned behaviour | `ProjectAegis.Data.Tests/Platform/PlatformWorkbook{RoundTrip,Validator,Importer,PhaseBImport,PhaseDWrite,GovernanceAdversarial,PeIntegrationHardening}Tests.cs`, `ProjectAegis.Data.Excel.Tests/PlatformWorkbook{BinaryGolden,EnumValidation}Tests.cs` |
| CLI verbs | `PlatformExportXlsxCommand.cs`, `PlatformImportXlsxCommand.cs`, `PlatformDiffXlsxCommand.cs` |

## See also

| Doc | Why |
|-----|-----|
| [`../../src/ProjectAegis.Data.Excel/README.md`](../../src/ProjectAegis.Data.Excel/README.md) | The production `.xlsx` (ClosedXML) adapter + EMCON dropdown validation. |
| [catalog-write-gate.md](catalog-write-gate.md) | The propose→approve→commit gate the importer stages into. |
| [catalog-release-train.md](catalog-release-train.md) | The immutable snapshot/release layer bound after approve (PLE-3.5). |
| [mission-editor-cli.md](mission-editor-cli.md) | The `platform_export_xlsx` / `platform_import_xlsx` / `platform_diff_xlsx` verbs. |
| [determinism-and-replay.md](determinism-and-replay.md) | The ordering / hashing / injected-clock rules this pipeline follows. |
| [../architecture/adr-011-platform-editor-excel-roundtrip.md](../architecture/adr-011-platform-editor-excel-roundtrip.md) | The platform-editor Excel round-trip decision. |
