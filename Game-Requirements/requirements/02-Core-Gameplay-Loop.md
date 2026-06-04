# 02 - Core Gameplay Loop

**Last Updated:** 2026-06-04  
**Related:** 01, 03, 04, 11, 13, 14, 15, 16, 17, 19, 20  
**Status:** Ready for design review  
**Locked spec:** [2026-05-30-core-gameplay-loop-decisions-design.md](../../docs/superpowers/specs/2026-05-30-core-gameplay-loop-decisions-design.md)

## Purpose
Define the primary gameplay loop that players and AI agents will experience, ensuring it supports seamless operation across human-only, mixed human+agent, and fully autonomous agent-versus-agent modes.

## Vision
A clean, intuitive, and deeply strategic gameplay loop that feels like commanding a real theater-level operation. The loop must scale from slow, deliberate human planning to lightning-fast agent-driven execution while maintaining full player oversight and replayability.

## Primary Gameplay Loop (5 Phases)

### Phase 1: Scenario Selection & Force Composition
- Player (or agent) selects theater, time period, and force packages
- Force composition includes manned platforms, loyal wingmen, drone swarms, submarines, and support assets
- Player can assign initial agent personalities and autonomy levels to units or task forces

### Phase 2: Pre-Mission Planning & Agent Assignment
- **Real-time with pause (CMO-style):** simulation clock is frozen on entry; the player plans at their own pace
- Detailed planning with route planning, sensor coverage, electronic warfare posture, and rules of engagement
- Player can delegate entire task forces, individual ships/aircraft, or specific weapon systems to specialized AI agents
- Agent personalities (Aggressive, Defensive, Cautious, Opportunistic, Swarm Coordinator, etc.) can be assigned per unit or group
- Player may optionally unpause the clock for time-sensitive planning (e.g., coordinating with a scenario timeline event)
- **Begin Execution** is an explicit player action — there is no implicit transition to Phase 3

### Phase 3: Execution & Real-Time Command
- Core gameplay phase with variable time compression (1x to 900x+)
- Player maintains high-level oversight while agents handle tactical execution
- Real-time intervention possible at any moment (override agent decisions, issue new orders, change autonomy levels)
- Agent personality edits follow the scenario's **personality edit policy** (see Resolved Design Decisions); default allows hot-swap anytime
- Player information visibility follows the scenario's **player info model** (see Resolved Design Decisions); default is full transparency
- Dynamic events (missile launches, detections, electronic warfare engagements) drive the simulation forward

### Phase 4: After-Action Review & Analysis
- Detailed replay with timeline scrubbing, kill chains, and decision heatmaps
- AI-generated After Action Report (AAR) summarizing key events, agent performance, and lessons learned
- Option to re-run the scenario with different agent assignments or parameters

### Phase 5: Iteration & Learning
- Player can adjust agent personalities, force composition, or technology levels
- Results feed into the Balance Tuning Agent and Database Intelligence Layer
- High-speed agent-vs-agent batch mode for rapid testing of new ideas

## Functional Requirements

| ID | Requirement | Phase / touchpoint |
|----|-------------|-------------------|
| LOOP-01 | Scenario and force-package selection | Phase 1 |
| LOOP-02 | RTwP planning with explicit **Begin Execution** | Phase 2 |
| LOOP-03 | Variable time compression during execution | Phase 3 |
| LOOP-04 | Player override of agent orders and autonomy | Phase 3 |
| LOOP-05 | Scenario `playerInfoModel` and `personalityEditPolicy` | Phase 2–3 ([13](13-Doctrine-ROE-EMCON-WRA.md) policy JSON) |
| LOOP-06 | Deterministic replay and AAR with order log | Phase 4 ([17](17-Replay-AAR-And-Order-Log.md)) |
| LOOP-07 | Balance-tuning feedback from outcomes | Phase 5 ([05](05-Dynamic-Systems-Agent.md)) |
| LOOP-08 | Doctrine and ROE inform agent constraints | Phase 2–3 ([13](13-Doctrine-ROE-EMCON-WRA.md)) |
| LOOP-09 | Sensor picture and EW shape player awareness | Phase 3 ([15](15-Sensor-Detection-And-EW.md)) |
| LOOP-10 | Engagement and magazine state in unit detail | Phase 3 ([14](14-Engagement-And-Fire-Control.md), [16](16-Logistics-And-Magazines.md)) |
| LOOP-11 | Comms degradation affects C2 overlays | Phase 3 ([19](19-Cyber-And-Comms.md), [20](20-Command-And-Control-UI.md)) |

## Key Design Principles

