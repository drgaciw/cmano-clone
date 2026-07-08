# 01 - Project Overview

**Last Updated:** 2026-07-08  
**Related:** [Game-Requirements-Index](../Game-Requirements-Index.md), [implementation tracker 2026-07-04](../implementation-tracker-2026-07-04.md), corpus docs 02–21 — see [Related Requirements Index](#related-requirements-index)  
**Status:** Locked vision — charter re-baselined 2026-07-08; commercial name still open; implementation grades live in the tracker  
**Research basis:** [Agentic CMO Research](../../docs/research/agentic-cmano-research.md)  
**Stage:** Release (`production/stage.txt`); S56 MVP closed; S73–S80 Baltic v3 content-complete; active forward program = scenario editor (req 11 / S81–S88)

## Purpose

Charter for product vision, scope tiers, NFR spine, and **corpus index** (FR map + Related docs). This file is not the live implementation grade sheet — use the [implementation tracker](../implementation-tracker-2026-07-04.md) for Partial / Partial+ / next-stack status.

## Program / stage snapshot

| Item | Value |
|------|--------|
| Production stage | **Release** (no Launch advance without explicit decision) |
| MVP (S56) | 21/21 rows MVP-done or documented Partial+ — **grades frozen** |
| Content (S57–S80) | Baltic v2 + v3 content programs complete; v3 isolated (`baltic-v3-*`) |
| Active engineering | **Scenario editor (req 11)** — headless-first; see roadmap `docs/reports/future-sprint-roadpmap-07042026.md` |
| Working title | Project Aegis (commercial name **Open**) |

## Vision
Create the definitive hardcore military simulation for the 2030s — a game that combines the deep, realistic command-and-control gameplay of Command: Modern Operations with modern agentic AI capabilities, massive drone swarms, autonomous systems, and near-future technologies. The game must support the full spectrum of play from pure single-player human command to fully autonomous agent-versus-agent simulation at extreme speeds.

## Core Project Goals

1. **Hardcore Military Simulation First**
   - Maintain the serious, realistic tone and depth of Command: Modern Operations
   - Accurate modeling of sensors, weapons, electronic warfare, and logistics
   - Regional-scale scenarios (Baltic, South China Sea, etc.) with thousands of entities

2. **Agentic Development & Gameplay**
   - Built from day one for agentic development using Unity + C# + Claude/Cursor
   - Players can delegate control of individual units, task forces, or entire sides to specialized AI agents
   - Full support for human vs. agent, agent vs. agent, and mixed modes

3. **Near-Future Focus (2028–2035)**
   - Heavy emphasis on drone swarms, loyal wingmen, hypersonics, directed energy weapons, autonomous underwater vehicles, and cognitive electronic warfare
   - Optional “Future Combat Mode” for speculative and black-project systems

4. **Scalable Simulation Architecture**
   - Deterministic, high-speed simulation engine capable of 1000x+ time compression
   - Headless execution for massive agent-vs-agent batch runs
   - Seamless transition between interactive and fully autonomous modes

5. **Clean-Room Product Discipline** *(from CMO research)*
   - No reuse of proprietary CMO databases, scenarios, code, or manuals
   - Reuse observable patterns: database-driven sim, scenario-centric workflow, community evidence intake, simulation-plus-analysis integration
   - Database and scenario systems auditable as first-class products
   - Agent outputs: reviewable, reversible, evidence-linked

## Target Audience

- Hardcore wargamers and military simulation enthusiasts
- Defense analysts and researchers interested in future warfare concepts
- AI/ML developers and agentic system researchers
- Players who want deep strategy with meaningful automation options

**Priority:** hardcore wargamers are the primary audience. The analyst/research and AI-developer audiences are served through the same headless and agentic capabilities ([03](03-Simulation-Modes.md), [07](07-Agentic-Infrastructure.md)) — not separate feature tracks.

## Scope Boundaries

### A — Shipped vertical slice (Release through S80)

Evidence of the shippable product spine (not multi-theater parity):

- Baltic theater content (v2 production baseline + v3 isolated corpus)
- Headless .NET simulation + delegation (`ProjectAegis.Sim`, `ProjectAegis.Delegation`)
- Deterministic replay goldens and CI gates (see Standing invariants under NFR)
- C2 proxy / PlayMode smoke harness (headless-first UI contract, [ADR-010](../../docs/architecture/adr-010-headless-first-command-driven-ui.md))
- Clean-room catalog/data layer with extend-only write gate ([06](06-Database-Intelligence.md))

### B — Active program (post-S80)

- **Scenario / mission editor (req 11)** — schema-backed scenario documents, validation engine, CLI/MCP authoring; Unity edit-mode Phase 2 per editor program plan
- Platform/catalog authoring (req 21) Excel round-trip on the write gate — Partial+; screenshots/live Editor polish remaining

### C — Product ambition (not equal-depth at vertical slice)

- Additional regional theaters (Black Sea, South China Sea, etc.) as **data extensibility**, not claimed equal content depth today
- Air, naval, and aggregate-level ground (brigade+) for air/naval interaction
- Drone swarms / autonomous systems depth beyond current OOB
- Full multi-theater 5,000+ entity interactive performance (north-star — see Success Criteria)

### Out of scope (later releases — unchanged pillars)

- Global/strategic-level campaigns (initially)
- All multiplayer — hotseat and async planned later; live networked play unscheduled
- Land warfare at battalion scale or below

## Success Criteria

### Gate-backed (current Release bar)

These are the **operational** success criteria for the vertical slice. Evidence paths are CI / AGENTS.md gates:

| ID | Criterion | Tag | Evidence |
|----|-----------|-----|----------|
| OV-SC-G1 | Full solution tests meet monotonic floor **≥1232** with 0 gate failures (known UA exclusions documented in AGENTS.md) | **Measured** | `dotnet test ProjectAegis.sln`; AGENTS.md |
| OV-SC-G2 | Baltic v2 ReplayGolden **6/6**; production hash **`17144800277401907079`** preserved | **Measured** | `tests/regression/`; AGENTS.md hash grep |
| OV-SC-G3 | PlayModeSmokeHarness **18/18** | **Measured** | UnityAdapter filter `PlayModeSmokeHarnessTests` |
| OV-SC-G4 | Clean-room: no proprietary CMO DB/scenarios/code committed | **Proxy** | Process + review; Goal 5 |
| OV-SC-G5 | Capability map FR-01…FR-19 each has a primary requirement doc on disk | **Measured** | Related Index + `ls Game-Requirements/requirements/` |

### North-star (aspirational — not current gate)

Original charter bars retained as product north-stars. **Do not treat as S80 acceptance.**

| ID | Criterion | Tag | Evidence / owner |
|----|-----------|-----|------------------|
| OV-SC-N1 | Realistic 2030-era Baltic with **5,000+ entities** at **60+ FPS** Human Mode on recommended hardware; **≥256×** headless AvA effective speed | **Deferred** | Scale target; [03](03-Simulation-Modes.md); not the S80 content gate |
| OV-SC-N2 | Agent personality presets distinguishable in blind AAR of AvA batch runs; decisions rated plausible human command | **Deferred** | [04](04-Agent-Delegation.md), [17](17-Replay-AAR-And-Order-Log.md); needs AAR protocol |
| OV-SC-N3 | Headless API sufficient for external researcher batch study without engine code changes | **Proxy** | Partial via demo/CLI/tests; full researcher package → [03](03-Simulation-Modes.md), [07](07-Agentic-Infrastructure.md) |

*(Agentic development workflow quality is a process goal tracked in [07](07-Agentic-Infrastructure.md), not a product success criterion.)*

### Overview acceptance hooks (charter verification)

| ID | Hook | How to verify |
|----|------|---------------|
| OV-AC-1 | Gate-backed table present with G1–G5 | Section above |
| OV-AC-2 | North-star table present with N1–N3 tagged Deferred/Proxy | Section above |
| OV-AC-3 | Standing invariants listed under NFR | Standing engineering invariants subsection |
| OV-AC-4 | Related Index 02–21 complete | Related Requirements Index |

## Project Name (Working Title)
**Project Aegis**

## Functional Requirements

High-level capabilities delivered across the requirements corpus (details in linked docs):

| ID | Capability | Primary doc |
|----|------------|-------------|
| FR-01 | Five-phase theater command loop with RTwP planning | [02](02-Core-Gameplay-Loop.md) |
| FR-02 | Human / Mixed / Agent-vs-Agent modes with phase gates | [03](03-Simulation-Modes.md) |
| FR-03 | Unit and task-force agent delegation | [04](04-Agent-Delegation.md) |
| FR-04 | Dynamic systems tuning agent | [05](05-Dynamic-Systems-Agent.md) |
| FR-05 | SQLite intelligence layer + provenance | [06](06-Database-Intelligence.md) |
| FR-06 | Agentic dev infrastructure (MCP, skills) | [07](07-Agentic-Infrastructure.md) |
| FR-07 | In-simulation agent architecture | [08](08-Agentic-Architecture.md) |
| FR-08 | Near-future and speculative platforms | [09](09-Near-Future-Technologies.md), [10](10-Speculative-Systems.md) |
| FR-09 | Scenario/mission editor (schema, validation, CLI/MCP; NL authoring Phase N) | [11](11-Agentic-Mission-Editor.md) |
| FR-10 | Shared vocabulary | [12](12-Terms-Glossary.md) |
| FR-11 | Doctrine, ROE, EMCON, WRA | [13](13-Doctrine-ROE-EMCON-WRA.md) |
| FR-12 | Engagement and fire control | [14](14-Engagement-And-Fire-Control.md) |
| FR-13 | Sensors, detection, EW | [15](15-Sensor-Detection-And-EW.md) |
| FR-14 | Logistics and magazines | [16](16-Logistics-And-Magazines.md) |
| FR-15 | Replay, order log, AAR | [17](17-Replay-AAR-And-Order-Log.md) |
| FR-16 | Multi-domain combat | [18](18-Combat-Domains.md) |
| FR-17 | Cyber and comms degradation | [19](19-Cyber-And-Comms.md) |
| FR-18 | Command-and-control UI | [20](20-Command-And-Control-UI.md) |
| FR-19 | Platform/catalog editor (Excel write-gate round-trip) | [21](21-Platform-Editor.md) |

## Non-Functional Requirements

- **Determinism:** seeded sim + order log; replay goldens in CI ([17](17-Replay-AAR-And-Order-Log.md))
- **Headless execution:** .NET 8 test harness and batch AvA without Unity Editor ([03](03-Simulation-Modes.md), [ADR-010](../../docs/architecture/adr-010-headless-first-command-driven-ui.md))
- **Clean-room:** no CMO proprietary DB/code; pattern reuse only (see Core Project Goals §5)
- **Performance:** 5,000+ entities at 60+ FPS on recommended spec; ≥256× headless AvA floor, 1000×+ target ([03](03-Simulation-Modes.md))
- **Reference hardware (v1 targets):** the sim is CPU-bound (DOTS/ECS); GPU demands are modest for a map-centric presentation
  - *Minimum (PC):* 6-core CPU (Ryzen 5 5600 / Core i5-12400 class), 16 GB RAM, GTX 1660 / RX 5600-class GPU, SSD — reference Baltic scenario at 30+ FPS
  - *Recommended (PC):* 8-core CPU (Ryzen 7 7700 / Core i7-13700 class), 32 GB RAM, RTX 3060-class GPU — 5,000+ entities at 60+ FPS
  - *Headless AvA node:* 8 vCPU / 16 GB Linux container — ≥256× effective speed on the reference Baltic scenario
- **Platforms:** Windows 10/11 x64 (primary, Steam); Linux x64 for the headless server/batch farm only. No macOS, console, or Steam Deck support in v1.
- **Moddability:** database and scenario modding supported at v1 — published SQLite schema and scenario format, consistent with the clean-room community-evidence intake ([06](06-Database-Intelligence.md)). Code/plugin modding out of scope for v1.
- **Localization:** English-only at v1. All UI strings externalized from day one (no hardcoded text) so later localization is a data task, not a refactor ([20](20-Command-And-Control-UI.md)).
- **Accessibility (v1 commitments):** colorblind-safe map symbology — shape-coded per MIL-STD-2525 conventions, never color-alone; scalable UI text; full keyboard operability of all command functions (follows from the command-driven UI, [ADR-010](../../docs/architecture/adr-010-headless-first-command-driven-ui.md)); no gameplay-critical information conveyed by audio alone. Screen-reader support out of scope for v1.
- **Agent safety:** delegation changes reviewable in order log; no hidden sim mutations from UI

### Standing engineering invariants (Release law)

These are load-bearing production constraints (see AGENTS.md). Charter-level; do not weaken without ADR + user decision:

| Invariant | Rule |
|-----------|------|
| Test floor | Solution tests **≥1232**, 0 gate failures (monotonic; known UA exclusions documented) |
| Replay hash | Production Baltic v2 hash **`17144800277401907079`** preserved unless golden ADR |
| ReplayGolden | **6/6** v2 suite green |
| C2 smoke | PlayModeSmokeHarness **18/18** |
| DelegationBridge | **Zero-touch** on hotpath through Release v1 |
| CatalogWriteGate | **Extend-only** write paths |
| Baltic v3 isolation | `baltic-v3-*` policies/goldens independent of v2 |

**Performance scale targets** (5k@60 FPS, ≥256× AvA) remain north-star under Success Criteria OV-SC-N1 — not the current CI gate.

## Technical Considerations

- **Engine:** Unity 6.3 LTS presentation; simulation and delegation in headless .NET assemblies ([ADR-010](../../docs/architecture/adr-010-headless-first-command-driven-ui.md))
- **Data:** `ProjectAegis.Data` SQLite catalog with provenance gates ([06](06-Database-Intelligence.md)); CatalogWriteGate extend-only
- **Bridge:** `DelegationBridge` / `UnitDetailBridge` — CRITICAL upstream; GitNexus impact required before edits; zero-touch hotpath policy
- **Authoring surface (active program):** scenario documents + Validation Engine + CLI/MCP ([11](11-Agentic-Mission-Editor.md)); platform Excel path ([21](21-Platform-Editor.md), [ADR-011](../../docs/architecture/adr-011-platform-editor-excel-roundtrip.md))
- **Verification:** `dotnet test`, PlayMode smoke harness, replay golden suite, hash grep — see Standing invariants

## Agentic Capabilities

- **In-simulation (product):** personality-bound agents, autonomy tiers, order-log reviewability ([04](04-Agent-Delegation.md), [08](08-Agentic-Architecture.md)) — shipped Partial+ at MVP; C2 badge/UI polish remains
- **Authoring agents (product program):** MCP/CLI scenario tools + validation ([11](11-Agentic-Mission-Editor.md)); NL freeform authoring remains Phase N where reserved
- **Development (process):** Claude/Cursor + Superpowers skills + GitNexus/Hindsight per [07](07-Agentic-Infrastructure.md) — not a player-facing success criterion

## Future Extensibility

1. **Active:** Scenario editor program (req 11) — complete headless ACs, then Unity edit-mode Phase 2 ([roadmap 2026-07-04](../../docs/reports/future-sprint-roadpmap-07042026.md))
2. **Active / Partial+:** Platform editor (req 21) polish (live Editor evidence)
3. **Later:** Global/strategic campaigns and live multiplayer (out of first commercial release)
4. **Later:** External RL/co-simulation agents on the headless farm
5. **Data-extensible:** Additional theaters and classification tiers without engine fork

## Open Questions / Decisions Needed

| Topic | Status | Notes |
|-------|--------|-------|
| Commercial product name | **Open** | Working title *Project Aegis* |
| Future Combat Mode default | **Deferred** | Optional scenario flag per [10](10-Speculative-Systems.md) |
| Charter items in docs 02–03 | **Locked** | See Resolved Design Decisions in [02](02-Core-Gameplay-Loop.md), [03](03-Simulation-Modes.md) |
| Reference hardware & platforms | **Decided 2026-06-09** | Windows primary + Linux headless; specs in Non-Functional Requirements |
| Modding / localization / accessibility scope | **Decided 2026-06-09** | Data modding yes, code modding no; English-only with externalized strings; targeted accessibility commitments — see Non-Functional Requirements |

## Related Requirements Index

Design status = per-doc header. Implementation grades = [implementation tracker](../implementation-tracker-2026-07-04.md). This table is a navigation map, not the grade sheet.

| Doc | Title | Design status (header) | Notes |
|-----|-------|------------------------|-------|
| [02](02-Core-Gameplay-Loop.md) | Core Gameplay Loop | Locked | Tracker: Partial |
| [03](03-Simulation-Modes.md) | Simulation Modes | Locked | Tracker: Partial+ |
| [04](04-Agent-Delegation.md) | Agent Delegation | Locked | Tracker: Partial+ |
| [05](05-Dynamic-Systems-Agent.md) | Dynamic Systems Agent | Locked | Tracker: Partial+ |
| [06](06-Database-Intelligence.md) | Database Intelligence | Locked | Tracker: Partial |
| [07](07-Agentic-Infrastructure.md) | Agentic Infrastructure | Locked | Tracker: Partial |
| [08](08-Agentic-Architecture.md) | Agentic Architecture | Locked | Tracker: Partial |
| [09](09-Near-Future-Technologies.md) | Near-Future Technologies | Research-integrated | Tracker: Partial |
| [10](10-Speculative-Systems.md) | Speculative Systems | Research-integrated | Tracker: Partial+ |
| [11](11-Agentic-Mission-Editor.md) | Agentic Mission Editor | **Revised** (2026-07-01) | Active program S81–S88; `AME-*` |
| [12](12-Terms-Glossary.md) | Terms Glossary | Locked | Tracker: Partial |
| [13](13-Doctrine-ROE-EMCON-WRA.md) | Doctrine / ROE / EMCON / WRA | Draft | Tracker: Partial |
| [14](14-Engagement-And-Fire-Control.md) | Engagement & Fire Control | Draft | Tracker: Partial+ |
| [15](15-Sensor-Detection-And-EW.md) | Sensor, Detection & EW | Draft | Tracker: Partial (MVP COVERED) |
| [16](16-Logistics-And-Magazines.md) | Logistics & Magazines | Draft | Tracker: Partial |
| [17](17-Replay-AAR-And-Order-Log.md) | Replay, AAR & Order Log | Draft | Tracker: Partial |
| [18](18-Combat-Domains.md) | Combat Domains | Draft | Tracker: Partial+ |
| [19](19-Cyber-And-Comms.md) | Cyber & Comms | Draft | Tracker: Partial |
| [20](20-Command-And-Control-UI.md) | Command & Control UI | Draft | Tracker: Partial |
| [21](21-Platform-Editor.md) | Platform Editor | Draft | Tracker: MVP-done / Partial+; FR-19 |

Status snapshot **2026-07-08** (Wave 0 hub re-baseline). Per-doc headers remain authoritative for design Status; tracker for implementation.
