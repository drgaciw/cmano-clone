# 21 - Platform Editor (Catalog Authoring & Excel Round-Trip)

**Last Updated:** 2026-07-08  
**Status:** Draft — ready for design review  
**FR reverse-ref:** [FR-19](01-Project-Overview.md) — Platform/catalog editor (Excel write-gate round-trip)  
**Author basis:** Codebase review of `ProjectAegis.Data` (write gate, snapshots, provenance, validation, importers); CMO Official Manual platform/DB concepts (clean-room, observable patterns only); requirements 06, 11, 15, 16, 18, 19.  
**Related:** [06-Database-Intelligence.md](06-Database-Intelligence.md) · [11-Agentic-Mission-Editor.md](11-Agentic-Mission-Editor.md) · [15-Sensor-Detection-And-EW.md](15-Sensor-Detection-And-EW.md) · [16-Logistics-And-Magazines.md](16-Logistics-And-Magazines.md) · [18-Combat-Domains.md](18-Combat-Domains.md) · [19-Cyber-And-Comms.md](19-Cyber-And-Comms.md)  
**Decision record:** [ADR-011 Platform Editor Excel Round-Trip (Accepted)](../../docs/architecture/adr-011-platform-editor-excel-roundtrip.md)  
**Tracker:** [implementation-tracker-2026-07-04.md](../implementation-tracker-2026-07-04.md) row 21 — **MVP-done / Partial+ (S56)**

## Purpose

Define requirements for a **Platform Editor**: an authoring capability that lets designers and curators create and edit the full configuration of every platform in the catalog — **ships, submarines, aircraft, ground units, and facilities** — including their **mobility, signatures, sensor suites, communications/datalinks, mounts, loadouts/magazines, and EMCON profiles** — with **Microsoft Excel as the primary editing surface**, all routed through the existing deterministic write gate (doc 06).

The objective is **complete flexibility and editing ease for all platform configurations in the database**, without sacrificing determinism, provenance, auditability, or validation.

Implements hub **[FR-19](01-Project-Overview.md)** (platform/catalog editor Excel write-gate round-trip).

## Vision

The platform database is a **first-class product** (doc 06). Designers should be able to open a workbook, see every platform and its fitting laid out as structured tables with dropdowns and validation, make sweeping or surgical edits, and re-import — with the system telling them exactly what changed, what broke, and what needs human approval before it reaches the simulation. Excel is the workbench; `ProjectAegis.Data` is the system of record. No edit ever bypasses the snapshot/write-gate/provenance machinery the simulation depends on.

## Scope (Locked Decisions — see ADR-011)

| Decision | Choice |
|----------|--------|
| **Excel integration model** | **Full round-trip + snapshot diff.** Export a bound snapshot to a multi-sheet `.xlsx`, edit offline, re-import; importer diffs against the source snapshot and stages **only changes** through `IWriteGate`. |
| **Primary editing surface** | **Excel-primary on the write-gate API.** Headless exporter/importer in `ProjectAegis.Data`; no new UI engine in v1. A read-only in-engine viewer is sequenced later. |
| **Schema scope** | **Full platform configuration schema specified now; delivery phased.** Phase A: sensors + loadouts/mounts/magazines + comms/datalinks. Phase B: signatures, mobility/propulsion, EMCON, damage model. |
| **Governance** | **All edits through the write gate**, with a **bulk-author batch path**: large imports validate as one batch rather than row-by-row, but commits affecting **> N records** (default 10, `CatalogValidationDefaults`) or any `balanceCritical` field still require explicit human `ApproveBatch` (consistent with **DBI-2.4**). |

**Out of scope (v1):** live Excel/Office.js add-in; in-engine WYSIWYG platform editor; TL-0–TL-5 branch databases (single `main` + tagged snapshots per doc 06 §4); cloud sync / modder public API.

## CMO Baseline — What We Must Match or Exceed

Parity targets derived from observable Command DB3000 / platform-editing patterns (clean-room — no proprietary data or text). No regression on items marked **P0**.

