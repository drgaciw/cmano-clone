# Sprint 30 — TL Bind, Corpus Scale & Combat Phase 4

**Dates:** 2026-10-16 → 2026-10-29  
**Trunk:** `main` @ `e447159` (Sprint 29 complete; 878/878; QA APPROVED)  
**Predecessor:** Sprint 29 — Operationalize Data-to-Fight Loop (13/13)

## Sprint Goal

Close the **TL release-train loop** (export Phase 3 tier filters + Phase 4 scenario `tlBranch` binding at load), scale **off-CI nightly `ship.md` approve**, advance **ADR-009** with bounded land-domain validators and hot-tick hit→HP wiring, and close **S29 lean-mode presentation debt**—without physical TL SQLite forks, full BDA component runtime, or 7208-sensor CI load.

## Capacity

| Metric | Value |
|--------|-------|
| Total days | 10 |
| Buffer (20%) | 2 days reserved |
| **Effective dev-days** | **8** |
| **Commit target** | **9 stories** (4 must + 4 should + closeout) |
| **Plan target** | **12 stories** |
| **Test baseline** | ≥878 day-1; closeout target **≥918** (+40) |

## Tasks

### Must Have (Critical Path)

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S30-01 | **Full-solution re-baseline** — day-1 `dotnet build` + `dotnet test ProjectAegis.sln`; record ≥878 in `sprint-status.yaml` + smoke doc; GitNexus @ trunk | c-sharp-devops-engineer | 1 | S29 merged | 0 errors; ≥878/878 PASS; ReplayGolden 6/6; indexed commit recorded |
| S30-02 | **TL export Phase 3** — per-tier filtered `ICatalogReader` export (read-only); `platform_export_xlsx` / JSON drops honor `tlTier` filter | team-data | 2 | S30-01 | Filtered export tests PASS; deterministic sort keys locked; no runtime branch DB |
| S30-03 | **TL export Phase 4** — scenario package `tlBranch` field + load-time validation (`ScenarioValidationEngine`); bind at authoring, not mid-tick | team-data + Mission Editor | 2.5 | S30-02 | `tlBranch` validated at load; `rg TlBranchDatabase\|BranchDatabase` → zero; CLI `scenario_validate` surfaces findings; evidence `sprint-30-tl-phase4-*.md` |
| S30-04 | **Nightly `ship.md` approve at scale** — off-CI curator path for full platform corpus (4844 records, chunk 500); `RecordRelease` + pinned hash | team-data | 2 | S30-01 | Off-CI evidence `sprint-30-nightly-ship-*.md`; all commits via `CatalogWriteGate.ApproveBatch`; CI stays curated slices |

**Sprint fails** if S30-03 does not bind `tlBranch` at scenario load through validated package metadata with zero `TlBranchDatabaseResolver` mid-tick behavior.

