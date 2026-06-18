# ProjectAegis.MissionEditor.Cli — Headless MCP tool surface

A headless console app (`net8.0`, `OutputType=Exe`) that implements the
**mission-editor MCP tools** defined by [ADR-008](../../docs/architecture/adr-008-mission-editor-validation-engine.md).
Every verb runs without Unity, emits machine-readable JSON, and is the
implementation behind the bindings in
[`tools/mission-editor/mcp-tools.json`](../../tools/mission-editor/mcp-tools.json).

The Unity-MCP host (or CI) invokes these verbs through the PowerShell wrappers in
[`tools/mission-editor/`](../../tools/mission-editor/README.md); this project is
the engine they call. It edits scenario JSON and drives the
[`ProjectAegis.Data`](../ProjectAegis.Data/README.md) catalog write gate — it does
**not** run the simulation.

## Invocation

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- <verb> [--flag value ...]
```

The first positional argument is the verb (see [Verbs](#verbs)); everything after
it is parsed by [`CliArgParser`](CliArgParser.cs). With no arguments the tool prints
usage and exits `1`.

## Output and exit-code contract

Most verbs print a single JSON line/object via [`McpToolResult`](McpToolResult.cs)
with **camelCase** properties:

| Outcome | Shape | Exit |
|---------|-------|------|
| Success | `{ "ok": true, ... }` | `0` |
| Error | `{ "ok": false, "code": "...", "message": "...", "details"?: ... }` | `1` |
| Edit-version conflict | error payload with `"code": "CONFLICT"` | `3` |

- Exit `3` is the optimistic-concurrency signal from
  [`ScenarioEditVersionGuard`](../ProjectAegis.Data/Scenario/Authoring/ScenarioEditVersionGuard.cs):
  the `--edit-version` you passed no longer matches the scenario on disk. Re-read the
  scenario, take its current edit version, and retry.
- Missing or invalid required flags are reported on **stderr** as plain text and exit
  `1` (these early failures are not JSON). Some catalog verbs instead emit
  `{ "ok": false, "error": "..." }` (e.g. `database_not_found`).
- `stdout` is reserved for the JSON result, so a caller can parse it directly.

## Verbs

All flags use `--name value`; repeatable flags (`--unit`, `--wp`, `--target`) may
appear multiple times. `--wp` takes `lat,lon` (invariant-culture decimals).

### Scenario lifecycle

| Verb | Required | Optional | Notes |
|------|----------|----------|-------|
| `scenario_create` | `--out` | `--db-ref`, `--policy-id`, `--seed` | Writes a new scenario JSON. |
| `scenario_validate` | `--path` | — | Returns `passed`, `canExport`, `reportHash`, `findings[]`. Exit `1` on blocking findings. |
| `scenario_export_brief` | `--path` | `--out` | Validates first; writes a stub brief only when validation passes (defaults to `<scenario>.brief.md`). |
| `scenario_simulate_sample` | `--path` | `--ticks` (default 32) | Validates, then runs an isolated Baltic harness sample. |
| `scenario_comms_status` | `--policy` | — | Comms readiness for a policy id. |
| `scenario_cyber_status` | `--policy` | — | Cyber readiness for a policy id. |
| `scenario_near_future_spawn` | `--path` | — | Near-future spawn preview for a scenario. |

### Mission CRUD + planning

All mission mutations require `--path`, `--edit-version N` (N ≥ 0), and `--id`.

| Verb | Extra flags |
|------|-------------|
| `mission_add_patrol` | `[--unit U]+ [--wp lat,lon]+` |
| `mission_add_strike` | `[--unit U]+ [--target T]+` |
| `mission_update_patrol` | `[--unit U]+ [--wp lat,lon]+` (zone optional on update) |
| `mission_update_strike` | `[--unit U]+ [--target T]+` |
| `mission_delete` | — |
| `mission_plan_suggest` | `--intent "<text>"` (no scenario/edit-version) |

An invalid waypoint yields `{ "ok": false, "code": "INVALID_ZONE", ... }` (exit `1`).

### Catalog write gate (ProjectAegis.Data)

These drive the `propose → approve → commit` gate; mutating verbs require an existing
SQLite `--db`. See the [Data layer README](../ProjectAegis.Data/README.md#core-invariant-the-write-gate).

| Verb | Required | Optional |
|------|----------|----------|
| `catalog_entity_map` | — | — |
| `catalog_intelligence_run` | — | `--db` |
| `catalog_write_propose` | `--db`, `--platform`, `--sensor`, `--base-pd` | — |
| `catalog_write_approve` | `--db`, `--batch` | `--snapshot-id`, `--release-version` |
| `catalog_import_markdown` | `--db`, `--markdown` | `--max-records`, `--chunk-size` (default from `CmoMarkdownImportProposer.DefaultChunkSize`), `--report-out` |

`catalog_write_approve` commits the batch and binds a snapshot, returning
`releaseVersion`, `snapshotId`, `contentHashSha256`, and `sensorRowCount`.

### Platform Excel round-trip (ADR-011)

| Verb | Required | Optional | Aliases |
|------|----------|----------|---------|
| `platform_export_xlsx` | `--out` | `--db`, `--snapshot` | `--out`=`--output`, `--snapshot`=`--snapshot-id` |
| `platform_import_xlsx` | `--db` | `--in`, `--actor-type` (default `cli`), `--actor-id` (default `mission-editor`) | `--in`=`--input` |
| `platform_diff_xlsx` | — | `--db`, `--base`, `--edited` | `--base`=`--source`, `--edited`=`--in` |

`platform_diff_xlsx` compares exporter output deterministically; an empty baseline
yields `diffCount: 0`.

### OSINT

| Verb | Required | Optional |
|------|----------|----------|
| `osint_search` | — | `--db` (defaults to the committed fixture `data/osint_facts.json`; missing fixture returns an empty, deterministic result) |
| `osint_staging_review` | `--db` | `--approve <batchId>` |

> **Known gap.** The manifest ([`mcp-tools.json`](../../tools/mission-editor/mcp-tools.json))
> also declares `osint_digest`, `osint_list_staging_proposals`,
> `osint_get_proposal_detail`, and `osint_submit_review_decision`. These are **not yet
> wired** in [`Program.cs`](Program.cs) — calling them returns `Unknown command`
> (exit `1`). They are expected to delegate to `OsintDigestRunner` /
> `OsintStagingReviewCommand` when implemented.

## Examples

```bash
# Validate a scenario (exit 0 = export allowed)
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  scenario_validate --path assets/data/scenarios/validation/golden_clean.json