| Capability | CMO behavior (observed) | Aegis requirement |
|------------|-------------------------|-------------------|
| Browse/edit platform record | DB viewer + scenario unit properties | **P0** — every platform editable as a structured row set |
| Sensor fit per platform | Sensors attached with arcs/roles | **P0** — sensor suite rows (extends `CatalogSensorBinding`) |
| Mounts & weapon records | Mounts host weapons; weapon envelopes | **P0** — mount rows + weapon-record references |
| Loadout / magazine | Magazine→mount→weapon, depth, reload | **P0** — loadout + magazine rows (doc 16) |
| Comms / datalink fit | Platform comm gear & datalink membership | **P1** — comms/datalink fitting rows (doc 19) |
| Signatures | RCS / IR / acoustic / magnetic | **P1** — signature rows feeding detection (doc 15) |
| Propulsion / mobility | Speed bands, fuel, endurance, altitude/depth | **P1** — mobility rows feeding logistics (doc 16) |
| EMCON profile | Per-condition emitter posture | **P1** — EMCON profile rows |
| DB versioning | Scenario tied to DB version | **P0** — export/import bound to `dbSnapshotId` (doc 06) |
| Bulk edit | Limited in CMO | **Exceed** — full Excel round-trip with diff & batch validation |

## Editable Platform Configuration Schema

The catalog today persists a thin P0 slice — `CatalogPlatformEntry(PlatformId, LatDeg, LonDeg, CombatRadiusNm)`, `CatalogSensorBinding`, and a `WeaponEnvelopeDto`. This requirement defines the **full editable schema**; columns map to catalog tables and are surfaced as Excel sheets. Each editable value carries provenance (doc 06 §6): `ValueTier`, `Confidence`, `CitationRef`, `ReviewerId`, `ReviewState`, `TrlLevel`.

> Position (`LatDeg`/`LonDeg`) is **scenario placement**, not platform-class data, and is **excluded** from the platform editor — it belongs to the mission/scenario editor (doc 11). The platform editor edits **classes/types**, not scenario instances.

### Workbook layout (one sheet per entity domain)

| Sheet | Entity | Key columns (illustrative) | Phase | Catalog target |
|-------|--------|----------------------------|-------|----------------|
| `Platforms` | Platform class | `PlatformId` (PK), `DisplayName`, `Domain` (Surface/Sub/Air/Land/Facility), `Class`, `Nationality`, `YearCommissioned`, `GameTechnologyLevel`, `RequiresBlackProject`, `TechnologyMaturity`, `CombatRadiusNm` | A | `CatalogPlatformEntry` (extended) |
| `Mobility` | Propulsion/mobility | `PlatformId` (FK), `MaxSpeedKnots`, `CruiseSpeedKnots`, `MaxAltitudeFt`/`MaxDepthM`, `FuelCapacity`, `RangeNm`, `EnduranceHr` | B | new `platform_mobility` |
| `Signatures` | Signatures | `PlatformId` (FK), `RcsBandDbsm`, `IrSignature`, `AcousticSignatureDb`, `MagneticSignature` | B | new `platform_signature` |
| `Sensors` | Sensor suite | `PlatformId` (FK), `SensorId` (FK), `Role`, `ArcDeg`, `BasePd`, provenance cols | A | `CatalogSensorBinding` (extended) |
| `SensorCatalog` | Sensor type | `SensorId` (PK), `DisplayName`, `Type` (Radar/Sonar/ESM/EO/IR), `MaxRange`, `RangeUnit`, `Band` | A | new `sensor_catalog` |
| `Comms` | Comms/datalink fit | `PlatformId` (FK), `LinkId` (FK), `Role` (Tx/Rx/Relay), `SatcomCapable` | A* | new `platform_comms` |
| `LinkCatalog` | Datalink/net type | `LinkId` (PK), `DisplayName`, `LinkType` (Strategic/Tactical/Voice/SATCOM), `LatencyMsNominal` | A* | new `link_catalog` (doc 19) |
| `Mounts` | Mounts | `PlatformId` (FK), `MountId` (PK within platform), `MountType` (VLS/Rail/Gun/Tube/Pylon), `Arc`, `Capacity` | A | new `platform_mount` |
| `Loadouts` | Loadout preset | `PlatformId` (FK), `LoadoutId`, `LoadoutName`, `Role` (ASuW/ASW/AAW/Strike), `Default` | A | new `platform_loadout` |
| `Magazines` | Magazine stores | `LoadoutId` (FK), `MountId` (FK), `WeaponId` (FK), `Quantity`, `ReloadTimeSec`, `Depth` | A | new `platform_magazine` |
| `WeaponCatalog` | Weapon record | `WeaponId` (PK), `DisplayName`, `MinRangeMeters`, `MaxRangeMeters`, `WeaponType`, `Guidance` | A | `WeaponEnvelopeDto` (extended) |
| `Emcon` | EMCON profile | `PlatformId` (FK), `Condition` (Silent/Restricted/Free), `EmitterId` (FK), `Posture` (Off/Standby/Active) | B | new `platform_emcon` |
| `_Meta` | Snapshot binding | `SourceSnapshotId`, `SchemaVersion`, `ExportUtc`, `WorkbookHash` (read-only) | A | export metadata |