### Should Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S30-05 | **Land aspect domain validator** — bounded ADR-009 plug-in; stable abort codes; flag-gated isolated fixture | team-simulation | 1.5 | S30-01 | `LandDomainValidator` registered; `Combat\|Domain` tests PASS; ReplayGolden 6/6 on default path |
| S30-06 | **Editor presentation evidence batch** — closes S29-04/07/08 QA conditions (import staging, doctrine, Begin Execution PNGs) | team-unity | 1 | S30-01 | Evidence under `production/qa/evidence/*-s30-*.png`; signoff script scenarios extended; headless tests unchanged PASS |
| S30-07 | **Begin Execution planning chrome** — map dimmed, drawer read-only while `SimulationPhase.Planning`; extends S29-08 | team-unity | 1.5 | S30-01 | `C2PlanningChromeTests` PASS; score freeze regression PASS; ZERO touch `DelegationBridge` |
| S30-08 | **Hot-tick damage extensions** — engagement `Hit` → `PlatformHpLedger` via `DeterministicDamageApplyBatch`; `damageLevel` 0–3 | team-simulation | 1.5 | S30-05 | Sim `Combat\|Domain\|Damage` PASS; `/replay-verify`; no hot-path SQLite |
| S30-13 | **Closeout hygiene** — replay 6/6; GitNexus @ stack tip; tracker rows 06/18/21; smoke doc ≥918 | c-sharp-devops-engineer | 0.5 | S30-03+ | Evidence `sprint-30-gitnexus-*.md`; stack/sprint29/* prune documented |

### Nice to Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S30-09 | **Production Baltic `combatDomainsEnabled` flip** — ADR-009 migration step 4; producer-gated | team-simulation | 0.5 | S30-05 | `baltic-patrol.policy.json` flag-on only if producer approves; isolated pins unchanged; ReplayGolden 6/6 |
| S30-10 | **Datalink share lag** — bounded TR-sensor-004 extension (`datalink.shareLagTicks`); deterministic merge order preserved | team-simulation | 1.5 | S30-01 | `Datalink` tests PASS; fixture `baltic-patrol-datalink-lag`; ReplayGolden 6/6 |
| S30-11 | **CMO entity nightly slices** — extend nightly scripts for `aircraft` / `submarine` / `facility` corpora (off-CI) | team-data | 2 | S30-04 | Script flags wired; curated golden per domain; full corpora never in CI |
| S30-12 | **CI/local gate refresh** — update `verify-ci-local.ps1` baseline ≥918 | c-sharp-devops-engineer | 0.25 | S30-01 | Doc-only; non-blocking |

## Carryover from Sprint 29

| Item | S29 status | S30 placement |
|------|------------|---------------|
| TL Phase 3 export filters | Deferred (S29 did Phases 1–2) | **S30-02 must-have** |
| TL Phase 4 scenario `tlBranch` binding | Deferred post-S29 | **S30-03 must-have** |
| Full `ship.md` nightly approve at scale | Curated `ship-slice-100` only | **S30-04 must-have** |
| Land/mine/facility validators | Out of scope (air/surface/subsurface done) | **S30-05 should-have** (land only; mine stub deferred) |
| Production Baltic `combatDomainsEnabled=true` | Isolated pin only (S29-05) | **S30-09 nice-to-have** (producer-gated) |
| Hot-tick Hit → HP ledger | S29-09 ambient drain only | **S30-08 should-have** |
| S29-04/07/08 Editor screenshots | QA advisory | **S30-06 should-have** |
| S29-08 planning chrome (UX spec) | Top-bar button only | **S30-07 should-have** |
| Datalink lag (TR-sensor-004 remainder) | S29-11 merge-only | **S30-10 nice-to-have** |
| CMO mission/scenario import | Out of scope | Spike only if S30-11 capacity |

## Explicitly Out of Scope

- **Full 7208-record `sensor.md` in `dotnet test` CI**
- **Physical TL-0…TL-5 SQLite fork databases** (TL Phase 5)
- **`TlBranchDatabaseResolver` / per-tier DB files**
- **Hot-tick full BDA component model** / mine-transit hazard runtime
- **CMO mission/scenario import runtime** (Req 11 Phase 2/3)
- **ECCM Phase 2**, **JADC2 node damage**, **swarm sector coordinator**
- **DOTS ECS migration**, **Cesium production globe**
- **ZERO touch violation** on `DelegationBridge.cs`

## Should-Have Cut Line

| Cut order | Drop | Keep |
|-----------|------|------|
| 1 | S30-10 (datalink lag) | S30-05 land validator |
| 2 | S30-11 (CMO entity slices) | S30-03 TL Phase 4 |
| 3 | S30-07 (planning chrome) | S30-04 ship.md approve |
| 4 | S30-08 (hot-tick hits) | S30-02 TL Phase 3 |

**Minimum shippable (beyond must-have):** S30-05 + S30-13.

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| TL Phase 4 scenario binding causes snapshot/branch drift | Medium | CRITICAL | Bind at package authoring only; grep gate; `/replay-verify` on scenario-load changes |
| `CatalogWriteGate` extend-only violation | Medium | CRITICAL | `gitnexus impact` before edit; WriteGate regression every data merge |
| ADR-009 golden drift (land validators / Baltic flip) | Medium | CRITICAL | Isolated flag-on fixtures; production Baltic flip producer-gated; separate pinned hashes |
| Parallel track overload (data + Unity + sim) | Medium | High | S29-style wave dispatch; cut line before nice-to-have; 20% buffer |
| Corpus scope creep into CI | High | High | Off-CI nightly + curated `--max-records` only |

## GitNexus Rules (Mandatory)

- **CRITICAL extend-only:** `CatalogWriteGate`
- **ZERO touch:** `DelegationBridge.cs`
- **HIGH:** `ICatalogReader`, `ScenarioValidationEngine`, `DomainValidatorRegistry`, `PlatformWorkbookWriteBridge`
- `/replay-verify` mandatory on S30-05, S30-08, S30-09 sim merges

## Producer Constraints (Inherited)

1. **Nightly corpus** — full sensor corpus stays off-CI; approve workflow off-CI
2. **GHA billing** — Buildkite merge authority; local-gate advisory permanent
3. **ADR-011 Excel-primary** — all writes via workbook + write gate
4. **TL Phase 5 physical forks** — post-MVP; S30 binds scenarios to branch-tagged snapshots on single `main` catalog

## Quality Gates

```bash
export PATH="/home/username01/.dotnet:$PATH"

# Day-1 (S30-01)
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal

# Data track
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "CmoMarkdown|WriteGate|Platform|CatalogImport|Snapshot|TlTier|Scenario" -v minimal
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "CatalogImport|Platform|Scenario" -v minimal
rg -l "TlBranchDatabase|BranchDatabase" src/ --glob "*.cs" || true

# Sim track
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Combat|Domain|Damage|Readiness|Datalink" -v minimal

# Unity track
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlayModeSmoke|PlatformImport|Doctrine|C2TopBar|C2Planning" -v minimal
```

## Epic Themes

| Epic | Goal |
|------|------|
| `sprint-30-tl-export-phase34` | Tier export filters + scenario `tlBranch` binding |
| `sprint-30-corpus-approve-scale` | `ship.md` nightly approve at scale |
| `sprint-30-combat-domains-phase4` | Land validator + hot-tick hit apply + optional Baltic flip |
| `sprint-30-c2-planning-chrome` | Planning chrome + presentation evidence |
| `sprint-30-closeout-devops` | Closeout hygiene + CI doc refresh |

## Parallel Planning Artifacts

- `production/agentic/sprint-30-parallel-kickoff-2026-06-18.md` *(create at kickoff)*
- `production/agentic/sprint-30-plan-data-2026-06-18.md` *(create at kickoff)*
- `production/agentic/sprint-30-plan-unity-2026-06-18.md` *(create at kickoff)*
- `production/agentic/sprint-30-plan-sim-2026-06-18.md` *(create at kickoff)*

## QA Plan

`production/qa/qa-plan-sprint-30-2026-10-16.md`

## Definition of Done for this Sprint

- [ ] All Must Have tasks completed
- [ ] All tasks pass acceptance criteria
- [ ] QA plan exists (`production/qa/qa-plan-sprint-30-*.md`)
- [ ] All Logic/Integration stories have passing unit/integration tests
- [ ] Smoke check passed (`/smoke-check sprint`)
- [ ] QA sign-off report: APPROVED or APPROVED WITH CONDITIONS (`/team-qa sprint`)
- [ ] No S1 or S2 bugs in delivered features
- [ ] Design documents updated for any deviations
- [ ] Code reviewed and merged

*Created following `/sprint-plan new` + dispatching-parallel-agents (2026-06-18). Lean review mode — PR-SPRINT skipped.*

**Scope check:** If stories expand beyond epic scope, run `/scope-check [epic]` before implementation.