# Scenario document authoring (`*.scenario.json`)

Reference for authoring the ADR-008 scenario **document** format — the `*.scenario.json`
files the headless [Mission Editor CLI](mission-editor-cli.md) creates, edits, validates,
and publishes. A document is the *authoring artifact* (metadata + order of battle + missions
+ events) that a designer builds up; it maps 1:1 onto
[`ScenarioDocumentDto`](../../src/ProjectAegis.Data/Scenario/Authoring/ScenarioDocumentDto.cs).

> **Not the same as sim *policy* files.** `data/scenarios/*.policy.json`
> ([scenario-policy-authoring.md](scenario-policy-authoring.md), `ScenarioPolicyJsonDto`) is
> **sim-runtime** config — ROE, engagement, EMCON, detection trials. A scenario *document*
> (this guide, `ScenarioDocumentDto`) is an **authoring** artifact. A document *references* a
> policy by id via `metadata.policyId`, but the two files have different shapes and loaders.
> Do not conflate them.

The canonical machine contract is
[`data/scenarios/scenario-document.schema.json`](../../data/scenarios/scenario-document.schema.json)
(JSON Schema draft 2020-12). This page is the human-facing companion: the load/save model, the
edit lifecycle, the field reference, per-mission-type rules, and the validation-finding catalog.

---

## How documents are loaded & saved

Source:
[`ScenarioDocumentJsonLoader`](../../src/ProjectAegis.Data/Scenario/Authoring/ScenarioDocumentJsonLoader.cs),
[`ScenarioDocumentJsonWriter`](../../src/ProjectAegis.Data/Scenario/Authoring/ScenarioDocumentJsonWriter.cs),
[`ScenarioDocumentEditor`](../../src/ProjectAegis.Data/Scenario/Authoring/ScenarioDocumentEditor.cs).

- **Reading is tolerant.** The loader uses `PropertyNameCaseInsensitive = true`,
  `ReadCommentHandling = Skip`, and `AllowTrailingCommas = true`, so hand-authored files may use
  any casing and include `//` comments and trailing commas. A file that does not deserialize to a
  `ScenarioDocumentDto` throws `InvalidDataException` naming the path.
