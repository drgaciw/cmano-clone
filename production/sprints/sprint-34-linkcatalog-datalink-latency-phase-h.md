# Sprint 34 — LinkCatalog Workbook Round-Trip, Datalink Latency Bridge & Platform Editor Phase H

**Dates:** 2026-12-11 → 2026-12-24  
**Trunk:** `main` @ S33 closeout `d3db76d` (**1143/1143**; ReplayGolden 6/6; Baltic hash `17144800277401907079`)  
**Predecessor:** Sprint 33 — **COMPLETE** (13/13, QA APPROVED 2026-06-19)

## Sprint Goal

Close tracker **Req 21** *datalink workbook round-trip beyond comms surfacing* by landing **LinkCatalog** data spine + **Platform Editor Phase H** Unity surfacing, and bind **catalog `LatencyMsNominal` → sim share lag** (bounded TR-sensor-004 slice) — without TL Phase 5 forks, full corpora in CI, ECCM Phase 2, or `DelegationBridge.cs` edits.

## Capacity

| Metric | Value |
|--------|-------|
| Total days | 10 |
| Buffer (20%) | 2 days reserved |
| **Effective dev-days** | **8** |
| **Commit target** | **8 stories** (5 must + 2 should + closeout) |
| **Plan target** | **12 stories** |
| **Test baseline** | ≥**1143** day-1 (S33 closeout); closeout target **≥1156** (+13) |

## Tasks

### Must Have (Critical Path)

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S34-01 | **Full-solution re-baseline** — day-1 build + full sln; record ≥1143; GitNexus @ trunk; ReplayGolden 6/6 | c-sharp-devops-engineer | 1 | S33-13 done | 0 errors; ≥1143 PASS; smoke doc; indexed commit recorded |
| S34-02 | **Link catalog data model + write-gate** — `CatalogLinkEntry`, `GetSortedLinks()`, `ProposeLinkCatalogBatch`, Baltic fixture seed | team-data | 2 | S34-01 | Stable `link_id ASC`; staging approve/reject; extend-only `CatalogWriteGate`; evidence `sprint-34-link-catalog-staging-*.md` |
| S34-03 | **LinkCatalog workbook round-trip** — export/import/diff sheet; empty-diff golden; schema version bump | team-data | 1.5 | S34-02 | `PlatformWorkbookRoundTripTests` PASS; bulk-author threshold; evidence `sprint-34-linkcatalog-roundtrip-*.md` |
| S34-04 | **Catalog-derived datalink share lag** — `DatalinkShareLagResolver` maps `LatencyMsNominal` → `ShareLagTicks` at harness bind; scenario override wins | team-simulation | 1.5 | S34-01; S34-02 | Default Baltic unchanged; ReplayGolden 6/6; **ZERO** `DelegationBridge.cs` diff; `/replay-verify` on merge |
| S34-06 | **Platform Editor Phase H — LinkCatalog Unity** — viewer link list + staging diff + headless propose→approve | team-unity | 2 | S34-01; S34-02; S34-03 | `PlatformLinkCatalogTests` PASS; comms resolve `DisplayName`; schema-only — no sim behavior in Unity |

**Sprint fails** if S34-02/03 do not produce operational LinkCatalog workbook round-trip, S34-06 does not surface LinkCatalog in Unity, or S34-04 does not resolve catalog latency into merger lag without changing default Baltic path.

### Should Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S34-05 | **Link FK + validation rules** — `LINK_ORPHAN_COMMS`, `LINK_TYPE_INVALID`, `LINK_LATENCY_INVALID`; detect-only | team-data | 1 | S34-03 | Deterministic `ValidationReport`; orchestrator surfaces codes; evidence `sprint-34-link-validation-*.md` |
| S34-07 | **Isolated `baltic-patrol-datalink-catalog-latency` fixture** — catalog latency + sharing; pinned isolated golden | team-simulation | 1 | S34-04 | **Not** in ReplayGolden 6/6; `/replay-verify` PASS; production Baltic hash unchanged |
| S34-08 | **`catalog_link_report` CLI** — read-only sorted stdout | team-data | 0.5 | S34-02 | Cli tests PASS; no live-table mutation; mirrors S33-08 contract |
| S34-10 | **Live Editor presentation evidence (Phase H)** — link-catalog viewer + import staging PNGs | team-unity | 1.5 | S34-06 | `production/qa/evidence/*-s34-*.png`; headless filter ≥48/48 PASS |
| S34-11 | **C2 manual sign-off upgrade** — Check 18 LinkCatalog; refresh 14–17 | team-qa | 1 | S34-06; S34-10 | Updated `c2-manual-signoff-*.md`; evidence `sprint-34-c2-signoff-*.md`; headless ≥55/55 |
| S34-13 | **Closeout hygiene** — replay 6/6; GitNexus @ tip; tracker rows 06/15/20/21; smoke ≥1156; prune `stack/sprint33/*` | c-sharp-devops-engineer | 0.5 | S34-02+ | Evidence `sprint-34-gitnexus-*.md`; `smoke-sprint-34-closeout-*.md` |

### Nice to Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S34-09 | **Datalink regression smoke (optional)** — reuse S29/S30/S33/S34 isolated pins + filter suite | team-simulation | 0.5 | S34-04; S34-07 | `Datalink\|Comms\|Contact` PASS; **drop before S34-07** on cut line |
| S34-12 | **CI/local gate refresh (S33-12 carryover)** — `verify-ci-local.ps1` baseline ≥1143 / closeout ≥1156 | c-sharp-devops-engineer | 0.25 | S34-01 | Doc-only; evidence `sprint-34-ci-hygiene-*.md`; non-blocking (5th deferral OK) |

## Carryover from Sprint 33

