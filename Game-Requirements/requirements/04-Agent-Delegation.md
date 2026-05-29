# 04 - Agent Delegation System

**Last Updated:** May 28, 2026

## Purpose
Define how players can assign specialized AI agents to individual units, groups, weapon systems, or entire task forces, enabling realistic autonomous behavior while maintaining human oversight.

## Vision
A flexible, intuitive delegation system that turns the game into a true “theater commander” experience. Players no longer need to micromanage every unit — they can assign agents with distinct personalities and let them execute with realistic human-like variability, while retaining the power to intervene at any moment.

## Core Delegation Concepts

### Delegation Levels
- **Unit Level** — Assign an agent to a single aircraft, ship, submarine, or drone
- **Group / Task Force Level** — Assign one agent to control an entire squadron, surface action group, or drone swarm
- **System Level** — Assign an agent to a specific weapon system (e.g., ship’s air defense, aircraft’s electronic warfare suite)
- **Side Level** (Advanced) — Assign a high-level strategic agent to command an entire faction

### Agent Personalities (Initial Set)
- **Aggressive** — Prioritizes offensive action and risk-taking
- **Defensive** — Focuses on protection, survival, and force preservation
- **Cautious** — Waits for clear advantage before committing
- **Opportunistic** — Exploits momentary weaknesses aggressively
- **Swarm Coordinator** — Optimized for managing large drone formations
- **Electronic Warfare Specialist** — Prioritizes jamming, deception, and sensor denial

### Autonomy Levels
1. **Manual** — Agent suggests actions only; player must approve
2. **Assisted** — Agent executes low-risk actions automatically; asks for high-risk decisions
3. **Semi-Autonomous** — Agent acts independently but player can override instantly
4. **Full Autonomous** — Agent operates with minimal oversight (can be recalled)

## Functional Requirements

### Assignment UI
- Simple drag-and-drop or right-click assignment interface
- Clear visual indicators showing which units are under agent control
- Ability to view and edit agent personality and autonomy settings in real time

### Override & Intervention
- Player can take direct control of any unit at any time
- Agent must immediately yield control when overridden
- Option to “pause agent” or “resume agent” without losing current tasking

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

- Built on the Decision Engine Agent (see 08-Agentic-Architecture.md)
- Each agent runs as a pluggable module (behavior tree, utility AI, or neural network)
- Agents must integrate cleanly with the ECS simulation core
- Support for hot-swapping agent personalities during execution

## Future Extensibility

- Player-created custom agent personalities (modding support)
- Machine learning agents that improve over time
- Multi-agent coordination (e.g., one agent commanding a swarm of subordinate agents)
- Integration with external AI models for advanced research use cases

## Open Questions / Decisions Needed

1. Should agents have limited “attention” or bandwidth (i.e., can only effectively control a certain number of units at once)?
2. How should we handle conflicting orders when a player overrides an agent that is part of a larger group?
3. Should there be a “trust” or “experience” system where agents improve or degrade based on performance over a campaign?

---

**Status:** Delegation system design approved