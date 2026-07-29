# 04 - Agent Delegation System

**Last Updated:** 2026-07-28  
**Related:** [01-Project-Overview.md](01-Project-Overview.md) · [02-Core-Gameplay-Loop.md](02-Core-Gameplay-Loop.md) · [03-Simulation-Modes.md](03-Simulation-Modes.md) · [08-Agentic-Architecture.md](08-Agentic-Architecture.md) · [13-Doctrine-ROE-EMCON-WRA.md](13-Doctrine-ROE-EMCON-WRA.md) · [14-Engagement-And-Fire-Control.md](14-Engagement-And-Fire-Control.md) · [17-Replay-AAR-And-Order-Log.md](17-Replay-AAR-And-Order-Log.md) · [19-Cyber-And-Comms.md](19-Cyber-And-Comms.md) · [20-Command-And-Control-UI.md](20-Command-And-Control-UI.md)  
**Status:** Locked  
**Locked spec:** [2026-05-30-agent-delegation-decisions-design.md](../../docs/superpowers/specs/2026-05-30-agent-delegation-decisions-design.md)

## Purpose
Define how players can assign specialized AI agents to individual units, groups, weapon systems, or entire task forces, enabling realistic autonomous behavior while maintaining human oversight.

Implements hub **[FR-03](01-Project-Overview.md#functional-requirements)** (unit and task-force agent delegation).

## Vision
A flexible, intuitive delegation system that turns the game into a true “theater commander” experience. Players no longer need to micromanage every unit — they can assign agents with distinct personalities and let them execute with realistic human-like variability, while retaining the power to intervene at any moment.

## Core Delegation Concepts

### Delegation Levels
- **Unit Level** — Assign an agent to a single aircraft, ship, submarine, or drone (**shipped** — v1 target model)
- **Group / Task Force Level** — Assign one agent to control an entire squadron, surface action group, or drone swarm (**shipped** — v1 target model)
- **System Level** — Assign an agent to a specific weapon system (e.g., ship’s air defense, aircraft’s electronic warfare suite) — **Phase N (not v1 target model)**
- **Side Level** (Advanced) — Assign a high-level strategic agent to command an entire faction — **Phase N (not v1 target model)**

### Agent Personalities (Initial Set)
- **Aggressive** — Prioritizes offensive action and risk-taking
- **Defensive** — Focuses on protection, survival, and force preservation
- **Cautious** — Waits for clear advantage before committing
- **Opportunistic** — Exploits momentary weaknesses aggressively
- **Swarm Coordinator** — Optimized for managing large drone formations
- **Electronic Warfare Specialist** — Prioritizes jamming, deception, and sensor denial

### Autonomy Levels
1. **Manual** — Agent suggests actions only; player must approve (`HUMAN_IN_LOOP`)
2. **Assisted** — Agent executes low-risk actions automatically; asks for high-risk decisions
3. **Semi-Autonomous** — Agent acts independently; player can override within reaction window (`HUMAN_ON_LOOP`)
4. **Full Autonomous** — Agent operates with minimal oversight; highest escalation risk (`FULL_AUTONOMOUS` — see req 13, doc 10)

Autonomy levels must integrate with side/unit ROE and policy evaluator (req 13, ADR-002). Full autonomous lethal engagement requires explicit player opt-in per mission phase.

## Functional Requirements

> **Scope note (2026-07-28).** The **AGD-\*** requirements below give IDs and pass conditions to the agent-management UI, which previously existed only as untestable prose ("simple drag-and-drop", "clear visual indicators") with no identifiers — leaving it unreferenceable from tests, the tracker, or doc 20. **No locked decision is reopened.** Every AGD requirement *surfaces* an already-decided, already-shipped headless semantic; where one contradicts [Resolved Design Decisions](#resolved-design-decisions), the locked decision wins.

### Assignment UI (AGD-01 – AGD-06)

| ID | Requirement | Priority / maturity |
|----|-------------|---------------------|
| **AGD-01** | **Assignment affordance.** An agent can be assigned to a unit or group by direct manipulation (drag) **and** by an equivalent keyboard-reachable path (context menu). Mouse-only assignment is a defect ([doc 20 **ACC-04**](20-Command-And-Control-UI.md)). | **P0** — **Partial / Phase N** (UI debt) |
| **AGD-02** | **Control ownership is visible without selection.** Every unit renders its controller class — human, agent, mixed, detached — on the map and in the OOB tree, distinguishable with colour removed ([doc 20 **ACC-01**](20-Command-And-Control-UI.md)). | **P0** — **Partial** (badges partial) |
| **AGD-03** | **Personality and autonomy are viewable and editable in play**, subject to **AGD-04**. Editing is a command with a result ([doc 20 **CMD-16**](20-Command-And-Control-UI.md)); denials surface the `LoopPolicyVerdict` reason rather than failing silently. | **P0** — **Partial** (`TryRebindAgentTraits` shipped; UI partial) |
| **AGD-04** | **`personalityEditPolicy` is legible *before* the operator attempts an edit** — the control states whether editing is permitted now, and under `tieredRebrief` names the **sim-time cost** of Rebrief Agent before commit, not after. Policy values are unchanged: `anytime` (default), `planningOnly`, `tieredRebrief`. | **P0** — **Open** (gate shipped; UI absent) |
| **AGD-05** | **`playerInfoModel` filtering is honoured by every agent-management surface** (req 02). A panel must never display agent state the active info model withholds; default remains full transparency. | **P0** — **Open** |
| **AGD-06** | **Bulk delegation** — assigning one agent across a multi-unit selection reports per-unit outcomes; partial success renders as partial ([doc 20 **CMD-22**](20-Command-And-Control-UI.md)). | **P1** — **Open** |

### Approval and suggestion queue (AGD-07 – AGD-10)

**Gap this closes — verified against shipped code 2026-07-28.** `AutonomyGate.Evaluate` returns `GateResult(ExecuteNow, QueueForApproval, Rejected, PolicyDenialReason)`, and **`QueueForApproval` is never read by product code** — its only consumers are `AutonomyGateTests`. In `AgentController`, the gate is called with `playerApproved: false` hardcoded, and only two branches are handled: `Rejected` (logged as a policy denial) and `ExecuteNow` (issued). An order that is neither is **silently discarded**.

Consequence: **Manual** autonomy — defined above as *"Agent suggests actions only; player must approve"* — currently computes the agent's suggestion, logs the `DecisionRecord`, and drops it. There is no approval path, so the autonomy level cannot behave as specified. The same applies to **Assisted** with a high-risk order.

This is not solvable by UI alone: the flag needs a consumer as well as a surface.

| ID | Requirement | Priority / maturity |
|----|-------------|---------------------|
| **AGD-07** | **Pending suggestions are durable and enumerable.** An order gated to `QueueForApproval` is retained as a pending item bound to its unit, agent, and originating `DecisionRecord` — not discarded. | **P0** — **Open** (flag has no consumer) |
| **AGD-08** | **Approve / reject affordance.** The operator can approve or reject each pending suggestion; both outcomes are commands with results ([doc 20 **CMD-16**](20-Command-And-Control-UI.md)) and both append to the order log (req 17), so an AAR can distinguish *agent proposed and human refused* from *agent never proposed*. | **P0** — **Open** |
| **AGD-09** | **Pending suggestions expire deterministically.** A suggestion has a stated validity (tick-based, not wall-clock) after which it lapses with a logged reason. Expiry must not vary with frame rate or compression, preserving replay determinism. | **P0** — **Open** |
| **AGD-10** | **Approval backlog is visible at a glance** and participates in alert priority ([doc 20 **ALR-01**](20-Command-And-Control-UI.md)) — an operator must not lose a fight because a Manual-autonomy unit's suggestions queued unnoticed. | **P0** — **Open** |

### Attention and load visibility (AGD-11 – AGD-14)

**Gap this closes.** Attention is designated a **core mechanic** ([Resolved Design Decision §1](#1-agent-attention--bandwidth)) — *"delegation is a trade-off — one super-agent cannot perfectly command an entire theater"*. It is genuinely implemented and load-bearing: `AttentionCalculator.Evaluate` is called from `AgentController` and feeds `DecisionPipeline`, and it already produces three **graded** degradation tiers (`SlowerReactions` at load > budget, `NarrowedFocus` at > 1.25×, `SimplerDecisions` at > 1.5×), with overload measurably slowing reactions (`Traits.ReactionDelay × 5`).

**None of it is projected to the UI.** The trade-off the design calls central is invisible to the operator, who therefore cannot make the decision the mechanic exists to pose.

| ID | Requirement | Priority / maturity |
|----|-------------|---------------------|
| **AGD-11** | **Per-agent load is displayed against budget** (e.g. load / budget), sourced from `AttentionEvaluation` — not re-derived in the UI, which would risk divergence from the value that actually gated the decision. | **P0** — **Open** |
| **AGD-12** | **Degradation tier is named, not implied.** The three shipped tiers are surfaced distinctly (slower reactions / narrowed focus / simpler decisions) so the operator can tell *how* an agent is degrading, not merely that it is. | **P0** — **Open** |
| **AGD-13** | **Overload is a first-class alert** ([doc 20 **ALR-01**](20-Command-And-Control-UI.md)) when an agent crosses into a degradation tier, attributable to the agent (**ALR-05**). | **P0** — **Open** |
| **AGD-14** | **Load impact is previewed before commit.** Assigning further units to an agent shows the projected post-assignment load, so overload is a choice rather than a discovery. | **P1** — **Open** |

### Override & Intervention (AGD-15 – AGD-18)

Locked semantics, restated as testable UI requirements. Behaviour is unchanged from [Resolved Design Decision §2](#2-conflicting-orders-on-group-override).

| ID | Requirement | Priority / maturity |
|----|-------------|---------------------|
| **AGD-15** | **Direct control at any time.** The player can take direct control of any unit; the agent yields immediately, via the shipped `OverrideService` controller swap. Exactly one active controller per target is preserved. | **P0** — **Shipped headless**; UI **Partial** |
| **AGD-16** | **Group override consequence is stated before commit.** Overriding a unit inside an agent-commanded group warns that the unit **detaches** and the group replans next cycle, before the operator commits — the locked detach-and-rejoin rule made legible rather than surprising. | **P0** — **Open** (rule shipped; UI absent) |
| **AGD-17** | **Pause / resume without losing tasking.** An agent can be paused and resumed with its current tasking intact; paused state is visible on the unit, and outstanding tasking is shown as held rather than cancelled. | **P0** — **Partial** |
| **AGD-18** | **Dual-side control is gated and unmistakable.** Commanding both sides requires `allowDualSideControl: true` (req 03); when active, the UI marks which side the operator is acting as, on every command surface. Per-target override semantics unchanged. | **P1** — **Open** |

### Realistic Variability
- Agents must exhibit human-like behavior: occasional mistakes, hesitation under pressure, different decision styles
- Agents should not be perfect tacticians — they should have strengths and weaknesses matching their personality

### Communication & Feedback (AGD-19 – AGD-24)

**What already exists.** `AgentController` appends a `DecisionRecord` for every decision carrying the **chosen action, the full candidate set, the rationale, attention load and budget, and the RNG draw**. The raw material for a genuine "why did it do that" answer is therefore already logged and deterministic — it is simply not presented.

| ID | Requirement | Priority / maturity |
|----|-------------|---------------------|
| **AGD-19** | **Decision explain surface.** For any agent action the operator can see what was chosen, what the alternatives were, and why — sourced from the logged `DecisionRecord`, not reconstructed. | **P0** — **Open** (data shipped) |
| **AGD-20** | **Explain includes the attention context** in force at decision time (load vs budget, degradation tier), because "the agent was overloaded" is frequently the true answer. | **P0** — **Open** |
| **AGD-21** | **Denials are explained in the agent's own surface**, not only the global message log — a policy denial against an agent order appears where the operator is looking at that agent, reusing the shared reason vocabulary ([doc 20 **CMD-17/18**](20-Command-And-Control-UI.md)). | **P0** — **Open** |
| **AGD-22** | **Status changes are reported and attributable** — controller changes, detach/rejoin, pause/resume and autonomy changes surface as operator-visible events attributable to human or agent ([doc 20 **ALR-05**](20-Command-And-Control-UI.md)). | **P0** — **Partial** (events logged; surfacing partial) |
| **AGD-23** | **Trust signals must not imply live effect.** `TrustSignal` is **emit-only** in the tactical MVP with *no effect on agent decisions during a run* ([Resolved Design Decision §3](#3-trust--experience-campaign)). Any UI showing trust must state that it does not affect current behaviour. Presenting it as live feedback would be a false claim about the simulation. | **P0** — **Open** |
| **AGD-24** | **Optional "agent voice" / flavour text is separable** from authoritative explain content, and never the only channel for a decision, denial, or status change. | **P2** — **Open** |

### Acceptance Criteria (agent management)

Doc 04 previously carried no acceptance criteria for its UI surface. These are checkable headlessly except where noted, consistent with ADR-010.

| # | Criterion | Evidence policy |
|---|-----------|-----------------|
| 1 | At **Manual** autonomy, an agent decision produces a **retained pending suggestion**; approving it executes the order, rejecting it does not, and both append to the order log (**AGD-07/08**) | **Open** — deterministic test, fixed seed; currently fails: `QueueForApproval` has no consumer |
| 2 | A pending suggestion left unapproved expires at a **tick-based** deadline with a logged reason, identically at 1× and 60× compression (**AGD-09**) | **Open** — headless, two compression settings, same tick outcome |
| 3 | Agent load/budget shown in the UI equals the `AttentionEvaluation` value that gated that decision (**AGD-11**) | **Open** — projection test asserting UI value == decision-time value |
| 4 | Crossing each of the three degradation thresholds surfaces a distinct named tier and raises one alert (**AGD-12/13**) | **Open** — drive load past budget, 1.25×, 1.5× |
| 5 | Overriding a group member warns of detach **before** commit; on commit, `GroupMemberDetach` + `ControllerChange` are logged and the group replans next cycle (**AGD-16**) | **Open** — the headless half is shipped; asserts the warning precedes the command |
| 6 | Under `planningOnly`, a personality edit attempt during execution is refused with the `LoopPolicyVerdict` reason displayed, and agent state is unmutated (**AGD-03/04**) | **Open** — `TryRebindAgentTraits` denial path is shipped; asserts the surfaced reason |
| 7 | Under a restrictive `playerInfoModel`, no agent-management surface displays withheld state (**AGD-05**) | **Open** — projection test per info model |
| 8 | Explain for any agent action lists chosen action, candidate set, rationale, and decision-time attention (**AGD-19/20**) | **Open** — assert against the logged `DecisionRecord` |
| 9 | Any trust display states that trust does not affect current-run behaviour (**AGD-23**) | **Open** — string/contract assertion |

## Non-Functional Requirements

- Delegation must feel responsive even with thousands of entities
- No performance penalty when many units are under agent control
- Full logging of all agent decisions for replay and analysis
- Agents must respect rules of engagement and player-defined constraints

## Agentic Capabilities

- Claude/Cursor (via Unity-MCP) can:
  - Create new agent personalities with custom behavior trees or utility functions
  - Tune existing agent parameters in real time
  - Analyze agent performance across hundreds of simulations
  - Generate new delegation strategies based on scenario goals

## Technical Considerations

- Built on the Decision Engine layer ([08-Agentic-Architecture.md](08-Agentic-Architecture.md))
- **Shipped decision path:** trait-weighted softmax via `DecisionPipeline` + `SeededRng` (deterministic given the same seed, traits, candidates, and attention)
- **Phase N:** pluggable behavior-tree (BT) or neural-network (NN) brains — not v1; shipped path remains trait-weighted softmax only
- Agents integrate with the ECS / world snapshot path via `DelegationBridge` / `ISimWorldSnapshot` (bridge hotpath is zero-touch)
- Support for hot-swapping agent personalities during execution (subject to scenario `personalityEditPolicy`; default `anytime`)

## Future Extensibility

- Player-created custom agent personalities (modding support)
- Machine learning agents that improve over time
- Multi-agent coordination (e.g., one agent commanding a swarm of subordinate agents)
- Integration with external AI models for advanced research use cases

## Cross-Domain Traceability

| Doc | How delegation interacts |
|-----|--------------------------|
| [13](13-Doctrine-ROE-EMCON-WRA.md) | Each delegated controller carries a **Policy Snapshot** at assign time; `IPolicyEvaluator` / `IRoeFilter` gate agent-issued orders before engage. Autonomy tiers (`HUMAN_IN_LOOP` … `FULL_AUTONOMOUS`) align with ROE and `AutonomyGate`; lethal full-auto requires explicit opt-in per mission phase. ROE violations feed **emit-only** `TrustSignal` records (no mid-run trait mutation). |
| [14](14-Engagement-And-Fire-Control.md) | Agent **intents** enter the same engagement resolver as player and mission-auto paths via `SimulationSession` (MVP engage bound on `DelegationBridge`). Personality affects timing/risk (e.g., Aggressive vs Cautious in DLZ); denials surface as `FireAbortReason` + order-log entries shared with replay. |
| [17](17-Replay-AAR-And-Order-Log.md) | `DecisionLog` is the canonical append-only stream: `AgentIntent`, `ControllerChange`, `GroupMemberDetach` / `GroupMemberRejoin`, policy denials, and `TrustSignal` at scenario finalize. `GetLiveOrderLogView()` applies `playerInfoModel` filtering for HUD/message log without altering stored log (deterministic replay hash). |
| [19](19-Cyber-And-Comms.md) | `DelegationBridge` hosts `CommsTimelineSimulator` (order delay, link degrade); agents receive stale/datalink flags in observations. Degraded comms may reduce effective **attention budget** (Phase 2); **Electronic Warfare Specialist** preset prioritizes comms/EMCON tradeoffs. |
| [20](20-Command-And-Control-UI.md) | Unity hosts bind **read-only** projections via `DelegationBridgeHost` / `UnitDetailBridge` — no sim mutation from UI. Delegation badges, autonomy sliders, pause/resume, and assisted intent preview map to bridge enqueue + `TryRebindAgentTraits`; all commits log through doc 17. |

## Open Questions / Decisions Needed

All charter questions for agent delegation are **locked**. See [Resolved Design Decisions](#resolved-design-decisions) and the [locked spec](../../docs/superpowers/specs/2026-05-30-agent-delegation-decisions-design.md). No reopen without user approval.

## Implementation Mapping (headless)

| Area | Path / type | Status | Evidence |
|------|-------------|--------|----------|
| Traits / personality presets | `TraitVector`, `PersonalityCatalog` (`ProjectAegis.Delegation` · `Traits/`) | **Shipped** | `src/ProjectAegis.Delegation/Traits/`; personality presets (Aggressive, Defensive, Cautious, …) feed decision scoring |
| Stochastic decision engine | `DecisionPipeline` + `SeededRng` (`Decision/`) | **Shipped** | Trait-weighted softmax; `src/ProjectAegis.Delegation.Tests/Decision/DecisionPipelineTests.cs`; deterministic given seed |
| Autonomy + ROE gating | `AutonomyGate`, `RoePolicyAdapter` (`Orchestration/`, `Roe/`) | **Shipped — with an unconsumed branch** | `AutonomyGateTests`, `RoePolicyAdapterTests`; Manual→FullAutonomous tiers; ROE filter before engage. **`GateResult.QueueForApproval` has no product consumer** — `AgentController` calls the gate with `playerApproved: false` hardcoded and handles only `Rejected` and `ExecuteNow`, so approval-gated orders are dropped (**AGD-07**) |
| Approval queue (Manual / Assisted-high-risk) | — | **Absent** | No pending-suggestion store, projection, or UI. Manual autonomy cannot behave as specified until this exists |
| Attention / bandwidth | `AttentionCalculator` (`Attention/`) | **Shipped (sim) / not projected (UI)** | Budget/load degradation feeding `DecisionPipeline` via `AgentController`; three graded tiers. Aligns with Resolved Design Decision §1 (default budget 20). **No C2 projection exposes it** — core mechanic is invisible to the operator (**AGD-11**) |
| Group override detach-rejoin | `DetachRejoinService` (`Groups/`) | **Shipped** | Detach-and-rejoin default; `GroupMemberDetach` / `GroupMemberRejoin` order-log events |
| Trust / experience emit | `TrustSignalEmitter` (`Trust/`) | **Shipped (emit-only)** | `TrustSignalEmitterTests`; tactical MVP emit-only — no mid-run trait mutation (campaign aggregate Phase 3) |
| Session facade / tick path | `DelegationBridge` (`ProjectAegis.Delegation.UnityAdapter` · `Bridge/`) | **Shipped — CRITICAL zero-touch hotpath** | Owns orchestrator bind, `Tick()`, engage/comms timelines; **GitNexus: CRITICAL** — no hotpath edits through Release v1; impact before any bridge change |
| Controller registry + phase gate | `DelegationOrchestrator` (`Orchestration/`) | **Shipped** | Phase gate, stochastic agent choice, detach-rejoin, `DecisionLog`, `FinalizeScenario()` → trust; wired from bridge + `BalticReplayHarness` |
| Scenario loop policy | `LoopPolicyGate` | **Shipped** | `personalityEditPolicy`, `playerInfoModel` from scenario JSON (req 02, 13) |
| Hot-swap personality / traits | `DelegationOrchestrator.TryRebindAgentTraits` | **Shipped** | Denials via `LoopPolicyVerdict` without mutating agent state |
| C2 delegation badges / drag-drop UI | Unity C2 hosts / presentation | **Partial / Phase N** | Headless projections + partial C2; full drag-drop assignment + polish badges remain UI debt (tracker row 04) |
| BT / NN pluggable brains | Decision engine extension points | **Phase N** | Shipped = trait-weighted softmax only; BT/utility/NN modules deferred |

**Blast radius:** Prefer orchestrator-only diffs; bridge edits require impact report and must preserve zero-touch hotpath (see [Sprint 13 kickoff](../../production/agentic/sprint-13-kickoff-2026-06-04.md), `AGENTS.md` invariants).

## Resolved Design Decisions

Decisions locked May 30, 2026. Full rationale: `docs/superpowers/specs/2026-05-30-agent-delegation-decisions-design.md`.

### 1. Agent attention / bandwidth

**Decision:** Yes — **core mechanic**, not optional.

- Every agent has an attention **budget** (default **20**); **load** scales with contacts, engagements, and group member count.
- Overload degrades gracefully: slower reactions → narrowed focus → simpler decisions.
- Personality presets may modulate budget (e.g., Swarm Coordinator +25%).
- Strategic intent: delegation is a trade-off — one super-agent cannot perfectly command an entire theater.

### 2. Conflicting orders on group override

**Decision:** **Detach-and-rejoin** (default).

- Override of a group member detaches the unit, suspends/resumes its agent via controller swap, and marks the group for **next-cycle** replan.
- Exactly one active controller per target — order conflicts are structurally impossible.
- Order log emits `GroupMemberDetach`, `GroupMemberRejoin`, and `ControllerChange` events.
- Future per-scenario option: `groupOverrideMode: stayAndSuggest` (not v1).

### 3. Trust / experience (campaign)

**Decision:** **Emit-only in tactical MVP; campaign aggregation Phase 3** (aligned with req 13).

- Tactical layer emits `TrustSignal` records (ROE violations, objectives met, friendly fire, override rate); **no effect on agent decisions during a scenario run**.
- Campaign layer (Phase 3) aggregates into `AgentExperienceBlob` and may adjust trait snapshots at scenario **load** only — never mid-tick, preserving replay determinism.

---

**Status:** Locked (Sprint 13). Resolved decisions locked May 30, 2026 — see [locked spec](../../docs/superpowers/specs/2026-05-30-agent-delegation-decisions-design.md).

---
**Implementation grade:** Partial+ — see [implementation-tracker-2026-07-04.md](../implementation-tracker-2026-07-04.md) row 04. Design Status remains **Locked**. Charter re-honesty: Wave 1 2026-07-08.

**Wave 2 (2026-07-28) — agent-management UI review.**

*Added:* **AGD-01 – AGD-24** and 9 acceptance criteria. The agent-management UI previously existed only as prose bullets with **no requirement IDs**, making it unreferenceable from tests, the tracker, or doc 20. Locked decisions are surfaced, never altered.

*Two findings recorded against shipped code, both verified by inspection rather than inferred:*

1. **`GateResult.QueueForApproval` has no product consumer.** `AgentController` calls `AutonomyGate.Evaluate(..., playerApproved: false)` and handles only `Rejected` and `ExecuteNow`; anything gated for approval is computed, logged as a `DecisionRecord`, then discarded. **Manual autonomy therefore cannot behave as this document specifies** ("agent suggests, player approves") — there is no approval path. Its only consumers are `AutonomyGateTests`. See **AGD-07 – AGD-10**.
2. **Attention is a designated core mechanic that no UI surface exposes.** `AttentionCalculator` genuinely gates decisions (three graded degradation tiers, reaction delay ×5 on overload) but is absent from every C2 projection, so the trade-off the design calls central is invisible to the operator. See **AGD-11 – AGD-14**.

*Correction to a prior claim of mine:* an earlier draft of [doc 20](20-Command-And-Control-UI.md)'s AUT-\* block asserted that no human-vs-agent contention rule existed. **That was wrong** — the rule is locked in §2 here and shipped via `OverrideService` / `DetachRejoinService`. Doc 20 has been corrected; the genuine gap is that the shipped rule has no operator-facing surface.