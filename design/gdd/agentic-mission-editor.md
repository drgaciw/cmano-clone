# GDD: Agentic Mission & Scenario Editor

> **Status:** Draft (for design review)
> **Created:** 2026-05-30
> **System:** 11 (`design/gdd/systems-index.md`)
> **Concept:** `design/gdd/agentic-mission-editor-concept.md`
> **Requirement:** `Game-Requirements/requirements/11-Agentic-Mission-Editor.md`
> **Depends on:** Platform Database (4), Mission Runtime (9), Simulation Core (1), Policy/ROE/EMCON (3), Order Log & Replay (2), Database Intelligence Pipeline (20)

---

## 1. Overview

The Agentic Mission & Scenario Editor is the human- and machine-facing authoring surface for Project Aegis scenarios. Its architecture is an **intent compiler**: a single canonical declarative scenario file is the source of truth, and every authoring path — map drawing, natural language, and MCP tool calls — emits the *same* canonical objects. Validation, determinism, git-diffability, headless generation, and AI co-authoring are therefore structural properties of the file format rather than features layered on top.

v1 ships the canonical-file foundation, a map ORBAT view, four mission archetypes (Strike, Patrol, Support, Ferry) with policy inheritance, a typed declarative event system, save/load, a blocking **Validation Engine**, and the core MCP tool suite. Operations-timeline polish, the LLM-driven authoring **agents** (Mission Planner, Red Force, Briefing Writer, Balance), deep NL, CMO import, and Lua are deferred (Phase 2/3).

> **Terminology:** "Engine" = deterministic rule code (the Validation Engine is pure, reproducible, no LLM). "Agent" is reserved exclusively for the Phase 2/3 LLM-driven systems. v1 contains **no** LLM in any blocking path — this protects the determinism pillar.

## 2. Player Fantasy

### v1 fantasy (what ships)

You are the **theater designer**. You build the scenario directly — place the ORBAT, draw patrol zones, lay missions and triggers on a map-first surface, never fighting a modal-heavy desktop tool. The difference from every editor you've used: **the tool never lets you ship something broken silently.** The moment a strike can't reach its target on fuel, a patrol zone is empty, or a ferry has no destination, the editor tells you *what* is wrong, *where*, and *how to fix it* — in plain language, before you ever press play. The scenario is a plain, diffable artifact you can version, share, and replay with confidence it behaves identically every run.

### Phase 2/3 north star (vision, not v1)

Later, an expert AI staff drafts the ORBAT, missions, and triggers as reviewable proposals from a natural-language brief — you correct and approve rather than assemble from scratch. v1 deliberately ships the *trustworthy foundation* (deterministic validation + canonical file) that those agents will build on. The drafting-staff experience is the roadmap, not a v1 promise.

## 3. Detailed Rules

### 3.1 Editor modes

1. **Play mode** — scenario features locked; matches CMO play behavior.
2. **Edit mode** — full authoring; optional auto-pause / low-speed live test.
3. **Headless edit mode** — CLI/MCP-only authoring; no UI; for CI and batch agent generation.

All three operate on the identical canonical file. Headless and UI are the same code path with different front-ends.

### 3.2 Canonical scenario file (`*.aegis-scenario`)

- ZIP package: `manifest.json`, `scenario.json` (canonical), optional `cache.bin` (derived, never authoritative).
- `scenario.json` schema (refines doc §Data Model):
  - `metadata` (title, description, author, `schemaVersion`, `dbRef`)
  - `features` (magazines, realism toggles, time-compression limits)
  - `sides[]` (briefing, doctrine defaults, score, `postures[]`)
  - `orbat` (units, groups, bases, cargo)
  - `referencePoints[]` (typed geometry: point, polygon, circle, line, corridor)
  - `missions[]` (typed: Strike | Patrol | Support | Ferry | Mining | MineClear | CargoDelivery | CargoTransfer)
  - `operationsTimeline[]` (scheduled mission transitions)
  - `events[]` (`trigger`, `conditions[]`, `actions[]`)
  - `variables{}`
  - `editorState` (**derived-only, non-authoritative**: camera, layer toggles. **No validation result is stored here.** See §3.3.)
