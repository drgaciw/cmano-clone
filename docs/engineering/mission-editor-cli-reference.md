# Mission Editor headless CLI / MCP command reference

> **Engineering reference + runbook.** Consolidated reference for every verb exposed by `ProjectAegis.MissionEditor.Cli` ([`Program.cs`](../../src/ProjectAegis.MissionEditor.Cli/Program.cs)) and how those verbs map to the Unity-MCP tool manifest ([`tools/mission-editor/mcp-tools.json`](../../tools/mission-editor/mcp-tools.json)). The headless-first contract derives from [ADR-010 — Headless-first, command-driven UI](../architecture/adr-010-headless-first-command-driven-ui.md) and the validation gate from [ADR-008 — Mission Editor validation engine](../architecture/adr-008-mission-editor-validation-engine.md). Per-feature deep dives are linked under [Related docs](#related-docs); this page is the index over the whole verb surface.

The CLI is a **single deterministic dispatcher**: `Program.cs` reads `args[0]` as the verb, routes the rest to a command handler, and exits with a status code. There is no daemon, no Unity dependency, and no hidden global state — every invocation is one process, one verb, one JSON line (the same contract Unity-MCP and CI both call).

## Intent

| Goal | How it is met |
|------|---------------|
| One contract for Unity, MCP, agents, and CI | Every mutating/inspecting action is a CLI verb that emits machine-readable JSON; the Unity-MCP host registers the same verbs via [`mcp-tools.json`](../../tools/mission-editor/mcp-tools.json) (ADR-010) |
| No blind catalog writes | `catalog_*`, `platform_import_xlsx`, and `osint_*` verbs stage through the write gate and return a `catalog_write_approve` next-step; they never auto-commit (ADR-006 / req-06) |
| Safe concurrent edits | All `mission_*` mutations require `--edit-version` and fail closed with `CONFLICT` (exit `3`) on a stale version — optimistic concurrency per ADR-008 / TR-editor-004 |
| Validation before export/play | `scenario_validate`, `scenario_export_brief`, and `scenario_simulate_sample` re-run the validation engine fresh and refuse to produce artifacts on blocking findings |
| Deterministic everywhere | Verbs sort output stably, take an explicit `--seed`, and never read the wall clock in the hot path; golden hashes are pinned in CI ([`SimulateSampleGoldenHashes.cs`](../../src/ProjectAegis.MissionEditor.Cli/SimulateSampleGoldenHashes.cs)) |

## How to run

```bash
# General form
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- <verb> [--flag value]...

# No args / unknown verb prints usage and exits 1
dotnet run --project src/ProjectAegis.MissionEditor.Cli
```

From Unity-MCP or PowerShell, the same verbs are reachable through the wrappers in `tools/mission-editor/` (`Invoke-MissionEditorMcp.ps1`, `Invoke-ScenarioValidate.ps1`, `Invoke-ScenarioSimulateSample.ps1`).

## Output contract

Most verbs emit a **single JSON line** via [`McpToolResult`](../../src/ProjectAegis.MissionEditor.Cli/McpToolResult.cs) (camelCase properties):

```jsonc
// success
{ "ok": true, /* verb-specific fields, e.g. editVersion, fileHash, path */ }

// error
{ "ok": false, "code": "CONFLICT", "message": "...", "details": { /* optional */ } }
```

Exceptions to the single-line shape: `scenario_validate` / `scenario_export_brief` emit a `ValidationReport` JSON; `scenario_simulate_sample` and `mission_plan_suggest` emit indented JSON. `scenario_validate` / `scenario_simulate_sample` emit `{"error":"file not found","path":...}` when the scenario file is missing.

### Exit codes

| Code | Meaning |
|------|---------|
| `0` | Success — for gated verbs, export/play/sample allowed |
| `1` | Generic failure — missing required flag, blocking validation findings, or a non-conflict error code |
| `2` | Scenario file not found (`scenario_validate`, `scenario_simulate_sample`) |
| `3` | `CONFLICT` — `--edit-version` did not match the on-disk scenario (optimistic-concurrency miss) |

`McpToolResult.WriteError` returns `3` only for the `CONFLICT` code ([`ScenarioEditVersionGuard.ConflictCode`](../../src/ProjectAegis.Data/Scenario/Authoring/ScenarioEditVersionGuard.cs)); all other error codes return `1`.

### Optimistic concurrency (mutating verbs)

Every `mission_*` verb takes `--edit-version N`. The handler loads the scenario, calls `RequireEditVersion(N)`, and on mismatch throws `ScenarioEditConflictException` → JSON `code: "CONFLICT"`, exit `3`, with `details.currentEditVersion` and `details.fileHash`. On success the response returns the **new** `editVersion` and `fileHash`. The caller loop is: read `editVersion` from the previous response (or from `scenario_create`), pass it to the next mutation, repeat.

## Command catalog

### Scenario lifecycle

| Verb | Required flags | Optional flags | Returns |
|------|----------------|----------------|---------|
| `scenario_create` | `--out <path>` | `--db-ref` (`baltic_patrol`), `--policy-id` (`baltic-patrol-catalog`), `--seed` (`42`) | `{ ok, path, editVersion, fileHash }`; `FILE_EXISTS` if `--out` exists |
| `scenario_validate` | `--path <scenario.json>` | — | `ValidationReport` JSON; exit `0` allowed, `1` blocked, `2` file missing |
| `scenario_export_brief` | `--path` | `--out` (defaults to `<scenario>.brief.md`) | Runs validation first; writes stub brief only when validation passes (`BRIEF_WRITTEN=<path>`) |
| `scenario_simulate_sample` | `--path` | `--ticks` (`32`, min `1`) | Validates, then runs an isolated Baltic harness sample: `{ seed, worldHash, fingerprint, engagementCount, reportHash, ... }`; exit `1` if validation blocks |

### Mission CRUD (require `--edit-version`)

| Verb | Required flags | Repeatable flags | Notes |
|------|----------------|------------------|-------|
| `mission_add_patrol` | `--path --edit-version --id` | `--unit U`, `--wp lat,lon` | Patrol zone needs ≥3 waypoints (`INVALID_ZONE`) and ≥1 unit (`NO_UNITS`) |
| `mission_add_strike` | `--path --edit-version --id` | `--unit U`, `--target T` | — |
| `mission_update_patrol` | `--path --edit-version --id` | `--unit U`, `--wp lat,lon` | Omitting `--wp` leaves the zone unchanged |
| `mission_update_strike` | `--path --edit-version --id` | `--unit U`, `--target T` | — |
| `mission_delete` | `--path --edit-version --id` | — | — |
| `mission_plan_suggest` | `--intent "<text>"` | — | NL keyword stub → suggested `mission_add_*` / `scenario_*` calls (no scenario write) |

Common error codes for mission verbs: `NOT_FOUND` (scenario missing), `CONFLICT` (stale edit-version), `INVALID_ZONE`, `NO_UNITS`, `DUPLICATE_MISSION`.

### Scenario inspection

| Verb | Required flags | Returns |
|------|----------------|---------|
| `scenario_comms_status` | `--policy <id>` | Comms display settings + timeline transitions |
| `scenario_cyber_status` | `--policy <id>` | Cyber abort codes + comms-coupling hints |
| `scenario_near_future_spawn` | `--path` | Gated CCA / hypersonic spawn plan from scenario metadata |

### Catalog & data (staged writes — CLI only, not in the MCP manifest)

| Verb | Required flags | Optional flags | Deep dive |
|------|----------------|----------------|-----------|
| `catalog_intelligence_run` | — | `--db <catalog.db>` | — |
| `catalog_entity_map` | — | — | Prints the catalog entity map |
| `catalog_write_propose` | `--db --platform --sensor --base-pd` | — | Stages a sensor-binding batch |
| `catalog_write_approve` | `--db --batch` | `--snapshot-id`, `--release-version` | Commit path for a staged batch |
| `catalog_import_markdown` | `--db --markdown` | `--max-records`, `--chunk-size` (`500`), `--report-out` | [CMO markdown catalog import](cmo-markdown-catalog-import.md) |

### OSINT discovery → staging

| Verb | Required flags | Optional flags | Deep dive |
|------|----------------|----------------|-----------|
| `osint_search` | — | `--db <fixture.json>` (defaults to `data/osint_facts.json`) | [OSINT catalog staging](osint-catalog-staging.md) |
| `osint_staging_review` | `--db <catalog.db>` | `--approve <batchId>` | List pending proposal batches / approve by id |

### Platform Editor Excel round-trip

| Verb | Required flags | Optional flags | Deep dive |
|------|----------------|----------------|-----------|
| `platform_export_xlsx` | `--out <path>` | `--db` (`baltic_patrol`), `--snapshot <id>` | [Platform Editor Excel round-trip](platform-editor-excel-roundtrip.md) |
| `platform_import_xlsx` | `--db <catalog.db>` | `--in <workbook>`, `--actor-type` (`cli`), `--actor-id` (`mission-editor`) | Stages via the write gate; `nextStep: catalog_write_approve` |
| `platform_diff_xlsx` | — | `--db` (`baltic_patrol`), `--base <path>`, `--edited <path>` | Deterministic diff report |

## CLI verb ↔ MCP tool mapping

The MCP manifest exposes the verbs above, plus **four OSINT alias tools** that delegate to the two real OSINT verbs. When wiring or debugging Unity-MCP, use this table — the MCP tool name is **not** always the CLI verb name:

| MCP tool (`mcp-tools.json`) | Underlying CLI verb | Notes |
|-----------------------------|---------------------|-------|
| `osint_digest` | `osint_search` | Alias — same fixture-driven digest output |
| `osint_list_staging_proposals` | `osint_staging_review` | Alias — list pending proposals |
| `osint_get_proposal_detail` | `osint_staging_review` | Alias — reuses the review proxy |
| `osint_submit_review_decision` | `osint_staging_review --approve <batchId>` | Alias — approve path |
| `scenario_validate` / `scenario_export_brief` | (PowerShell `Invoke-ScenarioValidate.ps1`) | Wrapper script, not a direct `dotnet run` arg |
| `scenario_simulate_sample` | (PowerShell `Invoke-ScenarioSimulateSample.ps1`) | Wrapper script |

> **Gap to know:** the `catalog_*` verbs (`catalog_intelligence_run`, `catalog_entity_map`, `catalog_write_propose`, `catalog_write_approve`, `catalog_import_markdown`) are **CLI-only** — they are not registered in `mcp-tools.json`. Drive them through `dotnet run` (or a custom MCP binding) rather than the current manifest. Keep `mcp-tools.json` and `Program.cs` in sync when adding verbs.

## Runbook: author → validate → simulate

```bash
CLI="dotnet run --project src/ProjectAegis.MissionEditor.Cli --"

# 1. Create a scenario (returns editVersion=0, fileHash)
$CLI scenario_create --out /tmp/baltic.json --policy-id baltic-patrol --seed 42

# 2. Add a patrol mission, passing the current edit-version
$CLI mission_add_patrol --path /tmp/baltic.json --edit-version 0 --id patrol-1 \
  --unit u1 --wp 57,20 --wp 57.1,20.1 --wp 57.2,20.2
#   -> { "ok": true, "missionId": "patrol-1", "editVersion": 1, "fileHash": "..." }

# 3. Validate before exporting / playing (exit 0 = allowed, 1 = blocked)
$CLI scenario_validate --path /tmp/baltic.json

# 4. Deterministic sample run (validates first; reuse golden hashes in CI)
$CLI scenario_simulate_sample --path /tmp/baltic.json --ticks 32
```

### Runbook: staged catalog edit (no auto-commit)

```bash
CLI="dotnet run --project src/ProjectAegis.MissionEditor.Cli --"

# Stage a platform workbook edit, then approve the batch it returns
$CLI platform_import_xlsx --db catalog.db --in edited.xlsx     # -> { ok, staged, nextStep: "catalog_write_approve" }
$CLI catalog_write_approve --db catalog.db --batch <batchId>   # commit
```

## Common pitfalls

- **`CONFLICT` / exit 3 on a mutation** — your `--edit-version` is stale. Read `details.currentEditVersion` from the error JSON (or re-run a read), then retry. Do not blindly retry with the same value.
- **Missing required flag returns exit 1** with a human-readable line on **stderr** (not the JSON line). Parse exit code first, then stdout JSON.
- **`scenario_export_brief` / `scenario_simulate_sample` produce nothing** — they run validation first and bail on blocking findings. Run `scenario_validate` to see why.
- **OSINT/catalog/platform verbs "did nothing"** — they *stage* into the write gate and never commit. Follow the `nextStep` (`catalog_write_approve`) to apply.
- **An MCP tool name has no matching CLI verb** — check the [alias mapping](#cli-verb--mcp-tool-mapping); `osint_digest` etc. delegate to `osint_search` / `osint_staging_review`.

## Related docs

- [OSINT discovery → catalog staging](osint-catalog-staging.md)
- [CMO markdown catalog import](cmo-markdown-catalog-import.md)
- [Platform Editor Excel round-trip](platform-editor-excel-roundtrip.md) · [ADR-011](../architecture/adr-011-platform-editor-excel-roundtrip.md)
- [Doctrine inheritance panel](doctrine-inheritance-panel.md)
- [ADR-008 — Mission Editor validation engine](../architecture/adr-008-mission-editor-validation-engine.md)
- [ADR-010 — Headless-first, command-driven UI](../architecture/adr-010-headless-first-command-driven-ui.md)
- [`tools/mission-editor/README.md`](../../tools/mission-editor/README.md) — PowerShell wrappers and Unity-MCP wiring

## Source map

| Path | Responsibility |
|------|----------------|
| [`src/ProjectAegis.MissionEditor.Cli/Program.cs`](../../src/ProjectAegis.MissionEditor.Cli/Program.cs) | Verb dispatcher + per-verb arg parsing and usage text |
| [`src/ProjectAegis.MissionEditor.Cli/McpToolResult.cs`](../../src/ProjectAegis.MissionEditor.Cli/McpToolResult.cs) | `WriteOk` / `WriteError` JSON + exit-code policy |
| [`src/ProjectAegis.MissionEditor.Cli/CliArgParser.cs`](../../src/ProjectAegis.MissionEditor.Cli/CliArgParser.cs) | Flag parsing (`GetFlag`, `GetRepeated`, `ParseWaypoints`, …) |
| `src/ProjectAegis.MissionEditor.Cli/*Command.cs` | One handler per verb |
| [`src/ProjectAegis.Data/Scenario/Authoring/ScenarioEditVersionGuard.cs`](../../src/ProjectAegis.Data/Scenario/Authoring/ScenarioEditVersionGuard.cs) | Optimistic-concurrency check (`CONFLICT`) |
| [`tools/mission-editor/mcp-tools.json`](../../tools/mission-editor/mcp-tools.json) | Unity-MCP tool manifest (schema v2) |
