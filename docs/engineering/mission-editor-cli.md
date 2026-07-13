# Mission Editor CLI & MCP tools — reference

Operational reference for the **headless Mission Editor** — the command surface in
[`src/ProjectAegis.MissionEditor.Cli`](../../src/ProjectAegis.MissionEditor.Cli) that
authors, validates, simulates, and publishes scenario documents, and that browses and
extends the catalog. Every verb runs without Unity (ADR-008 / ADR-010, *headless-first,
command-driven*) and is also registered as an **MCP tool** for the Unity-MCP host.

For a five-verb quickstart see [`tools/mission-editor/README.md`](../../tools/mission-editor/README.md).
This page is the full surface: conventions, the JSON/exit-code contract, the
optimistic-concurrency workflow, the complete verb list, and troubleshooting.

---

## How to invoke

Two equivalent entry points — the raw CLI and the PowerShell MCP wrapper:

```bash
# Raw CLI (repo root). Everything after `--` is passed to the tool.
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- <verb> [--flag value]...

# List all verbs and their usage (run with no args)
dotnet run --project src/ProjectAegis.MissionEditor.Cli
```

```powershell
# Generic MCP wrapper (used by mcp-tools.json): -Command <verb>, -ExtraArgs passthrough
.\tools\mission-editor\Invoke-MissionEditorMcp.ps1 -Command mission_plan_suggest -ExtraArgs --intent "patrol and strike baltic"
```

The wrapper simply `cd`s to the repo root and runs `dotnet run --project ... -- <verb> <ExtraArgs>`,
so exit codes and stdout are identical to the raw CLI.

---

## Conventions (apply to every verb)

### Output is a single JSON line

Tool results are serialized as one camelCase JSON object on **stdout** (see
[`McpToolResult`](../../src/ProjectAegis.MissionEditor.Cli/McpToolResult.cs)). Consumers
should parse the last stdout line.

- **Success:** the payload carries `ok: true` plus verb-specific fields.
- **Error:** `{ "ok": false, "code": "<CODE>", "message": "<human text>", "details": { ... } }`
  where `details` is present only when the verb has structured context to add.

Argument-validation failures (a missing required flag) are written to **stderr** as plain
text, not the JSON envelope, and return exit `1`.

### Exit codes

| Exit | Meaning |
|------|---------|
| `0`  | Success (`ok: true`), or a validation verb that passed / export allowed |
| `1`  | Usage error, not-found, or a blocking failure (validation findings, invalid input) |
| `3`  | **Edit-version conflict** — `code: "CONFLICT"` (optimistic-concurrency mismatch) |

Exit `3` is reserved specifically for the concurrency conflict path
(`ScenarioEditVersionGuard.ConflictCode`), so callers can distinguish "someone else
changed the file" from ordinary failures.

### Optimistic concurrency — `--edit-version`

Every **mutating** scenario verb requires `--edit-version N`
([TR-editor-004](../architecture/adr-008-mission-editor-validation-engine.md)). The value
must equal the scenario's current `metadata.editVersion`; if it does not, the mutation is
rejected with `code: "CONFLICT"` and exit `3`, and the error `details` report the
`currentEditVersion` and `fileHash` so the caller can re-read and retry.

Read-modify-write loop:

1. `scenario_create` (or any successful mutation) returns the new `editVersion` in its
   JSON payload.
2. Pass that value as `--edit-version` on the **next** mutation.
3. Each success bumps `editVersion` and returns the new value plus the new `fileHash`.

```jsonc
// mission_add_patrol success
{ "ok": true, "missionId": "patrol-1", "type": "Patrol", "editVersion": 2, "fileHash": "…" }

// mission_add_patrol with a stale --edit-version
{ "ok": false, "code": "CONFLICT",
  "message": "editVersion mismatch: expected 1, current 2.",
  "details": { "currentEditVersion": 2, "fileHash": "…" } }
```

### Undo and the validation gate

- Mission mutations capture an undo snapshot before writing; `scenario_undo --path P
  --edit-version N` reverts the last change (and is itself version-guarded).
- Export/publish are **gated on validation**: `scenario_export_brief`, `scenario_export`,
  and `scenario_publish` run the validator first and refuse to emit output when the
  scenario has blocking findings.

---

## Verb reference

Runnable verbs (from the dispatch switch in
[`Program.cs`](../../src/ProjectAegis.MissionEditor.Cli/Program.cs)). Flags in `[brackets]`
are optional; `flag+` may be repeated.

### Scenario lifecycle

