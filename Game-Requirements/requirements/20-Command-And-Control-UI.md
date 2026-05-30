# 20 - Command and Control User Interface

**Last Updated:** May 29, 2026  
**Status:** Draft — ready for design review  
**CMO basis:** Manual Ch 3–4, §6.2–7, §6.9, §1.3 multitaskers, §10.1 keyboard  
**Related:** 02 Gameplay Loop, 03 Simulation Modes, 04 Delegation, 11 Mission Editor, 12 Glossary, 13–17 sim systems

## Purpose

Define the **theater command UI**: globe map, symbology, panels, context menus, delegation overlays, and **information density** standards for a CMANO-scale wargame with agentic control.

## Vision

The UI is a **command post**, not a game HUD. It must support hours-long sessions, thousands of symbols, and instant reading of **who owns what** (human vs agent). Map-first interaction (doc 11) extends to play mode.

## CMO Parity Requirements

| Area | Manual | Aegis |
|------|--------|-------|
| Globe display | §3.1 | **P0** |
| Mouse map interaction | §3.2, §4.2 | **P0** |
| Unit/group symbology | §4.3 | **P0** NATO/APP-6 style |
| Group vs unit view | §4.4 | **P0** |
| Right-click unit context | §4.1 | **P0** |
| Side info panel | §4.5 | **P0** |
| Engage, plot course, throttle | §3.3.1–4 | **P0** |
| Doctrine/EMCON/WRA access | §3.3.12–15 | **P0** → doc 13 UI |
| Mission editor entry | §3.3.17 | **P0** → doc 11 |
| OOB, contacts, missions menus | §6.3.3, §6.8–9 | **P0** |
| Time compression | §6.3.2 | **P0** → doc 03 |
| Game options / map settings | §6.4–5 | **P1** |
| Keyboard shortcuts | §10.1 | **P1** |
| Custom overlays | §10.2 | **P2** |

## Layout Zones (Information Architecture)

**P0** Persistent zones:

| Zone | Content |
|------|---------|
| **Map (primary)** | Theater, units, contacts, missions, reference geometry |
| **Top bar** | Time, compression, pause, side, fog mode |
| **Right panel** | Selected unit/group: status, sensors, weapons, fuel, doctrine |
| **Bottom strip** | Message log (subset of doc 17) |
| **Left drawer** | OOB tree, mission list, contact list (tabbed) |

**P1** Collapsible panels for ultrawide / multitasker layouts (§1.3).

## Map and Symbology

- **P0** WGS84 globe; pan, zoom, rotate; theater quick-jump (§6.6)
- **P0** Unit icons by domain and affiliation; group marker with count
- **P0** Contact symbols distinct from friendly units (stale/datalink styling per doc 15)
- **P0** Mission areas: patrol, prosecution, strike axes, reference points (doc 11)
- **P1** LOD: cluster symbols when &gt; N icons per cell
- **P1** EMCON / EW overlay layers (doc 15)

## Delegation Overlays (Aegis unique)

- **P0** Badge on unit: human | agent | mixed; color by autonomy level (doc 04)
- **P0** Click agent badge → personality, autonomy slider, pause/resume agent
- **P0** **Intent preview** ghost (Assisted): proposed course or engage before commit
- **P1** Side-level “agent commander” panel for strategic agent
- **P0** Filter OOB: show only human-controlled / only agent-controlled

## Context Menus

**P0** Unit context (§4.1): attack options, plot course, formation, assign mission, doctrine, delegate agent, special actions.

**P0** Map context (§4.2): add reference point, create mission area, measure distance, quick place unit (editor).

**P0** All actions produce intents or orders logged (doc 17).

## Unit Detail Panel (§4.5)

- **P0** Status, sensors (with EMCON), weapons (with FireAbort preview doc 14), fuel, alt/speed
- **P0** Doctrine tab with inheritance chain (doc 13)
- **P0** “Why can’t I fire?” one-click explain

## Simulation Controls

- **P0** Pause / run; time compression presets per doc 03
- **P0** **Multitasker mode** (§1.3): bookmark camera + selection; stack pause reasons; resume restores agent state
- **P0** Mode indicator: Human / Mixed / Agent-vs-Agent

## Mission and Editor Entry

- **P0** In play: mission list + activate/deactivate (runtime doc 11)
- **P0** Edit mode toggle → Mission Board (doc 11) without separate app (unless Scenario Lab split later)

## Accessibility and Density

Per genre conventions (`docs/military-simulation/genre-conventions-reference.md`):

- **P0** Colorblind-safe affiliation palettes (shape + color)
- **P0** Keyboard focus order for OOB and message log
- **P1** Screen reader labels for critical controls
- **P0** Font scaling for message log and panels
- **P0** No single-screen requirement for 4K; minimum 1920×1080 usable

## Agentic / MCP

- **P1** `ui_capture_state` for Unity-MCP verification screenshots (coding standards)
- **P1** MCP cannot click UI — uses tools (doc 11, 14) — UI is human-first

## Non-Functional Requirements

| Area | Target |
|------|--------|
| Performance | 60 FPS map pan with 5k symbols (LOD on) |
| Responsiveness | Panel update &lt; 100 ms on selection change |
| Unity | UI Toolkit or UGUI per engine ADR; data binding to sim state |

## Acceptance Criteria

1. Select unit → panel shows effective doctrine and magazine % within one frame budget.
2. Delegate unit to agent → badge visible; pause agent stops intents.
3. Assisted mode shows ghost intent before engage; deny shows FireAbort tooltip.
4. 5000 symbols on map with LOD: pan stays above 30 FPS on target spec hardware.
5. Message log click selects unit and opens explain for referenced `sequenceId`.
6. All §4.1 core actions available without hidden modals-only paths.

## Phased Delivery

| Phase | Scope |
|-------|--------|
| **MVP** | Map, OOB, contacts, unit panel, context menu core, delegation badges, message log |
| **Phase 2** | Multitasker bookmarks, keyboard shortcuts, mission drawer, overlays |
| **Phase 3** | Custom overlays, Tacview hook UI, full accessibility audit |

## Open Questions

1. UI Toolkit vs UGUI for project (pending `/setup-engine`)?
2. Single right panel vs detachable windows for multitaskers?
3. 3D globe vs 2.5D map for performance?

## Traceability

| Doc | Relationship |
|-----|----------------|
| 02–04 | Loop, modes, delegation |
| 11 | Editor / Mission Board |
| 13–17 | Panel content and logs |
| `cmo-manual-traceability.md` | Ch 3–4, §6 |

---

**References:** CMO Manual Ch 3–4; `docs/military-simulation/genre-conventions-reference.md`; `docs/manual/index.html`
