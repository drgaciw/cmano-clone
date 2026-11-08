# Sprint 26 — CMO Phase 2 Import + Presentation Closeout

**Dates:** 2026-08-21 → 2026-09-03  
**Trunk:** `main` @ `76b57e6` (Sprint 25 stack merged; 661/661 baseline)  
**Predecessor:** Sprint 25 — Phase B Damage Model + Presentation Assurance (complete, 14/14)

## Sprint Goal

Land **bounded CMO weapon/platform markdown import** through the extend-only write gate (Req 06 Phase 2 completion), while closing **S24/S25 presentation advisory gaps** and advancing **catalog-to-sim readiness** without opening ADR-009 runtime combat scope.

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
| S26-01 | **Full-solution re-baseline** — day-1 `dotnet build` + `dotnet test ProjectAegis.sln`; record ≥661 in `sprint-status.yaml` + smoke doc; GitNexus analyze @ trunk | c-sharp-devops-engineer | 1 | S25 merged | 0 errors; 0 failures; ReplayGolden 6/6; indexed commit recorded |
| S26-02 | **CMO weapon markdown import CLI** — extend `catalog_import_markdown` for weapon entity; `CmoMarkdownImporter` → `ProposeWeaponBatch` → `ApproveBatch`; curated slice + `--max-records` | team-data | 2 | S26-01 | ≥50 weapons from curated slice staged+approved; quarantine JSON; no direct SQLite writes |
| S26-03 | **CMO platform markdown import CLI** — platform class rows from curated slice → `ProposePlatformBatch` (extend-only) | team-data | 2 | S26-02 | Platform rows import E2E; stable sort; chunk policy 500/batch |
| S26-04 | **Import E2E + golden hygiene** — empty-diff re-import; validation hash golden; snapshot hash stable; sensor+Phase A/B+damage regression unchanged | team-data | 1.5 | S26-03 | Re-import empty diff; WriteGate regression PASS; replay 6/6 unchanged |

**Sprint fails** if S26-04 weapon/platform round-trip does not land through the write gate.

### Should Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S26-05 | **Damage sim consumer wire (bounded)** — connect `PhaseBCatalogDamageReadinessStub` to policy/readiness evaluation path; **no BDA, no hot-tick damage apply** | team-simulation | 1.5 | S26-01 | Sim tests PASS; `/replay-verify` 6/6; `combatDomainsEnabled` stays false |
| S26-06 | **APP-6 texture atlas asset pack** — Addressables/USS sprite sheet beyond S25-08 vector MVP; expand `App6Sidc` registry | team-unity | 1.5 | S26-01 | Headless App6 tests PASS; spike doc PROCEED; ZERO touch `DelegationBridge` |
| S26-07 | **Unity Editor presentation evidence closeout** — live `cesium-s26-*.png`; tri-batch log (comms/classify/doctrine); billboard capture | team-unity | 1 | S26-01 | Clears S25-09/11/14 **APPROVED WITH CONDITIONS**; headless remains merge authority if Editor unavailable |
| S26-08 | **C2 signoff tooling: `-Scenario doctrine`** — extend `Invoke-C2PlayModeSignoffBatch.ps1` for doctrine batch | team-unity | 0.5 | S26-07 | Script accepts `-Scenario doctrine`; `RunDoctrineBatch` documented |

### Nice to Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S26-09 | **ADR-009 acceptance + validator stubs** — accept ADR-009; `IDomainValidator` no-op stubs + `combatDomainsEnabled` flag; **no runtime BDA** | c-sharp-architect + team-simulation | 1.5 | S26-01 | ADR-009 Accepted; golden replay abort set empty |
| S26-10 | **ADR-011 Phase C platform viewer spike** — read-only browse `ICatalogReader` platform rows in Unity; no edit path | team-unity | 1.5 | S26-04 | PROCEED/DEFER doc; zero write-gate bypass |
| S26-11 | **Closeout hygiene** — replay 6/6; GitNexus @ stack tip; tracker row 06 import progress; prune `stack/sprint25/*` | c-sharp-devops-engineer | 0.5 | S26-04+ | Evidence in `sprint-26-gitnexus-*.md` |

## Carryover from Sprint 25

