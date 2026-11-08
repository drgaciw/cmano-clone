# Sprint 31 — Corpus Complete, Combat Phase 5 & Presentation Polish

**Dates:** 2026-10-30 → 2026-11-12  
**Trunk:** `main` @ `3406bc4` (Sprint 30 complete; 956/956; QA APPROVED)  
**Predecessor:** Sprint 30 — TL Bind, Corpus Scale & Combat Phase 4 (13/13)

## Sprint Goal

Finish the **off-CI corpus approve loop** (sensor + balance-drift hygiene), wire **TL release-train snapshot resolution** at scenario load, advance **ADR-009 Phase 5** with bounded mine/facility/BDA hot paths, and close **S30 lean presentation debt**—without physical TL SQLite forks, full BDA component runtime, or 7208-record sensor load in CI.

## Capacity

| Metric | Value |
|--------|-------|
| Total days | 10 |
| Buffer (20%) | 2 days reserved |
| **Effective dev-days** | **8** |
| **Commit target** | **9 stories** (4 must + 4 should + closeout) |
| **Plan target** | **13 stories** |
| **Test baseline** | ≥956 day-1; closeout target **≥996** (+40) |

## Tasks

### Must Have (Critical Path)

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S31-01 | **Full-solution re-baseline** — day-1 build + full sln; record ≥956; GitNexus @ trunk; ReplayGolden 6/6 | c-sharp-devops-engineer | 1 | S30 merged | 0 errors; ≥956/956 PASS; smoke doc; indexed commit recorded |
| S31-02 | **Nightly `sensor.md` approve at scale (off-CI)** — 7208 records via nightly approve pattern; `RecordRelease` + pinned hash; CI curated slice only | team-data | 2 | S31-01 | Off-CI evidence `sprint-31-nightly-sensor-*.md`; all commits via `CatalogWriteGate.ApproveBatch` |
| S31-03 | **TL release-train snapshot resolution at load** — resolve `dbRef`/`snapshotId` from `tlBranch` + release train at package load (no Phase 5 forks) | team-data + Mission Editor | 2 | S31-01, S30-03 | `scenario_validate` surfaces mismatch; `rg TlBranchDatabase\|BranchDatabase` → zero |
| S31-04 | **Mine aspect domain validator (bounded)** — `MineAspectDomainValidator` + `MINE_ASPECT_BLOCK`; isolated flag-on fixture | team-simulation | 1.5 | S31-01, S30-05 | `Combat\|Domain` tests PASS; ReplayGolden 6/6 default; ZERO touch `DelegationBridge` |

**Sprint fails** if S31-03 does not resolve TL-tagged snapshots at load without physical branch databases or mid-tick DB switching.

### Should Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S31-05 | **Facility combat hot-tick** — extend S28-09 projection stub with hot-tick HP apply | team-simulation | 2 | S31-01, S30-08 | Isolated fixture; `/replay-verify`; production Baltic hash unchanged |
| S31-06 | **BDA hot path (bounded)** — Hit/`damageLevel` → contact status in BDA projections; not full component model | team-simulation | 1.5 | S30-08, S27-06 | BDA projection tests PASS; ReplayGolden 6/6 default |
| S31-07 | **Live Editor presentation evidence** — replace S30 protocol PNG placeholders; signoff script import + begin-execution | team-unity | 1 | S31-01 | Evidence `production/qa/evidence/*-s31-*.png`; headless proxy unchanged PASS |
| S31-08 | **C2 manual sign-off refresh (post-S30)** — extend checklist for import staging, doctrine, Begin Execution | team-qa / team-unity | 1 | S31-07 | Updated `c2-manual-signoff-*.md`; lean PASS WITH NOTES if no Editor host |
| S31-13 | **Closeout hygiene** — replay 6/6; GitNexus @ tip; tracker rows 06/18/21; smoke doc ≥996; prune `stack/sprint30/*` | c-sharp-devops-engineer | 0.5 | S31-03+ | Evidence `sprint-31-gitnexus-*.md` |

### Nice to Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S31-09 | **Balance drift advisory on nightly approve** — wire S29-10 evaluator into nightly approve summary; default off | team-data | 0.5 | S31-02 | Advisory when `enableBalanceDrift=true`; no write-gate bypass |
| S31-10 | **Nightly `weapon.md` approve at scale (off-CI)** — mirror S30-04 for 4403 records | team-data | 2 | S31-02 | Pinned hash; CI curated `weapon-slice-50` only |
| S31-11 | **Entity corpus nightly approve at scale** — aircraft/facility/submarine full corpora off-CI | team-data | 2 | S30-11 | Per-domain evidence; never in CI |
| S31-12 | **CI/local gate refresh** — `verify-ci-local.ps1` baseline ≥956 closeout ≥996 | c-sharp-devops-engineer | 0.25 | S31-01 | Doc-only; non-blocking |

## Carryover from Sprint 30

