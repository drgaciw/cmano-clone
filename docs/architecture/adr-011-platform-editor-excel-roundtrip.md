# ADR-011: Platform Editor — Excel Round-Trip on the Write Gate

## Status

**Accepted**

## Date

2026-06-17

## Decision Makers

Enterprise architect (DRGAMTD); milsim architecture review. Pairs with requirement [21-Platform-Editor](../../Game-Requirements/requirements/21-Platform-Editor.md) and builds on [06-Database-Intelligence](../../Game-Requirements/requirements/06-Database-Intelligence.md), [ADR-006 Data Layer Boundary](adr-006-data-layer-boundary.md), and [ADR-008 Mission Editor Validation Engine](adr-008-mission-editor-validation-engine.md).

## Context

The catalog (`ProjectAegis.Data`) has mature, deterministic governance — staged write gate (`IWriteGate`), immutable snapshots / release trains (`DbSnapshotStore`, `db_release`), per-field provenance (`CatalogProvenanceTier`), append-only change log (`CatalogChangeLogEntry`), quarantine, and a deterministic validation chain. What it lacks is an **authoring surface** for full platform configuration (ships, subs, aircraft, ground, facilities) and their **mobility, signatures, sensor suites, comms/datalinks, mounts, loadouts/magazines, and EMCON** — and any **Microsoft Excel** path. The persisted model is a thin P0 slice (`CatalogPlatformEntry(PlatformId, Lat, Lon, CombatRadiusNm)`, `CatalogSensorBinding`, `WeaponEnvelopeDto`); "Full platform import" is marked **Not started**.

The requirement is complete flexibility and editing ease for **all** platform configurations, with Excel as a supported editing tool, **without** regressing determinism, provenance, auditability, or validation.

## Decision

Build a **headless Platform Editor in `ProjectAegis.Data`** whose primary editing surface is a **multi-sheet Excel workbook** that round-trips through the existing write gate. Four decisions are locked:

### 1. Excel model — full round-trip + snapshot diff

Export a bound `dbSnapshotId` to `.xlsx` (one sheet per entity domain), edit offline, re-import. The importer **diffs the workbook against its source snapshot** (recorded in a read-only `_Meta` sheet) and stages **only the changes** — not a blind overwrite. An unedited round-tripped workbook must produce an **empty diff**.

*Rejected:* import-only (no safe edit-existing workflow, weak audit); live Office.js add-in (heavyweight, needs a live service, complicates determinism and the propose/approve gate); CSV-per-entity (loses Excel validation/dropdowns/multi-sheet ergonomics).

### 2. Editing surface — Excel-primary on the write-gate API

The exporter/importer are headless services in `ProjectAegis.Data`, exposed via CLI (`platform_export_xlsx` / `platform_import_xlsx` / `platform_diff_xlsx`) and matching MCP tools. **No new UI engine in v1.** A read-only in-engine viewer is sequenced post-P0. Excel I/O sits behind an `IPlatformWorkbookIo` port so the Data assembly stays engine-free (ADR-006) and unit-testable.

*Rejected:* in-engine Unity panel (large UI build, violates headless-first); standalone external app (whole app to maintain); agent/MCP-only (no human authoring ergonomics).

### 3. Schema scope — full schema specified now, phased delivery

The complete editable schema (platform core, mobility, signatures, sensor suite, comms/datalinks, mounts, loadouts, magazines, EMCON) is specified in doc 21 up front, but delivered in phases: **Phase A** = sensors + mounts/loadouts/magazines + comms/datalinks (comms pulled forward to A per the user's explicit ask); **Phase B** = signatures, mobility, EMCON, damage. Canonical IDs are immutable across phases.

*Rejected:* everything-at-once (longest lead, highest risk); narrow sensors+loadouts+comms-only (re-litigates schema later); full DB3000 taxonomy in one pass (excessive modeling effort before first value).

### 4. Governance — write gate for all edits, with a bulk-author batch path

Every Excel-originated change is staged via `IWriteGate.Propose*Batch` and committed only through `ApproveBatch`. A **bulk-author** mode validates a large import as a single batch (not row-by-row), but commits affecting **> N records** (default 10, `CatalogValidationDefaults`) or any `balanceCritical` field still require explicit human `ApproveBatch` — consistent with **DBI-2.4**. No direct-SQL/auto-commit path exists for the importer.

*Rejected:* strict per-batch-review-always (no fast bulk path); trusted direct-write for designers (breaks uniform audit and provenance).

## Consequences

**Positive**
- Reuses all existing governance — the editor is a new front door, not a parallel system.
- Excel gives designers familiar bulk-edit ergonomics (dropdowns, validation, multi-sheet) with deterministic, auditable, reversible commits.
- Headless + engine-free keeps it CI-testable (round-trip fidelity, empty-diff golden, validation hashes).

**Negative / risks**
- New Excel I/O dependency (recommend **ClosedXML / MIT**, behind `IPlatformWorkbookIo`, to avoid EPPlus non-commercial licensing). See doc 21 Open Question 2.
- Extending `CatalogPlatformEntry` / `CatalogSensorBinding` / `WeaponEnvelopeDto` touches `ProjectAegis.Sim` and `ProjectAegis.Delegation` consumers — **run `gitnexus_impact` before widening; expect HIGH blast radius** (CLAUDE.md mandate).
- Round-trip determinism requires strict `InvariantCulture` parsing and stable sort keys; locale/number drift is the most likely correctness bug.
- New catalog tables (mounts, loadouts, magazines, comms, signatures, mobility, EMCON) need migrations and golden fixtures.

## Compliance / Verification

- Empty-diff golden test: export snapshot → import unedited → zero staged changes.
- Write-gate parity test: no importer path reaches live tables without `ApproveBatch`; bulk batch over threshold returns `Committed == false` until approved.
- Provenance round-trip test: tiers/review states preserved on unedited rows; blanks normalized per doc 06 §6.
- Validation determinism: golden hash for a fixture workbook's `ValidationReport`.
- ADR-001/006 boundary: no `UnityEngine` reference enters `ProjectAegis.Data`; Excel I/O isolated behind the port.

## Related

- Requirement [21-Platform-Editor](../../Game-Requirements/requirements/21-Platform-Editor.md)
- [06-Database-Intelligence](../../Game-Requirements/requirements/06-Database-Intelligence.md) (write gate, snapshots, provenance)
- [ADR-006 Data Layer Boundary](adr-006-data-layer-boundary.md), [ADR-008 Mission Editor Validation Engine](adr-008-mission-editor-validation-engine.md)
