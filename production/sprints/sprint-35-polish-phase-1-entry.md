# Sprint 35 — Polish Phase 1 Entry (UX Foundation, Perf P0, C2 Hardening)

**Dates:** 2026-06-20 → 2026-07-03  
**Trunk:** `main` @ `8de98b1` (**1193/1193**; ReplayGolden 6/6; Baltic hash `17144800277401907079`)  
**Predecessor:** Sprint 34 — **COMPLETE** (11/12, QA APPROVED 2026-06-19)  
**Stage:** Production (`production/stage.txt`) — gate **CONCERNS (uplifted)** per [production-to-polish-2026-06-19-r2.md](../gate-checks/production-to-polish-2026-06-19-r2.md)  
**Authority:** [polish-scope-boundary-2026-06-19.md](../polish-scope-boundary-2026-06-19.md) — Phase 1 Baltic + C2 + Platform Editor only

## Sprint Goal

Enter **Polish Phase 1** by closing gate Sprint 0 foundations (accessibility, interaction patterns, difficulty curve, Unity C2 frame budget), landing **sim perf P0** quick wins, and hardening **C2/Platform Editor** player-facing polish — without globe/Cesium, delegation badges, loadout/magazine Unity, TL Phase 5, or tracker MVP-complete claims.

## Capacity

| Metric | Value |
|--------|-------|
| Total days | 10 |
| Buffer (20%) | 2 days reserved |
| **Effective dev-days** | **8** |
| **Commit target** | **8 stories** (6 must + 1 should + closeout) |
| **Plan target** | **14 stories** |
| **Test baseline** | ≥**1193** day-1; closeout target **≥1193** (no regression) |

## Tasks

### Must Have (Critical Path)

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S35-01 | **Full-solution re-baseline** — day-1 build + full sln; record 1193; GitNexus @ trunk; ReplayGolden 6/6 | c-sharp-devops-engineer | 1 | S34-13 done | 0 errors; 1193 PASS; smoke doc; indexed commit recorded |
| S35-02 | **Sprint 35 QA plan** — test matrix, C2 18/18 filters, playtest protocol; blocks feature waves | team-qa | 1 | S35-01 | `production/qa/qa-plan-sprint-35-2026-06-19.md` merged before S35-03+ |
| S35-03 | **UX foundation docs** — `accessibility-requirements.md`, `interaction-patterns.md`, `difficulty-curve.md` | team-ui | 2 | S35-02 | All three files exist; lean review; no new gameplay systems |
| S35-04 | **Unity C2 frame budget baseline** — Profiler capture + panel-bind timing vs 16.67 ms P0 | team-unity | 1.5 | S35-01 | `production/perf/unity-c2-frame-baseline-s35-*.md`; headless regression unchanged |
| S35-05 | **Sim perf P0 — detection hot path** — trial `Dictionary` + pre-sorted trials; remove per-tick `OrderBy` | team-simulation | 3 | S35-01 | ReplayGolden 6/6; Baltic hash unchanged; `/replay-verify` on merge |
| S35-06 | **C2 onboarding + comms/difficulty tooltips** — NPE copy, degrade legend, lag helper text | team-unity | 2 | S35-04 | C2 checks 1–13 proxy ≥61/61; no delegation badges (scope OUT) |
| S35-07 | **C2 sign-off refresh** — checks 1–18 headless proxy + manual doc update | team-qa | 1 | S35-06 | `sprint-35-c2-signoff-*.md`; 18/18 PASS WITH NOTES or PASS |
| S35-14 | **Closeout hygiene** — replay 6/6; GitNexus @ tip; smoke closeout; optional `stage.txt` → Polish | c-sharp-devops-engineer | 0.5 | S35-05+ | `smoke-sprint-35-closeout-*.md`; tracker notes |

**Sprint fails** if S35-02 does not land before feature work, S35-05 breaks ReplayGolden, or C2 proxy drops below **18/18**.

### Should Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S35-08 | **`AegisTokens.uss` + Platform Editor USS consolidation** — Phases C–H token import | team-unity | 1.5 | S35-04 | `Assets/UI/AegisTokens.uss`; Platform filters ≥51/51 PASS |
| S35-09 | **Live Editor presentation evidence** — replace `*-s30..s34-*.png` placeholders | team-unity | 2 | S35-08 | 12 PNGs or lean PASS WITH NOTES per S34-10 |
| S35-10 | **Sim perf P1 — DecisionLog + DatalinkSidePictureMerger LINQ** | team-simulation | 2.5 | S35-05 | Golden output hash-identical; perf appendix optional |
| S35-11 | **Playtest session 7** — structured session + advisory think-aloud | team-qa | 1 | S35-03 | `production/playtests/playtest-2026-06-*-s35-*.md` |
| S35-12 | **Platform Editor C–H validation polish** — diagnostics + rule coverage gaps | team-data | 1 | S35-01 | Deterministic validation messages; extend-only `CatalogWriteGate` |
| S35-13 | **Stage advance Production → Polish** — after user accepts gate CONCERNS | producer | 0.25 | S35-14 | `production/stage.txt` = `Polish` with gate r2 acknowledged |

### Nice to Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|--------------|-------------------|
| S35-15 | **CI/local gate refresh** — `verify-ci-local.ps1` + `tests/unit/` layout audit | c-sharp-devops-engineer | 0.5 | S35-01 | Doc-only disposition; 6th deferral OK |
| S35-16 | **Dependency-graph platform→link edges** — plan-only / ADR sketch | team-data | 0.5 | — | Interface sketch only; no UI runtime (handoff item 8) |
| S35-17 | **Perf re-profile appendix** — delta vs polish baseline | perf-profile | 0.5 | S35-05 | Update `perf-profile-polish-baseline-2026-06-19.md` §benchmarks |