| Item | S30 status | S31 placement |
|------|------------|---------------|
| Full 7208 `sensor.md` nightly approve | Off-CI ship/entity only | **S31-02 must-have** |
| TL runtime selection beyond export metadata | S30-03 load validation only | **S31-03 must-have** |
| Mine / facility combat runtime | Land done; mine/facility deferred | **S31-04 must / S31-05 should** |
| Full BDA hot path | S30-08 ledger-only | **S31-06 should-have** |
| S30-06/07 Editor screenshots | QA advisory | **S31-07 + S31-08 should-have** |
| Balance drift in nightly approve | S29-10 on propose path | **S31-09 nice-to-have** |
| Weapon + entity full corpus approve | S30-04/11 slices | **S31-10/11 nice-to-have** |

## Explicitly Out of Scope

- Full 7208-record `sensor.md` in **`dotnet test` CI**
- **TL Phase 5** physical SQLite forks / `TlBranchDatabaseResolver`
- Hot-tick **full BDA component model** / mine-laying missions / JADC2 node damage / ECCM Phase 2
- **CMO mission/scenario import** runtime (Req 11 Phase 2/3)
- **DOTS ECS**, **Cesium production globe**, **swarm sector coordinator**
- **ZERO touch violation** on `DelegationBridge.cs`

## Should-Have Cut Line

| Cut order | Drop | Keep |
|-----------|------|------|
| 1 | S31-11 (entity approve) | S31-04 mine validator |
| 2 | S31-10 (weapon approve) | S31-03 TL release-train |
| 3 | S31-08 (C2 sign-off refresh) | S31-02 sensor approve |
| 4 | S31-06 (BDA hot path) | S31-05 facility hot-tick |

**Minimum shippable (beyond must-have):** S31-05 + S31-13.

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| Off-CI sensor approve runtime / disk pressure | Medium | High | Chunk 500/batch; pinned hash; curator evidence only |
| TL snapshot resolution drift vs `tlBranch` metadata | Medium | CRITICAL | Load-time bind only; `scenario_validate`; grep gate |
| ADR-009 golden drift (mine/facility fixtures) | Medium | CRITICAL | Isolated pins; production Baltic hash pinned |
| Editor evidence blocked (no Unity host) | High | Low | Lean PASS WITH NOTES; headless proxy stays merge authority |
| `CatalogWriteGate` extend-only violation | Medium | CRITICAL | `gitnexus impact` before edit; WriteGate regression every merge |

## GitNexus Rules (Mandatory)

- **CRITICAL extend-only:** `CatalogWriteGate`
- **ZERO touch:** `DelegationBridge.cs`
- **HIGH:** `ScenarioValidationEngine`, `DomainValidatorRegistry`, `CatalogDamageHotTickApplier`
- `/replay-verify` mandatory on S31-04, S31-05, S31-06 sim merges

## Quality Gates

```bash
export PATH="/home/username01/.dotnet:$PATH"

# Day-1 (S31-01)
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal

# Data track
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "CmoMarkdown|WriteGate|Platform|CatalogImport|Snapshot|TlTier|Scenario" -v minimal
rg -l "TlBranchDatabase|BranchDatabase" src/ --glob "*.cs" || true

# Sim track
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Combat|Domain|Damage|Mine|Facility|Bda" -v minimal

# Unity track
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlayModeSmoke|PlatformImport|Doctrine|C2TopBar|C2Planning" -v minimal
```

## Epic Themes

| Epic | Goal |
|------|------|
| `sprint-31-corpus-approve-complete` | Sensor approve + balance drift + optional weapon/entity scale |
| `sprint-31-tl-release-train` | Snapshot resolution at scenario load |
| `sprint-31-combat-domains-phase5` | Mine validator + facility hot-tick + BDA hot path |
| `sprint-31-presentation-polish` | Live Editor evidence + C2 sign-off refresh |
| `sprint-31-closeout-devops` | Baseline + CI hygiene + closeout |

## Parallel Planning Artifacts

- `production/agentic/sprint-31-parallel-kickoff-2026-06-18.md`
- `production/agentic/sprint-31-plan-data-2026-06-18.md` *(create at kickoff)*
- `production/agentic/sprint-31-plan-unity-2026-06-18.md` *(create at kickoff)*
- `production/agentic/sprint-31-plan-sim-2026-06-18.md` *(create at kickoff)*

## QA Plan

`production/qa/qa-plan-sprint-31-2026-10-30.md`

## Definition of Done for this Sprint

- [ ] All Must Have tasks completed
- [ ] QA plan exists (`production/qa/qa-plan-sprint-31-*.md`)
- [ ] Smoke check passed (`/smoke-check sprint`)
- [ ] QA sign-off report: APPROVED or APPROVED WITH CONDITIONS (`/team-qa sprint`)
- [ ] No S1 or S2 bugs in delivered features
- [ ] Code reviewed and merged

*Created following `/sprint-plan new` + dispatching-parallel-agents (2026-06-18). Lean review mode — PR-SPRINT skipped.*

**Scope check:** If stories expand beyond epic scope, run `/scope-check [epic]` before implementation.