\* Comms (`Comms`/`LinkCatalog`) is **Phase A** per the user's explicit request, ahead of the doc-19 P1 default; behavior wiring into the sim may lag the catalog schema.

### Cross-sheet referential rules (enforced on import)

- Every FK (`SensorId`, `WeaponId`, `MountId`, `LinkId`, `LoadoutId`, `EmitterId`) must resolve within the workbook or the bound snapshot, else the row is **quarantined** (`CatalogQuarantinePromoter`), never silently dropped.
- `Magazines.MountId` must exist on the same `PlatformId` as its `Loadout`; magazine `WeaponId` must be compatible with `MountType` (e.g., VLS-capable weapon in a VLS mount).
- Canonical IDs are **immutable across releases** (DBI-7.3); renames are alias operations, not key rewrites.

## Functional Requirements

### 1. Snapshot Export to Excel

- Export a bound `dbSnapshotId` to a multi-sheet `.xlsx` with one sheet per entity domain, data-validation dropdowns for enums (domain, mount type, link type, review state, value tier), and a read-only `_Meta` sheet recording `SourceSnapshotId` + `WorkbookHash`.
- Export is **deterministic**: same snapshot → byte-stable workbook ordering (stable sort keys; no `DateTime.Now` — `ICatalogClock`).

**Acceptance**
- [x] **PLE-1.1** `PlatformWorkbookExporter.Export(snapshotId)` produces an `.xlsx` whose `_Meta.SourceSnapshotId` equals the requested snapshot and `WorkbookHash` is reproducible across runs. — `PlatformWorkbookRoundTripTests.Meta_sheet_binds_snapshot_and_content_hash`, `ClosedXmlPlatformWorkbookIoTests.ClosedXml_meta_snapshot_and_workbook_hash_survive_binary_round_trip`, `PlatformWorkbookBinaryGoldenTests`
- [ ] **PLE-1.2** Every enum column carries Excel data validation restricting input to the allowed set. — **Partial:** ClosedXML list validation for Emcon `Condition`/`Posture` (`ClosedXmlPlatformWorkbookIo`); full enum-column coverage incomplete
- [x] **PLE-1.3** Rows are sorted by stable composite keys (e.g., `Sensors` by `(PlatformId, SensorId)`) identical to `GetSortedSensorBindings()`. — `PlatformWorkbookRoundTripTests.CatalogSortKey_export_sheet_row_order_matches_comparer`, `PlatformWorkbookPhaseBSheetTests.Phase_B_rows_export_with_stable_OrderBy_keys`
- [x] **PLE-1.4** Export contains no machine-local paths or wall-clock timestamps in cell data (only in `_Meta.ExportUtc`, injected via `ICatalogClock`). — `PlatformWorkbookRoundTripTests.Export_is_deterministic` (`FixedCatalogClock`)

