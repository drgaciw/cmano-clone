# Systems Index: Project Aegis

> **Status:** Draft  
> **Created:** 2026-05-29  
> **Last Updated:** 2026-05-29  
> **Source Concept:** `design/gdd/game-concept.md`  
> **Requirements traceability:** `Game-Requirements/cmo-manual-traceability.md`

---

## Overview

Project Aegis is a **theater-level military simulation** with a **deterministic ECS sim core**, a **delegation layer** (already prototyped in `ProjectAegis.Delegation`), **data-driven platforms** (CMANO-scale DB), and **agentic authoring** (mission editor, MCP). Systems below map CMO manual coverage to implementable modules and link to requirement docs **13–20**.

---

## Systems Enumeration

| # | System Name | Category | Priority | Status | Req Doc | GDD | Depends On |
|---|-------------|----------|----------|--------|---------|-----|------------|
| 1 | Simulation Core & Time | Sim Core | MVP | In Review | 03, 08 | [simulation-core-time.md](simulation-core-time.md) | — |
| 2 | Order Log & Replay | Sim Core | MVP | In Design | 17 | [order-log-replay.md](order-log-replay.md) | 1 |
| 3 | Policy, ROE, EMCON, WRA | Sim Core | MVP | In Design | 13 | [policy-roe-emcon-wra.md](policy-roe-emcon-wra.md) | 1, 2 |
| 4 | Platform Database | Content | MVP | Partial | 06 | — | — |
| 5 | Sensor & Contact Model | Simulation | MVP | In Review | 15 | [sensor-detection-ew.md](sensor-detection-ew.md) | 1, 4 |
| 6 | Engagement & Fire Control | Simulation | MVP | In Progress (DLZ/mag MVP in `ProjectAegis.Sim`) | 14 | [engagement-fire-control.md](engagement-fire-control.md) | 3, 5, 16 |
| 7 | Logistics & Magazines | Simulation | MVP | Not Started | 16 | — | 4, 1 |
| 8 | Combat Domains & Damage | Simulation | MVP | Not Started | 18 | — | 6, 5, 7 |
| 9 | Mission Runtime | Gameplay | MVP | Partial (editor 11) | 11, 07 | — | 1, 3, 5 |
| 10 | Agent Delegation | Agentic | MVP | In Progress | 04, 08 | — | 1, 3, 9 |
| 11 | Scenario & Mission Editor | Content | MVP | In Design (GDD draft) | 11 | [agentic-mission-editor.md](agentic-mission-editor.md) | 4, 9 |
| 12 | Command & Control UI | UI | MVP | Not Started | 20 | — | 1–10 |
| 13 | Message Log & Briefing UI | UI | MVP | Not Started | 17, 20 | — | 2, 12 |
| 14 | Simulation Modes & Headless | Sim Core | MVP | Partial (`SimulationSession`, JSON scenario ROE) | 03 | — | 1, 10 |
| 15 | Near-Future Systems | Content | Vertical Slice | Not Started | 09 | — | 6, 8 |
| 16 | Cyber & Comms Degradation | Simulation | Vertical Slice | Not Started | 19 | — | 5, 10, 2 |
| 17 | Scoring & Losses | Gameplay | MVP | Not Started | 17 | — | 2, 7, 8 |
| 18 | Agentic Infrastructure (AAR, batch) | Agentic | Vertical Slice | Draft | 07 | — | 2, 14 |
| 19 | Speculative Systems Module | Content | Full Vision | Draft | 10 | — | 15 |
| 20 | Database Intelligence Pipeline | Content | MVP | Draft | 06 | — | 4, 11 |

---

## Categories (Project Aegis)

| Category | Description |
|----------|-------------|
| **Sim Core** | Tick, determinism, time compression, serialization, headless |
| **Simulation** | Sensors, engage, logistics, combat, cyber |
| **Gameplay** | Missions, scoring, modes |
| **Agentic** | Delegation, AI policies, MCP, AAR agents |
| **Content** | DB, scenarios, near-future/speculative data |
| **UI** | Globe, panels, symbology, editor surfaces |

---

## Priority Tiers

| Tier | Definition |
|------|------------|
| **MVP** | Minimum for Baltic-style vertical slice: plan → fight → replay |
| **Vertical Slice** | Polish + near-future + cyber + AAR agents |
| **Full Vision** | Campaigns, multiplayer, speculative full set |

---

## Dependency Map (Design Order)

### Foundation Layer

1. **Simulation Core & Time** — fixed tick, seed, world state  
2. **Platform Database** — units, sensors, weapons, magazines  
3. **Order Log & Replay** — extend `DecisionLog`; contract for all subsystems  

### Core Layer

