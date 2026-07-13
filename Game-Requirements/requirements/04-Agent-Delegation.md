# 04 - Agent Delegation System

**Last Updated:** 2026-07-08  
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

### Assignment UI
- Simple drag-and-drop or right-click assignment interface
- Clear visual indicators showing which units are under agent control
- Ability to view and edit agent personality and autonomy settings during play
- **Default (req 02):** personality and autonomy are editable **anytime** during Phase 3 execution
- Scenarios may override via `personalityEditPolicy` in scenario policy JSON:
  - `anytime` — full hot-swap during execution (default)
  - `planningOnly` — personalities locked at **Begin Execution**; autonomy may still change mid-fight
  - `tieredRebrief` — edit anytime at Manual/Assisted; Semi-Autonomous+ requires pause or **Rebrief Agent** with sim-time cost
- Player information visibility during execution follows scenario `playerInfoModel` (req 02); default is full transparency regardless of autonomy level

### Override & Intervention
- Player can take direct control of any unit at any time
- Agent must immediately yield control when overridden
- Option to “pause agent” or “resume agent” without losing current tasking
- **Group override:** when overriding a unit inside an agent-commanded group, the unit **detaches** from the group; the group-agent replans on the next cycle; player orders are authoritative until release/rejoin (see Resolved Design Decisions)
- **Dual-side testing:** commanding units on both sides in Mixed Mode requires scenario policy `allowDualSideControl: true` (req 03); per-target override semantics unchanged

### Realistic Variability
- Agents must exhibit human-like behavior: occasional mistakes, hesitation under pressure, different decision styles
- Agents should not be perfect tacticians — they should have strengths and weaknesses matching their personality

### Communication & Feedback
- Agents report key decisions and status changes to the player
- Optional “agent voice” or text log for immersion
- Clear explanation of why an agent made a specific decision

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
| Autonomy + ROE gating | `AutonomyGate`, `RoePolicyAdapter` (`Orchestration/`, `Roe/`) | **Shipped** | `AutonomyGateTests`, `RoePolicyAdapterTests`; Manual→FullAutonomous tiers; ROE filter before engage |
| Attention / bandwidth | `AttentionCalculator` (`Attention/`) | **Shipped** | Budget/load degradation; aligns with Resolved Design Decision §1 (default budget 20) |
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