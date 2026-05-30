# GDD: Agentic Mission & Scenario Editor

> **Status:** Draft (for design review)
> **Created:** 2026-05-30
> **System:** 11 (`design/gdd/systems-index.md`)
> **Concept:** `design/gdd/agentic-mission-editor-concept.md`
> **Requirement:** `Game-Requirements/requirements/11-Agentic-Mission-Editor.md`
> **Depends on:** Platform Database (4), Mission Runtime (9), Simulation Core (1), Policy/ROE/EMCON (3), Order Log & Replay (2)

---

## 1. Overview

The Agentic Mission & Scenario Editor is the human- and machine-facing authoring surface for Project Aegis scenarios. Its architecture is an **intent compiler**: a single canonical declarative scenario file is the source of truth, and every authoring path — map drawing, natural language, and MCP tool calls — emits the *same* canonical objects. Validation, determinism, git-diffability, headless generation, and AI co-authoring are therefore structural properties of the file format rather than features layered on top.

v1 ships the canonical-file foundation, a map ORBAT view, four mission archetypes (Strike, Patrol, Support, Ferry) with policy inheritance, a typed declarative event system, save/load, a blocking **Validation Agent**, and the core MCP tool suite. Operations-timeline polish, planner/red-force agents, deep NL, CMO import, and Lua are deferred (Phase 2/3).

## 2. Player Fantasy

You are the **theater designer and staff lead**. You sketch intent on a map — "patrol Gotland, strike the amphib group at H+2" — and an expert staff drafts the ORBAT, missions, and triggers as reviewable proposals. You correct, you approve, you never fight a modal-heavy desktop tool. When something is wrong, the tool *tells you why and how to fix it* before you ever press play. The scenario you build is a plain, diffable artifact you can version, share, and replay with confidence that it will behave identically every run.

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
  - `editorState` (**derived-only**: camera, layer toggles, last-validation cache)
- **Serialization rule:** stable key ordering, fixed numeric formatting, LF newlines → human-readable git diffs.

### 3.3 The load-bearing invariant

> No authoring front-end may hold private state that the simulation or validation engine reads. `editorState` is derived-only and is never an input to sim or validation. Map drawing emits into `referencePoints[]`; NL and MCP emit the same mission/event/ORBAT objects.

Any feature that violates this is rejected in review — it silently breaks determinism, diffability, and headless parity at once.

### 3.4 Mission archetypes (v1)

| Type | Required fields | Validation rules (v1) |
|---|---|---|
| **Strike** | targets[], assigned units, weapon behavior, optional TOT/TOS | ≥1 target; targets in DB; strike radius reachable on fuel |
| **Patrol** | patrol zone (polygon/circle), variant (ASW/ASuW/AAW/SEAD/CAS), optional prosecution zone | non-empty zone; ≥1 assigned unit; investigate/engage flags set |
| **Support** | role (Tanker/AEW/EW), station geometry | role-appropriate platform; station within theater bounds |
| **Ferry** | destination base | destination exists and is friendly/owned |

All types support **doctrine / ROE / EMCON inheritance** from parent unit with explicit override (req 13 / system 3). Inheritance is resolved at validation time and surfaced in the UI as a parent→child chain.

### 3.5 Event system (typed DSL, no Lua in v1)

- **Trigger types (P0):** Time, UnitDestroyed, UnitEntersZone, ContactDetected, Variable, MissionComplete, SidePostureChange, ScoreThreshold.
- **Action types (P0):** ActivateMission, DeactivateMission, SpawnUnit, RemoveUnit, SetVariable, Message/Briefing, ChangeDoctrine, SetWeather, TeleportUnit (edit-test only), EndScenario.
- Events compile to a deterministic runtime evaluation order (§4.2). The event debugger logs firing order, skipped conditions, and action results; exportable to JSON.

### 3.6 Agents (v1 = Validation only)

- **Validation Agent (P0-blocking):** runs all rules; export is blocked on any error-severity finding. Catches at minimum: empty patrol area, strike with no targets, ferry without destination, DB version mismatch, unreachable target on fuel, mission with no assigned units.
- Agents **propose** changes as preview diffs; nothing commits without explicit accept (configurable auto-accept in headless CI).
- Every agent edit records: prompt, rationale, diff hash, approving user/agent id (provenance, req §6).
- Planner / Red Force / Briefing / Balance / Migration agents are Phase 2/3.

