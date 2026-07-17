# Scenario AI-authoring assist & umpire adjudication workspace

Two authoring-time surfaces of the Mission Editor that sit *above* the document
model: the **AI-assisted authoring stubs** (`AiAuthoringServices`) and the
**first-class umpire adjudication workspace** (`AdjudicationWorkspace`). Both live
in `src/ProjectAegis.Data/Scenario/Authoring/`, both are engine-agnostic (nothing
references `UnityEngine`), and — crucially — both are **fully deterministic**: no
LLM, no wall clock, no dynamic code execution.

> **Scope.** This page covers the *AI-assist* and *adjudication* runtime object
> model. The *document format* (fields, per-mission-type rules, validation
> catalog) lives in [scenario-document-authoring.md](scenario-document-authoring.md);
> the interactive *host* (session, command bus, presenters) is in
> [scenario-authoring-host.md](scenario-authoring-host.md); the one-shot *CLI
> verbs* that expose these two surfaces (`scenario_ai_scaffold`,
> `scenario_umpire_snapshot`) are in [mission-editor-cli.md](mission-editor-cli.md);
> the event graph is in [scenario-event-system.md](scenario-event-system.md).
> Requirement source: `Game-Requirements/requirements/11-Agentic-Mission-Editor.md`
> (AC-3 umpire workspace, track 5/5 AI assist, NFR "no arbitrary code execution").

---

## The "no-LLM" contract (read this first)

The name *AI authoring* is aspirational. In v1 every one of these services is a
**deterministic heuristic stub** that emits **provenance tags** a human reviews —
not model inference. This is a deliberate architectural boundary, not a
placeholder waiting for a model:

- **[ADR-014](../architecture/adr-014-lua-compatibility-scope.md)** ("no Lua in
  v1") + doc 11's NFR "no arbitrary code execution in play mode" forbid any
  interpreter / scripting / eval surface on the authoring/export path.
- **`NoDynamicExecutionGateTests`** (`ProjectAegis.Data.Tests/Architecture/`)
  operationalizes that as an automated gate: it scans the `ProjectAegis.Data`
  and `ProjectAegis.MissionEditor.Cli` `.csproj` files for forbidden Lua-interpreter
  package references (`NLua`, `MoonSharp`, `KeraLua`, `Lua`) **and** scans the
  reachable authoring source (explicitly including `AiAuthoring*`) for
  `System.Reflection.Emit`, `Roslyn`, `CSharpScript`, `Process.Start`, and
  eval-style constructs.
- The pinning tests deliberately prove the stub nature: e.g. an unrecognised
  brief still returns a hard-coded fallback mission — "a real NL-understanding
  system would not need a hardcoded fallback" (`StubScopePinTests`).

So the correct mental model for a caller: these functions turn a brief / a
document / a question into a **structured, provenance-stamped draft or report**
that is 100% reproducible for the same input, then hand it to a human (or the
validation engine) for the real decision.

---

## `AiAuthoringServices` — the assist stubs

`public static class AiAuthoringServices`. Four independent pure methods, each
returning its own record plus one or more `ManifestBuilder.ProvenanceTag`s.

| Method | Signature | Returns |
|--------|-----------|---------|
| **NlScaffold** | `NlScaffold(string brief, string? baseDbRef = "baltic_patrol")` | `ScaffoldResult` |
| **CheckPlacement** | `CheckPlacement(string unitId, string? hostId, double lat, double lon, string mapBounds = "baltic")` | `(bool Allowed, string Reason, ProvenanceTag? Tag)` |
| **RunSmokeTestAgent** | `RunSmokeTestAgent(ScenarioDocumentDto document, string? policyContext = null)` | `SmokeTestReport` |
| **ExplainWithEvidence** | `ExplainWithEvidence(string question, ScenarioDocumentDto document, ValidationReport? report = null)` | `ExplainResult` |

### NlScaffold — brief → draft scaffold

Lower-cases the brief and applies a fixed keyword ruleset, seeding a real draft
via the public [`ScenarioDocumentEditor`](scenario-document-authoring.md) API
(`CreateNew` → `AddPatrolMission` / `AddStrikeMission`). Sides always start
`["Blue", "Red"]`.

| Brief keyword(s) | Effect |
|------------------|--------|
| `patrol` / `defend` / `baltic` | adds `patrol-blue-1` (patrol mission for `u1`, two waypoints) + objective |
| `strike` / `attack` / `offense` | adds `strike-blue-1` (strike mission for `u1` vs `hostile-1`) + objective |
| `support` / `tanker` | adds `support-1` objective (no editor mission) |
| *(no recognised keyword)* | falls back to a single hard-coded `patrol-default` mission |
| `red` / `opfor` | appends an `"Opfor"` side + `red-patrol-1` |

```csharp
var result = AiAuthoringServices.NlScaffold("Blue forces patrol the Baltic strait chokepoint");
// result.Missions        -> ["patrol-blue-1"]
// result.Sides           -> ["Blue", "Red"]
// result.DraftDocument   -> ScenarioDocumentDto with the patrol mission populated
// result.ProvenanceTags  -> [ ("nl-scaffold","ai","nl-scaffold-v1","blue forces patrol...") ]
// result.Explanation     -> "Scaffolded from NL brief using keyword parse. ..."
```

`ScaffoldResult` = `(ScenarioDocumentDto DraftDocument, string[] Sides, string[]
Missions, string[] Objectives, ProvenanceTag[] ProvenanceTags, string
Explanation)`. The `Explanation` prefix `"Scaffolded from NL brief using keyword
parse."` is contract-pinned — it advertises the deterministic mechanism.

### CheckPlacement — the ConstraintPlacementAssistant

Refuses invalid unit-host / map placements with a deterministic rule pair:

- **Valid host** = `hostId` empty, or starts with `base-`, or `== "carrier-1"`,
  or `== "airbase-blue"`.
- **Valid map** = `lat ∈ [54, 60]` and `lon ∈ [18, 25]` (rough Baltic box).

A refusal returns `(false, "<reason>", ProvenanceTag("constraint-refused", …))`;
an accept returns `(true, "Placement accepted by constraints",
ProvenanceTag("placement-accepted", …))`. Every outcome — including refusals — is
provenance-stamped so the decision trail survives into the manifest.

> The editor also exposes a much simpler `ConstraintPlacement(unit, host)` string
> helper; that is a separate CLI-observable shim and does **not** call the full
> `CheckPlacement` rules. Use `AiAuthoringServices.CheckPlacement` for the real
> map/host checks.

### RunSmokeTestAgent — degenerate-scenario detector

Scans a `ScenarioDocumentDto` for cheap "this scenario is broken" signals and
returns a `SmokeTestReport(bool Passed, IReadOnlyList<string> Issues, string
EvidenceSummary, ProvenanceTag Tag)`:

| Check | Issue raised when |
|-------|-------------------|
| Empty scenario | `Missions.Count == 0` |
| Degenerate patrol | `Patrol` mission with `< 2` patrol-zone waypoints |
| Impossible objective | `Strike` mission with `0` targets |
| Orphaned mission | any mission with `0` assigned units |

`Passed == (Issues.Count == 0)`. Note the **editor wrapper**
`ScenarioDocumentEditor.RunSmokeTestAgent()` returns a *fixed* summary string
(`"automated smoke-test agent: no trivial wins, no orphaned, no silent
failures"`) to guarantee a stable CLI observable — call the `AiAuthoringServices`
method directly when you need the real per-mission `Issues` list.

### ExplainWithEvidence — provenance-backed explanations

Ties a natural-language question to concrete document + validation evidence,
returning `ExplainResult(string Explanation, string[] EvidenceLines,
ProvenanceTag Tag)`. Evidence lines always include the mission count and
`EditVersion`; when a `ValidationReport` is supplied it folds in `Passed`,
finding count, and the first three findings (`{Code} {Severity} {Message}`); a
`why`/`patrol` question adds a patrol-detection note. The editor wrapper
`ExplainProvenance(item)` calls this with a `LiveValidate()` report.

### Provenance tags

Every AI output carries `ManifestBuilder.ProvenanceTag(string Tag, string Source,
string? AgentId = null, string? Evidence = null, string? Hash = null)`, where
`Source` marks origin (`"user"`, `"ai"`, `"import"`). These are the same tags
`ManifestBuilder.Build(...)` embeds in the published
[`ScenarioManifest.ProvenanceTags`](scenario-authoring-host.md) — so AI-assisted
content stays labelled as AI-assisted through publish.

### CLI / MCP surface

`scenario_ai_scaffold --brief "<text>" [--out <path>] [--db-ref <ref>]`
(`ScenarioAiScaffoldCommand.Run`) wraps `NlScaffold` and prints a JSON payload
(`sides`, `missions`, `objectives`, `provenanceTags`, `explanation`,
`draftMissions`). `--out` re-materialises the draft through the public
`CreateNew` + `Add*` API (refuses an existing file with `FILE_EXISTS`; a blank
brief returns `MISSING_BRIEF`). See [mission-editor-cli.md](mission-editor-cli.md).

---

## `AdjudicationWorkspace` — first-class umpire controls

`public sealed class AdjudicationWorkspace`. Wraps a `ScenarioDocumentEditor` to
give an umpire turn-boundary snapshots, before/after diffs, an audit log with
mandatory reasons, and freeze/step/inject/resume controls — all pure and
role-gated. This realises doc 11 AC-3's "umpire and adjudication workspace
(first-class)".

```csharp
var editor = ScenarioDocumentEditor.Load(path);      // or CreateNew()
var ws     = new AdjudicationWorkspace(editor, "umpire");

var before = ws.Snapshot("turn-0");
ws.Freeze();
ws.Inject(() => editor.AddPatrolMission("umpire-adj-1", new[] { "u1" },
    new[] { new ScenarioWaypointDto { Lat = 57.0, Lon = 20.0 } }),
    reason: "umpire adds adjudicated patrol for exercise control");
ws.Step();
ws.Resume();
var after = ws.Snapshot("turn-1");
var diff  = ws.ComputeDiff(before, after, "umpire inject");
```

### Role gating (verify-before)

`ApplyRoleGuard(role)` returns `"ok"` for `umpire` or `author` (case-insensitive)
and **throws `InvalidOperationException`** for anything else. The constructor
calls the guard immediately, so `new AdjudicationWorkspace(editor, "player")`
throws at construction — you cannot obtain a workspace in a disallowed role.
Every mutating/logging op re-checks the guard.

### Pure, deterministic snapshots & diffs

| Member | Behaviour |
|--------|-----------|
| `Snapshot(turn)` | deep-clones the current `ScenarioDocumentDto`; `StateHash = editor.ComputeFileHash()`; `Id = "snap-{turn}-{hash[0..8]}"`; **`Timestamp = hash`** (hash-based, *no wall clock*). Blank `turn` → `"turn-{StepCount}"`. Appends to `Snapshots`. |
| `ComputeDiff(before, after, reason)` | `DiffSummary` = mission-count `before→after (delta)` + `hash8→hash8`. Null snapshot → `ArgumentNullException`; blank reason → default text. |
| `AuditLog(action, reason, role?)` | appends `AuditEntry(Timestamp = "{stateHash}-{index}", Action, Reason, Role, StateHash)`. **Blank reason → `InvalidOperationException`** (a reason is mandatory). |

> `ComputeDiff` is a coarse mission-count summary, not a field-level semantic
> diff. For a full structural diff use
> [`ScenarioSemanticDiff`](scenario-authoring-host.md); the workspace diff is a
> quick "what did this intervention change" umpire log line.

### Freeze / step / inject / resume

| Control | Effect |
|---------|--------|
| `Freeze()` | sets `IsFrozen = true`, audits `"freeze"`. |
| `Step()` | `StepCount++`, audits `"step"`; does **not** un-freeze. |
| `Inject(mutation, reason)` | the core intervention: snapshots `pre-inject-{step}` → runs `mutation()` → `editor.CommitMutation()` → snapshots `post-inject-{step}` → `ComputeDiff` → audits `"inject"` with the diff summary. Allowed **while frozen** (that is the point). Null mutation → `ArgumentNullException`. |
| `Resume()` | sets `IsFrozen = false`, audits `"resume"`. |

Records exposed: `AdjudicationSnapshot(Id, Turn, StateHash, State, Timestamp)`,
`AdjudicationDiff(Id, BeforeHash, AfterHash, Reason, DiffSummary)`,
`AuditEntry(Timestamp, Action, Reason, Role, StateHash)`. `Snapshots` and
`AuditEntries` are exposed read-only for traceability.

### CLI / MCP surface

`scenario_umpire_snapshot [--path <file>]` (`RunScenarioUmpireSnapshot`)
demonstrates the full loop headlessly — snapshot, inject, diff, audit, role
guard, freeze/step/resume — printing the AC-3 keyword lines. See
[mission-editor-cli.md](mission-editor-cli.md).

---

## Determinism & replay safety

Both surfaces live in `ProjectAegis.Data` on the **authoring/export path**, not
in the simulation tick hotpath — they never touch `DelegationOrchestrator.Tick`
or the engagement/replay loop, so **nothing here affects the Baltic replay
goldens or the v2 hash `17144800277401907079`**. Within their own scope they are
still strictly deterministic: identical inputs yield byte-identical outputs
(hash-based snapshot timestamps, keyword heuristics, no `DateTime.UtcNow`, no
RNG), which is what lets `StubScopePinTests` / `McpMissionToolCliTests` assert on
exact strings.

## Extending safely

- **Adding an assist heuristic** — keep it a pure function of its inputs, emit a
  `ProvenanceTag`, and add a pinning test asserting the deterministic output.
  Do **not** introduce any dynamic-execution / interpreter dependency — the
  `NoDynamicExecutionGateTests` gate will fail closed.
- **Adding an adjudication op** — route it through `ApplyRoleGuard`, keep
  snapshots pure (hash-based timestamps, deep-clone state), and require a reason
  for anything that writes to the audit log.
- **Provenance discipline** — any new AI/imported content must remain tagged so
  `ManifestBuilder` can surface it at publish.

## Verify against source

| Concern | Source of truth |
|---------|-----------------|
| Assist stubs | `src/ProjectAegis.Data/Scenario/Authoring/AiAuthoringServices.cs` |
| Provenance tag / manifest | `src/ProjectAegis.Data/Scenario/Authoring/ScenarioManifest.cs` (`ManifestBuilder`) |
| Editor wrappers | `ScenarioDocumentEditor.NlScaffold/ExplainProvenance/RunSmokeTestAgent/ConstraintPlacement` |
| Umpire workspace | `src/ProjectAegis.Data/Scenario/Authoring/AdjudicationWorkspace.cs` |
| No-LLM gate | `src/ProjectAegis.Data.Tests/Architecture/NoDynamicExecutionGateTests.cs` |
| Pinned behaviour | `ProjectAegis.Data.Tests/Scenario/StubScopePinTests.cs`, `ProjectAegis.MissionEditor.Cli.Tests/McpMissionToolCliTests.cs` |
| CLI verbs | `ScenarioAiScaffoldCommand.cs`, `Program.cs` (`RunScenarioUmpireSnapshot`) |

## See also

| Doc | Why |
|-----|-----|
| [scenario-document-authoring.md](scenario-document-authoring.md) | The `ScenarioDocumentDto` model + validation catalog these services draft and check. |
| [scenario-authoring-host.md](scenario-authoring-host.md) | The interactive host, command bus, `ScenarioSemanticDiff`, and manifest publish. |
| [mission-editor-cli.md](mission-editor-cli.md) | The `scenario_ai_scaffold` / `scenario_umpire_snapshot` CLI + MCP verbs. |
| [scenario-event-system.md](scenario-event-system.md) | Sibling authoring-time analysis surface (event static analyzer / debugger). |
| [../architecture/adr-014-lua-compatibility-scope.md](../architecture/adr-014-lua-compatibility-scope.md) | The "no Lua / no dynamic execution in v1" decision behind the no-LLM contract. |