### 2. Excel Import with Snapshot Diff

- Parse an edited workbook, validate its `_Meta.SourceSnapshotId` against the live catalog, and compute a **field-level diff** between workbook rows and the source snapshot.
- Emit a structured **change set** (adds / edits / deletes per entity) — **not** a blind overwrite.

**Acceptance**
- [x] **PLE-2.1** `PlatformWorkbookImporter.Diff(workbook)` returns per-entity add/edit/delete sets; an unedited round-tripped workbook yields an **empty** diff (no spurious changes). — `PlatformWorkbookRoundTripTests.Unedited_round_trip_yields_empty_diff`, `PlatformWorkbookImporterTests.Plan_unedited_round_trip_has_no_changes`, `PlatformWorkbookBinaryGoldenTests.Phase_B_unedited_binary_round_trip_yields_empty_diff`
- [x] **PLE-2.2** Import rejects a workbook whose `SourceSnapshotId` is unknown or stale relative to the current head, with an explainable error. — `PlatformWorkbookImporterTests.Plan_unknown_snapshot_is_unresolved` (guard logic in `PlatformWorkbookImporter` / plan notes; no separate `PlatformEditVersionGuard` type)
- [ ] **PLE-2.3** A row whose FK does not resolve is routed to quarantine with a `CmoMarkdownQuarantineReportEntry`-style report entry, never committed. — **Partial:** orphan rows rejected / not proposed (`Stage_orphan_platform_mobility_is_rejected_not_proposed`, validator dangling-ref findings); full `CatalogQuarantinePromoter` report-entry path not universal for every sheet
- [x] **PLE-2.4** Deletions are explicit (row removed from sheet) and surface as a distinct diff category requiring confirmation. — `PlatformWorkbookDiff` emits `RowRemoved` / `SheetRemoved` (`PlatformWorkbookChangeKind`)

### 3. Write-Gate Staging & Governance

- Every diff is staged via `IWriteGate.Propose*Batch` and committed only through `ApproveBatch`; rejected batches leave live tables untouched (`RejectBatch`).
- **Bulk-author mode:** a large import is one batch with a single validation report; commits affecting **> N records** (default 10) or any `balanceCritical` field require explicit human approval.
- Every committed field writes a `CatalogChangeLogEntry` (`actorType`, `actorId`, `rationale`, `ApprovalState`, `RevisedUtcTicks`).

**Acceptance**
- [x] **PLE-3.1** Excel-originated changes reach live tables **only** after `ApproveBatch`; no direct-SQL or auto-commit path exists for the importer. — `PlatformWorkbookPhaseDWriteTests.E2E_sensor_export_edit_propose_approve_readback_via_write_service`, `PlatformWorkbookWriteBridgeTests`, `PlatformImportPanelTests.Import_host_and_projection_have_no_write_gate_bypass_patterns`
- [x] **PLE-3.2** A bulk import of M rows produces M change-log entries on commit and exactly one staging batch summary. — `CatalogWriteGatePlatformApproveTests.ApproveBatch_writes_change_log_for_platform_commit`, multi-entity propose tests
- [x] **PLE-3.3** A batch touching > 10 records or any `balanceCritical` field returns `Committed == false` until a human `ApproveBatch` is supplied (reuses **DBI-2.4** threshold). — `PlatformWorkbookImporterTests.Plan_large_changeset_requires_human_approval`
- [x] **PLE-3.4** `RejectBatch` discards staging and leaves the head snapshot unchanged. — `PlatformWorkbookPhaseDWriteTests.Reject_batch_discards_staging_without_live_commit`, `CatalogWriteGatePlatformApproveTests.RejectBatch_purges_all_staging_tables_DBI_1_4`
- [ ] **PLE-3.5** Commit produces a new immutable `snapshotId` / `db_release` row (doc 06 §4). — **Partial / reuse:** `DbSnapshotStore` / `DbReleaseRecord` exist; Excel commit path stages via gate batches — dedicated post-import release-row golden not cited here

