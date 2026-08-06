# ProjectAegis.MissionEditor.Cli

The **headless Mission Editor** — a single console executable that authors, validates,
simulates, and publishes scenario documents and browses/extends the catalog. Every verb runs
without Unity (ADR-008 / ADR-010, *headless-first, command-driven*) and is also registered as an
**MCP tool** for the Unity-MCP host, so the same code path serves the CLI, CI, and the editor UI.

> **This README documents the assembly's shape and conventions.** For the exhaustive verb list,
> flags, the JSON/exit-code contract, and troubleshooting, read the operational reference:
> [`docs/engineering/mission-editor-cli.md`](../../docs/engineering/mission-editor-cli.md). A
> five-verb quickstart lives in [`tools/mission-editor/README.md`](../../tools/mission-editor/README.md).

---

## Architecture

The executable is a thin dispatcher over per-verb command classes; it owns no domain logic of its own.

```
args[0] = verb
   │
   ▼
Program.cs  (top-level switch)              ← parse-and-route only
   │  CliArgParser.Get*(args, "--flag")     ← flag/positional/waypoint parsing
   ▼
<Verb>Command.Run(...)                      ← one static entry per verb family
   │  ProjectAegis.Data / .Data.Excel / .Delegation.UnityAdapter
   ▼
McpToolResult.WriteOk / WriteError          ← single-line camelCase JSON on stdout
```

- **`Program.cs`** — top-level statement dispatch. A `switch` on `args[0]` routes to a
  `Run*` helper that pulls flags and calls the matching command class. Unknown verb ⇒ usage + exit `1`.
- **`*Command.cs`** — one file per verb (or verb family), each exposing a static `Run(...)` that
  takes already-parsed arguments and a `TextWriter`, keeping every verb unit-testable without a process.
- **`CliArgParser.cs`** — the only argument parser: `GetFlag`, `GetIntFlag`, `GetDoubleFlag`,
  `GetULongFlag` (defaults on parse failure), `GetRepeated` (repeatable flags like `--unit`/`--wp`),
  `GetPositional` (skips flag *values* so a `--db` path is never misread as a positional), and
  `ParseWaypoints` (`lat,lon` → `ScenarioWaypointDto`, throws `FormatException` on bad input).
- **`McpToolResult.cs`** — serializes results as one camelCase JSON line and maps error codes to exit codes.

The project references `ProjectAegis.Data`, `ProjectAegis.Data.Excel` (the ClosedXML edge adapter,
wired for the `platform_*_xlsx` verbs), and `ProjectAegis.Delegation.UnityAdapter` (the Baltic harness
used by `scenario_simulate_sample`). Targets `net8.0`.

---

## Verb families

| Family | Verbs (prefix) | Purpose |
|--------|----------------|---------|
| Scenario lifecycle | `scenario_create` · `scenario_validate` · `scenario_export[_brief]` · `scenario_publish` · `scenario_simulate_sample` · `scenario_undo` · `scenario_event_trace` · `scenario_comms_status` · `scenario_cyber_status` · `scenario_near_future_spawn` · `scenario_ai_scaffold` · `scenario_migrate_preview` · `scenario_umpire_snapshot` | Create, validate, simulate, and publish scenario documents. |
| Mission authoring | `mission_add_{patrol,strike,ferry,support}` · `mission_update_{patrol,strike,ferry}` · `mission_delete` · `mission_plan_suggest` | Mutate missions inside a scenario (all mutations require `--edit-version`). |
| Catalog | `catalog_platform_browse` · `catalog_entity_map` · `catalog_intelligence_run` · `catalog_dependency_graph` · `catalog_kill_chain_report` · `catalog_link_report` · `catalog_release_diff` · `catalog_write_{propose,approve}` · `catalog_import_markdown` · `catalog_mount_loadout_quarantine_triage` | Read the catalog and stage/approve **extend-only** write-gate batches. |
| Platform Excel (ADR-011) | `platform_export_xlsx` · `platform_import_xlsx` · `platform_diff_xlsx` | Round-trip the platform workbook via the [`ProjectAegis.Data.Excel`](../ProjectAegis.Data.Excel/README.md) adapter (`--io closedxml\|canonical`). |
| OSINT | `osint_search` · `osint_staging_review` | Fetch OSINT proposals (fixture `data/osint_facts.json`) and review staged batches. |