| Item | S25 status | S26 placement |
|------|------------|---------------|
| CMO weapon/platform markdown import | Explicit defer | **S26-02..04 must-have** |
| APP-6 texture atlas asset pack | S25-08 deferral | **S26-06 should-have** |
| Live Unity Editor evidence | S25-09/11/14 CONDITIONS | **S26-07 should-have** |
| Damage sim beyond stub | S25-13 stub only | **S26-05 should-have (bounded)** |
| ADR-011 Phase C viewer | Out of scope S25 | **S26-10 nice-to-have spike** |
| GitHub Actions billing | Open since S16 | **S26-11 / CI hygiene** |

## Explicitly Out of Scope

- **Full 7208-record sensor.md CI load** (use curated slice + nightly job)
- **Runtime damage application / BDA** (ADR-009 combat domain — unless ADR-009 accepted with stub-only scope)
- **Component-level damage tables**
- **C5 human override UX**
- **CMO mission/scenario import** (doc 11 Phase 2/3)
- **TL-0–TL-5 branch databases**
- **ZERO touch violation** on `DelegationBridge.cs`

## Should-Have Cut Line

| Cut order | Drop | Keep |
|-----------|------|------|
| 1 | S26-10 (Phase C viewer spike) | Import path + S26-06 |
| 2 | S26-09 (ADR-009 stubs) | Must-have import |
| 3 | S26-08 (doctrine signoff script) | S26-07 minimum evidence |
| 4 | S26-05 (damage sim wire) | Import E2E only |

**Minimum shippable (beyond must-have):** S26-05 + S26-11.

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| CMO import scope creep (full corpus) | High | High | Curated slice + `--max-records`; chunk 500; human approve per batch |
| `CatalogWriteGate` extend-only violation | Medium | CRITICAL | `gitnexus impact` before edit; full WriteGate regression |
| Replay determinism regression | Medium | CRITICAL | `/replay-verify` on sim merges; Baltic fixture unchanged unless intentional |
| ADR-009 runtime creep | High | CRITICAL | ADR-009 acceptance required before BDA; `combatDomainsEnabled=false` default |
| Unity Editor unavailable | Medium | Medium | Headless proxy = merge authority; Editor evidence = CONDITIONS |

## GitNexus Rules (Mandatory)

- **CRITICAL extend-only:** `CatalogWriteGate`
- **ZERO touch:** `DelegationBridge.cs`
- **HIGH:** `ICatalogReader`, `CmoMarkdownImporter`, `PlatformWorkbookImporter`
- `/replay-verify` mandatory on S26-05 sim consumer merge

## Producer Decisions Needed

1. **CMO Phase 2 scope** — bounded weapon+platform slice (recommended) vs spike-only
2. **ADR-009** — accept + stub validators in S26 vs defer combat runtime to S27
3. **GitHub Actions billing** — remediate vs permanent advisory by Day 2

## Quality Gates

```bash
export PATH="/home/username01/.dotnet:$PATH"

# Day-1 (S26-01)
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal

# Data track
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "CmoMarkdown|WriteGate|Platform|CatalogImport" -v minimal
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "CatalogImport" -v minimal

# Unity track
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlayModeSmoke|Doctrine|MapPanelBinder|App6|Cesium" -v minimal
```

## QA Plan

`production/qa/qa-plan-sprint-26-2026-06-18.md`

## Closeout (2026-06-18)

**Verdict:** Sprint 26 **COMPLETE** — 11/11 stories done (must + should + nice-to-have).

| Gate | Result |
|------|--------|
| Full solution tests | **698/698 PASS** |
| ReplayGolden | **6/6 PASS** |
| GitNexus | 10,656 nodes / 22,048 edges |
| `DelegationBridge.cs` | ZERO touch |

**Evidence:** `production/qa/smoke-sprint-26-closeout-2026-06-18.md`, `production/qa/sprint-26-gitnexus-2026-06-18.md`, `production/agentic/stacks/sprint26/S26-*-DONE.md`

**Carry to S27:** ADR-009 runtime BDA (still stub-only); ADR-011 Phase C full editor UX; full CMO corpus nightly import; GitHub Actions billing remediation.