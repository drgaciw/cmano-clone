# Mission Editor — Headless CLI / MCP Tools

Headless tools (ADR-008 / ADR-010 / ADR-011) for Unity-MCP hosts, agents, and CI.
**No Unity required** — every verb is a `ProjectAegis.MissionEditor.Cli` command that
emits a single JSON result on stdout and a process exit code.

- Verb dispatch: [`src/ProjectAegis.MissionEditor.Cli/Program.cs`](../../src/ProjectAegis.MissionEditor.Cli/Program.cs)
- MCP bindings (schema v2): [`mcp-tools.json`](mcp-tools.json)
- Generic wrapper: [`Invoke-MissionEditorMcp.ps1`](Invoke-MissionEditorMcp.ps1)

## Invocation

Three equivalent ways to call a verb:

```bash
# 1. Direct dotnet (builds on first run)
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- <verb> [flags]
```

```powershell
# 2. PowerShell wrapper (used by mcp-tools.json; runs from repo root)
.\tools\mission-editor\Invoke-MissionEditorMcp.ps1 -Command <verb> -ExtraArgs --path scenario.json
```

```text
# 3. Unity-MCP host — register the tools in mcp-tools.json; the host maps
#    JSON inputs to the flags below and shells out to the wrapper.
```

`scenario_validate`, `scenario_export_brief`, and `scenario_simulate_sample` also
have dedicated wrappers (`Invoke-ScenarioValidate.ps1`, `Invoke-ScenarioSimulateSample.ps1`).

## Conventions

- **Output** — one JSON object on stdout. Authoring/mission verbs use the compact
  `McpToolResult` envelope (`{ "ok": true, ... }` on success; `{ "ok": false, "code",
  "message", "details"? }` on error). Catalog/platform verbs emit an indented,
  camelCase payload that always includes `ok`.
- **Exit codes** — `0` success; `1` usage error, validation failure, or general error;
  `3` optimistic-concurrency conflict (`code: "CONFLICT"`) on mission edits.
  `scenario_simulate_sample` and `scenario_export_brief` return non-zero when
  validation blocks the action.
- **Flags** — `--flag value` pairs (see [`CliArgParser`](../../src/ProjectAegis.MissionEditor.Cli/CliArgParser.cs)).
  Repeatable flags (`--unit`, `--wp`, `--target`) may be passed multiple times.
  `--wp` takes `lat,lon` (invalid input → `INVALID_ZONE` error).
- **Edit version** — every mission mutation requires `--edit-version N` matching the
  scenario's current version. A stale value returns `CONFLICT` (exit 3) so concurrent
  editors cannot silently clobber each other.

---

## Scenario authoring

| Verb | Required flags | Optional flags | Result |
|------|----------------|----------------|--------|
| `scenario_create` | `--out <path>` | `--db-ref R` (def `baltic_patrol`), `--policy-id P` (def `baltic-patrol-catalog`), `--seed N` (def `42`) | Writes a new canonical `scenario.json`. |
| `scenario_validate` | `--path <scenario.json>` | — | `{ passed, canExport, reportHash, findings[] }`. Exit `0` = export allowed, `1` = blocking findings. |
| `scenario_export_brief` | `--path <scenario.json>` | `--out brief.md` (def `<path>.brief.md`) | Validates first; writes the stub brief **only** when `canExport`. |
| `scenario_simulate_sample` | `--path <scenario.json>` | `--ticks N` (def `32`, min `1`) | Validates, then runs an isolated Baltic harness sample → `{ worldHash, fingerprint, engagementCount }`. Non-zero if validation blocks. |

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  scenario_create --out baltic.json --seed 42
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  scenario_simulate_sample --path baltic.json --ticks 32
```

Scenario `metadata` should carry `seed` and `policyId` (defaults `42` /
`baltic-patrol-catalog` when `dbRef` is Baltic).

## Mission CRUD + planning

All mutations require `--path`, `--edit-version N`, and `--id`. Conflicts → exit `3`.

| Verb | Required | Repeatable | Notes |
|------|----------|------------|-------|
| `mission_add_patrol` | `--path --edit-version --id` | `--unit U`, `--wp lat,lon` | Patrol zone from waypoints. |
| `mission_add_strike` | `--path --edit-version --id` | `--unit U`, `--target T` | |
| `mission_update_patrol` | `--path --edit-version --id` | `--unit U`, `--wp lat,lon` | Zone omitted ⇒ unchanged. |
| `mission_update_strike` | `--path --edit-version --id` | `--unit U`, `--target T` | |
| `mission_delete` | `--path --edit-version --id` | — | |
| `mission_plan_suggest` | `--intent "<text>"` | — | Returns JSON suggestions for the `mission_add_*` verbs. |

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  mission_add_patrol --path baltic.json --edit-version 0 --id p1 \
  --unit BLUE-DDG --wp 57.1,19.8 --wp 57.4,20.1
```

