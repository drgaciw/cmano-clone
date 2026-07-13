# Sprint 29 — Operationalize Data-to-Fight Loop

**Dates:** 2026-10-02 → 2026-10-15  
**Trunk:** `main` @ `1d93e86` (Sprint 28 complete; 801/801 baseline; QA APPROVED)  
**Predecessor:** Sprint 28 — CMO Corpus v2 + Platform Write Path + Combat Phase 2 (complete, 13/13)

## Sprint Goal

Operationalize the **data-to-fight loop**: TL export foundation, nightly corpus **approve** workflow, Unity Platform Editor import UX, and bounded **combat-domains Baltic enablement**—without hot-tick damage, full-corpus CI load, or TL production forks.

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
| S29-01 | **Full-solution re-baseline** — day-1 `dotnet build` + `dotnet test ProjectAegis.sln`; record ≥801 in `sprint-status.yaml` + smoke doc; GitNexus @ trunk | c-sharp-devops-engineer | 1 | S28 merged | 0 errors; ≥801/801 PASS; ReplayGolden 6/6; indexed commit recorded |
| S29-02 | **TL export Phase 1–2** — `tlTier` on export manifests; migration `007` `catalog_snapshot.branch` (`TL-0`…`TL-5`); metadata only; **no** runtime `tlBranch` binding | team-data | 2 | S29-01 | Migration applies; export drops carry `tlTier`; `rg TlBranch\|BranchDatabase` → zero; evidence spike Phases 1–2 |
| S29-03 | **Nightly corpus approve workflow** — curator `ApproveBatch` path for platform v2 nightly propose runs; `RecordRelease` + snapshot hash; off-CI only | team-data | 2 | S29-01 | Nightly job → propose → approve evidence doc; CI stays curated slices; WriteGate regression PASS |
| S29-04 | **Platform Editor Phase E — Unity import UI** — in-engine import → propose → approve atop `PlatformWorkbookWriteBridge`; no write-gate bypass | team-unity + team-data | 2.5 | S29-03 | Headless + viewer tests PASS; staging review UX wired; ZERO touch `DelegationBridge` |

**Sprint fails** if S29-03 approve workflow does not land through `CatalogWriteGate` with pinned snapshot evidence.

### Should Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S29-05 | **Combat domains Baltic enablement** — `combatDomainsEnabled=true` on isolated Baltic golden; `/replay-verify`; keep `combat-domains-smoke` on separate pin | team-simulation | 1.5 | S29-01 | New golden hash pinned; ReplayGolden 6/6; production Baltic fixture policy documented |
| S29-06 | **Catalog Phase B → sim consumption** — wire mobility/signatures/EMCON catalog rows into bounded validation/readiness paths | team-data + team-simulation | 2 | S29-03 | Sim tests PASS with catalog-sourced metadata; no direct SQLite in Sim |
| S29-07 | **Doctrine Inheritance Panel visual sign-off** — Editor/PlayMode evidence for ADR-010 panel (closes S22/S23 deferred gate) | team-unity | 1 | S29-01 | Headless tests unchanged; evidence `production/qa/evidence/doctrine-panel-s29-*.png` or lean proxy doc |
| S29-08 | **Begin Execution UX** — Planning→Executing phase control in C2 top bar / PlayMode harness | team-unity | 1 | S29-01 | Phase transition tests PASS; score/loss counters frozen until execution |
| S29-13 | **Closeout hygiene** — replay 6/6; GitNexus @ stack tip; tracker rows; smoke doc | c-sharp-devops-engineer | 0.5 | S29-03+ | Evidence `sprint-29-gitnexus-*.md`; ≥801 closeout |

### Nice to Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S29-09 | **Damage hot-tick apply (bounded)** — extend S28-08 readiness wire to tick-level catalog damage (no full BDA component model) | team-simulation | 1.5 | S29-05 | Sim tests PASS; `/replay-verify`; no hot-path SQLite |
| S29-10 | **Balance drift in catalog pipeline** — surface `enableBalanceDrift` advisory on import/approve diffs (Sim default stays false) | team-data | 0.5 | S29-01 | Pipeline tests PASS; Sim golden unchanged |
| S29-11 | **Datalink side picture (TR-sensor-004)** — bounded contact sharing + deterministic merge order | team-simulation | 1 | S29-01 | `ContactChange` order-log tests PASS; ReplayGolden 6/6 |
| S29-12 | **CI/local gate refresh** — update `verify-ci-local.ps1` for 801+ baseline | c-sharp-devops-engineer | 0.25 | S29-01 | Doc-only; non-blocking |

## Carryover from Sprint 28

| Item | S28 status | S29 placement |
|------|------------|---------------|
| TL production forks (TL-0–TL-5 SQLite) | S28-11 PROCEED export-only; runtime deferred | **S29-02 must-have** (Phases 1–2 only); Phase 4 binding deferred |
| Nightly approve (propose-only v2) | S28-02 propose-only | **S29-03 must-have** |
| Unity Excel import UI chrome | Out of scope (Phase D CLI only) | **S29-04 must-have** |
| `combatDomainsEnabled=true` on Baltic | Out of scope | **S29-05 should-have** |
| Hot-tick world-state damage | Out of scope (S28-08 readiness only) | **S29-09 nice-to-have** |
| Full 7208 `sensor.md` in CI | Still out of scope | Remains nightly-only |
| S28-07 Editor export screenshot | QA advisory | **S29-07** or polish evidence alongside Phase E |

