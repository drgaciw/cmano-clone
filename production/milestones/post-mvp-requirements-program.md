# Milestone: Post-MVP Requirements Program (Sprints 11–15)

**Status:** **100% complete** @ 2026-06-08 closeout (S11–15 program + Wave-5 on `main`)
**Target:** 2026-07-01 → 2026-08-15 (accelerated execution)  
**Type:** Requirements maturity (docs 01–12) + Wave-5 gameplay implementation (req 14, 16, 19, 20)  
**Indexed:** GitNexus `cmano-clone` (refresh with `npx gitnexus analyze` per sprint)  
**Tracker:** [Game-Requirements/implementation-tracker-2026-06-04.md](../../Game-Requirements/implementation-tracker-2026-06-04.md)  
**Doc plan:** [Agentic-Development-Plan.md](../../Agentic-Development-Plan.md)

## Program goal

1. **Mature** requirement documents 01–12 (stubs/partials → design-review ready) without reopening locked superpowers specs.  
2. **Implement** Wave-5 P0 gaps from the tracker: cyber spoof runtime (19), live unit readiness (16), interactive attack menu (14/20).  
3. **Refresh** RTM and architecture review after both tracks converge.

## Sprint map

| Sprint | Dates (proposed) | Primary deliverable |
|--------|------------------|---------------------|
| [11](../sprints/sprint-11-program-kickoff.md) | 2026-06-04 → 2026-06-18 | Baseline + QA gate closure + epic/story readiness |
| [12](../sprints/sprint-12-requirements-foundation.md) | 2026-06-18 → 2026-07-02 | Docs 01–03 (foundation) |
| [13](../sprints/sprint-13-wave5-spoof-readiness.md) | 2026-07-02 → 2026-07-16 | Req 19 spoof + req 16 live readiness (headless) |
| [14](../sprints/sprint-14-wave5-attack-menu.md) | 2026-07-16 → 2026-07-30 | Req 14/20 interactive attack menu |
| [15](../sprints/sprint-15-requirements-rtm-gate.md) | 2026-07-30 → 2026-08-13 | Docs 04–06, 12, consistency, RTM gate |

## GitNexus blast radius (pre-plan)

| Symbol | Risk | Sprint touch |
|--------|------|----------------|
| `DelegationBridge` | **CRITICAL** (77 upstream) | 13–14 — spoof/readiness/engage wiring |
| `EngagePreviewProjection` | LOW | 13–14 — abort preview |
| `EngageAttackOptions` | LOW | 14 — menu entries |
| `SimulationSession` | HIGH (verify before edit) | 13 — readiness + spoof hooks |

**Rule:** `npx gitnexus impact <Symbol> -d upstream -r cmano-clone` before every sim/delegation edit; `/replay-verify` after harness changes.

## Quality gates (all sprints)

| Gate | Command / skill |
|------|-----------------|
| Build + test | `dotnet restore` → `dotnet test ProjectAegis.sln -v minimal` |
| Play Mode smoke | Filter `PlayModeSmokeHarnessTests` |
| Determinism | `/replay-verify` when order log / engage changes |
| Doc review | `/design-review` per edited requirement file |
| Pre-merge | `npx gitnexus detect-changes -r cmano-clone` |

## Success criteria

- [x] Requirements 01–06, 12 at **Ready for design review** or **Locked** with superpowers cross-links — [sprint-15-design-review-2026-06-04.md](../qa/sprint-15-design-review-2026-06-04.md) APPROVED  
- [x] Tracker rows 14, 16, 19 updated to **Partial+** with automated AC tests cited — [implementation-tracker-2026-06-04.md](../../Game-Requirements/implementation-tracker-2026-06-04.md) rows 14/16/19/20  
- [x] `docs/architecture/requirements-traceability.md` rows for 01–12 (FULL / PARTIAL / STUB) — all rows **FULL** @ S15 gate  
- [x] `docs/reports/requirements-consistency-*.md` — 0 BLOCKER — [requirements-consistency-2026-06-04.md](../../docs/reports/requirements-consistency-2026-06-04.md)  
- [x] Unity manual C2 sign-off executed — **Complete** S19-01 @ `7401fac` (2026-06-08): `Invoke-C2PlayModeSignoffBatch.ps1` + headless proxy 13/13; [c2-manual-signoff-2026-06-02.md](../qa/c2-manual-signoff-2026-06-02.md)

## Out of scope (this program)

- Req 05 OSINT speculative agent (deferred to Sprint 16+ spike)  
- Full CMO catalog import Phase 2  
- Cesium globe production map  
- GDD authoring for 01–12 (follow-on per Agentic-Development-Plan)