# Sprint 23 — Platform Editor Phase B I/O + Doctrine Polish + Full-Solution Gate

**Dates:** 2026-07-08 → 2026-07-22  
**Trunk:** `main` @ `7253381`  
**Predecessor:** Sprint 22 — Platform Editor Phase A + DB Intelligence P1 + Doctrine Panel (complete)

## Sprint Goal

Close Sprint 22 deferred quality gates by wiring ClosedXML binary `.xlsx` round-trip I/O (Req 21), establishing a green full-solution `ProjectAegis.sln` test baseline, and delivering Unity Editor visual sign-off for the Doctrine Inheritance Panel (Req 13) toward MVP polish.

## Capacity

| Metric | Value |
|--------|-------|
| Total days | 10 |
| Buffer (20%) | 2 days reserved for unplanned work |
| **Available (effective dev-days)** | **8** |

## Tasks

### Must Have (Critical Path)

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|-------------|-------------------|
| S23-01 | **ClosedXML `.xlsx` adapter** — wire `ClosedXmlPlatformWorkbookIo` into `platform_export_xlsx` / `platform_import_xlsx` CLI paths (flag or default when ClosedXML restored); add integration test proving empty-diff golden on binary `.xlsx`; parity with `CanonicalTextWorkbookIo` round-trip contract (PLE-2.1) | c-sharp-engineer / team-data | 2.5 | S22 complete; `src/ProjectAegis.Data.Excel/ClosedXmlPlatformWorkbookIo.cs` exists | CLI export produces real `.xlsx` (not canonical text); import round-trip yields empty diff on unedited workbook; `ClosedXmlPlatformWorkbookIo` integration test PASS; `Platform\|WriteGate` scoped tests green; GitNexus impact on `IPlatformWorkbookIo` checked |
| S23-02 | **Full-solution test gate** — run `dotnet build` + `dotnet test ProjectAegis.sln` @ `7253381`; triage/fix failures; record baseline count in `sprint-status.yaml` and `production/qa/smoke-sprint-23-*.md` | c-sharp-devops-engineer | 1 | S22 pushed to `origin/main` | `dotnet build ProjectAegis.sln` — 0 errors; `dotnet test ProjectAegis.sln -v minimal` — 0 failures; evidence doc with test count + indexed commit |
| S23-03 | **Unity Doctrine Inheritance Panel Editor visual sign-off** (Req 13) — `DoctrineInheritancePanelHost` PlayMode batch + manual evidence; WRA/ROE/EMCON fields visible and bound; `SetDoctrineOverride` dispatch verified in Editor; ZERO touch `DelegationBridge` | c-sharp-engineer / team-unity | 2.5 | S22-05 headless proxy done; ADR-010 accepted | PlayMode smoke PASS (doctrine row + harness); manual evidence at `production/qa/sprint-23-doctrine-editor-signoff-*.md`; inheritance order explainable; grep confirms zero `DelegationBridge.cs` edits |

### Should Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|-------------|-------------------|
| S23-04 | **`ApproveBatch` multi-entity commit path** — extend `CatalogWriteGate.LoadStagingRows` + `ApproveBatch` beyond sensor-only to commit staged platform/weapon/mount/loadout/magazine/comms rows; DBI-1.4 orphan guard on reject | c-sharp-engineer / team-data | 2.5 | S22-01/22-04 staging tables exist | `ProposePlatformBatch` → `ApproveBatch` commits to live tables; CmoMarkdown platform import E2E test PASS; `WriteGate` filter green; extend-only on `CatalogWriteGate` (GitNexus CRITICAL) |
| S23-05 | **Phase B schema foundation spike** — migration stub + `CatalogSignature`/`CatalogMobility`/`CatalogEmcon` types + exporter sheet hooks for Signatures/Mobility/EMCON (read-only export; import deferred) | c-sharp-engineer / team-data | 2 | S23-01 I/O port stable | Migration applies cleanly; types compile; exporter emits empty Phase B sheets with headers; tracker row 21 updated |

### Nice to Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|-------------|-------------------|
| S23-06 | **CanonicalId determinism** on new `Catalog*` types (mount/loadout/magazine/comms/platform/weapon) — stable `OrderBy` composite keys per DBI-7.3 | c-sharp-engineer | 1 | S23-04 or parallel | Determinism test PASS; golden hash pinned |
| S23-07 | **GitNexus re-index** @ `7253381` + `detect_changes` baseline for sprint closeout | c-sharp-devops-engineer | 0.5 | S23-02 baseline green | `production/qa/sprint-23-gitnexus-*.md` with node/edge counts |

## Carryover from Sprint 22 Deferrals

