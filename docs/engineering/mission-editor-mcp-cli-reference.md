# Mission Editor — headless MCP CLI verb reference

Complete reference for every verb exposed by `ProjectAegis.MissionEditor.Cli`,
the headless command surface that Unity-MCP, CI, and operators drive without a
Unity Editor. The design rationale for the validation core lives in
[`ADR-008`](../architecture/adr-008-mission-editor-validation-engine.md); the
spreadsheet round-trip lives in
[`platform-editor-excel-roundtrip.md`](platform-editor-excel-roundtrip.md); the
catalog write gate lives in
[`catalog-write-gate-runbook.md`](catalog-write-gate-runbook.md). **This doc is
the flat index**: every verb, its flags, its JSON output, its exit codes, and
where to read more.

All verbs are dispatched from
[`src/ProjectAegis.MissionEditor.Cli/Program.cs`](../../src/ProjectAegis.MissionEditor.Cli/Program.cs);
each verb's logic lives in a sibling `*Command.cs`.

## How to invoke

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- <verb> [flags]
```

Running with no arguments (or an unknown verb) prints usage and exits `1`.
PowerShell wrappers in `tools/mission-editor/` (`Invoke-MissionEditorMcp.ps1`,
`Invoke-ScenarioValidate.ps1`, `Invoke-ScenarioSimulateSample.ps1`) forward to
the same verbs with the same contract.

## Output and exit-code contract

Verbs emit a **single JSON object** on stdout (camelCase property names; most
verbs pretty-print, the shared `McpToolResult` helper emits compact JSON).
Diagnostics for missing required flags go to **stderr**, not stdout.

| Field | Meaning |
|-------|---------|
| `ok` | `true` on success, `false` on a handled error |
| `code` / `error` | Machine-readable failure tag (e.g. `CONFLICT`, `policy_not_found`) |
| `message` | Human-readable failure description |
| `details` | Optional extra context (e.g. `currentEditVersion`, `fileHash` on a conflict) |

Exit codes (see
[`McpToolResult.cs`](../../src/ProjectAegis.MissionEditor.Cli/McpToolResult.cs)):

| Code | Meaning |
|------|---------|
| `0` | Success / validation passed / batch committed |
| `1` | Handled error — bad/missing args, validation failed, not-found, not committed |
| `2` | Missing required input on the status/spawn verbs (returned directly by those commands) |
| `3` | **Optimistic-concurrency conflict** — `editVersion` mismatch (`CONFLICT` code only) |

> Exit `2` vs `1` for bad input is **not uniform**: program-level flag checks
> return `1`, while a few commands (`scenario_comms_status`,
> `scenario_cyber_status`, `mission_plan_suggest`, `scenario_near_future_spawn`)
> return `2`. Treat any non-zero as failure and read `ok`/`code`.

## Optimistic concurrency (mission CRUD)

Every mutating mission verb takes `--edit-version N` and re-checks it against the
scenario's current `editVersion` before writing
([`ScenarioEditVersionGuard`](../../src/ProjectAegis.Data/Scenario/Authoring/ScenarioEditVersionGuard.cs)).
On mismatch the verb does **not** write and returns:

```json
{ "ok": false, "code": "CONFLICT", "message": "editVersion mismatch: expected 2, current 3.",
  "details": { "currentEditVersion": 3, "fileHash": "…" } }