Run the tool with **no arguments** to print the full usage list. Some command classes (e.g.
`EventCommands`) exist in the assembly but are **not yet wired** into the dispatch switch or the MCP
manifest — see the operational reference for the current runnable-verb set.

---

## Conventions

- **Output is one JSON line on stdout.** `McpToolResult.WriteOk` emits `{ "ok": true, … }`;
  `WriteError` emits `{ "ok": false, "code": …, "message": …, "details"? }` (camelCase). Consumers
  parse the last stdout line. Missing-flag errors are plain text on **stderr**, not the JSON envelope.
- **Exit codes:** `0` success · `1` usage/not-found/blocking failure · `3` **edit-version conflict**
  (`code: "CONFLICT"`, `ScenarioEditVersionGuard.ConflictCode`) so callers can distinguish a stale
  write from an ordinary error.
- **Optimistic concurrency:** every mutating scenario verb requires `--edit-version N` matching the
  file's current `metadata.editVersion`; a mismatch returns exit `3` with the current version in
  `details`. Each successful mutation bumps and returns the new `editVersion` and `fileHash`.
- **Validation-gated export:** `scenario_export[_brief]` and `scenario_publish` run the validator
  first and refuse to emit output when the scenario has blocking findings.
- **Determinism:** verbs that simulate (`scenario_simulate_sample`) or hash are pure functions of
  `(scenario, seed)`; golden hashes live in `SimulateSampleGoldenHashes` — regenerate intentionally.

---

## Build & run

```bash
dotnet build src/ProjectAegis.MissionEditor.Cli/ProjectAegis.MissionEditor.Cli.csproj

# Invoke a verb (everything after `--` is passed to the tool)
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- <verb> [--flag value]...

# List every verb + its usage
dotnet run --project src/ProjectAegis.MissionEditor.Cli

dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj -v minimal
```

`ProjectAegis.MissionEditor.Cli.Tests` (~43 tests, part of the ≥1232-test solution baseline)
exercises each command class directly plus `McpToolsManifestTests`, which asserts every required
MCP verb appears in [`tools/mission-editor/mcp-tools.json`](../../tools/mission-editor/mcp-tools.json)
with an `inputSchema`.

## See also

| Topic | Doc |
|-------|-----|
| Full verb / flag / JSON-contract reference + troubleshooting | [`docs/engineering/mission-editor-cli.md`](../../docs/engineering/mission-editor-cli.md) |
| Quickstart (validate / brief / simulate / plan) | [`tools/mission-editor/README.md`](../../tools/mission-editor/README.md) |
| Validation engine design | [`adr-008-mission-editor-validation-engine.md`](../../docs/architecture/adr-008-mission-editor-validation-engine.md) |
| Headless-first command-driven UI | [`adr-010-headless-first-command-driven-ui.md`](../../docs/architecture/adr-010-headless-first-command-driven-ui.md) |
| Platform Excel adapter (ClosedXML edge) | [`../ProjectAegis.Data.Excel/README.md`](../ProjectAegis.Data.Excel/README.md) |
| Data / catalog + extend-only write gate | [`../ProjectAegis.Data/README.md`](../ProjectAegis.Data/README.md) |
| Baltic harness used by `scenario_simulate_sample` | [`../ProjectAegis.Delegation.UnityAdapter/README.md`](../ProjectAegis.Delegation.UnityAdapter/README.md) |
| Hard invariants + verification block | [`AGENTS.md`](../../AGENTS.md#hard-invariants--never-break-these) |
