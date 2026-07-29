# 20 - Command and Control User Interface

**Last Updated:** 2026-07-28  
**Status:** Draft — Template B (Wave 3: command lifecycle, contention, degraded states, testable ACs)  
**CMO basis:** Manual Ch 3–4, §6.2–7, §6.9, §1.3 multitaskers, §10.1 keyboard; map/layer/view instructional parity (clean-room)  
**Related:** [01](01-Project-Overview.md), [02](02-Core-Gameplay-Loop.md), [03](03-Simulation-Modes.md), [04](04-Agent-Delegation.md), [11](11-Agentic-Mission-Editor.md), [12](12-Terms-Glossary.md), [13](13-Doctrine-ROE-EMCON-WRA.md)–[17](17-Replay-AAR-And-Order-Log.md), [19](19-Cyber-And-Comms.md) · [implementation tracker 2026-07-04](../implementation-tracker-2026-07-04.md)  
**Architecture (normative):** [ADR-010 Headless-First / Command-Driven UI](../../docs/architecture/adr-010-headless-first-command-driven-ui.md) · [ADR-007 C2 Map Presentation](../../docs/architecture/adr-007-c2-map-presentation.md)

## Purpose

Define the **theater command UI**: map, symbology, panels, context menus, delegation overlays, and **information density** standards for a CMANO-scale wargame with agentic control.

Implements hub **FR-18** ([01](01-Project-Overview.md)).

> **Normative architecture.** [ADR-010](../../docs/architecture/adr-010-headless-first-command-driven-ui.md) is **normative** for this document: Unity presentation is a **command-driven client** over headless .NET core (`ProjectAegis.Data` / `Sim` / `Delegation`). UI binds **read-only projections** and submits **validated commands** only — never authoritative sim state. Map presentation phasing is governed by [ADR-007](../../docs/architecture/adr-007-c2-map-presentation.md) (Phase A placeholder → Phase B Cesium / WGS84 → Phase C APP-6 LOD).

## Conventions (normative for this document)

### Status vocabulary

Prior waves used these labels inconsistently (`Partial+`, `P0 intent` were undefined). Fixed meanings:

| Label | Meaning |
|-------|---------|
| **Shipped** | Built, exercised by a named test, and listed in Implementation Mapping with evidence |
| **Partial / Shipped** | Host or contract exists and is smoke-covered; product behaviour incomplete |
| **Partial** | Some mechanism exists; not smoke-covered end to end, or covers a subset of the stated requirement |
| **Open** | Specified here, not started, not blocked by another phase |
| **Phase N / Deferred** | Deliberately out of the current product gate; requires a phase decision to start |
| **P0 intent** | Product intent is P0, but the *simulation* capability it depends on is not modelled yet (see [`docs/engineering/sim-capability-gap-backlog.md`](../../docs/engineering/sim-capability-gap-backlog.md)) — a UI requirement here cannot be satisfied before that gap closes |

