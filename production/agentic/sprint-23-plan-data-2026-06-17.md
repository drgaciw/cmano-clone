# Sprint 23 — Data / Platform Plan

**Date:** 2026-06-17  
**Domain:** Data layer (Req 06, Req 21)  
**Predecessor:** Sprint 22 SHIPPED @ `7253381` — write-gate Propose* coverage, CLI verbs, CmoMarkdown import, `BalanceTelemetryAccumulator`, OSINT TL routing  
**Author:** Sprint 23 Data/Platform Planning agent  
**Out of scope:** Unity kickoff, `sprint-status.yaml` (program agent owns)

---

## Sprint 23 data goal

Close the Platform Editor **commit loop** by extending `ApproveBatch` to persist all Phase A staged entity types, and land **ClosedXML binary `.xlsx` round-trip** behind `IPlatformWorkbookIo` so Req 21 PLE-6.x is no longer canonical-text-only.

---

## Context summary (Sprint 22 carryover)

| Item | Sprint 22 state | Sprint 23 action |
|------|-----------------|------------------|
| `Propose*Batch` (sensor/mount/loadout/magazine/comms/platform/weapon) | **Done** — staging tables + insert paths in `007_platform_editor_phase_a.sql` | Extend **commit** path only |
| `ApproveBatch` | **Sensor-only** — `LoadStagingRows` reads `catalog_staging_sensor` only; platform/weapon rows never reach live tables | **S23-D01** (must-have) |
| ClosedXML adapter | Skeleton at `src/ProjectAegis.Data.Excel/ClosedXmlPlatformWorkbookIo.cs`; **not in** `ProjectAegis.sln`; CLI uses `CanonicalTextWorkbookIo` | **S23-D02** (must-have) |
| ADR-011 | **Accepted** (2026-06-17) | Phase B scope sequenced as should-have |
| CanonicalId / sort-key determinism | QA plan gap: new `Catalog*` types use `PlatformId`/`WeaponId` keys but lack golden determinism tests; `OrderBy` present in Propose paths | **S23-D03** (must-have) |
| `BalanceTelemetrySinkFactory` | Accumulator + factory tests exist; **not wired** to orchestrator/batch runner | **S23-D05** (should-have, advisory) |
| OSINT TL routing (22-7) | **Shipped** in S22 | No Sprint 23 data work |
| Full-solution test gate | Retro action #7 — never run as single batch post-rebase | **S23-D08** (nice-to-have) |

### ADR-011 Phase B scope (deferred to should-have)

Per [adr-011-platform-editor-excel-roundtrip.md](../../docs/architecture/adr-011-platform-editor-excel-roundtrip.md) and [21-Platform-Editor.md](../../Game-Requirements/requirements/21-Platform-Editor.md) §Phasing:

- **Phase B sheets:** `Mobility`, `Signatures`, `Emcon`, damage-model columns
- **Phase B gates:** validation rule pack + sim consumption tests (docs 15/16/19)
- **Canonical IDs immutable** across phases (DBI-7.3)

---

## Must-have stories (critical path)

### S23-D01 — `ApproveBatch` multi-entity commit path

| Field | Value |
|-------|-------|
| **Estimate** | 4 days |
| **Owner** | team-data / c-sharp-engineer |
| **Dependencies** | Sprint 22 merged @ `7253381`; migration `007` applied; retro pre-work: commit + rebase on `main` |
| **Req trace** | DBI-1.1, DBI-1.4, DBI-4.1, DBI-7.2, PLE-3.1–3.5 |
| **GitNexus impact-check** | `CatalogWriteGate`, `IWriteGate`, `ApproveBatch`, `LoadStagingRows`, `DeleteStagingRows`, `CmoMarkdownImportProposer`, `PlatformWorkbookImporter`, `SqliteCatalogReader`, `ICatalogReader` |

**Problem:** `ApproveBatch` today loads only `catalog_staging_sensor` and upserts sensor rows. `ProposePlatformBatch`, `ProposeWeaponBatch`, `ProposeMountBatch`, `ProposeLoadoutBatch`, `ProposeMagazineBatch`, and `ProposeCommsBatch` stage correctly but **cannot commit** — blocking CmoMarkdown E2E and platform workbook approve flows.

**Approach (extend-only):**