- **Serialization rule:** stable key ordering, fixed numeric formatting, LF newlines → human-readable git diffs.

### 3.3 The load-bearing invariant

> No authoring front-end may hold private state that the simulation or validation engine reads. `editorState` is derived-only and is never an input to sim or validation. Map drawing emits into `referencePoints[]`; NL and MCP emit the same mission/event/ORBAT objects.

Any feature that violates this is rejected in review — it silently breaks determinism, diffability, and headless parity at once.

**Enforcement (not just a social contract):**
- A schema lint (CI gate) asserts `editorState` is the *only* node tagged `derived-only` and that no field under it is referenced by the sim or Validation Engine. Build fails otherwise (see AC-9).
- **No cached validation result is authoritative.** Export, play, and `scenario_simulate_sample` always re-run the Validation Engine against the current canonical file. A pass cannot be "remembered" across edits. This closes the stale-cache export-bypass hole.

### 3.4 Mission archetypes (v1)

| Type | Required fields | Validation rules (v1) |
|---|---|---|
| **Strike** | targets[], assigned units, weapon behavior, optional TOT/TOS | ≥1 target; targets in DB; each target fuel-reachable from the assigned unit's base per §4.1 (`reachable == true`) |
| **Patrol** | patrol zone (polygon/circle), variant (ASW/ASuW/AAW/SEAD/CAS), optional prosecution zone | non-empty zone; ≥1 assigned unit; investigate/engage flags set |
| **Support** | role (Tanker/AEW/EW), station geometry | role-appropriate platform; station within theater bounds |
| **Ferry** | destination base | destination exists and is friendly/owned |

All types support **doctrine / ROE / EMCON inheritance** from parent unit with explicit override (req 13 / system 3). Inheritance is resolved at validation time and surfaced in the UI as a parent→child chain.

### 3.5 Event system (typed DSL, no Lua in v1)

- **Trigger types (P0):** Time, UnitDestroyed, UnitEntersZone, ContactDetected, Variable, MissionComplete, SidePostureChange, ScoreThreshold.
- **Unit-state trigger types (P0, required for CMO parity):** UnitBingoFuel, UnitWinchester (out of a named weapon), UnitDamaged (crosses a damage threshold), DoctrineChanged. Without these, parity scenarios are forced into opaque `Variable` workarounds.
- **Action types (P0):** ActivateMission, DeactivateMission, SpawnUnit, RemoveUnit, SetVariable, Message/Briefing, ChangeDoctrine, SetWeather, TeleportUnit (edit-test only), EndScenario.
- Events compile to a deterministic runtime evaluation order (§4.2). The event debugger logs firing order, skipped conditions, and action results; exportable to JSON.

### 3.6 Validation Engine (v1) + agents (Phase 2/3)

- **Validation Engine (P0-blocking, deterministic — no LLM):** a pure rule engine over the canonical file. Same file in → same findings out, every run. At default settings it blocks export on any **error-severity** finding (the severity floor is a tuning knob, §7 — when set to `warning`, warnings also block). It catches **all six** v1 rules exhaustively (this is a coverage guarantee a deterministic engine can make and an LLM cannot):
  1. Mission with no assigned units
  2. Empty / degenerate patrol area
  3. Strike with no targets
  4. Ferry without a valid destination
  5. DB version mismatch (`dbRef` ≠ available DB)
  6. Strike target not fuel-reachable (§4.1)
- **Phase 2/3 agents (LLM-driven):** Mission Planner, Red Force, Briefing Writer, Balance, Migration. These **propose** changes as preview diffs; nothing commits without explicit accept (configurable auto-accept in headless CI). Every agent edit records: prompt, rationale, diff hash, approving user/agent id (provenance, req §6). They are advisory and never sit in a blocking path — the deterministic Validation Engine remains the sole export gate.

### 3.7 MCP tool suite (v1 core)

`scenario_create`, `scenario_load`, `scenario_save`, `mission_add`, `mission_update`, `mission_delete`, `mission_assign_units`, `reference_point_set`, `event_add`, `event_validate`, `scenario_validate`, `scenario_simulate_sample`, `scenario_export_brief`. Each tool mutates only the canonical file and re-runs validation.

