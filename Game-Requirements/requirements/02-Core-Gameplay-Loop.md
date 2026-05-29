# 02 - Core Gameplay Loop

**Last Updated:** May 28, 2026

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
- Detailed planning phase with route planning, sensor coverage, electronic warfare posture, and rules of engagement
- Player can delegate entire task forces, individual ships/aircraft, or specific weapon systems to specialized AI agents
- Agent personalities (Aggressive, Defensive, Cautious, Opportunistic, Swarm Coordinator, etc.) can be assigned per unit or group

### Phase 3: Execution & Real-Time Command
- Core gameplay phase with variable time compression (1x to 900x+)
- Player maintains high-level oversight while agents handle tactical execution
- Real-time intervention possible at any moment (override agent decisions, issue new orders, change autonomy levels)
- Dynamic events (missile launches, detections, electronic warfare engagements) drive the simulation forward

### Phase 4: After-Action Review & Analysis
- Detailed replay with timeline scrubbing, kill chains, and decision heatmaps
- AI-generated After Action Report (AAR) summarizing key events, agent performance, and lessons learned
- Option to re-run the scenario with different agent assignments or parameters

### Phase 5: Iteration & Learning
- Player can adjust agent personalities, force composition, or technology levels
- Results feed into the Balance Tuning Agent and Database Intelligence Layer
- High-speed agent-vs-agent batch mode for rapid testing of new ideas

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

## Non-Functional Requirements

- Loop must feel responsive even at 5,000+ entities
- Clear visual and audio feedback for agent decisions and autonomy changes
- Full deterministic replay support with seed-based reproducibility
- Smooth transition between interactive and headless execution

## Agentic Capabilities

- Claude/Cursor (via Unity-MCP) can:
  - Generate complete scenarios from natural language descriptions
  - Assign and tune agent personalities across large forces
  - Analyze AARs and suggest improvements to agent behavior
  - Run thousands of agent-vs-agent simulations and identify balance issues

## Open Questions / Decisions Needed

1. Should the planning phase be turn-based or continuous real-time?
2. How much information should be hidden from the player when using high-autonomy agents (fog of war for the player themselves)?
3. Should agent personalities be editable in real time during execution, or only during planning?

---

**Status:** Gameplay loop structure approved