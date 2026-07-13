# Sprint 27 — CMO Corpus Pipeline + Combat Bounded + Phase C Viewer

**Dates:** 2026-09-04 → 2026-09-17  
**Trunk:** `main` @ `ab30d35` (Sprint 26 complete; 698/698 baseline)  
**Predecessor:** Sprint 26 — CMO Phase 2 Import + Presentation Closeout (complete, 11/11)

## Sprint Goal

Scale **CMO corpus intake** to a nightly off-CI pipeline, complete **mount→loadout→magazine** markdown import through the extend-only write gate, advance **ADR-011 Phase C** platform viewer UX, and land **ADR-009 bounded** validator + optional order-log BDA slices — without opening full combat runtime or full-corpus CI load.

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
| S27-01 | **Full-solution re-baseline** — day-1 `dotnet build` + `dotnet test ProjectAegis.sln`; record ≥698 in `sprint-status.yaml` + smoke doc; GitNexus @ trunk | c-sharp-devops-engineer | 1 | S26 merged | 0 errors; ≥698/698 PASS; ReplayGolden 6/6; indexed commit recorded |
| S27-02 | **Nightly full CMO corpus import job** — off-CI pipeline for `sensor.md` (7208), `weapon.md`, platform slices; chunk 500/batch; CI keeps curated fixtures + `--max-records` only | team-data | 2 | S27-01 | Job script + scheduled runner; full sensor propose-only run with quarantine artifact; no direct SQLite writes; evidence `sprint-27-nightly-cmo-import-*.md` |
| S27-03 | **CMO mount→loadout→magazine markdown import** — extend `CmoMarkdownImporter` beyond S26-03 mounts-only; `ProposeLoadoutBatch` + `ProposeMagazineBatch` | team-data | 2 | S27-01 | Baltic fixture E2E mount+loadout+magazine; FK quarantine on orphans; chunk 500; GitNexus CRITICAL on `CatalogWriteGate` |
| S27-04 | **Import E2E + golden hygiene** — extend `CmoMarkdownImportGoldenTests`; WriteGate + replay regression unchanged | team-data | 1 | S27-03 | Re-import stable hash; WriteGate regression PASS; ReplayGolden 6/6; **sprint fails** if loadout/magazine round-trip missing |

**Sprint fails** if S27-04 loadout/magazine round-trip does not land through the write gate.

### Should Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S27-05 | **ADR-009 bounded validators** — deterministic damage outcome batch sort; `AirAspectDomainValidator` stub; validator deny → order log (flag-gated) | team-simulation | 2 | S27-01 | Sim tests PASS; `combatDomainsEnabled=false` on Baltic; ReplayGolden 6/6; ZERO touch `DelegationBridge` |
| S27-06 | **Order-log BDA slice** *(producer-gated)* — sorted kill outcomes → contact picture projection drop; no hot-tick world mutation | team-simulation | 1 | S27-05 | Projection tests only; flag-gated fixture; Baltic golden unchanged |
| S27-07 | **Addressables + `Map/App6FrameAtlas` live group** — wire `com.unity.addressables`; degrade to Unicode when unavailable | team-unity | 1 | S27-01 | Manifest + group; headless App6 tests PASS; ZERO touch `DelegationBridge` |
| S27-08 | **Platform catalog viewer panel** — UXML/USS + search/filter on `CatalogPlatformBrowseProjection`; read-only | team-unity | 2 | S27-04 | Headless `PlatformCatalogViewerTests` PASS; no write-gate bypass |
| S27-09 | **Browse projection enrichment** — `MountCount`, `SensorCount` on browse rows + CLI JSON | team-data | 0.5 | S27-03 | Stable sort preserved; CLI tests updated |
| S27-10 | **Editor presentation evidence** — platform viewer + APP-6 atlas screenshots (advisory) | team-unity | 0.5 | S27-07, S27-08 | Protocol placeholders acceptable; headless = merge authority |
| S27-11 | **Platform viewer smoke harness** — PlayMode row or scene integration | team-unity | 0.5 | S27-08 | Headless proxy PASS; write-gate grep clean |
| S27-13 | **Closeout hygiene** — replay 6/6; GitNexus @ stack tip; tracker rows 06/18/21; prune `stack/sprint26/*` | c-sharp-devops-engineer | 0.5 | S27-04+ | Evidence in `sprint-27-gitnexus-*.md`; 698+ closeout |

