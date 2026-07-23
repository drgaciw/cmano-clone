# Scenario event system — model, static analysis, debugger & fire order

Operational reference for the **scenario event graph** as it behaves at *authoring time*: how
events are modelled, how the **static analyzer** flags bad graphs, how the single-event
**debugger trace** projects a fire/no-fire result, how the deterministic **fire order** is
computed, and the CLI/MCP verbs and headless view model that surface it all.

The subsystem is engine-free and lives under
[`ProjectAegis.Data/Scenario/Authoring/`](../../src/ProjectAegis.Data/Scenario/Authoring/):
[`EventStaticAnalyzer`](../../src/ProjectAegis.Data/Scenario/Authoring/EventStaticAnalyzer.cs),
[`EventDebuggerTrace`](../../src/ProjectAegis.Data/Scenario/Authoring/EventDebuggerTrace.cs), and
[`EventFireOrderCalculator`](../../src/ProjectAegis.Data/Scenario/Authoring/EventFireOrderCalculator.cs).
It is driven by CLI verbs in
[`EventCommands.cs`](../../src/ProjectAegis.MissionEditor.Cli/EventCommands.cs) /
[`ScenarioEventTraceCommand.cs`](../../src/ProjectAegis.MissionEditor.Cli/ScenarioEventTraceCommand.cs)
and rendered headlessly by the Unity-adapter
[`EventGraphPresenter`](../../src/ProjectAegis.Delegation.UnityAdapter/Authoring/EventGraphPresenter.cs).

