# 11 - Agentic Mission & Scenario Editor

**Last Updated:** 2026-07-08
**Status:** Revised — implementation-aligned (was Draft; realigned to approved GDD + shipped headless stack)
**FR reverse-ref:** [FR-09](01-Project-Overview.md) — Scenario/mission editor
**Author basis:** Approved GDD [`design/gdd/agentic-mission-editor.md`](../../design/gdd/agentic-mission-editor.md) (terminology, determinism contract, AC-1…AC-12); codebase review of `ProjectAegis.Data/Scenario/Authoring` + `ProjectAegis.Data/Validation` + `ProjectAegis.MissionEditor.Cli`; [scenario-editor research](../../docs/research/scenario-editor-research.md); [CMO Official Manual](https://www.matrixgames.com/amazon/PDF/CMO/CMO_manual_EBOOK.pdf) (Mission Editor §3.3.17/§7.1, Scenario Editor §4.1.5, ScenEdit §5, clean-room observable behavior only); requirements 01–10, 13, 14, 17.
**Related:** [06-Database-Intelligence.md](06-Database-Intelligence.md) · [21-Platform-Editor.md](21-Platform-Editor.md) · [04-Agent-Delegation.md](04-Agent-Delegation.md) · [07-Agentic-Infrastructure.md](07-Agentic-Infrastructure.md) · [08-Agentic-Architecture.md](08-Agentic-Architecture.md) · [13-Doctrine-ROE-EMCON-WRA.md](13-Doctrine-ROE-EMCON-WRA.md) · [17-Replay-AAR-And-Order-Log.md](17-Replay-AAR-And-Order-Log.md)
**Decision record:** [ADR-008 Mission-Editor Validation Engine (Accepted)](../../docs/architecture/adr-008-mission-editor-validation-engine.md) · [ADR-013 CMO Scenario Import Policy (Proposed)](../../docs/architecture/adr-013-cmo-scenario-import-policy.md) · [ADR-014 Lua Compatibility Scope (Accepted)](../../docs/architecture/adr-014-lua-compatibility-scope.md) · [ADR-015 Agent-Authored Scenario Transparency (Proposed)](../../docs/architecture/adr-015-agent-authored-scenario-transparency.md) · [ADR-016 Event-Graph Complexity Caps (Accepted)](../../docs/architecture/adr-016-event-graph-complexity-caps.md) · [ADR-017 Editor Topology: Client vs Scenario Lab (Proposed)](../../docs/architecture/adr-017-editor-topology-client-vs-scenario-lab.md)

## Purpose

Implements hub **[FR-09](01-Project-Overview.md)** (Scenario/mission editor).

Define requirements for an **agentic mission and scenario editor** that preserves the depth and designer power of Command: Modern Operations (CMO) while delivering a uniquely improved authoring experience built on an **intent-compiler spine**: a single canonical declarative scenario file is the source of truth, and every authoring path (map drawing, natural language, MCP tool calls) emits the *same* canonical objects. Determinism, git-diffability, deterministic validation, headless generation, and AI co-authoring are therefore structural properties of the file format, not features layered on top.

## Vision

Scenario design should feel like **theater planning with an expert staff**, not fighting a modal-heavy desktop tool. Human designers retain full authority. The differentiator vs every editor before it: **the tool never lets you ship something broken silently** — the moment a strike can't reach its target on fuel, a patrol zone is empty, or a ferry has no destination, the editor tells you *what* is wrong, *where*, and *how to fix it*, in plain language, before you press play. Every action is machine-readable, replayable, and MCP-addressable so Claude/Cursor can co-author alongside humans.

> **Terminology (locked — GDD §1, ADR-008).** **"Engine"** = deterministic rule code (the **Validation Engine** is pure and reproducible; **no LLM in any blocking path**). **"Agent"** is reserved exclusively for the **Phase 2/3 LLM-driven advisory systems** (Mission Planner, Red Force, Briefing Writer, Balance, Migration) that *propose* diffs and **never** sit in a blocking path. v1 contains no LLM in any gate — this protects the determinism pillar.

> **v1 reality (honesty note).** v1 is **headless / file-based**: a canonical scenario file, a CLI/MCP tool surface, four mission archetypes, a deterministic Validation Engine, and headless sampling. There is **no GUI editor, no map-first drawing surface, and no sides/faction placement UI in v1** — those are Phase 2/3 (see FR §4, Phasing). Aspirational pillars below are retained as advisory/phased scope, not shipped claims.

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

**Out of scope (v1):** GUI/map-first drawing; sides/factions placement UI; operations-timeline UI; mining/mine-clear/cargo missions; NL Mission Planner; CMO import execution; Lua; Steam-Workshop sharing. All retained as Phase 2/3.

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
| **Ferry** | Redeploy aircraft between bases | Destination validity + reachability (**domain shipped; no CLI verb — see AME-8.4**) | P0 |
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
- **AME-3.4** (P1) — **Mission Board** UI (single view by side/type/status, add-mission wizard, clone, template library, flight-plan preview). *Phase 2 — not in v1 headless.*
- **AME-3.5** (P1) — **Operations timeline**: Gantt binding missions to start/end triggers, per-unit priority stack, editor scrub preview. *Phase 2 — `operationsTimeline[]` node reserved in schema.*
- **AME-3.6** (P1) — Mining / mine-clear / cargo-delivery / cargo-transfer archetypes. *Phase 2.*

### 4. Geometry & map authoring (phased)

> **Honesty note (item 14):** v1 has **no map-first drawing surface and no unit-placement / sides-faction geo UI.** The only geometry authored in v1 is **patrol-waypoint lat/lon** via `mission_add_patrol --wp lat,lon`. The requirements below are the target; only AME-4.1 is v1.

- **AME-4.1** (P0) — Patrol geometry as lat/lon waypoint lists in `missions[].patrolZone`; degenerate zones (<3 waypoints) are a blocking validation error. *Shipped.*
- **AME-4.2** (P1) — Map-first placement/move/clone/group of units (air, surface, sub, land, satellite) with immediate local render and commit-on-gesture-end into `orbat`/`referencePoints[]` (GDD §3.8 map-interaction contract). *Phase 2.*
- **AME-4.3** (P1) — Draw/edit reference geometries (polygon, circle, line, corridor); snap/measure tools; invalid-draw stays on screen marked invalid rather than being erased. *Phase 2.*
- **AME-4.4** (P1) — Layer toggles (ORBAT, missions, EMCON, contacts, EW, airspace, mining), minimap + theater bounds, LOD icon density. *Phase 2.*
- **AME-4.5** (P1) — Sides / factions authoring and per-side briefing/posture placement. *Phase 2 — `sides[]` node reserved; no v1 UI.*

### 5. Event & trigger system (typed DSL, no Lua v1)

- **AME-5.1** (P0) — Declarative typed event model (`trigger`, `conditions[]`, `actions[]`), human- and agent-editable, compiled to a deterministic runtime evaluation order.
- **AME-5.2** (P0) — Trigger types: Time, UnitDestroyed, UnitEntersZone, ContactDetected, Variable, MissionComplete, SidePostureChange, ScoreThreshold.
- **AME-5.3** (P0) — Unit-state trigger types (CMO parity): UnitBingoFuel, UnitWinchester, UnitDamaged (threshold), DoctrineChanged.
- **AME-5.4** (P0) — Action types: ActivateMission, DeactivateMission, SpawnUnit, RemoveUnit, SetVariable, Message/Briefing, ChangeDoctrine, SetWeather, **TeleportUnit (edit-test only)**, EndScenario.
- **AME-5.5** (P0) — **Event debugger**: projects the same firing sequence as order-log `EventFired` entries; per event `{eventId, simTick, sequenceId, unmetConditions[], actionResults[]}`; the debugger JSON is a filtered view of the order log, not a second store (doc 17; AC-7). *Shipped (minimal)* — `ExplainEventTrace` / `scenario_event_trace` (**Maturity: stub — minimal trace strings**).
- **AME-5.6** (P1) — Optional Lua compatibility shim over the typed DSL (ADR-014 — deferred; typed DSL is the v1 surface).
- **AME-5.7** (P1) — **Event static analysis**: dead triggers, unreachable states, contradictory conditions, circular dependencies. *Maturity: TCA stub only today (static-analysis hook on editor); full analysis Phase 2 (research §static-analysis).*

> **Maturity note:** events/triggers are a **string stub** in the current headless model — the typed schema and runtime are specified here but the shipped editor stores/handles events at a demonstrative level. Do not read AME-5.2–5.5 as fully shipped.

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

### 7. Concurrency, import, export & versioning

- **AME-7.1** (P0) — **Optimistic concurrency (AC-10):** a mutating tool sends the `editVersion` it read; on mismatch the tool **conflict-rejects** with a `CONFLICT` error carrying the current `editVersion` + file hash so the caller can re-fetch and retry. **Never last-write-wins**; no partial write. *Shipped* — `ScenarioEditVersionGuard` (`ConflictCode = "CONFLICT"`, returns `CurrentEditVersion` + `FileHash`).
- **AME-7.2** (P0) — Canonical JSON is **git-friendly** (stable key ordering; AC-6) with an optional derived binary cache for large scenarios.
- **AME-7.3** (P0) — **Semantic diff** ("Strike Alpha +2 units, Patrol Bravo area moved 20 nm east"). *v1: byte-diffable JSON; semantic summarization Phase 2.*
- **AME-7.4** (P0) — Scenario **schema version** (`schemaVersion`) with automated migrators; distinct from `editVersion`.
- **AME-7.5** (P1) — CMO import pipeline (best-effort): missions, RP, sides, events → Aegis mapping table, documented separately. **Advisory Migration Agent only, never blocking** (ADR-013). *Phase 2/3.*

### 8. Tool surface (CLI / MCP / NL)

- **AME-8.1** (P0) — v1 core MCP/CLI tools (each mutates only the canonical file and re-runs validation; GDD §3.7): `scenario_create`, `scenario_load`, `scenario_save`, `mission_add`, `mission_update`, `mission_delete`, `mission_assign_units`, `reference_point_set`, `event_add`, `event_validate`, `scenario_validate`, `scenario_simulate_sample`, `scenario_export_brief`.
- **AME-8.2** (P0) — CLI and MCP share the same underlying APIs; MCP bindings mirror the CLI (`tools/mission-editor/mcp-tools.json`). No special auto-commit path.
- **AME-8.3** (P0) — **Required verbs per mission type.** Patrol and Strike expose `mission_add_*` + `mission_update_*`; Support and Ferry must expose equivalents.
- **AME-8.4** (P0) — **Ferry CLI/MCP verbs shipped:** `mission_add_ferry` / `mission_update_ferry` expose ferry authoring at the tool surface (was GAP; closed 2026-07-03).
- **AME-8.5** (P0, **GAP**) — **Undo/rollback is in-memory only** in `ScenarioDocumentEditor` and is **not wired to any CLI verb**. Committed-mutation undo/redo (GDD §3.8) is unshipped at the tool surface. **Open implementation gap.**
- **AME-8.6** (P1) — **Natural-language authoring** ("Add a SEAD patrol over Gotland H+0→H+2, then transition fighters to a strike…"). *Phase 2/3.* v1 ships only a demonstrative scaffold — `scenario_ai_scaffold` / `AiAuthoringServices.NlScaffold` (**Maturity: stub**) and a heuristic `mission_plan_suggest`.

> **Shipped CLI verbs (headless, `ProjectAegis.MissionEditor.Cli/Program.cs`):** `scenario_create`, `scenario_validate`, `scenario_publish`, `scenario_ai_scaffold`, `scenario_event_trace`, `scenario_migrate_preview`, `scenario_umpire_snapshot`, `scenario_export_brief`, `scenario_simulate_sample`, `scenario_comms_status`, `scenario_cyber_status`, `scenario_near_future_spawn`, `mission_add_patrol`, `mission_add_strike`, `mission_update_patrol`, `mission_update_strike`, `mission_delete`, `mission_plan_suggest` (~18 verbs). Note the generic `mission_add`/`event_add`/`reference_point_set`/`mission_assign_units` names in AME-8.1 are the GDD contract; the shipped surface uses per-type verbs and lacks ferry verbs (AME-8.4).

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
- **AME-10.5** (P1) — **Event static analysis** — see AME-5.7 (TCA stub today; dead triggers / unreachable states / circular deps are Phase 2).

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

## Implementation Mapping (headless)

| Requirement area | Type / path (`ProjectAegis.Data` unless noted) | Status |
|------------------|-----------------------------------------------|--------|
| Canonical document editor | `Scenario/Authoring/ScenarioDocumentEditor` (missions, live validate, migration preview, umpire hooks) | Shipped (Partial+) |
| Serialization | `Scenario/Authoring/ScenarioDocumentJsonWriter` / `ScenarioDocumentJsonLoader` | Shipped |
| DTOs | `Scenario/Authoring/ScenarioDocumentDto` / `ScenarioMetadataDto` | Shipped |
| Optimistic concurrency | `Scenario/Authoring/ScenarioEditVersionGuard` (`CONFLICT` + editVersion + hash) | Shipped |
| Validation Engine | `Validation/ScenarioValidationEngine` + `Validation/Rules/ValidationRules` | Shipped |
| Reachability formula | `Validation/ReachabilityCalculator` | Shipped |
| Export gate | `Validation/ScenarioValidationExportGate` + `ValidationReport.CanExport` | Shipped |
| Validation config / knobs | `Validation/ValidationConfig` (`assets/data/editor/validation-config.json`) | Shipped |
| Golden determinism hashes | `Validation/ValidationGoldenHashes`, CLI `SimulateSampleGoldenHashes` | Shipped |
| JSON Schema + fixtures | `data/scenarios/scenario-document.schema.json`, `data/scenarios/examples/*.scenario.json` | Shipped |
| CLI / MCP surface | `ProjectAegis.MissionEditor.Cli/Program.cs` (~18 verbs); `tools/mission-editor/mcp-tools.json` | Shipped (Partial — ferry verbs + undo missing) |
| Mission verbs | `MissionAddPatrolCommand`, `MissionAddStrikeCommand`, `MissionUpdatePatrolCommand`, `MissionUpdateStrikeCommand`, `MissionDeleteCommand` | Shipped (no ferry verb — AME-8.4) |
| Headless sample | `ScenarioSimulateSampleCommand` | Shipped |
| Publish + manifest | `ScenarioPublishCommand`, `Scenario/Authoring/ScenarioManifest` | Shipped (Partial+) |
| Umpire / adjudication | `Scenario/Authoring/AdjudicationWorkspace`, `scenario_umpire_snapshot` | Shipped (Partial — no UX) |
| DB migration preview | `Scenario/Authoring/ScenarioDbMigrationPreview`, `scenario_migrate_preview` | Shipped (Partial — no reversible persistence) |
| Event trace / debugger | `ExplainEventTrace`, `scenario_event_trace` | Shipped (Stub) |
| Event static analysis | TCA static-analysis hook on editor | Stub |
| AI scaffold / NL | `Scenario/Authoring/AiAuthoringServices`, `ScenarioAiScaffoldCommand`, `mission_plan_suggest` | Stub (advisory) |
| Model-integrity rules | `IncompatibleHostRule` / `BrokenRefRule` | Shipped (demo/simplistic) |
| Map-first / Mission Board / timeline UI | Unity edit-mode UX | Not started (Phase 2) |
| Authoring agents | Mission Planner / Red Force / Briefing / Balance / Migration | Not started (Phase 2/3) |
| Tests | `src/ProjectAegis.Data.Tests/Scenario/` (`ScenarioDocumentEditorTests`, `ScenarioEditVersionGuardTests`, `ScenarioPackageTests`, live-validation tests) | Shipped (extend for AC-1…12) |

## Acceptance Criteria

Adopted verbatim in substance from the approved GDD (AC-1…AC-12); each is independently, mechanically testable with a defined fixture and observable output, and bound to the AME requirement(s) it verifies.

> **Residual honesty (editor program).** AC checkboxes and any `tests/unit/editor/` (and related integration) paths remain **editor-program residual (S81–S88)**; do not claim all ACs green from headless Partial+ alone.

- [ ] **AC-1 (Logic → AME-6.2, Formulas):** A strike whose target lies beyond `combat_radius_nm * fuel_fraction` makes `scenario_validate` return a blocking error `STRIKE_UNREACHABLE` with message "…out of combat radius by N nm…"; test asserts code + computed `N`. *`tests/unit/editor/`.*
- [ ] **AC-2 (Logic → AME-6.6, AME-6.7):** Given a fixed `metadata.seed` + identical knobs, two independent `scenario_simulate_sample` runs produce (a) byte-identical `fire_order` arrays and (b) identical world-state hash = SHA-256 over canonical post-run state excluding `editorState`; both emit `SEED=<v> HASH=<sha256>`; holds under parallel CI runners. *`tests/integration/editor/determinism/`; ties to replay-verify.*
- [ ] **AC-3 (Logic → AME-6.2):** The Validation Engine flags all six v1 rules, each with its specific error code — AC-3a `MISSION_NO_UNITS`, AC-3b `PATROL_ZONE_DEGENERATE`, AC-3c `STRIKE_NO_TARGETS`, AC-3d `FERRY_NO_DESTINATION`, AC-3e `DB_MISMATCH`, AC-3f `STRIKE_UNREACHABLE`. *Unit test per rule, `tests/unit/editor/validation/`.*
- [ ] **AC-4 (Integration → AME-3.2):** Fixture `doctrine-inheritance.aegis-scenario` (Side A `ROE=WeaponsFree`; a Strike override `ROE=WeaponsTight`; a Patrol with no override) → `scenario_validate` reports resolved Strike `WeaponsTight`, Patrol `WeaponsFree` (inherited). *`tests/integration/editor/doctrine-inheritance/`.*
- [ ] **AC-5 (Integration → AME-8.1):** The core MCP suite creates, validates, and runs a 15-min headless sample of a Strike+Patrol+Support+Ferry scenario with no Unity process spawned; runner exits 0 and emits a `sample-complete` JSON record. *Headless integration test.*
- [ ] **AC-6 (Config → AME-2.5, AME-7.2):** (a) Saving an unedited scenario twice yields byte-identical `scenario.json` (SHA-256 equal). (b) Changing exactly one field yields a `git diff` of exactly one hunk / one JSON key, no reordering. *`tools/ci/smoke-ac6.sh`.*
- [ ] **AC-7 (Logic → AME-5.5):** For an event `E` whose `UnitEntersZone` condition never holds, the debugger JSON contains `{ "event_id": E.id, "fired": false, "last_evaluated_tick": <int>, "unmet_conditions": [ { "type": "UnitEntersZone", "result": false, … } ] }`. *`tests/unit/editor/debugger/` vs `event-no-fire.aegis-scenario`.*
- [ ] **AC-8 (Integration → AME-1.4, AME-2.4):** A scenario authored entirely via headless MCP loads in the Unity host with intact ORBAT/missions/events and `editorState` populated with schema defaults (camera at theater centroid, all layers on). *Integration test.*
- [ ] **AC-9 (Logic → AME-2.4):** The schema lint fails the build if any field other than `editorState` is tagged `derived-only`, or if any sim/Validation-Engine path reads a field under `editorState`. *CI lint test.*
- [ ] **AC-10 (Integration → AME-7.1):** Two mutating MCP calls with a stale `editVersion` → the second returns a `CONFLICT` error carrying current `editVersion` + file hash; no partial write occurs. *Integration test.*
- [ ] **AC-11 (Logic → AME-6.8):** Exporting a scenario containing TeleportUnit actions produces an exported event set with zero TeleportUnit actions + a manifest entry logging each removal; the headless sample's post-transform event set equals the exported set. *Unit test.*
- [ ] **AC-12 (Logic → AME-6.5):** Save succeeds on a scenario with blocking errors; export / play / `scenario_simulate_sample` on the same file are rejected with the blocking error list. *Unit test.*

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
| **v1 (headless)** | Canonical file + schema, Strike/Patrol/Support/Ferry archetypes, typed events (stub), Validation Engine (six rules + extras), determinism contract, editVersion concurrency, headless sample, core CLI/MCP, publish/umpire/migration-preview (partial) | **Shipped (Partial+)** — gaps: ferry verbs (AME-8.4), undo wiring (AME-8.5), event maturity (AME-5.x), live-validation UX (AME-6.9) |
| **Phase 2** | Unity edit-mode UX (map-first, Mission Board, operations timeline), mining/cargo archetypes, NL Mission Planner, CMO import (best-effort), visual event graph + full static analysis, reversible migration persistence | Not started |
| **Phase 3** | Full event DSL + optional Lua shim, Red Force Agent, collaborative review, Workshop-style sharing | Not started |

## Open Questions / Decisions Needed

Each former open question now points to its ADR.

| # | Question | ADR | Status / Recommendation |
|---|----------|-----|-------------------------|
| Q1 | Legal/policy stance on **CMO scenario import** (feasibility vs licensing) | ADR-013 | **Proposed** — best-effort, advisory Migration Agent only, never blocking; confirm licensing before build |
| Q2 | **Lua compatibility**: full `ScenEdit_*` shim vs curated subset | ADR-014 | **Accepted** — typed DSL is v1; **no Lua v1**; optional shim deferred to Phase 3 |
| Q3 | Should **agent-authored scenarios** be labeled for transparency? | ADR-015 | **Proposed — recommend yes**; store in `metadata`/provenance (affects Phase 2/3 agents only) |
| Q4 | Maximum **event-graph complexity** before warnings (soft cap) | ADR-016 | **Accepted** — soft complexity + tick-density warnings; hard cap 32 conditions/event; never blocks export; finalize thresholds at perf budgeting |
| Q5 | Editor **inside game client** only, or standalone **Scenario Lab** sharing the core library | ADR-017 | **Proposed** — v1 is headless/file-based; topology decision pending |

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
| GDD `agentic-mission-editor.md` | Authoritative content source: terminology, determinism, AC-1…AC-12 |
| CMO Manual §3.3.17 / §4.1.5 / §5 / §7.1–7.3 | Parity baseline (clean-room, observable only) |

---

**References**

- [CMO Manual (PDF)](https://www.matrixgames.com/amazon/PDF/CMO/CMO_manual_EBOOK.pdf) — §3.3.17 Mission Editor, §4.1.5 Scenario Editor, §5 ScenEdit, §7.1–7.3 Missions and Reference Points
- [Command Lua Documentation](https://commandlua.github.io/) — reference only; no Lua in v1 (ADR-014)
- [CMO Scenario Editor Manual Addendum](https://command.matrixgames.com/?page_id=2709)
- [scenario-editor-research.md](../../docs/research/scenario-editor-research.md) — umpire/adjudication, migration, publish governance, live validation, static analysis

---

**Status:** Revised — implementation-aligned. v1 headless stack shipped (Partial+); design gate APPROVED (review log 2026-06-01); open implementation gaps tracked: ferry tool surface (AME-8.4), undo wiring (AME-8.5), event-model + static-analysis maturity (AME-5.x), live-validation UX (AME-6.9), reversible migration persistence (AME-10.2).

---

**Charter mechanical honesty Wave 4 2026-07-08** (doc 11 residual still editor-owned).