4. **Policy, ROE, EMCON, WRA** — snapshots + evaluator (extend `IRoeFilter`)  
5. **Sensor & Contact Model** — feeds `ObservedState` / bridge  
6. **Logistics & Magazines** — constraints for missions and fire  
7. **Engagement & Fire Control** — unified pipeline  
8. **Mission Runtime** — execute doc 11 missions on sim  

### Feature Layer

9. **Combat Domains & Damage** — domain validators, facilities  
10. **Agent Delegation** — integrate with 3–9 (code exists; widen contracts)  
11. **Simulation Modes & Headless** — batch runners, metrics  
12. **Scenario & Mission Editor** — authoring (req 11 largely done)  

### Presentation Layer

13. **Command & Control UI** — map, panels, delegation overlays  
14. **Message Log & Briefing UI** — player-facing log from order log  

### Extension Layer

15. **Near-Future Systems** — swarms, hypersonics, DEW hooks  
16. **Cyber & Comms** — datalink degrade, order delay  
17. **Agentic Infrastructure** — AAR, validation agents, MCP expansion  
18. **Scoring & Losses** — scenario outcomes, batch CSV  

---

## Requirements → System Mapping

| Req ID | Title | Primary system # |
|--------|-------|------------------|
| 13 | Doctrine/ROE/EMCON/WRA | 3 |
| 14 | Engagement | 6 |
| 15 | Sensors/EW | 5 |
| 16 | Logistics | 7 |
| 17 | Replay/AAR | 2, 17, 18 |
| 18 | Combat domains | 8 |
| 19 | Cyber/comms | 16 |
| 20 | C2 UI | 12, 13 |

---

## GitNexus Implementation Notes

- Before editing **`IRoeFilter`**, run: `npx gitnexus impact --repo cmano-clone -d upstream IRoeFilter` (**HIGH** risk).  
- Before editing **`DecisionLog`**, run: `npx gitnexus impact --repo cmano-clone -d upstream DecisionLog` (**LOW** risk).  
- After adding sim assembly: `npx gitnexus analyze` in repo root.

---

## Next Design Actions

| Order | Action | Command |
|-------|--------|---------|
| 1 | ~~Policy GDD~~ | Done → `policy-roe-emcon-wra.md` |
| 2 | ~~Master architecture~~ | Done → `docs/architecture/architecture.md` + ADR-001..005 |
| 3 | ~~Order log GDD~~ | Done → `order-log-replay.md` |
| 4 | ~~Sim core GDD~~ | Done → `simulation-core-time.md` |
| 5 | ~~Setup engine~~ | Unity 6.3 LTS → `docs/engine-reference/unity/` |
| 6 | ~~ADRs 001–005~~ | **Accepted** |
| 7 | ~~`ProjectAegis.Sim` scaffold~~ | `src/ProjectAegis.Sim` + tests |
| 8 | ~~Engagement resolver stub~~ | `IEngagementResolver` in `ProjectAegis.Sim.Engage` |
| 9 | ~~Engagement pipeline wired~~ | `SimTickPipeline` + `SimulationSession` |
| 10 | ~~Scenario ROE templates~~ | `ScenarioPolicyCatalog` + `scenarioPolicyId` |
| 11 | ~~Order log union~~ | `ChronologicalEntries` / `ComputeFingerprint` |
| 12 | **Engagement implementation** | DLZ, magazines, real launches |
| 13 | **Unity Editor** | `tools/init-unity-project.ps1` + Hub 6.3 |
| 9 | ~~Wire orchestrator → `IPolicyEvaluator`~~ | Done — `RoePolicyAdapter` + MVP `PolicyEvaluator` |
| 10 | `npx gitnexus analyze` | After each sim merge |

**Recommended next GDD:** **Order Log & Replay** (system 2) — resolves design-review blocker C1 (`DecisionLog` vs doc 17).

**Architecture:** [architecture.md](../../docs/architecture/architecture.md) · ADRs in `docs/architecture/`

---

## Changelog

| Date | Change |
|------|--------|
| 2026-05-30 | Mission editor GDD (system 11) authored: 8 sections, 4 mission types, typed event DSL, Validation Agent, fuel + event-order formulas, 8 ACs |
| 2026-05-30 | Mission editor concept (system 11): intent-compiler spine, DSL-only logic, engine-agnostic core; open Qs 1/2/5 resolved |
| 2026-05-29 | Unity 6.3 pin, sim/sensor GDDs, ADRs accepted, `ProjectAegis.Sim` scaffold |
| 2026-05-29 | Policy GDD + master architecture + ADR stubs; sim-core GDD skeleton |
| 2026-05-29 | Initial index from requirements 13–20 + GitNexus delegation analysis |
