# Scenario authoring host (session, command bus, presenters)

The **in-process object model** that drives interactive scenario editing on top of a
`*.scenario.json` document. Where the [Mission Editor CLI](mission-editor-cli.md) is a
*one-shot* read-modify-write process, the authoring host is a *long-lived* object graph that a
Unity `EditorWindow` (or a test) opens once and mutates repeatedly through a single serialized
pipeline. It is engine-agnostic — nothing here references `UnityEngine` — so the same view
models are exercised headless in CI.

> **Relationship to the document guide.** This page covers the *runtime host* —
> `ScenarioAuthoringSession`, `ScenarioEditCommandBus`, and the headless presenter view models.
> The *document format* (fields, per-mission-type rules, validation-finding catalog) and the
> conceptual edit lifecycle live in
> [scenario-document-authoring.md](scenario-document-authoring.md); the *CLI verbs* that expose
> the same mutations one-shot are in [mission-editor-cli.md](mission-editor-cli.md); the *event
> graph* presenter is detailed in [scenario-event-system.md](scenario-event-system.md).

---

## Layering

```
Unity EditorWindow / tests            (host UI — not in this repo's core)
        │  drive
        ▼
Presenters (view models)              src/ProjectAegis.Delegation.UnityAdapter/Authoring/
  MissionBoardPresenter · MapAuthoringSurface · SelectionInspectorModel
  EditModeController · LiveFindingsPresenter · EventGraphPresenter
        │  mutate only via
        ▼
ScenarioEditCommandBus                src/ProjectAegis.Data/Scenario/Authoring/
  (serialized mutate pipeline → ScenarioMutationResult)
        │  wraps
        ▼
ScenarioAuthoringSession              (file-backed lifecycle: path, dirty, editVersion)
        │  owns
        ▼
ScenarioDocumentEditor                (in-memory ScenarioDocumentDto + mutations)
        │  persists via
        ▼
ScenarioUndoStackStore  +  <scenarioPath>.undo-stack.json  +  <scenarioPath>
```

Two hard directional rules hold at every layer:

- **Presenters never touch the document directly.** They stage intent (filters, selection,
  tentative gestures) and commit *only* through `session.Bus`. The one read they do perform is
  `session.Editor.ToDto()` to rebuild their view models — never a write.
- **`EditorState` is derived-only.** `ScenarioAuthoringSession.EditorState`
  (camera/layers) must never be fed to the Validation Engine or a headless sim entry point. It
  is UI scratch, not authoritative scenario data.

---

## `ScenarioAuthoringSession`

File-backed session — the unit of "a scenario is open".
[`ScenarioAuthoringSession`](../../src/ProjectAegis.Data/Scenario/Authoring/ScenarioAuthoringSession.cs)

| Member | Meaning |
|--------|---------|
| `Open(path)` | Loads an existing document into a new session. Throws `FileNotFoundException` if missing. |
| `Editor` | The owned [`ScenarioDocumentEditor`](../../src/ProjectAegis.Data/Scenario/Authoring/ScenarioDocumentEditor.cs) (in-memory document + mutations). |
| `Bus` | The [`ScenarioEditCommandBus`](../../src/ProjectAegis.Data/Scenario/Authoring/ScenarioEditCommandBus.cs) — the *only* supported mutation path. |
| `EditVersion` | Current optimistic-concurrency token (`= Editor.Metadata.EditVersion`). Pass this to every bus call. |
| `IsDirty` | True when in-memory state has diverged from the last successful `Save()`. Set by the bus on every mutation. |
| `Save()` | Persists the editor document to `Path` and clears `IsDirty`. |
| `EditorState` | **Derived-only** camera/layers scratch. Never read by validation. |

`ScenarioAuthoringSession` is `IDisposable` for `using`-block lifetime clarity; it holds no
unmanaged resources.

---

## `ScenarioEditCommandBus` — the mutate pipeline

Every mutation flows through one private `Mutate(...)` method, so undo, concurrency, commit,
save, and live validation behave identically for all verbs.

