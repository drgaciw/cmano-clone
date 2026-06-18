# Mission Editor CLI / MCP command reference

> **Subsystem:** `ProjectAegis.MissionEditor.Cli` (headless command host)
> **Decision of record:** [ADR-010 — Headless-first, command-driven UI](../architecture/adr-010-headless-first-command-driven-ui.md)
> **MCP bindings:** [`tools/mission-editor/mcp-tools.json`](../../tools/mission-editor/mcp-tools.json) (schema v2)
> **Quick-start wrappers:** [`tools/mission-editor/README.md`](../../tools/mission-editor/README.md)

Every authoring, catalog, and OSINT action ships first as a **headless verb** on a single
console entry point. Unity, CI, and the Unity-MCP host all call the *same* binary, so the verb
table below is the canonical contract. This page is the complete index of verbs as wired in
`src/ProjectAegis.MissionEditor.Cli/Program.cs` today, plus the shared invocation, output, and
exit-code conventions. Subsystem-specific behaviour lives in the linked runbooks — this page does
not duplicate it.

## Invocation

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- <verb> [--flag value ...]
```

- The first positional argument is the verb (`args[0]`). With **no arguments** the host prints
  usage and exits `1`.
- An unknown verb prints `Unknown command: <verb>`, the usage block, and exits `1`.
- Flags are parsed by `CliArgParser` as simple `--name value` pairs:
  - `GetFlag` returns the value following the flag, or `null` if absent.
  - `GetRepeated` collects **every** occurrence — repeat `--unit`, `--target`, `--wp` to pass
    lists.
  - Numeric flags fall back to a default when the value is missing or unparsable
    (`GetIntFlag`, `GetDoubleFlag`, `GetULongFlag`); doubles parse with invariant culture.
  - Waypoints (`--wp`) are `lat,lon` pairs; a malformed pair raises an `INVALID_ZONE` error.
- There is no global `--help`; per-verb usage is emitted to **stderr** when required flags are
  missing.

## Output and exit-code contract

Most verbs emit a single JSON object on **stdout** (`McpToolResult`, camelCase properties):

| Shape | When |
|-------|------|
| `{ "ok": true, ... }` | Success payload (verb-specific fields). |
| `{ "ok": false, "code": "...", "message": "...", "details": {...} }` | Handled error via `McpToolResult.WriteError`. |

Exit codes are the machine-readable signal — callers should branch on these, not on parsing text:

| Code | Meaning |
|------|---------|
| `0` | Success (`WriteOk`), or validation passed where the verb gates on validation. |
| `1` | Missing/invalid arguments, validation failure, or a handled error. |
| `3` | **Optimistic-concurrency conflict** — the supplied `--edit-version` does not match the document (`code = "CONFLICT"`, `ScenarioEditVersionGuard`). Re-read the scenario, take the new `editVersion`, and retry. |

> Note: a few commands (e.g. `platform_import_xlsx`, `platform_diff_xlsx`) write a
> `WriteIndented` JSON object with their own `{ ok, verb, ... }` fields rather than the compact
> `McpToolResult` envelope. The `ok` flag and exit code are still authoritative.

## Optimistic concurrency for mission edits

All `mission_*` mutation verbs require `--edit-version N` (a non-negative integer). The host
rejects the call with usage text if it is missing or negative. The value is checked against the
scenario document by `ScenarioEditVersionGuard`; a stale version returns the `CONFLICT` envelope
and **exit `3`**. This is the headless equivalent of an editor "your copy is out of date" guard —
fetch the document, read the current `editVersion`, and reissue the edit.

## Verb index

### Scenario lifecycle

| Verb | Required flags | Optional flags | Result / notes |
|------|----------------|----------------|----------------|
| `scenario_create` | `--out` | `--db-ref`, `--policy-id`, `--seed` | Writes a new canonical `scenario.json`. |
| `scenario_validate` | `--path` | — | JSON `ValidationReport` (`passed`, `canExport`, `reportHash`, `findings[]`). Exit `1` on blocking findings. |
| `scenario_export_brief` | `--path` | `--out` | Runs `scenario_validate` first; writes a stub brief only when `canExport` is true. Defaults `--out` to `<path>.brief.md`; prints `BRIEF_WRITTEN=<path>`. |
| `scenario_simulate_sample` | `--path` | `--ticks` (default `32`, min `1`) | Validates, then runs an isolated Baltic sample harness (no shared sim state). |
| `scenario_comms_status` | `--policy` | — | JSON comms display settings + timeline transitions. |
| `scenario_cyber_status` | `--policy` | — | JSON cyber abort codes + comms coupling hints. |
| `scenario_near_future_spawn` | `--path` | — | JSON gated near-future spawn plan (CCA / hypersonic). |

### Mission CRUD + planning (require `--edit-version`)

| Verb | Required flags | Repeated flags | Notes |
|------|----------------|----------------|-------|
| `mission_add_patrol` | `--path`, `--edit-version`, `--id` | `--unit`, `--wp lat,lon` | Bad waypoint → `INVALID_ZONE`. |
| `mission_add_strike` | `--path`, `--edit-version`, `--id` | `--unit`, `--target` | — |
| `mission_update_patrol` | `--path`, `--edit-version`, `--id` | `--unit`, `--wp lat,lon` | Empty waypoint set leaves the zone unchanged. |
| `mission_update_strike` | `--path`, `--edit-version`, `--id` | `--unit`, `--target` | — |
| `mission_delete` | `--path`, `--edit-version`, `--id` | — | — |
| `mission_plan_suggest` | `--intent` | — | JSON suggestions consumable by the `mission_add_*` verbs. No edit-version (read-only). |

### Catalog (Database Intelligence write gate)

| Verb | Required flags | Optional flags | Notes |
|------|----------------|----------------|-------|
| `catalog_intelligence_run` | — | `--db` | Runs the intelligence pass over the catalog. |
| `catalog_entity_map` | — | — | Prints the entity→table map. |
| `catalog_write_propose` | `--db`, `--platform`, `--sensor`, `--base-pd` | — | Stages a sensor-binding batch (pending). See the [catalog write-gate runbook](catalog-write-gate.md). |
| `catalog_write_approve` | `--db`, `--batch` | `--snapshot-id`, `--release-version` | Commits a pending batch. |
| `catalog_import_markdown` | `--db`, `--markdown` | `--max-records`, `--chunk-size` (default `CmoMarkdownImportProposer.DefaultChunkSize`), `--report-out` | Proposes rows from a `CmoMarkdown` export; does not auto-commit. |

### Platform editor (Excel round-trip)

| Verb | Required flags | Optional flags (aliases) | Notes |
|------|----------------|--------------------------|-------|
| `platform_export_xlsx` | `--out` (`--output`) | `--db`, `--snapshot` (`--snapshot-id`) | Emits canonical workbook text (xlsx adapter deferred per ADR-011). |
| `platform_import_xlsx` | `--db` | `--in` (`--input`), `--actor-type` (default `cli`), `--actor-id` (default `mission-editor`) | Stages via `IWriteGate`; no auto-commit. `nextStep` points to `catalog_write_approve`. |
| `platform_diff_xlsx` | — | `--db`, `--base` (`--source`), `--edited` (`--in`) | JSON diff report (change count + determinism note). |

See the [platform editor Excel round-trip runbook](platform-editor-excel-roundtrip.md) for the
full staging→approve flow.

### OSINT ingestion

| Verb | Required flags | Optional flags | Notes |
|------|----------------|----------------|-------|
| `osint_search` | — | `--db` (fixture path override) | Runs `FileOsintConnector` + `OsintDigestRunner` (threshold `0.65`); JSON `proposals[]` + `logOnlyCount`. Falls back to `data/osint_facts.json`; missing fixture → empty (deterministic). |
| `osint_staging_review` | `--db` | `--approve <batchId>` | Lists pending OSINT proposals; with `--approve`, submits the review decision. |

See the [OSINT ingestion → catalog staging runbook](osint-ingestion-staging.md) for the
score-gate, mapper, and approve semantics.

## CLI verb ↔ MCP tool mapping

The Unity-MCP host registers the tools in [`mcp-tools.json`](../../tools/mission-editor/mcp-tools.json).
Most tools map 1:1 to a CLI verb. The exceptions are OSINT review tools that **alias** existing
verbs (the dedicated `osint_digest` / `osint_list_staging_proposals` / `osint_get_proposal_detail` /
`osint_submit_review_decision` verbs are not yet implemented in `Program.cs`):

| MCP tool | Underlying CLI verb |
|----------|---------------------|
| `osint_digest` | `osint_search` |
| `osint_list_staging_proposals` | `osint_staging_review` |
| `osint_get_proposal_detail` | `osint_staging_review` |
| `osint_submit_review_decision` | `osint_staging_review --approve ${batchId}` |

> **Drift watch:** when adding a verb to `Program.cs`, add the matching tool to `mcp-tools.json`
> (covered by `McpToolsManifestTests`) and a row to this table. When implementing a dedicated
> OSINT verb, update the alias rows above so the mapping stays truthful.

## Worked examples

```bash
# Validate, then simulate a sample if it passes
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  scenario_validate --path assets/data/scenarios/validation/golden_clean.json
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  scenario_simulate_sample --path assets/data/scenarios/validation/golden_clean.json --ticks 64