**Concurrency & isolation:**
- **`scenario_simulate_sample` runs in an isolated sim-world instance** with no mutable state shared between calls (no global event queue, no shared `variables{}`). Determinism is owned by Simulation Core (system 1, seeded fixed tick); the editor guarantees only that it constructs a fresh world per call. This makes AC-2 reproducible even under parallel CI runners.
- **Optimistic concurrency for mutating tools:** `metadata.editVersion` is a monotonic integer bumped on every committed write (distinct from `schemaVersion`, which changes only on format migration). A mutating tool sends the `editVersion` it read; if it no longer matches, the tool **rejects with a conflict error** containing the current `editVersion` and file hash so the caller can re-fetch and retry. Policy is **conflict-reject, not last-write-wins** — correct for a determinism-first tool; no silent discards.

### 3.8 Map interaction contract (feel)

The intent-compiler spine must not make the map feel like a form. Rules that preserve CMO-like tactility:

- **Immediate local render.** A drawn geometry or moved unit renders instantly in the editor view as a *tentative* overlay; the commit into `referencePoints[]` / `orbat` happens on gesture-end (mouse-up / confirm). The screen never waits on a file round-trip.
- **Undo/redo** operates on committed canonical mutations (one undo = one committed edit), scoped per scenario session.
- **Invalid-draw handling.** If the Validation Engine flags a just-drawn object (e.g. degenerate patrol zone), the object **stays on screen** marked invalid (red outline + inline reason) rather than being erased — the designer fixes it in place. Invalid objects persist in the canonical file (editable-invalid state) and only block *export*, never *editing*.
- Direct manipulation (right-click unit → assign mission) emits the same canonical objects as NL/MCP — the front-end differs, the result does not.

## 4. Formulas

### 4.1 Strike fuel-reachability check

Used by the Validation Engine to flag unreachable strikes.

**DB convention (locked):** Platform DB (system 4) stores `combat_radius_nm` as the standard military **combat radius** — the maximum *one-way* distance a unit can fly out, deliver ordnance, and return to base with reserves. The round trip is therefore *already accounted for* in the DB value; the formula compares the one-way distance to target against it and must **not** double the distance.

```
range_to_target_nm = haversine(launch_base, target) + ingress_egress_pad_nm
available_radius_nm = combat_radius_nm * fuel_fraction
reachable          = available_radius_nm >= range_to_target_nm
```

- `haversine(a,b)` — great-circle distance in nautical miles between two lat/lon points (≥ 0).
- `ingress_egress_pad_nm` — extra one-way reach consumed by non-direct routing/loiter, expressed against the combat-radius budget. **Range:** 20–150. Default 50.
- `combat_radius_nm` — combat radius from Platform DB (system 4) for the assigned airframe. **Must be > 0**; a unit with `combat_radius_nm ≤ 0` (e.g. a non-air unit assigned to a Strike) is a validation error, not a reachability `false`.
- `fuel_fraction` — usable fraction of combat radius after planning reserves. **Range:** 0.70–0.95. Default 0.85. (0.85 ≈ a conservative bingo-fuel reserve; 0.95 = aggressive, 0.70 = cautious doctrine.)

**Input validation (run before the formula):** reject `fuel_fraction` outside [0.70, 0.95] and `ingress_egress_pad_nm` outside [20, 150] as configuration errors; reject `combat_radius_nm ≤ 0` as an ORBAT error. This prevents degenerate passes (e.g. a zero pad + 0.95 fraction is allowed; a negative/zero radius is not).

**Example:** base→target = 280 nm, pad = 50 → `range_to_target = 330`. Airframe `combat_radius_nm = 450`, `fuel_fraction = 0.85` → `available_radius = 382.5`. `382.5 >= 330` → **reachable = true.** With a longer-legged target at 400 nm: `range = 450`, `available = 382.5` → **false → flagged**, fix suggestion: "Strike *N* target out of combat radius by 68 nm — assign a tanker (Support mission) or move the launch base ≥68 nm closer."

**Edge — co-located target:** `haversine = 0` → `range = pad`. Reachable for any valid airframe, but a zero-distance strike is flagged as a **warning** (likely an authoring error), see §5.

### 4.2 Deterministic event evaluation order

Events must fire in a reproducible order independent of authoring order or hash-map iteration.

