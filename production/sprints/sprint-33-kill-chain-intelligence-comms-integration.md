# Sprint 33 — Kill-Chain Intelligence, Comms Integration & Platform Editor Phase G

**Dates:** 2026-11-27 → 2026-12-10  
**Trunk:** `main` @ S32 closeout `d3db76d` (**1073/1073**; ReplayGolden 6/6; Baltic hash `17144800277401907079`)  
**Predecessor:** Sprint 32 — **COMPLETE** (13/13, QA APPROVED 2026-06-19)

## Sprint Goal

Land **post-P0 Database Intelligence P1** (DBI-1.5 dependency graph + DBI-3.5 kill-chain rule pack), close the **S32-deferred datalink share gate on comms degrade**, and surface **Platform Editor Phase G** (comms/datalink workbook in Unity) — without TL Phase 5 forks, full corpora in CI, or `DelegationBridge.cs` edits.

## Capacity

| Metric | Value |
|--------|-------|
| Total days | 10 |
| Buffer (20%) | 2 days reserved |
| **Effective dev-days** | **8** |
| **Commit target** | **9 stories** (5 must + 2 should + closeout) |
| **Plan target** | **13 stories** |
| **Test baseline** | ≥**1073** day-1 (S32 closeout); closeout target **≥1086** (+13) |

## Tasks

### Must Have (Critical Path)

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S33-01 | **Full-solution re-baseline** — day-1 build + full sln; record ≥1073; GitNexus @ trunk; ReplayGolden 6/6 | c-sharp-devops-engineer | 1 | S32-13 done | 0 errors; ≥1073 PASS; smoke doc; indexed commit recorded |
| S33-02 | **Weapon→mount→sensor dependency graph (DBI-1.5)** — materialized sorted edge index; `ICatalogReader` API; commit-invalidated on `ApproveBatch` | team-data | 2.5 | S33-01 | `GetSortedDependencyEdges()` stable keys; Baltic + `ship-slice-100` CI only; evidence `sprint-33-dependency-graph-*.md`; WriteGate extend-only |
| S33-03 | **Kill-chain impossibility rule pack (DBI-3.5)** — extend `CatalogRulesValidationAgent` with bounded R1–R4 `KILL_CHAIN_*` codes | team-data | 2.5 | S33-02 | Deterministic findings; golden hash stable on Baltic; orchestrator surfaces new codes; detect-only |
| S33-04 | **Datalink comms share gate** — `DatalinkSidePictureMerger` gates peer share on `CommsState` (Nominal/Degraded/Denied) | team-simulation | 1.5 | S33-01 | Harness passes `bridge.CurrentCommsState`; default Nominal preserves S30-10; **ZERO** `DelegationBridge.cs` diff |
| S33-06 | **Platform Editor Phase G — comms/datalink Unity** — viewer comms columns + staging diff + headless propose→approve | team-unity | 2 | S33-01; S33-03; S33-04 | `PlatformCatalogViewerHost` shows `LinkId`/`Role`/`SatcomCapable`; staging diff for comms edits; headless tests PASS |

**Sprint fails** if S33-02 does not produce an operational dependency graph index, S33-04 does not gate datalink sharing on comms degrade, or S33-06 does not surface comms workbook round-trip in Unity.

### Should Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S33-05 | **Orchestrator + write-gate kill-chain gate** — `DatabaseIntelligenceOrchestrator` runs graph + rules on platform batches | team-data | 1.5 | S33-03 | Blocking `KILL_CHAIN_*` errors prevent `ApproveBatch` commit; quarantine partition respected |
| S33-07 | **Isolated `baltic-patrol-datalink-comms` fixture** — comms transitions + datalink doctrine; pinned isolated golden | team-simulation | 1 | S33-04 | **Not** in ReplayGolden 6/6; `/replay-verify` PASS; production Baltic hash unchanged |
| S33-08 | **Kill-chain CLI verbs** — `catalog_dependency_graph` + `catalog_kill_chain_report` (read-only) | team-data | 1 | S33-02, S33-03 | Deterministic sorted stdout; Cli tests PASS; no live-table mutation |
| S33-09 | **Phase 6 regression smoke (optional)** — reuse S32 isolated pins + filter suite; stretch: `baltic-patrol-combat-phase6-smoke` | team-simulation | 0.5 | S33-04; S33-07 | Isolated golden; not in 6/6 catalog; **drop before S33-07** on cut line |
| S33-10 | **Live Editor presentation evidence (Phase G)** — comms viewer + import staging PNGs | team-unity | 1.5 | S33-06 | `production/qa/evidence/*-s33-*.png`; headless filter ≥38/38 PASS |
| S33-11 | **C2 manual sign-off upgrade** — Check 17 comms fittings; refresh 14–16 | team-qa | 1 | S33-06, S33-10 | Updated `c2-manual-signoff-*.md`; evidence `sprint-33-c2-signoff-*.md` |
| S33-13 | **Closeout hygiene** — replay 6/6; GitNexus @ tip; tracker rows 06/18/20/21; smoke ≥1086; prune `stack/sprint32/*` | c-sharp-devops-engineer | 0.5 | S33-02+ | Evidence `sprint-33-gitnexus-*.md`; `smoke-sprint-33-closeout-*.md` |

### Nice to Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S33-12 | **CI/local gate refresh (S32-12 carryover)** — `verify-ci-local.ps1` baseline ≥1073 / closeout ≥1086 | c-sharp-devops-engineer | 0.25 | S33-01 | Doc-only; evidence `sprint-33-ci-hygiene-*.md`; non-blocking (4th deferral OK) |

