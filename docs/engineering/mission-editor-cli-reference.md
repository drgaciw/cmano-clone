# Mission Editor CLI — headless MCP verb reference

Complete reference for the **headless mission-editor command surface** (`ProjectAegis.MissionEditor.Cli`).
Every verb here is the *same* contract whether it is called from CI, a shell, the Unity-MCP host, or a
future agent authoring flow — that single-surface guarantee is the whole point of
[ADR-010 (headless-first / command-driven UI)](../architecture/adr-010-headless-first-command-driven-ui.md)
and [ADR-008 (mission-editor validation engine)](../architecture/adr-008-mission-editor-validation-engine.md).

This page is the **index and contract** for the verb catalog. Deep dives for individual subsystems live in
their own pages and are linked inline; do not duplicate their content here.

| Question | Answer |
|----------|--------|
| What is it? | A single console app (`dotnet run --project src/ProjectAegis.MissionEditor.Cli`) that dispatches one verb per invocation. |
| Who calls it? | CI scripts, developers, the Unity-MCP host (via PowerShell wrappers), and agent tooling. |
| What does it return? | A one-line JSON envelope on stdout plus a process exit code. |
| What guarantees safety? | Mutating verbs use optimistic concurrency (`--edit-version`) or stage through the catalog write gate; nothing auto-commits. |
| Where are the bindings? | [`tools/mission-editor/mcp-tools.json`](../../tools/mission-editor/mcp-tools.json) (schema v2). |

## Why it exists

Project Aegis is headless-first: `ProjectAegis.Data`, `ProjectAegis.Sim`, and `ProjectAegis.Delegation`
are pure C# with no `UnityEngine` dependency. The Unity UI is a *client* that submits the same commands a
test or CI job could. This CLI is the concrete embodiment of that contract — it exposes scenario authoring,
mission CRUD, validation, simulation sampling, catalog governance, platform editing, and OSINT review as
verbs that produce canonical, Git-diffable artifacts and deterministic JSON.

Because the same verbs drive humans, MCP, and agents, the editor cannot drift: there is no parallel "UI-only"
write path. See ADR-010 §2 ("UI is a client, not an authority").

## Invocation

Direct (any platform with the .NET 8 SDK):

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- <verb> [--flag value ...]
```

Through the MCP/PowerShell wrapper (what the Unity-MCP host registers):

```powershell
.\tools\mission-editor\Invoke-MissionEditorMcp.ps1 -Command <verb> -ExtraArgs --flag value
```

The wrapper simply forwards to `dotnet run -- <verb> <ExtraArgs>` from the repo root and propagates the exit
code, so the contract below is identical for both. `scenario_validate`, `scenario_export_brief`, and
`scenario_simulate_sample` also have dedicated wrappers (`Invoke-ScenarioValidate.ps1`,
`Invoke-ScenarioSimulateSample.ps1`).

### Argument conventions

Flags are `--name value` pairs parsed by `CliArgParser`. Notable forms:

- **Repeatable flags** (`--unit`, `--target`, `--wp`) may appear multiple times; each occurrence is collected.
- **Waypoints** (`--wp`) are `lat,lon` decimal pairs (e.g. `--wp 57.1,20.1`). A malformed pair fails with an
  `INVALID_ZONE` error envelope.
- **Aliases**: several verbs accept synonyms — `--out`/`--output`, `--in`/`--input`, `--snapshot`/`--snapshot-id`,
  `--base`/`--source`, `--edited`/`--in`.
- Unknown verbs print usage and exit `1`.

## Result envelope and exit codes

Most verbs emit a single line of JSON via `McpToolResult`:

```jsonc
// success
{ "ok": true, /* verb-specific payload (camelCase) */ }