# Add a patrol mission (optimistic concurrency via --edit-version)
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  mission_add_patrol --path scenario.json --edit-version 0 --id m1 \
  --unit BLU-01 --wp 55.0,18.0 --wp 55.5,18.5

# Propose then approve a catalog write-gate batch
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  catalog_write_propose --db catalog.db --platform DDG --sensor SPY --base-pd 0.7
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  catalog_write_approve --db catalog.db --batch <batchId>
```

## Common pitfalls

- **Stale `--edit-version`.** Any mismatch fails with `CONFLICT` and exit `3`, not a
  silent overwrite — always pass the version you just read.
- **Validation gates export.** `scenario_export_brief` and `scenario_simulate_sample`
  run validation first and refuse to proceed if it fails.
- **`--db` must already exist** for mutating catalog/OSINT verbs; a missing file returns
  `database_not_found` (exit `1`).
- **Parse `stdout` as JSON only.** Usage/argument errors go to `stderr`.

## Tests

`src/ProjectAegis.MissionEditor.Cli.Tests/` — including
[`McpToolsManifestTests`](../ProjectAegis.MissionEditor.Cli.Tests/McpToolsManifestTests.cs),
which asserts every required verb is present in the MCP manifest.

```bash
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj -v minimal
```

## See also

- [`tools/mission-editor/README.md`](../../tools/mission-editor/README.md) — MCP host wiring + PowerShell wrappers
- [`mcp-tools.json`](../../tools/mission-editor/mcp-tools.json) — schema-v2 tool bindings
- [ProjectAegis.Data README](../ProjectAegis.Data/README.md) — the catalog write gate this CLI drives
- [ADR-008 — mission-editor validation engine](../../docs/architecture/adr-008-mission-editor-validation-engine.md)
- [ADR-011 — platform editor Excel round-trip](../../docs/architecture/adr-011-platform-editor-excel-roundtrip.md)