# Add a patrol with two units and a three-point zone (note repeated flags)
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  mission_add_patrol --path scenario.json --edit-version 3 --id m1 \
  --unit blue-01 --unit blue-02 --wp 56.1,19.2 --wp 56.4,19.9 --wp 56.0,20.5

# Stage an OSINT batch, review it, then approve through the catalog gate
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- osint_search --db data/osint_facts.json
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- osint_staging_review --db catalog.db
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- osint_staging_review --db catalog.db --approve <batchId>
```

## Common pitfalls

- **Forgetting `--edit-version`** on a `mission_*` verb prints usage and exits `1`; a stale value
  exits `3` (`CONFLICT`). Always read the current `editVersion` from the document first.
- **Unquoted repeated values** — pass each list item as its own `--flag value`; there is no
  comma-joined list form except for `--wp` (which is itself `lat,lon`).
- **Branching on text** instead of exit codes / `ok` — the JSON payload shape varies per verb;
  the exit code and `ok` flag are the stable contract.
- **Catalog/platform/OSINT verbs never auto-commit.** They stage pending batches; promotion is a
  separate `catalog_write_approve` (or `osint_staging_review --approve`) step.
- **Missing `--db`** on catalog/platform verbs that require it prints a per-verb error to stderr
  and exits `1`; the database must already exist with schema `007+`.