1. Replace sensor-only `LoadStagingRows` with entity-aware staging probe (no `entity_type` column on `catalog_staging_batch` — detect populated staging tables per `batch_id`).
2. Add upsert paths: `platform` (+ extended columns), `weapon_catalog`, `platform_mount`, `platform_loadout`, `platform_magazine`, `platform_comms`.
3. Run existing validation chain before commit (mirror sensor path; enforce DBI-2.4 bulk threshold via `CatalogValidationDefaults`).
4. Append `catalog_change_log` rows per committed field; record snapshot via `DbSnapshotStore.RecordApprovedImport`.
5. Preserve existing sensor `ApproveBatch` behavior — **zero regression** on S18/S22 sensor tests.

**Acceptance criteria:**

- [ ] `ApproveBatch` commits staged rows from `catalog_staging_platform`, `_weapon`, `_mount`, `_loadout`, `_magazine`, `_comms` into live tables.
- [ ] `RejectBatch` purges all staging tables for the batch (DBI-1.4 / DBI-4.4) — existing behavior retained.
- [ ] CmoMarkdown import: propose platform+weapon+mount → approve → `ICatalogReader` reflects new rows in stable sort order (DBI-1.1).
- [ ] Batch touching > 10 records returns `Committed == false` until explicit human approve (DBI-2.4 / PLE-3.3).
- [ ] No importer or MCP path auto-commits without `ApproveBatch` (PLE-3.1).
- [ ] `gitnexus impact CatalogWriteGate --direction upstream` run and CRITICAL blast radius documented before merge.

**Test file paths:**

- `src/ProjectAegis.Data.Tests/WriteGate/CatalogWriteGatePlatformApproveTests.cs` *(new)*
- `src/ProjectAegis.Data.Tests/WriteGate/CatalogWriteGateTests.cs` *(regression — sensor path)*
- `src/ProjectAegis.Data.Tests/Import/CmoMarkdownImporterTests.cs` *(extend: approve → read-back)*
- `src/ProjectAegis.Data.Tests/Snapshots/DbSnapshotBindingTests.cs` *(post-commit snapshot)*

---

### S23-D02 — ClosedXML adapter integration + binary `.xlsx` round-trip

| Field | Value |
|-------|-------|
| **Estimate** | 3 days |
| **Owner** | team-data |
| **Dependencies** | S23-D01 not blocking (parallel-safe); `dotnet restore` for ClosedXML 0.104.2 |
| **Req trace** | PLE-1.1, PLE-2.1, PLE-6.1, ADR-011 §1 (Excel model), ADR-006 (engine-free boundary) |
| **GitNexus impact-check** | `ClosedXmlPlatformWorkbookIo`, `IPlatformWorkbookIo`, `CanonicalTextWorkbookIo`, `PlatformWorkbookExporter`, `PlatformWorkbookImporter`, `PlatformExportXlsxCommand`, `PlatformImportXlsxCommand` |

**Problem:** Production adapter exists but is outside the solution; CLI and tests use `CanonicalTextWorkbookIo` only. Req 21 long-term Excel path requires real `.xlsx` fidelity.

**Approach:**

1. Add `src/ProjectAegis.Data.Excel/ProjectAegis.Data.Excel.csproj` to `ProjectAegis.sln`.
2. Add `ProjectAegis.Data.Excel.Tests` (or integration tests in `Data.Tests` with conditional package ref) exercising `ClosedXmlPlatformWorkbookIo`.
3. Prove contract parity with canonical golden: export → write `.xlsx` → read → **empty diff** on unedited round-trip (PLE-2.1).
4. Pin text number-format (`@`) to prevent numeric coercion (already in adapter).
5. CLI: add `--io closedxml` or auto-detect `.xlsx` extension on `platform_export_xlsx` / `platform_import_xlsx`.

**Acceptance criteria:**

- [ ] `dotnet build ProjectAegis.sln` includes `ProjectAegis.Data.Excel` without `UnityEngine` leakage (ADR-006).
- [ ] `ClosedXmlPlatformWorkbookIo` round-trip on Baltic fixture produces **empty diff** (same as `PlatformWorkbookRoundTripTests`).
- [ ] `_Meta.SourceSnapshotId` and `WorkbookHash` survive binary write/read (PLE-1.1).
- [ ] `platform_export_xlsx --out /tmp/test.xlsx` emits valid `.xlsx` (PLE-6.1).
- [ ] `McpToolsManifestTests` updated if CLI surface changes.

