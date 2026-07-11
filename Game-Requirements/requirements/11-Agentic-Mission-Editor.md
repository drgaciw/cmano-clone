# 11 - Agentic Mission & Scenario Editor

**Last Updated:** 2026-07-09
**Status:** Revised — implementation-aligned (was Draft; realigned to approved GDD + shipped headless stack)
**FR reverse-ref:** [FR-09](01-Project-Overview.md) — Scenario/mission editor
**Author basis:** Historical approved GDD [`design/gdd/agentic-mission-editor.md`](../../design/gdd/agentic-mission-editor.md) (original terminology, determinism contract, AC-1…AC-12); current codebase review of `ProjectAegis.Data/Scenario/Authoring` + `ProjectAegis.Data/Validation` + `ProjectAegis.MissionEditor.Cli`; [scenario-editor research](../../docs/research/scenario-editor-research.md); [CMO Official Manual](https://www.matrixgames.com/amazon/PDF/CMO/CMO_manual_EBOOK.pdf) (Mission Editor §3.3.17/§7.1, Scenario Editor §4.1.5, ScenEdit §5, clean-room observable behavior only); requirements 01–10, 13, 14, 17.
**Related:** [06-Database-Intelligence.md](06-Database-Intelligence.md) · [21-Platform-Editor.md](21-Platform-Editor.md) · [04-Agent-Delegation.md](04-Agent-Delegation.md) · [07-Agentic-Infrastructure.md](07-Agentic-Infrastructure.md) · [08-Agentic-Architecture.md](08-Agentic-Architecture.md) · [13-Doctrine-ROE-EMCON-WRA.md](13-Doctrine-ROE-EMCON-WRA.md) · [17-Replay-AAR-And-Order-Log.md](17-Replay-AAR-And-Order-Log.md)
**Decision record:** [ADR-008 Mission-Editor Validation Engine (Accepted)](../../docs/architecture/adr-008-mission-editor-validation-engine.md) · [ADR-013 CMO Scenario Import Policy (Proposed)](../../docs/architecture/adr-013-cmo-scenario-import-policy.md) · [ADR-014 Lua Compatibility Scope (Accepted)](../../docs/architecture/adr-014-lua-compatibility-scope.md) · [ADR-015 Agent-Authored Scenario Transparency (Proposed)](../../docs/architecture/adr-015-agent-authored-scenario-transparency.md) · [ADR-016 Event-Graph Complexity Caps (Accepted)](../../docs/architecture/adr-016-event-graph-complexity-caps.md) · [ADR-017 Editor Topology: Client vs Scenario Lab (Proposed)](../../docs/architecture/adr-017-editor-topology-client-vs-scenario-lab.md)

## Purpose

Implements hub **[FR-09](01-Project-Overview.md)** (Scenario/mission editor).

Define requirements for an **agentic mission and scenario editor** that preserves the depth and designer power of Command: Modern Operations (CMO) while delivering a uniquely improved authoring experience built on an **intent-compiler spine**: a single canonical declarative scenario file is the source of truth, and every authoring path (map drawing, natural language, MCP tool calls) emits the *same* canonical objects. Determinism, git-diffability, deterministic validation, headless generation, and AI co-authoring are therefore structural properties of the file format, not features layered on top.

## Vision

Scenario design should feel like **theater planning with an expert staff**, not fighting a modal-heavy desktop tool. Human designers retain full authority. The differentiator vs every editor before it: **the tool never lets you ship something broken silently** — the moment a strike can't reach its target on fuel, a patrol zone is empty, or a ferry has no destination, the editor tells you *what* is wrong, *where*, and *how to fix it*, in plain language, before you press play. Every action is machine-readable, replayable, and MCP-addressable so Claude/Cursor can co-author alongside humans.

> **Terminology (locked — GDD §1, ADR-008).** **"Engine"** = deterministic rule code (the **Validation Engine** is pure and reproducible; **no LLM in any blocking path**). **"Agent"** is reserved exclusively for the **Phase 2/3 LLM-driven advisory systems** (Mission Planner, Red Force, Briefing Writer, Balance, Migration) that *propose* diffs and **never** sit in a blocking path. v1 contains no LLM in any gate — this protects the determinism pillar.

> **v1 reality (honesty note).** v1 is **headless / file-based**: a canonical scenario file, a CLI/MCP tool surface, four mission archetypes, a deterministic Validation Engine, and headless sampling. There is **no Unity Edit Mode map GUI and no sides/faction placement UI in v1**. **Phase 2 Partial+ (P2.1)** later shipped **headless** map ORBAT/reference-point mutations (API + CLI + tests + headless `UnityAdapter.Authoring` surfaces) — see AME-4.2/4.3. Full map-first Edit Mode `EditorWindow` host remains residual (ME-W0). Aspirational pillars below are retained as advisory/phased scope, not full-GUI claims.

## Scope (Locked Decisions — see ADRs)

| Decision | Choice | ADR |
|----------|--------|-----|
| **Source of truth** | Single canonical declarative scenario file; all front-ends emit identical objects; `editorState` is derived-only and never read by sim/validation | GDD §3.2/§3.3 |
| **Validation** | Deterministic **Validation Engine** (pure rule code, no LLM); sole export gate; same file → same findings; blocks export on error-severity findings | ADR-008 |
| **Determinism** | Same file + seed → byte-identical `fire_order` + identical world-state hash; `scenario_simulate_sample` runs in isolated sim-world per call | ADR-008 / GDD §4.2 |
| **Concurrency** | Optimistic `metadata.editVersion` (monotonic int, distinct from `schemaVersion`); mismatch → **conflict-reject** (`CONFLICT`), never last-write-wins | ADR-008 / GDD §3.7 |
| **CMO import** | Proposed policy — best-effort, legally/technically gated; advisory Migration **Agent** only, never a blocking path | ADR-013 |
| **Lua scope** | **Typed event DSL** is the v1 authoring surface; **no Lua in v1**; optional compatibility shim deferred | ADR-014 |
| **Agent-authored labeling** | Proposed — label agent-authored scenarios in multiplayer/briefing for transparency; store in `metadata`/provenance | ADR-015 |
| **Event-graph caps** | Soft/hard caps accepted: soft complexity + tick-density **warnings**; hard cap 32 conditions/event | ADR-016 |
| **Editor topology** | Proposed — in-client editor vs standalone "Scenario Lab" sharing the core library | ADR-017 |

**Out of scope (v1):** Unity Edit Mode map GUI; sides/factions placement UI; operations-timeline UI; mining/mine-clear/cargo missions; NL Mission Planner; CMO import execution; Lua; Steam-Workshop sharing. *(Headless map ORBAT/RP mutations shipped under Phase 2 Partial+ — AME-4.2/4.3; not a v1 claim.)* Remaining Phase 2/3 product UI and agents stay phased.

## Status Vocabulary

- **P0 / P1 / P2:** delivery priority: v1 blocking, Phase 2, and Phase 3/advisory respectively. Priority does not imply implementation status.
- **Shipped:** implemented on the canonical headless path with cited automated evidence.
- **Partial+:** a useful, tested subset is shipped, but named product/UI or breadth requirements remain.
- **Residual:** an unshipped portion of an otherwise Partial+ requirement; it must have an explicit completion gate below.
- **Deferred:** intentionally outside the completed phase and not claimed as shipped.

## CMO Baseline — What We Must Match or Exceed

Parity targets from the CMO manual and observable community tooling (clean-room). No regression on items marked **P0**. Priority is now carried as a marker, not buried in prose.

### Scenario lifecycle (CMO Scenario Editor §4.1.5)

| Capability | CMO behavior | Aegis requirement | Priority |
|------------|--------------|-------------------|----------|
| Create blank scenario | Edit mode, empty theater | `scenario_create` with theater, start time, duration, seed | P0 |
| Load / save scenario | `.scen` + side briefing, DB version match | Versioned package with embedded DB snapshot reference (`dbRef`) | P0 |
| Scenario features & settings | Locked in play; editable in editor | Feature flags as typed config; diffable | P0 |
| Side selection & briefing | Per-side briefing text, side picker | Structured briefing (objectives, ROE summary, intel) | P1 |
| Database binding | Scenario tied to DB version; rebuild shallow/deep | Integrate with Database Intelligence Layer (doc 06); `dbRef`/`tlBranch` binding | P0 |

### Mission system (CMO Mission Editor §7.1)

| Mission type | CMO purpose | Aegis notes | Priority |
|--------------|-------------|-------------|----------|
| **Strike** | Attack assigned targets; TOT/TOS; weapon behavior | Flight-plan generation, package assignment (v1: fuel-reachability check) | P0 |
| **Patrol** | Patrol / prosecution areas; ASW/ASuW/AAW/SEAD/CAS | Separate patrol vs prosecution geometry | P0 |
| **Support** | Tanker, AEW, EW jamming | Role-appropriate platform; station geometry | P0 |
| **Ferry** | Redeploy aircraft between bases | Destination validity + reachability (**shipped** — domain + `mission_add_ferry` / `mission_update_ferry`; AME-8.4 closed) | P0 |
| **Mining** | Lay mines in area | — | P1 |
| **Mine-clearing** | Clear mines | — | P1 |
| **Cargo — Delivery / Transfer** | Unload / move cargo | — | P1 |

### Events & scripting (CMO ScenEdit §5)

| Capability | CMO behavior | Aegis requirement | Priority |
|------------|--------------|-------------------|----------|
| Event editor (TCA) | Triggers, Conditions, Actions | Declarative typed DSL (JSON/YAML representation) | P0 |
| Lua API | `ScenEdit_*`, `Tool_*` functions | **Typed event DSL is v1**; no Lua v1; optional shim deferred (ADR-014) | P1 |
| Lua console | In-game REPL | Editor/debug console with MCP mirror | P2 |
| Persistent scenario state | `SetKeyValue`/`GetKeyValue` | `variables{}` store | P0 |
| Side posture & doctrine API | Hostile/neutral, doctrine get/set | Doctrine/ROE/EMCON inheritance (doc 13) | P0 |

### Designer ergonomics (CMO pain points → Aegis improvements)

| CMO limitation | Aegis improvement | Phase |
|----------------|-------------------|-------|
| Heavy modal/tab mission UI | Unified Mission Board + map-first editing | Phase 2 |
| Manual time-based mission handoff | Operations timeline with scheduled activation | Phase 2 |
| Lua pasted into opaque action boxes | Versioned typed DSL modules, linting, agent-generated scripts with review | Phase 2/3 |
| Weak validation until playtest | **Continuous/live Validation Engine** (fuel, targets, zones, DB) | v1 core (live-validation UX in flight) |
| No semantic diff / collaboration | Git-friendly canonical JSON + AI change summaries | v1 (diff) / Phase 2 (AI summaries) |
| Scenario creation is expert-only | NL authoring | Phase 2/3 |

## Functional Requirements

Every requirement carries an **AME-N.M** ID and a **priority** marker. P0 = v1 blocking; P1 = Phase 2; P2 = Phase 3/advisory.

### 1. Editor modes

- **AME-1.1** (P0) — **Play mode**: scenario features locked per design; matches CMO play behavior.
- **AME-1.2** (P0) — **Edit mode**: full authoring; optional auto-pause / low-speed sim for live testing.
- **AME-1.3** (P0) — **Headless edit mode**: CLI/MCP-only authoring for CI and agent batch generation (no UI). *Shipped* — `ProjectAegis.MissionEditor.Cli`.
- **AME-1.4** (P0) — All three modes operate on the **identical canonical file**; headless and UI are the same code path with different front-ends.

### 2. Canonical scenario file, schema & serialization

- **AME-2.1** (P0) — Native package `*.aegis-scenario` (ZIP): `manifest.json`, `scenario.json` (canonical), optional `cache.bin` (derived, never authoritative).
- **AME-2.2** (P0) — `metadata` required keys: `title`, `description`, `author`, `schemaVersion`, `dbRef`, `seed` (ulong RNG root), `editVersion` (int, monotonic optimistic-lock counter, **distinct** from `schemaVersion`).
- **AME-2.3** (P0) — Top-level nodes: `features`, `sides[]`, `orbat`, `referencePoints[]` (typed geometry), `missions[]` (typed), `operationsTimeline[]`, `events[]`, `variables{}`, `editorState`.
- **AME-2.4** (P0) — **Load-bearing invariant:** `editorState` is **derived-only** and is **never** an input to sim or the Validation Engine. No validation result is cached in it. A CI schema-lint enforces this (see AC-9).
- **AME-2.5** (P0) — **Serialization:** stable key ordering, fixed numeric formatting, LF newlines → deterministic, human-readable git diffs (AC-6). *Shipped* — `ScenarioDocumentJsonWriter` / `ScenarioDocumentJsonLoader`.
- **AME-2.6** (P0) — A **formal JSON Schema** for the scenario document is committed at [`data/scenarios/scenario-document.schema.json`](../../data/scenarios/scenario-document.schema.json) and is the machine contract for all tools/agents. Committed example fixtures live under [`data/scenarios/examples/`](../../data/scenarios/examples/): `baltic-patrol.scenario.json`, `strike-package.scenario.json`, `ferry-redeploy.scenario.json`. *(Schema + fixtures authored in parallel by the data workstream.)*

### 3. Mission system & archetypes

- **AME-3.1** (P0) — Four v1 archetypes with required fields + validation rules (GDD §3.4):

  | Type | Required fields | v1 validation |
  |---|---|---|
  | **Strike** | targets[], assigned units, weapon behavior, optional TOT/TOS | ≥1 target; targets in DB; each target fuel-reachable (§Formulas / AME-6) |
  | **Patrol** | patrol zone (≥3 waypoints / polygon/circle), variant, optional prosecution zone | non-empty zone; ≥1 assigned unit |
  | **Support** | role (Tanker/AEW/EW), station geometry | role-appropriate platform; station within theater |
  | **Ferry** | destination base | destination exists & friendly/owned; reachable |

- **AME-3.2** (P0) — All types support **doctrine / ROE / EMCON inheritance** from parent unit with explicit override, resolved at validation time and surfaced as a parent→child chain (doc 13; AC-4).
- **AME-3.3** (P0) — Typed mission CRUD emits identical canonical objects regardless of front-end. *Shipped (headless)* — `ScenarioDocumentEditor` + `mission_add_*` / `mission_update_*` / `mission_delete`.
- **AME-3.4** (P1) — **Mission Board** UI (single view by side/type/status, add-mission wizard, clone, template library, flight-plan preview). ***Phase 2 Partial+ (headless board APIs):*** `MissionBoardQuery` / bus clone+template / CLI+MCP / headless `MissionBoardPresenter` + integration tests (ME-W1). ***Residual:*** Unity `ScenarioMissionBoardWindow` product chrome (deferred).
- **AME-3.5** (P1) — **Operations timeline**: Gantt binding missions to start/end triggers, per-unit priority stack, editor scrub preview. ***Phase 2 Partial+ (headless ME-W3):*** `ScenarioDocumentEditor.UpsertTimelineEntry` / `TryRemoveTimelineEntry`; bus commit; CLI/MCP `timeline_list` / `timeline_upsert` / `timeline_delete` over `operationsTimeline[]`. Tests: `ScenarioTimelineEditorTests`, `TimelineCliTests`. ***Residual / Phase 2.4+ deferred:*** full Gantt UI, per-unit priority stack chrome, editor scrub preview.
- **AME-3.6** (P1) — Mining / mine-clear / cargo-delivery / cargo-transfer archetypes. ***Phase 2.4+ deferred (ME-W3 honesty):*** new archetypes need validation formulas and mission-type wiring — not started; do not claim shipped.

**Residual completion gate (AME-3.4/3.5):** PlayMode/manual evidence must show filtering by side/type/status, clone/template actions, keyboard traversal, timeline drag/edit, validation feedback, and scrub preview. **AME-3.6** is complete only when all four archetypes have DTOs, validation rules, CLI/MCP verbs, sample fixtures, and export/play rejection tests.

### 4. Geometry & map authoring (phased)

> **Honesty note (P2.1 / ME-W0, 2026-07-08):** **v1** ships patrol-waypoint lat/lon via `mission_add_patrol --wp lat,lon` (AME-4.1 only). **Phase 2 Partial+ (P2.1)** ships **headless** ORBAT place/move/clone and reference-point upsert/remove on the canonical document (API + CLI + co-located tests) plus headless authoring surfaces under `ProjectAegis.Delegation.UnityAdapter.Authoring` (`MapAuthoringSurface`, `EditModeController`, `LiveFindingsPresenter`) wired through `ScenarioAuthoringSession` + `ScenarioEditCommandBus`. **Not claimed:** full Unity Edit Mode map GUI / `EditorWindow` (ME-W0 host residual), snap/measure tools, layer toggles UI, sides/faction placement UI, or invalid-draw-on-screen chrome.

- **AME-4.1** (P0) — Patrol geometry as lat/lon waypoint lists in `missions[].patrolZone`; degenerate zones (<3 waypoints) are a blocking validation error. *Shipped.*
- **AME-4.2** (P1) — Map-first placement/move/clone/group of units (air, surface, sub, land, satellite) with immediate local render and commit-on-gesture-end into `orbat`/`referencePoints[]` (GDD §3.8 map-interaction contract). ***Phase 2 Partial+ (headless):*** `ScenarioDocumentEditor.UpsertOrbatUnit` / `MoveOrbatUnit` / `CloneOrbatUnit`; session/bus commit path; CLI `orbat_upsert_unit` / `orbat_move_unit` / `orbat_clone_unit`; headless `MapAuthoringSurface` + `EditModeController` (gesture-end → bus). Tests: `ScenarioOrbatReferencePointEditorTests`, `MapAuthoringSurfaceTests`, `ScenarioMapAuthoringIntegrationTests`, `OrbatReferencePointCliTests`. ***Residual:*** Unity Edit Mode map GUI host (ME-W0); full group-select / immediate local render chrome.
- **AME-4.3** (P1) — Draw/edit reference geometries (polygon, circle, line, corridor); snap/measure tools; invalid-draw stays on screen marked invalid rather than being erased. ***Phase 2 Partial+ (headless):*** `ScenarioDocumentEditor.UpsertReferencePoint` / `TryRemoveReferencePoint`; CLI `reference_point_upsert`; typed geometry on `referencePoints[]` via headless surface/session. Tests: same suite as AME-4.2. ***Residual:*** snap/measure tools, invalid-draw-on-screen marking, full map GUI draw gestures (Unity host).
- **AME-4.4** (P1) — Layer toggles (ORBAT, missions, EMCON, contacts, EW, airspace, mining), minimap + theater bounds, LOD icon density. ***Phase 2.4+ deferred (ME-W3 honesty):*** Unity map chrome residual — not started; do not claim shipped.
- **AME-4.5** (P1) — Sides / factions authoring and per-side briefing/posture placement. ***Phase 2 Partial+ (headless ME-W3):*** `ScenarioDocumentEditor.UpsertSide` / `TryRemoveSide`; bus commit; CLI/MCP `side_list` / `side_upsert` / `side_delete` over `sides[]` (name, ROE, EMCON, postures). Tests: `ScenarioSideEditorTests`, `SideCliTests`. ***Residual:*** per-side briefing/placement UI; Unity faction chrome.

**Residual completion gate (AME-4.2–4.5):** the Unity host must demonstrate scenario load, selection/group-select, gesture-end commits, persistent invalid-geometry overlays, layer toggles, minimap/theater bounds, documented LOD thresholds, and dated screenshot/PlayMode evidence.

### 5. Event & trigger system (typed DSL, no Lua v1)

- **AME-5.1** (P0) — Declarative typed event model (`trigger`, `conditions[]`, `actions[]`), human- and agent-editable, compiled to a deterministic runtime evaluation order.
- **AME-5.2** (P0) — Trigger types: Time, UnitDestroyed, UnitEntersZone, ContactDetected, Variable, MissionComplete, SidePostureChange, ScoreThreshold.
- **AME-5.3** (P0) — Unit-state trigger types (CMO parity): UnitBingoFuel, UnitWinchester, UnitDamaged (threshold), DoctrineChanged.
- **AME-5.4** (P0) — Action types: ActivateMission, DeactivateMission, SpawnUnit, RemoveUnit, SetVariable, Message/Briefing, ChangeDoctrine, SetWeather, **TeleportUnit (edit-test only)**, EndScenario.
- **AME-5.5** (P0) — **Event debugger**: projects the same firing sequence as order-log `EventFired` entries; per event `{eventId, simTick, sequenceId, unmetConditions[], actionResults[]}`; the debugger JSON is a filtered view of the order log, not a second store (doc 17; AC-7). ***Phase 2 Partial+ / Shipped headless (ME-W2):*** `EventDebuggerTrace` + `ExplainEventTrace` / `scenario_event_trace` emit full AC-7 projection fields (`sim_tick`, `sequence_id`, `action_results` plus `event_id` / `fired` / `last_evaluated_tick` / `unmet_conditions`). Tests: `EventDebuggerTests`, `StubScopePinTests` (full-projection pins). ***Residual:*** live Unity debugger chrome; order-log store coupling beyond filtered projection semantics.
- **AME-5.6** (P1) — Optional Lua compatibility shim over the typed DSL (ADR-014 — deferred; typed DSL is the v1 surface).
- **AME-5.7** (P1) — **Event static analysis**: dead triggers, unreachable states, contradictory conditions, circular dependencies. ***Phase 2 Partial+ / Shipped headless (ME-W2):*** pure `EventStaticAnalyzer` codes `EVENT_DEAD_TRIGGER`, `EVENT_UNREACHABLE_ACTION`, `EVENT_CONTRADICTORY`, `EVENT_CIRCULAR`; editor surface `AnalyzeTcaGraph()`; tests `EventStaticAnalyzerTests` + StubScope pins. ***Residual:*** visual event-graph EditorWindow; full state-reachability beyond ActivateMission↔MissionComplete cycles.

> **Maturity note (ME-W2 honesty 2026-07-08):** AME-5.5 debugger + AME-5.7 static analysis codes are **Shipped headless (Partial+)**. Typed event CRUD (`event_add` / `event_update` / `event_delete`) and headless graph node listing via `AnalyzeTcaGraph` ship with ME-W2. AME-5.2–5.4 trigger/action *type catalogs* remain partially demonstrated (condition evaluation is structured for AC-7 fixtures, not a full sim event runtime for every listed type). Do **not** claim Unity visual event-graph product chrome or Phase 3 Lua.

**Residual completion gate (AME-5.5/5.7):** graph nodes/edges must render from the canonical event model, dead/circular paths must be highlighted, and trace drill-down must use the order-log projection without creating a second event store.

### 6. Validation Engine & determinism (deterministic — no LLM)

- **AME-6.1** (P0) — The **Validation Engine** is a pure deterministic rule engine over the canonical file: same file in → same findings out, every run. It is the **sole export gate**. There is **no "Validation Agent"** and no LLM in any blocking path (ADR-008). *Shipped* — `ScenarioValidationEngine`.
- **AME-6.2** (P0) — The engine exhaustively covers the **six v1 rules** with their real error codes (confirmed in `ValidationRules.cs`):

  | # | Rule | Error code(s) |
  |---|------|---------------|
  | 1 | Mission with no assigned units | `MISSION_NO_UNITS` |
  | 2 | Empty / degenerate patrol area | `PATROL_ZONE_DEGENERATE` |
  | 3 | Strike with no targets | `STRIKE_NO_TARGETS` |
  | 4 | Ferry without a valid destination | `FERRY_NO_DESTINATION` |
  | 5 | DB version mismatch (`dbRef` ≠ available DB) | `DB_MISMATCH` |
  | 6 | Strike target not fuel-reachable | `STRIKE_UNREACHABLE` / `STRIKE_UNREACHABLE_FUEL` |

- **AME-6.3** (P0) — Additional implemented rules (beyond the six) also emit distinct codes: `FERRY_UNREACHABLE` / `FERRY_UNREACHABLE_FUEL`, `STRIKE_INVALID_PLATFORM` (assigned unit has invalid `combat_radius_nm`), `AIR_NOT_READY`, and DB/TL-binding rules `TL_BRANCH_MISSING` / `TL_BRANCH_INVALID` / `TL_BRANCH_SNAPSHOT_MISMATCH` / `TL_RELEASE_TRAIN_NOT_FOUND` / `TL_RELEASE_TRAIN_MISMATCH`.
- **AME-6.4** (P0) — **Export gate:** blocks export on any **error-severity** finding; the severity floor (`error`/`warning`) is a tuning knob (§Tuning). *Shipped* — `ScenarioValidationExportGate.EvaluateExport` → `report.CanExport(config)`.
- **AME-6.5** (P0) — **Save-vs-export rule (AC-12):** **save is allowed** with blocking errors (WIP persists); **export / play / `scenario_simulate_sample` are rejected**. Save and export are distinct gates.
- **AME-6.6** (P0) — **Determinism contract (AC-2):** same file + seed → (a) byte-identical `fire_order` (ordered array of `event.id` strings, sort key `(trigger_time_resolved, priority, event.id)`) **and** (b) identical **world-state hash** = SHA-256 over the canonical post-run world state **excluding `editorState`**. Both runs emit `SEED=<v> HASH=<sha256>`.
- **AME-6.7** (P0) — **Sim isolation:** `scenario_simulate_sample` runs in an isolated sim-world per call (no shared event queue / `variables{}`), so AC-2 holds under parallel CI runners. *Shipped* — `ScenarioSimulateSampleCommand` + `SimulateSampleGoldenHashes`.
- **AME-6.8** (P0) — **TeleportUnit transform (AC-11):** at export, an **explicit, logged** transform removes all TeleportUnit actions and records each removal in the export manifest (**not a silent strip**); the headless sample and exported scenario share an identical post-transform event set. UI badges TeleportUnit actions "edit-test only" persistently.
- **AME-6.9** (P0) — **Live / continuous validation** during authoring (re-validate on mutation, surface findings in place). *Maturity: in flight — `track1-continuous-live-validation`; headless per-mutation validation shipped via `ScenarioDocumentEditor` + live-validation tests; UX not built.*
- **AME-6.10** — **Maturity flags:** `IncompatibleHostRule` (`INCOMPATIBLE_HOST`) and `BrokenRefRule` (`BROKEN_REF`) are **simplistic/demo** rules (heuristic host/ref checks), not production model-integrity validation. Treat as demonstrative.

**Residual completion gate (AME-6.9):** every supported mutation must refresh findings in place in the Unity authoring UI, with PlayMode evidence for added, updated, and cleared findings.

### 7. Concurrency, import, export & versioning

- **AME-7.1** (P0) — **Optimistic concurrency (AC-10):** a mutating tool sends the `editVersion` it read; on mismatch the tool **conflict-rejects** with a `CONFLICT` error carrying the current `editVersion` + file hash so the caller can re-fetch and retry. **Never last-write-wins**; no partial write. *Shipped* — `ScenarioEditVersionGuard` (`ConflictCode = "CONFLICT"`, returns `CurrentEditVersion` + `FileHash`).
- **AME-7.2** (P0) — Canonical JSON is **git-friendly** (stable key ordering; AC-6) with an optional derived binary cache for large scenarios.
- **AME-7.3** (P0) — **Semantic diff** ("Strike Alpha +2 units, Patrol Bravo area moved 20 nm east"). ***Phase 2 Partial+ (ME-W3):*** pure `ScenarioSemanticDiff.Summarize(before, after)` emits deterministic id-level bullets for missions, sides, ORBAT units, timeline entries, and events; CLI/MCP `scenario_diff_summary --before --after`. Tests: `ScenarioSemanticDiffTests`, `ScenarioDiffSummaryCliTests`. ***Residual:*** natural-language prose summaries ("area moved 20 nm east"); AI change-summary agents (Phase 2/3 boundary).
- **AME-7.4** (P0) — Scenario **schema version** (`schemaVersion`) with automated migrators; distinct from `editVersion`.
- **AME-7.5** (P1) — CMO import pipeline (best-effort): missions, RP, sides, events → Aegis mapping table, documented separately. **Advisory Migration Agent only, never blocking** (ADR-013). *Phase 2/3.*

### 8. Tool surface (CLI / MCP / NL)

- **AME-8.1** (P0) — v1 core MCP/CLI tools (each mutates only the canonical file and re-runs validation; GDD §3.7): `scenario_create`, `scenario_load`, `scenario_save`, `mission_add`, `mission_update`, `mission_delete`, `mission_assign_units`, `reference_point_set`, `event_add`, `event_validate`, `scenario_validate`, `scenario_simulate_sample`, `scenario_export_brief`.
- **AME-8.2** (P0) — CLI and MCP share the same underlying APIs; MCP bindings mirror the CLI (`tools/mission-editor/mcp-tools.json`). No special auto-commit path.
- **AME-8.3** (P0) — **Required verbs per mission type.** Patrol and Strike expose `mission_add_*` + `mission_update_*`; Support and Ferry must expose equivalents.
- **AME-8.4** (P0) — **Ferry CLI/MCP verbs shipped:** `mission_add_ferry` / `mission_update_ferry` expose ferry authoring at the tool surface (was GAP; closed 2026-07-03).
- **AME-8.5** (P0) — **Undo/rollback shipped at CLI/MCP:** `scenario_undo` restores the prior committed mutation from the on-disk undo stack (wired through `ScenarioDocumentEditor`; closed with ferry track). In-memory + file-backed undo at the tool surface. *Tests:* `src/ProjectAegis.MissionEditor.Cli.Tests/ScenarioUndoCliTests.cs`.
- **AME-8.6** (P1) — **Natural-language authoring** ("Add a SEAD patrol over Gotland H+0→H+2, then transition fighters to a strike…"). *Phase 2/3.* v1 ships only a demonstrative scaffold — `scenario_ai_scaffold` / `AiAuthoringServices.NlScaffold` (**Maturity: stub**) and a heuristic `mission_plan_suggest`.

> **Shipped scenario/mission CLI verbs (headless, `ProjectAegis.MissionEditor.Cli/Program.cs`):** `scenario_create`, `scenario_validate`, `scenario_publish`, `scenario_export`, `scenario_export_brief`, `scenario_simulate_sample`, `scenario_ai_scaffold`, `scenario_event_trace`, `scenario_migrate_preview`, `scenario_umpire_snapshot`, `scenario_comms_status`, `scenario_cyber_status`, `scenario_near_future_spawn`, `scenario_undo`, `mission_add_patrol`, `mission_add_strike`, `mission_add_support`, `mission_add_ferry`, `mission_update_patrol`, `mission_update_strike`, `mission_update_support`, `mission_update_ferry`, `mission_delete`, `mission_plan_suggest`, **`orbat_upsert_unit`**, **`orbat_move_unit`**, **`orbat_clone_unit`**, **`reference_point_upsert`**, **`event_add`**, **`event_update`**, **`event_delete`** (ME-W2), **`side_list`**, **`side_upsert`**, **`side_delete`**, **`timeline_list`**, **`timeline_upsert`**, **`timeline_delete`**, **`scenario_diff_summary`** (ME-W3). Generic GDD names in AME-8.1 (`mission_add` / `event_add` / `reference_point_set` / `mission_assign_units`) map to per-type / ORBAT-RP / event verbs on the shipped surface. Ferry + support add/update, undo (AME-8.4 / AME-8.5), P2.1 ORBAT/RP map mutations, ME-W2 event CRUD, and ME-W3 sides/timeline/semantic-diff are **shipped** (headless Partial+).

### 9. Agentic authoring agents (advisory — Phase 2/3, LLM-driven)

> These are **advisory** systems. They **propose** changes as preview diffs; nothing commits without explicit accept (configurable auto-accept in headless CI). Every agent edit records prompt, rationale, diff hash, and approving user/agent id (provenance, doc 07). **They never sit in a blocking path** — the deterministic Validation Engine remains the sole export gate.

- **AME-9.1** (P2) — **Mission Planner Agent** — proposes ORBAT, missions, reference points from an NL brief.
- **AME-9.2** (P2) — **Red Force Agent** — designs plausible adversary missions, EMCON, triggers.
- **AME-9.3** (P2) — **Briefing Writer Agent** — generates side briefings, intel, victory conditions.
- **AME-9.4** (P2) — **Balance Agent** — runs quick headless samples; flags overpowered force ratios.
- **AME-9.5** (P2) — **Migration Agent** — imports CMO scenarios (where permitted) and maps to Aegis format (ADR-013).
- **AME-9.6** (P2) — **Transparency:** agent-authored scenarios are labeled in multiplayer/briefing, stored in `metadata`/provenance (ADR-015, recommended yes).

### 10. Shipped scenario-ops capabilities (umpire, migration, publish, static analysis)

Capabilities that shipped in the headless stack but were previously unspecified. Status is stated honestly.

- **AME-10.1** (P1) — **Umpire / adjudication workspace**: snapshot, before/after diff, audit, freeze/step/inject/resume, role-based permissions (player/author/reviewer/umpire) for adjudicated play (research §umpire). *Shipped (headless)* — `AdjudicationWorkspace` + `scenario_umpire_snapshot`. **Maturity: Partial — headless, no Unity UX; adjudication surfaces are demonstrative.**
- **AME-10.2** (P1) — **DB migration preview + reversibility**: preview a scenario's DB upgrade, detect broken mounts/sensors/loadouts/doctrine refs, with reversible snapshot/rollback (research §migration). *Shipped (preview)* — `ScenarioDbMigrationPreview` + `scenario_migrate_preview`. **Maturity: Partial — `track25-scenario-db-migration` in flight; reversible-migration persistence to disk is NOT yet complete.**
- **AME-10.3** (P1) — **Publish manifest & governance**: publish emits a provenance manifest (semver, embedded validation report, review gate, ORBAT provenance) and blocks on validation failure (research §publish). *Shipped* — `ScenarioPublishCommand` + `ScenarioManifest` / `ManifestBuilder` + `scenario_publish`. **Maturity: Partial+.**
- **AME-10.4** (P1) — **Live / continuous validation during authoring** — see AME-6.9 (`track1-continuous-live-validation` in flight).
- **AME-10.5** (P1) — **Event static analysis** — see AME-5.7 (**ME-W2 Shipped headless:** `EventStaticAnalyzer` codes for dead / unreachable / contradictory / circular; visual graph UI residual).

**Residual completion gate (AME-10.2):** migration must write a persisted rollback snapshot and manifest entry, expose a restore command, and pass an interrupted/failing-migration recovery test.

**Phase 3 acceptance gate (AME-9.1–9.6):** before any authoring agent is called shipped, each agent must define a proposal-diff schema, provenance fields, approval policy, deterministic Validation Engine handoff, transparency-label behavior, and an automated test proving no LLM can block export.

## Formulas

### Strike/Ferry fuel-reachability (Validation Engine, GDD §4.1)

**DB convention (locked):** `combat_radius_nm` is the standard military **combat radius** (one-way out, deliver, return with reserves). The round trip is already accounted for — the formula compares one-way distance to target and **must not** double it.

```
range_to_target_nm = haversine(launch_base, target) + ingress_egress_pad_nm
available_radius_nm = combat_radius_nm * fuel_fraction
reachable          = available_radius_nm >= range_to_target_nm
```

- `ingress_egress_pad_nm` — extra one-way reach for routing/loiter. **Range 20–150, default 50.**
- `combat_radius_nm` — **must be > 0**; a unit with `combat_radius_nm ≤ 0` is a validation error (`STRIKE_INVALID_PLATFORM`), not `reachable=false`.
- `fuel_fraction` — usable fraction after reserves. **Range 0.70–0.95, default 0.85.**
- **Input validation first:** reject `fuel_fraction` outside [0.70,0.95] and `ingress_egress_pad_nm` outside [20,150] as config errors; reject `combat_radius_nm ≤ 0` as an ORBAT error.

*Shipped* — `ReachabilityCalculator.HaversineNm` + `TryClassifyStrikeUnreachable` (emits `STRIKE_UNREACHABLE` / `STRIKE_UNREACHABLE_FUEL`; ferry variant maps to `FERRY_UNREACHABLE[_FUEL]`).

### Deterministic event evaluation order (GDD §4.2)

```
sort_key(event) = (trigger_time_resolved ASC, event.priority ASC, event.id ASC)
fire_order      = stable_sort(active_events, by = sort_key)
```

`event.priority` 0–1000 (default 100, lower fires first); `event.id` unique (lexicographic tiebreaker → total order, no ties). Within a tick, events fire sequentially in `fire_order`; each event's actions apply before the next event's conditions evaluate. `fire_order` is exported as an ordered array of `event.id` strings (AC-2 / AC-7 assert against it).

### Event-graph complexity (soft cap, ADR-016)

```
complexity        = E + sum(conditions_per_event) + C * cross_refs
peak_tick_density = max over ticks of (events with trigger_time_resolved == tick)
warn if complexity > WARN_THRESHOLD OR peak_tick_density > DENSITY_THRESHOLD
```

`WARN_THRESHOLD` 200–1000 (default 400); `DENSITY_THRESHOLD` 10–50 (default 20); `C` 1–4 (default 2); **hard cap 32 conditions/event** (error above). Both are **warnings only** — never block export.

## Non-Functional Requirements

| Area | Target |
|------|--------|
| Determinism | Same file + seed → identical `fire_order` + world-state hash (AME-6.6); pure Validation Engine; no wall-clock/locale dependence in canonical output |
| Performance | Edit 5,000+ unit ORBAT without freeze; background/live validation; event-graph caps protect the eval loop |
| Assembly boundary | No `UnityEngine` in `ProjectAegis.Data` (ADR-001/006); authoring/validation live in the Data assembly; headless-testable |
| Accessibility | Keyboard-first mission board; screen-reader labels (Phase 2 UI) |
| Security | No arbitrary code execution in play mode; typed DSL (no Lua v1) removes the script-injection surface |
| Localization | All player-facing briefing strings externalized |
| Audit | Full edit log per scenario; publish manifest carries provenance (AME-10.3) |

## Requirement Status Matrix

| AME IDs | Priority / phase | Current state | Evidence | Residual / completion boundary |
|---------|------------------|---------------|----------|--------------------------------|
| AME-1.1…2.6, 3.1…3.3, 4.1, 6.1…6.8, 7.1…7.4, 8.1…8.5 | P0 / v1 | **Shipped / Partial+ headless** | Implementation mapping + AC-1…AC-12 manifest | Keep canonical paths and deterministic gates green |
| AME-3.4…3.5, 4.2…4.3, 4.5, 5.5, 5.7, 7.3, 10.1, 10.3, 10.5 | P1 / Phase 2 | **Shipped / Partial+ headless; phase gate complete 2026-07-09** | Headless mapping, co-located tests, phase gate | Unity/product chrome and breadth gates above remain open |
| AME-3.6, 4.4, 6.9, 10.2, 10.4 | P0/P1 / Phase 2.4+ | **Residual or Deferred** | Existing headless subset where cited | Archetype, UI, live-finding, and rollback gates above |
| AME-5.6, 7.5, 8.6, 9.1…9.6 | P1/P2 / Phase 3 | **Deferred / not shipped** | ADR-013…015 and agent acceptance gate | Legal/design decisions and deterministic proposal workflow required |

## Implementation Mapping (headless)

| Requirement area | Type / path (`ProjectAegis.Data` unless noted) | Status |
|------------------|-----------------------------------------------|--------|
| Canonical document editor | `Scenario/Authoring/ScenarioDocumentEditor` (missions, **ORBAT/RP**, live validate, migration preview, umpire hooks) | Shipped (Partial+) |
| ORBAT / reference-point mutations (AME-4.2/4.3) | `ScenarioDocumentEditor.UpsertOrbatUnit`, `MoveOrbatUnit`, `CloneOrbatUnit`, `UpsertReferencePoint`, `TryRemoveReferencePoint` | **Phase 2 Partial+ (P2.1 headless)** |
| Authoring session + command bus | `Scenario/Authoring/ScenarioAuthoringSession`, `ScenarioEditCommandBus` (editVersion, undo capture, live findings) | **Phase 2 Partial+ (P2.1 headless)** |
| Headless map authoring surfaces | `ProjectAegis.Delegation.UnityAdapter/Authoring/MapAuthoringSurface`, `EditModeController`, `LiveFindingsPresenter` | **Phase 2 Partial+ (P2.1 headless)** — no Edit Mode `EditorWindow` claimed |
| Serialization | `Scenario/Authoring/ScenarioDocumentJsonWriter` / `ScenarioDocumentJsonLoader` | Shipped |
| DTOs | `Scenario/Authoring/ScenarioDocumentDto` / `ScenarioMetadataDto` | Shipped |
| Optimistic concurrency | `Scenario/Authoring/ScenarioEditVersionGuard` (`CONFLICT` + editVersion + hash) | Shipped |
| Validation Engine | `Validation/ScenarioValidationEngine` + `Validation/Rules/ValidationRules` | Shipped |
| Reachability formula | `Validation/ReachabilityCalculator` | Shipped |
| Export gate | `Validation/ScenarioValidationExportGate` + `ValidationReport.CanExport` | Shipped |
| Validation config / knobs | `Validation/ValidationConfig` (`assets/data/editor/validation-config.json`) | Shipped |
| Golden determinism hashes | `Validation/ValidationGoldenHashes`, CLI `SimulateSampleGoldenHashes` | Shipped |
| JSON Schema + fixtures | `data/scenarios/scenario-document.schema.json`, `data/scenarios/examples/*.scenario.json` | Shipped |
| CLI / MCP surface | `ProjectAegis.MissionEditor.Cli/Program.cs` (~28 scenario/mission/ORBAT-RP verbs); `tools/mission-editor/mcp-tools.json` | Shipped (ferry + support + undo + P2.1 ORBAT/RP) |
| Mission verbs | `MissionAddPatrolCommand`, `MissionAddStrikeCommand`, `MissionAddSupportCommand`, `MissionAddFerryCommand`, `MissionUpdatePatrolCommand`, `MissionUpdateStrikeCommand`, `MissionUpdateSupportCommand`, `MissionUpdateFerryCommand`, `MissionDeleteCommand`, `ScenarioUndoCommand` | Shipped (AME-8.4 ferry + AME-8.5 undo closed; `mission_update_support` shipping) |
| ORBAT / RP CLI verbs (AME-4.2/4.3) | `OrbatUpsertUnitCommand`, `OrbatMoveUnitCommand`, `OrbatCloneUnitCommand`, `ReferencePointUpsertCommand` → `orbat_upsert_unit`, `orbat_move_unit`, `orbat_clone_unit`, `reference_point_upsert` | **Phase 2 Partial+ (P2.1 headless)** |
| Headless sample | `ScenarioSimulateSampleCommand` | Shipped |
| Publish + manifest | `ScenarioPublishCommand`, `Scenario/Authoring/ScenarioManifest` | Shipped (Partial+) |
| Umpire / adjudication | `Scenario/Authoring/AdjudicationWorkspace`, `scenario_umpire_snapshot` | Shipped (Partial — no UX) |
| DB migration preview | `Scenario/Authoring/ScenarioDbMigrationPreview`, `scenario_migrate_preview` | Shipped (Partial — no reversible persistence) |
| Event trace / debugger (AME-5.5 / AC-7) | `EventDebuggerTrace`, `ExplainEventTrace`, `scenario_event_trace` — full projection: `sim_tick`, `sequence_id`, `action_results`, `event_id`, `fired`, `last_evaluated_tick`, `unmet_conditions` | **Phase 2 Partial+ / Shipped headless (ME-W2)** — AC-7 green |
| Event static analysis (AME-5.7 / 10.5) | `EventStaticAnalyzer` (`EVENT_DEAD_TRIGGER`, `EVENT_UNREACHABLE_ACTION`, `EVENT_CONTRADICTORY`, `EVENT_CIRCULAR`); `ScenarioDocumentEditor.AnalyzeTcaGraph` | **Phase 2 Partial+ / Shipped headless (ME-W2)** — visual graph UI residual |
| Event CRUD CLI/MCP (ME-W2) | `EventCommands` → `event_add` / `event_update` / `event_delete`; editor upsert/remove + bus | **Phase 2 Partial+ / Shipped headless (ME-W2)** |
| Sides CRUD (AME-4.5) | `ScenarioDocumentEditor.UpsertSide` / `TryRemoveSide`; bus; CLI `side_list` / `side_upsert` / `side_delete` | **Phase 2 Partial+ / Shipped headless (ME-W3)** — Unity faction UI residual |
| Operations timeline (AME-3.5) | `UpsertTimelineEntry` / `TryRemoveTimelineEntry`; CLI `timeline_list` / `timeline_upsert` / `timeline_delete` | **Phase 2 Partial+ / Shipped headless (ME-W3)** — Gantt UI Phase 2.4+ deferred |
| Semantic diff (AME-7.3) | `ScenarioSemanticDiff.Summarize`; CLI `scenario_diff_summary` | **Phase 2 Partial+ / Shipped headless (ME-W3)** — NL prose residual |
| Mining / cargo (AME-3.6) | — | **Phase 2.4+ deferred (ME-W3 honesty)** — not started |
| Layers / minimap (AME-4.4) | Unity map chrome | **Phase 2.4+ deferred (ME-W3 honesty)** — not started |
| AI scaffold / NL | `Scenario/Authoring/AiAuthoringServices`, `ScenarioAiScaffoldCommand`, `mission_plan_suggest` | Stub (advisory) |
| Model-integrity rules | `IncompatibleHostRule` / `BrokenRefRule` | Shipped (demo/simplistic) |
| Map-first Edit Mode GUI / Mission Board / timeline UI | Unity `EditorWindow` host + product chrome | **Partial+ / residual** — headless map mutations shipped (AME-4.2/4.3); **AME-3.4 headless Mission Board APIs shipped (ME-W1)**; **AME-3.5/4.5 headless timeline + sides shipped (ME-W3)**; Unity Mission Board window + map GUI host + Gantt + layers/minimap remain residual / Phase 2.4+ |
| Authoring agents | Mission Planner / Red Force / Briefing / Balance / Migration | Not started (Phase 3 — do not claim shipped) |
| Tests (co-located) | `src/ProjectAegis.Data.Tests/Scenario/` (incl. `EventDebuggerTests`, `EventStaticAnalyzerTests`, `ScenarioEventCrudEditorTests`, `StubScopePinTests`, `ScenarioSideEditorTests`, `ScenarioTimelineEditorTests`, `ScenarioSemanticDiffTests`, ORBAT/map suites), `…/Validation/` (incl. `EventGraphComplexityTests`), `…/Architecture/`, `src/ProjectAegis.MissionEditor.Cli.Tests/` (incl. `EventCrudCliTests`, `OrbatReferencePointCliTests`, `SideCliTests`, `TimelineCliTests`, `ScenarioDiffSummaryCliTests`), `src/ProjectAegis.Delegation.UnityAdapter.Tests/Authoring/`, AC-8 proxy in `…/Bridge/PlayModeSmokeHarnessTests.cs` | Shipped (AC-1…12 green headless; **AC-7 Met** full projection; **AC-8 Met** host load path; ME-W2 event + ME-W3 sides/timeline/diff suites) |

## Acceptance Criteria

Adopted in substance from the historical approved GDD (AC-1…AC-12); each is independently, mechanically testable with a defined fixture and observable output, and bound to the AME requirement(s) it verifies. Primary test evidence is co-located under `src/ProjectAegis.*.Tests/` (hybrid layout); supporting fixtures, scripts, and gate notes are cited where relevant. Auditable path inventory: [req-11 scenario-editor evidence manifest (2026-07-11)](../evidence/req-11-scenario-editor-evidence-2026-07-11.md).

> **Residual honesty (SE-W1 + P2.1 ME-W0-c + ME-W2 + ME-W3 2026-07-09).** Headless AC-1…AC-12 have green co-located tests cited below. **AC-7 is Met** (full debugger projection: `sim_tick`, `sequence_id`, `action_results`) — evidence `EventDebuggerTests` + `StubScopePinTests`. **AC-8 is Met** (host load path / PlayMode proxy evidence) — not reopened. **AME-4.2/4.3** are **Phase 2 Partial+ headless**; **AME-5.5 / 5.7** are **Phase 2 Partial+ / Shipped headless (ME-W2)**; **AME-3.5 / 4.5 / 7.3** are **Phase 2 Partial+ / Shipped headless (ME-W3)**; **AME-3.6 mining/cargo** and **AME-4.4 layers/minimap** are **Phase 2.4+ deferred** (honest non-ship). Unity visual event-graph chrome, Mission Board product window, Gantt UI residual. Phase 3 agents/import/Lua **not** shipped.

- [x] **AC-1 (Logic → AME-6.2, Formulas):** A strike whose target lies beyond `combat_radius_nm * fuel_fraction` makes `scenario_validate` return a blocking error `STRIKE_UNREACHABLE` with message "…out of combat radius by N nm…"; test asserts code + computed `N`. *`src/ProjectAegis.Data.Tests/Validation/ScenarioValidationEngineTests.cs` (`Strike_unreachable_message_matches_AC1_format_with_computed_excess_nm`); CLI surface `src/ProjectAegis.MissionEditor.Cli.Tests/ScenarioValidateCliTests.cs`.*
- [x] **AC-2 (Logic → AME-6.6, AME-6.7):** Given a fixed `metadata.seed` + identical knobs, two independent `scenario_simulate_sample` runs produce (a) byte-identical `fire_order` arrays and (b) identical world-state hash = SHA-256 over canonical post-run state excluding `editorState`; both emit `SEED=<v> HASH=<sha256>`; holds under parallel CI runners. *`src/ProjectAegis.MissionEditor.Cli.Tests/ScenarioSimulateSampleCliTests.cs` (`scenario_simulate_sample_determinism_two_runs_identical_fire_order_and_hash`, `…_nonempty`, `…_holds_under_parallel_execution_isolation`).*
- [x] **AC-3 (Logic → AME-6.2):** The Validation Engine flags all six v1 rules, each with its specific error code — AC-3a `MISSION_NO_UNITS`, AC-3b `PATROL_ZONE_DEGENERATE`, AC-3c `STRIKE_NO_TARGETS`, AC-3d `FERRY_NO_DESTINATION`, AC-3e `DB_MISMATCH`, AC-3f `STRIKE_UNREACHABLE`. *`src/ProjectAegis.Data.Tests/Validation/ScenarioValidationEngineTests.cs` (per-rule Facts) + `ValidationGoldenTests.All_six_v1_rules_emit_expected_codes` + `ScenarioDocumentEditorLiveValidationTests.cs`.*
- [x] **AC-4 (Integration → AME-3.2):** Fixture `data/scenarios/validation/doctrine-inheritance.json` (Side A `ROE=WeaponsFree`; a Strike override `ROE=WeaponsTight`; a Patrol with no override) → `scenario_validate` reports resolved Strike `WeaponsTight`, Patrol `WeaponsFree` (inherited). *`src/ProjectAegis.Data.Tests/Validation/DoctrineInheritanceValidateTests.cs`.*
- [x] **AC-5 (Integration → AME-8.1):** The core MCP/CLI suite creates, validates, and runs a 15-min headless sample of a Strike+Patrol+Support+Ferry scenario with no Unity process spawned; runner exits 0 and emits a `sample-complete` JSON record. *`src/ProjectAegis.MissionEditor.Cli.Tests/SampleCompletePipelineTests.cs` (`strike_patrol_support_ferry_pipeline_validates_and_emits_sample_complete`); also `ScenarioSimulateSampleCliTests` AC-5 ferry sample.*
- [x] **AC-6 (Config → AME-2.5, AME-7.2):** (a) Two independent create+mutate runs with identical CLI args yield byte-identical `scenario.json` (SHA-256 equal) — proves deterministic serialization across processes. (b) Changing exactly one content field (which also bumps `metadata.editVersion`) yields a `git diff` of **≤2 hunks** / ≤2 changed content locations, no key reordering. *Honesty: every mutation bumps `editVersion`, so a pure single-hunk content-only diff is not available via CLI; see `tools/ci/smoke-ac6.sh`.*
- [x] **AC-7 (Logic → AME-5.5) — Met (full projection, ME-W2):** For an event `E` whose `UnitEntersZone` condition never holds, the debugger JSON contains `{ "event_id": E.id, "fired": false, "last_evaluated_tick": <int>, "sim_tick": <int>, "sequence_id": <int>, "action_results": [ { "type": …, "applied": false, … } ], "unmet_conditions": [ { "type": "UnitEntersZone", "result": false, … } ] }`. Full order-log-aligned projection fields **shipped**. *`src/ProjectAegis.Data.Tests/Scenario/EventDebuggerTests.cs` (`UnitEntersZone_never_holds_emits_fired_false_with_unmet_conditions`, fixture path `UnitEntersZone_never_holds_from_event_no_fire_fixture_…`) + fixture `data/scenarios/examples/event-no-fire.scenario.json`; implementation `src/ProjectAegis.Data/Scenario/Authoring/EventDebuggerTrace.cs`; maturity pin `src/ProjectAegis.Data.Tests/Scenario/StubScopePinTests.cs` (full AC-7 field types).*
- [x] **AC-8 (Integration → AME-1.4, AME-2.4) — Met (host load path):** Headless-authored `.scenario.json` loads with intact ORBAT/missions/events; `editorState` defaults (camera + layers) applied in-memory (derived-only; never written to canonical fixtures). *`src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/PlayModeSmokeHarnessTests.cs` (`AC8_Unity_host_roundtrip_…`, `AC8_strike_package_fixture_…`); evidence `production/qa/ac8-unity-roundtrip-evidence-2026-07-08.md`. Not claimed: full Unity Edit Mode map GUI (ME-W0 residual). Headless ORBAT/RP map mutations are separate AME-4.2/4.3 Partial+ scope.*
- [x] **AC-9 (Logic → AME-2.4):** The schema lint fails the build if any field other than `editorState` is tagged `derived-only`, or if any sim/Validation-Engine path reads a field under `editorState`. *`src/ProjectAegis.Data.Tests/Scenario/DerivedOnlyInvariantTests.cs` + `src/ProjectAegis.Data.Tests/Architecture/DerivedOnlyInvariantTests.cs`.*
- [x] **AC-10 (Integration → AME-7.1):** Two mutating MCP/CLI calls with a stale `editVersion` → the second returns a `CONFLICT` error carrying current `editVersion` + file hash; no partial write occurs. *`src/ProjectAegis.Data.Tests/Scenario/ScenarioEditVersionGuardTests.cs`; CLI: `src/ProjectAegis.MissionEditor.Cli.Tests/McpMissionToolCliTests.cs` (`mission_add_patrol_stale_edit_version_returns_conflict_exit_code`), `MissionAddFerryCommandTests.mission_update_ferry_stale_edit_version_returns_conflict_exit_code`.*
- [x] **AC-11 (Logic → AME-6.8):** Exporting a scenario containing TeleportUnit actions produces an exported event set with zero TeleportUnit actions + a manifest entry logging each removal; the headless sample's post-transform event set equals the exported set. *`src/ProjectAegis.Data.Tests/Scenario/TeleportUnitExportTests.cs`.*
- [x] **AC-12 (Logic → AME-6.5):** Save succeeds on a scenario with blocking errors; export / play / `scenario_simulate_sample` on the same file are rejected with the blocking error list. *`src/ProjectAegis.Data.Tests/Validation/SaveVsExportGateTests.cs`.*

## Tuning Knobs

Data-driven; live in `assets/data/editor/validation-config.json` unless noted (`ValidationConfig`).

| Knob | Range | Default | Affects |
|---|---|---|---|
| `ingress_egress_pad_nm` | 20–150 | 50 | Strike/ferry reachability strictness |
| `fuel_fraction` | 0.70–0.95 | 0.85 | Fuel-validation conservatism |
| `event.priority` (per event) | 0–1000 | 100 | Intra-tick firing order |
| `WARN_THRESHOLD` (complexity) | 200–1000 | 400 | Complexity perf warning |
| `DENSITY_THRESHOLD` (events/tick) | 10–50 | 20 | Tick-density perf warning |
| `C` cross-ref weight | 1–4 | 2 | Complexity sensitivity to coupling |
| Headless sample length | 1–60 min | 15 | `scenario_simulate_sample` duration |
| Validation severity floor for export block | error / warning | error | Export-gate strictness (AME-6.4) |

## Phased Delivery

| Phase | Scope | Status |
|-------|-------|--------|
| **v1 (headless)** | Canonical file + schema, Strike/Patrol/Support/Ferry archetypes, typed events (Partial+ after ME-W2 debugger/static analysis), Validation Engine (six rules + extras), determinism contract, editVersion concurrency, headless sample, core CLI/MCP (ferry + support + undo shipped), publish/umpire/migration-preview (partial) | **Shipped (Partial+)** — residual: live-validation UX (AME-6.9). **AC-7 Met** / **AC-8 Met** |
| **Phase 2** | Map authoring (ORBAT/RP), Unity edit-mode host UX, Mission Board, operations timeline, mining/cargo archetypes, visual event graph + full static analysis (AC-7 green), reversible migration persistence; NL Mission Planner / CMO import deferred toward Phase 2/3 boundary | **Phase gate complete 2026-07-09 (Partial+ headless)** — human ack received: **“Mission editor Phase 2 complete”**. P2.1 headless map ORBAT/RP, Mission Board APIs, event debugger/static analysis/CRUD, sides, timeline, and semantic diff shipped. Mining/cargo, layers/minimap, reversible migration persistence, and Unity product chrome remain explicit Phase 2.4+ residual/deferred scope. |
| **Phase 3** | Full event DSL + optional Lua shim, Red Force Agent, collaborative review, Workshop-style sharing | **Not started** — do not invent as shipped |

## Open Questions / Decisions Needed

Each former open question now points to its ADR.

| # | Question | ADR | Owner / review date | Status / Recommendation |
|---|----------|-----|---------------------|-------------------------|
| Q1 | Legal/policy stance on **CMO scenario import** (feasibility vs licensing) | ADR-013 | Product / Legal · 2026-09-01 | **Proposed** — best-effort, advisory Migration Agent only, never blocking; confirm licensing before build |
| Q2 | **Lua compatibility**: full `ScenEdit_*` shim vs curated subset | ADR-014 | Accepted; no pending review | **Accepted** — typed DSL is v1; **no Lua v1**; optional shim deferred to Phase 3 |
| Q3 | Should **agent-authored scenarios** be labeled for transparency? | ADR-015 | Design · 2026-09-01 | **Proposed — recommend yes**; store in `metadata`/provenance (affects Phase 2/3 agents only) |
| Q4 | Maximum **event-graph complexity** before warnings (soft cap) | ADR-016 | Accepted; perf-budget review | **Accepted** — soft complexity + tick-density warnings; hard cap 32 conditions/event; never blocks export; finalize thresholds at perf budgeting |
| Q5 | Editor **inside game client** only, or standalone **Scenario Lab** sharing the core library | ADR-017 | Technical Director · 2026-10-01 | **Proposed** — v1 is headless/file-based; topology decision pending |

## Traceability

| Related doc / artifact | Relationship |
|------------------------|--------------|
| 01 Project Overview | Theater-scale scenarios, agentic gameplay |
| 02 Core Gameplay Loop | Phase 1–2 planning and mission assignment |
| 04 Agent Delegation | Mission-level autonomy and personalities (agents advisory) |
| 06 Database Intelligence | Unit data, validation, DB version binding (`dbRef`/`tlBranch`); migration preview |
| 07 Agentic Infrastructure | Scenario Generation, authoring agents, provenance |
| 08 Agentic Architecture | Deterministic sim, MCP, headless execution |
| 13 Doctrine/ROE/EMCON/WRA | Runtime policy inheritance (AME-3.2) |
| 14 Engagement & Fire Control | Mission auto-engage and fire pipeline |
| 17 Replay & Order Log | Deterministic replay, event debugger projection (AME-5.5), quick-run evidence |
| 21 Platform Editor | Platform classes edited there; scenario placement stays here |
| ADR-008 | Validation Engine + determinism (authority) |
| ADR-013…017 | Import policy, Lua scope, agent labeling, event-graph caps, editor topology |
| `data/scenarios/scenario-document.schema.json` + `examples/` | Machine contract + fixtures (AME-2.6) |
| GDD `agentic-mission-editor.md` | Historical design source; current implementation truth is this requirement plus the 2026-07-09 gate evidence |
| CMO Manual §3.3.17 / §4.1.5 / §5 / §7.1–7.3 | Parity baseline (clean-room, observable only) |

---

**References**

- [CMO Manual (PDF)](https://www.matrixgames.com/amazon/PDF/CMO/CMO_manual_EBOOK.pdf) — §3.3.17 Mission Editor, §4.1.5 Scenario Editor, §5 ScenEdit, §7.1–7.3 Missions and Reference Points
- [Command Lua Documentation](https://commandlua.github.io/) — reference only; no Lua in v1 (ADR-014)
- [CMO Scenario Editor Manual Addendum](https://command.matrixgames.com/?page_id=2709)
- [scenario-editor-research.md](../../docs/research/scenario-editor-research.md) — umpire/adjudication, migration, publish governance, live validation, static analysis

---

**Status:** Revised — implementation-aligned through Mission Editor Phase 2 gate completion on 2026-07-09. v1/headless and named Phase 2 headless capabilities are shipped Partial+ with AC-1…AC-12 green. Residual Phase 2.4+ product UI and archetype/migration work is governed by the explicit completion gates above. Phase 3 agents/import/Lua are not shipped.

---

## Delivery History Appendix

**Charter honesty SE-W1 2026-07-08** — AC paths realigned to co-located tests; ferry/undo/mapping fixed.  
**P2.1 ME-W0-c 2026-07-08** — Doc honesty for headless map ORBAT/RP ship; removed “map not started” / blanket “no map-first” claims that contradicted AME-4.2/4.3 Partial+; Unity map GUI host residual explicit.  
**ME-W2 W2-e/f 2026-07-08** — AC-7 flipped green with `EventDebuggerTests` evidence (`sim_tick`, `sequence_id`, `action_results`); AME-5.5/5.7 maturity Partial+/Shipped headless; implementation mapping for `EventStaticAnalyzer` + event CRUD; ME-003 Complete.  
**ME-W3 W3 honesty 2026-07-09** — AME-4.5 sides CRUD Partial+ (`UpsertSide`, `side_list`/`upsert`/`delete`); AME-3.5 timeline Partial+ (`timeline_list`/`upsert`/`delete`; Gantt UI Phase 2.4+); AME-7.3 `ScenarioSemanticDiff` + `scenario_diff_summary` Partial+; AME-3.6 mining/cargo + AME-4.4 layers/minimap explicitly Phase 2.4+ deferred; ME-004 Complete.
