# 20 - Command and Control User Interface

**Last Updated:** 2026-07-27  
**Status:** Draft — Template B (Wave 2 re-honesty)  
**CMO basis:** Manual Ch 3–4, §6.2–7, §6.9, §1.3 multitaskers, §10.1 keyboard; map/layer/view instructional parity (clean-room)  
**Related:** [01](01-Project-Overview.md), [02](02-Core-Gameplay-Loop.md), [03](03-Simulation-Modes.md), [04](04-Agent-Delegation.md), [11](11-Agentic-Mission-Editor.md), [12](12-Terms-Glossary.md), [13](13-Doctrine-ROE-EMCON-WRA.md)–[17](17-Replay-AAR-And-Order-Log.md), [19](19-Cyber-And-Comms.md) · [implementation tracker 2026-07-04](../implementation-tracker-2026-07-04.md)  
**Architecture (normative):** [ADR-010 Headless-First / Command-Driven UI](../../docs/architecture/adr-010-headless-first-command-driven-ui.md) · [ADR-007 C2 Map Presentation](../../docs/architecture/adr-007-c2-map-presentation.md)

## Purpose

Define the **theater command UI**: map, symbology, panels, context menus, delegation overlays, and **information density** standards for a CMANO-scale wargame with agentic control.

Implements hub **FR-18** ([01](01-Project-Overview.md)).

> **Normative architecture.** [ADR-010](../../docs/architecture/adr-010-headless-first-command-driven-ui.md) is **normative** for this document: Unity presentation is a **command-driven client** over headless .NET core (`ProjectAegis.Data` / `Sim` / `Delegation`). UI binds **read-only projections** and submits **validated commands** only — never authoritative sim state. Map presentation phasing is governed by [ADR-007](../../docs/architecture/adr-007-c2-map-presentation.md) (Phase A placeholder → Phase B Cesium / WGS84 → Phase C APP-6 LOD).

## Vision

The UI is a **command post**, not a game HUD. It must support long sessions, dense symbology, and instant reading of **who owns what** (human vs agent). Map-first interaction (doc 11) extends to play mode. **Product globe, multitasker, and 5k@60 FPS** remain **Phase N / Deferred** north-star targets (hub **OV-SC-N1**), not the current CI gate.

## CMO Parity Requirements

| Area | Manual | Aegis | Honesty |
|------|--------|-------|---------|
| Globe display | §3.1 | **P0 product intent** | **Phase N / Deferred** — Phase A `MapPlaceholderPanelHost` **Shipped**; full Cesium/WGS84 product globe **Partial** (ADR-007 Phase B) |
| Mouse map interaction | §3.2, §4.2 | **P0** | **Partial** — placeholder + selection; product pan/zoom/rotate globe deferred |
| Unit/group symbology | §4.3 | **P0** NATO/APP-6 style | **Partial** — basic affiliation markers; APP-6 atlas **Phase N** (ADR-007 Phase C) |
| Group vs unit view | §4.4 | **P0** | **Partial** |
| Right-click unit context | §4.1 | **P0** | **Partial** — core attack/delegate paths via smoke proxies |
| Side info panel | §4.5 | **P0** | **Partial** — right unit detail host |
| Engage, plot course, throttle | §3.3.1–4 | **P0** | **Partial** — engage preview / command path; full CMO parity open |
| Doctrine/EMCON/WRA access | §3.3.12–15 | **P0** → doc 13 UI | **Partial** — `DoctrineInheritancePanelHost` |
| Mission editor entry | §3.3.17 | **P0** → doc 11 | **Partial** — mission list host; full editor GUI Phase N |
| OOB, contacts, missions menus | §6.3.3, §6.8–9 | **P0** | **Partial / Shipped** — left drawer tabs |
| Time compression | §6.3.2 | **P0** → doc 03 | **Partial** — top bar hosts |
| Game options / map settings | §6.4–5 | **P1** | Open |
| Keyboard shortcuts | §10.1 | **P1** | Open / Partial focus order |
| Custom overlays | §10.2 | **P2** | Phase N |
| Multitasker layouts | §1.3 | was **P0** | **Phase N / Deferred** — collapsible multi-monitor bookmarks not shipped product |

