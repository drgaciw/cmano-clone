# Sprint 32 — Release Train Ops, Combat Phase 6 & Platform Editor Phase F

**Dates:** 2026-11-13 → 2026-11-26  
**Trunk:** `main` @ `3406bc4` (Sprint 31 complete; 1006/1006; QA APPROVED)  
**Predecessor:** Sprint 31 — Corpus Complete, Combat Phase 5 & Presentation Polish (12/13; S31-12 deferred)

## Sprint Goal

Operationalize the **S31 corpus-complete drop** into a **unified release-train manifest** with **mount/loadout quarantine remediation**, advance **ADR-009 Phase 6** (facility validator, bounded ECCM, optional mine/BDA sim hooks), and land **Platform Editor Phase F** — damage workbook surfacing in Unity — without TL Phase 5 forks, full corpora in CI, or `DelegationBridge.cs` edits.

## Capacity

| Metric | Value |
|--------|-------|
| Total days | 10 |
| Buffer (20%) | 2 days reserved |
| **Effective dev-days** | **8** |
| **Commit target** | **9 stories** (6 must + 2 should + closeout) |
| **Plan target** | **13 stories** |
| **Test baseline** | ≥1006 day-1; closeout target **≥1046** (+40) |

## Tasks

### Must Have (Critical Path)

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S32-01 | **Full-solution re-baseline** — day-1 build + full sln; record ≥1006; GitNexus @ trunk; ReplayGolden 6/6 | c-sharp-devops-engineer | 1 | S31 merged | 0 errors; ≥1006/1006 PASS; smoke doc; indexed commit recorded |
| S32-02 | **Unified release-train manifest** — consolidate S31 domain `releaseVersion` rows into one curator drop + export manifest | team-data | 2 | S32-01 | `RecordRelease` publishes consolidated manifest; `scenario_validate` resolves manifest-backed `dbRef`; evidence `sprint-32-release-train-manifest-*.md`; WriteGate only |
| S32-03 | **Mount/loadout quarantine triage** — FK repair on quarantined ship/facility/submarine child rows | team-data | 2.5 | S32-02 | Curator triage script; bounded FK repair; quarantine count reduction evidenced; CI stays curated slices only |
| S32-04 | **Facility aspect domain validator (bounded)** — `FacilityAspectDomainValidator` + `FACILITY_ASPECT_BLOCK` | team-simulation | 1.5 | S32-01, S31-05 | `Combat\|Domain\|Facility` tests PASS; ReplayGolden 6/6 default; production Baltic hash unchanged |
| S32-05 | **ECCM scenario factor (bounded Phase 2)** — optional `eccmFactor` on `ScenarioDetectionTrial` + policy JSON | team-simulation | 1.5 | S32-01 | `baltic-patrol-jammed` isolated fixture; default path unchanged; ReplayGolden 6/6 |
| S32-06 | **Platform Editor Phase F — damage Unity surfacing** — viewer damage columns + import staging diff + headless approve round-trip | team-unity | 2 | S32-01 | `PlatformCatalogViewerHost` shows damage fields; staging diff for `MaxHp` edits; headless propose→approve tests PASS |

**Sprint fails** if S32-02 does not produce an operational unified release-train manifest or S32-06 does not surface damage workbook round-trip in Unity.

### Should Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S32-07 | **Release diff report CLI (DBI-4.5)** — deterministic diff between two `ReleaseVersion` values | team-data | 1.5 | S32-02 | `catalog_release_diff` verb; empty-diff golden on re-import; no live-table mutation |
| S32-08 | **Mine transit hazard hot-tick (bounded)** — scenario `mineHazard` zone + seeded placement; no mine-laying missions | team-simulation | 2 | S32-04, S31-04 | Isolated fixture `baltic-patrol-mine-transit-hazard`; `/replay-verify`; not in ReplayGolden 6/6 catalog |
| S32-09 | **BDA contact lifecycle sim hook** — promote contact FSM to `Lost` when `damageLevel ≥ 3` behind `combatDomainsEnabled` | team-simulation | 1.5 | S31-06 | Isolated fixture; projection + sim-kernel consistent; ReplayGolden 6/6 default |
| S32-10 | **Live Editor presentation evidence** — replace S31 protocol PNGs with live captures or refresh `*-s32-*.png` placeholders | team-unity | 1.5 | S32-01 | Evidence `production/qa/evidence/*-s32-*.png`; headless filter ≥35/35 PASS |
| S32-11 | **C2 manual sign-off upgrade** — re-run checklist 14–16; upgrade S31 PASS WITH NOTES when live evidence exists | team-qa | 1 | S32-06, S32-10 | Updated `c2-manual-signoff-*.md`; evidence `sprint-32-c2-signoff-*.md` |
| S32-13 | **Closeout hygiene** — replay 6/6; GitNexus @ tip; tracker rows 06/18/20/21; smoke doc ≥1046; prune `stack/sprint31/*` | c-sharp-devops-engineer | 0.5 | S32-02+ | Evidence `sprint-32-gitnexus-*.md`; `smoke-sprint-32-closeout-*.md` |

### Nice to Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S32-12 | **CI/local gate refresh (S31-12 carryover)** — `verify-ci-local.ps1` baseline ≥1006 / closeout ≥1046 | c-sharp-devops-engineer | 0.25 | S32-01 | Doc-only; evidence `sprint-32-ci-hygiene-*.md`; non-blocking |