**Test file paths:**

- `src/ProjectAegis.Data.Excel.Tests/ClosedXmlRoundTripTests.cs` *(new project)*
- `src/ProjectAegis.Data.Tests/Platform/PlatformWorkbookRoundTripTests.cs` *(shared fixture data)*
- `src/ProjectAegis.MissionEditor.Cli.Tests/McpToolsManifestTests.cs`

---

### S23-D03 — Canonical sort-key determinism on `Catalog*` types

| Field | Value |
|-------|-------|
| **Estimate** | 1.5 days |
| **Owner** | team-data |
| **Dependencies** | None (parallel with D01/D02) |
| **Req trace** | DBI-1.1, DBI-1.2, DBI-7.3, PLE-1.3 |
| **GitNexus impact-check** | `CatalogPlatformBinding`, `CatalogWeaponRecord`, `CatalogMount`, `CatalogLoadout`, `CatalogMagazineEntry`, `CatalogCommsBinding`, `CatalogWriteGate` (Propose* `OrderBy` paths) |

**Problem:** Sprint 22 QA flagged determinism gap — new catalog record types stage with `OrderBy` on composite keys but lack golden tests proving stable ordering across propose, export, and read APIs.

**Approach:**

1. Add `CatalogSortKeyComparer` helpers (or document canonical key tuples per type).
2. Golden tests: same fixture proposed twice → identical `batch_id` row order and export row order.
3. Verify `CmoMarkdownImporter` and `PlatformWorkbookExporter` emit rows in same order as `GetSorted*` reader methods.
4. Document that `PlatformId` / `WeaponId` / `MountId` are the canonical immutable IDs (DBI-7.3) — renames are alias ops, not key rewrites.

**Acceptance criteria:**

- [ ] Propose* batch insert order is stable for all 7 entity types (DBI-1.2).
- [ ] Export sort keys match reader sort keys (PLE-1.3).
- [ ] Re-import of unedited export yields empty diff (cross-check with D02).
- [ ] No `DateTime.Now` in commit or export paths (`ICatalogClock` only).

**Test file paths:**

- `src/ProjectAegis.Data.Tests/Catalog/CatalogSortKeyDeterminismTests.cs` *(new)*
- `src/ProjectAegis.Data.Tests/Platform/PlatformWorkbookRoundTripTests.cs`
- `src/ProjectAegis.Data.Tests/Import/CmoMarkdownImporterTests.cs`

---

## Should-have stories

### S23-D04 — Phase B schema migration + exporter sheet stubs

| Field | Value |
|-------|-------|
| **Estimate** | 2 days |
| **Owner** | team-data |
| **Dependencies** | S23-D01 (live tables pattern); ADR-011 Phase B scope locked |
| **Req trace** | Req 21 §Mobility/Signatures/Emcon sheets; ADR-011 Phase B |
| **GitNexus impact-check** | `PlatformWorkbookExporter`, `PlatformWorkbookImporter`, `PlatformWorkbookValidator`, `SqliteCatalogReader`, `ICatalogReader` |

**Scope:** Additive migration `008_platform_editor_phase_b.sql` with `platform_mobility`, `platform_signature`, `platform_emcon` tables + staging tables. Exporter emits empty/stub sheets with headers and enum validation metadata; importer stages changes but sim wiring deferred.

**Acceptance criteria:**

- [ ] Migration `008` idempotent via `ShouldSkipMigration` guard.
- [ ] Exporter includes `Mobility`, `Signatures`, `Emcon` sheets with stable column headers per Req 21.
- [ ] Unedited Phase B stub sheets do not produce spurious diff entries.
- [ ] `gitnexus impact ICatalogReader` before any reader API extension.

**Test file paths:**

- `src/ProjectAegis.Data.Tests/Platform/PlatformWorkbookPhaseBSheetTests.cs` *(new)*
- `assets/data/catalog/migrations/008_platform_editor_phase_b.sql` *(new)*

---

### S23-D05 — Wire `BalanceTelemetrySinkFactory` to batch runner (advisory)

| Field | Value |
|-------|-------|
| **Estimate** | 1.5 days |
| **Owner** | c-sharp-engineer |
| **Dependencies** | S23-D01 (meaningful post-commit entity IDs); S22-06 shipped |
| **Req trace** | DBI-5.1–5.3 (advisory only) |
| **GitNexus impact-check** | `BalanceTelemetrySinkFactory`, `BalanceTelemetryAccumulator`, `DatabaseIntelligenceOrchestrator`, `CatalogIntelligenceRunCommand` |

