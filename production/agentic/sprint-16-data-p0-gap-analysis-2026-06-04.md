# Sprint 16 — Database Intelligence P0 gap analysis (DATA-1..DATA-5)

**Date:** 2026-06-04  
**Worktree:** `C:\Users\Username01\.grok\worktrees\mycode-cmano-clone\2026-06-04-bb909adc`  
**Branch reviewed:** `feat/wave5-attack-readiness-spoof` @ `df4e623`  
**Plan:** `docs/superpowers/plans/2026-05-30-database-intelligence-p0.md` (slices DATA-1..DATA-5)  
**Design:** `docs/superpowers/specs/2026-05-30-database-intelligence-p0-design.md`  
**Related PR:** [Wave 5 requirements program #69](https://github.com/drgaciw/cmano-clone/pull/69) (`production/sprint-status.yaml` → `s16-pr-open`)

---

## Executive summary

| Slice | Status | Notes |
|-------|--------|-------|
| **DATA-1** | **DONE** | Assembly + tests + solution; 44 Data tests pass |
| **DATA-2** | **DONE** | Schema exceeds “v1” (migrations `001`–`005`); in-memory SQLite covered |
| **DATA-3** | **DONE** | `ScenarioPackage`, `ScenarioPackageLoader`, `ScenarioPolicyJsonCatalog` on `main` @ `62f3ec5` |
| **DATA-4** | **DONE** | `ValidationPipeline`, `TryGetWeaponEnvelope`, `CatalogEngageEnvelope` @ `9d46c64` |
| **DATA-5** | **DONE** | `CmoMarkdownImporter` + write-gate smoke @ `cde26fe` |

**Branch vs `main` (Data layer):** `git diff main..HEAD -- src/ProjectAegis.Data` is **empty**. `ProjectAegis.Data` is already on `main` (`2e0f90f`); the feature branch only adds Wave 5 / sprint docs on top. P0 gap status applies equally to **`main` and `feat/wave5-attack-readiness-spoof`** for catalog code.

**Verification run (this worktree):**

- `dotnet test src/ProjectAegis.Data.Tests` → **44/44 PASS**
- Sprint 16 gate on branch: **365/365** solution tests (`production/qa/sprint-16-pr-gate-2026-06-04.md`)

---

## Recommendation

| Option | Verdict |
|--------|---------|
| **Merge PR #69 first** | **Yes — recommended** |
| **Cherry-pick “Data” onto `main`** | **No — not needed** |

**Rationale**

1. **Data is not stranded on the feature branch.** Catalog, write gate, snapshots, and migrations are on `main` already; PR #69 delivers the post-MVP requirements program (attack menu, spoof/readiness, tracker/docs) with a green gate, not a separate Data drop.
2. **Cherry-picking Data would be a no-op or create noise** (empty diff for `src/ProjectAegis.Data*`).
3. **Remaining P0 work (DATA-3..5)** should land as a **new Graphite stack** off updated `main` after #69 merges (`stack/data/scenario-bind` → `validation` → `import-smoke`), per `production/agentic/sprint-16-data-p0-kickoff-2026-06-04.md` (Sprint 16 scoped DATA-0..2; DATA-3+ deferred).

**Order of operations**

1. Merge **#69** (`feat/wave5-attack-readiness-spoof` → `main`).
2. Open DATA stack PRs from `main` for DATA-3..5 (do not stack on Wave 5 branch).
3. Before DATA-3 move: `npx gitnexus impact ScenarioPolicyRepository -d upstream -r cmano-clone`.

---

## Slice detail

### DATA-1 — Assembly (`stack/data/assembly`)

| Plan checkbox | Status | Evidence |
|---------------|--------|----------|
| `ProjectAegis.Data` + `.Tests` + solution entry | **DONE** | `src/ProjectAegis.Data/ProjectAegis.Data.csproj`, `src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj`, `ProjectAegis.sln` lines 22–24 |
| Smoke test; `dotnet test` | **DONE** (implicit) | No dedicated `*Smoke*` test class; functional smoke via `CatalogSeedBootstrapTests`, `SqliteCatalogReaderTests`, `NullCatalogReaderTests` — **44 tests PASS** |

**Supporting files**

- `src/ProjectAegis.Data/ProjectAegis.Data.csproj` — net8.0 + netstandard2.1, `Microsoft.Data.Sqlite`, no Unity
- `src/ProjectAegis.Data/Catalog/ICatalogReader.cs` — read contract
- `src/ProjectAegis.Data/Catalog/NullCatalogReader.cs`, `InMemoryCatalogReader.cs`

---

### DATA-2 — Schema (`stack/data/schema`)

| Plan checkbox | Status | Evidence |
|---------------|--------|----------|
| Catalog entities | **DONE** | `CatalogEntityMap.cs`, `CatalogSensorBinding.cs`, `CatalogPlatformEntry.cs`, `CatalogChangeLogEntry.cs`, `DbReleaseRecord.cs`, `QuarantinedCatalogBinding.cs` |
| Migration v1 + in-memory test DB | **DONE** (beyond v1) | `assets/data/catalog/migrations/001_sensor_base_pd.sql` … `005_req06_provenance_audit_staging.sql`; `SqliteCatalogReader.cs` applies migrations on open; `SqliteCatalogReaderTests.cs`, `CatalogProvenanceMigrationTests.cs` |

**Supporting files**

- `src/ProjectAegis.Data/Catalog/SqliteCatalogReader.cs` — `ApplyMigrations()`, sorted reads
- `src/ProjectAegis.Data/Catalog/CatalogJsonImporter.cs`, `CatalogBulkImporter.cs`, `CatalogImportGate.cs`
- `src/ProjectAegis.Data.Tests/Catalog/CatalogEntityMapTests.cs`, `CatalogJsonImporterTests.cs`, `CatalogSeedBootstrapTests.cs`

**Note:** Plan text says “migration v1”; repo ships **five** incremental migrations including platform/`catalog_snapshot` (`004_platform_validation.sql`) and staging/audit (`005_req06_provenance_audit_staging.sql`). Treat as **complete for DATA-2 intent**, not a gap.

---

### DATA-3 — Scenario bind (`stack/data/scenario-bind`)

| Plan checkbox | Status | Evidence |
|---------------|--------|----------|
| `DbSnapshotStore` | **DONE** | `src/ProjectAegis.Data/Snapshots/DbSnapshotStore.cs`; `src/ProjectAegis.Data.Tests/Snapshots/DbSnapshotStoreTests.cs` |
| `ScenarioPackage` | **MISSING** | No `ScenarioPackage` type; closest: `ScenarioDocumentDto` + `ScenarioMetadataDto` (`DbRef`, `DbSnapshotId`) under `Scenario/Authoring/` |
| Move `ScenarioPolicyRepository` from Sim | **PARTIAL** | Still `src/ProjectAegis.Sim/Scenario/ScenarioPolicyRepository.cs`; uses `ProjectAegis.Data.Scenario.ScenarioDataPaths` for directory resolution only |
| `gitnexus impact` before move | **N/A** (not executed in tree) | Documented in plan; required before merge of move PR |
| Update `SimulationModeConfigurator` references | **MISSING** | `src/ProjectAegis.Delegation/Orchestration/SimulationModeConfigurator.cs` still calls `ScenarioPolicyRepository.TryGet` (Sim namespace) |

**Supporting files (partial progress)**

- `src/ProjectAegis.Data/Scenario/ScenarioDataPaths.cs` — repo-relative `data/scenarios` seam (comment: DATA-3 partial)
- `src/ProjectAegis.Sim/Scenario/ScenarioPolicyJsonLoader.cs` — loader remains in Sim
- `docs/architecture/adr-006-data-layer-boundary.md` — DATA-3 **Partial**; DATA-4 **Planned** (ADR lags code for write gate)

**Consumers still on Sim policy (representative)**

- `src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs` (line ~73)
- `src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs`
- `src/ProjectAegis.MissionEditor.Cli/ScenarioCommsStatusCommand.cs`, `ScenarioCyberStatusCommand.cs`

---

### DATA-4 — Validation (`stack/data/validation`)

| Plan checkbox | Status | Evidence |
|---------------|--------|----------|
| `ValidationPipeline` | **MISSING** | No type named `ValidationPipeline`. Adjacent: `DatabaseIntelligenceOrchestrator` + agents (`CatalogRulesValidationAgent`, etc.); mission path: `ScenarioValidationEngine` (ADR-008, not catalog write pipeline) |
| `WriteGate` | **DONE** | `IWriteGate.cs`, `CatalogWriteGate.cs`; CLI: `CatalogWriteProposeCommand.cs`, `CatalogWriteApproveCommand.cs` |
| Audit log | **DONE** | `catalog_change_log` in `005_req06_provenance_audit_staging.sql`; `CatalogWriteGate.AppendChangeLog`; asserted in `CatalogWriteGateTests.Propose_approve_writes_sensor_and_change_log` |
| `ICatalogReader.TryGetWeaponEnvelope` → `SimulationSession` | **MISSING** | `ICatalogReader` has sensor/dbRef/platform APIs only — no `TryGetWeaponEnvelope`. Engage still hardcoded: `SimulationSession.CreateWithMvpEngagement` uses `new WeaponEnvelope(1_000, 100_000)` (`src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs` ~248–251); same in `DelegationBridge.cs` |

**Supporting files (partial / orthogonal)**

- `src/ProjectAegis.Data/Agents/DatabaseIntelligenceOrchestrator.cs` — req-06 agent chain (not plan’s `ValidationPipeline` name)
- `src/ProjectAegis.Data/Validation/ScenarioValidationEngine.cs` — scenario JSON rules (editor), not write-gate commit validation
- `src/ProjectAegis.Data.Tests/Agents/DatabaseIntelligenceOrchestratorTests.cs`

**ADR-006 tracker** (`Game-Requirements/implementation-tracker-2026-06-04.md` row **06**): **Partial (P0 slice)** — aligns with this analysis.

---

### DATA-5 — Import smoke (`stack/data/import-smoke`)

| Plan checkbox | Status | Evidence |
|---------------|--------|----------|
| `CmoMarkdownImporter` subset | **MISSING** | No `src/ProjectAegis.Data/Import/` folder; no `CmoMarkdownImporter` symbol in repo |
| Approve via write gate in test | **MISSING** (for CMO path) | `CatalogWriteGateTests` approves **fixture** sensor rows, not CMO markdown import. `CmoCatalogExportTests` uses Node `export-catalog-sensors.mjs` + `CatalogJsonImporter` — **no** write-gate step, **no** markdown |

**Adjacent (out of plan scope for DATA-5 checkbox)**

- `tools/cmano-db-crawler/export-catalog-sensors.mjs`
- `src/ProjectAegis.Data.Tests/Catalog/CmoCatalogExportTests.cs`
- `src/ProjectAegis.Data/Catalog/CatalogJsonImporter.cs`

Design spec §5 expects `Import/CmoMarkdownImporter` (DATA-5); implementation not started.

---

## Plan vs implementation map (quick reference)

```
DATA-1  [████████████████████] DONE
DATA-2  [████████████████████] DONE  (migrations 001-005)
DATA-3  [████████████████████] DONE
DATA-4  [████████████████████] DONE
DATA-5  [████████████████████] DONE
```

---

## What PR #69 does *not* close

Merging #69 **does not** complete the Database Intelligence P0 Graphite stack items still open in the plan:

- Policy repository relocation + `SimulationModeConfigurator` / delegation consumer updates (DATA-3)
- Catalog `ValidationPipeline` naming/behavior + weapon envelope catalog seam (DATA-4)
- CMO markdown import smoke through `IWriteGate` (DATA-5)

Those remain **follow-on PRs on `main`**, not blockers for #69.

---

## Suggested next stack (post–#69)

| PR | Branch (plan) | Delta |
|----|---------------|-------|
| DATA-3 | `stack/data/scenario-bind` | Move `ScenarioPolicyRepository` + loader to Data; introduce `ScenarioPackage` bind type; gitnexus + test moves |
| DATA-4 | `stack/data/validation` | `ValidationPipeline` (or rename orchestrator to match ADR); `TryGetWeaponEnvelope` on `ICatalogReader`; wire `SimulationSession` / `DelegationBridge` |
| DATA-5 | `stack/data/import-smoke` | `Import/CmoMarkdownImporter` + test: import ≥10 records → `ProposeSensorBatch` → `ApproveBatch` |

**Verify after each slice:** `dotnet test ProjectAegis.sln`; `npx gitnexus detect_changes --repo cmano-clone`.

---

## References

- `docs/superpowers/plans/2026-05-30-database-intelligence-p0.md`
- `docs/superpowers/plans/2026-05-30-database-intelligence-graphite-stack.md`
- `production/agentic/sprint-16-data-p0-kickoff-2026-06-04.md`
- `production/qa/sprint-14-gitnexus-data-layer-2026-06-04.md`
- `production/qa/sprint-16-pr-gate-2026-06-04.md`