### 3.7 MCP tool suite (v1 core)

`scenario_create`, `scenario_load`, `scenario_save`, `mission_add`, `mission_update`, `mission_delete`, `mission_assign_units`, `reference_point_set`, `event_add`, `event_validate`, `scenario_validate`, `scenario_simulate_sample`, `scenario_export_brief`. Each tool mutates only the canonical file and re-runs validation.

## 4. Formulas

### 4.1 Strike fuel-reachability check

Used by the Validation Agent to flag unreachable strikes.

```
required_range_nm = haversine(launch_base, target) * 2 * RT + ingress_egress_pad_nm
reachable = (combat_radius_nm * fuel_fraction) >= required_range_nm
```

- `haversine(a,b)` — great-circle distance in nautical miles between two lat/lon points.
- `RT` — round-trip multiplier. `RT = 1` if a Support/Tanker mission covers the route (recovery elsewhere), else `RT = 1` (distance already ×2 for return); set `RT = 0.5` only for one-way (e.g. ferry-to-strike). **Range:** {0.5, 1}. Default 1.
- `ingress_egress_pad_nm` — reserve for routing/loiter. **Range:** 20–150. Default 50.
- `combat_radius_nm` — from Platform DB (system 4) for the assigned airframe.
- `fuel_fraction` — usable fraction after reserves. **Range:** 0.7–0.95. Default 0.85.

**Example:** base→target = 280 nm, `RT=1`, pad = 50 → `required = 280*2*... ` wait — distance is one-way 280, round trip = 560, `+50 = 610`. Airframe `combat_radius_nm = 700`, `fuel_fraction = 0.85` → available `= 595`. `595 >= 610` → **false → flagged unreachable**, fix suggestion: "assign tanker (Support mission) or reduce strike radius by ≥15 nm."

> Note: `combat_radius_nm` already encodes a round-trip combat radius in the DB convention; the editor uses the *raw one-way distance ×2* form above and treats `combat_radius_nm` as the one-way reach budget. The GDD-review action item is to confirm which convention the Platform DB stores and collapse to one. (Flagged in §5 / §8.)

### 4.2 Deterministic event evaluation order

Events must fire in a reproducible order independent of authoring order or hash-map iteration.

```
sort_key(event) = (trigger_time_resolved, event.priority, event.id)
fire_order = stable_sort(active_events, by = sort_key ascending)
```

- `trigger_time_resolved` — absolute sim tick the trigger became satisfiable (for non-time triggers, the tick the condition first evaluated true).
- `event.priority` — integer designer field. **Range:** 0–1000. Default 100. Lower fires first.
- `event.id` — stable unique string; final tiebreaker guarantees total ordering.

This makes acceptance criterion #7 (event debugger explains why an event did/didn't fire) and the determinism pillar enforceable: same file + seed → identical `fire_order`.

### 4.3 Event-graph complexity warning (soft cap)

```
complexity = E + sum(conditions_per_event) + C * cross_refs
warn if complexity > WARN_THRESHOLD
```

- `E` — event count. `cross_refs` — events referencing missions/variables set by other events. `C` — cross-ref weight, default 2.
- `WARN_THRESHOLD` — **Range:** 200–1000. Default 400. Soft warning only; never blocks export (doc open Q4 recommendation).

## 5. Edge Cases