// error
{ "ok": false, "code": "INVALID_ZONE", "message": "…", "details": { /* optional */ } }
```

| Exit code | Meaning |
|-----------|---------|
| `0` | Success (or, for `scenario_export_brief`/`scenario_simulate_sample`, validation passed and the step ran). |
| `1` | Usage error (missing/invalid required flag) or a generic tool error envelope. |
| `3` | **Edit-version conflict** — the supplied `--edit-version` did not match the scenario's current version (optimistic-concurrency guard, code `CONFLICT`). Re-read the scenario and retry. |

> The conflict exit code is load-bearing for MCP/agent retries: a `3` means "someone else moved the file",
> not "your request was malformed". See `ScenarioEditVersionGuard` (TR-editor-004 / ADR-008).

## Verb catalog

### Scenario authoring & inspection

| Verb | Required flags | Optional flags | Returns |
|------|----------------|----------------|---------|
| `scenario_create` | `--out <scenario.json>` | `--db-ref`, `--policy-id`, `--seed` | Writes a new canonical scenario file. |
| `scenario_validate` | `--path <scenario.json>` | — | `ValidationReport` (`passed`, `canExport`, `reportHash`, `findings[]`). |
| `scenario_export_brief` | `--path <scenario.json>` | `--out <brief.md>` | Validates first; writes a stub brief only when `canExport`. Blocks (non-zero) if validation fails. |
| `scenario_simulate_sample` | `--path <scenario.json>` | `--ticks <N>` (default 32) | Isolated Baltic harness sample (`worldHash`, fingerprint, engagement count). Validates first. |
| `scenario_comms_status` | `--policy <id>` | — | Comms display settings + timeline transitions. |
| `scenario_cyber_status` | `--policy <id>` | — | Cyber abort codes + comms-coupling hints. |
| `scenario_near_future_spawn` | `--path <scenario.json>` | — | Gated CCA / hypersonic spawn plan from scenario metadata. |

### Mission CRUD (optimistic concurrency)

All mutating mission verbs require `--edit-version <N>` and emit exit `3` on a version mismatch.

| Verb | Required flags | Repeatable flags | Notes |
|------|----------------|------------------|-------|
| `mission_add_patrol` | `--path`, `--edit-version`, `--id` | `--unit`, `--wp lat,lon` | Adds a patrol mission with a waypoint zone. |
| `mission_add_strike` | `--path`, `--edit-version`, `--id` | `--unit`, `--target` | Adds a strike mission. |
| `mission_update_patrol` | `--path`, `--edit-version`, `--id` | `--unit`, `--wp lat,lon` | Replaces units/zone when supplied. |
| `mission_update_strike` | `--path`, `--edit-version`, `--id` | `--unit`, `--target` | Replaces units/targets when supplied. |
| `mission_delete` | `--path`, `--edit-version`, `--id` | — | Removes a mission by id. |
| `mission_plan_suggest` | `--intent "<text>"` | — | NL keyword stub; returns suggested `mission_add_*` / scenario calls (read-only, no file write). |

### Catalog governance

The catalog write path is **propose → approve → commit**; nothing the CLI does bypasses the gate. See
[catalog write gate](catalog-write-gate.md) and [CMO markdown catalog import](cmo-markdown-catalog-import.md).

| Verb | Required flags | Optional flags | Returns |
|------|----------------|----------------|---------|
| `catalog_intelligence_run` | — | `--db <catalog.db>` | Runs the catalog intelligence pass. |
| `catalog_entity_map` | — | — | Emits the entity→table mapping. |
| `catalog_write_propose` | `--db`, `--platform`, `--sensor`, `--base-pd` | — | Stages a proposed write batch (returns a batch id). |
| `catalog_write_approve` | `--db`, `--batch` | `--snapshot-id`, `--release-version` | Approves/commits a staged batch. |
| `catalog_import_markdown` | `--db`, `--markdown` | `--max-records`, `--chunk-size`, `--report-out` | Stages CMO markdown rows through the write gate. |

### Platform editor (Excel round-trip)

Export a bound snapshot, edit offline, re-import the **diff** through the staged write gate. Full runbook:
[platform editor Excel round-trip](platform-editor-excel-roundtrip.md) / [ADR-011](../architecture/adr-011-platform-editor-excel-roundtrip.md).

| Verb | Required flags | Optional flags | Returns |
|------|----------------|----------------|---------|
| `platform_export_xlsx` | `--out <path>` | `--db`, `--snapshot` | Exports the bound snapshot workbook (canonical text; `.xlsx` adapter staged per ADR-011). |
| `platform_import_xlsx` | `--db` | `--in`, `--actor-type`, `--actor-id` | Stages workbook edits via `IWriteGate` (no auto-commit; `nextStep: catalog_write_approve`). |
| `platform_diff_xlsx` | — | `--db`, `--base`, `--edited` | Deterministic diff report (change count + determinism note). |

### OSINT discovery & review

Discovery is log-only by default; promotion to the catalog goes through the write gate. See
[OSINT discovery pipeline](osint-discovery-pipeline.md).

| Verb | Required flags | Optional flags | Returns |
|------|----------------|----------------|---------|
| `osint_search` | — | `--db <fixture.json>` | Proposals from `FileOsintConnector` + digest runner, plus `logOnlyCount`. |
| `osint_staging_review` | `--db` | `--approve <batchId>` | Lists pending proposals, or approves a batch. |

> The MCP manifest also exposes thin aliases — `osint_digest`, `osint_list_staging_proposals`,
> `osint_get_proposal_detail`, `osint_submit_review_decision` — that delegate to `osint_search` /
> `osint_staging_review` (req 05). They are bindings over the same two CLI verbs, not separate code paths.

## Worked examples

Create a scenario, add a patrol, validate, then sample-simulate:

```bash
PROJ=src/ProjectAegis.MissionEditor.Cli

