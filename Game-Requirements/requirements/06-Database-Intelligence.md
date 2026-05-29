# 06 - Database Intelligence Layer

**Last Updated:** May 28, 2026

## Purpose
Provide a robust, agent-driven layer that maintains the integrity, consistency, and balance of the entire game database (units, weapons, sensors, platforms, and speculative systems) as new content is added or existing data is modified.

## Vision
A self-maintaining, intelligent database that automatically detects inconsistencies, normalizes values, validates cross-system relationships, and keeps the simulation balanced — even as hundreds of new speculative systems are added over time by the Dynamic Speculative Systems Agent.

## Functional Requirements

### 1. Automated Re-indexing Agent
- Monitors all changes to the database (new systems, stat edits, deletions)
- Automatically updates search indexes, relationship graphs, and dependency maps
- Ensures fast lookup for simulation systems and AI decision engines

### 2. Consistency & Normalization Agent
- Enforces consistent units and scales across all systems (e.g., all ranges in nautical miles, all speeds in knots or Mach)
- Normalizes cost, signature, and performance values relative to era and technology level
- Flags outliers that deviate significantly from established norms

### 3. Cross-System Validation Agent
- Checks logical relationships between related systems (e.g., a missile’s range must be compatible with its launch platform’s sensors)
- Validates sensor vs. stealth interactions
- Detects impossible combinations (e.g., a subsonic aircraft with Mach 5 missile)
- Prevents broken kill chains or illogical detection probabilities

### 4. Version Control & Change Tracking
- Maintains full history of every change with timestamps, author (human or agent), and rationale
- Supports rollback to previous database versions
- Generates “what changed” reports after every batch of updates

### 5. Balance Drift Detection
- Continuously monitors win rates and engagement statistics from agent-vs-agent runs
- Flags systems that have become over- or under-powered over time
- Works closely with the Balance Tuning Agent to propose corrections

## Non-Functional Requirements

- Must handle thousands of systems without performance degradation
- All changes must be auditable and reversible
- Agent decisions must be explainable (why a value was normalized or flagged)
- Zero tolerance for data corruption or inconsistent states

## Agentic Capabilities

- Claude/Cursor (via Unity-MCP) can:
  - Ask the Database Intelligence Layer to “normalize all new speculative systems added this week”
  - Request a full consistency report across the entire database
  - Trigger a validation pass after approving new speculative content
  - View change history and rollback specific updates

- All agents in the system (Dynamic Speculative Systems Agent, Balance Tuning Agent, etc.) must route their database writes through this layer

## Technical Considerations

- Built on a structured database (SQLite for development, scalable solution for production)
- Uses Unity DOTS-friendly data formats for fast simulation access
- Implements a clear API so other agents can safely read/write data
- Strong emphasis on data provenance and audit logging

## Future Extensibility

- Cloud-based database with real-time synchronization for multiplayer and research use cases
- Machine learning models to predict balance impact of new systems before they are added
- Public API for modders to contribute new systems with automatic validation
- Integration with external military databases (when licensing allows)

## Open Questions / Decisions Needed

1. Should normalization be fully automatic or require human confirmation for large changes?
2. What is the acceptable tolerance for “balance drift” before the system flags a weapon?
3. Should the layer support branching (e.g., separate databases for “Current Tech” vs. “Future Combat Mode”)?

---

**Status:** Ready for implementation