```
sort_key(event) = (trigger_time_resolved ASC, event.priority ASC, event.id ASC)
fire_order      = stable_sort(active_events, by = sort_key)   // ascending on each component
```

- `trigger_time_resolved` — absolute sim tick the trigger became satisfiable (for non-time triggers, the tick the condition first evaluated true).
- `event.priority` — integer designer field. **Range:** 0–1000. Default 100. **Lower fires first** (ascending).
- `event.id` — stable unique string (enforced unique on add); lexicographic final tiebreaker → `sort_key` is a **total order**, no unbroken ties.

**Same-tick resolution semantics (locked):** within a single tick, events fire **sequentially in `fire_order`**, and each event's actions are applied **before** the next event's conditions are evaluated. I.e. event B (later in `fire_order`) sees the post-action state of event A. This is deterministic because `fire_order` is a total order. Designers control intra-tick dependencies via `priority`.

`fire_order` is exported as an ordered array of `event.id` strings (the structure AC-2 / AC-7 assert against). Same file + seed → identical `fire_order`, enforcing the determinism pillar.

### 4.3 Event-graph complexity warning (soft cap)

```
complexity         = E + sum(conditions_per_event) + C * cross_refs
peak_tick_density  = max over all ticks of (events with trigger_time_resolved == that tick)
warn if complexity > WARN_THRESHOLD  OR  peak_tick_density > DENSITY_THRESHOLD
```

| Variable | Meaning | Range / Default |
|---|---|---|
| `E` | total event count | ≥ 0 |
| `conditions_per_event` | per-event condition count; **hard cap 32 per event** (validation error above) | 0–32 |
| `cross_refs` | number of **reference edges**: each event→{mission or variable set by another event} link counts once (per-edge, not per-pair) | ≥ 0 |
| `C` | cross-ref weight | 1–4, default 2 |
| `WARN_THRESHOLD` | complexity soft cap | 200–1000, default 400 |
| `DENSITY_THRESHOLD` | max events firing in one tick before warning | 10–50, default 20 |

Both are **soft warnings only** — never block export (doc open Q4). `peak_tick_density` protects the deterministic evaluation loop and frame budget against the pathological "400 events all at T+0" case that the topology-only score misses. The 32-condition hard cap prevents a single event from exceeding `WARN_THRESHOLD` alone.

## 5. Edge Cases

| Case | Behavior |
|---|---|
| Mission with zero assigned units | Validation error (blocking). Message: "Mission *X* has no assigned units — assign ORBAT or delete the mission." |
| Patrol zone polygon with <3 vertices / zero area | Validation error. "Patrol zone for *X* is degenerate — redraw with ≥3 vertices." |
| Strike target not present in bound DB | Validation error. "Target *T* not found in DB *dbRef* — re-add from catalog or rebind DB." |
| DB version mismatch (file `dbRef` ≠ available DB) | Blocking error on load *and* export. Offer shallow/deep rebind (req 06). Never silently auto-migrate. |
| Two events with identical trigger time + priority + (impossible) id collision | id is enforced unique on add; collision is a load-time integrity error, not a runtime ambiguity. |
| `editorState` present in a headless-authored file | Ignored by sim/validation entirely; preserved on round-trip so UI state survives. |
| TeleportUnit action present at export | Export runs an **explicit, logged transform** that removes all TeleportUnit actions and records each removal in the export manifest (not a silent strip). The headless sample and the exported scenario therefore share an *identical post-transform* event set — preserving the "identical code path" guarantee. UI badges TeleportUnit actions "edit-test only" persistently. (AC-11) |
| NL/MCP emits a mission referencing a unit not in `orbat` | Validation error; the emit succeeds (file is editable-invalid) but export is blocked. |
| Concurrent MCP edits to same scenario | Optimistic concurrency on `metadata.editVersion` (monotonic int, separate from `schemaVersion`). A mutating tool sends the `editVersion` it read; mismatch → **conflict-reject** (no partial write). The conflict error returns the current `editVersion` + file hash so the caller re-fetches and retries. Never last-write-wins. (AC-10) |
| Ferry destination is an enemy or destroyed base | Validation error. "Ferry destination *B* is not a valid friendly recovery base." |
| Time trigger set before scenario start time | Validation **warning** (surfaced, not silently corrected): "Event *E* triggers before scenario start — it will fire at T+0; intended H-relative time?" Event fires at T+0. |
| Zero-distance strike (target co-located with base) | Validation **warning** — reachable but likely an authoring error (§4.1). |
| Event references a mission deleted after the event was authored | Validation error (dangling reference). Fix suggestion names the missing mission id. |
| Save with unresolved blocking errors | Save **is allowed** (work-in-progress persists); **export / play / headless-sample is blocked**. Save and export are distinct gates. |

