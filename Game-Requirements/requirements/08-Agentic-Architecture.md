# 08 - Agentic Architecture Layer

**Last Updated:** May 28, 2026

## Purpose
Define the core software architecture that enables true agentic development, high-performance military simulation, and seamless operation across human, mixed, and fully autonomous agent-versus-agent modes.

## Vision
A modern, modular, and highly agent-aware architecture built on Unity’s Data-Oriented Technology Stack (DOTS/ECS) combined with a pluggable AI Decision Engine. The architecture must support massive entity counts (10,000+ units and drones), deterministic high-speed simulation, real-time human oversight, and direct live editing by AI agents via Unity-MCP.

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

## Open Questions / Decisions Needed

1. Should we use Unity’s built-in Netcode for Entities from day one, or start with a custom deterministic lockstep system?
2. What is the maximum acceptable entity count for the first release (target: 10k or 25k)?
3. Do we want to support live co-simulation with external agents (e.g., Python-based reinforcement learning agents)?
4. How much of the sensor and physics logic should be Burst-compiled vs. managed C#?

---

**Status:** Ready for implementation planning