# Project Aegis

Project Aegis is a working-title near-future hardcore military simulation inspired by Command: Modern Air Naval Operations. The project focuses on theater-level command, autonomous systems, drone swarms, advanced AI agents, and emerging military technologies set roughly 5-10 years in the future.

## Vision

Create a next-generation wargame that combines traditional military simulation depth with agentic AI capabilities. The player acts as a theater commander, directing human and AI-driven forces while delegating tactical decisions to specialized agents where appropriate.

## Current Status

The repository contains requirements documentation, architecture (ADRs), GDDs, and **.NET 8** libraries for delegation and the simulation core scaffold:

| Area | Path | Notes |
|------|------|--------|
| Delegation | `src/ProjectAegis.Delegation/` | Controllers, orchestration, decision pipeline (NUnit tests) |
| Sim core | `src/ProjectAegis.Sim/` | `SimTickRunner`, clock, seeded RNG, `IPolicyEvaluator` (no UnityEngine) |
| Unity bridge | `src/ProjectAegis.Delegation.UnityAdapter/` | `ISimWorldSnapshot` in, `IOrderSink` out; `RoePolicyAdapter` (ADR-002) |
| Unity wiring | `unity/ProjectAegis/` | DLL copy + optional `DelegationBridgeHost` — **Editor not active yet** |
| Console demo | `src/ProjectAegis.Delegation.Demo/` | Headless delegation walkthrough |

**Design / implementation history (delegation):**

- `docs/superpowers/specs/2026-05-28-agent-delegation-framework-design.md`
- `docs/superpowers/plans/2026-05-28-agent-delegation-framework.md`

Build and test (requires [.NET 8 SDK](https://dotnet.microsoft.com/download)):

```bash
dotnet test ProjectAegis.sln -v minimal
dotnet run --project src/ProjectAegis.Delegation.Demo
```

**Not started yet:** full Unity `Assets/` project, `ProjectAegis.Sim.DOTS`, detection/engage systems, and production C2 UI. Requirements live under `Game-Requirements/`.

## Tech Stack

**Unity 6.3 LTS** (editor `6000.3.14f1`) + **C# / .NET 8**, with agentic tooling (Cursor, Copilot, Claude Code), **Unity-MCP**, **Claude-Code-Game-Studios**, **GitNexus**, **Microsoft Learn** ([.NET reference](docs/engine-reference/dotnet/README.md)), and **Context7**. Claude/MCP repo wiring is **configured**; Unity Editor activation is pending until the Unity project is fully scaffolded.

- [Tech Stack - Agentic Game Development](Tech-Stack.md)
- [Claude Agent Setup](Game-Requirements/Claude-Agent-Setup.md)

## Requirements

Start here:

- [Game Requirements Master Index](Game-Requirements/Game-Requirements-Index.md)

Core documents:

- [Project Overview](Game-Requirements/requirements/01-Project-Overview.md)
- [Core Gameplay Loop](Game-Requirements/requirements/02-Core-Gameplay-Loop.md)
- [Simulation Modes](Game-Requirements/requirements/03-Simulation-Modes.md)

Agent and intelligence systems:

- [Agent Delegation System](Game-Requirements/requirements/04-Agent-Delegation.md)
- [Dynamic Speculative Systems Agent](Game-Requirements/requirements/05-Dynamic-Systems-Agent.md)
- [Database Intelligence Layer](Game-Requirements/requirements/06-Database-Intelligence.md)
- [Agentic Infrastructure Framework](Game-Requirements/requirements/07-Agentic-Infrastructure.md)
- [Agentic Architecture Layer](Game-Requirements/requirements/08-Agentic-Architecture.md)

Content and systems:

- [Near-Future Technologies](Game-Requirements/requirements/09-Near-Future-Technologies.md)
- [Speculative & Black Project Systems](Game-Requirements/requirements/10-Speculative-Systems.md)

Simulation and C2 (CMO manual traceability — see [matrix](Game-Requirements/cmo-manual-traceability.md)):

- [Agentic Mission Editor](Game-Requirements/requirements/11-Agentic-Mission-Editor.md)
- [Terms Glossary](Game-Requirements/requirements/12-Terms-Glossary.md)
- [Doctrine, ROE, EMCON, WRA](Game-Requirements/requirements/13-Doctrine-ROE-EMCON-WRA.md)
- [Engagement & Fire Control](Game-Requirements/requirements/14-Engagement-And-Fire-Control.md)
- [Sensors, Detection & EW](Game-Requirements/requirements/15-Sensor-Detection-And-EW.md)
- [Logistics & Magazines](Game-Requirements/requirements/16-Logistics-And-Magazines.md)
- [Replay, AAR & Order Log](Game-Requirements/requirements/17-Replay-AAR-And-Order-Log.md)
- [Combat Domains](Game-Requirements/requirements/18-Combat-Domains.md)
- [Cyber & Comms](Game-Requirements/requirements/19-Cyber-And-Comms.md)
- [Command & Control UI](Game-Requirements/requirements/20-Command-And-Control-UI.md)

## Key Concepts

- Human, mixed, and fully autonomous simulation modes
- AI agent delegation for units, groups, and task forces
- Dynamic discovery and proposal of emerging military systems
- Database intelligence agents for validation, normalization, and change tracking
- Near-future systems including loyal wingman UAVs, drone swarms, hypersonic weapons, directed energy weapons, autonomous underwater vehicles, advanced electronic warfare, and quantum sensors

## Project Phase

Requirements detailing is in progress. Docs 01–20 and the [CMO manual traceability matrix](Game-Requirements/cmo-manual-traceability.md) are drafted. Design: [systems index](design/gdd/systems-index.md), [architecture](docs/architecture/architecture.md) (ADRs accepted), GDDs ([policy](design/gdd/policy-roe-emcon-wra.md), [order log](design/gdd/order-log-replay.md), [sim core](design/gdd/simulation-core-time.md), [sensors](design/gdd/sensor-detection-ew.md)). Code: `ProjectAegis.Sim` + delegation wired via `RoePolicyAdapter` (ADR-002). Engine: [Unity 6.3 LTS](docs/engine-reference/unity/VERSION.md). Next: DLZ/magazines, JSON scenario import, open `unity/ProjectAegis` in Unity Hub (`tools/init-unity-project.ps1`).
