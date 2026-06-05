# Agent Delegation — Resolved Decisions

**Date:** 2026-05-30  
**Project:** Project Aegis (cmano-clone)  
**Status:** Approved — documentation pass; implementation in DELEG-8–10  
**Source requirement:** `Game-Requirements/requirements/04-Agent-Delegation.md`  
**Related:** `2026-05-28-agent-delegation-framework-design.md`, `2026-05-30-simulation-modes-decisions-design.md`, `13-Doctrine-ROE-EMCON-WRA.md`, `17-Replay-AAR-And-Order-Log.md`

---

## 1. Purpose

Resolves the three open questions in req 04 and records how those answers fold into the approved delegation framework and `ProjectAegis.Delegation` code. No new framework architecture — this pass **locks** decisions for traceability and review.

---

## 2. Locked decisions

| # | Open question (req 04) | Decision |
|---|------------------------|----------|
| 1 | Limited agent attention / bandwidth? | **Yes — core mechanic.** Continuous budget + load-driven degradation. Default budget **20**; personality-modulated. |
| 2 | Conflicting orders on group override? | **Detach-and-rejoin default.** Single active controller; next-cycle group replan; order-log events. |
| 3 | Trust / experience over a campaign? | **Emit-only in tactical MVP; campaign aggregation Phase 3.** Traits at scenario **load** only, never mid-tick. |

---

## 3. Attention / bandwidth

Every `AgentController` has an **attention budget** (default **20**). **Load** scales with contacts (0.5×), engagements (1.0×), and group members (0.25×).

When load exceeds budget, degradation applies in order:

1. Slower reactions (`load > budget`)
2. Narrowed focus (`load > budget × 1.25`)
3. Simpler decisions (`load > budget × 1.5`)

Personality presets modulate budget via `AttentionBudgetMultiplier` (Swarm Coordinator **1.25**, EW Specialist **0.9**). Delegation is a strategic trade-off — one super-agent cannot perfectly command an entire theater.

---

## 4. Group override — detach-and-rejoin

When the player overrides a unit in an agent-commanded group:

1. Unit **detaches** (`DetachedFromGroupId` recorded).
2. Group removes member; **`PendingReplan`** on next orchestrator tick.
3. `HumanController` attached; player orders authoritative.
4. On release, unit **rejoins** if detached from a group.

Order-log events: `GroupMemberDetach`, `GroupMemberRejoin`, `ControllerChange`.

Future (not v1): per-scenario `groupOverrideMode: stayAndSuggest`.

---

## 5. Trust / experience (campaign)

| Phase | Scope |
|-------|--------|
| **MVP (tactical)** | `TrustSignalEmitter.EmitFromSession` at `FinalizeScenario`; logged for AAR; **zero effect** on `TryDecide`. |
| **Campaign (Phase 3)** | Aggregate into `AgentExperienceBlob`; UI trust/reputation; trait deltas at scenario **load** only. |

MVP metrics: `missions_succeeded`, `objectives_met_ratio`, `roe_violations`, `friendly_fire_incidents`, `player_override_rate`.

Determinism: if trust ever feeds decisions, use sorted metric keys or fixed trait snapshot at `Begin Execution` (DET-002).

---

## 6. Implementation mapping (DELEG-8–10)

| Decision | Code anchor |
|----------|-------------|
| Override + detach | `DelegationOrchestrator.TryTakeDirectControl`, `DetachRejoinService`, `DelegationBridge.TryTakeDirectControl` |
| Order log | `ControllerChangeRecord`, `GroupMemberDetachRecord`, `GroupMemberRejoinRecord`, `DecisionLog` |
| Trust emit | `TrustSignalEmitter`, `DelegationOrchestrator.FinalizeScenario` |
| Attention presets | `PersonalityCatalog.ResolveAttentionBudget`, `CreateAgentFromPreset` |

**Tests:** `OrchestratorOverrideTests`, `TrustSignalEmitterTests`, `PersonalityAttentionBudgetTests`, `DelegationBridgeTests.TryTakeDirectControl_group_member_via_bridge`

---

## 7. Traceability

| Requirement (req 04) | Section |
|----------------------|---------|
| Realistic variability under pressure | §3 + framework §4 |
| Override & intervention | §4 |
| Conflicting orders (OQ #2) | §4 |
| Attention limit (OQ #1) | §3 |
| Trust/experience (OQ #3) | §5 |
| Full decision logging | §4 order log + framework §6 |