## Layout Zones (Information Architecture)

**Persistent zones** — layout chrome is **Partial / Shipped** (UI Toolkit hosts + PlayModeSmoke 18/18). Do not demote zone presence; polish and product map fidelity remain open.

| Zone | Content | Status |
|------|---------|--------|
| **Map (primary)** | Theater symbols, contacts, missions, reference geometry | **Partial / Shipped** (placeholder map); product globe **Phase N** |
| **Top bar** | Time, compression, pause, side, fog / COMMS legend, Begin Execution | **Partial / Shipped** (`C2TopBarPanelHost`) |
| **Right panel** | Selected unit/group: status, sensors, weapons, fuel, doctrine | **Partial / Shipped** (`RightUnitPanelHost`, `DoctrineInheritancePanelHost`) |
| **Bottom strip** | Message log (subset of doc 17) | **Partial / Shipped** (`MessageLogPanelHost`) |
| **Left drawer** | OOB tree, mission list, contact list (tabbed) | **Partial / Shipped** (`C2LeftDrawerPanelHost`, related hosts) |

**Phase N / Deferred:** detachable multitasker windows and ultrawide multi-bookmark layouts (§1.3).

## Functional Requirements (major IDs)

| ID | Requirement | Status |
|----|-------------|--------|
| **CMD-01** | Persistent layout zones (map, top bar, right detail, message log, left drawer) | **Partial / Shipped** |
| **CMD-02** | UI Toolkit presentation stack (not UGUI) for C2 hosts | **Shipped** |
| **CMD-03** | Headless-first command-driven binding per ADR-010 (projections in; commands out) | **Shipped** (contract); panel polish open |
| **CMD-04** | Top bar: time, compression, pause, mode, Begin Execution | **Partial / Shipped** |
| **CMD-05** | Message log projection + row selection / sequence deep-link | **Partial / Shipped** |
| **CMD-06** | Map symbol picture (placeholder Phase A; product globe Phase N) | **Partial** (placeholder **Shipped**; Cesium **Partial**) |
| **CMD-07** | Unit/group selection sync (map ↔ OOB ↔ right panel) | **Partial / Shipped** |
| **CMD-08** | Doctrine inheritance / effective policy panel (doc 13) | **Partial / Shipped** |
| **CMD-09** | Delegation badges and autonomy affordances (doc 04) | **Partial** |
| **CMD-10** | Context menus: engage / plot / doctrine / delegate core actions | **Partial** |
| **CMD-11** | Intent preview / FireAbort explain surfaces (Assisted; docs 14/13) | **Partial** |
| **CMD-12** | Accessibility: colorblind-safe affiliation, font scaling, keyboard focus (v1 commitments in hub) | **Partial** |
| **CMD-13** | Product WGS84 globe + APP-6 LOD at theater scale | **Phase N / Deferred** (ADR-007 B/C; hub **OV-SC-N1**) |
| **CMD-14** | Multitasker bookmarks / multi-monitor detachable chrome | **Phase N / Deferred** |
| **CMD-15** | 5,000 symbols @ 60 FPS interactive map performance | **Phase N / Deferred** (hub **OV-SC-N1** — north-star, not CI gate) |

### Operator interface modules (CUI-*)

CMO unit/navigation/combat UI instructional parity (clean-room). Full Obsidian note: vault `Projects/cmano-clone/requirements/CMO Interface Modules.md`.

