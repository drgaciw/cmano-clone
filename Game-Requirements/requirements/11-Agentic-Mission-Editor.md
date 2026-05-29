# 11 - Agentic Mission & Scenario Editor

**Last Updated:** May 29, 2026  
**Status:** Draft — ready for design review  
**Basis:** [CMO Official Manual](https://www.matrixgames.com/amazon/PDF/CMO/CMO_manual_EBOOK.pdf) (Mission Editor §7.1, Scenario Editor §4.1.5, ScenEdit §5), [Command Lua API](https://commandlua.github.io/), and Project Aegis requirements 01–10.

## Purpose

Define requirements for an **agentic mission and scenario editor** that preserves the depth and designer power of Command: Modern Operations (CMO) while delivering a uniquely improved authoring experience: natural-language and AI-assisted creation, deterministic validation, version control, headless generation, and first-class integration with Project Aegis delegation and agent-vs-agent simulation.

## Vision

Scenario design should feel like **theater planning with an expert staff**, not fighting a modal-heavy desktop tool. Human designers retain full authority; AI agents act as planners, validators, and batch testers. Every editor action is **machine-readable**, **replayable**, and **MCP-addressable** so Claude/Cursor can co-author scenarios alongside humans in Unity or headless pipelines.

## CMO Baseline — What We Must Match or Exceed

The following capabilities are **parity requirements** derived from the CMO manual and community tooling. Project Aegis must not regress on any item marked **P0**.

### Scenario lifecycle (CMO Scenario Editor)

| Capability | CMO behavior | Aegis requirement |
|------------|--------------|-------------------|
| Create blank scenario | Edit mode, empty theater | **P0** — `CreateScenario` with theater, start time, duration |
| Load / save scenario | `.scen` + side briefing, DB version match | **P0** — versioned scenario package with embedded DB snapshot reference |
| Scenario features & settings | Locked in play; editable in editor (magazines, realism toggles) | **P0** — feature flags as typed config; diffable |
| Side selection & briefing | Per-side briefing text, optional side picker | **P0** — structured briefing (objectives, ROE summary, intel) |
| Database binding | Scenario tied to DB version; rebuild shallow/deep | **P0** — integrate with Database Intelligence Layer (doc 06) |

### Mission system (CMO Mission Editor §7.1)

Missions group units for AI tasking. CMO mission types and options become **first-class mission archetypes** in Aegis:

| Mission type | CMO purpose | Aegis notes |
|--------------|-------------|-------------|
| **Strike** | Attack assigned targets; TOT/TOS; weapon behavior; auto-planner | **P0** — include flight-plan generation, package assignment |
| **Patrol** | Patrol / prosecution areas; ASW, ASuW, AAW, SEAD, CAS variants | **P0** — separate patrol vs prosecution geometry |
| **Support** | Tanker, AEW, EW jamming, etc. | **P0** |
| **Ferry** | Redeploy aircraft between bases | **P0** |
| **Mining** | Lay mines in area | **P1** |
| **Mine-clearing** | Clear mines | **P1** |
| **Cargo — Delivery** | Unload cargo to map / facility | **P1** |
| **Cargo — Transfer** | Move cargo between holding units (like ferry destination pick) | **P1** |

**Mission parameters (all types where applicable):**

- Rules of engagement, doctrine, EMCON (inheritance from parent unit with override prompts — CMO §RoE/Doctrine)
- Reference points: patrol zones, prosecution zones, waypoints, stations
- Formation editor: relative/fixed bearing stations; diamond station markers on map
- Operations planner: mission priority queue, time-phased mission switching (e.g., patrol until T, then strike)
- Task pools and packages (strike packaging, shared asset pools)
- Unit assignment: drag-assign units/groups; clone missions
- Investigate / engage options on patrol (within weapon range, outside patrol area)

### Events & scripting (CMO ScenEdit)

| Capability | CMO behavior | Aegis requirement |
|------------|--------------|-------------------|
| Event editor (TCA) | Triggers, Conditions, Actions | **P0** — visual + declarative (JSON/YAML) representation |
| Lua API | `ScenEdit_*`, `Tool_*` functions | **P0** — scripted layer; migrate toward **typed event DSL** + optional Lua compatibility shim |
| Lua console | In-game REPL for testing | **P1** — editor/debug console with MCP mirror |
| Persistent scenario state | `ScenEdit_SetKeyValue` / `GetKeyValue` | **P0** — scenario variables store |
| Side posture & doctrine API | Hostile/neutral, doctrine get/set | **P0** |

### Designer ergonomics (CMO pain points → Aegis improvements)

| CMO limitation | Aegis improvement |
|----------------|-------------------|
| Heavy modal/tab mission UI | Unified **Mission Board** + map-first editing |
| Manual time-based mission handoff | **Operations timeline** with scheduled mission activation (CMO operations planner++, visual) |
| Lua pasted into opaque action boxes | **Versioned script modules**, linting, and agent-generated scripts with review |
| Weak validation until playtest | **Continuous validation agent** (ORBAT, fuel, magazines, mission feasibility) |
| No semantic diff / collaboration | **Git-friendly scenario format** + AI-generated change summaries |
| Scenario creation is expert-only | **NL authoring**: “Baltic 2032, NATO defensive, 2x drone swarms, staggered SEAD then strike” |

## Functional Requirements

### 1. Editor modes

1. **Play mode** — scenario features locked per design; matches CMO play behavior.
2. **Edit mode** — full authoring; auto-pause or low-speed sim optional for live testing.
3. **Headless edit mode** — CLI/MCP-only authoring for CI and agent batch generation (no UI).

### 2. Map-first authoring

- Place, move, clone, and group units on operational map (air, surface, subsurface, land facilities, satellites).
- Draw and edit reference geometries: polygons, circles, lines, corridors; snap and measure tools.
- Layer toggles: ORBAT, missions, EMCON, contacts (test), EW, airspace, mining zones.
- **Minimap + theater bounds** with scale-aware icon density (LOD).

### 3. Mission Board (replaces CMO mission editor UX)

- Single view listing all missions by side, type, status (active/scheduled/complete), assigned units.
- **Add mission** wizard: type → geometry → parameters → assign units → validate.
- Inline editors for doctrine/ROE/EMCON with inheritance visualization (parent ship → embarked helo).
- **Clone mission**, **template library** (community and project-shipped templates).
- Flight plan preview for air missions (ETA, tanker segments, bingo fuel warnings).

### 4. Operations timeline

- Gantt-style timeline binding missions to start/end triggers (absolute time, relative time, event trigger).
- Priority stack per unit: which mission wins when overlapping (CMO operations planner parity).
- Conditions: “when patrol complete”, “when contact destroyed”, “when zone entered”, “when variable set”.
- Simulation scrub in editor: preview timeline at 1x without full playthrough.

### 5. Event & trigger system (ScenEdit successor)

**Declarative event model** (human- and agent-editable):

```yaml
events:
  - id: red_launch_swarm
    trigger: { type: Time, after: "T+02:00:00" }
    conditions:
      - { type: SidePosture, a: NATO, b: RED, is: Hostile }
    actions:
      - { type: ActivateMission, missionId: RED_SWARM_STRIKE }
      - { type: Message, side: NATO, text: "SIGINT: mass drone launch detected." }
```

- **P0** trigger types: Time, UnitDestroyed, UnitEntersZone, ContactDetected, Variable, MissionComplete, SidePostureChange, ScoreThreshold.
- **P0** action types: ActivateMission, DeactivateMission, SpawnUnit, RemoveUnit, SetVariable, Message/Briefing, ChangeDoctrine, SetWeather, TeleportUnit (editor test only), EndScenario.
- **P1** Lua/DSL hybrid: compile declarative events to runtime; optional Lua block for advanced logic.
- Event debugger: log firing order, skipped conditions, action results; export to XML/JSON (CMO `Tool_DumpEvents` equivalent).

### 6. Agentic authoring agents

Dedicated editor agents (see also docs 04, 07, 08):

| Agent | Responsibility |
|-------|----------------|
| **Mission Planner Agent** | Proposes ORBAT, missions, reference points from NL brief or objective list |
| **Red Force Agent** | Designs plausible adversary missions, EMCON, and triggers |
| **Validation Agent** | Blocks export on fuel, magazine, unreachable targets, empty patrol zones, DB mismatches |
| **Balance Agent** | Runs quick headless samples; flags overpowered force ratios |
| **Briefing Writer Agent** | Generates side briefings, intel paragraphs, victory conditions |
| **Migration Agent** | Imports CMO scenarios (where legally/technically permitted) and maps to Aegis format |

**Human-in-the-loop rules:**

- Agents **propose** changes as preview diffs; nothing commits without explicit accept (or configurable auto-accept for headless CI).
- Every agent edit stores: prompt, rationale, diff hash, approving user/agent id.

### 7. Natural language & MCP interface

**P0** MCP tools (Unity-MCP + headless CLI):

| Tool | Description |
|------|-------------|
| `scenario_create` | Create scenario from structured params or NL brief |
| `scenario_load` / `scenario_save` | IO with validation |
| `mission_add` / `mission_update` / `mission_delete` | Typed mission CRUD |
| `mission_assign_units` | Assign ORBAT to mission |
| `reference_point_set` | Geometry CRUD |
| `event_add` / `event_validate` | Event graph CRUD |
| `scenario_validate` | Run all validation rules |
| `scenario_simulate_sample` | Headless N-minute sample at 100x+ |
| `scenario_export_brief` | Player-facing briefing export |

NL examples the system must support:

- “Add a SEAD patrol over Gotland from H+0 to H+2, then transition assigned fighters to a strike on the amphib group.”
- “Give RED a cautious doctrine but aggressive swarm coordinator on the Shahed package.”
- “What missions have no assigned units?” / “Why is Strike 3 invalid?”

### 8. Integration with gameplay systems

- **Delegation (doc 04):** Default agent personalities per side/mission; mission-level autonomy presets.
- **Simulation modes (doc 03):** Editor can set recommended mode and default autonomy per side.
- **Database Intelligence (doc 06):** All units resolved through DB; inline “add unit” searches validated catalog.
- **Scenario Generation Agent (doc 07):** Editor is the human-facing surface; batch generator uses same file format.

### 9. Import, export, and versioning

- **P0** Native format: `*.aegis-scenario` (ZIP: manifest JSON, missions, events, ORBAT, briefings, metadata).
- **P1** CMO import pipeline (best-effort): missions, RP, sides, events → Aegis mapping table documented separately.
- **P0** Git-friendly: canonical JSON with stable key ordering; optional binary cache for large scenarios.
- **P0** Semantic diff: “Strike Alpha +2 units, Patrol Bravo area moved 20nm east”.
- Scenario **schema version** with automated migrators.

### 10. Testing & playtest from editor

- **Instant playtest** from current edit state (selected side or observer).
- **Deterministic seed** displayed and copyable for bug reports.
- **Quick run**: 5 / 15 / 60 minute accelerated sim with summary (losses, mission success, event log).
- Link to **AAR Agent** (doc 07) on playtest completion.

## Non-Functional Requirements

| Area | Target |
|------|--------|
| Performance | Edit 5,000+ unit ORBAT without UI freeze; background validation |
| Determinism | Same scenario file + seed → identical event order in headless sample |
| Accessibility | Keyboard-first mission board; screen-reader labels on mission list |
| Security | No arbitrary code execution in play mode; script sandbox in editor |
| Localization | All player-facing briefing strings externalized |
| Audit | Full edit log per scenario for multiplayer design and research reproducibility |

## UX Principles (differentiators vs CMO)

1. **Map-first, list-second** — geometry on the map drives mission creation, not tabs alone.
2. **Explain every validation error** — with fix suggestions (“Assign tanker to Support mission 2 or reduce strike radius”).
3. **Timeline over memory** — designers should not track H+2 handoffs manually.
4. **Agents as staff officers** — proposals, not silent auto-edits.
5. **Same format for human and machine** — NL and MCP produce the same JSON mission objects.

## Data Model (high level)

```
Scenario
├── metadata (title, description, author, schemaVersion, dbRef)
├── features (magazines, realism, time compression limits)
├── sides[]
│   ├── briefing, doctrine defaults, score
│   └── postures[]
├── orbat (units, groups, bases, cargo)
├── referencePoints[]
├── missions[] (typed: Strike | Patrol | Support | Ferry | Mining | MineClear | CargoDelivery | CargoTransfer)
├── operationsTimeline[] (scheduled mission transitions)
├── events[] (trigger, conditions[], actions[])
├── variables{}
└── editorState (non-runtime: camera, layers, last validation)
```

## Acceptance Criteria

1. Designer can recreate a **Baltic-style defensive scenario** with patrol, support, ferry, and time-phased strike without Lua.
2. **Mission Planner Agent** generates a draft scenario from a 3-paragraph NL brief; human accepts ≥80% of mission structure with minor edits (playtest metric).
3. **Validation Agent** catches: empty patrol area, strike with no targets, ferry without destination, DB version mismatch.
4. All **P0 mission types** assignable with doctrine/ROE/EMCON inheritance and override.
5. **MCP tool suite** can create, validate, and run a 15-minute headless sample without opening Unity UI.
6. Scenario file diffs are human-readable in Git.
7. Event debugger shows why an event did or did not fire in a quick run.

## Phased Delivery

| Phase | Scope |
|-------|--------|
| **MVP** | Edit mode, map ORBAT, Strike/Patrol/Support/Ferry, reference points, basic events, save/load, validation agent, core MCP tools |
| **Phase 2** | Operations timeline, mining/cargo, templates, NL Mission Planner Agent, CMO import (best-effort) |
| **Phase 3** | Full event DSL + Lua shim, Red Force Agent, collaborative review, Steam Workshop–style sharing (if product requires) |

## Open Questions / Decisions Needed

1. Legal/policy stance on **CMO scenario import** — technical feasibility vs licensing.
2. **Lua compatibility**: full `ScenEdit_*` shim vs curated subset for Year 1.
3. Should **agent-authored scenarios** be labeled in multiplayer/briefing for transparency?
4. Maximum **event graph complexity** before performance warnings (soft cap)?
5. Editor runs **inside game client** only, or standalone **Scenario Lab** desktop app sharing core library?

## Traceability

| Related doc | Relationship |
|-------------|--------------|
| 01 Project Overview | Theater-scale scenarios, agentic gameplay |
| 02 Core Gameplay Loop | Phase 1–2 planning and mission assignment |
| 04 Agent Delegation | Mission-level autonomy and personalities |
| 07 Agentic Infrastructure | Scenario Generation, Event agents |
| 08 Agentic Architecture | Deterministic sim, MCP, headless execution |
| 06 Database Intelligence | Unit data, validation, DB version binding |

---

**References**

- [CMO Manual (PDF)](https://www.matrixgames.com/amazon/PDF/CMO/CMO_manual_EBOOK.pdf) — §3.3.17 Mission Editor, §4.1.5 Scenario Editor, §5 ScenEdit, §7 Missions and Reference Points
- [Command Lua Documentation](https://commandlua.github.io/)
- [CMO Scenario Editor Manual Addendum](https://command.matrixgames.com/?page_id=2709)
