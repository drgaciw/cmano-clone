# Mission Editor — Headless MCP Tools

ADR-008 headless tools for Unity-MCP and CI. No Unity required.

> **Full verb reference:** every CLI/MCP verb, its flags, JSON output, and exit
> codes are catalogued in
> [`docs/engineering/mission-editor-mcp-cli-reference.md`](../../docs/engineering/mission-editor-mcp-cli-reference.md).
> This README covers the common workflows; the reference is the flat index.

## `scenario_validate`

```powershell
.\tools\mission-editor\Invoke-ScenarioValidate.ps1 -ScenarioPath assets\data\scenarios\validation\golden_clean.json
```

Or:

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_validate --path <scenario.json>
```

Returns JSON (`passed`, `canExport`, `reportHash`, `findings[]`). Exit `0` = export allowed; `1` = blocking findings.

## `scenario_export_brief`

Runs validation first; writes stub brief only when `canExport` is true.

```powershell
.\tools\mission-editor\Invoke-ScenarioValidate.ps1 -ScenarioPath assets\data\scenarios\validation\golden_clean.json -ExportBrief
```

## `scenario_simulate_sample`

Validates first, then runs an isolated Baltic harness sample (no shared sim state).

```powershell
.\tools\mission-editor\Invoke-ScenarioSimulateSample.ps1 -ScenarioPath assets\data\scenarios\validation\golden_clean.json -Ticks 32
```

Scenario `metadata` should include `seed` and `policyId` (defaults: `42`, `baltic-patrol-catalog` when `dbRef` is Baltic).

## Mission CRUD + plan suggest (CLI)

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- mission_plan_suggest --intent "patrol and strike baltic"
```

## Platform editor — Excel round-trip (`platform_export_xlsx` / `platform_import_xlsx` / `platform_diff_xlsx`)

Author catalog platform data by round-tripping a workbook through the staged
write gate (ADR-011). Export → edit → diff → import (stage) → approve.

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- platform_export_xlsx --db <catalog.db> --out <workbook> [--snapshot <id>]
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- platform_diff_xlsx [--db <catalog.db>] [--base <path>] [--edited <path>]
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- platform_import_xlsx --db <catalog.db> [--in <workbook>]
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_write_approve --db <catalog.db> --batch <batchId>
```

Import only **stages** a batch (`IWriteGate.Propose*Batch`); nothing commits without
`catalog_write_approve`. The `.xlsx` adapter is deferred (S23-01) — the verbs
currently use the canonical text reference format. Full workflow, sheet layout,
status, and pitfalls: [`docs/engineering/platform-editor-excel-roundtrip.md`](../../docs/engineering/platform-editor-excel-roundtrip.md).

## Unity-MCP wiring

Register tools from [`mcp-tools.json`](mcp-tools.json) (schema v2 — create, mission CRUD, validate, simulate, plan suggest) in the Unity-MCP host, or call `Invoke-MissionEditorMcp.ps1` / `Invoke-*.ps1` directly — same contract as `design/gdd/agentic-mission-editor.md` §3.7.