```
RequireEditVersion(expected)   → throws ScenarioEditConflictException on mismatch (no write)
CaptureUndoSnapshot()          → deep clone of the current document
action(editor)                 → the actual edit (UpsertOrbatUnit, AddStrikeMission, …)
PersistUndoSnapshot(path, snap)→ push snapshot to the disk-backed undo stack (only after success)
CommitMutation()               → bump editVersion by 1
IsDirty = true;  if (save) Save()
LiveValidate()                 → refresh findings; attach to the result
```

On an `editVersion` mismatch the guard throws **before** any document change or file write, so a
`CONFLICT` result never leaves partial state behind. The snapshot is pushed *only after* the
action succeeds, so a failed mutation does not pollute the undo stack.

### `ScenarioMutationResult`

Every verb returns a [`ScenarioMutationResult`](../../src/ProjectAegis.Data/Scenario/Authoring/ScenarioAuthoringSession.cs)
(never throws for expected failures):

| Field | On success | On failure |
|-------|-----------|-----------|
| `Ok` | `true` | `false` |
| `EditVersion` | new (bumped) version | current version (`CONFLICT` carries the server's actual version) |
| `FileHash` | content hash after commit | hash from the conflict, when available |
| `Report` | live `ValidationReport` snapshot | `null` |
| `ErrorCode` / `ErrorMessage` | `null` | see codes below |

| `ErrorCode` | Cause |
|-------------|-------|
| `CONFLICT` | `expectedEditVersion` did not match the document's current version. Re-read `result.EditVersion`, rebuild, retry. |
| `INVALID_OPERATION` | The edit itself was rejected (e.g. delete a mission/side/event/timeline entry that does not exist). |

### Verb reference

All verbs take `expectedEditVersion` first and `bool save` last; `save: false` mutates in memory
and defers persistence to an explicit `session.Save()`.

| Verb | Effect |
|------|--------|
| `PlaceUnit(v, unit, save)` | Insert/replace an ORBAT unit (map place / inspector apply). |
| `MoveUnit(v, unitId, lat, lon, save)` | Move an existing unit. |
| `CloneUnit(v, sourceId, newId, lat, lon, save)` | Clone a unit under a new id at a position. |
| `UpsertReferencePoint(v, rp, save)` | Insert/replace a reference point (draw gesture end). Invalid geometry still commits (marked invalid on rebuild). |
| `AttachPatrolFromSelection(v, missionId, unitIds, zone, save)` | Attach a Patrol mission. |
| `AttachStrikeFromSelection(v, missionId, unitIds, targetIds, save)` | Attach a Strike mission. |
| `AttachFerryFromSelection(v, missionId, unitIds, destBaseId, save)` | Attach a Ferry mission. |
| `AttachSupportFromSelection(v, missionId, unitIds, role, stationZone, save)` | Attach a Support mission. |
| `CloneMission(v, sourceId, newId, save)` | Deep-copy a mission under a new id. |
| `AddFromTemplate(v, templateId, newMissionId, save)` | Add a mission from a built-in template (see below). |
| `DeleteMission(v, missionId, save)` | Remove a mission (`INVALID_OPERATION` if absent). |
| `UpsertEvent(v, evt, save)` / `DeleteEvent(v, eventId, save)` | Event-graph CRUD (see [event system](scenario-event-system.md)). |
| `UpsertTimelineEntry(v, entry, save)` / `DeleteTimelineEntry(v, missionId, save)` | Operations-timeline CRUD (keyed by mission id). |
| `UpsertSide(v, side, save)` / `DeleteSide(v, sideId, save)` | Side/faction CRUD. `DeleteSide` does **not** cascade ORBAT units. |
| `RefreshFindings()` | Re-run live validation without mutating; returns/stores `ValidationReport`. |

Built-in mission templates
([`MissionTemplateCatalog`](../../src/ProjectAegis.Data/Scenario/Authoring/MissionTemplateCatalog.cs)):
`tpl-patrol-empty`, `tpl-strike-empty`, `tpl-ferry-empty`, `tpl-support-tanker`.

---

## Presenters (headless view models)

Located in
[`src/ProjectAegis.Delegation.UnityAdapter/Authoring/`](../../src/ProjectAegis.Delegation.UnityAdapter/Authoring/).
Each is a plain C# class with no `UnityEngine` dependency, so it is unit-tested headless
(`ProjectAegis.Delegation.UnityAdapter.Tests/Authoring/`) and reused as the binding target for a
Unity host.

### `LiveFindingsPresenter`

Façade over `ScenarioEditCommandBus.RefreshFindings()`.

- `RefreshImmediate()` re-runs validation and stores `LastReport` plus `LastCodes` (codes sorted
  **ordinal ascending** — deterministic).
- `HasErrorSeverity` is true when any finding is `ValidationSeverity.Error`.
- The `debounceMs` constructor arg (default 300) is **reserved** — P2.1 always refreshes
  immediately; a future Unity host may coalesce `ScheduleRefresh()` calls on an `EditorWindow`
  tick (200–400 ms design range). Do not assume actual debouncing yet.

### `MissionBoardPresenter`

List/filter/select/clone/add missions (AME-3.4 / ME-W1).

- `TypeFilter` / `SideFilter` / `StatusFilter` (each `null` = all) feed
  [`MissionBoardQuery.List`](../../src/ProjectAegis.Data/Scenario/Authoring/MissionBoardQuery.cs),
  which projects the document into [`MissionBoardRow`](../../src/ProjectAegis.Data/Scenario/Authoring/MissionBoardRow.cs)
  (id, type, derived side, `Assigned`/`Unassigned` status, unit count) sorted by id ordinal.
  `SideId` is derived from the first assigned unit's side; `Status` is `Assigned` when unit
  count > 0.
- `Refresh()` rebuilds `Rows`; `Select(id)` sets `SelectedMissionId`.
- `CloneSelected(newId)` returns `null` when nothing is selected; `AddFromTemplate(templateId,
  newId)` adds from a built-in template. Both commit via the bus and, on success, re-`Refresh()`
  rows and refresh findings.

### `MapAuthoringSurface`

Map presentation + gesture staging (place unit / draw reference point).

- `RebuildFromDocument()` projects ORBAT into `OrbatGlyphView` and reference points into
  `ReferencePointView`, applying
  [`ScenarioGeometryValidity`](../../src/ProjectAegis.Data/Scenario/Authoring/ScenarioGeometryValidity.cs)
  so invalid geometry stays **visible and flagged** (`IsGeometryValid` + `InvalidReason`) — this
  is a map hint, not a Validation Engine finding.
- Gestures are two-phase: `BeginPlaceUnit` / `BeginDrawReferencePoint` stage a *tentative* DTO
  (nothing committed); `CommitPlaceUnit` / `CommitReferencePoint` push it through the bus and
  rebuild on success; `CancelGesture()` discards without mutating. `CommitPlaceUnit` /
  `CommitReferencePoint` return `null` when no gesture is staged.
- **Invalid reference-point geometry still commits** by design — the operator sees it on the map
  marked invalid rather than being silently blocked.
- `SelectUnit(id)` / `SelectReferencePoint(id)` populate the shared `Selection`
  (`SelectionInspectorModel`); unknown ids clear that selection.

### `SelectionInspectorModel`

Presentation-only unit/reference-point summary. `SetUnit` / `SetReferencePoint` are mutually
exclusive (selecting one clears the other) and build a one-line `SummaryLine`. It never mutates
the document.

### `EditModeController`

Play ↔ Edit finite-state machine for the host.

- Default `Mode` is `ScenarioHostMode.Edit`.
- `TryEnterPlay(forceConfirmInvalid = false)` refreshes findings first; if any is
  `Error`-severity and `forceConfirmInvalid` is false it stays put and returns `false`.
  `forceConfirmInvalid: true` allows play preview over an invalid document.
- `EnterEdit()` returns to edit mode. **No mode switch mutates the document/ORBAT** — the FSM
  only gates preview.

### `EventGraphPresenter`

Headless event-graph nodes/edges + static-analysis codes + per-event debugger JSON. Fully
documented in [scenario-event-system.md](scenario-event-system.md#headless-event-graph-view-model-eventgraphpresenter);
it consumes `EventStaticAnalyzer` / `EventDebuggerTrace` only and never touches `DelegationBridge`.

---

## Worked example

```csharp
using var session = ScenarioAuthoringSession.Open("baltic.scenario.json");
var findings = new LiveFindingsPresenter(session);
var board = new MissionBoardPresenter(session, findings);

// Place a unit through the bus (in-memory; defer disk write).
var place = session.Bus.PlaceUnit(
    session.EditVersion,
    new ScenarioOrbatUnitDto { Id = "u2", SideId = "blue", PlatformId = "corvette-a", Lat = 57.1, Lon = 20.4 },
    save: false);

if (!place.Ok && place.ErrorCode == "CONFLICT")
{
    // Someone else bumped the version — rebuild against place.EditVersion and retry.
}

// Add a mission from a template, then read the board.
session.Bus.AddFromTemplate(session.EditVersion, "tpl-patrol-empty", "m-patrol-1", save: false);
board.Refresh();
foreach (var row in board.Rows)
    Console.WriteLine(row.SummaryLine);   // e.g. "Patrol m-patrol-1 | units=0 | Unassigned"

session.Save();   // flush the deferred writes once
```

---

## Constraints & pitfalls

- **Always pass `session.EditVersion` to bus calls, and after each success read the new version
  back** (`result.EditVersion` or `session.EditVersion`). Re-using a stale local `int` across
  two mutations yields a spurious `CONFLICT`.
- **`save: false` batches edits in memory** — you *must* call `session.Save()` (or pass
  `save: true` on the final mutation) or the changes never reach disk. `IsDirty` tells you a
  flush is pending.
- **Never mutate the document outside the bus** in a hosted session. Calling
  `session.Editor.*` mutators directly bypasses undo capture, the version bump, and live
  validation — the presenters and undo stack will then be out of sync.
- **`EditorState` is not scenario data.** Do not encode authoritative values there; the
  validator and sim ignore it.
- **The CLI uses a different (equivalent) path.** One-shot verbs load
  `ScenarioDocumentEditor` per invocation and run the same capture → mutate → persist → commit
  steps directly (see [mission-editor-cli.md](mission-editor-cli.md)); the command bus is the
  long-lived in-process wrapper of that same contract. Undo is disk-backed
  (`<scenarioPath>.undo-stack.json`) so it survives across both.
- **Determinism:** presenters sort by ordinal string comparison (rows by id, finding codes,
  event nodes/edges) so headless output is stable and diffable.

---

## Source & tests

| Area | Source | Tests |
|------|--------|-------|
| Session / mutate pipeline | `ScenarioAuthoringSession`, `ScenarioEditCommandBus` | `ScenarioEditCommandBusTests`, `ScenarioMissionAttachBusTests`, `ScenarioMissionCloneBusTests`, `ScenarioSideEditorTests`, `ScenarioTimelineEditorTests` (`ProjectAegis.Data.Tests/Scenario/`) |
| Mission board | `MissionBoardQuery`, `MissionBoardRow`, `MissionBoardPresenter` | `MissionBoardIntegrationTests` (Data), `MissionBoardPresenterTests` (UnityAdapter) |
| Map surface / selection | `MapAuthoringSurface`, `SelectionInspectorModel`, `ScenarioGeometryValidity` | `ScenarioMapAuthoringIntegrationTests` (Data), `MapAuthoringSurfaceTests` (UnityAdapter) |
| Mode / findings | `EditModeController`, `LiveFindingsPresenter` | `EditModeControllerTests`, `LiveFindingsPresenterTests` (UnityAdapter) |
| Event graph | `EventGraphPresenter` | `EventGraphPresenterTests` (UnityAdapter) — see [event system](scenario-event-system.md) |

## Related

- [scenario-document-authoring.md](scenario-document-authoring.md) — the document format & edit lifecycle this host mutates.
- [mission-editor-cli.md](mission-editor-cli.md) — the one-shot CLI/MCP verbs over the same contract.
- [scenario-event-system.md](scenario-event-system.md) — the event-graph analyzer/debugger the `EventGraphPresenter` surfaces.
- [`docs/architecture/`](../architecture/) — ADR-008 (scenario document authoring) and related decisions.