### 4. Validation on Import

- Run the existing validation chain (`CatalogRulesValidationAgent`, `CatalogConsistencyAgent`, `ScenarioValidationEngine`-style rules) over the staged batch and attach a deterministic `ValidationReport`.
- New rule classes for platform fitting: mount/weapon compatibility, magazine depth ≤ mount capacity, sensor unit enums (nm/knots/Mach), comms link-type sanity, TL/provenance gating.

**Acceptance**
- [x] **PLE-4.1** Import surfaces unit-enum and sanity findings (range ≤ 0, Mach > 25) as explainable `ValidationFinding` codes (extends DBI-2.1/2.2). — `PlatformWorkbookValidatorTests` (negative quantity, over-capacity, clean fitting); Phase B validation packs
- [x] **PLE-4.2** Mount/weapon incompatibility and magazine-over-capacity produce blocking findings before approve. — `PlatformWorkbookValidatorTests.Over_capacity_is_flagged_as_error`, `Dangling_mount_reference_is_flagged`, `Stage_blocked_by_validation_error_stages_nothing`
- [x] **PLE-4.3** `ValidationReport` for a given workbook is deterministic (golden-hash stable). — `PlatformWorkbookValidatorTests.Findings_are_sorted_deterministically`
- [ ] **PLE-4.4** Speculative/near-future rows respect `TrlLevel` / `CatalogArchetypeGate`; black-project rows stay gated (docs 09/10). — **Partial / deferred** to archetype/TL export paths; not fully asserted on workbook import alone

### 5. Provenance Capture from Excel

- Provenance columns (`ValueTier`, `Confidence`, `CitationRef`, `ReviewerId`, `ReviewState`, `TrlLevel`) are first-class editable cells; defaults applied when blank (`gameplay_abstraction`, `provisional`).
- Values entered or changed via Excel default to `interpreted_value`/`provisional` until a reviewer sets `approved` (mirrors DBI-6.5).

**Acceptance**
- [x] **PLE-5.1** Round-tripped provenance is preserved exactly (no tier downgrade on unedited rows). — export/import preserve `ValueTier`/`ReviewState`/`CitationRef` on sensor/comms sheets; empty-diff round-trips above
- [x] **PLE-5.2** Blank `ValueTier` normalizes to `gameplay_abstraction`; unknown `ReviewState` normalizes to `provisional`. — `CatalogProvenanceTier.Normalize` + importer `ValueTier: CatalogProvenanceTier.Normalize(...)`; damage seed defaults (`CatalogPhaseBDamageMigrationTests`)
- [ ] **PLE-5.3** Edited values not explicitly approved export to sim as non-`approved` and are excluded from sim-visible export until promoted (reuses DBI-6.3 path). — **Partial / deferred:** sim-visible export gate is doc-06 path; not fully asserted as Excel-only AC

### 6. CLI & MCP Surface

- Headless CLI verbs (consistent with `catalog_import_markdown` / `catalog_write_propose`): `platform_export_xlsx`, `platform_import_xlsx`, `platform_diff_xlsx`.
- MCP tools mirror the CLI so Claude/Cursor can drive export → review → approve without bypassing the gate.

**Acceptance**
- [x] **PLE-6.1** `platform_export_xlsx --snapshot <id> --out <path>` writes a valid workbook headlessly. — `PlatformExportXlsxCommand` + `Program.cs` verb; ClosedXML default via `PlatformWorkbookIoFactories.ClosedXml`
- [x] **PLE-6.2** `platform_import_xlsx --in <path>` stages a batch and prints the validation report + diff summary; commit requires a separate explicit approve verb. — `PlatformImportXlsxCommand` (wires `IWriteGate`; no auto-commit per PLE-6.3 note in source)
- [x] **PLE-6.3** MCP and CLI share the same `IWriteGate` / `ICatalogReader` APIs — no special auto-commit path (reuses doc 06 P0 guardrail). — `McpToolsManifestTests` lists `platform_export_xlsx` / `platform_import_xlsx` / `platform_diff_xlsx`; import command docs PLE-6.3 / DBI-8.3

