# 06 - Database Intelligence Layer

**Last Updated:** May 29, 2026  
**Research basis:** [Agentic CMO Research](../../docs/research/agentic-cmano-research.md) (database workflow), [Near-Future Tech Research](../../docs/research/near-future-tech-research.md), [Speculative Systems Research](../../docs/research/speculative-systems-research.md)

## Purpose

Provide a robust, agent-driven layer that maintains the integrity, consistency, provenance, and balance of the entire game database (units, weapons, sensors, platforms, and speculative systems) as new content is added or existing data is modified.

## Vision

A self-maintaining, auditable database treated as a **first-class product** — not support files hidden behind the executable. The layer detects inconsistencies, normalizes values, validates cross-system relationships, tracks provenance per field, and keeps the simulation balanced even as hundreds of new systems are proposed by agents. Military data is noisy and contradictory; agents **propose, never auto-merge**.

## Dual-Track Content Pipeline *(from CMO public DB workflow)*

Modeled on the observable Command database request process:

| Track | Purpose | Access |
|-------|---------|--------|
| **Public intake** | Community evidence-backed requests (new units, corrections, doctrine) | Issue-only public channel; templates; no direct DB edit |
| **Internal curation** | Coordinated work, overhauls, non-public priorities | Private queue with triage states |

**Triage states:** `accepted`, `needs_evidence`, `deferred`, `merged_into_overhaul`, `blocked`, `rejected`

Handling is **not FIFO** — driven by broader database objectives and release trains.

### Release trains

- Database drops ship **separate from engine releases** so data can update more frequently than code.
- Each drop is versioned, diffable, and tied to scenario DB binding (req 11).

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

### 6. Provenance & Evidence Model *(new)*

Every database field must support optional provenance metadata:

- Source link or citation (manual, OSINT, program doc)
- Confidence score (agent-assigned, human-overridable)
- Reviewer identity and revision date
- TRL and Technology Level gate (docs 09/10)

**Data separation:**

- `source_facts` — directly cited values
- `interpreted_values` — analyst/agent inference
- `gameplay_abstractions` — simplified sim parameters

### 7. Schema-Aware Editing & Constraint Rules *(new)*

- Prevent invalid mount/sensor/date/magazine combinations at edit time
- Temporal validity windows for capabilities and variants (canonical immutable IDs)
- Constraint rules for incompatible loadouts and platform generations
- Branching support: separate DB snapshots for TL-0 through TL-5 (doc 10)

### 8. Database Research Agent Workflow *(new)*

Agent pipeline for content updates — all outputs require human approval:

1. **Retrieval agent** — gathers articles, manuals, procurement notices, prior issue history
2. **Entity resolution agent** — maps aliases to canonical platform IDs
3. **Diff agent** — proposes additions/edits against current DB state
4. **Rules agent** — schema and temporal consistency checks
5. **Human reviewer** — approves or rejects patch bundle

Posture: **propose, not auto-merge.** Agents function as research assistants and validators, not final authority.

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

## Resolved Decisions (May 29, 2026)

| Question | Decision |
|----------|----------|
| Normalization autonomy? | **Human confirmation for large changes**; auto-normalize only within defined tolerance bands |
| Balance drift tolerance? | Flag when win-rate delta exceeds **±8%** over 500+ agent-vs-agent runs (tuneable) |
| Database branching? | **Yes** — TL-gated DB branches (TL-0–TL-5) with shared canonical IDs |

---

**Status:** Research-integrated — ready for schema design and Git-backed workflow implementation