| ID | Requirement | Priority / maturity |
|----|-------------|---------------------|
| **CUI-01** | Right **unit status panel**: proficiency, DB/catalog link, identifiers | **P0** — **Partial / Shipped** |
| **CUI-02** | Live **course, speed, altitude/depth, fuel** binding | **P0** — **Partial / Shipped** |
| **CUI-03** | **Loadout** inspect/manage (munitions, pods) | **P0** — **Partial** |
| **CUI-04** | Contact **detect / classify / rename** | **P0** — classify **Shipped** headless; rename UI **Phase N** |
| **CUI-05** | **Meta-grouping** for collective move/formation, keep unit data | **P0** — **Partial+** groups |
| **CUI-06** | **F3** course plot with distance/bearing feedback | **P0** — **Phase N** chrome |
| **CUI-07** | Waypoint **drag** and **insert** | **P0** — **Phase N** |
| **CUI-08** | Throttle presets (Loiter/Cruise/Military/Afterburner) + alt/depth MSL/AGL | **P0** — **Phase N** |
| **CUI-09** | Terrain/obstacle **avoidance** | **P1** — **Phase N** |
| **CUI-10** | **Automatic** engagement (threat + resources + ROE) | **P0** — **Partial+** |
| **CUI-11** | **Manual** weapon/quantity engagement | **P0** — **Partial / Shipped** |
| **CUI-12** | **Bearing-only** launch | **P1** — **Phase N** |
| **CUI-13** | Per-sensor **toggle** (stealth/EMCON) | **P0** — EMCON Partial; per-sensor chrome **Phase N** |
| **CUI-14** | **Chaff** and **decoys** | **P1** — **Phase N** |
| **CUI-15** | **Ctrl-hover** weapon/sensor summary cards | **P1** — **Phase N** |
| **CUI-16** | Hotkeys **F1** Attack, **F3** Move/plot | **P0** — **Phase N** full |
| **CUI-17** | Overlays: fuel/weapon **range rings**, **cloud** layer | **P0/P1** — **Phase N** (see MAP-*) |

### Keyboard command map (KEY-*) — CMO §10.1 parity

| ID | Requirement | Priority / maturity |
|----|-------------|---------------------|
| **KEY-01** | Capability-class keyboard map (not proprietary key ownership) | **P0** — **Phase N** |
| **KEY-02** | Remap + CMO instructional vs Aegis default parity table | **P0** — **Phase N** |
| **KEY-03** | Engage/Attack class (F1 instructional; **CUI-16**) | **P0** — **Phase N** full; command path **Partial** |
| **KEY-04** | Plot/Move class (F3 instructional) | **P0** — **Phase N** |
| **KEY-05** | Group operations class | **P1** — **Phase N** |
| **KEY-06** | Doctrine access class | **P0** — panel **Partial**; hotkey **Phase N** |
| **KEY-07** | Air ops class (F6; LOG-14) | **P0** — **Phase N** |
| **KEY-08** | Boat ops class (F7; LOG-11) | **P1** — **Phase N** |
| **KEY-09** | Bearing-only launch class | **P1** — **Phase N** |
| **KEY-10** | Map measure range/bearing (**MAP-23**) | **P1** — **Phase N** |
| **KEY-11** | Insert/place unit + RP place | **P0** edit / **P1** play — chrome **Phase N** |
| **KEY-12** | Mode-aware bindings, cancel, focus, time/pause | **P0** — **Partial** |

### Special Actions play surface (SPA-*)

| ID | Requirement | Priority / maturity |
|----|-------------|---------------------|
| **SPA-01** | First-class `specialActions[]` scenario objects | **P0** — **Phase N** (authoring req 11) |
| **SPA-02** | Side-scoped SPA list (Game menu parity) | **P0** — **Phase N** |
| **SPA-03** | Unit-scoped SPA on context/unit chrome | **P0** — **Phase N** |
| **SPA-04** | Invoke UI + optional keyboard palette | **P0** — **Phase N** |
| **SPA-05** | Lock/Available/Exhausted states | **P0** — **Phase N** |
| **SPA-06** | Invoke → order log (req 17) | **P0** — **Phase N** |
| **SPA-07** | Optional event linkage (typed DSL; no Lua v1) | **P0** — **Phase N** |
| **SPA-08** | Editor CRUD + validation + semantic diff | **P0** — **Phase N** |

## Map and Symbology

- **Partial / Shipped:** Phase A tactical map placeholder — normalized symbols from `MapPictureProjection` via `MapPlaceholderPanelHost` ([ADR-007](../../docs/architecture/adr-007-c2-map-presentation.md))
- **Partial:** Contact styling distinct from friendly; EMCON / COMMS affordances on C2 chrome (docs 15, 19)
- **Partial:** Mission areas / reference points as projection data permits
- **Phase N / Deferred:** Full WGS84 Cesium globe product (pan, zoom, rotate, theater quick-jump); APP-6 / NATO icon atlas; LOD clustering for thousands of icons
- **Phase N / Deferred:** Full EW overlay product layers

