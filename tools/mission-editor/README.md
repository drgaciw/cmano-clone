# Mission Editor — Headless MCP Tools

Headless, Unity-free tools that back the Mission Editor MCP surface (ADR-008) and CI.
Every verb is a subcommand of the [`ProjectAegis.MissionEditor.Cli`](../../src/ProjectAegis.MissionEditor.Cli/Program.cs)
console app and is also exposed to the Unity-MCP host via [`mcp-tools.json`](mcp-tools.json).

> **Last updated:** 2026-06-18 — adds catalog write-gate, OSINT, and platform `.xlsx`
> round-trip verbs (S22–S23). For the catalog write/ordering contract behind the
> `catalog_*` and `platform_*` verbs, read
> [Catalog Write-Gate & Determinism](../../docs/engineering/catalog-write-gate-determinism.md).

## How to invoke

There are three equivalent entry points; pick whichever fits the host:

| Entry point | Use when |
|-------------|----------|
| `dotnet run --project src/ProjectAegis.MissionEditor.Cli -- <verb> [flags]` | Local dev / CI, no PowerShell host |
| `tools/mission-editor/Invoke-MissionEditorMcp.ps1 -Command <verb> -ExtraArgs ...` | PowerShell host / Unity-MCP shim |
| Unity-MCP tool registration | Register verbs from [`mcp-tools.json`](mcp-tools.json) (schema v2) |

All verbs print a single JSON object to **stdout** and use exit codes:

| Exit code | Meaning |
|-----------|---------|
| `0` | Success (`ok: true`) |
| `1` | Validation failure, bad arguments, or blocked operation (`ok: false`) |
| `3` | Optimistic-concurrency conflict — `--edit-version` is stale ([`ScenarioEditVersionGuard.ConflictCode`](../../src/ProjectAegis.MissionEditor.Cli/McpToolResult.cs)) |

Error payloads follow the `McpToolResult` shape: `{ "ok": false, "code", "message", "details?" }`.

Run with no arguments to print the full usage banner.

## Scenario lifecycle

### `scenario_create`

Create a new canonical scenario file bound to a DB ref, policy, and seed.

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  scenario_create --out scenario.json [--db-ref baltic_patrol] [--policy-id baltic-patrol-catalog] [--seed 42]
```

### `scenario_validate`

```powershell
.\tools\mission-editor\Invoke-ScenarioValidate.ps1 -ScenarioPath assets\data\scenarios\validation\golden_clean.json
```

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_validate --path <scenario.json>
```

Returns a `ValidationReport` (`passed`, `canExport`, `reportHash`, `findings[]`).
Exit `0` = export allowed; `1` = blocking findings.

### `scenario_export_brief`

Runs validation first, then writes a stub brief **only when `canExport` is true**.
`--out` defaults to `<scenario>.brief.md`.

```powershell
.\tools\mission-editor\Invoke-ScenarioValidate.ps1 -ScenarioPath <scenario.json> -ExportBrief
```

### `scenario_simulate_sample`

Validates first, then runs an isolated Baltic harness sample (no shared sim state).
Returns `worldHash`, `fingerprint`, `engagementCount`; exit `1` if validation blocks.

```powershell
.\tools\mission-editor\Invoke-ScenarioSimulateSample.ps1 -ScenarioPath <scenario.json> -Ticks 32
```

Scenario `metadata` should include `seed` and `policyId` (defaults `42`,
`baltic-patrol-catalog` when `dbRef` is Baltic). `--ticks` defaults to `32`, minimum `1`.

### Status verbs

| Verb | Flags | Returns |
|------|-------|---------|
| `scenario_comms_status` | `--policy <id>` | Comms display settings and timeline transitions |
| `scenario_cyber_status` | `--policy <id>` | Cyber abort codes and comms coupling hints |
| `scenario_near_future_spawn` | `--path <scenario.json>` | Gated near-future spawn plan (CCA / hypersonic) |

## Mission CRUD

All mutating mission verbs take `--path`, `--edit-version` (optimistic concurrency token),
and `--id`. A stale `--edit-version` returns exit code `3`. Invalid patrol waypoints return
an `INVALID_ZONE` error.