| Item | S33 status | S34 placement |
|------|------------|---------------|
| LinkCatalog workbook beyond Comms surfacing (Req 21) | Deferred → tracker row 21 | **S34-02, S34-03, S34-06** must-have |
| Datalink delay TR-sensor-004 catalog slice (Req 15) | Scenario lag + comms gate done | **S34-04** must-have; **S34-07** fixture |
| S33-12 CI hygiene | Done (doc) | **S34-12** nice-to-have (5th deferral OK) |
| Kill-chain graph/rules/orchestrator/CLI | **Done** (S33-02/03/05/08) | No S34 carryforward |

## Explicitly Out of Scope

- Full corpora in **`dotnet test` CI** (7208 sensor, 4844 ship, 4403 weapon)
- **TL Phase 5** physical SQLite forks / `TlBranchDatabaseResolver`
- **Full ECCM Phase 2**, catalog onboard ECCM flags, JADC2 node damage
- **Globe map**, **`HYPERSONIC_ALERT` UI**, loadout/magazine Unity surfacing (defer S35)
- **DOTS ECS**, **Cesium production globe**, **CMO mission/scenario import** runtime
- **ZERO touch violation** on `DelegationBridge.cs`
- Dependency-graph **platform→link** edges (plan-only / S35 stretch)

## Should-Have Cut Line

| Cut order | Drop | Keep |
|-----------|------|------|
| 1 | S34-12 (CI hygiene — 5th deferral OK) | S34-03 workbook round-trip |
| 2 | S34-09 (datalink regression — optional) | S34-04 catalog lag resolver |
| 3 | S34-10 (live Editor — lean placeholders) | S34-06 Phase H |
| 4 | S34-08 (link-report CLI) | S34-05 validation rules |
| 5 | S34-11 (C2 live upgrade) | S34-07 catalog-latency fixture |

**Minimum shippable (beyond must-have):** **S34-05** validation rules + **S34-07** isolated fixture + **S34-13** closeout.

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| S34-06 starts before S34-03 merges | Medium | High | Wave plan blocks Unity until data workbook path lands |
| Catalog lag changes default merge | Medium | CRITICAL | Scenario `shareLagTicks` override wins; ReplayGolden 6/6 every sim merge |
| `link_catalog` empty in production DB | Low | Medium | Baltic fixture seed in S34-02; CI-only curated rows |
| Phase H scope creep into sim behavior | Medium | High | Lock S34-06 to viewer + staging diff; sim owns S34-04 |
| `CatalogWriteGate` regression | Medium | CRITICAL | Extend-only; `WriteGate\|LinkCatalog` filter on every data PR |

## GitNexus Rules (Mandatory)

- **CRITICAL extend-only:** `CatalogWriteGate`
- **ZERO touch:** `DelegationBridge.cs`
- **HIGH:** `PlatformWorkbookExporter`, `PlatformWorkbookImporter`, `PlatformCatalogViewerHost`, `DatalinkShareLagResolver`, `DatalinkSidePictureMerger`
- `/replay-verify` mandatory on S34-04, S34-07, S34-09 sim merges

## Quality Gates

```bash
export PATH="/home/username01/.dotnet:$PATH"

# Day-1 (S34-01)
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal

# Data track
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "WriteGate|LinkCatalog|Platform|Comms|DatabaseIntelligence" -v minimal
rg -l "TlBranchDatabase|BranchDatabase" src/ --glob "*.cs" || true

# Sim track
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Datalink|Comms|ShareLag|Contact" -v minimal

# Unity track
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlatformImport|PlatformCatalogViewer|PlatformComms|PlatformLinkCatalog" -v minimal
```

## Epic Themes

| Epic | Goal |
|------|------|
| `sprint-34-link-catalog-data` | LinkCatalog reader + write-gate + workbook round-trip + validation + CLI |
| `sprint-34-datalink-latency` | Catalog→sim share lag resolver + isolated fixture |
| `sprint-34-platform-editor-phase-h` | LinkCatalog Unity surfacing + presentation evidence |
| `sprint-34-presentation-qa` | C2 sign-off upgrade (Check 18) |
| `sprint-34-closeout-devops` | Baseline + CI hygiene + closeout |

## Parallel Planning Artifacts

- `production/agentic/sprint-34-parallel-kickoff-2026-06-19.md`
- `production/agentic/sprint-34-plan-data-2026-06-19.md`
- `production/agentic/sprint-34-plan-sim-2026-06-19.md`
- `production/agentic/sprint-34-plan-unity-2026-06-19.md`
- `production/agentic/sprint-34-plan-devops-qa-2026-06-19.md`

## QA Plan

> ⚠️ **No QA Plan**: This sprint was started without a QA plan. Run `/qa-plan sprint` before the last story is implemented. The Production → Polish gate requires a QA sign-off report, which requires a QA plan.

Target artifact: `production/qa/qa-plan-sprint-34-2026-12-11.md`

## Definition of Done for this Sprint

- [ ] All Must Have tasks completed
- [ ] QA plan exists (`production/qa/qa-plan-sprint-34-*.md`)
- [ ] Smoke check passed (`/smoke-check sprint`)
- [ ] QA sign-off report: APPROVED or APPROVED WITH CONDITIONS (`/team-qa sprint`)
- [ ] No S1 or S2 bugs in delivered features
- [ ] Tracker rows 06, 15, 20, 21 updated for LinkCatalog + catalog latency slice

*Generated via `/sprint-plan new` + dispatching-parallel-agents (2026-06-19). Lean review mode — PR-SPRINT skipped. **Blocking before feature dispatch:** `/qa-plan sprint`. First dev dispatch: `/dev-story dispatch S34-01`.*

**Scope check:** If stories expand beyond epic scope, run `/scope-check [epic]` before implementation.