## Explicitly Out of Scope

- **Full 7208-record `sensor.md` in `dotnet test` CI**
- **TL runtime scenario binding** (`tlBranch` field, `TlBranchDatabaseResolver`) — Phase 4 deferred post-S29
- **Hot-tick full BDA component model** / mine-land-facility full runtime
- **CMO mission/scenario import** (Req 11 Phase 2/3)
- **ECCM Phase 2**, **JADC2 node damage**, **swarm sector coordinator**
- **DOTS ECS migration**, **Cesium production globe**
- **ZERO touch violation** on `DelegationBridge.cs`

## Should-Have Cut Line

| Cut order | Drop | Keep |
|-----------|------|------|
| 1 | S29-09 (hot-tick damage) | S29-05 Baltic enable |
| 2 | S29-08 (Begin Execution) | S29-06 catalog sim consumption |
| 3 | S29-07 (doctrine visual) | S29-04 import UI |
| 4 | S29-11 (datalink) | S29-02 TL export |

**Minimum shippable (beyond must-have):** S29-05 + S29-13.

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| `CatalogWriteGate` extend-only violation (migration 007 + import UI) | Medium | CRITICAL | `gitnexus impact` before edit; WriteGate regression every data merge |
| ADR-009 Baltic golden drift (S29-05 flag-on) | Medium | CRITICAL | Separate pinned hash; keep smoke fixture isolated; `/replay-verify` mandatory |
| Parallel track overload (data + Unity + combat) | Medium | High | S28-style wave dispatch; cut line before nice-to-have; 20% buffer |
| Corpus scope creep into CI | High | High | Nightly + curated `--max-records` only; producer sign-off |

## GitNexus Rules (Mandatory)

- **CRITICAL extend-only:** `CatalogWriteGate`
- **ZERO touch:** `DelegationBridge.cs`
- **HIGH:** `CmoMarkdownImporter`, `PlatformWorkbookWriteBridge`, `ICatalogReader`, `DomainValidatorRegistry`
- `/replay-verify` mandatory on S29-05, S29-09 sim merges

## Producer Constraints (Inherited from S28)

1. **Nightly corpus** — full sensor corpus stays off-CI; approve workflow off-CI
2. **GHA billing** — Buildkite merge authority; local-gate advisory permanent
3. **ADR-011 Excel-primary** — all writes via workbook + write gate
4. **TL export-only until Phase 4** — no physical branch DBs in S29

## Quality Gates

```bash
export PATH="/home/username01/.dotnet:$PATH"

# Day-1 (S29-01)
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal

# Data track
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "CmoMarkdown|WriteGate|Platform|CatalogImport|Excel|Snapshot" -v minimal
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "CatalogImport|Platform" -v minimal

# Sim track
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Combat|Domain|Damage|Readiness|Magazine|Datalink" -v minimal

# Unity track
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlayModeSmoke|PlatformCatalog|Doctrine|Excel" -v minimal
```

## Epic Themes

| Epic | Goal |
|------|------|
| `sprint-29-tl-export-foundation` | TL export Phases 1–2 (migration 007 + manifest) |
| `sprint-29-corpus-approve` | Nightly propose→approve curator workflow |
| `sprint-29-platform-editor-phase-e` | Unity import UI atop Phase D write path |
| `sprint-29-combat-domains-phase3` | Baltic combat enable + bounded hot-tick damage |
| `sprint-29-catalog-sim-bridge` | Phase B catalog rows → sim validation |
| `sprint-29-c2-core-loop` | Begin Execution + doctrine visual polish |
| `sprint-29-closeout-devops` | Closeout hygiene + CI doc refresh |

## Parallel Planning Artifacts

- `production/agentic/sprint-29-parallel-kickoff-2026-06-18.md` *(create at kickoff)*
- `production/agentic/sprint-29-plan-data-2026-06-18.md` *(create at kickoff)*
- `production/agentic/sprint-29-plan-unity-2026-06-18.md` *(create at kickoff)*

## QA Plan

`production/qa/qa-plan-sprint-29-2026-10-02.md`

## Definition of Done for this Sprint

- [ ] All Must Have tasks completed
- [ ] All tasks pass acceptance criteria
- [ ] QA plan exists (`production/qa/qa-plan-sprint-29-*.md`)
- [ ] All Logic/Integration stories have passing unit/integration tests
- [ ] Smoke check passed (`/smoke-check sprint`)
- [ ] QA sign-off report: APPROVED or APPROVED WITH CONDITIONS (`/team-qa sprint`)
- [ ] No S1 or S2 bugs in delivered features
- [ ] Design documents updated for any deviations
- [ ] Code reviewed and merged

*Created following `/sprint-plan new` + dispatching-parallel-agents (2026-06-18). Lean review mode — PR-SPRINT skipped. Run `/qa-plan sprint` before Wave 1.*

**Scope check:** If stories expand beyond epic scope, run `/scope-check [epic]` before implementation.