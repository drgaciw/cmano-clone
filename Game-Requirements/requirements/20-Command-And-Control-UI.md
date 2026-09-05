# 20 - Command and Control User Interface

**Last Updated:** 2026-09-05 (added draft military/civilian symbology requirements; implementation not claimed)
**Status:** Draft — Template B (Wave 2 re-honesty)  
**CMO basis:** Manual Ch 3–4, §6.2–7, §6.9, §1.3 multitaskers, §10.1 keyboard  
**Related:** [01](01-Project-Overview.md), [02](02-Core-Gameplay-Loop.md), [03](03-Simulation-Modes.md), [04](04-Agent-Delegation.md), [11](11-Agentic-Mission-Editor.md), [12](12-Terms-Glossary.md), [13](13-Doctrine-ROE-EMCON-WRA.md)–[17](17-Replay-AAR-And-Order-Log.md), [19](19-Cyber-And-Comms.md) · [implementation tracker 2026-07-04](../implementation-tracker.md)  
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
| Persistent unit-order toolbar | §3.3 | **P1** | **Open** — CMD-16; only context menus specified previously |
| Aircraft Operations window | §3.3.7 | **P1** | **Partial / Phase N** — CMD-24; gated by doc 16 LOG-08 |
| Boat Operations window | §3.3.8 | **P2** | **Phase N** — CMD-25; gated by doc 16 LOG-09…11 |
| Ground formations (brigade+) | §9.2.5 | **P2** | **Open (partial scope)** — CMD-26; battalion depth excluded by doc 01 |
| Scenario library / load | §2.1 | **P0** | **Open** — CMD-27; Phase 1 of the doc 02 loop |

## Layout Zones (Information Architecture)

**Persistent zones** — layout chrome is **Partial / Shipped** (UI Toolkit hosts + PlayModeSmoke 18/18). Do not demote zone presence; polish and product map fidelity remain open.

| Zone | Content | Status |
|------|---------|--------|
| **Map (primary)** | Theater symbols, contacts, missions, reference geometry | **Partial / Shipped** (placeholder map); product globe **Phase N** |
| **Top bar** | Time, compression, pause, side, fog / COMMS legend, Begin Execution | **Partial / Shipped** (`C2TopBarPanelHost`) |
| **Right panel** | Selected unit/group: status, sensors, weapons, fuel, doctrine | **Partial / Shipped** (`RightUnitPanelHost`, `DoctrineInheritancePanelHost`) |
| **Bottom strip** | Message log (subset of doc 17) | **Partial / Shipped** (`MessageLogPanelHost`) |
| **Left drawer** | OOB tree, mission list, contact list (tabbed) | **Partial / Shipped** (`C2LeftDrawerPanelHost`, related hosts) |
| **Order toolbar** | Persistent unit-order verbs for the current selection (CMD-16) | **Open** — no host; order verbs reachable only via context menu |

**Phase N / Deferred:** detachable multitasker windows and ultrawide multi-bookmark layouts (§1.3).

## Functional Requirements (major IDs)

### Symbology additions — 2026-09-05

- **SYM-MIL-01 — Military Tactical symbology:** selectable NTDS-informed naval graphics with a documented, versioned APP-6 / MIL-STD-2525 mapping for U.S. Navy ships and supported domains. [Requirement and acceptance criteria](../drafts/2026-09-05-military-tactical-symbology.md). **Draft / not scheduled.** Extends CMD-06/12/13; does not mark the APP-6 atlas shipped.
- **SYM-CIV-01 — Custom Civilian-Friendly gameplay symbology:** original, accessible pictograms over the same projected tactical picture, with profile switching that preserves selection and simulation semantics. [Requirement and acceptance criteria](../drafts/2026-09-05-civilian-gameplay-symbology.md). **Draft / scope clarification recorded.** Provisionally an alternate profile for all units; civilian-vessel-only intent remains an open question.

Both are requirements records only. Notion holds their design records; Linear tracks delivery. Existing Phase N and release boundaries remain unchanged.

### Existing C2 requirements