```

with exit code `3`. Re-read the scenario, take the returned `editVersion`, and
retry. Successful mutations return the **new** `editVersion` and `fileHash`, so a
caller can chain edits by feeding each result's `editVersion` into the next call.

---

## Scenario lifecycle

### `scenario_create`
Create a new scenario document (refuses to overwrite an existing file).

| Flag | Required | Default | Notes |
|------|----------|---------|-------|
| `--out` | yes | — | Output `scenario.json` path |
| `--db-ref` | no | `baltic_patrol` | Catalog DB reference |
| `--policy-id` | no | `baltic-patrol-catalog` | Scenario policy preset |
| `--seed` | no | `42` | Deterministic seed (`ulong`) |

Returns `{ ok, path, editVersion, fileHash }`. `FILE_EXISTS` (exit `1`) if the
target already exists.

### `scenario_validate`
Run the ADR-008 validation engine over a scenario.

| Flag | Required | Notes |
|------|----------|-------|
| `--path` | yes | Scenario JSON to validate |

Returns `{ passed, canExport, reportHash, findings[] }`. Exit `0` = export
allowed; `1` = blocking findings (or missing `--path`). For the rule pipeline,
finding codes, sort/hash determinism, and golden fixtures see
[`scenario-validation-engine.md`](scenario-validation-engine.md).

### `scenario_export_brief`
Validate first, then write a stub brief only when `canExport` is true.

| Flag | Required | Default | Notes |
|------|----------|---------|-------|
| `--path` | yes | — | Scenario JSON |
| `--out` | no | `<path>.brief.md` | Brief output path |

Prints `BRIEF_WRITTEN=<path>` on success. If validation fails, nothing is
written and the validation exit code is propagated.

### `scenario_simulate_sample`
Validate, then run an isolated Baltic harness sample (no shared sim state).

| Flag | Required | Default | Notes |
|------|----------|---------|-------|
| `--path` | yes | — | Scenario JSON |
| `--ticks` | no | `32` | Tick count (floored at `1`) |

Scenario `metadata` should carry `seed` and `policyId` (defaults `42` /
`baltic-patrol-catalog` for Baltic `dbRef`).

### `scenario_comms_status`
Snapshot the comms display + transition timeline for a policy (req 19).

| Flag | Required | Notes |
|------|----------|-------|
| `--policy` | yes | Scenario policy id |

Returns `{ ok, policyId, commsDisplay{…}, commsTimeline[] }`. Missing
`--policy` → exit `2`; unknown policy → `{ ok:false, error:"policy_not_found" }`,
exit `1`.

### `scenario_cyber_status`
Cyber/comms abort catalog and policy coupling (req 19).

| Flag | Required | Notes |
|------|----------|-------|
| `--policy` | yes | Scenario policy id |

Returns `{ ok, policyId, cyberAbortCodes[], commsOrderDelayTicks,
commsTransitions, mcpTools[] }`. Same missing/unknown-policy behavior as
`scenario_comms_status`.

### `scenario_near_future_spawn`
List gated CCA/hypersonic spawns derived from scenario metadata (req 09).

| Flag | Required | Notes |
|------|----------|-------|
| `--path` | yes | Scenario JSON with `metadata.nearFutureUnits` |

Returns `{ ok, scenarioPath, maxTechnologyLevel, requested, accepted, spawns[] }`.
Spawns are filtered against `maxTechnologyLevel` using the catalog
`near_future_archetypes.json`. Missing file → exit `2`.

### `mission_plan_suggest`
Keyword-driven NL stub that suggests follow-up verbs (req 11).

| Flag | Required | Notes |
|------|----------|-------|
| `--intent` | yes | Free-text intent (e.g. `"patrol and strike baltic"`) |

Returns `{ ok, intent, suggestions[] }`. Keywords map to verbs: `patrol` →
`mission_add_patrol`, `strike`/`attack` → `mission_add_strike`, `baltic` →
`scenario_create`, `roe`/`doctrine` → `scenario_validate`, `comms`/`ew` →
`scenario_comms_status`, `cyber` → `scenario_cyber_status`,
`cca`/`hypersonic`/`near-future` → `scenario_near_future_spawn`. No match falls
back to `scenario_validate`.

---

## Mission CRUD

All mutating mission verbs share required flags `--path <scenario.json>`,
`--edit-version N`, `--id <missionId>`, return `{ ok, missionId, type,
editVersion, fileHash }` on success, and honor the optimistic-concurrency
contract above.

### `mission_add_patrol`
Add a patrol mission with a patrol zone (**≥ 3 waypoints**).

| Flag | Required | Repeatable | Notes |
|------|----------|------------|-------|
| `--unit` | yes (≥1) | yes | Unit ids |
| `--wp lat,lon` | yes (≥3) | yes | Waypoints; `INVALID_ZONE` if unparsable or `<3` |

### `mission_add_strike`
Add a strike mission.

| Flag | Required | Repeatable | Notes |
|------|----------|------------|-------|
| `--unit` | yes | yes | Unit ids |
| `--target` | yes | yes | Target ids |

### `mission_update_patrol`
Update units and/or the patrol zone of an existing mission. Omitting `--wp`
leaves the zone unchanged; supplying any waypoints replaces the zone (parsed via
the same lat,lon rule).

### `mission_update_strike`
Update units and/or targets of an existing strike mission.

### `mission_delete`
Delete a mission by id.

| Flag | Required | Notes |
|------|----------|-------|
| `--path`, `--edit-version`, `--id` | yes | Standard mutation flags |

Common error codes across mission CRUD: `NOT_FOUND` (scenario missing),
`NO_UNITS`, `INVALID_ZONE`, `DUPLICATE_MISSION`, `CONFLICT` (exit `3`).

---

## Catalog / database intelligence

### `catalog_intelligence_run`
Run the requirement-06 database intelligence agent pipeline (MCP/CI).

| Flag | Required | Notes |
|------|----------|-------|
| `--db` | no | Catalog SQLite path; falls back to the Baltic-patrol fixture if absent |

Returns `{ ok, agents[]{ agentId, passed, findings[] }, mcpTools[] }`. `ok`
mirrors the orchestrator's `Passed`; exit `1` when any agent fails.

### `catalog_entity_map`
Emit the canonical entity → table binding map (no flags).

Returns `{ ok, entities[]{ entity, table, primaryKey, orderBy, dto } }`, ordered
by entity name. Use this to confirm deterministic ordering and runtime DTO
bindings before exporting to DOTS.

### `catalog_write_propose`
Stage a single platform↔sensor `basePd` edit through the write gate.

| Flag | Required | Notes |
|------|----------|-------|
| `--db` | yes | Catalog DB |
| `--platform` | yes | Platform id |
| `--sensor` | yes | Sensor id |
| `--base-pd` | yes | Probability of detection (`≥0`) |

Staging only — nothing commits until `catalog_write_approve`. See the
[catalog write-gate runbook](catalog-write-gate-runbook.md).

### `catalog_write_approve`
Approve and commit a staged batch.

| Flag | Required | Notes |
|------|----------|-------|
| `--db` | yes | Catalog DB |
| `--batch` | yes | Batch id from a propose/import verb |
| `--snapshot-id` | no | Bind the commit to a snapshot |
| `--release-version` | no | Tag the committed release |

### `catalog_import_markdown`
Propose CMO sensor/platform markdown through the write gate in chunked batches.

| Flag | Required | Default | Notes |
|------|----------|---------|-------|
| `--db` | yes | — | Catalog DB |
| `--markdown` | yes | — | CMO markdown source |
| `--max-records` | no | all | Cap parsed records |
| `--chunk-size` | no | `CmoMarkdownImportProposer.DefaultChunkSize` | Records per batch |
| `--report-out` | no | — | Also write the JSON payload to this file |

Returns `{ ok, parsedCount, approvedCount, quarantinedCount, batchCount,
batches[], nextStep }` plus a `quarantineReport[]` when records are rejected.
Imports **stage** batches; approve each with `catalog_write_approve`.

---

## Platform editor (Excel round-trip)

Front door onto the same write gate via a spreadsheet model (ADR-011). Full
workflow, sheet layout, and current delivery status (the `.xlsx` adapter is
deferred — verbs use the canonical text format today):
[`platform-editor-excel-roundtrip.md`](platform-editor-excel-roundtrip.md).

### `platform_export_xlsx`
Export catalog platform data to a workbook model.

| Flag | Required | Notes |
|------|----------|-------|
| `--out` (or `--output`) | yes | Workbook output path |
| `--db` | no | Catalog DB |
| `--snapshot` (or `--snapshot-id`) | no | Bind export to a snapshot |

### `platform_diff_xlsx`
Diff a base workbook against an edited one (deterministic; unedited → empty diff).

| Flag | Required | Notes |
|------|----------|-------|
| `--base` (or `--source`) | no | Base workbook |
| `--edited` (or `--in`) | no | Edited workbook |
| `--db` | no | Catalog DB |

### `platform_import_xlsx`
Stage edited-row changes through the write gate (no auto-commit).

| Flag | Required | Default | Notes |
|------|----------|---------|-------|
| `--db` | yes | — | Catalog DB |
| `--in` (or `--input`) | no | — | Edited workbook |
| `--actor-type` | no | `cli` | Provenance actor type |
| `--actor-id` | no | `mission-editor` | Provenance actor id |

Commit staged batches with `catalog_write_approve`.

---

## OSINT ingestion

End-to-end ingestion design and review flow:
[`osint-ingestion-runbook.md`](osint-ingestion-runbook.md).

### `osint_search`
Run the OSINT digest over a fixture and return ranked proposals.

| Flag | Required | Notes |
|------|----------|-------|
| `--db` | no | Override fixture path; defaults to `data/osint_facts.json` (empty + deterministic if absent) |

Returns `{ ok, proposals[]{ canonicalId, sourceUrl, relevanceScore, snippet },
logOnlyCount }`. Proposals score ≥ the digest threshold (0.65); the rest are
counted in `logOnlyCount`.

### `osint_staging_review`
List pending staged OSINT batches, or approve one.

| Flag | Required | Notes |
|------|----------|-------|
| `--db` | yes | Catalog DB |
| `--approve` | no | Batch id to approve; omit to list pending |

List mode → `{ ok, pending[]{ batchId, recordCount, actorType, approvalState } }`.
Approve mode → `{ ok, batchId, errors }` (`ok` mirrors commit success; exit `1`
if not committed or DB missing).

---

## Unity-MCP wiring

Tool schemas for the Unity-MCP host live in
[`tools/mission-editor/mcp-tools.json`](../../tools/mission-editor/mcp-tools.json)
(schema v2: create, mission CRUD, validate, simulate, plan suggest). The MCP
contract matches `design/gdd/agentic-mission-editor.md` §3.7. Whether called
through Unity-MCP or the PowerShell wrappers, the JSON output and exit-code
contract documented above are identical.