## Carryover from Sprint 34 / Gate

| Item | Source | S35 placement |
|------|--------|---------------|
| Accessibility + interaction patterns missing | Gate r2 residual | **S35-03** must-have |
| Unity C2 16.67 ms frame UNKNOWN | Perf-profile WARNING | **S35-04** must-have |
| Sim LINQ/allocation hotspots P0/P1 | Perf-profile | **S35-05**, **S35-10** |
| Onboarding/comms/difficulty tooltips | Fun hypothesis + playtests | **S35-06** |
| Live Editor PNG placeholders | S34-10 PASS WITH NOTES | **S35-09** should-have |
| `verify-ci-local.ps1` hygiene | S34-12 5× deferred | **S35-15** nice-have |
| Delegation badges UX | Polish scope **OUT** | **Deferred S36+** (handoff item 7) |
| Globe, loadout/magazine, TL Phase 5, corpora CI | Polish scope **OUT** | **Not S35** |

## Explicitly Out of Scope

Per [polish-scope-boundary-2026-06-19.md](../polish-scope-boundary-2026-06-19.md) §Explicitly Out of Scope:

- Cesium/globe production, `HYPERSONIC_ALERT` UI, hypersonic DOTS spawn
- C2 delegation badges + trust emit-only UX (Req 04 — **S36+**)
- Loadout/magazine Unity surfacing (Req 16/21)
- TL Phase 5 physical SQLite forks, full corpora in CI, full ECCM Phase 2
- DOTS/ECS hot-path migration, 5k-entity scale proof
- **ZERO touch** on `DelegationBridge.cs`
- Tracker rows 01–21 → MVP-complete claims

## Should-Have Cut Line

| Cut order | Drop | Keep |
|-----------|------|------|
| 1 | S35-15 (CI hygiene — 6th deferral OK) | S35-05 sim P0 |
| 2 | S35-16 (dependency-graph plan-only) | S35-06 C2 tooltips |
| 3 | S35-09 (live Editor — lean placeholders OK) | S35-08 AegisTokens |
| 4 | S35-11 (playtest session 7) | S35-07 C2 sign-off |
| 5 | S35-10 (P1 LINQ) | S35-03 UX foundation |

**Minimum shippable (beyond must-have):** **S35-08** tokens + **S35-10** P1 LINQ (if capacity) + **S35-13** stage advance (if user confirms).

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| S35-02 delayed → feature waves start blind | Medium | High | Block S35-03+ until QA plan merged |
| S35-05 sim change alters Baltic hash | Medium | CRITICAL | `/replay-verify` every sim PR; isolated fixtures only for new pins |
| Unity Profiler unavailable on Linux CI host | High | Medium | Headless panel-bind timing test + defer live Profiler to Editor host |
| UX docs expand into gameplay scope | Low | High | Polish boundary §Cut Line Rules; doc-only acceptance |
| Stage advance without user ack | Low | Medium | S35-13 optional; requires explicit user confirmation |

## GitNexus / Hard Gates (Mandatory)

- **CRITICAL extend-only:** `CatalogWriteGate`
- **ZERO touch:** `DelegationBridge.cs`
- **HIGH:** `PdDetectionContactSimulator`, `DeterministicDetectionLoop`, `DecisionLog`, `DatalinkSidePictureMerger`, C2 USS hosts
- `/replay-verify` mandatory on **S35-05**, **S35-10** sim merges
- Production Baltic hash `17144800277401907079` immutable unless signed refresh

## Definition of Done

- [ ] All Must Have tasks completed
- [ ] QA plan exists: `production/qa/qa-plan-sprint-35-2026-06-19.md`
- [ ] Smoke check PASS: `production/qa/smoke-sprint-35-closeout-*.md`
- [ ] QA sign-off: APPROVED or APPROVED WITH CONDITIONS
- [ ] C2 proxy **18/18** maintained
- [ ] ReplayGolden **6/6**; full sln **≥1193/1193**
- [ ] UX foundation trio committed (`accessibility`, `interaction-patterns`, `difficulty-curve`)
- [ ] No S1/S2 bugs in delivered paths

## Producer Feasibility Gate

**PR-SPRINT skipped — Lean mode** (`production/review-mode.txt`). Plan validated via parallel domain agents (data, sim, unity, UX/QA, devops) 2026-06-19.

> ⚠️ **QA Plan**: Run `/qa-plan sprint` as **S35-02** before any feature implementation. Production → Polish gate requires QA sign-off at sprint close.

> **Scope check:** Run `/scope-check` if any story cites tracker "next stack" without a [polish-scope-boundary](../polish-scope-boundary-2026-06-19.md) in-scope path.

## Related Artifacts

| Artifact | Path |
|----------|------|
| Parallel kickoff | [sprint-35-parallel-kickoff-2026-06-19.md](../agentic/sprint-35-parallel-kickoff-2026-06-19.md) |
| Domain plans | `production/agentic/sprint-35-plan-{data,sim,unity,devops-qa}-2026-06-19.md` |
| Gate r2 | [production-to-polish-2026-06-19-r2.md](../gate-checks/production-to-polish-2026-06-19-r2.md) |
| Perf baseline | [perf-profile-polish-baseline-2026-06-19.md](../perf/perf-profile-polish-baseline-2026-06-19.md) |