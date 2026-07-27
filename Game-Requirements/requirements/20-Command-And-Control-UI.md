# 20 - Command and Control User Interface

**Last Updated:** 2026-07-26 (CMD-16…CMD-26 landed from CMO reference-product analysis)  
**Status:** Draft — Template B (Wave 2 re-honesty)  
**CMO basis:** Manual Ch 3–4, §6.2–7, §6.9, §1.3 multitaskers, §10.1 keyboard  
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
| Persistent unit-order toolbar | §3.3 | **P1** | **Open** — CMD-16; only context menus specified previously |
| Aircraft Operations window | §3.3.7 | **P1** | **Partial / Phase N** — CMD-24; gated by doc 16 LOG-08 |
| Boat Operations window | §3.3.8 | **P2** | **Phase N** — CMD-25; gated by doc 16 LOG-09…11 |
| Ground formations (brigade+) | §9.2.5 | **P2** | **Open (partial scope)** — CMD-26; battalion depth excluded by doc 01 |

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
| [ADR-010](../../docs/architecture/adr-010-headless-first-command-driven-ui.md) | **Normative** headless-first command-driven UI |
| [ADR-007](../../docs/architecture/adr-007-c2-map-presentation.md) | Map presentation phases |
| `cmo-manual-traceability.md` | Ch 3–4, §6 |
| GDD / UX | `design/gdd/command-and-control-ui.md`, `design/ux/c2-command-post.md` |

---

**References:** CMO Manual Ch 3–4; `docs/military-simulation/genre-conventions-reference.md`; `docs/manual/index.html`; [ADR-010](../../docs/architecture/adr-010-headless-first-command-driven-ui.md); [ADR-007](../../docs/architecture/adr-007-c2-map-presentation.md)

**Implementation grade:** Partial — see [implementation-tracker-2026-07-04.md](../implementation-tracker-2026-07-04.md) row 20. Design Status remains **Draft** (Template B). Charter re-honesty: Wave 2 2026-07-08.
