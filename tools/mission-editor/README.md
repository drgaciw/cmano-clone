# Mission Editor — Headless MCP Tools

ADR-008 headless tools for Unity-MCP and CI. No Unity required.

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

## Unity-MCP wiring

Register tools from [`mcp-tools.json`](mcp-tools.json) (schema v2 — create, mission CRUD, validate, simulate, plan suggest) in the Unity-MCP host, or call `Invoke-MissionEditorMcp.ps1` / `Invoke-*.ps1` directly — same contract as `design/gdd/agentic-mission-editor.md` §3.7.