> **Authoring-time, not sim runtime.** Everything here evaluates the *static document* — there is
> no live `ISimWorldSnapshot`. The debugger deliberately reports most conditions as *unmet* because
> it has no unit positions or contacts (see [Debugger trace](#debugger-trace-scenario_event_trace)).
> The *what-fires-when* of a running scenario is decided by the sim tick pipeline, not by this
> subsystem. For the JSON document shape (the `events[]` block itself) see
> [`scenario-document-authoring.md`](scenario-document-authoring.md#events).

---

## The event model

Each entry in a scenario document's `events[]` is a
[`ScenarioEventDto`](../../src/ProjectAegis.Data/Scenario/Authoring/ScenarioDocumentDto.cs):

| Field | Type | Meaning |
|-------|------|---------|
| `id` | string | Stable event id (case-insensitive; drives ordering and graph nodes). |
| `triggerType` | string | e.g. `Time`, `MissionComplete`, `UnitDestroyed`, `UnitEntersZone`. |
| `conditions[]` | condition | Each carries `type` + optional `unitId` / `zoneId` / `result` (a nullable boolean debugger hint). |
| `actions[]` | action | Each carries `type` + optional `unitId` / `lat` / `lon`. |

Two conventions the tooling relies on:

- **`ActivateMission` actions carry the mission id in `unitId`.** An action of `type =
  "ActivateMission"` uses its `unitId` field as the *mission id* it activates — the analyzer and the
  graph presenter both read it that way.
- **`result` is a static hint, not sim truth.** `result: true` forces a condition to "hold" in the
  authoring debugger; `result: false` forces it unmet; `null` (default) means "resolve normally"
  (which, without sim state, is unmet for everything except `UnitEntersZone` backed by editor state).

---

## Static analysis (`EventStaticAnalyzer`)

`EventStaticAnalyzer.Analyze(document)` is a **pure** pass over the event graph. It is wired into
[`ScenarioValidationEngine`](../../src/ProjectAegis.Data/Validation/ScenarioValidationEngine.cs)
(`findings.AddRange(EventStaticAnalyzer.Analyze(scenario))`), so **every `scenario_validate` run and
the export gate emit these codes**. They are all `Warning` severity — they surface in the report but
do **not** block export at the default `Error` floor.

| Code | When |
|------|------|
| `EVENT_DEAD_TRIGGER` | Event has **zero conditions** and `triggerType` is not `Time` — it can never fire. |
| `EVENT_UNREACHABLE_ACTION` | An `ActivateMission` action is missing its mission id, or targets a mission id not present in `missions[]`. |
| `EVENT_CONTRADICTORY` | The same event has both a `result: true` and a `result: false` condition. |
| `EVENT_CIRCULAR` | The event participates in an `ActivateMission → MissionComplete` cycle (see below). |

**Cycle detection.** The analyzer builds a directed graph: edge `A → B` when `A` has an
`ActivateMission(unitId = M)` action and `B` listens for mission `M` (its `triggerType` is
`MissionComplete` with a resolvable `unitId`, or it has a `MissionComplete` *condition* with
`unitId = M`). It runs **Tarjan SCC** and flags every node in a component of size > 1, or a singleton
with a self-loop. Findings are ordered deterministically by `code`, then event id, then message, so
the report hash is stable for a given document order.

> These four codes are **separate** from the complexity rule
> ([`ADR-016`](../architecture/adr-016-event-graph-complexity-caps.md), implemented by
> `EventGraphComplexityRule`), which adds the soft `EVENT_GRAPH_COMPLEXITY_HIGH` /
> `EVENT_GRAPH_PEAK_TICK_DENSITY_HIGH` warnings and the one **hard** blocking cap,
> `EVENT_CONDITION_CAP_EXCEEDED` (> 32 conditions per event). See the validation-findings table in
> [`scenario-document-authoring.md`](scenario-document-authoring.md#validation-findings).

---

## Debugger trace (`scenario_event_trace`)

`EventDebuggerTrace.Evaluate(document, eventId, horizon?)` evaluates **one** event against the static
document and returns an order-log-aligned projection (AC-7). The CLI verb
`scenario_event_trace --path <file> [--event ID]` delegates to it via
`ScenarioDocumentEditor.ExplainEventTrace`.

Projection JSON (snake_case; nulls omitted):

| Field | Meaning |
|-------|---------|
| `event_id` | The evaluated event id. |
| `fired` | Whether the event would fire under authoring-time rules (see below). |
| `last_evaluated_tick` | `0` when fired, else the evaluation horizon (default **32** ticks). |
| `sim_tick` | `0` when fired, else equals `last_evaluated_tick`. |
| `sequence_id` | Index of the event among `events[]` **ordered by id ordinal** (stable), or `0` if missing. |
| `unmet_conditions[]` | Per-condition `{type, result, unit_id, zone_id, note?}` for each condition that did not hold. |
| `action_results[]` | Per-action `{type, applied, note?}` — `applied = fired`; `note = "not-fired"` when the event did not fire. |

**Fire rule.** With conditions present, `fired` is true only when *all* conditions hold. With **no**
conditions, `fired` is true only when `triggerType == "Time"` (mirrors the `EVENT_DEAD_TRIGGER`
rule).

**Condition evaluation (no live sim).**

1. `result: false` → unmet.
2. `type == "UnitEntersZone"` → holds only if the document's `editorState.unitZonePresence` map
   places `unitId` in `zoneId`; otherwise unmet with `note = "no-sim-state"`.
3. `result: true` → holds.
4. Anything else (`ContactDetected`, `Variable`, …) → unmet with `note = "no-sim-state"`, because
   the authoring document carries no unit positions or contacts.

This is why the canonical AC-7 example event `evt-no-fire` reports `fired: false` — its
`UnitEntersZone` condition cannot hold without sim state. (`scenario_event_trace` defaults `--event`
to `evt-no-fire` and, when the path is missing, traces an in-memory default document.)

> A second, older projection —
> [`EventDebuggerJsonProjection`](../../src/ProjectAegis.Data/Scenario/Authoring/EventDebuggerJsonProjection.cs)
> — emits a camelCase `{eventId, fired, lastEvaluatedTick, unmetConditions[]}` shape. The active
> CLI/graph path uses `EventDebuggerTrace` (snake_case, order-log-aligned); prefer it.

---

## Fire order (`EventFireOrderCalculator`)

`EventFireOrderCalculator.ComputeFireOrder(events)` returns the deterministic authoring-time fire
order (GDD §4.2):

- `Time`-triggered events rank **0**; every other trigger ranks `int.MaxValue`.
- Ties break by **event id, ordinal**.

So `Time` events lead (in id order), then all other events (in id order). The order is returned in
the `event_add` / `event_update` response (`fireOrder`) and is consumed by
`scenario_simulate_sample` to compare authored order against the harness.

---

## Headless event-graph view model (`EventGraphPresenter`)

The Unity-adapter [`EventGraphPresenter`](../../src/ProjectAegis.Delegation.UnityAdapter/Authoring/EventGraphPresenter.cs)
is the engine-free view model behind the Event Graph editor panel. It **does not** depend on
`UnityEngine` or touch `DelegationBridge`. On `Refresh()` it produces, from the session document:

- **Nodes** — one per event, ordered by id ordinal: `{eventId, triggerType, sequenceId, lastFired}`
  (`lastFired` from `EventDebuggerTrace.Evaluate(...).Fired`).
- **Edges** — deterministic order, two kinds:
  - `FireOrder`: consecutive events in id-ordinal order.
  - `ActivateMission`: `A → B` when `A` activates mission `M` and `B` listens for `MissionComplete M`.
- **`StaticAnalysisCodes`** — the `EVENT_*` codes from `EventStaticAnalyzer`, ordinal-sorted.
- **`ExplainSelected()`** — AC-7 debugger JSON for the selected event.

---

## CLI / MCP verbs

All four are wired into the CLI dispatch
([`Program.cs`](../../src/ProjectAegis.MissionEditor.Cli/Program.cs)) **and** the MCP tools manifest
(pinned by `McpToolsManifestTests`). The three mutating verbs require `--edit-version` and go through
the edit-version / undo lifecycle documented in
[`scenario-document-authoring.md`](scenario-document-authoring.md#edit-lifecycle-version-undo-export-gate).

| Verb | Required flags | Notes |
|------|----------------|-------|
| `event_add` | `--path`, `--edit-version`, `--id`, `--trigger` | `[--condition Type[:UnitId[:ZoneId]]]+ [--action Type[:UnitId]]+`. Upserts by id. Response includes `conditionCount`, `actionCount`, `fireOrder`, `editVersion`, `fileHash`. |
| `event_update` | (same as `event_add`) | Alias of `event_add` — same upsert path. |
| `event_delete` | `--path`, `--edit-version`, `--id` | `EVENT_NOT_FOUND` if the id is absent. |
| `scenario_event_trace` | `--path` | `[--event ID]` (default `evt-no-fire`); read-only AC-7 debugger JSON. |

Condition/action tokens accept either `:` or `,` as the separator (`ParseCondition` / `ParseAction`).
Missing-flag errors go to **stderr** and return exit `1`; the JSON envelope is on **stdout**.

```bash
# Add a Time-triggered event that activates a mission, then trace it:
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- event_add \
  --path scenario.json --edit-version 3 --id evt-open --trigger Time \
  --action ActivateMission:strike-1
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_event_trace \
  --path scenario.json --event evt-open
```

---

## Determinism

The whole subsystem is deterministic given the same document (a hard invariant for the validation
report hash and replay):

- Static findings, graph nodes/edges, and fire order are all sorted by **ordinal** keys (id, then
  code/message) — never by hash-set iteration order.
- The debugger horizon is a fixed default (`32` ticks); it reads no clock and no RNG.
- `sequence_id` is the id-ordinal index, so it is stable across reorderings of the raw `events[]`.

---

## Common pitfalls

- **The debugger almost always reports `fired: false`.** That is expected — it has no live sim state.
  Use `result: true` on a condition (or seed `editorState.unitZonePresence` for `UnitEntersZone`) to
  force a hold when you want to exercise the fired path.
- **`ActivateMission` targets go in `unitId`, not a `missionId` field.** A blank or unknown target
  raises `EVENT_UNREACHABLE_ACTION`.
- **Static `EVENT_*` codes are warnings — they never block export.** Only the ADR-016 hard cap
  (`EVENT_CONDITION_CAP_EXCEEDED`, > 32 conditions) blocks. Do not expect `scenario_export` to fail
  on a dead trigger or a cycle.
- **A zero-condition event only fires on `Time`.** Any other zero-condition trigger is a dead trigger.

---

## See also

| Topic | Doc |
|-------|-----|
| The `events[]` document format + the full validation-findings catalog | [`scenario-document-authoring.md`](scenario-document-authoring.md) |
| Full Mission Editor CLI verb reference | [`mission-editor-cli.md`](mission-editor-cli.md) |
| Event-graph complexity policy (soft warnings + hard 32-condition cap) | [`ADR-016`](../architecture/adr-016-event-graph-complexity-caps.md) |
| Validation engine (ADR-008) and report hashing | [`ADR-008`](../architecture/adr-008-mission-editor-validation-engine.md) |
| Determinism rules (clock, ordering, hashing) | [`determinism-and-replay.md`](determinism-and-replay.md) |