### Map, layer & view system (MAP-*)

CMO map/layer/view instructional parity (clean-room). Brand names (Sentinel-2, BMG, Stamen, etc.) denote **capability classes**, not mandatory third-party embeds. Obsidian working note: vault `Projects/cmano-clone/requirements/Map and View System.md`.

#### Layer management (toggleable)

| ID | Capability | Priority / maturity |
|----|------------|---------------------|
| **MAP-01** | HD **satellite basemap** | **P1** — **Phase N** |
| **MAP-02** | Political/geographic **boundaries** | **P1** — **Phase N** |
| **MAP-03** | **Relief** elevation (color-coded) supporting LOS | **P0 intent** — **Phase N** |
| **MAP-04** | Infrastructure / OSM-class basemap | **P1** — **Phase N** |
| **MAP-05** | **Roads and cities** overlay | **P1** — **Phase N** |
| **MAP-06** | **Land cover** (desert, built-up, crop, …) affecting move/damage/detect | **P0 intent** — **Phase N** |
| **MAP-07** | **Custom layers** (PNG + world-file georef) | **P1** — **Phase N** |
| **MAP-08** | Toggleable **place names** | **P1** — **Phase N** |
| **MAP-09** | **Day/night lighting** → visual/IR sensor effectiveness | **P1** — **Phase N** |

#### Selection, ranges, tactical overlays

| ID | Requirement | Priority / maturity |
|----|-------------|---------------------|
| **MAP-10** | Left-click selection | **P0** — **Partial / Shipped** |
| **MAP-11** | Drag-box multi-selection | **P0** — **Phase N** |
| **MAP-12** | **Group View** for nested units | **P0** — **Partial** |
| **MAP-13** | Toggle sensor / weapon **range** rings | **P0** — **Phase N** |
| **MAP-14** | **Aircraft range** from fuel, speed, consumption | **P1** — **Phase N** |
| **MAP-15** | **Merged sensor** coverage (clutter reduction) | **P1** — **Phase N** |
| **MAP-16** | **Illumination channels** (FC radar lock) | **P1** — **Phase N** |
| **MAP-17** | **Targeting vectors** attacker→target | **P1** — **Phase N** / intent ghost Partial |
| **MAP-18** | **Contact emissions** passive/active cues | **P1** — **Phase N** |
| **MAP-19** | **LOS tool** altitude-aware visibility | **P0 intent** — **Phase N** |

#### Mission geometry & navigation tools

| ID | Requirement | Priority / maturity |
|----|-------------|---------------------|
| **MAP-20** | **No-fly zones** geofence pathfinding / auto-move | **P1** — **Phase N** |
| **MAP-21** | **Reference points** (Ctrl+Insert parity) | **P0** — **Partial+** headless |
| **MAP-22** | **Plotted courses** path visualization | **P0** — **Partial** |
| **MAP-23** | **Range and bearing** measure (Ctrl+D parity) | **P1** — **Phase N** |
| **MAP-24** | **Mini-maps** supplemental viewports | **P1** — **Phase N** |

## Delegation Overlays (Aegis unique)

- **Partial:** Badge on unit: human | agent | mixed; autonomy affordances (doc 04)
- **Partial:** Agent pause/resume and personality surfaces where hosts exist
- **Partial:** Intent preview / engage ghost (Assisted) via projection + smoke paths
- **Phase N:** Side-level “agent commander” strategic panel
- **Partial:** OOB filters for human-controlled / agent-controlled where implemented

## Context Menus

**Partial (P0 intent):** Unit context (§4.1): attack options, plot course, formation, assign mission, doctrine, delegate agent, special actions — core paths exercised via C2 / attack menu proxies; full CMO parity open.

**Partial (P0 intent):** Map context (§4.2): reference points, mission areas, measure, editor place — product completeness open.