| ID | Requirement | Status |
|----|-------------|--------|
| **CMD-01** | Persistent layout zones (map, top bar, right detail, message log, left drawer) | **Partial / Shipped** |
| **CMD-02** | UI Toolkit presentation stack (not UGUI) for C2 hosts | **Shipped** |
| **CMD-03** | Headless-first command-driven binding per ADR-010 (projections in; commands out) | **Shipped** (contract); panel polish open |
| **CMD-04** | Top bar: time, compression, pause, mode, Begin Execution | **Partial / Shipped** — time display disambiguated by **CMD-22**; state the compression model (multiplier vs time-per-step) explicitly |
| **CMD-05** | Message log projection + row selection / sequence deep-link | **Partial / Shipped** |
| **CMD-06** | Map symbol picture (placeholder Phase A; product globe Phase N) | **Partial** (placeholder **Shipped**; Cesium **Partial**) |
| **CMD-07** | Unit/group selection sync (map ↔ OOB ↔ right panel) | **Partial / Shipped** |
| **CMD-08** | Doctrine inheritance / effective policy panel (doc 13) | **Partial / Shipped** |
| **CMD-09** | Delegation badges and autonomy affordances (doc 04) | **Partial** — unit-granularity only; a tri-state badge cannot express "mixed". Per-axis granularity is **CMD-19** |
| **CMD-10** | Context menus: engage / plot / doctrine / delegate core actions | **Partial** — toolbar parity is **CMD-16**; both paths, not one |
| **CMD-11** | Intent preview / FireAbort explain surfaces (Assisted; docs 14/13) | **Partial** |
| **CMD-12** | Accessibility: colorblind-safe affiliation, font scaling, keyboard focus (v1 commitments in hub) | **Partial** |
| **CMD-13** | Product WGS84 globe + APP-6 LOD at theater scale | **Phase N / Deferred** (ADR-007 B/C; hub **OV-SC-N1**) |
| **CMD-14** | Multitasker bookmarks / multi-monitor detachable chrome | **Phase N / Deferred** |
| **CMD-15** | 5,000 symbols @ 60 FPS interactive map performance | **Phase N / Deferred** (hub **OV-SC-N1** — north-star, not CI gate) |
| **CMD-16** | Persistent unit-order toolbar (order verbs available without right-click) | **P1** — **Open** |
| **CMD-17** | Unknown-due-to-comms as a first-class display state, naming the cause | **P0** — **Open** (sim models degradation per doc 19; UI has no representation) |
| **CMD-18** | Domain-appropriate discrete alt/depth/throttle presets as named commands | **P1** — **Open** |
| **CMD-19** | Per-axis manual/auto delegation control and mode readout | **P0** — **Open** (implies projection change; see ADR-010 note) |
| **CMD-20** | Map distance scale (nm) + camera altitude readout | **P0** — **Open** |
| **CMD-21** | Selected-unit sensor/weapon envelope rings | **P0** — **Open** (**Phase A baseline**, *not* the Phase N EW overlay product) |
| **CMD-22** | Top bar: scenario date, Zulu, local, remaining duration | **P1** — **Open** |
| **CMD-23** | Collapsible non-primary zones with persisted state | **P2** — **Open** |
| **CMD-24** | Air Operations window (readiness, loadout feasibility, tasking) | **P1** — **Partial / Phase N**, gated by doc 16 **LOG-08** |
| **CMD-25** | Boat Operations window (embarked craft, launch/recovery, tasking) | **P2** — **Phase N**, gated by doc 16 **LOG-09…11** |
| **CMD-26** | Ground Operations window, **brigade+ formations only** | **P2** — **Open (partial scope)**; battalion depth **out of charter** per doc 01 |
| **CMD-27** | Scenario library: browse, preview, pre-load feasibility, campaigns | **P0** — **Open** (Phase 1 of the doc 02 loop; specified nowhere) |
| **CMD-28** | Menu system, basemap layer stack, view tools, shortcut discovery | **P1** — **Open** (no menu bar specified; no layer model exists) |
| **CMD-29** | Contact detail panel, distinct from the own-unit panel | **P0** — **Open** (projections shipped; specification absent) |
| **CMD-30** | Tactical overlay control (range symbology, vectors, datalinks, legends) | **P1** — **Open**; distinct stack from CMD-28.2 basemap layers |


## Amendment — Drone swarm platforms (doc 22 / 2026-08-09)

> **Phase A:** reuse unit panel / Air Ops surfaces for selection + integrity readout (**SWARM-05**, **SWARM-09**). No per-drone micro-UI. **Phase B:** dedicated Swarm Ops panel fields (**SWARM-14**); CMD-24 remains shared path for air readiness chrome. Color alone must not be the only integrity channel (**CMD-12**).

## Map and Symbology

