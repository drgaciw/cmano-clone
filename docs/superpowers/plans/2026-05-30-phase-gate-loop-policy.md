# Phase Gate & Loop Policy Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Gate simulation ticks behind req 02 `Begin Execution` and enforce scenario personality-edit policies at runtime.

**Architecture:** `SimulationPhase` on `SimulationSession` blocks `Tick` during planning; `LoopPolicyGate` holds pure policy rules; `DelegationOrchestrator.TryRebindAgentTraits` applies personality changes through the gate.

**Tech Stack:** .NET 8, NUnit, `ProjectAegis.Delegation`, `ProjectAegis.Sim.Scenario`

**Design:** `docs/superpowers/specs/2026-05-30-phase-gate-loop-policy-design.md`

---

## File map

| File | Role |
|------|------|
| `Orchestration/SimulationPhase.cs` | Phase enum |
| `Orchestration/LoopPolicyGate.cs` | Policy verdict helpers |
| `Orchestration/SimulationSession.cs` | Phase state + tick gate |
| `Orchestration/DelegationOrchestrator.cs` | `TryRebindAgentTraits` |
| `Controllers/AgentController.cs` | `RebindTraits` |
| `Tests/Orchestration/SimulationSessionPhaseTests.cs` | Phase gate tests |
| `Tests/Orchestration/LoopPolicyGateTests.cs` | Policy matrix tests |

---

## Tasks

- [x] Add `SimulationPhase` enum
- [x] Gate `SimulationSession.Tick` on phase; add `BeginExecution`
- [x] Add `LoopPolicyGate` + `TryRebindAgentTraits`
- [x] Update existing session tests to call `BeginExecution`
- [x] Add phase and policy unit tests
- [x] Run full Delegation + Sim test projects (2026-06-02: Delegation 84, Sim 45, Adapter 40 — 0 failures on `main`)
- [ ] Commit (deferred — no user commit request this session)