**Partial / Shipped contract:** Authoritative actions produce intents or orders logged (doc 17); UI does not mutate sim internals ([ADR-010](../../docs/architecture/adr-010-headless-first-command-driven-ui.md)).

## Unit Detail Panel (§4.5)

- **Partial / Shipped:** Status, sensors, weapons, fuel, alt/speed via right unit panel host
- **Partial / Shipped:** Doctrine tab / inheritance chain host (doc 13)
- **Partial:** “Why can’t I fire?” / FireAbort explain surfaces (docs 13–14)

## Simulation Controls

- **Partial / Shipped:** Pause / run; time compression chrome on top bar (doc 03)
- **Phase N / Deferred:** **Multitasker mode** (§1.3): bookmark camera + selection; stack pause reasons; multi-monitor restore
- **Partial:** Mode indicator: Human / Mixed / Agent-vs-Agent (doc 03)

## Mission and Editor Entry

- **Partial:** In play: mission list + activate/deactivate (runtime doc 11)
- **Phase N:** Full edit-mode Mission Board GUI without separate app (unless Scenario Lab split — doc 11 / ADR-017)

## Accessibility and Density

Per genre conventions (`docs/military-simulation/genre-conventions-reference.md`) and hub NFRs:

- **Partial / v1 commitment:** Colorblind-safe affiliation palettes (shape + color)
- **Partial:** Keyboard focus order for OOB and message log
- **Phase N:** Screen reader labels for critical controls (hub: out of scope for v1 product gate)
- **Partial:** Font scaling for message log and panels
- **Partial:** Minimum 1920×1080 usable; no single-screen 4K requirement

## Agentic / MCP

- **Partial / P1:** `ui_capture_state` / Unity-MCP verification screenshots where tooling exists
- **Shipped contract:** MCP cannot click UI — uses tools (docs 07, 11, 14) — UI is human-first; same command surface as headless

## Non-Functional Requirements

| Area | Target | Honesty |
|------|--------|---------|
| Performance (product) | 60 FPS map pan with 5k symbols (LOD on) | **Phase N / Deferred** — hub **OV-SC-N1**; not the current CI gate |
| Responsiveness | Panel update &lt; 100 ms on selection change | **Partial** — projection bind timing tested; full Editor frame p95 open |
| Unity stack | **UI Toolkit shipped** for C2 hosts | **Resolved** — not UGUI for project C2 |
| Architecture | Headless-first command-driven UI | **Normative** — [ADR-010](../../docs/architecture/adr-010-headless-first-command-driven-ui.md) |
| Map presentation | Placeholder → Cesium → APP-6 | [ADR-007](../../docs/architecture/adr-007-c2-map-presentation.md) |

## Implementation Mapping

| Area | Path / type | Status | Evidence |
|------|-------------|--------|----------|
| C2 presentation facade | `C2PresentationController` (`ProjectAegis.Delegation.UnityAdapter` / Unity runtime) | **Partial / Shipped** | PlayModeSmoke + controller tests |
| Top bar | `C2TopBarPanelHost` | **Partial / Shipped** | `C2TopBarBeginExecutionTests`, PlayModeSmoke |
| Message log | `MessageLogPanelHost` + `MessageLogProjection` | **Partial / Shipped** | PlayModeSmoke |
| Map Phase A | `MapPlaceholderPanelHost` + `MapPictureProjection` | **Partial / Shipped** | PlayModeSmoke; ADR-007 Phase A |
| Doctrine panel | `DoctrineInheritancePanelHost` | **Partial / Shipped** | Doctrine panel / smoke proxies |
| Left drawer / OOB | `C2LeftDrawerPanelHost`, `OobTreePanelHost`, mission/contact hosts | **Partial / Shipped** | PlayModeSmoke |
| Right unit detail | `RightUnitPanelHost` | **Partial / Shipped** | PlayModeSmoke |
| Sensor / COMMS C2 strip | `SensorC2PanelHost`, related bridges | **Partial / Shipped** | SensorC2 / C2Comms tests |
| C2 proxy gate | `PlayModeSmokeHarnessTests` | **Shipped** | **18/18** |
| Cesium / globe Phase B | Cesium bridge / host (where present) | **Partial** | ADR-007 Phase B; not full product globe |
| Product multitasker / 5k@60 | — | **Phase N / Deferred** | Hub **OV-SC-N1** |

