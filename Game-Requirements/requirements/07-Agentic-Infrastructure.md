# 07 - Agentic Infrastructure Framework

**Last Updated:** May 29, 2026  
**Research basis:** [Agentic CMO Research](../../docs/research/agentic-cmano-research.md)

## Purpose

Provide a comprehensive set of high-level infrastructure agents that automate and enhance the entire game development and simulation lifecycle — scenario authoring, batch analysis, balance, events, performance, and human-supervised operator assistance — aligned with professional wargaming workflows (Monte Carlo, experiment runners, external telemetry).

## Vision

A self-improving game infrastructure where specialized AI agents support developers, analysts, and players. Agents generate scenarios, orchestrate reproducible experiments, tune balance, manage events, optimize performance, and act as **supervised copilots** — every recommendation evidence-linked and reversible. Inspired by CMO Professional Edition analysis tooling, implemented as a clean-room, API-first platform.

## Functional Requirements

### 1. Scenario Generation Agent
- Automatically creates balanced, historically plausible, or procedurally generated scenarios based on:
  - Selected theater (Baltic, South China Sea, etc.)
  - Force composition (NATO vs. near-peer adversary)
  - Time period and technology level
  - Mission objectives (strike, defense, escort, swarm attack)
- Supports parameter-driven generation (difficulty, entity count, weather, electronic warfare intensity)
- Outputs complete scenario files ready for immediate play or agent-vs-agent testing

### 2. After Action Review (AAR) Agent
- Analyzes completed scenarios and generates detailed, human-readable debriefs including:
  - Kill chains and engagement timelines
  - Key decision points and their outcomes
  - Agent performance metrics (if agents were used)
  - Lessons learned and tactical recommendations
- Supports natural language summaries and visual timeline generation

### 3. Balance Tuning Agent
- Runs thousands of agent-vs-agent simulations in headless mode
- Analyzes win rates, engagement statistics, and cost-effectiveness across weapon systems
- Proposes specific balance adjustments (range, damage, stealth, cost, reload times)
- Maintains a balance dashboard with confidence scores for each change

### 4. Event & Trigger System Agent
- Dynamically manages world events, random occurrences, and mission triggers
- Supports complex conditional logic (e.g., “if 3+ ships detected in zone X, launch drone swarm”)
- Enables emergent gameplay through intelligent event chaining
- Exposes tools for designers to define high-level event rules that the agent expands

### 5. Performance Optimization Agent
- Monitors simulation performance in real time (entity count, system load, frame time)
- Automatically adjusts detail levels for large drone swarms (LOD, sensor update rates, physics fidelity)
- Suggests architectural improvements for specific scenarios
- Provides headless performance benchmarks for agent-vs-agent runs

### 6. Experiment & Monte Carlo Agent *(new — CMO Pro parity)*

Formalizes reproducible batch analysis beyond single-playthrough simulation:

- **Scenario seed control** — deterministic runs (ADR-003, req 17)
- **Parameter sweep definitions** — doctrine, ROE, force ratios, TL gates
- **Batch-run workers** — headless parallel execution (1000×+ compression)
- **Statistical output** — kill chains, sortie effectiveness, loss distributions, mission completion rates
- **Artifact storage** — structured results for AAR and regression baselines

An **experiment orchestration agent** can generate parameter matrices, launch batches, detect anomalies, summarize results, and propose follow-up tests for human analysts.

**P1 for v1.0** (infrastructure stub acceptable); **P0 for pro/analyst workflow** (Phase 5 roadmap).

### 7. Operator Copilot *(new — supervised player assistance)*

Sits above the simulation; does **not** execute hidden black-box actions. Capabilities:

- Course-of-action generation with transparent state evidence
- Doctrine and ROE explanation (req 13)
- Sensor/weapon employment suggestions
- Alert triage and prioritization (req 20)
- Natural-language querying over scenario state

Every recommendation must link to observable sim state and be **reversible** by the player. Integrates with Agent Delegation (doc 04) but does not replace unit-level agents.

### 8. Database Research Assistants *(new — extends doc 06)*

Infrastructure wrappers for the five-stage DB agent pipeline:

- Scheduled OSINT ingestion runs
- Weekly consistency and provenance audit reports
- Batch normalization proposals after speculative system additions (docs 09/10)

All writes route through Database Intelligence Layer; no direct merge.

## Non-Functional Requirements

- All agents must be **headless-capable** (run without Unity Editor open)
- Agents must integrate with Unity-MCP for live editing when the editor is open
- Full logging and traceability of all agent decisions and changes
- Support for parallel execution (multiple scenarios or tuning runs simultaneously)

## Agentic Capabilities

- Every infrastructure agent exposes **MCP tools** so Claude/Cursor can:
  - Trigger scenario generation with natural language prompts
  - Request detailed AARs on specific replays
  - Ask the Balance Tuning Agent for recommendations on specific systems
  - Define new event rules conversationally
  - Monitor and adjust performance settings during large-scale tests

- Agents can collaborate (e.g., Scenario Generation Agent → Balance Tuning Agent → AAR Agent in a single workflow)

## Technical Considerations

- Built as Unity Editor tools + standalone headless executables
- Uses Unity’s Job System and Burst for high-speed simulation batches
- Stores results in a structured database (SQLite or similar) for querying and trend analysis
- Designed to work with the Database Intelligence Layer for consistent data

## Future Extensibility

- Cloud-based scenario farm for running 10,000+ simulations in parallel
- Integration with external wargaming tools and military scenario databases
- Reinforcement learning integration for the Balance Tuning Agent
- Multiplayer scenario co-generation with human designers

## Resolved Decisions (May 29, 2026)

| Question | Decision |
|----------|----------|
| Scenario generation priority? | **Realism first**, balance second, variety third — with explicit difficulty/variety overrides |
| AAR detail v1.0? | Kill chains, key decision points, agent metrics; NL summary **P1** |
| Balance agent autonomy? | **Suggest only** with confidence scores; auto-apply never without human approval |
| Performance optimization? | **Suggest with one-click apply** in editor; headless auto-LOD for swarm scenarios only |

## Build Phase Alignment *(from agentic research roadmap)*

| Phase | Infrastructure focus |
|-------|---------------------|
| 1 — Sim kernel | Headless runner, deterministic saves |
| 2 — Database platform | DB agents, validation, public intake workflow |
| 3 — Scenario/automation | Scenario gen, import/export, replay, initial batch tools |
| 4 — Agentic layer | Copilot, experiment orchestration, DB research assistants |
| 5 — Pro workflow | Full Monte Carlo management, external connectors, institutional approval flows |

---

**Status:** Research-integrated — ready for implementation