| Verb | Required flags | Notes |
|------|----------------|-------|
| `scenario_create` | `--out` | `[--db-ref] [--policy-id] [--seed]`; defaults `baltic_patrol` / `baltic-patrol-catalog` / `42`. Errors `FILE_EXISTS` if the target already exists. |
| `scenario_validate` | `--path` | Emits `ValidationReport` (`passed`, `canExport`, `reportHash`, `findings[]`). Exit `1` on blocking findings. |
| `scenario_export` | `--path` | Validate + prepare export package (manifest + report hash). |
| `scenario_export_brief` | `--path` | `[--out]`; validates first, writes a stub brief only when export is allowed. |
| `scenario_publish` | `--path` | Validate + export publish gate. |
| `scenario_simulate_sample` | `--path` | `[--ticks N]` (default `32`); runs an isolated Baltic harness sample; reports `worldHash`, fingerprint, engagement count. |
| `scenario_undo` | `--path`, `--edit-version` | Reverts the last captured mutation. |
| `scenario_event_trace` | `--path` | `[--event ID]`; structured event-trigger trace. |
| `scenario_comms_status` | `--policy` | JSON comms display settings / timeline transitions. |
| `scenario_cyber_status` | `--policy` | JSON cyber abort codes + comms coupling hints. |
| `scenario_near_future_spawn` | `--path` | Gated near-future spawn plan (CCA / hypersonic). |
| `scenario_ai_scaffold` | — | `[--brief]`; NL-planning scaffold stub (no LLM in the blocking path). |
| `scenario_migrate_preview` | — | `[--path] [--target]`; previews a DB migration with snapshot rollback demo. |
| `scenario_umpire_snapshot` | — | `[--path]`; adjudication-workspace freeze/step/inject/resume demo. |

### Mission authoring (all mutating → require `--edit-version`)

| Verb | Required flags | Notes |
|------|----------------|-------|
| `mission_add_patrol` | `--path`, `--edit-version`, `--id` | `--unit U+`, `--wp lat,lon+`; needs **≥3** waypoints (`INVALID_ZONE`) and ≥1 unit (`NO_UNITS`). |
| `mission_update_patrol` | `--path`, `--edit-version`, `--id` | `[--unit U]+ [--wp lat,lon]+`. |
| `mission_add_strike` | `--path`, `--edit-version`, `--id` | `--unit U`, `--target T+`. |
| `mission_update_strike` | `--path`, `--edit-version`, `--id` | `[--unit U]+ [--target T]+`. |
| `mission_add_ferry` | `--path`, `--edit-version`, `--id` | `[--unit U]+ --destination D`. |
| `mission_update_ferry` | `--path`, `--edit-version`, `--id` | `[--unit U]+ [--destination D]`. |
| `mission_add_support` | `--path`, `--edit-version`, `--id`, `--role` | `--role Tanker\|AEW\|EW`, `[--unit U]+ [--wp lat,lon]+`. |
| `mission_delete` | `--path`, `--edit-version`, `--id` | Removes a mission. |
| `mission_plan_suggest` | `--intent` | Heuristic intent→mission suggestions (stub; no LLM). |

### Catalog (read + extend-only write gate)

| Verb | Required flags | Notes |
|------|----------------|-------|
| `catalog_platform_browse` | — | `[--db] [--max-records N]`. |
| `catalog_entity_map` | — | Entity→table map. |
| `catalog_intelligence_run` | — | `[--db]`; catalog intelligence pass. |
| `catalog_dependency_graph` | — | `[--db]`; supports `--help`. |
| `catalog_kill_chain_report` | — | `[--db]`; supports `--help`. |
| `catalog_link_report` | — | `[--db]`; supports `--help`. |
| `catalog_release_diff` | `--db`, `--from`, `--to` | Diff two release versions. Prefer the named flags — see troubleshooting. |
| `catalog_write_propose` | `--db`, `--platform`, `--sensor`, `--base-pd` | Stages a proposal batch (**extend-only** write gate). |
| `catalog_write_approve` | `--db`, `--batch` | `[--snapshot-id] [--release-version] [--enable-balance-drift]`; commits a staged batch. |
| `catalog_import_markdown` | `--db`, `--markdown` | `[--entity …] [--map-baltic-platform-ids] [--max-records N] [--chunk-size N] [--report-out P]`. |
| `catalog_mount_loadout_quarantine_triage` | `--db` | `[--entity] [--propose-json P] [--apply]`; **dry-run unless `--apply`**. |