| Item | Sprint 22 ID | Reason deferred | S23 placement |
|------|--------------|-----------------|---------------|
| ClosedXML `.xlsx` binary adapter | 22-2 / sign-off C2 | Phase A used `CanonicalTextWorkbookIo` per ADR-011 interim boundary | **S23-01 must-have** |
| Full `dotnet test ProjectAegis.sln` closeout | kickoff DoD | Lean QA used scoped filters only | **S23-02 must-have** |
| Unity Editor `DoctrineInheritancePanelHost` visual | 22-5 | Headless proxy sufficient for sprint closeout | **S23-03 must-have** |
| `ApproveBatch` commit for platform/weapon staged rows | 22-4 | Staging verified; `LoadStagingRows` sensor-only | **S23-04 should-have** |
| Phase B signatures/mobility/EMCON schema | ADR-011 / tracker row 21 | Phase A scope lock | **S23-05 should-have (spike)** |
| CanonicalId on new Catalog types | qa-plan note | Determinism gap noted pre-impl | **S23-06 nice-to-have** |

> **Must-have trade-off (per program constraint):** ClosedXML `.xlsx` I/O chosen over `ApproveBatch` for must-have. `ApproveBatch` multi-entity path is should-have — defer cleanly to Sprint 24 if S23-01/03 run long.

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| `CatalogWriteGate.ApproveBatch` CRITICAL blast radius on S23-04 | High | High | `gitnexus impact CatalogWriteGate direction=upstream` before edit; extend `LoadStagingRows` only; sensor path regression tests mandatory |
| ClosedXML NuGet restore / CI environment | Medium | Medium | Verify `dotnet restore` on `ProjectAegis.Data.Excel`; fallback flag keeps `CanonicalTextWorkbookIo` for headless CI |
| Full-solution test reveals latent failures post-S22 merge | Medium | High | S23-02 scheduled day 1; block feature work until baseline recorded or failures triaged |
| Doctrine panel Editor sign-off needs local Unity 6.3 | Medium | Medium | Document headless proxy as CI fallback; manual evidence required for Production → Polish gate |
| Phase B schema scope creep (full signatures/mobility/EMCON import) | High | Medium | S23-05 is export-stub spike only; import + validation deferred to Sprint 24 |
| S23-04 deferred if must-haves consume buffer | Medium | Low | ApproveBatch is should-have; staging-only path from S22 remains valid |

## Dependencies on External Factors

- Sprint 22 stack merged and pushed to `origin/main` @ `7253381` (done 2026-06-17)
- ADR-011 **Accepted** (2026-06-17) — no architecture waiver needed for ClosedXML wiring
- Unity Editor 6.3 LTS available locally for S23-03 manual sign-off
- MVP milestone review (2026-07-15) may overlap sprint week 1 — prioritize S23-02 baseline early

## GitNexus Rules (Mandatory)

- **Before ANY symbol edit:** run `gitnexus impact` upstream on target symbol; report callers/processes/risk
- **CRITICAL (extend-only):** `CatalogWriteGate` — only extend `LoadStagingRows`/`ApproveBatch`; do not change existing sensor commit behavior
- **ZERO touch:** `DelegationBridge` (CRITICAL, 77 upstream)
- **HIGH:** `IPlatformWorkbookIo`, `PlatformWorkbookImporter`, `PlatformWorkbookExporter`
- After edits: `npx gitnexus detect_changes --repo cmano-clone` before commit
- Determinism: stable `OrderBy` on CanonicalId for all batch proposals and sheet export ordering

## Parallel Worktree Layout (Suggested)

```
main
 ├── stack/sprint23/closedxml-xlsx-io      (S23-01)
 ├── stack/sprint23/full-sln-gate          (S23-02 — day 1)
 ├── stack/sprint23/doctrine-editor-visual (S23-03)
 └── stack/sprint23/approve-batch-multi    (S23-04)
```

S23-02 runs on `main` day 1. S23-01 and S23-03 can parallel after baseline. S23-04 depends on S23-02 green.

## Quality Gates

```powershell
# Must — full solution (S23-02 establishes baseline; re-run at closeout)
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal

# Scoped — per-story verification
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter "Platform|WriteGate|ClosedXml"
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj --filter "Mcp|Platform"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "Doctrine|PlayModeSmoke"
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj --filter "Doctrine"

# CLI smoke — binary xlsx (S23-01)
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- platform_export_xlsx --snapshot <id> --output /tmp/test.xlsx
# verify file is ZIP/OLE xlsx, not canonical text

# Unity Editor (S23-03) — local only
# Invoke-C2PlayModeSignoffBatch.ps1 or doctrine-specific PlayMode harness
```

## Definition of Done

