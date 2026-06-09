# 21 - Platform Editor (Catalog Authoring & Excel Round-Trip)

**Last Updated:** 2026-06-08
**Status:** Draft — ready for design review
**Author basis:** Codebase review of `ProjectAegis.Data` (write gate, snapshots, provenance, validation, importers); CMO Official Manual platform/DB concepts (clean-room, observable patterns only); requirements 06, 11, 15, 16, 18, 19.
**Related:** [06-Database-Intelligence.md](06-Database-Intelligence.md) · [11-Agentic-Mission-Editor.md](11-Agentic-Mission-Editor.md) · [15-Sensor-Detection-And-EW.md](15-Sensor-Detection-And-EW.md) · [16-Logistics-And-Magazines.md](16-Logistics-And-Magazines.md) · [18-Combat-Domains.md](18-Combat-Domains.md) · [19-Cyber-And-Comms.md](19-Cyber-And-Comms.md)
**Decision record:** [ADR-011 Platform Editor Excel Round-Trip](../../docs/architecture/adr-011-platform-editor-excel-roundtrip.md)

## Purpose

Define requirements for a **Platform Editor**: an authoring capability that lets designers and curators create and edit the full configuration of every platform in the catalog — **ships, submarines, aircraft, ground units, and facilities** — including their **mobility, signatures, sensor suites, communications/datalinks, mounts, loadouts/magazines, and EMCON profiles** — with **Microsoft Excel as the primary editing surface**, all routed through the existing deterministic write gate (doc 06).

The objective is **complete flexibility and editing ease for all platform configurations in the database**, without sacrificing determinism, provenance, auditability, or validation.

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
- [ ] **PLE-1.1** `PlatformWorkbookExporter.Export(snapshotId)` produces an `.xlsx` whose `_Meta.SourceSnapshotId` equals the requested snapshot and `WorkbookHash` is reproducible across runs.
- [ ] **PLE-1.2** Every enum column carries Excel data validation restricting input to the allowed set.
- [ ] **PLE-1.3** Rows are sorted by stable composite keys (e.g., `Sensors` by `(PlatformId, SensorId)`) identical to `GetSortedSensorBindings()`.
- [ ] **PLE-1.4** Export contains no machine-local paths or wall-clock timestamps in cell data (only in `_Meta.ExportUtc`, injected via `ICatalogClock`).

### 2. Excel Import with Snapshot Diff

- Parse an edited workbook, validate its `_Meta.SourceSnapshotId` against the live catalog, and compute a **field-level diff** between workbook rows and the source snapshot.
- Emit a structured **change set** (adds / edits / deletes per entity) — **not** a blind overwrite.

**Acceptance**
- [ ] **PLE-2.1** `PlatformWorkbookImporter.Diff(workbook)` returns per-entity add/edit/delete sets; an unedited round-tripped workbook yields an **empty** diff (no spurious changes).
- [ ] **PLE-2.2** Import rejects a workbook whose `SourceSnapshotId` is unknown or stale relative to the current head (`ScenarioEditVersionGuard` analogue — `PlatformEditVersionGuard`), with an explainable error.
- [ ] **PLE-2.3** A row whose FK does not resolve is routed to quarantine with a `CmoMarkdownQuarantineReportEntry`-style report entry, never committed.
- [ ] **PLE-2.4** Deletions are explicit (row removed from sheet) and surface as a distinct diff category requiring confirmation.

### 3. Write-Gate Staging & Governance

- Every diff is staged via `IWriteGate.Propose*Batch` and committed only through `ApproveBatch`; rejected batches leave live tables untouched (`RejectBatch`).
- **Bulk-author mode:** a large import is one batch with a single validation report; commits affecting **> N records** (default 10) or any `balanceCritical` field require explicit human approval.
- Every committed field writes a `CatalogChangeLogEntry` (`actorType`, `actorId`, `rationale`, `ApprovalState`, `RevisedUtcTicks`).

**Acceptance**
- [ ] **PLE-3.1** Excel-originated changes reach live tables **only** after `ApproveBatch`; no direct-SQL or auto-commit path exists for the importer.
- [ ] **PLE-3.2** A bulk import of M rows produces M change-log entries on commit and exactly one staging batch summary.
- [ ] **PLE-3.3** A batch touching > 10 records or any `balanceCritical` field returns `Committed == false` until a human `ApproveBatch` is supplied (reuses **DBI-2.4** threshold).
- [ ] **PLE-3.4** `RejectBatch` discards staging and leaves the head snapshot unchanged.
- [ ] **PLE-3.5** Commit produces a new immutable `snapshotId` / `db_release` row (doc 06 §4).