- **Partial / Shipped:** Phase A tactical map placeholder — normalized symbols from `MapPictureProjection` via `MapPlaceholderPanelHost` ([ADR-007](../../docs/architecture/adr-007-c2-map-presentation.md))
- **Partial:** Contact styling distinct from friendly; EMCON / COMMS affordances on C2 chrome (docs 15, 19)
- **Partial:** Mission areas / reference points as projection data permits
- **Open (Phase A baseline, CMD-20):** Distance scale in nautical miles + camera altitude readout. Range is the primary spatial judgement in this genre; a theater map without a scale cannot support it.
- **Open (Phase A baseline, CMD-21):** Sensor and weapon envelope rings on the selected unit, visually distinguishable. **This is not an EW overlay** — it is the baseline read that makes a selection meaningful, and must not be deferred alongside the Phase N overlay product below.
- **Phase N / Deferred:** Full WGS84 Cesium globe product (pan, zoom, rotate, theater quick-jump); APP-6 / NATO icon atlas; LOD clustering for thousands of icons
- **Phase N / Deferred:** Full EW overlay product layers

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
- **Open (CMD-17):** Where telemetry is unavailable *because the unit is out of comms*, render an explicit unknown state naming the cause — visually distinct from zero, blank, and last-known. Doc 19 models comms degradation; a blank field reads as a UI defect, while a named unknown reads as a tactical fact. Must also settle whether last-known values are shown with an age or suppressed — these are different games.
- **Open (CMD-18):** Altitude/depth and throttle commandable as **named doctrinal states** appropriate to the platform domain, not solely numeric entry. A depth command of “just under the layer” is a thermocline-relative acoustic decision whose numeric value varies with conditions; the named command carries intent the number does not, and is the correct unit of delegation to an agent. Preset sets are per-domain and need a catalog source (doc 06).
- **Open (CMD-19):** Each independently commandable axis (depth/altitude, throttle, EMCON, sensors) exposes its own manual-vs-automatic control, with current mode readable without opening a menu. **CMD-09's unit-level badge cannot express this** — a unit may be under agent speed control while the human holds depth. Implies per-axis autonomy state reaching the panel via projection (ADR-010), not view work alone.

## Platform Operations Windows

Secondary windows opened from the order toolbar (**CMD-16**). Each is a view over a doc 16 domain
model, and **none may render state its model cannot produce** — an absent column is honest, an empty
one reads as a defect, a fabricated one is a lie.

### Air Operations (CMD-24) — **Partial / Phase N**

- **Open (buildable on the doc 16 shipped spine):** cross-host aggregation of air assets across the
  selection, grouped by type and host with counts; binary `ReadyForLaunch` with the refusal naming
  the same `AIR_NOT_READY` cause the engage gate emits; magazine-constrained loadout feasibility
  (*how many airframes can I arm from current stock* — `MagazineLedger`); aviation fuel shown
  distinctly from platform fuel; assign-to-mission (doc 11); aggregate ready count in group status.
- **Phase N (gated on doc 16 LOG-08 air-ops FSM):** time-to-ready timers; launch individually vs as
  group; abort launch; Air Facilities (deck / hangar / runway capacity, deck cycle); per-airframe
  drill-down. These should arrive together — a launch action without an abort is worse than neither.

### Boat Operations (CMD-25) — **Phase N**

Gated on doc 16 **LOG-09…11** (boat FSM, sea-state gate, embarked load). Not a copy of Air
Operations: recovery limits are stricter than launch limits, sea state gates the operation, embarked
load is personnel and cargo rather than weapon stores, and craft counts are small enough that each
hull is individually named rather than type-aggregated. **Open:** embarked-craft ready count in group
status is the only piece not blocked.

### Ground Operations (CMD-26) — **Open, partial scope**

Scoped to **brigade echelon and above**, per doc 01: *"aggregate-level ground (brigade+) for
air/naval interaction"* is in scope; *"land warfare at battalion scale or below"* is **excluded**.

**Structural consequence:** if modelling caps at brigade+, a brigade is a **leaf, not a container** —
the TO&E tree has no depth to expand, because battalions are not modelled entities. Aggregate rows
without expanders are the honest shape under current charter; a deep tree would be interface for a
hierarchy the sim will never populate.

- **Open:** aggregate formation list at brigade+; air-defence contribution per formation (the
  charter's stated *purpose* for modelling ground at all, so the window's centre of gravity);
  facility status with runway/sortie linkage into doc 16; aggregate strength/readiness as a band
  rather than a roll-up of subordinates that do not exist.
- **Out of charter:** battalion and below. Requires amending doc 01 first, then doc 18, then
  introducing an echelon/TO&E model distinct from the tactical Group model (`GroupTarget`,
  `DetachRejoinService`) — organisational subordination survives detachment; tactical grouping does
  not, and conflating them would make hierarchy dissolvable.