## Carryover from Sprint 32

| Item | S32 status | S33 placement |
|------|------------|---------------|
| Cross-system validation P1 (DBI-3.5) | Deferred → S33 scope | **S33-03** must-have |
| Weapon→mount dependency graph (DBI-1.5) | Deferred → S33 scope | **S33-02** must-have |
| Datalink share gate on comms degrade | Deferred → S33 scope | **S33-04** must-have |
| S32-02 manifest / S32-03 quarantine / S32-07 diff | **Done** (S32-02/03/07) | No S33 fallback — graph/rules use S32 outputs |
| S32-04..09 sim stories | **Done** (S32-04..09) | S33-09 regression-only optional (0.5d); no carryforward |
| S32-12 CI hygiene | **Done** | **S33-12** nice-to-have (4th deferral OK) |

## Explicitly Out of Scope

- Full corpora in **`dotnet test` CI** (7208 sensor, 4844 ship, 4403 weapon)
- **TL Phase 5** physical SQLite forks / `TlBranchDatabaseResolver`
- Auto-repair of kill-chain findings (detect-only per DBI-2.3)
- Datalink **behavior** wiring in Unity (schema surfacing only — sim owns S33-04)
- **DOTS ECS**, **Cesium production globe**, **CMO mission/scenario import** runtime
- **ZERO touch violation** on `DelegationBridge.cs`

## Should-Have Cut Line

| Cut order | Drop | Keep |
|-----------|------|------|
| 1 | S33-12 (CI hygiene — 4th deferral OK) | S33-03 kill-chain rules |
| 2 | S33-10 (live Editor — lean placeholders) | S33-06 Phase G |
| 3 | S33-09 (Phase 6 integration) | S33-04 share gate |
| 4 | S33-08 (kill-chain CLI) | S33-05 orchestrator gate |
| 5 | S33-11 (C2 live upgrade) | S33-07 datalink-comms fixture |

**Minimum shippable (beyond must-have):** **S33-05** orchestrator gate + **S33-07** isolated fixture + **S33-13** closeout.

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| Kill-chain graph index staleness after batch approve | Low | Medium | Commit-invalidate on `ApproveBatch`; Baltic + `ship-slice-100` CI only |
| Share gate changes default merge | Medium | CRITICAL | Default `CommsState.Nominal`; ReplayGolden 6/6 on every sim merge |
| Kill-chain rule noise from quarantine | Medium | High | Partition quarantined edges; coordinate with S32-03 if open |
| Phase G scope creep into sim behavior | Medium | High | Lock S33-06 to viewer + staging diff; behavior stays S33-04 |
| `CatalogWriteGate` regression | Medium | CRITICAL | Extend-only; full WriteGate filter on every data PR |

## GitNexus Rules (Mandatory)

- **CRITICAL extend-only:** `CatalogWriteGate`
- **ZERO touch:** `DelegationBridge.cs`
- **HIGH:** `CatalogRulesValidationAgent`, `DatalinkSidePictureMerger`, `PlatformCatalogViewerHost`, `DatabaseIntelligenceOrchestrator`
- `/replay-verify` mandatory on S33-04, S33-07, S33-09 sim merges

## Quality Gates

```bash
export PATH="/home/username01/.dotnet:$PATH"

# Day-1 (S33-01)
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal

# Data track
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "WriteGate|DependencyGraph|KillChain|CrossSystem|DatabaseIntelligence" -v minimal
rg -l "TlBranchDatabase|BranchDatabase" src/ --glob "*.cs" || true

# Sim track
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Datalink|Comms|Combat|Domain" -v minimal

# Unity track
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlatformImport|PlatformCatalogViewer|PlatformComms" -v minimal
```

## Epic Themes

| Epic | Goal |
|------|------|
| `sprint-33-kill-chain-intelligence` | DBI-1.5 graph + DBI-3.5 rules + orchestrator + CLI |
| `sprint-33-cyber-comms-datalink` | Comms share gate + isolated fixtures + Phase 6 integration |
| `sprint-33-platform-editor-phase-g` | Comms/datalink Unity surfacing + live evidence |
| `sprint-33-presentation-qa` | C2 sign-off upgrade (Check 17) |
| `sprint-33-closeout-devops` | Baseline + CI hygiene + closeout |

## Parallel Planning Artifacts

- `production/agentic/sprint-33-parallel-kickoff-2026-06-19.md`
- `production/agentic/sprint-33-plan-data-2026-06-19.md`
- `production/agentic/sprint-33-plan-sim-2026-06-19.md`
- `production/agentic/sprint-33-plan-unity-2026-06-19.md`
- `production/agentic/sprint-33-plan-devops-qa-2026-06-19.md`

## QA Plan

`production/qa/qa-plan-sprint-33-2026-11-27.md`

## Definition of Done for this Sprint

- [ ] All Must Have tasks completed
- [ ] QA plan exists (`production/qa/qa-plan-sprint-33-*.md`)
- [ ] Smoke check passed (`/smoke-check sprint`)
- [ ] QA sign-off report: APPROVED or APPROVED WITH CONDITIONS (`/team-qa sprint`)
- [ ] No S1 or S2 bugs in delivered features
- [ ] Tracker row 06 updated (DBI-1.5, DBI-3.5 → Partial+ or Done)

*Refreshed post-S32 closeout following `/sprint-plan new` + dispatching-parallel-agents (2026-06-19). Lean review mode — PR-SPRINT skipped. First dev dispatch: `/dev-story dispatch S33-01`.*

**Scope check:** If stories expand beyond epic scope, run `/scope-check [epic]` before implementation.