## Non-Functional Requirements

- **Determinism:** export and diff are pure functions of (snapshot, workbook); no wall-clock or locale dependence in cell data. Excel number/locale parsing pinned to `InvariantCulture`.
- **No `UnityEngine` in `ProjectAegis.Data`** (ADR-001); exporter/importer live in the Data assembly. Excel I/O via a headless library (**ClosedXML**, MIT — **Accepted**) isolated behind an `IPlatformWorkbookIo` port so the Data assembly stays test-friendly and engine-free (ADR-006 boundary). Production impl: `ClosedXmlPlatformWorkbookIo` in `ProjectAegis.Data.Excel`; golden/reference: `CanonicalTextWorkbookIo`.
- **Scale:** handle thousands of platforms / tens of thousands of fitting rows without degradation; streaming read of large workbooks.
- **Auditability:** every change reversible via snapshot rollback; full provenance and change-log coverage.
- **Headless tests** in `ProjectAegis.Data.Tests` for round-trip fidelity, diff emptiness, quarantine routing, and golden validation hashes.

## Architecture Notes

- **Shipped ports/types:** `IPlatformWorkbookIo`, `ClosedXmlPlatformWorkbookIo`, `CanonicalTextWorkbookIo`, `PlatformWorkbookExporter`, `PlatformWorkbookImporter`, `PlatformWorkbookDiff`, `PlatformWorkbookValidator`, `PlatformWorkbookWriteService`, plus catalog rows `CatalogMount`, `CatalogLoadout`, `CatalogMagazineEntry`, `CatalogCommsBinding`, `CatalogLinkEntry`, `CatalogSignature`, `CatalogMobility`, `CatalogEmcon` (EMCON profile row; not named `CatalogEmconProfile`), `CatalogPlatformDamage`, and extended `CatalogPlatformEntry` / `CatalogSensorBinding` / `CatalogWeaponRecord`.
- **Snapshot guard:** unknown/stale `SourceSnapshotId` handled in `PlatformWorkbookImporter` plan path (no separate `PlatformEditVersionGuard` type on disk).
- **Reuse, don't replace:** staging (`CatalogWriteGate` — **extend-only consumer**; Excel path does not claim write-path mutations), provenance (`CatalogProvenanceTier`), snapshots (`DbSnapshotStore`), change log (`CatalogChangeLogEntry`), quarantine (`CatalogQuarantinePromoter` / mount-loadout triage), validation agents — the editor is a **front door** onto existing machinery.
- **Impact discipline (CLAUDE.md):** run `gitnexus_impact` before extending `CatalogPlatformEntry`, `CatalogSensorBinding`, `ICatalogReader`, or `WeaponEnvelopeDto` — these are consumed by `ProjectAegis.Sim` (engage/envelope, DATA-4) and `ProjectAegis.Delegation`; widening them is likely **HIGH** blast radius.
- **Not shipped:** in-engine WYSIWYG platform editor; live Excel/Office.js add-in; full sheet/PK workbook protection (OQ5).

## Implementation Mapping (headless)