| Verb | Required flags | Repeatable flags |
|------|----------------|-------------------|
| `mission_add_patrol` | `--path --edit-version --id` | `--unit U`, `--wp lat,lon` |
| `mission_add_strike` | `--path --edit-version --id` | `--unit U`, `--target T` |
| `mission_update_patrol` | `--path --edit-version --id` | `--unit U`, `--wp lat,lon` |
| `mission_update_strike` | `--path --edit-version --id` | `--unit U`, `--target T` |
| `mission_delete` | `--path --edit-version --id` | — |
| `mission_plan_suggest` | `--intent "<text>"` | — (returns suggestions for `mission_add_*`) |

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  mission_add_patrol --path scenario.json --edit-version 0 --id patrol-1 --unit BLU-DDG --wp 55.0,18.0 --wp 55.5,19.0
```

## Catalog write-gate

These verbs stage and commit `ProjectAegis.Data` catalog rows through `IWriteGate`. They
never touch live tables directly — see the
[Catalog Write-Gate & Determinism runbook](../../docs/engineering/catalog-write-gate-determinism.md)
for the propose → approve → commit contract and ordering invariants.

| Verb | Required flags | Notes |
|------|----------------|-------|
| `catalog_entity_map` | — | Print canonical entity-resolution map |
| `catalog_intelligence_run` | `[--db <catalog.db>]` | Run catalog intelligence pass |
| `catalog_write_propose` | `--db --platform --sensor --base-pd` | Stages one sensor binding; seeds Baltic DB if missing; returns `batchId` (no commit) |
| `catalog_write_approve` | `--db --batch` `[--snapshot-id] [--release-version]` | Commits a staged batch; returns `releaseVersion`, `snapshotId`, `contentHashSha256`, `sensorRowCount` |
| `catalog_import_markdown` | `--db --markdown` `[--max-records N] [--chunk-size 500] [--report-out report.json]` | Bulk-stage sensor rows from CMO markdown (propose only) |

```bash
# propose, then approve the returned batch
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_write_propose --db catalog.db --platform DDG-51 --sensor SPY-1D --base-pd 0.7
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_write_approve --db catalog.db --batch <batchId>
```

## Platform workbook round-trip (`.xlsx`)

Export / import / diff the platform catalog as a workbook (Req 21, ADR-011). The headless
path uses the deterministic `CanonicalTextWorkbookIo` reference format; the production
ClosedXML `.xlsx` adapter is delivered separately (S23-01) — see
[ADR-011](../../docs/architecture/adr-011-platform-editor-excel-roundtrip.md).

| Verb | Flags | Behaviour |
|------|-------|-----------|
| `platform_export_xlsx` | `[--db <catalog.db>] --out <path> [--snapshot <id>]` | Exports via `PlatformWorkbookExporter` + `CanonicalTextWorkbookIo`. No gate side-effects. `--out` defaults to `platform-export.platform.txt` |
| `platform_import_xlsx` | `--db <catalog.db> [--in <workbook>] [--actor-type] [--actor-id]` | Exercises the `IWriteGate` surface (propose-only, **no auto-commit**). `nextStep` points at `catalog_write_approve`. DB must exist with schema 007+ |
| `platform_diff_xlsx` | `[--db <catalog.db>] [--base <path>] [--edited <path>]` | Deterministic diff via `PlatformWorkbookDiff.Compare`; returns `diffCount`. Empty inputs yield `0` |

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- platform_export_xlsx --db catalog.db --out platform.platform.txt --snapshot rel-2026-06
```

> **Constraint:** `platform_import_xlsx` currently stages through the gate only; loading a
> real `.xlsx` into a `PlatformWorkbook` is deferred until the ClosedXML adapter lands.
> Approve staged rows with `catalog_write_approve` to commit.

## OSINT

| Verb | Flags | Returns |
|------|-------|---------|
| `osint_search` | `[--db <fixture.json>]` | Proposals + `logOnlyCount` from `FileOsintConnector` + digest runner (defaults to `data/osint_facts.json`) |
| `osint_staging_review` | `--db <catalog.db> [--approve <batchId>]` | Pending OSINT proposals; with `--approve`, submits a review decision |

## Unity-MCP wiring

Register the verbs from [`mcp-tools.json`](mcp-tools.json) (schema v2) in the Unity-MCP host,
or call the `Invoke-*.ps1` shims directly — the contract matches
`design/gdd/agentic-mission-editor.md` §3.7. Each tool entry declares its `inputSchema`
(required fields and defaults) and a `returns` description; keep the manifest and the CLI
`switch` in [`Program.cs`](../../src/ProjectAegis.MissionEditor.Cli/Program.cs) in sync when
adding a verb.

## Common pitfalls

| Pitfall | Symptom | Fix |
|---------|---------|-----|
| Stale `--edit-version` | Exit code `3`, conflict error | Re-read the scenario and retry with the current edit version |
| `platform_import_xlsx` against a missing/old DB | `gate open failed` error | Point `--db` at an existing catalog with schema 007+ |
| Expecting `platform_import_xlsx` to commit | Rows staged but not in live tables | Run `catalog_write_approve --batch <id>` afterwards |
| `catalog_*` determinism drift | Golden-hash CI failure | Follow the [write-gate determinism runbook](../../docs/engineering/catalog-write-gate-determinism.md) |
| Manifest verb not callable | Unity-MCP host can't find the tool | Add the matching `case` in `Program.cs` and an entry in `mcp-tools.json` |

## See also

- [Catalog Write-Gate & Determinism](../../docs/engineering/catalog-write-gate-determinism.md)
- [ADR-008 — Mission Editor Validation Engine](../../docs/architecture/adr-008-mission-editor-validation-engine.md)
- [ADR-011 — Platform Editor Excel Round-trip](../../docs/architecture/adr-011-platform-editor-excel-roundtrip.md)