### Nice to Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S27-12 | **CI hygiene / GHA billing spike** — remediation doc or `verify-ci-local.ps1` refresh | c-sharp-devops-engineer | 0.5 | S27-01 | Documented path; non-blocking |
| S27-14 | **Curated platform corpus slice** — `ship-slice-100.md` + chunk boundary test | team-data | 1 | S27-03 | 501→2 batches; quarantine JSON |
| S27-15 | **Platform browse detail pane** — row expansion for damage/mobility fields | team-unity | 0.5 | S27-08 | Headless bind test |
| S27-16 | **ADR-009 validation checklist closeout** — flag-on smoke fixture (separate hash) | c-sharp-architect | 0.25 | S27-05 | ADR-009 criteria evidenced |

## Carryover from Sprint 26

| Item | S26 status | S27 placement |
|------|------------|---------------|
| Full CMO corpus nightly import | Explicit defer | **S27-02 must-have** |
| Mount/loadout/magazine CMO import | S26-03 mounts only | **S27-03..04 must-have** |
| ADR-011 Phase C full editor UX | S26-10 spike PROCEED | **S27-08..11 should-have** |
| ADR-009 runtime BDA | S26-09 stubs only | **S27-05..06 should-have (bounded)** |
| Addressables APP-6 atlas | S26-06 deferral | **S27-07 should-have** |
| GitHub Actions billing | Open since S16 | **S27-12 nice-to-have** |

## Explicitly Out of Scope

- **Full 7208-record sensor.md in `dotnet test` CI** (nightly job only)
- **Hot-tick world-state damage apply** / full BDA component model
- **Mine/land/facility combat domains**
- **`combatDomainsEnabled=true` on Baltic production fixtures**
- **TL-0–TL-5 branch databases**
- **CMO mission/scenario import** (doc 11 Phase 2/3)
- **Platform write / Excel import UI in-engine** (ADR-011 Excel-primary)
- **ZERO touch violation** on `DelegationBridge.cs`

## Should-Have Cut Line

| Cut order | Drop | Keep |
|-----------|------|------|
| 1 | S27-06 (order-log BDA) | S27-05 validators |
| 2 | S27-11 (viewer harness) | S27-08 panel |
| 3 | S27-10 (Editor evidence) | Headless proxy |
| 4 | S27-07 (Addressables) | S26 atlas USS |
| 5 | S27-09 (browse enrichment) | S27-08 list-only |

**Minimum shippable (beyond must-have):** S27-05 + S27-13.

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| CMO corpus scope creep into CI | High | High | Nightly only; producer sign-off on job scope |
| `CatalogWriteGate` extend-only violation | Medium | CRITICAL | `gitnexus impact` before edit; D04 WriteGate regression |
| ADR-009 Baltic golden drift | Medium | CRITICAL | `combatDomainsEnabled=false` default; flag-on in isolated fixtures |
| Unity/data viewer overlap | Medium | Medium | S27-09 before S27-08; single owner per file |
| Addressables package churn | Medium | Medium | Land S27-07 first; Unicode fallback |

## GitNexus Rules (Mandatory)

- **CRITICAL extend-only:** `CatalogWriteGate`
- **ZERO touch:** `DelegationBridge.cs`
- **HIGH:** `CmoMarkdownImporter`, `ICatalogReader`, `CatalogPlatformBrowseProjection`
- `/replay-verify` mandatory on S27-05..06 sim merges

## Producer Decisions (Approved 2026-06-18)

1. **S27-06** — **APPROVED** — order-log-only BDA slice (TR-combat-dom-003 partial); projection-tested; no hot-tick world mutation
2. **Nightly corpus v1** — **sensor + weapon only** in first job; platform slices deferred to nightly v2 / S27-14 nice-to-have
3. **GHA billing** — **permanent local-gate advisory**; S27-12 documents `verify-ci-local.ps1` path; Buildkite remains merge authority

## Quality Gates

```bash
export PATH="/home/username01/.dotnet:$PATH"

# Day-1 (S27-01)
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal

# Data track
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "CmoMarkdown|WriteGate|Platform|CatalogImport" -v minimal
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "CatalogImport|CatalogPlatformBrowse" -v minimal

# Sim track
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Combat|Domain|Damage|Readiness" -v minimal

# Unity track
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlayModeSmoke|PlatformCatalog|App6|Cesium|Doctrine" -v minimal
```

## QA Plan

`production/qa/qa-plan-sprint-27-2026-06-18.md`

## Parallel Planning Artifacts

- `production/agentic/sprint-27-parallel-kickoff-2026-06-18.md`
- `production/agentic/sprint-27-plan-data-2026-06-18.md`
- `production/agentic/sprint-27-plan-unity-2026-06-18.md`
- `production/agentic/sprint-27-plan-qa-2026-06-18.md`

*Created following sprint-plan skill (Phase 0–6) + dispatching-parallel-agents for context gather. Lean review mode (PR-SPRINT skipped). QA plan required before implementation begins.*