- [ ] All Must Have tasks completed (S23-01, S23-02, S23-03)
- [ ] All tasks pass acceptance criteria
- [ ] QA plan exists (`production/qa/qa-plan-sprint-23-2026-07-08.md`) — run `/qa-plan sprint` before last story
- [ ] Full-solution gate green (`dotnet test ProjectAegis.sln` 0 failures)
- [ ] Scoped story filters green per Quality Gates above
- [ ] Smoke check passed (`/smoke-check sprint`)
- [ ] QA sign-off report: APPROVED or APPROVED WITH CONDITIONS (`/team-qa sprint`)
- [ ] No S1 or S2 bugs in delivered features
- [ ] Tracker rows 13 + 21 updated for doctrine visual + xlsx I/O
- [ ] Code reviewed and merged

## References

- Req 21 Platform Editor: `Game-Requirements/requirements/21-Platform-Editor.md`
- Req 06 Database Intelligence: `Game-Requirements/requirements/06-Database-Intelligence.md`
- Req 13 Doctrine ROE: `Game-Requirements/requirements/13-Doctrine-ROE-EMCON-WRA.md`
- ADR-010: `docs/architecture/adr-010-headless-first-command-driven-ui.md`
- ADR-011: `docs/architecture/adr-011-platform-editor-excel-roundtrip.md` (Accepted 2026-06-17)
- ClosedXML adapter: `src/ProjectAegis.Data.Excel/ClosedXmlPlatformWorkbookIo.cs`
- Canonical text I/O: `src/ProjectAegis.Data/Platform/CanonicalTextWorkbookIo.cs`
- Write-gate: `src/ProjectAegis.Data/WriteGate/CatalogWriteGate.cs`
- Doctrine panel: `unity/ProjectAegis/Assets/Scripts/Runtime/DoctrineInheritancePanelHost.cs`
- Sprint 22 retro: `production/retrospectives/retro-sprint-22-2026-06-17.md`
- Sprint 22 sign-off conditions: `production/agentic/sprint-22-pr-description-2026-06-17.md` (C2–C4)
- Tracker: `Game-Requirements/implementation-tracker-2026-06-04.md` (rows 13, 21)
- MVP milestone: `production/milestones/vertical-slice-mvp.md`
- Implementation plan: `docs/superpowers/plans/sprint-23-implementation.md`

*Created by Sprint 23 Program Orchestrator — 2026-06-17. Lean review mode. Stories inline (no separate epic files). QA plan required before implementation begins.*

## Pre-Work (Mandatory Before Any Implementation)

1. `npx gitnexus analyze .` — refresh index @ `7253381`
2. Impact analysis: `CatalogWriteGate` (CRITICAL), `IPlatformWorkbookIo`, `PlatformWorkbookImporter`, `DoctrineInheritancePanelHost`, `DelegationBridge` (ZERO touch)
3. Baseline gate: `dotnet build + test ProjectAegis.sln` (S23-02 day-1)
4. Read Sprint 22 retro + sign-off conditions C2–C4
5. Read this kickoff + `docs/superpowers/plans/sprint-23-implementation.md` before coding

## QA Test Cases (inline — full plan via `/qa-plan sprint`)

### S23-01 ClosedXML `.xlsx` adapter — Integration
**Automated:**
- `ClosedXmlPlatformWorkbookIo` round-trip empty-diff golden (PLE-2.1)
- CLI `platform_export_xlsx` emits binary `.xlsx`; import unedited → zero staged changes
- Parity: same logical diff whether via `CanonicalTextWorkbookIo` or `ClosedXmlPlatformWorkbookIo`
- Text column format `@` prevents numeric coercion (existing adapter contract)
**Edge cases:** empty workbook, sheet name sanitization, large Baltic fixture
**Test path:** `src/ProjectAegis.Data.Tests/Platform/ClosedXmlPlatformWorkbookIoTests.cs` (new); extend `PlatformWorkbookRoundTripTests`

### S23-02 Full-solution test gate — Config/CI
**Automated:**
- `dotnet build ProjectAegis.sln` — 0 errors
- `dotnet test ProjectAegis.sln` — 0 failures; count recorded
**Evidence:** `production/qa/smoke-sprint-23-*.md`

### S23-03 Doctrine Editor visual — UI + Integration
**Automated:** PlayMode smoke + headless command round-trip (regression)
**Manual:** Panel screenshot/video; WRA/ROE/EMCON visible; inheritance chain; ZERO touch `DelegationBridge`
**Evidence:** `production/qa/sprint-23-doctrine-editor-signoff-*.md`

### S23-04 ApproveBatch multi-entity — Integration
**Automated:**
- `ProposePlatformBatch` → `ApproveBatch` → live table readback
- Mount/loadout/magazine/comms commit paths
- `RejectBatch` purges all staging tables (DBI-1.4)
- Sensor path regression unchanged
**Test path:** extend `src/ProjectAegis.Data.Tests/WriteGate/CatalogWriteGateTests.cs`

### S23-05 Phase B schema spike — Integration (export only)
**Automated:** migration applies; exporter emits Signatures/Mobility/EMCON sheet headers
**Deferred:** import, validation, sim consumer wiring