### 4. Validation on Import

- Run the existing validation chain (`CatalogRulesValidationAgent`, `CatalogConsistencyAgent`, `ScenarioValidationEngine`-style rules) over the staged batch and attach a deterministic `ValidationReport`.
- New rule classes for platform fitting: mount/weapon compatibility, magazine depth ≤ mount capacity, sensor unit enums (nm/knots/Mach), comms link-type sanity, TL/provenance gating.

**Acceptance**
- [ ] **PLE-4.1** Import surfaces unit-enum and sanity findings (range ≤ 0, Mach > 25) as explainable `ValidationFinding` codes (extends DBI-2.1/2.2).
- [ ] **PLE-4.2** Mount/weapon incompatibility and magazine-over-capacity produce blocking findings before approve.
- [ ] **PLE-4.3** `ValidationReport` for a given workbook is deterministic (golden-hash stable).
- [ ] **PLE-4.4** Speculative/near-future rows respect `TrlLevel` / `CatalogArchetypeGate`; black-project rows stay gated (docs 09/10).

### 5. Provenance Capture from Excel

- Provenance columns (`ValueTier`, `Confidence`, `CitationRef`, `ReviewerId`, `ReviewState`, `TrlLevel`) are first-class editable cells; defaults applied when blank (`gameplay_abstraction`, `provisional`).
- Values entered or changed via Excel default to `interpreted_value`/`provisional` until a reviewer sets `approved` (mirrors DBI-6.5).

**Acceptance**
- [ ] **PLE-5.1** Round-tripped provenance is preserved exactly (no tier downgrade on unedited rows).
- [ ] **PLE-5.2** Blank `ValueTier` normalizes to `gameplay_abstraction`; unknown `ReviewState` normalizes to `provisional`.
- [ ] **PLE-5.3** Edited values not explicitly approved export to sim as non-`approved` and are excluded from sim-visible export until promoted (reuses DBI-6.3 path).

### 6. CLI & MCP Surface

- Headless CLI verbs (consistent with `catalog_import_markdown` / `catalog_write_propose`): `platform_export_xlsx`, `platform_import_xlsx`, `platform_diff_xlsx`.
- MCP tools mirror the CLI so Claude/Cursor can drive export → review → approve without bypassing the gate.

**Acceptance**
- [ ] **PLE-6.1** `platform_export_xlsx --snapshot <id> --out <path>` writes a valid workbook headlessly.
- [ ] **PLE-6.2** `platform_import_xlsx --in <path>` stages a batch and prints the validation report + diff summary; commit requires a separate explicit approve verb.
- [ ] **PLE-6.3** MCP and CLI share the same `IWriteGate` / `ICatalogReader` APIs — no special auto-commit path (reuses doc 06 P0 guardrail).

## Non-Functional Requirements

- **Determinism:** export and diff are pure functions of (snapshot, workbook); no wall-clock or locale dependence in cell data. Excel number/locale parsing pinned to `InvariantCulture`.
- **No `UnityEngine` in `ProjectAegis.Data`** (ADR-001); exporter/importer live in the Data assembly. Excel I/O via a headless library (e.g., a `ClosedXML`/`EPPlus`-class dependency) isolated behind an `IPlatformWorkbookIo` port so the Data assembly stays test-friendly and engine-free (ADR-006 boundary).
- **Scale:** handle thousands of platforms / tens of thousands of fitting rows without degradation; streaming read of large workbooks.
- **Auditability:** every change reversible via snapshot rollback; full provenance and change-log coverage.
- **Headless tests** in `ProjectAegis.Data.Tests` for round-trip fidelity, diff emptiness, quarantine routing, and golden validation hashes.

## Architecture Notes