dotnet run --project $PROJ -- scenario_create --out work/baltic.json --seed 42
dotnet run --project $PROJ -- mission_add_patrol \
  --path work/baltic.json --edit-version 0 --id patrol-1 \
  --unit u1 --wp 57.0,20.0 --wp 57.1,20.1
dotnet run --project $PROJ -- scenario_validate --path work/baltic.json
dotnet run --project $PROJ -- scenario_simulate_sample --path work/baltic.json --ticks 32
```

Handle an edit-version conflict (exit `3`):

```bash
dotnet run --project $PROJ -- mission_delete --path work/baltic.json --edit-version 0 --id patrol-1
# -> {"ok":false,"code":"CONFLICT","message":"editVersion mismatch: expected 0, current 1."} ; exit 3
# Re-read the scenario's current editVersion and retry with the new value.
```

Stage a platform edit and approve it:

```bash
dotnet run --project $PROJ -- platform_export_xlsx --db baltic_patrol --out work/platforms.txt
# ...edit offline...
dotnet run --project $PROJ -- platform_import_xlsx --db baltic_patrol --in work/platforms.txt
dotnet run --project $PROJ -- catalog_write_approve --db baltic_patrol --batch <batchId>
```

## Adding or changing a verb

1. Add a `case` in `src/ProjectAegis.MissionEditor.Cli/Program.cs` plus a `Run*` helper and a `*Command.cs`.
2. Emit results through `McpToolResult.WriteOk` / `WriteError` so the envelope and exit codes stay consistent.
3. Register the MCP binding in [`tools/mission-editor/mcp-tools.json`](../../tools/mission-editor/mcp-tools.json)
   (bump `schemaVersion` only on breaking changes) and keep `McpToolsManifestTests` green.
4. Update the catalog table above and, for a substantial subsystem, add or extend its dedicated page.

## Related docs

- [`tools/mission-editor/README.md`](../../tools/mission-editor/README.md) — wrapper-script quickstart.
- [ADR-010 — headless-first / command-driven UI](../architecture/adr-010-headless-first-command-driven-ui.md)
- [Catalog write gate](catalog-write-gate.md) · [CMO markdown catalog import](cmo-markdown-catalog-import.md)
- [Platform editor Excel round-trip](platform-editor-excel-roundtrip.md) · [ADR-011](../architecture/adr-011-platform-editor-excel-roundtrip.md)
- [OSINT discovery pipeline](osint-discovery-pipeline.md)
- [Doctrine inheritance panel](doctrine-inheritance-panel.md) · [Balance telemetry drift](balance-telemetry-drift.md)