> **Catalog writes are extend-only.** `catalog_write_propose` → `catalog_write_approve` is
> a two-step propose/approve flow; it adds rows and never mutates existing write paths (see
> `CatalogWriteGate` in [`AGENTS.md`](../../AGENTS.md#hard-invariants--never-break-these)).

### Platform Excel round-trip (ADR-011)

| Verb | Required flags | Notes |
|------|----------------|-------|
| `platform_export_xlsx` | `--out` | `[--db] [--snapshot] [--tl-tier TL-0..TL-5] [--io closedxml\|canonical]`. |
| `platform_import_xlsx` | `--db` | `[--in] [--io …]`; stages via the write gate (no auto-commit) → next step `catalog_write_approve`. |
| `platform_diff_xlsx` | — | `[--db] [--base] [--edited] [--io …]`; deterministic diff report. |

`--io closedxml` (the default for `.xlsx` paths) uses the ClosedXML adapter in
[`ProjectAegis.Data.Excel`](../../src/ProjectAegis.Data.Excel/README.md); `--io canonical` uses the
dependency-free text adapter for fully headless runs. Both satisfy the same round-trip contract.

### OSINT

| Verb | Required flags | Notes |
|------|----------------|-------|
| `osint_search` | — | `[--db]`; falls back to the committed fixture `data/osint_facts.json`; returns proposals + `logOnlyCount`. |
| `osint_staging_review` | `--db` | `[--approve batchId]`; list / approve staged OSINT proposals. |

---

## Examples

```bash
# 1. Create a scenario (note the editVersion in the output)
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  scenario_create --out scratch/demo.scenario.json
# {"ok":true,"path":"scratch/demo.scenario.json","editVersion":1,"fileHash":"…"}

# 2. Add a patrol mission (edit-version must match the current value)
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  mission_add_patrol --path scratch/demo.scenario.json --edit-version 1 --id patrol-1 \
  --unit u1 --wp 57.0,20.0 --wp 57.5,20.5 --wp 57.2,20.8
# {"ok":true,"missionId":"patrol-1","type":"Patrol","editVersion":2,"fileHash":"…"}

# 3. Validate before publishing
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  scenario_validate --path scratch/demo.scenario.json

# 4. Undo the last change (version-guarded)
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  scenario_undo --path scratch/demo.scenario.json --edit-version 2
```

---

## MCP integration

The Unity-MCP host registers these verbs from
[`tools/mission-editor/mcp-tools.json`](../../tools/mission-editor/mcp-tools.json)
(`schemaVersion: 2`). Each tool declares an `inputSchema`; the `${placeholder}` tokens in
its `args` map to schema properties, and the wrapper scripts forward them to the CLI:

- `Invoke-MissionEditorMcp.ps1` — generic `-Command <verb> -ExtraArgs …` passthrough.
- `Invoke-ScenarioValidate.ps1`, `Invoke-ScenarioSimulateSample.ps1` — verb-specific.

A few MCP tool names alias the same CLI verb — e.g. `osint_list_staging_proposals`,
`osint_get_proposal_detail`, and `osint_submit_review_decision` all invoke
`osint_staging_review`, and `osint_digest` invokes `osint_search`. The canonical verb list
is validated in
[`McpToolsManifestTests`](../../src/ProjectAegis.MissionEditor.Cli.Tests/McpToolsManifestTests.cs)
(every required verb must appear in the manifest with an `inputSchema`).

---

## Troubleshooting & pitfalls

| Symptom | Cause / fix |
|---------|-------------|
| Exit `3`, `code: "CONFLICT"` | `--edit-version` is stale. Re-read the current `editVersion` (from `details.currentEditVersion` or by loading the file) and retry. |
| `code: "INVALID_ZONE"` on patrol | Patrol zones need **≥3** `--wp lat,lon` waypoints; each must be `lat,lon` with parseable numbers. |
| `code: "FILE_EXISTS"` on create | `scenario_create --out` refuses to overwrite; choose a new path or delete the file. |
| `catalog_release_diff` misreads the DB path as a version | Always pass `--from`/`--to` explicitly. The positional fallback skips the `--db` value, but relying on positionals is error-prone. |
| Export/brief/publish returns exit `1` with no file | Validation is blocking — run `scenario_validate --path …` and clear the findings first. |
| `event_add` / `event_validate` "unknown command" | Those command classes exist but are **not wired** into the CLI dispatch or the MCP manifest yet; they are not runnable verbs today. |
| Missing-flag error is plain text, not JSON | Argument-validation errors go to **stderr** and return `1`; parse stdout for the JSON envelope. |

---

## Related docs

| Topic | Doc |
|-------|-----|
| Quickstart (validate / brief / simulate / plan) | [`tools/mission-editor/README.md`](../../tools/mission-editor/README.md) |
| Validation engine design | [`adr-008-mission-editor-validation-engine.md`](../architecture/adr-008-mission-editor-validation-engine.md) |
| Headless-first command-driven UI | [`adr-010-headless-first-command-driven-ui.md`](../architecture/adr-010-headless-first-command-driven-ui.md) |
| Platform Excel round-trip | [`adr-011-platform-editor-excel-roundtrip.md`](../architecture/adr-011-platform-editor-excel-roundtrip.md) |
| Requirement (Agentic Mission Editor) | [`Game-Requirements/requirements/11-Agentic-Mission-Editor.md`](../../Game-Requirements/requirements/11-Agentic-Mission-Editor.md) |
| Build / test / CI-parity commands | [`../../README.md`](../../README.md), [`../../AGENTS.md`](../../AGENTS.md) |
| Local editor setup / ENOSPC | [`local-dev-environment.md`](local-dev-environment.md) |
