# Sprint 19 — 2026-06-18 to 2026-07-02

> **[Producer] Review (PR-SPRINT)**: CONCERNS (accepted) — 2026-06-08. User proceeded as planned; capacity ~8.5d vs 8 available.

**Trunk:** `main` @ `7401fac` (S19-01 C2 sign-off PASS 2026-06-08)  
**Predecessor:** Sprint 18 — carryover S18-01 → S19-01 (C2 sign-off)  
**Review mode:** `full` (per-run override)

## Sprint Goal

Close the Unity C2 operator loop, ship Catalog Phase 2 bulk import (P2-2/P2-3), and wire the OSINT proposal path end-to-end — while keeping sim/delegation green.

## Capacity

- **Total days:** 10 working days
- **Buffer (20%):** 2 days reserved for unplanned work
- **Available:** 8 effective dev-days (plus ~0.5 day human Editor time for C2 sign-off)

## Tasks

### Must Have (Critical Path)

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|---------------------|
| S19-01 | Execute Unity C2 manual sign-off | `team-qa` + human | 0.5 | `PLAYMODE-SMOKE`, runbook ready | 13/13 checks PASS recorded in `production/qa/c2-manual-signoff-2026-06-02.md` |
| S19-02 | Catalog P2-2 bulk chunked import | `team-data` | 2 | P2-1 CLI on `main` | 500-sensor batches, quarantine report, `CmoMarkdown` tests green |
| S19-03 | Catalog P2-3 snapshot binding | `team-data` | 1.5 | S19-02 | `ScenarioPackage.dbSnapshotId` + stable hash test; replay golden unchanged unless fixtures change |
| S19-04 | Wave-5 epic + tracker closure | Producer | 0.5 | S19-01 | `wave5-engage-cyber-logistics-slice` → Complete; tracker rows 14/16/19 updated |

### Should Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|---------------------|
| S19-05 | OSINT digest runner (headless) | `team-data` | 2 | `OsintProposalGate` on `main` | Fixture digest → proposal batch via write gate; no wall-clock in hot path |
| S19-06 | GDD refresh wave (3 systems) | Design + `/design-system` | 2 | `systems-index` gaps | `simulation-core-time`, `logistics-magazines`, `engagement-fire-control` → design-review ready |
| S19-07 | GitHub Actions billing / local gate SOP | Producer | 0.5 | — | Required check enabled **or** documented fallback (`sprint-18-ci-local-gate`) ratified |

### Nice to Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|---------------------|
| S19-08 | Cesium Phase B spike scene | `team-unity` | 1 | Unity Editor | Scene loads per `CESIUM-SPIKE-SETUP.md` |
| S19-09 | Hindsight retain Sprint 18 outcomes | `hindsight-dev` | 0.25 | Hindsight `:8888` up | `dev-cmano-clone` bank has Sprint 18 closeout |

## Carryover from Previous Sprint

| Task | Reason | New Estimate |
|------|--------|--------------|
| S18-01 Unity C2 manual sign-off | Human Editor only; automated proxy green | 0.5 day (→ S19-01) |
| S16-pr-ci GitHub Actions billing | Blocked since Sprint 16 | 0.5 day (→ S19-07) |

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| `DelegationBridge` CRITICAL blast radius | Medium | High | `gitnexus impact` before sim edits; bias sprint toward data-only work |
| Human C2 sign-off slips again | High | Medium | Schedule Editor session Day 1; headless proxy already green |
| P2-2 scope creep (full 7208 sensors) | Medium | Medium | `maxRecords` + curated slice in CI only |
| GitHub Actions billing unresolved | High | Low | Local gate SOP exists (`sprint-18-ci-local-gate`) |
| team-data lane contention (P2 + OSINT) | Medium | Medium | Sequence P2-2 → P2-3 before OSINT e2e |

## Dependencies on External Factors

- Unity **6000.3.14f1** Editor for S19-01 (human-only)
- P2-1 `catalog_import_markdown` on `main` (merged @ `f7e6fa6`)
- No Node toolchain for catalog import path
- Hindsight server optional for S19-09

## Definition of Done for this Sprint

- [ ] All Must Have tasks completed
- [ ] All tasks pass acceptance criteria
- [ ] QA plan exists (`production/qa/qa-plan-sprint-19-2026-06-08.md`)
- [ ] All Logic/Integration stories have passing unit/integration tests
- [ ] Smoke check passed (`/smoke-check sprint`)
- [ ] QA sign-off report: APPROVED or APPROVED WITH CONDITIONS (`/team-qa sprint`)
- [ ] No S1 or S2 bugs in delivered features
- [ ] Design documents updated for any deviations
- [ ] Code reviewed and merged

## GitNexus Rules

| Symbol | Risk | Sprint touch |
|--------|------|--------------|
| `DelegationBridge` | **CRITICAL** | Avoid unless required for sign-off |
| `CatalogWriteGate` | **HIGH** | S19-02, S19-03, S19-05 |
| `ICatalogReader` | **HIGH** | S19-03 snapshot binding |

## Quality Gates

```powershell
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests
```

Replay when touching sim/delegation: `--filter ReplayGolden|ReplayOrderLog`

## Parallel worktree layout (suggested)

```
main
 ├── stack/sprint19/c2-signoff          (S19-01 docs only)
 ├── stack/data/phase2-bulk              (S19-02)
 ├── stack/data/phase2-bind              (S19-03)
 └── stack/sprint19/osint-digest         (S19-05)
```

## References

- `production/sprints/sprint-18-kickoff.md`
- `docs/superpowers/plans/2026-06-04-catalog-phase2-import.md`
- `production/agentic/sprint-18-osint-spike-2026-06-04.md`
- `production/qa/sprint-18-c2-signoff-runbook-2026-06-04.md`