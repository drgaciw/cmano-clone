# Phase Gate & Loop Policy Enforcement — Design Spec

**Date:** 2026-05-30  
**Project:** Project Aegis  
**Status:** Approved for implementation  
**Source:** `Game-Requirements/requirements/02-Core-Gameplay-Loop.md`, `docs/superpowers/specs/2026-05-30-core-gameplay-loop-decisions-design.md`

---

## 1. Goal

Implement runtime support for req 02 Phase 2→3 transition and scenario loop policies already stored on `ScenarioPolicyProfile`.

**In scope (vertical slice):**
- `SimulationPhase` (`Planning` | `Executing`) on `SimulationSession`
- `BeginExecution()` gate — no delegation/sim ticks while `Planning`
- `LoopPolicyGate` — pure policy checks for personality edit rules
- `DelegationOrchestrator.TryRebindAgentTraits` — enforced personality hot-swap
- Unit tests for phase gate and policy matrix

**Out of scope (follow-on):**
- Unity `DelegationBridge` phase wiring
- Live HUD info filtering for `delegationFog` / `tieredByAutonomy`
- Planning-phase optional clock unpause
- `tieredRebrief` sim-time cost mechanics

---

## 2. Architecture

| Component | Layer | Responsibility |
|-----------|-------|----------------|
| `SimulationPhase` | Delegation.Orchestration | Session phase enum |
| `SimulationSession` | Delegation.Orchestration | Owns phase; gates `Tick` |
| `LoopPolicyGate` | Delegation.Orchestration | Pure scenario-policy rules |
| `AgentController.RebindTraits` | Delegation.Controllers | Mutable personality vector |
| `DelegationOrchestrator.TryRebindAgentTraits` | Delegation.Orchestration | Gate + rebind + log hook point |

Phase lives on `DelegationOrchestrator` (May 30 follow-up) so `SimulationSession` and `DelegationBridge` share one gate. Agent-vs-agent mode auto-calls `BeginExecution()` in `SimulationModeConfigurator`. Unity smoke host uses `autoBeginOnStart` (default true); production RTwP sets it false and wires a UI button to `DelegationBridgeHost.BeginExecution()`.

---

## 3. Behavior

### Phase gate

1. New session starts in `Planning`.
2. `Tick(state)` is a no-op while `Planning` (no orchestrator tick, no sim tick, no log entries).
3. `BeginExecution()` transitions to `Executing` (idempotent).
4. Agent-vs-agent headless callers invoke `BeginExecution()` after setup (same as player pressing the button).

### Personality edit policy

| Policy | Planning | Executing (Manual/Assisted) | Executing (Semi+/Full) |
|--------|----------|----------------------------|-------------------------|
| `Anytime` | allow | allow | allow |
| `PlanningOnly` | allow | deny | deny |
| `TieredRebrief` | allow | allow | deny (rebrief deferred) |

Autonomy changes always allowed (req 02).

### Player info model

`LoopPolicyGate.ResolvePlayerInfoModel(profile)` returns effective model for UI layer. No filtering in this slice — contract only.

---

## 4. Testing

- Planning ticks produce empty decision log
- After `BeginExecution`, existing session tests behave as before
- Policy matrix tests for `CanEditPersonality`
- Orchestrator rebind denied when `planningOnly` + executing

---

**Approved:** May 30, 2026