| Area | Path / type | Status | Evidence |
|------|-------------|--------|----------|
| Excel I/O port | `IPlatformWorkbookIo` (`ProjectAegis.Data` · `Platform/`) | **Shipped** | `IPlatformWorkbookIo.cs`; selection via `PlatformWorkbookIoSelection` |
| ClosedXML adapter | `ClosedXmlPlatformWorkbookIo`, `PlatformWorkbookIoFactories` (`ProjectAegis.Data.Excel`) | **Shipped** | `ClosedXmlPlatformWorkbookIoTests`, `PlatformWorkbookBinaryGoldenTests` |
| Canonical text I/O | `CanonicalTextWorkbookIo` | **Shipped** | golden/reference path in round-trip tests |
| Export | `PlatformWorkbookExporter`, `PlatformWorkbookHash`, `PlatformCatalogExportResolver` | **Shipped** | `PlatformWorkbookRoundTripTests`, Phase B sheet tests |
| Import / diff | `PlatformWorkbookImporter`, `PlatformWorkbookDiff`, `PlatformImportPlan` | **Shipped** | `PlatformWorkbookImporterTests`, empty-diff goldens |
| Write orchestration | `PlatformWorkbookWriteService`, `PlatformWorkbookWriteResult` | **Shipped** | `PlatformWorkbookPhaseDWriteTests` |
| Fitting validation | `PlatformWorkbookValidator` | **Shipped** | `PlatformWorkbookValidatorTests` |
| Staging consumer | `IWriteGate`, `CatalogWriteGate` (`WriteGate/`) | **Shipped (extend-only reuse)** | `CatalogWriteGatePlatformApproveTests`, Phase B/D approve tests — **no claim of write-path rewrite** |
| Snapshots / release | `DbSnapshotStore`, `DbReleaseRecord` | **Shipped (reuse)** | snapshot bind on `_Meta.SourceSnapshotId` |
| Provenance | `CatalogProvenanceTier`, `CatalogChangeLogEntry` | **Shipped (reuse)** | importer normalize + change-log approve tests |
| Catalog rows (Phase A) | `CatalogMount`, `CatalogLoadout`, `CatalogMagazineEntry`, `CatalogCommsBinding`, `CatalogLinkEntry`, `CatalogSensorBinding`, `CatalogWeaponRecord` | **Shipped** | export/import stage tests per entity |
| Catalog rows (Phase B) | `CatalogMobility`, `CatalogSignature`, `CatalogEmcon`, `CatalogPlatformDamage` | **Shipped** | `CatalogPhaseBReaderTests`, `PlatformWorkbookPhaseBImportTests`, damage migration/reader tests |
| CLI verbs | `platform_export_xlsx`, `platform_import_xlsx`, `platform_diff_xlsx` (`MissionEditor.Cli`) | **Shipped** | `PlatformExportXlsxCommand`, `PlatformImportXlsxCommand`, `PlatformDiffXlsxCommand`, `McpToolsManifestTests` |
| Unity read-only viewer | `PlatformCatalogViewerHost` (`unity/.../Scripts/Runtime/`) | **Partial+** | `PlatformCatalogViewerTests`, `PlatformCommsTests`, `PlatformLinkCatalogTests` — **not** WYSIWYG editor |
| Unity import chrome | `PlatformImportPanelHost` | **Partial+** | `PlatformImportPanelTests` (propose/approve/reject proxy) |
| Unity write bridge | `PlatformWorkbookWriteBridge`, `PlatformCatalogExportBridge` (`UnityAdapter` · `Bridge/`) | **Partial+** | `PlatformWorkbookWriteBridgeTests`, `PlatformCatalogExportBridgeTests` |
| Projections | `PlatformCatalogListProjection`, `PlatformCatalogDetailProjection`, `PlatformImportStagingProjection`, comms/link list projections (`Delegation` · `Projection/`) | **Shipped (headless)** | Delegation.Tests + UA Platform tests |
| Data tests | `src/ProjectAegis.Data.Tests/Platform/` | **Shipped** | round-trip, importer, validator, Phase B/D, ClosedXML |
| UA / proxy tests | `src/ProjectAegis.Delegation.UnityAdapter.Tests/Platform/` | **Shipped (proxy)** | import panel, viewer, write bridge, comms, link catalog |
| Live Editor screenshots | presentation PNGs / Editor Mode capture | **Residual → Phase N** | tracker residual: Live Editor screenshots (not product AC for MVP grade) |
| In-engine WYSIWYG editor | (none) | **Not shipped** | out of scope v1 / ADR-011 |