## Acceptance Criteria

| # | Criterion | Evidence policy |
|---|-----------|-----------------|
| 1 | Select unit → panel shows effective doctrine and magazine % within panel budget | Smoke + projection tests; full frame budget **Partial** |
| 2 | Delegate unit to agent → badge visible; pause agent stops intents | **Partial** — delegation hosts + doc 04 paths |
| 3 | Assisted mode shows ghost intent before engage; deny shows FireAbort tooltip | **Partial** — engage preview projections |
| 4 | 5000 symbols on map with LOD: pan stays above 30 FPS on target hardware | **Deferred** — **OV-SC-N1**; not CI gate |
| 5 | Message log click selects unit and opens explain for referenced `sequenceId` | **Partial / Shipped** path in smoke proxies |
| 6 | Core §4.1 actions available without hidden modals-only paths | **Partial** — command-driven hosts |

## Phased Delivery

| Phase | Scope | Honesty |
|-------|--------|---------|
| **MVP / shipped chrome** | Layout zones, OOB, contacts, unit panel, message log, top bar, delegation badges (proxy), UI Toolkit hosts | **Partial / Shipped** — PlayModeSmoke **18/18** |
| **Phase 2** | Mission drawer polish, keyboard shortcuts, overlays, doctrine/engage explain UX | **Partial / open** |
| **Phase 3 / N** | Product Cesium globe, APP-6 LOD, custom overlays, multitasker, Tacview hook UI, full accessibility audit, 5k@60 | **Phase N / Deferred** |

## Open Questions

| # | Question | Status |
|---|----------|--------|
| 1 | UI Toolkit vs UGUI for project? | **Resolved — UI Toolkit shipped** for C2 hosts (ADR-010 / Unity 6.3 presentation). Not reopened without ADR. |
| 2 | Single right panel vs detachable windows for multitaskers? | **Open** — product multitasker **Phase N / Deferred**; single-panel layout is current shipped chrome |
| 3 | 3D globe vs 2.5D map for performance? | **Partially resolved** — ADR-007: Phase A placeholder **Shipped**; product WGS84 globe via Cesium **Partial / Phase N**; not a blocking open for C2 chrome |

## Traceability

| Doc | Relationship |
|-----|----------------|
| [01](01-Project-Overview.md) | Hub **FR-18**; **OV-SC-N1** scale deferral; accessibility NFRs |
| [02](02-Core-Gameplay-Loop.md)–[04](04-Agent-Delegation.md) | Loop, modes, delegation overlays |
| [11](11-Agentic-Mission-Editor.md) | Editor / Mission Board entry |
| [13](13-Doctrine-ROE-EMCON-WRA.md)–[17](17-Replay-AAR-And-Order-Log.md) | Panel content, engage explain, message log |
| [19](19-Cyber-And-Comms.md) | COMMS legend / degrade affordances on C2 chrome |
| [ADR-010](../../docs/architecture/adr-010-headless-first-command-driven-ui.md) | **Normative** headless-first command-driven UI |
| [ADR-007](../../docs/architecture/adr-007-c2-map-presentation.md) | Map presentation phases |
| `cmo-manual-traceability.md` | Ch 3–4, §6 |
| GDD / UX | `design/gdd/command-and-control-ui.md`, `design/ux/c2-command-post.md` |

---

**References:** CMO Manual Ch 3–4; `docs/military-simulation/genre-conventions-reference.md`; `docs/manual/index.html`; [ADR-010](../../docs/architecture/adr-010-headless-first-command-driven-ui.md); [ADR-007](../../docs/architecture/adr-007-c2-map-presentation.md)

**Implementation grade:** Partial — see [implementation-tracker-2026-07-04.md](../implementation-tracker-2026-07-04.md) row 20. Design Status remains **Draft** (Template B). Charter re-honesty: Wave 2 2026-07-08.