## 6. Dependencies

Bidirectional — partner systems must reference this editor in their own docs.

- **Platform Database (4):** all units/targets resolved through the DB; `dbRef` pins the snapshot. *DB doc must note the editor as a consumer requiring a stable snapshot id and reachability fields (`combat_radius_nm`).*
- **Mission Runtime (9):** the editor's `missions[]`/`events[]` are executed by the runtime; the canonical schema is the contract. *Runtime doc must reference the editor as the producer of mission/event objects.*
- **Simulation Core & Time (1):** headless sample (`scenario_simulate_sample`) drives the sim; determinism guarantees come from the core's seeded fixed tick. *Core doc must note editor headless sampling as a consumer of `World.SetTime`/seeded stepping.*
- **Policy/ROE/EMCON/WRA (3):** doctrine inheritance resolved against the policy evaluator; editor sets per-side/per-mission defaults. *Policy doc must reference the editor as the authoring source of scenario policy ids.*
- **Order Log & Replay (2):** quick-run/playtest evidence and the event debugger draw on the order log; editor displays the deterministic seed for bug reports. *Order-log doc must note the editor as a reader for AAR/quick-run summaries.*
- **Database Intelligence Pipeline (20):** "add unit" search validates against the catalog.

## 7. Tuning Knobs

All knobs are data-driven (see CLAUDE.md coding standard) and live in `assets/data/editor/validation-config.json` unless noted.

| Knob | Range | Default | Affects |
|---|---|---|---|
| `ingress_egress_pad_nm` | 20–150 | 50 | Strictness of strike-reachability flags (§4.1) |
| `fuel_fraction` | 0.70–0.95 | 0.85 | Conservatism of fuel validation (§4.1) |
| `event.priority` (per event, in scenario file) | 0–1000 | 100 | Event firing order within a tick (§4.2) |
| `WARN_THRESHOLD` (event complexity) | 200–1000 | 400 | When the complexity perf warning appears (§4.3) |
| `DENSITY_THRESHOLD` (events/tick) | 10–50 | 20 | When the tick-density perf warning appears (§4.3) |
| `C` cross-ref weight | 1–4 | 2 | Sensitivity of complexity score to coupling (§4.3) |
| Headless sample length | 1–60 min | 15 | `scenario_simulate_sample` duration |
| Validation severity floor for export block | error / warning | error | How strict the export gate is (§3.6) |

> Note: there is no "auto-accept agent edits" knob in v1 — v1 has no authoring agents. It returns in Phase 2/3 alongside the LLM agents.

## 8. Acceptance Criteria

Each AC is independently, mechanically testable with a defined fixture and observable output.