## Phasing

| Phase | Content | Status | Gate / residual |
|-------|---------|--------|-----------------|
| **A** | Platforms (extended), Sensors, SensorCatalog, Mounts, Loadouts, Magazines, WeaponCatalog, **Comms/LinkCatalog**; export + diff + write-gate staging + fitting validation + CLI | **Shipped (Partial+ / MVP headless)** | Round-trip empty-diff golden + write-gate parity — met in Data.Tests |
| **B** | Mobility, Signatures, EMCON, damage model columns; schema export/import | **Shipped (Partial+)** | Phase B sheet/import/damage tests; sim wiring residual where docs 15/19 lag |
| **C** | Read-only in-engine viewer (browse/filter/detail) | **Shipped Partial+** | `PlatformCatalogViewerHost` + proxy tests; **not** full TL branch UI |
| **D** | In-engine / bridge Excel write path (propose→approve via gate) | **Shipped Partial+** | `PlatformWorkbookWriteService` + `PlatformWorkbookWriteBridge` + Phase D tests |
| **E–H** | Import panel chrome, damage/comms/LinkCatalog Unity surfacing (Phases E–H per S29–S34) | **Shipped Partial+ (headless/proxy)** | UA Platform/* tests; presentation polish residual |
| **Phase N** | **Live Editor screenshots** / full Editor Mode presentation evidence; remaining OQ5 sheet/PK protection; full enum data-validation matrix; WYSIWYG editor (if ever) | **Residual** | Tracker residual: Live Editor screenshots |

## Open Questions / Decisions Needed

| # | Question | Recommendation / status |
|---|----------|-------------------------|
| 1 | Target authors: internal curators only, or community modders too (v1)? | **Internal first**; community via doc-06 public-intake track later. |
| 2 | Excel library: `ClosedXML` vs `EPPlus` (licensing — EPPlus is non-commercial-restricted) vs OpenXML SDK? | **ClosedXML Accepted** — production code uses `ClosedXML` via `ClosedXmlPlatformWorkbookIo` (`ProjectAegis.Data.Excel`); MIT; behind `IPlatformWorkbookIo`. |
| 3 | Bulk threshold `N` for forced human approval? | Reuse **DBI-2.4 default of 10**; make per-entity tuneable in `CatalogValidationDefaults`. — exercised by `Plan_large_changeset_requires_human_approval`. |
| 4 | Should comms/datalink **behavior** (doc 19) ship with the Phase-A schema, or schema-only first? | **Schema-only in A**, behavior wiring lag OK — schema + Unity surfacing shipped; sim share-lag bridge is separate. |
| 5 | Workbook protection: lock `_Meta` and PK columns against edit? | **Partial / deferred** — `ClosedXmlPlatformWorkbookIo` comment: sheet/PK-column protection (OQ5) remains deferred; Emcon list validation exists. |

## Traceability

| Source | This document |
|--------|---------------|
| Hub FR-19 | Platform/catalog editor Excel write-gate round-trip ([01](01-Project-Overview.md)) |
| Doc 06 Database Intelligence | Write gate, snapshots, provenance, validation, quarantine — all reused |
| Doc 11 Mission Editor | Scenario placement stays in mission editor; platform editor edits classes |
| Doc 15 Sensors/EW | Sensor suite + signature rows feed detection |
| Doc 16 Logistics/Magazines | Mount→loadout→magazine chain |
| Doc 19 Cyber/Comms | Comms/datalink fitting rows |
| ADR-001 / ADR-006 | Assembly boundary; Data stays engine-free, Excel I/O behind a port |
| ADR-011 | Locked decisions for this capability |

---

**Implementation grade:** MVP-done / Partial+ (S56) — see [implementation-tracker-2026-07-04.md](../implementation-tracker-2026-07-04.md) row 21.  
Design Status remains **Draft**. Charter re-honesty: Wave 3 2026-07-08.
