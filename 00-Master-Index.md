# Game Requirements — Master Index

**Project:** Project Aegis — near-future hardcore military simulation (CMO-style, agentic)  
**Last Updated:** 2026-06-04  
**Canonical index:** [Game-Requirements/Game-Requirements-Index.md](Game-Requirements/Game-Requirements-Index.md)  
**Implementation status:** [Game-Requirements/implementation-tracker-2026-06-04.md](Game-Requirements/implementation-tracker-2026-06-04.md)

> The May 28 index listed only docs 01–10. Requirements **11–20** (simulation, C2, combat) live under `Game-Requirements/requirements/` and are indexed in the canonical file above.

## Core (01–03)

| Doc | Title |
|-----|--------|
| [01](Game-Requirements/requirements/01-Project-Overview.md) | Project overview |
| [02](Game-Requirements/requirements/02-Core-Gameplay-Loop.md) | Core gameplay loop (5 phases) |
| [03](Game-Requirements/requirements/03-Simulation-Modes.md) | Simulation modes |

## Agent & intelligence (04–06)

| Doc | Title |
|-----|--------|
| [04](Game-Requirements/requirements/04-Agent-Delegation.md) | Agent delegation |
| [05](Game-Requirements/requirements/05-Dynamic-Systems-Agent.md) | Dynamic speculative systems agent |
| [06](Game-Requirements/requirements/06-Database-Intelligence.md) | Database intelligence layer |

## Architecture (07–08)

| Doc | Title |
|-----|--------|
| [07](Game-Requirements/requirements/07-Agentic-Infrastructure.md) | Agentic infrastructure |
| [08](Game-Requirements/requirements/08-Agentic-Architecture.md) | Agentic architecture |

## Content (09–10)

| Doc | Title |
|-----|--------|
| [09](Game-Requirements/requirements/09-Near-Future-Technologies.md) | Near-future technologies |
| [10](Game-Requirements/requirements/10-Speculative-Systems.md) | Speculative / black-project systems |

## Authoring & reference (11–12)

| Doc | Title |
|-----|--------|
| [11](Game-Requirements/requirements/11-Agentic-Mission-Editor.md) | Agentic mission editor |
| [12](Game-Requirements/requirements/12-Terms-Glossary.md) | Terms glossary |

## Simulation, C2, combat (13–20)

| Doc | Title |
|-----|--------|
| [13](Game-Requirements/requirements/13-Doctrine-ROE-EMCON-WRA.md) | Doctrine, ROE, EMCON, WRA |
| [14](Game-Requirements/requirements/14-Engagement-And-Fire-Control.md) | Engagement & fire control |
| [15](Game-Requirements/requirements/15-Sensor-Detection-And-EW.md) | Sensors, detection, EW |
| [16](Game-Requirements/requirements/16-Logistics-And-Magazines.md) | Logistics & magazines |
| [17](Game-Requirements/requirements/17-Replay-AAR-And-Order-Log.md) | Replay, AAR, order log |
| [18](Game-Requirements/requirements/18-Combat-Domains.md) | Combat domains |
| [19](Game-Requirements/requirements/19-Cyber-And-Comms.md) | Cyber & communications |
| [20](Game-Requirements/requirements/20-Command-And-Control-UI.md) | Command & control UI |

## Additional files

| File | Purpose |
|------|---------|
| [Data-Population-CMAODB.md](Game-Requirements/Data-Population-CMAODB.md) | CMO DB reference crawl & catalog intake (clean-room) |
| [Claude-Agent-Setup.md](Game-Requirements/Claude-Agent-Setup.md) | Agent / MCP / Unity tooling setup |
| [research-traceability.md](Game-Requirements/research-traceability.md) | Research → requirements mapping |
| [cmo-manual-traceability.md](Game-Requirements/cmo-manual-traceability.md) | CMO manual → requirements mapping |

## Traceability & reviews

See [Game-Requirements-Index.md](Game-Requirements/Game-Requirements-Index.md) for architecture links, GDDs, ADRs, and design-review verdicts.

## Agentic implementation workflow

1. Read MVP row in [implementation-tracker-2026-06-04.md](Game-Requirements/implementation-tracker-2026-06-04.md)  
2. `gitnexus_impact` before symbol edits  
3. Skills: `team-simulation`, `team-data`, `replay-verify`, `c-sharp-engineer`  
4. Verify: `dotnet test ProjectAegis.sln` (314 tests on `main`)