- **Open question:** regiment is brigade-equivalent in NATO and Soviet-pattern usage, so *probably*
  in scope — but the charter says "brigade+", not "brigade-equivalent+". Resolve explicitly.

## Simulation Controls

- **Partial / Shipped:** Pause / run; time compression chrome on top bar (doc 03)
- **Phase N / Deferred:** **Multitasker mode** (§1.3): bookmark camera + selection; stack pause reasons; multi-monitor restore
- **Partial:** Mode indicator: Human / Mixed / Agent-vs-Agent (doc 03)
- **Open (CMD-22):** Top bar shows scenario date, **Zulu**, **local**, and remaining scenario duration. Zulu-vs-local is doctrinal in coalition operations and drives day/night reasoning; remaining duration frames session pacing. Cheap now, awkward to retrofit once the top bar is dense.
- **Open (CMD-23):** Message log and left drawer collapsible to a title affordance with persisted state. Over multi-hour sessions the map is the scarce resource, and the log matters intensely but intermittently.

## Menu System, Map Layers, and View Tools (CMD-28)

**Open.** REQ-20 previously specified **no menu bar**. Keyboard shortcuts appear once in the parity
table (§10.1, **P1 — Open**) and custom overlays once (§10.2, **P2 — Phase N**); nothing covers
fullscreen, zoom, measurement, a coordinate grid, a 3D toggle, or basemap layers.

- **CMD-28.1 — The menu is how shortcuts become discoverable.** Every menu item displays its binding
  inline. A shortcut nobody can find is not a feature; specifying shortcuts (§10.1) without
  specifying where they are advertised leaves them unusable. Pairs with the `?` shortcut sheet.
- **CMD-28.2 — Basemap layer stack.** Independently toggleable layers with persisted checked state
  (satellite, relief, borders/coastlines, terrain, roads/cities, land cover, placenames,
  day+night lighting, custom). **Aegis has no layer model** — ADR-007 phases map *presentation*
  but never mentions a layer stack, and `src/` has no `MapLayer` / `Basemap` type. Requires a
  decision on which layers exist, whether the set is extensible, and **who owns visibility state**.
  Layer visibility is pure presentation with no sim meaning, so it should be **UI-local** — an
  explicit exception to ADR-010's projection rule, worth stating rather than discovering.
- **CMD-28.3 — View-state actions are exempt from CLI/MCP parity; command actions are not.**
  Zoom, pan, layers, grid, fullscreen and 3D change only what *this operator sees*: no headless
  meaning, no CLI verb, no order-log row, no determinism guarantee. Anything that mutates the
  scenario or issues orders keeps parity and produces logged intents (ADR-010). Stating the line
  prevents both plumbing view toggles through the command bus and letting a mutating menu item
  bypass the order log.
- **CMD-28.4 — Spatial tools.** Range/bearing measurement with a binding — REQ-20 lists "measure"
  only as a map context-menu item (§4.2, *Partial*), but in a genre where range is the primary
  spatial judgement (same reasoning as **CMD-20**) it deserves to be a tool. Plus a Lat/Lon grid
  overlay as a coordinate reference alongside CMD-20's distance scale.
- **CMD-28.5 — Keyboard unit cycling** (next/previous). Complements CMD-07 selection sync by making
  selection reachable without pointing.
- **CMD-28.6 — Single-panel detachment is much cheaper than the multitasker.** Detaching one panel
  (e.g. the message log) to one window is a fraction of **CMD-14**'s multi-monitor bookmark product
  and delivers most of the value for a log wanted continuously visible. Split from CMD-14 rather
  than inheriting its Phase N deferral.
- **CMD-28.7 — Menu organisation.** Adopt the coverage, not the layout: the reference mixes window
  management, camera, selection, tools, panel spawning, overlays, layers and render mode in one flat
  list of nineteen items.
- **CMD-28.8 — Camera and window controls as first-class menu entries.** Zoom in/out, pan, and
  fullscreen/windowed toggle. Trivial individually, but they are the actions a new player looks for
  first, and they must exist somewhere other than an undiscoverable gesture.
- **CMD-28.9 — Primary view actions carry both a keyboard and a pointer binding.** The reference
  advertises `Zoom In — Z / Mouse Scroll` and `Zoom Out — X / Mouse Scroll`: one action, two input
  paths, both stated. This is an **accessibility requirement, not a convenience** — it guarantees a
  keyboard-only route to every camera action and directly supports CMD-12's keyboard commitment.
  Where an action has both paths, the menu shall state both.

  **Focus styling is ad hoc today:** only **3 of 27** shipped `.uss` files carry any `:focus` rule
  (`MessageLogPanel.uss` ×2 copies, `ScenarioEditorShell.uss`). Critically, the shared token file
  `AegisTokens.uss` has **none**, so there is no focus-ring token and each panel would have to
  invent its own — which is why `ScenarioMapAuthoringPanel.uss` has no focus styling at all. A
  `--focus-ring` token in `AegisTokens.uss` is the prerequisite for CMD-12 across every panel.