## Scenario status (read-only)

| Verb | Required | Result |
|------|----------|--------|
| `scenario_comms_status` | `--policy <id>` | Comms display settings + timeline transitions. |
| `scenario_cyber_status` | `--policy <id>` | Cyber abort codes + comms-coupling hints. |
| `scenario_near_future_spawn` | `--path <scenario.json>` | Gated near-future spawn plan (CCA / hypersonic). |

## Catalog write gate (propose → approve)

Database edits never auto-commit. An agent **proposes** a staged batch through
`IWriteGate`; a human **approves** it, which commits and binds an immutable snapshot
(ADR-006 data-layer boundary). See `ProjectAegis.Data/WriteGate`.

| Verb | Required | Optional | Result |
|------|----------|----------|--------|
| `catalog_entity_map` | — | — | Entity → table mapping. |
| `catalog_intelligence_run` | — | `--db <catalog.db>` | Runs the catalog intelligence pass. |
| `catalog_write_propose` | `--db --platform --sensor --base-pd <0..1>` | — | Stages one sensor binding → `{ ok, batchId, recordCount }`. Seeds a Baltic DB if `--db` does not exist. |
| `catalog_write_approve` | `--db --batch <batchId>` | `--snapshot-id S`, `--release-version V` | Commits the batch and binds a snapshot → `{ ok, batchId, releaseVersion, snapshotId, contentHashSha256, sensorRowCount }`. Errors if the DB is missing or the batch fails to commit. |
| `catalog_import_markdown` | `--db --markdown <sensor.md>` | `--max-records N`, `--chunk-size 500`, `--report-out report.json` | Stages CMO sensor markdown → `{ parsedCount, approvedCount, quarantinedCount, batchCount, batches[], quarantineReport[]? }`. |

```bash
# Propose, then approve with the returned batchId
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  catalog_write_propose --db catalog.db --platform DDG-51 --sensor SPY-1 --base-pd 0.7
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  catalog_write_approve --db catalog.db --batch <batchId>
```

## Platform Editor — Excel round-trip (S22 / ADR-011)

Headless export/import/diff for full platform configuration on the write gate.
**I/O is canonical text today** — the `.xlsx` adapter is deferred per
[ADR-011](../../docs/architecture/adr-011-platform-editor-excel-roundtrip.md), so an
unedited round-trip must yield an empty diff. Import requires a DB at schema `007+`.

| Verb | Required | Optional | Result |
|------|----------|----------|--------|
| `platform_export_xlsx` | `--out <path>` | `--db <ref>` (def `baltic_patrol`), `--snapshot <id>` (def `cli-s22-export`) | Writes the workbook (default `platform-export.platform.txt`) → `{ ok, verb, snapshotId, outPath, note }`. |
| `platform_import_xlsx` | `--db <catalog.db>` | `--in <workbook>`, `--actor-type` (def `cli`), `--actor-id` (def `mission-editor`) | Exercises `IWriteGate` (no auto-commit) → `{ ok, staged, nextStep: catalog_write_approve }`. |
| `platform_diff_xlsx` | — | `--db <ref>`, `--base <path>`, `--edited <path>` | Deterministic diff → `{ ok, diffCount, note }`. Unedited round-trip ⇒ `diffCount: 0`. |

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  platform_export_xlsx --out platform.platform.txt --snapshot rel-2026-06
# edit offline, then:
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  platform_diff_xlsx --base platform.platform.txt --edited platform.edited.txt
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  platform_import_xlsx --db catalog.db --in platform.edited.txt
# review the staged batch, then approve via catalog_write_approve
```

## OSINT

| Verb | Required | Optional | Result |
|------|----------|----------|--------|
| `osint_search` | — | `--db <fixture.json>` | Reads `data/osint_facts.json` by default (override only if the path exists) → `{ ok, proposals[], logOnlyCount }`. |
| `osint_staging_review` | `--db <catalog.db>` | `--approve <batchId>` | Lists pending OSINT proposals; `--approve` commits one. |

The MCP manifest also exposes `osint_digest`, `osint_list_staging_proposals`,
`osint_get_proposal_detail`, and `osint_submit_review_decision`; these map onto the two
CLI verbs above (see [`mcp-tools.json`](mcp-tools.json)).

## Unity-MCP wiring

Register the tools from [`mcp-tools.json`](mcp-tools.json) in the Unity-MCP host, or
call the wrappers directly — same contract as `design/gdd/agentic-mission-editor.md`
§3.7. The manifest verb set is asserted by
[`McpToolsManifestTests`](../../src/ProjectAegis.MissionEditor.Cli.Tests/McpToolsManifestTests.cs);
add new verbs to both `Program.cs` and `mcp-tools.json` to keep that gate green.
