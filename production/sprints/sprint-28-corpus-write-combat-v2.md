# Sprint 28 — CMO Corpus v2 + Platform Write Path + Combat Phase 2

**Dates:** 2026-09-18 → 2026-10-01  
**Trunk:** `main` @ `a93b55e` (Sprint 27 complete; 741/741 baseline)  
**Predecessor:** Sprint 27 — CMO Corpus Pipeline + Combat Bounded + Phase C Viewer (complete, 16/16)

## Sprint Goal

Extend the **nightly CMO pipeline to platform corpus v2**, land a **bounded in-engine Excel write path** (ADR-011 Phase D) through the extend-only write gate, and advance **ADR-009 combat domains** with surface/subsurface validators and catalog→engage bridges — without full corpus CI load, Baltic golden drift, or hot-tick world mutation.

## Capacity

| Metric | Value |
|--------|-------|
| Total days | 10 |
| Buffer (20%) | 2 days reserved |
| **Effective dev-days** | **8** |

## Tasks

### Must Have (Critical Path)

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S28-01 | **Full-solution re-baseline** — day-1 `dotnet build` + `dotnet test ProjectAegis.sln`; record ≥741 in `sprint-status.yaml` + smoke doc; GitNexus @ trunk | c-sharp-devops-engineer | 1 | S27 merged | 0 errors; ≥741/741 PASS; ReplayGolden 6/6; indexed commit recorded |
| S28-02 | **Nightly CMO corpus v2 — platform slices** — extend `tools/cmo-nightly-import.sh` beyond sensor+weapon v1; chunk 500/batch; propose-only + quarantine JSON; CI stays curated fixtures only | team-data | 2 | S28-01 | Nightly job runs platform slices; evidence `sprint-28-nightly-cmo-import-*.md`; no 7208-record sensor in `dotnet test` |
| S28-03 | **Platform corpus E2E + golden hygiene** — extend import golden tests for v2 nightly output; WriteGate + replay regression unchanged | team-data | 1 | S28-02 | Stable hash on curated platform run; WriteGate regression PASS; ReplayGolden 6/6 |
| S28-04 | **ADR-011 Phase D — in-engine Excel write path** — Unity/CLI hook to stage platform workbook changes via `CatalogWriteGate` (propose→approve); no bypass | team-data + team-unity | 2.5 | S28-01 | Headless write-gate tests PASS; export→edit→propose round-trip on Baltic fixture; GitNexus CRITICAL on `CatalogWriteGate` |

**Sprint fails** if S28-03 platform corpus round-trip does not land through the write gate.

### Should Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S28-05 | **Surface/subsurface domain validators (bounded)** — extend ADR-009 beyond air aspect; flag-gated fixtures only | team-simulation | 1.5 | S28-01 | Sim tests PASS; `combatDomainsEnabled=false` on Baltic; ReplayGolden 6/6; ZERO touch `DelegationBridge` |
| S28-06 | **Live magazine counts from catalog** — Req 16 bridge: catalog loadout/magazine → engage readiness / validation | team-data + team-simulation | 1.5 | S28-03 | Readiness tests PASS; no direct SQLite writes outside write gate |
| S28-07 | **Platform viewer export/diff hook** — read-only export trigger from Phase C viewer; defers import UI to CLI | team-unity | 1 | S28-04 | Headless export path test; no write-gate bypass in viewer host |
| S28-08 | **Damage sim consumer wire (beyond stub)** — connect Phase B damage catalog to readiness/withdraw evaluation | team-simulation | 1.5 | S28-01 | Sim tests PASS; stub path extended; no hot-tick world mutation |
| S28-13 | **Closeout hygiene** — replay 6/6; GitNexus @ stack tip; tracker rows 06/18/21; prune `stack/sprint27/*` | c-sharp-devops-engineer | 0.5 | S28-03+ | Evidence in `sprint-28-gitnexus-*.md`; 741+ closeout |

### Nice to Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S28-09 | **Facility damage projection stub** — order-log/projection-only slice mirroring S27-06 BDA pattern | team-simulation | 1 | S28-05 | Projection tests only; Baltic golden unchanged |
| S28-10 | **Balance drift telemetry consumer** — wire `enableBalanceDrift` advisory from S22-06 | team-simulation | 0.5 | S28-01 | Telemetry tests PASS; default false |
| S28-11 | **TL branching spike (export-only)** — document TL-gated branch DB workflow; no production branching | team-data | 1 | S28-02 | Spike doc PROCEED/DEFER; no runtime branch binding |
| S28-12 | **CI/local gate refresh** — update `verify-ci-local.ps1` evidence for 741+ baseline | c-sharp-devops-engineer | 0.25 | S28-01 | Doc-only; non-blocking |

## Carryover from Sprint 27

| Item | S27 status | S28 placement |
|------|------------|---------------|
| Nightly platform corpus v2 | Deferred (v1 = sensor+weapon only) | **S28-02..03 must-have** |
| In-engine Excel write / platform editor write UI | Out of scope (Phase C read-only) | **S28-04 must-have** |
| Mine/land/facility combat domains | Out of scope (air only) | **S28-05 should-have (surface/subsurface bounded)** |
| Hot-tick world-state damage apply | Out of scope | **S28-08 should-have (readiness wire only)** |
| Live magazine counts from catalog | Tracker Req 16 gap | **S28-06 should-have** |
| TL-0–TL-5 branch databases | Out of scope | **S28-11 nice-to-have spike** |
| Full 7208 sensor in CI | Intentionally nightly-only | **Still out of scope** |
| `combatDomainsEnabled=true` on Baltic | Smoke fixture only | **Still out of scope** |