- **Player as Theater Commander** — The player operates at the operational/strategic level, not micromanaging every unit.
- **Agent Delegation is Core** — Assigning agents to units or systems is not an optional feature; it is a fundamental part of gameplay.
- **Seamless Mode Switching** — The same scenario can be played fully manually, with heavy agent support, or run completely autonomously at extreme speed.
- **Meaningful Autonomy with Oversight** — Agents make realistic decisions with human-like variability, but the player always retains the ability to intervene.

## Agent Integration in the Loop

- **Human Mode:** Player controls everything manually (classic Command-style play)
- **Mixed Mode:** Player commands one side or specific assets; AI agents command the opposing force or delegated friendly units
- **Agent vs Agent Mode:** Full autonomous execution at maximum speed for rapid iteration and research

## Time Compression & Simulation Speed

- 1x – 10x: Interactive command and detailed observation
- 30x – 300x: Accelerated play with meaningful human oversight
- 900x+: Headless high-speed mode for agent-vs-agent batch runs and balance testing

## Resolved Design Decisions

Decisions locked May 30, 2026. Full rationale: `docs/superpowers/specs/2026-05-30-core-gameplay-loop-decisions-design.md`.

### 1. Planning phase structure

**Decision:** Real-time with pause (CMO-style).

- Phase 2 sim clock is paused by default.
- Player plans routes, ROE, and agent assignments without time pressure.
- Execution begins only when the player selects **Begin Execution**.

### 2. Player information model (scenario-configurable)

**Default:** `fullTransparency` — the player always sees the full friendly sensor picture and all agent decisions in the order log. Autonomy changes *who acts*, not *what the player knows*.

**Scenario overrides** (for training, narrative, or realism scenarios):

| Value | Behavior |
|-------|----------|
| `fullTransparency` | Full friendly sensor picture + complete agent decision log (default) |
| `delegationFog` | At Full Autonomous, player receives summary reports and contact updates on a delay; must request status or take direct control for the full picture |
| `tieredByAutonomy` | Manual/Assisted = full picture; Semi-Autonomous = near-real-time with optional detail; Full Autonomous = summary + alerts unless player overrides |

### 3. Agent personality editing (scenario-configurable)

**Default:** `anytime` — personality and autonomy may be hot-swapped at any point during execution (aligns with req 03 mode switching and req 04 assignment UI).

**Scenario overrides:**

| Value | Behavior |
|-------|----------|
| `anytime` | Full hot-swap of personality and autonomy during execution (default) |
| `planningOnly` | Personalities locked at **Begin Execution**; autonomy level may still change mid-fight |
| `tieredRebrief` | Editable anytime at Manual/Assisted; at Semi-Autonomous+ requires pause or explicit **Rebrief Agent** action with sim-time cost |

### Scenario policy contract

Per-scenario overrides are carried in scenario policy JSON alongside ROE (see `data/scenarios/*.policy.json`, `ScenarioPolicyProfile`, req 13). Required fields when authoring scenarios:

```
playerInfoModel:       fullTransparency | delegationFog | tieredByAutonomy
personalityEditPolicy: anytime | planningOnly | tieredRebrief
```

Mission editor (req 11) is the preferred authoring surface; exported scenarios must emit these fields (defaulting when omitted).

## Non-Functional Requirements

- Loop must feel responsive even at 5,000+ entities
- Clear visual and audio feedback for agent decisions and autonomy changes
- Full deterministic replay support with seed-based reproducibility
- Smooth transition between interactive and headless execution

## Technical Considerations

- **Phase gate:** `DelegationOrchestrator.Phase` blocks ticks and orders until `BeginExecution()` ([03](03-Simulation-Modes.md))
- **Loop policy:** `LoopPolicyGate` enforces personality-edit rules per scenario JSON
- **Order log:** single append-only stream for replay and live HUD filtering ([17](17-Replay-AAR-And-Order-Log.md))
- **C2 UI:** map, unit detail, and message log bind to projection layer only — no sim mutation from UI ([20](20-Command-And-Control-UI.md))

## Future Extensibility

- Campaign-level persistence across scenarios (deferred)
- Async multiplayer with shared order log export
- Player-defined custom loop policies for training scenarios

## Agentic Capabilities

- Claude/Cursor (via Unity-MCP) can:
  - Generate complete scenarios from natural language descriptions
  - Assign and tune agent personalities across large forces
  - Analyze AARs and suggest improvements to agent behavior
  - Run thousands of agent-vs-agent simulations and identify balance issues

---

**Status:** Gameplay loop structure approved; Template A complete (Sprint 12). Open questions resolved May 30, 2026.