- **CMD-28.10 — 2D / 3D view toggle.** ADR-007 Phase B (Cesium / WGS84 globe) implies 3D capability;
  this is its control surface. **Open question:** is 2D-versus-3D a discrete mode, or a continuum of
  camera pitch on one globe? The answer changes whether this is a toggle, a camera control, or both,
  and it should be settled with ADR-007 Phase B rather than at UI time.
- **CMD-28.11 — Camera bookmarks ("quick jump").** Numbered slots storing camera position and zoom
  altitude, saved and recalled by hotkey and listed in a menu for pointer access. At theater scale
  the operator repeatedly returns to the same few areas, and re-navigating by pan and zoom each time
  is the dominant navigation cost.

  **This is also the clearest case for the disabled-with-reason rule.** In the reference product the
  bookmark menu renders greyed with no explanation, which reads as broken or unavailable; it is
  simply *empty*, because no slot has been saved yet. The correct label always existed — "no saved
  views — press Ctrl+1 to save one" — and omitting it made a working feature look like a defect.
  An empty state is not a disabled state, and the two must not share a rendering.

## Tactical Overlay Control (CMD-30)

**Open.** Distinct from **CMD-28.2** and the distinction is architectural, not cosmetic:

| Stack | Content | Ownership under ADR-010 |
|-------|---------|--------------------------|
| **Basemap layers** (CMD-28.2) | Imagery, relief, borders, land cover — *what the world looks like* | **UI-local**; no sim meaning |
| **Tactical overlays** (CMD-30) | Range envelopes, vectors, datalinks, courses — *what the sim state looks like* | **Projections** from the headless core |

Two menus in the reference, two different owners. Conflating them would either plumb basemap
toggles through the command bus or let sim-derived overlays become UI-local state that replay
cannot reproduce.

- **CMD-30.1 — Range symbology is a taxonomy, not a toggle.** Eight independent controls across
  {Air, Surface, Underwater} × {Sensors, Weapons}, plus Land Weapons and Aircraft Range, each with
  its own colour. **CMD-21** (selected-unit envelope rings) is a *subset* of this surface, not a
  synonym — CMD-21 is the Phase A baseline for one unit; CMD-30.1 is the full picture-wide taxonomy.
- **CMD-30.2 — Non-friendly range symbols are beliefs.** Showing estimated hostile envelopes is a
  major tactical affordance, but those rings are inference (**CMD-29** epistemics) and must not
  render identically to own-force envelopes, which are known.
- **CMD-30.3 — Merged range symbols are a Phase A decluttering technique, not Phase N LOD.**
  REQ-20 currently discusses decluttering only under APP-6 LOD and the 5k@60 north-star
  (**CMD-13/15**, Phase N). Merging overlapping envelopes into a hull is cheap, needs no LOD
  product, and becomes necessary long before 5,000 symbols. Same baseline-versus-Phase-N split as
  CMD-21; do not let it be deferred with the LOD work.
- **CMD-30.4 — State in text, not colour alone.** The reference writes `(Current: ON)` on every
  toggle *beside* its colour chip. Adopt this: it satisfies **CMD-12** by construction and makes the
  menu readable without relying on the swatch.
- **CMD-30.5 — Engagement geometry overlays.** Illumination and targeting vectors — who is
  illuminating or targeting whom (doc 14). Pairs with **CMD-11** intent preview; these make the
  engagement picture legible before commitment.
- **CMD-30.6 — Connectivity and emissions overlays.** Datalinks (**modelled** — 9 datalink types in
  `src/`) and contact emissions (EMCON **modelled** via `EmconState` / `CatalogEmcon` /
  `CatalogRadarEmconResolver`; emissions as a distinct concept are not). Links docs 15 and 19 to the
  map.
- **CMD-30.7 — Route and mission-area overlays.** Plotted courses and mission areas/courses.
- **CMD-30.8 — Datablocks.** Configurable text labels attached to symbols; the density knob that
  makes a dense picture readable or unreadable. **Not modelled.**
- **CMD-30.9 — Camera and analysis tools.** Track-selected-unit (camera follow), LOS tool, minimaps.
  **None modelled** — sonobuoy visibility, LOS, and minimaps have no type in `src/`.
