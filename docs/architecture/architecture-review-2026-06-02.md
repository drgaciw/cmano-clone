# Architecture Review Report

**Date:** 2026-06-02  
**Engine:** Unity 6.3 LTS + .NET 8 headless (`ProjectAegis.sln`)  
**GDDs reviewed (MVP scope):** 6 production GDDs under `design/gdd/`  
**ADRs reviewed:** 10 accepted ADRs under `docs/architecture/`

---

## Requirements design review blockers (C1–C5)

Source: [requirements-13-20-design-review-2026-05-29.md](../../Game-Requirements/reviews/requirements-13-20-design-review-2026-05-29.md)

| Blocker | Requirement | Code / ADR evidence | Status |
|---------|-------------|---------------------|--------|
| **C1** | Order log schema + replay (doc 17) | `DecisionLog` implements `IOrderLog`; ADR-003; `ReplayGolden*` + `/replay-verify` PASS | **Closed** |
| **C2** | Combat outcomes + message log (doc 18) | `EngagementOutcomeRecord`, `MessageLogProjection`, `MessageLogPanelBinder`, Unity `MessageLogPanelHost` | **Closed** (HUD strip; full doc-20 drawer deferred) |
| **C3** | ROE / policy evaluator (doc 19) | `IPolicyEvaluator`, `PassthroughRoeFilter`, `EmconPolicyEvaluator`, scenario JSON ROE | **Closed** (headless MVP) |
| **C4** | EMCON (doc 20) | `EmconPolicyEvaluator`, `ScenarioEmconResolver`, unit radar in scenario JSON | **Closed** (radar slice; full EMCON doctrine UI deferred) |
| **C5** | Human-in-the-loop (doc 13) | `SimulationModeProfile`, `PlayerOrderRecord` in order log; no full pause/override UX | **Deferred** — ADR-001; MVP uses autonomous + Mixed scaffold |

**MVP gate:** C1–C4 satisfied for Baltic vertical slice; C5 explicitly deferred with ADR-001 until Pre-Production UI sprint.

---

## Traceability summary (MVP technical requirements)

| Layer | Requirements (sample) | ADR coverage | Status |
|-------|----------------------|--------------|--------|
| Foundation | Seeded sim, deterministic tick | ADR-001, ADR-004 | Covered |
| Core | Order log, policy, engage | ADR-002, ADR-003, ADR-005 | Covered |
| Sensor | Pd detection, classify FSM, C2 projection | GDD `sensor-detection-ew` + headless tests | Covered |
| Data | Catalog basePd | `platform-db-basepd` GDD + DATA assembly | Partial (import pipeline stretch) |
| UI | Sensor C2 + message log | Unity Toolkit hosts | Partial (OOB/missions/doc-20 shell open) |

**Totals (MVP slice):** ~18 traced requirements — **14 covered**, **3 partial**, **1 gap** (full doc-20 left drawer).

---

## Cross-ADR conflicts

None blocking. `DecisionLog` remains single writer (ADR-003); policy evaluation is pre-engage only (ADR-002); engagement outcomes append-only (ADR-005).

### Recommended ADR implementation order (unchanged)

1. ADR-001 Deterministic simulation  
2. ADR-003 Order log  
3. ADR-002 Policy evaluator  
4. ADR-005 Engagement pipeline  
5. ADR-004 Replay checkpoints  

---

## Engine compatibility

- Unity **6000.3.x** per `docs/engine-reference/unity/VERSION.md`  
- UI Toolkit panels use ListView binding (Unity 6 compatible)  
- No deprecated API flags in reviewed ADRs  

---

## Architecture document coverage

`docs/architecture/architecture.md` — not present. MVP traceability lives in this report and [requirements-traceability.md](requirements-traceability.md). Run `/create-architecture` when expanding beyond 6/20 GDD systems.

---

## Verdict: **CONCERNS**

**Rationale:** MVP spine is architecturally sound and C1–C4 are closed in code. Remaining concerns: 70% GDD backlog, doc-20 full C2 shell, C5 human-in-the-loop UX, and no master `architecture.md`.

### Blocking issues (none for MVP merge)

### Required follow-ups (priority)

1. `/design-system` for remaining 14 requirement docs when entering Production breadth  
2. Doc-20 left drawer (OOB, missions, full message log) — Sprint 3 UI  
3. ADR or story for C5 player override UX when Mixed mode is player-facing  

---

## History

| Date | Verdict | Notes |
|------|---------|-------|
| 2026-06-02 | CONCERNS | Post Sprint 2; C1–C4 closed; C5 deferred |