**Approach:** Inject optional `IBalanceTelemetrySink` into `DatabaseIntelligenceOrchestrator` / CLI `catalog_intelligence_run`; on successful `ApproveBatch`, record entity outcomes from batch summary. Feature flag `enableBalanceDrift` defaults **false**; ±8% findings never bypass write gate.

**Acceptance criteria:**

- [ ] Flag off → `NoOpBalanceTelemetrySink` (no behavior change).
- [ ] Flag on → accumulator receives post-approve entity events; `EvaluateDrift()` returns deterministic hash.
- [ ] No write-gate bypass from telemetry path (DBI-5.2).

**Test file paths:**

- `src/ProjectAegis.Data.Tests/Telemetry/BalanceTelemetrySinkFactoryTests.cs`
- `src/ProjectAegis.Data.Tests/Telemetry/BalanceTelemetryAccumulatorTests.cs`
- `src/ProjectAegis.Data.Tests/Agents/DatabaseIntelligenceOrchestratorTests.cs` *(extend)*

---

### S23-D06 — Platform workbook binary verification harness

| Field | Value |
|-------|-------|
| **Estimate** | 1 day |
| **Owner** | team-data |
| **Dependencies** | S23-D02 |
| **Req trace** | PLE-2.1, PLE-4.3 (validation hash stability on binary I/O) |
| **GitNexus impact-check** | `PlatformWorkbookValidator`, `PlatformWorkbookHash`, `ClosedXmlPlatformWorkbookIo` |

**Scope:** Golden-file test: export Baltic fixture → `.xlsx` → re-import → assert zero staged changes + stable `ValidationReport` hash. Catches locale/number-format regressions ClosedXML may introduce.

**Acceptance criteria:**

- [ ] Golden hash stable across two CI runs on same fixture.
- [ ] Edited cell produces deterministic diff category (add/edit/delete).

**Test file paths:**

- `src/ProjectAegis.Data.Excel.Tests/PlatformWorkbookBinaryGoldenTests.cs` *(new)*
- `src/ProjectAegis.Data.Tests/Platform/PlatformWorkbookValidatorTests.cs`

---

## Nice-to-have stories

### S23-D07 — Excel UX: data-validation dropdowns + sheet protection

| Field | Value |
|-------|-------|
| **Estimate** | 2 days |
| **Owner** | team-data |
| **Dependencies** | S23-D02 |
| **Req trace** | PLE-1.2, Req 21 OQ5 (_Meta + PK column protection) |
| **GitNexus impact-check** | `ClosedXmlPlatformWorkbookIo`, `PlatformWorkbookExporter` |

**Scope:** ClosedXML data-validation lists for enum columns (domain, mount type, link type, review state, value tier); protect `_Meta` sheet and PK columns.

**Test file paths:**

- `src/ProjectAegis.Data.Excel.Tests/ClosedXmlValidationMetadataTests.cs` *(new)*

---

### S23-D08 — Full-solution `dotnet test` baseline gate

| Field | Value |
|-------|-------|
| **Estimate** | 0.5 days |
| **Owner** | c-sharp-devops-engineer |
| **Dependencies** | Retro action #7; post-rebase `main` |
| **Req trace** | Sprint 22 DoD carryover |

**Scope:** Document and script single-batch `dotnet test ProjectAegis.sln` gate; capture baseline counts in `production/qa/smoke-2026-06-17.md` successor.

**Test file paths:** N/A (infra); references all `*.Tests.csproj` in solution.

---

## Recommended worktree layout

```
main  (rebased post-S22 commit @ 7253381)
 ├── stack/sprint23/approve-batch-commit     (S23-D01 — CRITICAL, solo merge first)
 ├── stack/sprint23/closedxml-roundtrip      (S23-D02 + S23-D06)
 ├── stack/sprint23/canonical-determinism    (S23-D03)
 └── stack/sprint23/phase-b-schema           (S23-D04)
```

**Merge order:** D01 → D03 → D02/D06 → D04 → D05. D01 blocks E2E verification for CmoMarkdown and platform import approve; land before parallel ClosedXML work rebases.

---

## Quality gate commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone

# Build (includes Data.Excel after S23-D02)
dotnet build ProjectAegis.sln -v q

# Data layer — write gate + platform + import + determinism
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "FullyQualifiedName~WriteGate|FullyQualifiedName~Platform|FullyQualifiedName~CmoMarkdown|FullyQualifiedName~CatalogSortKey" -v q

# ClosedXML integration (after S23-D02)
dotnet test src/ProjectAegis.Data.Excel.Tests/ProjectAegis.Data.Excel.Tests.csproj -v q

# CLI / MCP parity
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj -v q

# Telemetry advisory path (after S23-D05)
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "FullyQualifiedName~Telemetry|FullyQualifiedName~DatabaseIntelligence" -v q

# Optional full gate (S23-D08)
dotnet test ProjectAegis.sln -v q

# CLI smoke — binary export
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  platform_export_xlsx --out /tmp/s23-smoke.xlsx --snapshot baltic_patrol
```

**Pre-merge GitNexus (mandatory):**

```
gitnexus impact CatalogWriteGate --direction upstream
gitnexus impact IPlatformWorkbookIo --direction upstream
gitnexus_detect_changes()
```

---

## Risks

| # | Risk | Probability | Impact | Mitigation |
|---|------|-------------|--------|------------|
| **R1** | **`CatalogWriteGate` CRITICAL extend-only violation** — `ApproveBatch` refactor breaks sensor commit or changes `IWriteGate` signatures | Medium | **CRITICAL** | Impact-check before edit; preserve sensor code path verbatim; regression-run `CatalogWriteGateTests` first; no signature changes to existing `ProposeSensorBatch` |
| **R2** | **Multi-table staging detection complexity** — `catalog_staging_batch` has no `entity_type`; mixed batches or empty-table probes could mis-route | Medium | High | Probe staging tables by `batch_id` count; unit-test each entity type in isolation; reject ambiguous batches with explainable error |
| **R3** | **ClosedXML build/CI friction** — package not in solution today; native restore failures | Medium | Medium | Land `Data.Excel` project early in sprint; pin ClosedXML 0.104.2; keep `CanonicalTextWorkbookIo` as fallback behind port |
| **R4** | **Locale/number coercion in `.xlsx`** — Excel converts IDs like `057` to `57`, breaking empty-diff golden | Medium | High | Text format `@` on all columns (already in adapter); binary golden test in S23-D06 |
| **R5** | **Phase B / `ICatalogReader` blast radius** — mobility/signature reads touch `ProjectAegis.Sim` consumers | Low | High | Schema + export stubs only in S23-D04; defer reader API widening until `gitnexus impact ICatalogReader` review |
| **R6** | **Sprint 22 rebase conflicts** — retro notes 8 commits behind `origin/main` with doctrine overlap | Medium | Medium | Complete retro action #1–2 before Sprint 23 implementation; re-run quality gates post-rebase |

### Top 3 risks (executive)

1. **R1 — CatalogWriteGate CRITICAL extend-only** (sensor regression on ApproveBatch refactor)
2. **R2 — Multi-entity staging commit routing** (no `entity_type` discriminator on batch header)
3. **R4 — ClosedXML determinism / numeric coercion** (empty-diff contract breakage)

---

## Capacity estimate

| Tier | Stories | Days |
|------|---------|------|
| Must-have | D01 + D02 + D03 | **8.5** |
| Should-have | D04 + D05 + D06 | **4.5** |
| Nice-to-have | D07 + D08 | **2.5** |

**Recommendation:** Commit to must-have (8.5d) within a 10-day sprint with 20% buffer. Should-have D04 + D06 if D01/D02 land by mid-sprint. Defer D07 to Sprint 24 unless ClosedXML lands early.

---

## References

- Retro: `production/retrospectives/retro-sprint-22-2026-06-17.md` (action items #4, #5, #7; tech debt table)
- ADR: `docs/architecture/adr-011-platform-editor-excel-roundtrip.md`
- Req 06: `Game-Requirements/requirements/06-Database-Intelligence.md`
- Req 21: `Game-Requirements/requirements/21-Platform-Editor.md`
- Sprint 22 gates: `production/agentic/sprint-22-commit-plan-2026-06-17.md`
- Implementation tracker: `Game-Requirements/implementation-tracker-2026-06-04.md` (row 21)

*Generated by Sprint 23 Data/Platform Planning agent — 2026-06-17.*