- **CMD-30.10 — Legends for data-driven colouring.** The reference pairs its land-cover layer with a
  17-class Terrain Type Legend. **Any layer whose colour encodes a taxonomy requires a legend** —
  without one the colouring is decoration the player cannot decode.
- **CMD-30.11 — Group / Unit view toggle**, complementing CMD-07 selection sync (§4.4).

> **Colour budget — worth an explicit audit.** Between eight range-symbol colours, affiliation
> colours (**CMD-12**, which must stay colourblind-safe), seventeen land-cover classes, severity
> colours, and diff colours, the colour channel is heavily oversubscribed. Adding overlays without a
> palette audit will collide with the accessibility commitment already made in CMD-12.

## Contact Detail Panel (CMD-29)

**Open.** REQ-20's Right-panel row specifies *"Selected unit/group: status, sensors, weapons, fuel,
doctrine"* — own-unit data. Contacts appear in the left drawer and in map styling, but **selecting a
contact has no specified panel**.

**The framing:** the unit panel shows what *is*; the contact panel shows what we *believe*. Every
contact attribute is inference from sensor returns and may be wrong. Collapsing both into one
"selected object" panel silently asserts that hostile data is as trustworthy as own-force data.

This is **CMD-17's epistemics from the other side** — CMD-17 is *"I own this unit but cannot reach
it"*; CMD-29 is *"I can see this thing but do not own it."* Both concern the gap between world and
picture, and should be answered consistently.

**Unusually, the data mostly exists** — `ContactSummaryProjection`, `ContactPictureProjection`,
`ContactPictureEntry`, `BdaContactDamageStates` are shipped in
`src/ProjectAegis.Delegation/Projection/`; `ContactChangeRecord` carries contact lifecycle into the
order log (doc 17); WRA appears in the engage path. **The gap is specification, not model**, which
makes this cheaper than its P0 suggests.

- **CMD-29.1 — Identification with its confidence.** Classification specificity increases as sensor
  data accumulates; the panel must show *how well* a contact is identified rather than asserting a
  name. An overconfident label on a poorly-resolved contact is how players engage the wrong thing.
- **CMD-29.2 — Detection provenance.** Which sensor, on which platform, and when. Answers *why do I
  think this exists*, links doc 15 to CMD-11's "why can't I fire?" surface, and makes single-source
  or stale tracks visible rather than implicit.
- **CMD-29.3 — WRA classification** on the panel where the engagement decision is made (doc 13), not
  only inside an abort reason after refusal.
- **CMD-29.4 — BDA as belief, not truth.** Battle damage *assessment* is an estimate from
  observation; rendering it identically to own-unit damage asserts knowledge the player lacks.
- **CMD-29.5 — Contact report action** producing the fuller intelligence picture for the track.
- **CMD-29.6 — Staleness.** A contact not currently held is a last-known position *with an age*.
  Answer together with CMD-17's last-known-value question — the same design decision on own units
  and on hostiles.
- **CMD-29.7 — Operator override of inference.** The player shall be able to manually reclassify a
  contact — friendly, neutral, hostile — and to drop a contact from tracking, overriding the
  automatic classification.

  This is a different act from everything else on the panel. CMD-29.1–29.6 present what the *system*
  believes; this lets the operator assert what *they* believe, on knowledge the sim does not model
  (a radio call, a known deployment, an out-of-band recognition). It is the human staying in command
  of the picture rather than reading it.

  Three consequences:

  1. **An overridden classification must remain visibly an override**, not silently become fact.
     Otherwise the panel loses the distinction between what was sensed and what was asserted — which
     is the whole point of separating the contact panel from the unit panel.
  2. **It must be reversible**, and the original inferred classification must survive the override so
     it can be restored.
  3. **It is a command, not view state**, so it produces a logged intent (doc 17, `ContactChangeRecord`)
     and keeps CLI/MCP parity per CMD-28.3 — unlike the view toggles in CMD-28, this changes shared
     state and must replay.

  Overrides also feed engagement: a contact marked friendly must not then be engageable without an
  explicit confirmation path (doc 13 / CMD-11), or the override becomes a fratricide vector.

## Scenario Library and Load Operations (CMD-27)

**Open.** The first screen of the product, and specified nowhere until now: doc 02 names it
*"Phase 1: Scenario Selection & Force Composition"* — the core loop **begins** here — and
`design/ux/c2-command-post.md:45` carries it in the navigation flow
(`Main Menu → Scenario Select → Mission Planning → C2 Command Post → AAR`). REQ-20 previously had no
mention of load, main menu, or scenario select, and `design/ux/` has no main-menu spec.

