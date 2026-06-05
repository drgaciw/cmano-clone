# 01 - Project Overview

**Last Updated:** 2026-06-04  
**Related:** 02, 03, 04, 05, 06, 07, 08, 09, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20  
**Status:** Locked  
**Research basis:** [Agentic CMO Research](../../docs/research/agentic-cmano-research.md)

## Purpose
Define the high-level vision, scope, and strategic goals for a next-generation near-future military simulation game that evolves the Command: Modern Air Naval Operations formula into the agentic AI era.

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

## Scope Boundaries (First Release)

**In Scope:**
- Regional theaters (Baltic, Black Sea, South China Sea, etc.)
- Air, naval, and limited ground forces
- Drone swarms and autonomous systems
- Full agent delegation system
- High-speed agent-vs-agent simulation mode

**Out of Scope (Future Releases):**
- Global/strategic-level campaigns (initially)
- Full multiplayer (hotseat and async supported later)
- Land warfare at battalion scale or below

## Success Criteria

- Players can run realistic 2030-era Baltic scenario with 5,000+ entities at playable speeds
- AI agents produce believable, human-like decision-making with clear personality differences
- The game becomes a platform for both entertainment and serious wargaming/research
- Strong agentic development workflow that allows rapid iteration using Claude + Unity-MCP

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
| FR-09 | Natural-language mission editor | [11](11-Agentic-Mission-Editor.md) |
| FR-10 | Shared vocabulary | [12](12-Terms-Glossary.md) |
| FR-11 | Doctrine, ROE, EMCON, WRA | [13](13-Doctrine-ROE-EMCON-WRA.md) |
| FR-12 | Engagement and fire control | [14](14-Engagement-And-Fire-Control.md) |
| FR-13 | Sensors, detection, EW | [15](15-Sensor-Detection-And-EW.md) |
| FR-14 | Logistics and magazines | [16](16-Logistics-And-Magazines.md) |
| FR-15 | Replay, order log, AAR | [17](17-Replay-AAR-And-Order-Log.md) |
| FR-16 | Multi-domain combat | [18](18-Combat-Domains.md) |
| FR-17 | Cyber and comms degradation | [19](19-Cyber-And-Comms.md) |
| FR-18 | Command-and-control UI | [20](20-Command-And-Control-UI.md) |

## Non-Functional Requirements

- **Determinism:** seeded sim + order log; replay goldens in CI ([17](17-Replay-AAR-And-Order-Log.md))
- **Headless execution:** .NET 8 test harness and batch AvA without Unity Editor ([03](03-Simulation-Modes.md), [ADR-010](../../docs/architecture/adr-010-headless-first-command-driven-ui.md))
- **Clean-room:** no CMO proprietary DB/code; pattern reuse only (see Core Project Goals §5)
- **Performance:** 5,000+ entities interactive; ≥256× headless AvA floor, 1000×+ target ([03](03-Simulation-Modes.md))
- **Agent safety:** delegation changes reviewable in order log; no hidden sim mutations from UI

## Technical Considerations

- **Engine:** Unity 6.3 LTS presentation; simulation and delegation in headless .NET assemblies
- **Data:** `ProjectAegis.Data` SQLite catalog with provenance gates ([06](06-Database-Intelligence.md))
- **Bridge:** `DelegationBridge` / `UnitDetailBridge` — CRITICAL upstream; GitNexus impact required before edits
- **Verification:** `dotnet test`, PlayMode smoke harness, replay golden suite

## Agentic Capabilities

- **Development:** Claude/Cursor + Superpowers skills + GitNexus/Hindsight per [07](07-Agentic-Infrastructure.md), [08](08-Agentic-Architecture.md)
- **In-game:** personality-bound agents, autonomy tiers, mission editor NL authoring ([04](04-Agent-Delegation.md), [11](11-Agentic-Mission-Editor.md))

## Future Extensibility

- Global/strategic campaigns and live multiplayer (out of first release)
- External RL/co-simulation agents attached to headless farm
- Additional theaters and classification tiers in data layer without engine fork

## Open Questions / Decisions Needed

| Topic | Status | Notes |
|-------|--------|-------|
| Commercial product name | **Open** | Working title *Project Aegis* |
| Future Combat Mode default | **Deferred** | Optional scenario flag per [10](10-Speculative-Systems.md) |
| Charter items in docs 02–03 | **Locked** | See Resolved Design Decisions in [02](02-Core-Gameplay-Loop.md), [03](03-Simulation-Modes.md) |

## Related Requirements Index

| Doc | Title | Sprint wave |
|-----|-------|-------------|
| [02](02-Core-Gameplay-Loop.md) | Core Gameplay Loop | 12 |
| [03](03-Simulation-Modes.md) | Simulation Modes | 12 |
| [04](04-Agent-Delegation.md) | Agent Delegation | 13 |
| [05](05-Dynamic-Systems-Agent.md) | Dynamic Systems Agent | 13 |
| [06](06-Database-Intelligence.md) | Database Intelligence | 14 |
| [07](07-Agentic-Infrastructure.md) | Agentic Infrastructure | 14 |
| [08](08-Agentic-Architecture.md) | Agentic Architecture | 14 |
| [09](09-Near-Future-Technologies.md) | Near-Future Technologies | 15 |
| [10](10-Speculative-Systems.md) | Speculative Systems | 15 |
| [11](11-Agentic-Mission-Editor.md) | Agentic Mission Editor | 15 |
| [12](12-Terms-Glossary.md) | Terms Glossary | 15 |
| [13](13-Doctrine-ROE-EMCON-WRA.md) | Doctrine / ROE / EMCON / WRA | 15 |
| [14](14-Engagement-And-Fire-Control.md) | Engagement & Fire Control | 15 |
| [15](15-Sensor-Detection-And-EW.md) | Sensor, Detection & EW | 15 |
| [16](16-Logistics-And-Magazines.md) | Logistics & Magazines | 15 |
| [17](17-Replay-AAR-And-Order-Log.md) | Replay, AAR & Order Log | 15 |
| [18](18-Combat-Domains.md) | Combat Domains | 15 |
| [19](19-Cyber-And-Comms.md) | Cyber & Comms | 15 |
| [20](20-Command-And-Control-UI.md) | Command & Control UI | 15 |

---

**Status:** Core vision locked; Template A complete (Sprint 12)