| Case | Behavior |
|---|---|
| Mission with zero assigned units | Validation error (blocking). Message: "Mission *X* has no assigned units — assign ORBAT or delete the mission." |
| Patrol zone polygon with <3 vertices / zero area | Validation error. "Patrol zone for *X* is degenerate — redraw with ≥3 vertices." |
| Strike target not present in bound DB | Validation error. "Target *T* not found in DB *dbRef* — re-add from catalog or rebind DB." |
| DB version mismatch (file `dbRef` ≠ available DB) | Blocking error on load *and* export. Offer shallow/deep rebind (req 06). Never silently auto-migrate. |
| Two events with identical trigger time + priority + (impossible) id collision | id is enforced unique on add; collision is a load-time integrity error, not a runtime ambiguity. |
| `editorState` present in a headless-authored file | Ignored by sim/validation entirely; preserved on round-trip so UI state survives. |
| TeleportUnit action used in a non-edit (play) scenario | Stripped at export with a warning: "TeleportUnit is edit-test only — removed from shipped scenario." |
| NL/MCP emits a mission referencing a unit not in `orbat` | Validation error; the emit succeeds (file is editable-invalid) but export is blocked. |
| Concurrent MCP edits to same scenario | Last-write-wins on the file with an optimistic version token in `metadata.schemaVersion` + edit hash; conflicting token → tool returns conflict error, no partial write. |
| Ferry destination is an enemy or destroyed base | Validation error. "Ferry destination *B* is not a valid friendly recovery base." |
| Time trigger set before scenario start time | Validation warning; event fires at T+0. |
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

| Knob | Range | Default | Affects |
|---|---|---|---|
| `ingress_egress_pad_nm` | 20–150 | 50 | Strictness of strike-reachability flags |
| `fuel_fraction` | 0.70–0.95 | 0.85 | Conservatism of fuel validation |
| `event.priority` (per event) | 0–1000 | 100 | Event firing order within a tick |
| `WARN_THRESHOLD` (event complexity) | 200–1000 | 400 | When the perf warning appears |
| `C` cross-ref weight | 1–4 | 2 | Sensitivity of complexity score to coupling |
| Headless sample length | 1–60 min | 15 | `scenario_simulate_sample` duration |
| Auto-accept agent edits (headless) | on/off | off | Whether CI commits agent proposals without review |
| Validation severity floor for export block | error / warning | error | How strict the export gate is |

## 8. Acceptance Criteria

Testable; map to req §Acceptance Criteria and `tests/` evidence.

1. **AC-1 (Logic):** Given a scenario with a strike whose `required_range_nm > available`, `scenario_validate` returns a blocking error with a fix suggestion. *Unit test, `tests/unit/editor/`.*
2. **AC-2 (Logic):** Given identical scenario file + seed, two independent `scenario_simulate_sample` runs produce an identical event `fire_order` and world-state hash. *Determinism unit/integration test; ties to replay-verify.*
3. **AC-3 (Logic):** Validation Agent flags all of: empty patrol area, strike with no targets, ferry without destination, DB version mismatch, mission with no assigned units. *Unit test per rule.*
4. **AC-4 (Integration):** All four v1 mission types are assignable with doctrine/ROE/EMCON inheritance and per-mission override, resolved through system 3. *Integration test, `tests/integration/editor/`.*
5. **AC-5 (Integration):** The core MCP tool suite can create, validate, and run a 15-minute headless sample of a Strike+Patrol+Support+Ferry scenario without opening Unity. *Headless integration test.*
6. **AC-6 (Config):** Saving the same scenario twice with no edits produces byte-identical `scenario.json`; a single-field change produces a minimal, human-readable git diff. *Smoke check.*
7. **AC-7 (Logic):** For a scenario with an event that did not fire, the event debugger reports the specific unmet condition and the tick it was last evaluated. *Unit test on debugger output.*
8. **AC-8 (Integration):** A scenario authored entirely via headless MCP (no UI) loads in the Unity host with intact ORBAT, missions, events, and derived `editorState` defaults. *Integration test.*

### GDD-review action items (do not block authoring, resolve before implementation)

- Confirm the Platform DB's `combat_radius_nm` convention (one-way vs round-trip) and collapse §4.1 to a single form. *Owner: Platform DB (4).*
- Confirm doc open Q3 (label agent-authored scenarios in multiplayer/briefing) — recommended **yes**, store in `metadata`/provenance.
- Confirm doc open Q4 final `WARN_THRESHOLD` during perf budgeting against the 5,000-unit ORBAT NFR.

---

## Changelog

| Date | Change |
|---|---|
| 2026-05-30 | Initial GDD from concept; intent-compiler spine, 4 mission types, typed event DSL, Validation Agent, core MCP, fuel + event-order formulas |