### Pre-load feasibility — the part that is not CMO parity

Aegis scenarios are not just files. They are validated, catalog-bound, deterministic artifacts that
can fail to load for reasons knowable **before** the load:

- **Catalog binding** — `dbRef` / `dbSnapshotId` may not resolve (`DB_MISMATCH`); `ref:` ids may dangle (`BROKEN_REF`)
- **Validation state** — ADR-008 findings with severity; a scenario can carry blocking errors
- **Schema / version** — `scenario-document.schema.json` compatibility and `editVersion`

**CMD-27.1** The library shall state, per entry, whether the scenario resolves against the installed
catalog and passes validation, before the user commits to loading it.

This is the **load-time analogue of the export gate**: a constraint the system already enforces
should be visible before the user hits it, not as a failure afterwards.

### Sub-requirements

- **CMD-27.2 Provenance and trust** — authored / AI-scaffolded / imported
  (`ManifestBuilder.ProvenanceTag`, `Source ∈ {user, ai, import}`) and bundled / user-saved /
  third-party. **ADR-015** (Proposed, due 2026-09-01) would derive an agent-authored label from
  provenance; this library is where it becomes visible to a player.
- **CMD-27.3 Determinism metadata** — `metadata.seed`, `metadata.tlBranch`, bound policy, and
  whether a published manifest with `ReportHash` exists. Where replay parity is a release gate,
  *"will this reproduce"* is worth seeing before opening.
- **CMD-27.4 Separate artifact lifecycles** — autosaves, manual saves, and authored scenarios have
  different retention and trust and shall not share one flat list.
- **CMD-27.5 Search and filter**, not only sort — by side, region, duration, validation state, TL branch.
- **CMD-27.6 Preview pane with a real zero-state** — licence prefix + title, Difficulty and
  Complexity meters, `Location — Year`, briefing, and map preview on selection; instruction rather
  than void when nothing is selected.
- **CMD-27.7 Unavailable entries state their reason on the row**, not only after selection.
- **CMD-27.8 Difficulty and Complexity as separate rated axes** — orthogonal: difficulty is how hard
  it is to win, complexity is how much there is to manage. **Agent delegation is the mitigation for
  complexity, not for difficulty**, so this is where that value proposition first becomes legible.
  Pair the meter with a value or label — do not encode level by colour alone (CMD-12).
- **CMD-27.9 Setting metadata** — `Location` and `Year`; also the natural filter axes for CMD-27.5.
- **CMD-27.10 Briefing rendered from `scenario_export_brief`** — a shipped CLI verb. One brief, two
  surfaces; CLI/MCP parity applied to the library rather than a separately authored summary.
- **CMD-27.11 Map preview showing force disposition** — terrain, symbols, and place labels, so the
  shape of the fight reads before load. **Reuses shipped `MapPictureProjection` /
  `MapPlaceholderPanelHost`** (CMD-06, ADR-007 Phase A) over an unopened document rather than live
  sim state — reuse, not new rendering work.
- **CMD-27.12 Campaigns are an artifact class, not a folder** — an ordered chain with narrative
  progression. The library must model campaign membership, sequence, and completion state.

### Do not inherit

- **File extensions in the UI** — show scenario titles; `*.scenario.json` paths secondary.
- **Mixed hierarchy** — expandable folders and flat files as siblings at the same level.
- **Ordering encoded in filenames** — sequence and in-fiction date are **metadata**, not naming
  convention. Encoding them makes reordering a rename and breaks sorting on a missing leading zero.

**Dependency:** there is **no scenario list/browse verb or projection** — `scenario_*` has create /
validate / export / publish / simulate / undo / diff / trace, but no list. Under ADR-010 this needs a
read-only projection, and under CLI/MCP parity a headless equivalent (likely `scenario_list`).
**Not view-only work.**

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
| 7 | Unit out of comms shows named unknown state, not blank or zero; distinct from last-known | **Open** — CMD-17 |
| 8 | Every order verb reachable from the toolbar as well as the context menu | **Open** — CMD-16 |
| 9 | Selected unit shows envelope rings; map shows nm scale at every zoom | **Open** — CMD-20/21 |
| 10 | Each commandable axis shows its own manual/auto mode without opening a menu | **Open** — CMD-19 |
| 11 | Platform ops windows render no column their doc 16 model cannot populate | **Open** — CMD-24/25/26 |
| 12 | Library flags a scenario whose `dbRef` does not resolve **before** load, not as a load failure | **Open** — CMD-27.1 |
| 13 | Campaign membership, sequence and completion survive without filename encoding | **Open** — CMD-27.12 |
| 14 | A manually reclassified contact still reads as an override, is reversible, and logs an intent | **Open** — CMD-29.7 |
| 15 | An empty bookmark list states that it is empty; it is not rendered as disabled | **Open** — CMD-28.11 |

