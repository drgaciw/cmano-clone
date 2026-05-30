# 08 - Agentic Architecture Layer

**Last Updated:** May 29, 2026  
**Research basis:** [Agentic CMO Research](../../docs/research/agentic-cmano-research.md)  
**Implementation:** [Master Architecture](../../docs/architecture/architecture.md), ADR-001–005

## Purpose

Define the core software architecture that enables true agentic development, high-performance military simulation, and seamless operation across human, mixed, and fully autonomous agent-versus-agent modes.

## Vision

A modern, modular, agent-aware architecture built on Unity DOTS/ECS (`ProjectAegis.Sim`) plus a pluggable AI Decision Engine (`ProjectAegis.Delegation`). The simulation core, scenario model, databases, UX, and automation layer evolve **semi-independently** — matching the CMO pattern of preserving the sim kernel while improving interface and tooling.

## Five-Layer Clean-Room Architecture *(research-aligned)*

| Layer | Responsibilities | Project Aegis direction |
|-------|------------------|-------------------------|
| **Simulation kernel** | Time step, detection, kinematics, weapons, fuel, comms, doctrine | `ProjectAegis.Sim` — deterministic tick (ADR-004), policy evaluator (ADR-002) |
| **Entity database** | Platforms, sensors, weapons, signatures, mounts, provenance | Database Intelligence Layer (doc 06); Git-backed content repo |
| **Scenario system** | Sides, geography, ORBAT, triggers, events, objectives | Versioned JSON/YAML + compiled cache (req 11); Lua compatibility shim |
| **UX layer** | Map, timelines, planners, inspectors, agent supervision | Unity presentation; req 20 C2 UI |
| **Automation layer** | Scripting, batch analysis, orchestration, human-in-the-loop review | Agentic Infrastructure (doc 07); MCP tools |

### Interchange & integration requirements *(new)*

- **Structured simulation API** — local-first, scriptable; headless and in-editor parity
- **Scenario import/export** — stable JSON/protobuf interchange; optional XML for external interoperability (CMO Pro pattern)
- **External telemetry export** — order log, 3D replay hooks, AAR artifacts (req 17)
- **Separate release trains** — engine vs database versioning (doc 06)
- **Deterministic stepping** — mandatory for replay, Monte Carlo, and agent-vs-agent (ADR-003, ADR-004)

Highest-risk failure mode: uncontrolled complexity in equipment database and doctrine logic — architecture must prioritize auditable data and testable policy over UI polish.

## Functional Requirements

### 1. Simulation Core Agent
- **Deterministic Simulation Engine**
  - Fixed timestep physics and logic updates
  - Full support for time compression (1x to 900x+)
  - Headless execution mode for agent-vs-agent batch simulations
  - Complete replay and save/load functionality with deterministic seeds

- **Event Scheduling System**
  - Priority queue for future events (missile launches, sensor detections, etc.)
  - Support for both real-time and accelerated execution

### 2. Entity Component System (ECS) Layer
- **High-Performance Entity Management**
  - Built on Unity DOTS (Entities, Components, Systems)
  - Support for 10,000–50,000+ entities with low memory footprint
  - Burst-compiled systems for sensor, movement, and weapon logic

- **Dynamic Entity Archetypes**
  - Aircraft, ships, submarines, drones, missiles, ground units, sensors, EW emitters
  - Runtime entity creation and destruction (critical for drone swarms)

### 3. Decision Engine Agent
- **Pluggable AI Decision System**
  - Core decision-making framework that different agent personalities plug into
  - Supports rule-based, utility-based, behavior-tree, and neural network agents
  - Agent personality profiles (Aggressive, Defensive, Cautious, Opportunistic)

- **Autonomy Levels**
  - Full manual control → Semi-autonomous (agent suggests) → Full autonomous (agent executes)

### 4. Physics & Sensing Agent
- **Advanced Sensor & Detection Modeling**
  - Multi-spectrum sensors (radar, IR, sonar, ESM, visual)
  - Line-of-sight, terrain masking, atmospheric effects
  - Stealth signature and countermeasure modeling

- **Electronic Warfare & Cyber Layer**
  - Jamming, deception, and directed energy effects
  - Cyber attack vectors on C4I systems

### 5. State Synchronization Agent
- **Game State Management**
  - Full serialization of simulation state for save/load and replay
  - Delta compression for future multiplayer synchronization
  - Versioned state for agent-vs-agent reproducibility

## Non-Functional Requirements

- **Performance**
  - 60+ FPS in interactive mode with 5,000+ entities
  - 1000x+ simulation speed in headless agent-vs-agent mode
  - Efficient memory usage for large drone swarms

- **Determinism**
  - 100% reproducible results when using the same seed and inputs

- **Scalability**
  - Horizontal scaling support for future cloud-based batch simulations

- **Maintainability**
  - Clear separation of concerns so AI agents can modify individual systems without breaking others

## Agentic Capabilities

- Every major system exposes **MCP tools** via Unity-MCP so Claude/Cursor can:
  - Inspect and modify entity archetypes
  - Tune sensor and weapon parameters in real time
  - Create new decision logic or agent personalities
  - Run high-speed simulation batches and analyze results
  - Debug and balance systems interactively

- Custom C# attributes allow rapid creation of new MCP tools without boilerplate

## Technical Considerations

- **Primary Technology Stack**
  - Unity 6 + DOTS (Entities 1.3+, Burst, Jobs)
  - Unity-MCP for live AI control
  - Custom high-performance event system (priority queue + fixed timestep)

- **Future-Proofing**
  - Netcode for GameObjects / Netcode for Entities ready for multiplayer
  - Modular design allows swapping physics or sensor backends

- **Integration with Other Layers**
  - Tightly coupled with Database Intelligence Layer for consistent unit data
  - Exposes hooks for the Dynamic Speculative Systems Agent to inject new entity types

## Future Extensibility

- Cloud-based simulation farm for running thousands of agent-vs-agent scenarios in parallel
- Support for larger operational theaters (theater-level to strategic-level)
- Integration with external military data feeds for real-world scenario import
- Hybrid CPU/GPU simulation for extreme entity counts

## Resolved Decisions (May 29, 2026)

| Question | Decision |
|----------|----------|
| Netcode from day one? | **Custom deterministic lockstep first**; Netcode for Entities when multiplayer is in scope |
| Max entity count v1.0? | **10,000 entities** target; 500/swarm with LOD degradation path to 25k headless |
| External agent co-simulation? | **Yes** — structured sim API + seed control for Python/RL agents (P2) |
| Burst vs managed C#? | **Burst for hot paths** (sensors, movement, weapons); managed for policy, agents, I/O |

---

**Status:** Research-integrated — aligned with ADR-001–005 and implemented Sim/Delegation assemblies