## Carryover from Sprint 31

| Item | S31 status | S32 placement |
|------|------------|---------------|
| S31-12 CI/local gate refresh | Deferred backlog | **S32-12 nice-to-have** |
| Live Editor screenshots | S31-07/08 PASS WITH NOTES | **S32-10 + S32-11 should-have** |
| Cross-system validation P1 (DBI-3.5) | Not started | **Deferred to Sprint 33** |
| Weapon→mount dependency graph (DBI-1.5) | Not started | **Deferred to Sprint 33** |
| Datalink share gate on comms degrade | Sim agent proposed | **Deferred to Sprint 33** |

## Explicitly Out of Scope

- Full corpora in **`dotnet test` CI** (7208 sensor, 4844 ship, 4403 weapon, entity full sets)
- **TL Phase 5** physical SQLite forks / `TlBranchDatabaseResolver`
- **Mine-laying / mine-clearing missions**; full mine danger-area map layer
- Hot-tick **full BDA component model**; catalog onboard ECCM flags
- **JADC2 node damage**; **CMO mission/scenario import** runtime (Req 11 Phase 2/3)
- **DOTS ECS**, **Cesium production globe**, **swarm sector coordinator**
- **ZERO touch violation** on `DelegationBridge.cs`

## Should-Have Cut Line

| Cut order | Drop | Keep |
|-----------|------|------|
| 1 | S32-12 (CI hygiene — 3rd deferral OK) | S32-03 quarantine triage |
| 2 | S32-10 (live Editor — lean placeholders) | S32-06 Phase F |
| 3 | S32-09 (BDA lifecycle hook) | S32-04 facility validator |
| 4 | S32-08 (mine transit hazard) | S32-05 ECCM factor |
| 5 | S32-11 (C2 live upgrade) | S32-07 release diff CLI |

**Minimum shippable (beyond must-have):** **S32-07** release diff CLI + **S32-13** closeout.

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| Quarantine FK repair scope creep | Medium | High | Bounded repair rules; per-domain evidence; off-CI scratch DB |
| Unified manifest drift vs per-domain S31 hashes | Medium | CRITICAL | Deterministic sorted export; `scenario_validate` gate |
| ADR-009 golden drift (facility/ECCM fixtures) | Medium | CRITICAL | Isolated pins; production Baltic hash pinned |
| Editor evidence blocked (no Unity host) | High | Low | Lean PASS WITH NOTES; headless proxy merge authority |
| Phase F damage UI scope vs ADR-011 | Medium | Medium | Read-only viewer + staging diff only; no new migrations |

## GitNexus Rules (Mandatory)

- **CRITICAL extend-only:** `CatalogWriteGate`
- **ZERO touch:** `DelegationBridge.cs`
- **HIGH:** `DomainValidatorRegistry`, `ScenarioValidationEngine`, `CatalogDamageHotTickApplier`
- `/replay-verify` mandatory on S32-04, S32-05, S32-08, S32-09 sim merges

## Quality Gates

```bash
export PATH="/home/username01/.dotnet:$PATH"

# Day-1 (S32-01)
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal

# Data track
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "WriteGate|CmoMarkdown|Platform|CatalogImport|Snapshot|TlTier|Scenario|TlRelease|Quarantine" -v minimal
rg -l "TlBranchDatabase|BranchDatabase" src/ --glob "*.cs" || true

# Sim track
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Combat|Domain|Mine|Facility|Bda|Damage|Eccm" -v minimal

# Unity track
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlatformImport|Doctrine|C2TopBar|PlayModeSmoke|PlatformCatalogViewer" -v minimal
```

## Epic Themes

| Epic | Goal |
|------|------|
| `sprint-32-release-train-ops` | Unified manifest + quarantine triage + release diff CLI |
| `sprint-32-combat-domains-phase6` | Facility validator + ECCM + mine/BDA bounded hooks |
| `sprint-32-platform-editor-phase-f` | Damage Unity surfacing + live Editor evidence |
| `sprint-32-presentation-qa` | C2 sign-off upgrade |
| `sprint-32-closeout-devops` | Baseline + CI hygiene + closeout |

## Parallel Planning Artifacts

- `production/agentic/sprint-32-parallel-kickoff-2026-06-18.md`
- `production/agentic/sprint-32-plan-data-2026-06-18.md` *(create at kickoff)*
- `production/agentic/sprint-32-plan-sim-2026-06-18.md` *(create at kickoff)*
- `production/agentic/sprint-32-plan-unity-2026-06-18.md` *(create at kickoff)*

## QA Plan

`production/qa/qa-plan-sprint-32-2026-11-13.md`

## Definition of Done for this Sprint

- [ ] All Must Have tasks completed
- [ ] QA plan exists (`production/qa/qa-plan-sprint-32-*.md`)
- [ ] Smoke check passed (`/smoke-check sprint`)
- [ ] QA sign-off report: APPROVED or APPROVED WITH CONDITIONS (`/team-qa sprint`)
- [ ] No S1 or S2 bugs in delivered features
- [ ] Code reviewed and merged

*Created following `/sprint-plan new` + dispatching-parallel-agents (2026-06-18). Lean review mode — PR-SPRINT skipped.*

**Scope check:** If stories expand beyond epic scope, run `/scope-check [epic]` before implementation.