A requirement marked **P0** with maturity **Phase N** is a scheduling risk, not an achievement. The KEY-\* block is currently entirely in that state; see [Risk Register](#risk-register).

### ID ownership (resolves cross-namespace duplication)

Several capabilities were previously specified twice under different IDs. Each capability now has exactly one **owning** ID; the others are cross-references only and must not carry independent acceptance criteria.

| Capability | Owner | Cross-references (non-normative) |
|---|---|---|
| Attack/engage hotkey | **KEY-03** | CUI-16 |
| Move/plot hotkey | **KEY-04** | CUI-16, CUI-06 |
| Range/bearing measure | **MAP-23** | KEY-10 |
| Sensor/weapon range rings | **MAP-13** | CUI-17 |
| Aircraft fuel-range overlay | **MAP-14** | CUI-17 |
| Reference-point placement | **MAP-21** | KEY-11 |

**Rule:** a capability's status is authoritative at its owning ID. When statuses disagree, the owner wins and the cross-reference is stale.

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
| **CMD-03** | Headless-first command-driven binding per ADR-010 (projections in; commands out) | **Partial** — *inbound* projection half **Shipped**; *outbound* command half has *no result channel* (see **CMD-16**) |
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

### Command lifecycle and failure (CMD-16 – CMD-22)

**Gap this closes.** ADR-010 requires the UI to "submit user intent as commands", but specifies nothing about what comes *back*. Verified against shipped code (2026-07-28): `C2PresentationController`'s public surface is selection-only, and the **player-order** path terminates in `DecisionLog.AppendPlayerOrder(...)` → `void`; `PlayerOrderExecutionQueue.Enqueue` likewise returns `void`. The player-order submission path is fire-and-forget, so the UI cannot tell an operator whether an order was accepted, or why one was refused — even though the sim models **23 distinct engagement abort reasons** (`EngagementAbortReason`), already exposed through `EngagePreviewProjection` and `MessageLogProjection`.

**The shape already exists and should be reused, not invented.** The *agent* order path is already gated by `AutonomyGate.Evaluate(...)`, which returns `GateResult(ExecuteNow, QueueForApproval, Rejected, PolicyDenialReason)` — precisely the `Accepted` / `Queued` / `Rejected`-with-reason triple **CMD-16** requires. The defect is that the human command path does not return it. This narrows the work from "design a result protocol" to "return the existing one".

For a ROE-gated wargame, *"why can't I fire?"* is the central operator question. The data and the result type both exist; the return path to the operator does not.

| ID | Requirement | Priority / maturity |
|----|-------------|---------------------|
| **CMD-16** | **Command result channel.** Every authoritative UI command returns a typed outcome — `Accepted` (with the assigned `sequenceId`), `Rejected` (with a machine code + operator-readable reason), or `Queued` (with the tick it will execute on). Fire-and-forget submission is not acceptable for any command that can fail. | **P0** — **Open** |
| **CMD-17** | **Rejection is explained, never silent.** A rejected command surfaces its reason at the point of interaction (context menu, panel, or map), not only in the message log. Reason text is drawn from the shared abort/deny vocabulary so UI and log agree verbatim. | **P0** — **Open** |
| **CMD-18** | **No raw enum leakage.** All 23 `EngagementAbortReason` members — and every deny code added later — have an operator-facing string. A missing mapping is a build-time failure, not a runtime fallback to the symbol name. | **P0** — **Open** |
| **CMD-19** | **Queued-order visibility and cancel.** Orders awaiting a future tick are listed, attributed to their unit, and cancellable before execution. Cancelling is itself a logged command (doc 17). | **P0** — **Open** |
| **CMD-20** | **Confirmation for irreversible actions.** Weapons release, mission deletion, and delegation handover require explicit confirmation, with a per-action "don't ask again" that persists per side and is disclosed in settings. Confirmation must never be the *only* guard against a rejected command (that is **CMD-16**'s job). | **P0** — **Open** |
| **CMD-21** | **Commanding under time compression.** Issuing a command at compression > 1× either (a) auto-pauses to a decision point, or (b) targets an explicit future tick — chosen by the operator, never silently. The doc-03 compression state at submission is recorded on the order. | **P0** — **Open** |
| **CMD-22** | **Multi-unit application semantics.** A command issued against a multi-unit selection reports per-unit outcomes; partial success is rendered as partial, never as blanket success or blanket failure. | **P1** — **Open** |

### Authority and contention (AUT-*)

**Gap this closes — corrected 2026-07-28.** An earlier draft of this section asserted that no human-vs-agent conflict rule existed. **That was wrong**, and the error came from grepping only `Delegation/Decision/`. The rule is locked in [doc 04 §2](04-Agent-Delegation.md#2-conflicting-orders-on-group-override) — **detach-and-rejoin**, *"exactly one active controller per target — order conflicts are structurally impossible"* — and it is **shipped**: `OverrideService.TakeDirectControl` swaps the controller slot agent→human, and `DetachRejoinService` handles the group case, emitting `GroupMemberDetach` / `GroupMemberRejoin` / `ControllerChange`.

The real gap is narrower and is a **UI** gap: the semantics are decided and implemented headlessly, but the operator-facing surface that *communicates* them is UI debt (doc 04 Implementation Mapping: "full drag-drop assignment + polish badges remain UI debt"). AUT-\* therefore specifies how the shipped rule is **surfaced**, not what the rule should be.

| ID | Requirement | Priority / maturity |
|----|-------------|---------------------|
| **AUT-01** | **The locked contention rule is legible in the UI.** Overriding an agent-controlled unit shows, before commit, that control transfers to the operator (and, for a group member, that the unit **detaches** and the group replans next cycle). No silent transfer. Surfaces the doc 04 §2 rule; does not redefine it. | **P0** — **Open** (rule shipped; UI absent) |
| **AUT-02** | **Takeover is an explicit, logged, acknowledged act.** Assuming manual control is a command with a result (**CMD-16**) and produces an order-log entry naming prior and new controller — mirroring the `ControllerChange` event already emitted headlessly. | **P0** — **Open** (event shipped; UI ack absent) |
| **AUT-03** | **Badge reflects contention state**, not just ownership: `human`, `agent`, `mixed`, **`contested`** (both issued orders this tick), **`stale`** (agent paused with orders outstanding). | **P0** — **Open** |
| **AUT-04** | **Agent action while a menu is open** does not silently invalidate the operator's pending choice; the affordance updates or disables with a reason rather than executing against changed state. | **P0** — **Open** |
| **AUT-05** | **Autonomy changes are reversible and visible** — raising or lowering a unit's autonomy shows what it changes *before* commit (which orders the agent may then issue unprompted). | **P1** — **Open** |
| **AUT-06** | **Side-level agent commander surfaces its intent** before acting, at Assisted autonomy, consistent with the unit-level ghost intent (**CMD-11**). | **P1** — **Phase N** |

### Degraded and edge states (DEG-*)

**Gap this closes.** The document specifies the happy path. A command post is judged on contested and degraded states — and doc 19 already models comms degradation without stating what the UI does about it.

| ID | Requirement | Priority / maturity |
|----|-------------|---------------------|
| **DEG-01** | **Stale projection is visibly stale.** When the UI is rendering data older than a stated threshold, affected panels are marked; they must not present stale values as current. | **P0** — **Open** |
| **DEG-02** | **Comms degradation changes displayed truth, honestly** (doc 19). Contact data degraded by comms loss is rendered as *last known*, with its age — never as live. | **P0** — **Open** |
| **DEG-03** | **Selected unit destroyed** transitions the panel to a terminal state naming the cause and time; it does not blank, and does not silently reselect another unit. | **P0** — **Open** |
| **DEG-04** | **Commands against dead or departed units** are refused with a specific reason (**CMD-17**), distinct from an ROE refusal. | **P0** — **Open** |
| **DEG-05** | **Scenario end** freezes the command surface into a read-only state with AAR entry (doc 17); late commands are refused, not queued into a finished sim. | **P0** — **Open** |
| **DEG-06** | **Projection/host failure degrades locally.** One failed panel does not take down the command post; the failed zone reports its own failure. | **P1** — **Open** |
| **DEG-07** | **Empty states are explicit** — "no contacts" reads as *no contacts*, distinguishable from *not loaded* and from *sensors down*. | **P1** — **Open** |

### Operator attention and alerting (ALR-*)

**Gap this closes.** At theater scale with agents acting autonomously, what interrupts the operator is a first-order design concern, and is currently unspecified.

| ID | Requirement | Priority / maturity |
|----|-------------|---------------------|
| **ALR-01** | **Alert priority classes** are defined (e.g. weapons-release-imminent > new hostile contact > mission complete > informational) and drive ordering and persistence. | **P0** — **Open** |
| **ALR-02** | **Nothing blocks the map.** Alerts never occlude the primary map or steal focus mid-interaction; the operator dismisses on their own schedule. | **P0** — **Open** |
| **ALR-03** | **Every alert is navigable** — selecting it selects the subject unit/contact and opens the relevant explain surface (parity with the message-log deep-link, **CMD-05**). | **P0** — **Open** |
| **ALR-04** | **Rate limiting and coalescing.** Repeated same-class alerts coalesce with a count rather than flooding; the log retains every instance (doc 17). | **P1** — **Open** |
| **ALR-05** | **Agent-initiated actions are attributable** in the alert stream — the operator can always answer "did I do that, or did the agent?" | **P0** — **Open** |

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

## Accessibility and Density (ACC-*)

Per genre conventions (`docs/military-simulation/genre-conventions-reference.md`) and hub NFRs. The previous wave stated these as adjectives ("colorblind-safe", "font scaling") with no pass condition, so none were testable. Restated with thresholds:

| ID | Requirement | Priority / maturity |
|----|-------------|---------------------|
| **ACC-01** | **Affiliation is never encoded by colour alone** — shape or fill pattern also distinguishes friendly / hostile / neutral / unknown, verifiable with colour stripped. | **P0** — **Partial** (palette exists; shape redundancy unverified) |
| **ACC-02** | **Contrast:** text and meaningful UI glyphs meet **WCAG 2.2 AA** (4.5:1 body, 3:1 large text and graphical objects) against their own background, including on the map. | **P0** — **Open** |
| **ACC-03** | **Font scaling to 200%** without loss of function or clipping in panels and message log. | **P0** — **Partial** |
| **ACC-04** | **Every command reachable by keyboard**, with a visible focus indicator meeting **ACC-02** contrast. Mouse-only paths are defects. | **P0** — **Partial** (OOB + log focus order only) |
| **ACC-05** | **Pointer targets ≥ 24×24 px** for controls in chrome (map symbols exempt; see **ACC-06**). | **P1** — **Open** |
| **ACC-06** | **Map symbol selection tolerance** is independent of icon size, so dense theaters stay selectable at low zoom. | **P0** — **Open** |
| **ACC-07** | **Screen-reader labels** for critical controls and alerts. | **Phase N** — out of scope for v1 product gate (hub) |
| **ACC-08** | **Minimum usable resolution 1920×1080**; no single-screen 4K requirement. | **P0** — **Partial** |

### Latency budgets by interaction class

The prior wave specified one budget (panel update < 100 ms). Interaction classes have materially different tolerances; a command post that acknowledges a weapons order as slowly as it redraws a list is unusable.

| Class | Budget (p95, target hardware) | Maturity |
|-------|-------------------------------|----------|
| Selection → panel bind | **< 100 ms** | **Partial** — projection bind timing tested |
| Command submit → **acknowledgement** (**CMD-16**) | **< 150 ms** | **Open** — no result channel exists |
| Command submit → order-log entry visible | **< 250 ms** | **Open** |
| Context menu open (populated, ROE-evaluated) | **< 150 ms** | **Open** |
| Map pan/zoom frame time | **≥ 30 FPS** interactive at current scale | **Partial** |
| Alert raised → visible (**ALR-01**) | **< 500 ms** | **Open** |

Budgets are **p95, measured under the scenario scale in force at the time** — not the deferred 5k-symbol north-star (**CMD-15**, hub **OV-SC-N1**).

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

The prior wave carried **6 criteria against 76+ requirement IDs**, and most requirements were stated as capability names ("Left-click selection", "Throttle presets") with no pass condition — untestable as written, and in conflict with the project's own coding standard that acceptance criteria be *testable success conditions*. Criteria below are stated so that each can be mechanically checked; **AC-07 onward are new**.

Where a criterion depends on an unbuilt mechanism, the evidence column says so rather than implying coverage.

| # | Criterion | Evidence policy |
|---|-----------|-----------------|
| 1 | Select unit → panel shows effective doctrine and magazine % within the selection budget (< 100 ms p95) | Smoke + projection tests; full frame budget **Partial** |
| 2 | Delegate unit to agent → badge visible; pause agent stops intents | **Partial** — delegation hosts + doc 04 paths |
| 3 | Assisted mode shows ghost intent before engage; deny shows FireAbort tooltip | **Partial** — engage preview projections |
| 4 | 5000 symbols on map with LOD: pan stays above 30 FPS on target hardware | **Deferred** — **OV-SC-N1**; not CI gate |
| 5 | Message log click selects unit and opens explain for referenced `sequenceId` | **Partial / Shipped** path in smoke proxies |
| 6 | Core §4.1 actions available without hidden modals-only paths | **Partial** — command-driven hosts |
| **7** | **Every** authoritative command returns `Accepted` / `Rejected` / `Queued`; a submission that returns nothing fails the test (**CMD-16**) | **Open** — headless contract test over the command surface; no Unity needed |
| **8** | A command refused by ROE surfaces its reason **at the interaction point**, and that string is byte-identical to the order-log entry for the same event (**CMD-17**) | **Open** — headless: assert UI reason string == log reason string |
| **9** | Enumerating all `EngagementAbortReason` members yields an operator-facing string for each; a missing mapping fails the build (**CMD-18**) | **Open** — exhaustive enum test, same pattern as the existing abort-reason manifest gate |
| **10** | A queued order is listed before execution, and cancelling it prevents execution and appends a cancellation entry (**CMD-19**) | **Open** — headless queue test at a fixed tick |
| **11** | Human order to an agent-controlled unit resolves per the stated **AUT-01** rule, with the outcome logged and attributable | **Open** — deterministic two-controller test, fixed seed |
| **12** | With comms degraded, contact panels render *last known + age*; no degraded value is presented as live (**DEG-02**) | **Open** — projection test with degraded comms state (doc 19) |
| **13** | Destroying the selected unit leaves the panel in a terminal state naming cause and time; no blanking, no silent reselect (**DEG-03**) | **Open** — smoke: kill selected unit mid-run |
| **14** | Commands submitted after scenario end are refused with an end-of-scenario reason, not queued (**DEG-05**) | **Open** — headless |
| **15** | Affiliation remains distinguishable with colour removed (**ACC-01**) | **Open** — render symbols to greyscale, assert shape/pattern distinctness |
| **16** | Every command in the context menu is reachable by keyboard with a visible focus indicator (**ACC-04**) | **Open** — focus-traversal test over the menu model |
| **17** | Text and meaningful glyphs meet WCAG 2.2 AA contrast, including over the map (**ACC-02**) | **Open** — computed contrast over the palette + map background samples |
| **18** | Alerts never occlude the map or steal focus; each alert navigates to its subject (**ALR-02**, **ALR-03**) | **Open** — layout assertion + navigation test |
| **19** | Every alert and order-log entry is attributable to human or agent (**ALR-05**) | **Open** — headless over a mixed-autonomy run |
| **20** | Issuing a command at compression > 1× either auto-pauses or targets an explicit future tick, and records the compression state (**CMD-21**) | **Open** — headless at 1×/4×/60× |

**Coverage note.** AC 7–20 are deliberately weighted toward the *command return path, contention, and degraded states* — the three areas the prior wave did not specify at all. Most are checkable headlessly, consistent with ADR-010's requirement that authoritative actions be drivable without the Unity Editor.

## Non-Goals (v1)

Stated so that "not present" is not repeatedly re-litigated as "missing". These are distinct from **Phase N / Deferred**, which *are* intended, later.

| Not doing | Why |
|-----------|-----|
| Reproducing CMO's exact key bindings or menu wording | Clean-room posture (see CMO parity note); Aegis specifies **capability classes**, not proprietary key ownership (**KEY-01**) |
| Embedding named third-party basemaps (Sentinel-2, Stamen, BMNG) as a shipping dependency | MAP-\* brand names denote capability classes; licensing and offline determinism are unresolved |
| Touch / mobile / controller input | Command post targets keyboard + mouse at desk scale (**ACC-08**) |
| In-UI scenario authoring beyond mission activate/deactivate | Owned by doc 11 / ADR-017 editor topology |
| Real-time multiplayer command deconfliction | Single-operator + agents; no concurrent human operators in v1 |
| Localization of operator-facing strings | English-only v1; **CMD-18** requires the string *table* to exist, which makes localization tractable later |

## Risk Register

| # | Risk | Evidence | Consequence if unaddressed |
|---|------|----------|----------------------------|
| **R1** | **The entire KEY-\* block is P0 at maturity Phase N** — 12 requirements, none started | This document | A P0 surface with no started work and no acceptance criteria will surface late as scope, not as polish |
| **R2** | **No command result channel** (**CMD-16**) | `AppendPlayerOrder` → `void`; `PlayerOrderExecutionQueue.Enqueue` → `void`; `C2PresentationController` exposes selection only | The operator cannot distinguish "order accepted" from "order silently dropped" — the failure mode is invisible, and every explain requirement (CMD-11, CMD-17) is unbuildable until it exists |
| **R3** | **Contention semantics are shipped but invisible** (**AUT-01/02**) — *corrected: an earlier draft wrongly claimed no rule existed* | Rule locked in doc 04 §2 and implemented (`OverrideService`, `DetachRejoinService`, `ControllerChange`); doc 04 lists the C2 badge/drag-drop surface as **Partial / Phase N** UI debt | The operator cannot see who holds a unit, or that overriding a group member detaches it — correct behaviour that reads as a bug because nothing communicates it |
| **R4** | **`P0 intent` requirements depend on unmodelled sim capability** — MAP-03 relief/LOS, MAP-06 land cover, MAP-19 LOS tool | [`sim-capability-gap-backlog.md`](../../docs/engineering/sim-capability-gap-backlog.md) (terrain, weather absent) | UI work would build affordances over data the simulation cannot supply — the vocabulary-only defect class this project has hit repeatedly |
| **R5** | **Acceptance criteria still trail requirement count** — this wave raised criteria 6 → 20, but also raised IDs 76 → **109** (CMD 22, CUI 17, KEY 12, SPA 8, MAP 24, AUT 6, DEG 7, ALR 5, ACC 8), so coverage went from ~8% to ~18% and most CUI/KEY/SPA/MAP entries remain capability names with no pass condition | This document | Requirements without pass conditions get marked done by assertion. Next wave should add criteria for the CUI/MAP blocks rather than more requirements |

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

**Wave 3 (2026-07-28) — review, corrections and additions.**

*Corrected:*
- **CMD-03** was labelled **Shipped (contract)**. Verified against shipped code: the inbound projection half is real, the outbound command half has no result channel. Re-labelled **Partial** with the gap named. This is the one status in the document that was overstated.
- Status vocabulary had undefined labels (`Partial+`, `P0 intent`) — now defined in [Conventions](#conventions-normative-for-this-document).
- Six capabilities were specified twice under different IDs (CUI-16 ↔ KEY-03/04, CUI-06 ↔ KEY-04, MAP-23 ↔ KEY-10, CUI-17 ↔ MAP-13/14, MAP-21 ↔ KEY-11). Ownership assigned; cross-references made non-normative.

*Added:* **CMD-16 – CMD-22** (command lifecycle and failure), **AUT-\*** (authority and contention), **DEG-\*** (degraded and edge states), **ALR-\*** (operator attention), **ACC-\*** (accessibility restated with thresholds), per-interaction latency budgets, AC 7–20, [Non-Goals](#non-goals-v1), [Risk Register](#risk-register).

*Method:* claims about existing behaviour were checked against `src/` at `main` rather than inferred from prior documentation. All added requirements are marked **Open** — none assert work that has not been done.