## Explicitly Out of Scope

- **Full 7208-record `sensor.md` in `dotnet test` CI** (nightly job only)
- **Hot-tick world-state damage apply** / full BDA component model
- **Mine/land/facility combat at full runtime** (bounded validators only)
- **`combatDomainsEnabled=true` on Baltic production fixtures**
- **TL-0–TL-5 production branch databases** (spike doc only)
- **CMO mission/scenario import** (doc 11 Phase 2/3)
- **Full Unity Excel import UI chrome** (write path + CLI authority; ADR-011 Excel-primary)
- **ZERO touch violation** on `DelegationBridge.cs`

## Should-Have Cut Line

| Cut order | Drop | Keep |
|-----------|------|------|
| 1 | S28-08 (damage consumer wire) | S28-05 validators |
| 2 | S28-07 (viewer export hook) | S28-04 Excel write path |
| 3 | S28-06 (live magazines) | S28-03 corpus hygiene |
| 4 | S28-09 (facility stub) | S28-05 surface validator |

**Minimum shippable (beyond must-have):** S28-05 + S28-13.

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| CMO platform corpus scope creep into CI | High | High | Nightly only; producer sign-off on v2 scope; `--max-records` in CI |
| `CatalogWriteGate` extend-only violation | Medium | CRITICAL | `gitnexus impact` before edit; D04 WriteGate regression |
| ADR-009 Baltic golden drift | Medium | CRITICAL | `combatDomainsEnabled=false` default; flag-on in isolated fixtures only |
| Data ↔ Unity overlap on Excel write | Medium | Medium | Land data API first; single owner per file; write-gate grep on viewer |
| TL branching + parallel track overload | Medium | Medium | 20% buffer; cut line before nice-to-have; Graphite stack order |

## GitNexus Rules (Mandatory)

- **CRITICAL extend-only:** `CatalogWriteGate`
- **ZERO touch:** `DelegationBridge.cs`
- **HIGH:** `CmoMarkdownImporter`, `IPlatformWorkbookIo`, `CatalogPlatformBrowseProjection`
- `/replay-verify` mandatory on S28-05..08 sim merges

## Producer Constraints (Inherited from S27)

1. **Nightly corpus** — platform v2 in job; full sensor corpus stays off-CI
2. **GHA billing** — permanent local-gate advisory; Buildkite merge authority
3. **ADR-011 Excel-primary** — write path via workbook + write gate, not raw SQLite
4. **BDA / damage** — projection or readiness only until golden passes with flag-on fixtures

## Quality Gates

```bash
export PATH="/home/username01/.dotnet:$PATH"

# Day-1 (S28-01)
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal

# Data track
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "CmoMarkdown|WriteGate|Platform|CatalogImport|Excel" -v minimal
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "CatalogImport|Platform" -v minimal

# Sim track
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Combat|Domain|Damage|Readiness|Magazine" -v minimal

# Unity track
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlayModeSmoke|PlatformCatalog|App6|Excel" -v minimal
```

## Epic Themes

| Epic | Goal |
|------|------|
| `sprint-28-cmo-corpus-v2` | Nightly platform corpus v2 + import golden hygiene |
| `sprint-28-platform-editor-write` | ADR-011 Phase D Excel write path + viewer export hook |
| `sprint-28-combat-domains-phase2` | Surface/subsurface validators + damage readiness wire |
| `sprint-28-logistics-catalog-bridge` | Live magazine counts from catalog |
| `sprint-28-closeout-devops` | Closeout hygiene + CI doc refresh |

## Parallel Planning Artifacts

- `production/agentic/sprint-28-parallel-kickoff-2026-06-18.md`
- `production/agentic/sprint-28-plan-data-2026-06-18.md` *(create at kickoff)*
- `production/agentic/sprint-28-plan-unity-2026-06-18.md` *(create at kickoff)*
- `production/agentic/sprint-28-plan-qa-2026-06-18.md` *(create at kickoff)*

## QA Plan

> ⚠️ **No QA Plan**: This sprint was started without a QA plan. Run `/qa-plan sprint` before the last story is implemented. The Production → Polish gate requires a QA sign-off report, which requires a QA plan.

Target path: `production/qa/qa-plan-sprint-28-2026-09-18.md`

## Definition of Done for this Sprint

- [ ] All Must Have tasks completed
- [ ] All tasks pass acceptance criteria
- [ ] QA plan exists (`production/qa/qa-plan-sprint-28-*.md`)
- [ ] All Logic/Integration stories have passing unit/integration tests
- [ ] Smoke check passed (`/smoke-check sprint`)
- [ ] QA sign-off report: APPROVED or APPROVED WITH CONDITIONS (`/team-qa sprint`)
- [ ] No S1 or S2 bugs in delivered features
- [ ] Design documents updated for any deviations
- [ ] Code reviewed and merged

*Created following `/sprint-plan new` + dispatching-parallel-agents context gather (2026-06-18). Lean review mode — PR-SPRINT skipped. Run `/qa-plan sprint` before implementation begins.*