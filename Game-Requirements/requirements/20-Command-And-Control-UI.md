# 20 - Command and Control User Interface

**Last Updated:** July 8, 2026  
**Status:** Committed — rev 2 (post-implementation baseline; UX review 2026-07-08)  
**CMO basis:** Manual Ch 3–4, §6.2–7, §6.9, §1.3 multitaskers, §10.1 keyboard  
**Related:** 02 Gameplay Loop, 03 Simulation Modes, 04 Delegation, 11 Mission Editor, 12 Glossary, 13–17 sim systems  
**Review:** [requirements-20-ux-review-2026-07-08.md](../reviews/requirements-20-ux-review-2026-07-08.md)  
**Implementation status:** All MVP layout zones shipped as UI Toolkit panel hosts — see zone ownership table in `design/gdd/command-and-control-ui.md`.

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
| Keyboard shortcuts (core set) | §10.1 | **P0** (rev 2 — pause, compression, cycle unit, engage, center-on-threat `F`; remap stub IDs per `design/accessibility-requirements.md` §6.3) |
| Keyboard shortcuts (full map) | §10.1 | **P1** |
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

*(Rev 2 additions — review F6/F9; S37 playtest + accessibility lean review note #3):*

- **P0** Symbology standard: committed **APP-6(D) frame subset** (affiliation frames + domain modifiers used by docs 13–17); shape remains the primary cue per `design/accessibility-requirements.md` §5.
- **P0** Icon size ladder per zoom band (theater / regional / tactical); labels appear from regional zoom, declutter by priority (selected &gt; engaged &gt; hostile &gt; friendly) with leader lines before hiding.
- **P1** In-UI symbology + COMMS-state legend (toggleable overlay/tooltip) — resolves S37 session 9 confusion point and accessibility review note #3.
- **P1** Narrow-panel density rule: kill-chain / dependency rows truncate with expand-on-hover, never wrap beyond two lines (S37 session 9).

## Delegation Overlays (Aegis unique)

- **P0** Badge on unit: human | agent | mixed; color by autonomy level (doc 04)
- **P0** Click agent badge → personality, autonomy slider, pause/resume agent
- **P0** **Intent preview** ghost (Assisted): proposed course or engage before commit
- **P1** Side-level “agent commander” panel for strategic agent
- **P0** Filter OOB: show only human-controlled / only agent-controlled
- **P1** **Agent activity digest** (rev 2 — review F7): summarized list of agent-issued intents since the player last focused that unit/bookmark ("while you were away"); rows link to log `sequenceId`. Absorbs the S37 "in-fight graph attribution overlay" residual.

## Selection and Command Model

*(Added rev 2 — review F1/F2. CMO basis: §4.1–4.4 multi-unit operations, §3.3 order flow.)*

### Selection

- **P0** Single select: map pick or OOB row (shipped). Selection is presentation state, never sim state.
- **P0** Multi-select: drag-box on map; shift-click add/remove; ctrl-click multi-row in OOB tree.
- **P0** Group orders: any context-menu action valid for all selected units issues one intent per unit through the existing intent pipeline; partial-validity actions show per-unit eligibility before commit.
- **P0** Unit cycling hotkeys: next/previous friendly unit; center-on-selection.
- **P1** Saved selection sets (recall via hotkey) — pairs with multitasker bookmarks.

### Order lifecycle and feedback

- **P0** Every order surfaces a state: `accepted → queued → executing → completed | denied | aborted`. State visible in unit detail panel and message log row; denial links to the "Why can't I fire?" explain.
- **P0** Confirmation gate on weapons-release intents when ROE/doctrine requires positive control (doc 13); single-keystroke confirm, Esc cancels.
- **P0** Cancel/replan: a plotted course or queued order can be cancelled before execution; cancellation is itself a logged intent (doc 17).
- **P1** Order queue view per unit (pending intents in sequence).

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

## Alerting and Interruption

*(Added rev 2 — review F3. CMO basis: §6.4 game options message/pause settings.)*

- **P0** Event severity tiers: **Critical** (weapons inbound, unit lost, ROE breach), **Notable** (new contact, contact classification change, mission state change), **Routine** (nav, fuel routine, log-only).
- **P0** Configurable auto-pause per category (default: Critical pauses; Notable flashes; Routine log-only). Pause reasons stack per multitasker mode.
- **P0** Routing rules: Critical → transient toast + log + optional pause; Notable → log highlight; Routine → log only. Toasts never occlude the right panel; max 3 stacked; click focuses the referenced unit/`sequenceId`.
- **P0** Per-category message log filters (extends shipped log; categories per doc 17).
- **P1** Multitasker alert routing: alerts tag the bookmark/camera context they occurred in; resuming a bookmark replays its unseen Critical/Notable items.

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

## Resolution, Scaling, and Layout Adaptation

*(Added rev 2 — review F5.)*

- **P0** PanelSettings: Scale With Screen Size, reference 1920×1080, match height.
- **P0** UI scale setting 100 / 125 / 150% via `C2AccessibilitySettings.ScalePercent` (single source — `design/accessibility-requirements.md` §3); 10px evidence-text floor holds at 100%.
- **P0** 4K / high-DPI: honor OS scale factor at startup; no sub-10px rendered text at any supported combination.
- **P1** Ultrawide (≥21:9): side panels stay fixed-width; map absorbs surplus width; optional third column reserved for detachable panels (open question 2).

## Agentic / MCP

- **P1** `ui_capture_state` for Unity-MCP verification screenshots (coding standards)
- **P1** MCP cannot click UI — uses tools (doc 11, 14) — UI is human-first

## Non-Functional Requirements

| Area | Target |
|------|--------|
| Performance | Map pan with 5k symbols (LOD on): **60 FPS target on recommended spec; 30 FPS floor on min spec** (rev 2 — reconciles former NFR/AC-4 contradiction) |
| Responsiveness | Selection change: **first paint ≤ 1 frame** (placeholder acceptable), **full panel refresh ≤ 100 ms** |
| List scale | OOB tree and message log MUST use virtualized `ListView`/`TreeView`; no per-row GameObjects/VisualElements beyond viewport |
| Unity | **UI Toolkit (runtime) — committed** (rev 2; six panel hosts shipped). Data binding via projections only (ADR-010) |

## Acceptance Criteria

1. Select unit → panel first paint within one frame; effective doctrine and magazine % fully refreshed ≤ 100 ms.
2. Delegate unit to agent → badge visible; pause agent stops intents.
3. Assisted mode shows ghost intent before engage; deny shows FireAbort tooltip.
4. 5000 symbols on map with LOD: pan ≥ 60 FPS on recommended spec; ≥ 30 FPS on min spec.
5. Message log click selects unit and opens explain for referenced `sequenceId`.
6. All §4.1 core actions available without hidden modals-only paths.
7. *(rev 2)* Drag-box selects ≥ 2 units; one context-menu engage issues one logged intent per eligible unit; ineligible units listed before commit.
8. *(rev 2)* Queued order shows its lifecycle state in unit panel; cancelling it emits a logged cancellation intent.
9. *(rev 2)* Critical event with auto-pause enabled pauses the sim and shows a toast whose click focuses the source unit.
10. *(rev 2)* OOB and message log scroll with 5k rows without frame drop (virtualization proof).

## Phased Delivery

| Phase | Scope |
|-------|--------|
| **MVP** | Map, OOB, contacts, unit panel, context menu core, delegation badges, message log, **core hotkeys, multi-select + group orders, order lifecycle states, severity tiers + auto-pause (rev 2)** |
| **Phase 2** | Multitasker bookmarks, full keyboard map, mission drawer, overlays, **symbology legend, log category filters UI, agent activity digest, ultrawide layout (rev 2)** |
| **Phase 3** | Custom overlays, Tacview hook UI, full accessibility audit, **saved selection sets, order queue view, bookmark alert routing (rev 2)** |

## Open Questions

1. ~~UI Toolkit vs UGUI~~ — **Resolved rev 2:** UI Toolkit (runtime) committed; hosts shipped.
2. Single right panel vs detachable windows for multitaskers?
3. Globe technology: Cesium vs custom URP terrain — **Resolved 2026-07-09: Cesium for Unity (ADR-018)**; CI default remains MapPlaceholder (`useGlobeMap=false`). Spike evidence: `docs/engineering/cesium-phase-b-spike-checklist.md`.
4. *(rev 2)* Default auto-pause matrix per severity tier: fixed defaults or per-scenario/difficulty presets?

## Traceability

| Doc | Relationship |
|-----|----------------|
| 02–04 | Loop, modes, delegation |
| 11 | Editor / Mission Board |
| 13–17 | Panel content and logs |
| `cmo-manual-traceability.md` | Ch 3–4, §6 |
| `design/accessibility-requirements.md` | Scaling enum, remap stub IDs, colorblind cues (rev 2) |
| `../reviews/requirements-20-ux-review-2026-07-08.md` | Rev 2 rationale (F1–F10) |

---

**References:** CMO Manual Ch 3–4; `docs/military-simulation/genre-conventions-reference.md`; `docs/manual/index.html`