- New ports/types (proposed): `IPlatformWorkbookIo`, `PlatformWorkbookExporter`, `PlatformWorkbookImporter`, `PlatformWorkbookDiff`, `PlatformEditVersionGuard`, plus catalog rows `CatalogMount`, `CatalogLoadout`, `CatalogMagazineEntry`, `CatalogCommsBinding`, `CatalogSignature`, `CatalogMobility`, `CatalogEmconProfile`, and extended `CatalogPlatformEntry` / `CatalogSensorBinding` / `CatalogWeaponRecord`.
- **Reuse, don't replace:** staging (`CatalogWriteGate`), provenance (`CatalogProvenanceTier`), snapshots (`DbSnapshotStore`), change log (`CatalogChangeLogEntry`), quarantine (`CatalogQuarantinePromoter`), validation agents — the editor is a **new front door** onto existing machinery.
- **Impact discipline (CLAUDE.md):** run `gitnexus_impact` before extending `CatalogPlatformEntry`, `CatalogSensorBinding`, `ICatalogReader`, or `WeaponEnvelopeDto` — these are consumed by `ProjectAegis.Sim` (engage/envelope, DATA-4) and `ProjectAegis.Delegation`; widening them is likely **HIGH** blast radius.

## Implementation Mapping (headless)

| Requirement area | Type / path (`ProjectAegis.Data`) | Status |
|------------------|-----------------------------------|--------|
| Excel I/O port | `IPlatformWorkbookIo`, `IPlatformWorkbookIo` impl (ClosedXML-class) | New |
| Export | `PlatformWorkbookExporter` | New |
| Import / diff | `PlatformWorkbookImporter`, `PlatformWorkbookDiff`, `PlatformEditVersionGuard` | New |
| Staging | `IWriteGate`, `CatalogWriteGate` | Reuse |
| Snapshots / release | `DbSnapshotStore`, `DbReleaseRecord` | Reuse |
| Provenance | `CatalogProvenanceTier`, change log | Reuse |
| Validation | `CatalogRulesValidationAgent`, `CatalogConsistencyAgent`, new fitting rules | Extend |
| New catalog rows | `CatalogMount`, `CatalogLoadout`, `CatalogMagazineEntry`, `CatalogCommsBinding`, `CatalogSignature`, `CatalogMobility`, `CatalogEmconProfile` | New |
| CLI / MCP | `platform_export_xlsx`, `platform_import_xlsx`, `platform_diff_xlsx` | New |
| Tests | `src/ProjectAegis.Data.Tests/Platform/` | New |

## Phasing

| Phase | Content | Gate |
|-------|---------|------|
| **A** | Platforms (extended), Sensors, SensorCatalog, Mounts, Loadouts, Magazines, WeaponCatalog, **Comms/LinkCatalog**; export + diff + write-gate staging + fitting validation + CLI | Round-trip empty-diff golden + write-gate parity |
| **B** | Mobility, Signatures, EMCON, damage model columns; sim wiring for comms/signatures (docs 15/19) | Validation rule pack + sim consumption tests |
| **C (post-P0)** | Read-only in-engine viewer; TL branch awareness; modder workbook templates | Doc 06 §4 TL branches |

## Open Questions / Decisions Needed

| # | Question | Recommendation |
|---|----------|----------------|
| 1 | Target authors: internal curators only, or community modders too (v1)? | **Internal first**; community via doc-06 public-intake track later. |
| 2 | Excel library: `ClosedXML` vs `EPPlus` (licensing — EPPlus is non-commercial-restricted) vs OpenXML SDK? | **`ClosedXML`** (MIT) behind `IPlatformWorkbookIo` to avoid EPPlus license entanglement. Confirm. |
| 3 | Bulk threshold `N` for forced human approval? | Reuse **DBI-2.4 default of 10**; make per-entity tuneable in `CatalogValidationDefaults`. |
| 4 | Should comms/datalink **behavior** (doc 19) ship with the Phase-A schema, or schema-only first? | **Schema-only in A**, behavior wiring in B — decouples catalog editing from sim work. |
| 5 | Workbook protection: lock `_Meta` and PK columns against edit? | **Yes** — protect `_Meta` and PK cells; renames go through alias verb, not cell edit. |

## Traceability

| Source | This document |
|--------|---------------|
| Doc 06 Database Intelligence | Write gate, snapshots, provenance, validation, quarantine — all reused |
| Doc 11 Mission Editor | Scenario placement stays in mission editor; platform editor edits classes |
| Doc 15 Sensors/EW | Sensor suite + signature rows feed detection |
| Doc 16 Logistics/Magazines | Mount→loadout→magazine chain |
| Doc 19 Cyber/Comms | Comms/datalink fitting rows |
| ADR-001 / ADR-006 | Assembly boundary; Data stays engine-free, Excel I/O behind a port |
| ADR-011 | Locked decisions for this capability |

---

**Status:** Draft — ready for design review