- **Writing is canonical.** The writer emits **camelCase**, indented, with
  `DefaultIgnoreCondition = Never` — so *every* DTO property is always serialized: nullable fields
  become `null` and empty collections become `[]`. Editor-produced files therefore have a stable,
  fully-populated shape (this is what the schema's `required` lists reflect).
- **Hand-authored files may omit optional fields.** Because the schema only *requires* the
  always-emitted set, a file that leaves out optional metadata (`title`, `description`, `author`,
  `schemaVersion`) still validates and loads — the committed examples do exactly this.
- **The editor is the mutation seam.** `ScenarioDocumentEditor.Load(path)` /
  `.CreateNew(...)` → mutate → `.Save(path)`. All CLI mission verbs go through it, so undo,
  edit-version guarding, and canonical serialization apply uniformly.

Minimal valid file (metadata + an empty mission list are the only required top-level members):

```json
{
  "metadata": {
    "dbRef": "baltic_patrol",
    "dbSnapshotId": null,
    "editVersion": 1,
    "seed": 42,
    "policyId": "baltic-patrol-catalog",
    "tlBranch": "TL-0",
    "unitReadiness": null,
    "maxTechnologyLevel": 2,
    "nearFutureUnits": null,
    "sideRoe": null
  },
  "missions": []
}
```

`scenario_create --out P` writes a document like this (defaults `dbRef=baltic_patrol`,
`seed=42`, `policyId=baltic-patrol-catalog`, `tlBranch=TL-0` = the catalog default,
`editVersion=1`, no missions). The canonical writer additionally emits the optional metadata
fields (`title`, `description`, `author` as `null`; `schemaVersion` as `1`), which the
hand-authored examples omit. See the
[Mission Editor CLI reference](mission-editor-cli.md#scenario-lifecycle).

---

## Edit lifecycle: version, undo, export gate

Every **mutating** operation on a document is version-guarded and undoable, and export/publish
is validation-gated.

- **Optimistic concurrency (`editVersion`).**
  [`ScenarioEditVersionGuard`](../../src/ProjectAegis.Data/Scenario/Authoring/ScenarioEditVersionGuard.cs)
  compares the caller's expected `editVersion` against the file's current value. A mismatch throws
  `ScenarioEditConflictException` (`code: "CONFLICT"`, CLI exit `3`) carrying the
  `currentEditVersion` and `fileHash` so the caller can re-read and retry. `CreateNew()` seeds
  `editVersion = 1`; each successful `CommitMutation()` increments it by one.
- **Undo is disk-backed and cross-process.**
  [`ScenarioUndoStackStore`](../../src/ProjectAegis.Data/Scenario/Authoring/ScenarioUndoStackStore.cs)
  persists a JSON sidecar `<scenarioPath>.undo-stack.json`, so `scenario_undo` works across
  separate CLI invocations. Snapshots clone **all** canonical sections (metadata, sides, orbat,
  reference points, operations timeline, missions, events, variables, editor state) — a partial
  snapshot would silently reset untouched data on undo.
- **Export/publish is gated on validation.** `scenario_export`, `scenario_export_brief`, and
  `scenario_publish` run the validator first and refuse to emit output while any blocking
  (`Error`) finding remains. See the finding catalog below.

For the CLI-side read-modify-write loop and exit-code contract, see
[mission-editor-cli.md](mission-editor-cli.md#optimistic-concurrency----edit-version). For the
**in-process** object model that wraps this same pipeline for interactive hosts
(`ScenarioAuthoringSession` / `ScenarioEditCommandBus` + the presenter view models), see
[scenario-authoring-host.md](scenario-authoring-host.md).

---

## Top-level structure

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `metadata` | object | **yes** | Scenario-wide binding & tuning (see below). |
| `missions` | array | **yes** | Mission list; may be empty `[]`. |
| `features` | object \| null | no | Feature toggles (`realismMagazines` default `true`, `maxTimeCompression` default `256`). |
| `sides` | array | no | Side definitions (`id`, `name`, `defaultRoe?`, `defaultEmcon?`, `postures[]`). |
| `orbat` | object \| null | no | Order of battle: `units[]` (`id`, `sideId`, `platformId`, `lat`, `lon`, `parentUnitId?`, `roeOverride?`, `emconOverride?`) and `bases[]`. |
| `referencePoints` | array | no | Named points/zones (`id`, `type`, `geometry[]`, `radiusNm?`). |
| `operationsTimeline` | array | no | `{ missionId, activateAtTick }` activation entries. |
| `events` | array \| null | no | Typed event DSL (AME-5.x); see [Events](#events). |
| `variables` | object \| null | no | String→string scenario variable store. |
| `editorState` | object \| null | no | **Derived-only** UI state (camera, layers). Never read by the validation engine — do not encode authoritative data here. |

### `metadata`

| Field | Type | Default | Notes |
|-------|------|---------|-------|
| `title` / `description` / `author` | string \| null | `null` | Free text; optional. |
| `schemaVersion` | int | `1` | Authoring schema version. |
| `dbRef` | string \| null | `null` | Catalog/database reference bound at load (e.g. `baltic_patrol`). Must resolve to an available snapshot (`DB_MISMATCH` otherwise). |
| `dbSnapshotId` | string \| null | `null` | Optional immutable snapshot id of the bound DB. |
| `editVersion` | int | `1` (via `CreateNew`) | Optimistic-concurrency counter (see above). |
| `seed` | int (ulong) | `42` | Scenario RNG root for headless sample runs. |
| `policyId` | string \| null | `null` | Id of a `data/scenarios/*.policy.json` used by `scenario_simulate_sample`. |
| `tlBranch` | string \| null | catalog default | Technology-level branch `TL-0`…`TL-5`, validated **at load**, not mid-tick. Required by the validator. |
| `unitReadiness` | map \| null | `null` | Per-unit `{ readyForLaunch: bool }` (req 16); drives `AIR_NOT_READY` for Strike units. |
| `maxTechnologyLevel` | int | `2` | Near-future gate ceiling (req 09). |
| `nearFutureUnits` | array \| null | `null` | `{ archetypeId, unitId }` spawn rows for CCA / attritable runtime (req 09). |
| `sideRoe` | string \| null | `null` | Side-level ROE default (AME-3.2). `null` resolves to `WeaponsFree`. |

---

## Missions

Every mission carries the same property set; the `type` discriminator selects which fields are
meaningful (the writer still emits the rest as `[]`/`null`). Valid `type` values are
`Patrol`, `Strike`, `Ferry`, `Support` (PascalCase, as written by the editor).

| Field | Type | Meaningful for | Notes |
|-------|------|----------------|-------|
| `id` | string | all | Unique within the document. |
| `type` | string | all | `Patrol` \| `Strike` \| `Ferry` \| `Support`. |
| `assignedUnitIds` | string[] | all | ≥1 required by the validator (`MISSION_NO_UNITS`). |
| `targetIds` | string[] | Strike | ≥1 required for Strike (`STRIKE_NO_TARGETS`). |
| `ferryDestinationBaseId` | string \| null | Ferry | Required for Ferry (`FERRY_NO_DESTINATION`). |
| `patrolZone` | waypoint[] | Patrol, Support | `{ lat, lon }` vertices; ≥3 required (`PATROL_ZONE_DEGENERATE`). Support stores its station geometry here. |
| `stationGeometry` | waypoint[] \| null | Support | Mirror of the station geometry; `null` for non-Support. |
| `supportRole` | string \| null | Support | `Tanker` \| `AEW` \| `EW`. |
| `roeOverride` | string \| null | all | Mission-level ROE override (AME-3.2); `null` inherits `sideRoe` (else `WeaponsFree`). |
| `emconOverride` | string \| null | all | Mission-level EMCON override; `null` inherits. |

**Per-type minimum content** (enforced by the schema `allOf` branches *and* the validation
engine):

| Type | Must have |
|------|-----------|
| `Patrol` | ≥1 assigned unit, ≥3 `patrolZone` waypoints |
| `Strike` | ≥1 assigned unit, ≥1 `targetId` (and reachable — see reachability findings) |
| `Ferry` | ≥1 assigned unit, non-empty `ferryDestinationBaseId` (and reachable) |
| `Support` | ≥1 assigned unit, a `supportRole`, ≥3 `patrolZone` waypoints (station) |

## Events

Optional typed event entries (`events[]`) each declare `id`, `triggerType`, `conditions[]`, and
`actions[]`:

```json
{
  "id": "evt-no-fire",
  "triggerType": "Time",
  "conditions": [
    { "type": "UnitEntersZone", "unitId": "u1", "zoneId": "zone-alpha", "result": null }
  ],
  "actions": [ { "type": "Message" } ]
}
```

- **Conditions** carry `type` plus optional `unitId` / `zoneId` / `result` (a boolean debugger
  hint). **Actions** carry `type` plus optional `unitId` / `lat` / `lon`.
- The event graph is bounded by the complexity rule (ADR-016): soft `Warning` findings for high
  graph complexity and peak-tick density, and one **hard** blocking cap of **32 conditions per
  event** (`EVENT_CONDITION_CAP_EXCEEDED`).
- Static analysis (dead triggers, unreachable `ActivateMission` actions, contradictory conditions,
  cycles) and the single-event `scenario_event_trace` debugger are their own subsystem — see
  [scenario-event-system.md](scenario-event-system.md).

---

## Validation findings

`scenario_validate` (and the export gate) runs
[`ScenarioValidationEngine`](../../src/ProjectAegis.Data/Validation/ScenarioValidationEngine.cs)
and returns a [`ValidationReport`](../../src/ProjectAegis.Data/Validation/ValidationReport.cs):
`passed` (no `Error`), a deterministically-sorted `findings[]`, and a SHA-256 `reportHash`.
`canExport` is false whenever any finding is at or above the `Error` floor.

| Code | Severity | Meaning |
|------|----------|---------|
| `TL_BRANCH_MISSING` | Error | `metadata.tlBranch` is empty. |
| `TL_BRANCH_INVALID` | Error | `tlBranch` is not `TL-0`…`TL-5`. |
| `TL_BRANCH_SNAPSHOT_MISMATCH` | Error | `tlBranch` ≠ the bound snapshot's branch. |
| `TL_RELEASE_TRAIN_NOT_FOUND` | Error | No catalog snapshot in the release train for `tlBranch`. |
| `TL_RELEASE_TRAIN_MISMATCH` | Error | Explicit DB binding resolves to a different snapshot than the `tlBranch` release train expects. |
| `DB_MISMATCH` | Error | `dbRef`/`dbSnapshotId` does not resolve to an available snapshot. |
| `MISSION_NO_UNITS` | Error | Mission has no assigned units. |
| `PATROL_ZONE_DEGENERATE` | Error | Patrol mission has <3 waypoints. |
| `STRIKE_NO_TARGETS` | Error | Strike mission has no targets. |
| `FERRY_NO_DESTINATION` | Error | Ferry mission has no destination base. |
| `AIR_NOT_READY` | Error | A Strike-assigned unit is marked `readyForLaunch: false`. |
| `STRIKE_INVALID_PLATFORM` | Error | Strike unit has an invalid `combat_radius_nm`. |
| `STRIKE_UNREACHABLE` / `STRIKE_UNREACHABLE_FUEL` | Error | Target beyond combat radius / fuel range (great-circle distance vs. combat radius + ingress/egress pad ÷ fuel fraction). |
| `FERRY_UNREACHABLE` / `FERRY_UNREACHABLE_FUEL` | Error | Ferry destination beyond combat radius / fuel range. |
| `INCOMPATIBLE_HOST` | Error | Model-integrity check for incompatible host relationships. |
| `BROKEN_REF` | Error | A mission references a `ref:`-prefixed id that resolves to nothing. |
| `EVENT_CONDITION_CAP_EXCEEDED` | Error | An event exceeds the 32-condition hard cap (ADR-016). |
| `EVENT_GRAPH_COMPLEXITY_HIGH` | Warning | Event-graph complexity over the soft threshold — does **not** block export. |
| `EVENT_GRAPH_PEAK_TICK_DENSITY_HIGH` | Warning | Peak tick density over the soft threshold — does **not** block export. |
| `EVENT_DEAD_TRIGGER` | Warning | Event has zero conditions and `triggerType` ≠ `Time` (can never fire). |
| `EVENT_UNREACHABLE_ACTION` | Warning | An `ActivateMission` action has no mission id, or targets a mission not in `missions[]`. |
| `EVENT_CONTRADICTORY` | Warning | The same event has both a `result: true` and a `result: false` condition. |
| `EVENT_CIRCULAR` | Warning | Event participates in an `ActivateMission → MissionComplete` cycle. |
| `DOCTRINE_RESOLVED` | Info | Reports the resolved ROE per mission and its inheritance source (`override`/`side`). |

Reachability and event thresholds come from
[`ValidationConfig`](../../src/ProjectAegis.Data/Validation/ValidationConfig.cs) — defaults:
ingress/egress pad `50` nm, fuel fraction `0.85`, complexity warn `400`, density warn `20`,
cross-ref weight `2`, max conditions per event `32`.

---

## Worked examples

Four committed, schema-conformant examples live in
[`data/scenarios/examples/`](../../data/scenarios/examples/) and are asserted against the schema
in CI by
[`ScenarioDocumentSchemaConformanceTests`](../../src/ProjectAegis.Data.Tests/Scenario/ScenarioDocumentSchemaConformanceTests.cs)
(which also validates live `ScenarioDocumentEditor` output, so DTO drift fails the build):

| Example | Shows |
|---------|-------|
| [`baltic-patrol.scenario.json`](../../data/scenarios/examples/baltic-patrol.scenario.json) | A single Patrol mission with a 3-waypoint zone and per-unit readiness. |
| [`strike-package.scenario.json`](../../data/scenarios/examples/strike-package.scenario.json) | A two-flight Strike with multiple targets on a `TL-0` branch. |
| [`ferry-redeploy.scenario.json`](../../data/scenarios/examples/ferry-redeploy.scenario.json) | A Ferry mission with a destination base. |
| [`event-no-fire.scenario.json`](../../data/scenarios/examples/event-no-fire.scenario.json) | An empty mission list plus a typed event with a condition and action. |

Author a Strike inline (ids resolve against the bound catalog):

```json
{
  "id": "strike-1",
  "type": "Strike",
  "assignedUnitIds": ["strike-flight-1", "strike-flight-2"],
  "targetIds": ["target-kaliningrad-sam", "target-baltiysk-pier"],
  "ferryDestinationBaseId": null,
  "patrolZone": [],
  "roeOverride": null,
  "supportRole": null
}
```

---

## Constraints & pitfalls

- **`editorState` is derived-only.** It is never read by the validation engine — never store
  authoritative scenario data there.
- **Stale `--edit-version` → `CONFLICT` (exit 3).** Re-read `currentEditVersion` from the error
  details (or reload the file) and retry; don't retry blindly.
- **Ids resolve against the bound catalog.** `dbRef` must resolve, and reachability rules read
  unit/target positions and combat radius from that catalog — unresolved ids are skipped by
  reachability (no false positives) but broken bindings surface as `DB_MISMATCH`.
- **Warnings never block; only `Error` does.** Event-graph soft warnings are informational; the
  32-condition cap is the only hard event limit.
- **Keep authoring documents out of the sim-policy path.** A document's `policyId` points at a
  `*.policy.json`; editing the *document* never changes replay goldens, but editing the
  *referenced policy* can — see the [replay hash invariant](determinism-and-replay.md).

---

## Related docs

| Where | What |
|-------|------|
| [`scenario-document.schema.json`](../../data/scenarios/scenario-document.schema.json) | Canonical machine contract (draft 2020-12). |
| [scenario-policy-authoring.md](scenario-policy-authoring.md) | The sibling `*.policy.json` sim-policy format. |
| [scenario-event-system.md](scenario-event-system.md) | The event graph subsystem: static analysis (`EVENT_*` codes above), the `scenario_event_trace` debugger, fire order, and the event CLI verbs. |
| [mission-editor-cli.md](mission-editor-cli.md) | Headless CLI/MCP verbs that create, edit, validate, simulate & publish documents. |
| [determinism-and-replay.md](determinism-and-replay.md) | Replay hashing & the golden-fixture workflow. |
| [`ProjectAegis.Data` README](../../src/ProjectAegis.Data/README.md) | The assembly that owns the DTOs, loader/writer, undo store, and validation engine. |
| [`docs/architecture/`](../architecture/) | ADR-008 (mission validation), ADR-016 (event-graph complexity caps). |
| [`AGENTS.md`](../../AGENTS.md#hard-invariants--never-break-these) | Hard invariants — replay hash, Baltic v3 isolation. |