## Phased Delivery

| Phase | Scope | Honesty |
|-------|--------|---------|
| **MVP / shipped chrome** | Layout zones, OOB, contacts, unit panel, message log, top bar, delegation badges (proxy), UI Toolkit hosts | **Partial / Shipped** — PlayModeSmoke **18/18** |
| **Phase 2** | Mission drawer polish, keyboard shortcuts, overlays, doctrine/engage explain UX, **CMD-16…23** (order toolbar, comms-unknown state, per-axis autonomy, map scale + envelope rings, time disambiguation, collapsible zones) | **Partial / open** |
| **Phase 3 / N** | Product Cesium globe, APP-6 LOD, custom overlays, multitasker, Tacview hook UI, full accessibility audit, 5k@60 | **Phase N / Deferred** |

## Open Questions

| # | Question | Status |
|---|----------|--------|
| 1 | UI Toolkit vs UGUI for project? | **Resolved — UI Toolkit shipped** for C2 hosts (ADR-010 / Unity 6.3 presentation). Not reopened without ADR. |
| 2 | Single right panel vs detachable windows for multitaskers? | **Open** — product multitasker **Phase N / Deferred**; single-panel layout is current shipped chrome |
| 3 | 3D globe vs 2.5D map for performance? | **Partially resolved** — ADR-007: Phase A placeholder **Shipped**; product WGS84 globe via Cesium **Partial / Phase N**; not a blocking open for C2 chrome |
| 4 | Do CMD-17 and CMD-19 require an ADR-010 contract change? | **Open** — both need state the current projections may not carry (comms staleness; per-axis autonomy). Confirm before estimating; this is not view-only work |
| 5 | Is regiment brigade-equivalent for CMD-26 scope? | **Open** — see doc 01 line 82 |
| 6 | Time compression: multiplier or time-per-step? | **Open** — CMD-04/CMD-22; current chrome uses multipliers, CMO offers both; different mental models |

## Traceability

| Doc | Relationship |
|-----|----------------|
| [01](01-Project-Overview.md) | Hub **FR-18**; **OV-SC-N1** scale deferral; accessibility NFRs |
| [02](02-Core-Gameplay-Loop.md)–[04](04-Agent-Delegation.md) | Loop, modes, delegation overlays |
| [11](11-Agentic-Mission-Editor.md) | Editor / Mission Board entry |
| [13](13-Doctrine-ROE-EMCON-WRA.md)–[17](17-Replay-AAR-And-Order-Log.md) | Panel content, engage explain, message log |
| [16](16-Logistics-And-Magazines.md) | **Gating model for CMD-24/25** — air-ops FSM (LOG-08), boat ops (LOG-09…11), magazine feasibility, readiness |
| [18](18-Combat-Domains.md) | Land combat scope for CMD-26 (`LandAspectDomainValidator`; battalion tactics excluded) |
| [19](19-Cyber-And-Comms.md) | COMMS legend / degrade affordances on C2 chrome; **CMD-17** unknown-due-to-comms |
| [01](01-Project-Overview.md) | **Charter scope for CMD-26** — brigade+ in scope (line 82); battalion and below excluded (line 90) |
| [02](02-Core-Gameplay-Loop.md) | **Phase 1 Scenario Selection** is the entry point CMD-27 specifies |
| ADR-008 / ADR-015 | Validation findings and provenance surfaced pre-load (CMD-27.1 / CMD-27.2) |
| [ADR-010](../../docs/architecture/adr-010-headless-first-command-driven-ui.md) | **Normative** headless-first command-driven UI |
| [ADR-007](../../docs/architecture/adr-007-c2-map-presentation.md) | Map presentation phases |
| `cmo-manual-traceability.md` | Ch 3–4, §6 |
| GDD / UX | `design/gdd/command-and-control-ui.md`, `design/ux/c2-command-post.md` |

---

**References:** CMO Manual Ch 3–4; `docs/military-simulation/genre-conventions-reference.md`; `docs/manual/index.html`; [ADR-010](../../docs/architecture/adr-010-headless-first-command-driven-ui.md); [ADR-007](../../docs/architecture/adr-007-c2-map-presentation.md)

**Implementation grade:** Partial — see [implementation-tracker.md](../implementation-tracker.md) row 20. Design Status remains **Draft** (Template B). Charter re-honesty: Wave 2 2026-07-08.