1. **AC-1 (Logic):** Given a strike whose target lies beyond `combat_radius_nm * fuel_fraction` (per §4.1), `scenario_validate` returns a blocking error whose code is `STRIKE_UNREACHABLE` and whose message matches the form "…out of combat radius by N nm…". Test asserts the error code and the computed `N`. *Unit test, `tests/unit/editor/`.*
2. **AC-2 (Logic):** Given a scenario with a fixed `metadata.seed` and identical tuning-knob values, two independent `scenario_simulate_sample` runs produce (a) byte-identical `fire_order` arrays (ordered list of `event.id` strings, §4.2) and (b) an identical **world-state hash** = SHA-256 over the canonical serialization of post-run world state (all fields except `editorState`). Both runs emit `SEED=<v> HASH=<sha256>` to stdout. Holds under parallel CI runners (isolation, §3.7). *Determinism integration test, `tests/integration/editor/determinism/`; ties to replay-verify.*
3. **AC-3 (Logic):** The Validation Engine flags **all six** v1 rules (§3.6), each verified by a dedicated test: AC-3a no-units, AC-3b empty patrol, AC-3c strike-no-targets, AC-3d ferry-no-destination, AC-3e DB-mismatch, AC-3f strike-unreachable. Each must produce its specific error code. *Unit test per rule, `tests/unit/editor/validation/`.*
4. **AC-4 (Integration):** Given fixture `doctrine-inheritance.aegis-scenario` (Side A `ROE=WeaponsFree`; a Strike with override `ROE=WeaponsTight`; a Patrol with no override), `scenario_validate` reports resolved Strike `ROE=WeaponsTight` and Patrol `ROE=WeaponsFree` (inherited). *Integration test, `tests/integration/editor/doctrine-inheritance/`.*
5. **AC-5 (Integration):** The core MCP suite creates, validates, and runs a 15-min headless sample of a Strike+Patrol+Support+Ferry scenario with no Unity process spawned; the runner exits 0 and emits a `sample-complete` JSON record. *Headless integration test.*
6. **AC-6 (Config):** (a) Saving an unedited scenario twice yields byte-identical `scenario.json` (SHA-256 equal). (b) Changing exactly one field yields a `git diff` of exactly one hunk touching exactly one JSON key, with no key reordering. *Smoke check, `tools/ci/smoke-ac6.sh`.*
7. **AC-7 (Logic):** For an event `E` whose `UnitEntersZone` condition never holds, the debugger JSON export contains `{ "event_id": E.id, "fired": false, "last_evaluated_tick": <int>, "unmet_conditions": [ { "type": "UnitEntersZone", "result": false, … } ] }`. Test asserts schema + values against `event-no-fire.aegis-scenario`. *Unit test, `tests/unit/editor/debugger/`.*
8. **AC-8 (Integration):** A scenario authored entirely via headless MCP loads in the Unity host with intact ORBAT/missions/events and `editorState` populated with schema defaults (camera at theater centroid, all layers on). *Integration test.*
9. **AC-9 (Logic):** The schema lint fails the build if any field other than `editorState` is tagged `derived-only`, or if any sim/Validation-Engine code path reads a field under `editorState` (§3.3 invariant enforcement). *CI lint test.*
10. **AC-10 (Integration):** Two mutating MCP calls with a stale `editVersion` → the second returns a `CONFLICT` error carrying the current `editVersion` + file hash; no partial write occurs (§3.7, §5). *Integration test.*
11. **AC-11 (Logic):** Exporting a scenario containing TeleportUnit actions produces an exported event set with zero TeleportUnit actions and a manifest entry logging each removal; the headless sample's post-transform event set equals the exported set (§5). *Unit test.*
12. **AC-12 (Logic):** Save succeeds on a scenario with blocking errors; export / play / `scenario_simulate_sample` on the same file are rejected with the blocking error list (save-vs-export gate, §5). *Unit test.*

### GDD-review action items (resolve before implementation, non-blocking to authoring)

- ~~Platform DB `combat_radius_nm` convention~~ — **resolved: round-trip combat radius** (§4.1 locked). Platform DB (4) owner to confirm the DB field matches this on schema authoring.
- Confirm doc open Q3 (label agent-authored scenarios in multiplayer/briefing) — recommended **yes**, store in `metadata`/provenance. (Affects Phase 2/3 agents only.)
- Confirm final `WARN_THRESHOLD` / `DENSITY_THRESHOLD` during perf budgeting against the 5,000-unit ORBAT NFR.

---

## Changelog

| Date | Change |
|---|---|
| 2026-05-30 | Major revision (design-review verdict): Validation **Engine** (deterministic, renamed from Agent); §2 fantasy rescoped to v1; §4.1 fuel formula rewritten round-trip + input validation; determinism contract (world-state hash, fire_order, sim isolation); concurrency `editVersion`/conflict-reject; §3.3 invariant enforced (no cached validation); §3.8 map-interaction contract; unit-state triggers; tick-density warning; AC-1..AC-12 rewritten testable |
| 2026-05-30 | Initial GDD from concept; intent-compiler spine, 4 mission types, typed event DSL, core MCP, fuel + event-order formulas |
