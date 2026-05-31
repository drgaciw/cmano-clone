# Simulation Modes — Resolved Decisions

**Date:** 2026-05-30  
**Project:** Project Aegis (cmano-clone)  
**Status:** Approved design — documentation pass (Approach 1); code hooks in DELEG-7–9  
**Source requirement:** `Game-Requirements/requirements/03-Simulation-Modes.md`  
**Related:** `04-Agent-Delegation.md`, `17-Replay-AAR-And-Order-Log.md`, `2026-05-28-agent-delegation-framework-design.md`

---

## 1. Purpose

Resolves the three open questions in req 03. Modes remain controller configuration per the delegation framework design — no fourth `SimulationModeKind`.

---

## 2. Locked decisions

| # | Question | Decision |
|---|----------|----------|
| 1 | Dual-side control in Mixed Mode? | **Scenario-gated only.** `allowDualSideControl: true` in scenario policy; default **false**. |
| 2 | Minimum AvA batch speed? | **256× effective** headless = batch-analysis floor. **1000×+** = performance **target**, not minimum. |
| 3 | Spectator Mode? | **Not a fourth enum.** AvA + optional **`AttachReplayViewer`** session flag — read-only replay UI on order log + checkpoints. |

---

## 3. Dual-side control

- Default Mixed: one human side via `SimulationModeProfile.PlayerControlsFriendlySide`.
- When `allowDualSideControl: true`: `AssignHuman` on **both** sides; `PlayerControlsFriendlySide` ignored.
- JSON: `allowDualSideControl` (boolean, default false). C#: `ScenarioPolicyProfile.AllowDualSideControl`.
- UI: **TEST SANDBOX** banner when active.

---

## 4. AvA speed tiers

| Tier | Speed | Role |
|------|-------|------|
| Debug | &lt; 256× headless | Smoke only |
| **Batch minimum** | **≥ 256×** | Designer sweeps — **requirement floor** |
| **Farm target** | **≥ 1000×** | Cloud batch — **performance target** |

Interactive Phase 3 may reach **900×+** with rendering; headless tiers apply when render is skipped.

---

## 5. Observer attach (`AttachReplayViewer`)

- Session flag on `DelegationOrchestrator` (shared by `SimulationSession` / `DelegationBridge`).
- Blocks human order ingress; agents continue ticking.
- Pause/scrub via replay UI; full `DecisionLog` preserved for AAR (req 17).

---

## 6. Stack traceability (Graphite)

| PR | Branch | Scope |
|----|--------|-------|
| DELEG-6 | `stack/delegation/sim-modes-docs` | This spec + req 03 |
| DELEG-7 | `stack/delegation/dual-side-policy` | `ScenarioPolicyProfile` + JSON |
| DELEG-8 | `stack/delegation/dual-side-config` | `SimulationModeConfigurator` |
| DELEG-9 | `stack/delegation/observer-attach` | `AttachReplayViewer` |

GitNexus upstream of `Apply`: `DelegationBridge.ConfigureSimulationMode`, `SimulationModeConfiguratorTests` — **MEDIUM** risk.
