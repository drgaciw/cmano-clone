# Scenario semantic diff — id-level change summary (AME-7.3 / ME-W3)

[`ScenarioSemanticDiff`](../../src/ProjectAegis.Data/Scenario/Authoring/ScenarioSemanticDiff.cs)
(`ProjectAegis.Data/Scenario/Authoring/`) produces a compact, human-readable summary of the
**id-level** differences between two canonical scenario documents — "what missions/units/sides/
events/timeline entries were added, removed, or changed?" — and the headless
[`scenario_diff_summary`](../../src/ProjectAegis.MissionEditor.Cli/ScenarioDiffSummaryCommand.cs)
CLI/MCP verb exposes it over two files. It is the review/QA companion to the authoring stack:
[scenario-document-authoring.md](scenario-document-authoring.md) defines the document, the
[authoring host](scenario-authoring-host.md) mutates it, and this diff explains what a change set
actually did.

> **Scope.** AME-7.3 (Partial+) / ME-W3 Track W3-c. `Summarize` is a **pure, deterministic,
> read-only** function over two [`ScenarioDocumentDto`](../../src/ProjectAegis.Data/Scenario/Authoring/ScenarioDocumentDto.cs)
> instances (ADR-008 scenario document). It compares **ids only** — not full field-by-field
> content — plus the two fields that carry an obvious ordering/typing signal (mission `Type`,
> timeline `ActivateAtTick`). No sim, no I/O, no mutation.

Related: [scenario-document-authoring.md](scenario-document-authoring.md) ·
[scenario-authoring-host.md](scenario-authoring-host.md) ·
[scenario-event-system.md](scenario-event-system.md) ·
[mission-editor-cli.md](mission-editor-cli.md) (the CLI/MCP verb surface) ·
[ADR-008 mission editor validation engine](../architecture/adr-008-mission-editor-validation-engine.md).

---

## What it compares

`Summarize(before, after)` walks five id-keyed collections of the document and emits one bullet per
detected change. Adds/removes are detected everywhere; **content changes** are only reported for the
two fields below (everything else is add/remove only):

| Collection | Keyed by | Add | Remove | Change reported |
|------------|----------|-----|--------|-----------------|
| `Missions` | `Id` | ✅ | ✅ | `Type` change |
| `Orbat.Units` | `Id` | ✅ | ✅ | — (set diff) |
| `Sides` | `Id` | ✅ | ✅ | — (set diff) |
| `Events` | `Id` | ✅ | ✅ | — (set diff) |
| `OperationsTimeline` | `MissionId` | ✅ | ✅ | `ActivateAtTick` change |

Null collections are treated as empty, so a document with no `Orbat` simply contributes no unit
bullets. When two entries share an id, the **last one wins** (`ToLastById`), matching the canonical
document's own last-write semantics.

## Bullet grammar

Each bullet is `"{kind} {op}{id}"`, where `op` is `+` (added), `-` (removed), or `~` (changed):

| Bullet | Meaning |
|--------|---------|
| `mission +m-new (Ferry)` | mission `m-new` added, with its `Type` |
| `mission -m-gone` | mission `m-gone` removed |
| `mission ~m-flip type Patrol→Strike` | mission `m-flip`'s `Type` changed |
| `unit +u3` / `unit -u2` | ORBAT unit added / removed |
| `side +green` / `side -red` | side added / removed |
| `event +e3` / `event -e2` | event added / removed |
| `timeline +m-d` / `timeline -m-c` | operations-timeline entry added / removed (by `MissionId`) |
| `timeline ~m-b tick 20→25` | timeline entry's `ActivateAtTick` changed |

## Determinism

Determinism is the load-bearing property (it feeds review tooling and tests):

- Bullets are collected in a fixed collection order, then **sorted with `StringComparer.Ordinal`**
  and joined by the `Separator` `"; "`.
- Two documents with no id-level differences return the `NoChanges` constant, `"no semantic
  changes"`.
- A `null` `before` or `after` throws `ArgumentNullException`.

```csharp
// after adds side "alpha", unit "z-unit", mission "m1" (Patrol) and event "e-early" to an empty doc:
ScenarioSemanticDiff.Summarize(before, after);
// => "event +e-early; mission +m1 (Patrol); side +alpha; unit +z-unit"  (ordinal-sorted, "; "-joined)
```

## CLI / MCP verb — `scenario_diff_summary`

[`ScenarioDiffSummaryCommand.Run(beforePath, afterPath, output)`](../../src/ProjectAegis.MissionEditor.Cli/ScenarioDiffSummaryCommand.cs)
loads both scenario JSON files via `ScenarioDocumentJsonLoader.LoadFromFile`, runs `Summarize`, and
writes `{ "ok": true, "summary": "…" }`. It is **read-only** (never writes a document).

| Outcome | Result |
|---------|--------|
| Missing `--before` / `--after` | error `INVALID_ARGS` |
| A path does not exist | error `NOT_FOUND` |
| A file fails to load/parse (`InvalidDataException` / `IOException` / `JsonException`) | error `LOAD_FAILED` |
| Success | `{ ok, summary }` (summary is the `Summarize` string or `no semantic changes`) |

See [mission-editor-cli.md](mission-editor-cli.md) for the full verb surface and how the CLI is
invoked headlessly / over MCP.

## Runbook — extend the diff

1. **Report a new collection** → add an `Append…Diffs` helper that keys the collection by its id and
   calls `AppendSetDiffs` (add/remove) or the map pattern (for `~` change detection), then wire it
   into `Summarize`. Keep ids ordinal-sorted.
2. **Report a new content change** → follow the mission-`Type` / timeline-`ActivateAtTick` pattern:
   compare the field only when the id exists on **both** sides and emit a `~{id} …→…` bullet.
3. **Keep it pure and deterministic** → no I/O, no clock; every new bullet must sort stably under
   `StringComparer.Ordinal`. Add a case to `ScenarioSemanticDiffTests`.

## Tests

| Test file | Covers |
|-----------|--------|
| [`ScenarioSemanticDiffTests.cs`](../../src/ProjectAegis.Data.Tests/Scenario/ScenarioSemanticDiffTests.cs) | Identical-docs `no semantic changes`, `null` throws, mission add/remove/`Type`-change (unchanged mission emits nothing), unit/side/event set add/remove, timeline add/remove/`tick` change, and the ordinal-sorted `"; "`-joined ordering. |
