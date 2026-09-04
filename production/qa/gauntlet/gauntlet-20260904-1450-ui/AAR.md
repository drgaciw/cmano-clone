# Gauntlet UI-track AAR — gauntlet-20260904-1450-ui

**Team:** `/team-qa-gauntlet --mode ui` → `/qa-gauntlet-ui`
**Verdict:** PASS

| Gate | Result |
|------|--------|
| Headless UI/C2/Presentation suite | **145/145** passed, 0 failed (floor ≥118) |
| IA oracles (`UiIa`) | **11/11** passed, 0 failed (floor ≥11) |
| ReplayGolden filter | **17/17** passed, 0 failed (floor ≥6) |
| C2 signoff ×5 | **PASS** each (`C2PlayModeSignoffBatchRunner PASS:` present) |
| Hash / DelegationBridge | **OK** — hash hits **26**; `DelegationBridge.cs` diff **0** bytes |
| Remediation / UCA | `UNITY_MCP_READY` stripped from player scripting defines (S122-00 MCP pin side effect). Existing test `Player_scripting_defines_do_not_enable_unity_mcp_ready` went red then green. UCA **PASS** — ADR-010/007/001; Editor-only MCP; no `DelegationBridge` hotpath. |

**Manual UAT:** not in scope — use `/team-qa` / `/smoke-check`.
**Ladder:** not run — use `/qa-gauntlet` / `--mode ladder`.

## Run identity

| Field | Value |
|-------|-------|
| RUN_ID | `gauntlet-20260904-1450-ui` |
| RUN_DIR | `production/qa/gauntlet/gauntlet-20260904-1450-ui/` |
| git SHA | `7279aecad16fc9872f0461a60fbf4f7fe856f71a` |
| branch | `sprint-123-integration` |
| captured | 2026-09-04 |
| Unity Editor | 6000.3.22f1 batchmode `-nographics` after interactive Editor was stopped (one-lock) |
| `dotnet` | `$HOME/.dotnet` |

## Step 1 — UI/C2/Presentation suite

Filter (skill-exact):

`FullyQualifiedName~PlayModeSmoke|FullyQualifiedName~Presentation|FullyQualifiedName~C2|FullyQualifiedName~MapPlaceholder|FullyQualifiedName~MapCanvas|FullyQualifiedName~UnityCsharpScriptHygiene|FullyQualifiedName~Panel|FullyQualifiedName~MessageLog|FullyQualifiedName~SensorC2|FullyQualifiedName~UiIa`

First run: **144 passed / 1 failed** (`Player_scripting_defines_do_not_enable_unity_mcp_ready` — player `scriptingDefineSymbols` contained `UNITY_MCP_READY` on every platform, including `Standalone: SENTIS_ANALYTICS_ENABLED;APP_UI_EDITOR_ONLY;UNITY_MCP_READY`). Root cause: MCP pin wrote the define into player settings. Fix: restore HEAD player defines (`Standalone: SENTIS_ANALYTICS_ENABLED;APP_UI_EDITOR_ONLY` only).

Retry:

```
Passed!  - Failed:     0, Passed:   145, Skipped:     0, Total:   145
```

Log: `ui/dotnet-ui-suite.log`

Dedicated `FullyQualifiedName~UiIa`:

```
Passed!  - Failed:     0, Passed:    11, Skipped:     0, Total:    11
```

Log: `ui/dotnet-uiia.log`

## Step 2 — ReplayGolden

```
Passed!  - Failed:     0, Passed:    17, Skipped:     0, Total:    17
```

Log: `ui/replay-golden.log`

## Step 3 — C2 Play Mode signoff ×5

| Method | Scenario | Result |
|--------|----------|--------|
| `RunBatch` | `baltic-patrol-comms` | PASS |
| `RunClassifyBatch` | `baltic-patrol-classify` | PASS |
| `RunDoctrineBatch` | `baltic-patrol-mission-roe` | PASS |
| `RunImportBatch` | `baltic-patrol-classify` | PASS |
| `RunBeginExecutionBatch` | `baltic-patrol-classify` | PASS |

Logs: `ui/signoff/<Method>.log`. Each contains `C2PlayModeSignoffBatchRunner PASS:`.

## Step 4 — invariants

Log: `ui/invariants.txt`

## unity-csharp-architect — PR finish (UCA-M4)

**Checklist:** `production/agentic/skills/unity-csharp-architect/checklists/pr-finish.md`
**ADRs:** ADR-010, ADR-007, ADR-001 (never ADR-018)

**Verdict:** PASS

**Evidence:**
- Presentation reads: `LastSensorC2` / `LastUnitDetail` / binders; smoke host `Start()` calls public `RunTick` once so paused Play has a snapshot — `DelegationBridge.cs` untouched
- Command path: N/A (policy seed + selection presentation; no new order path)
- Assemblies: UnityAdapter seeder + Unity Runtime host; plugin copy `./tools/copy-delegation-assemblies.sh` (14 DLLs, netstandard2.1)
- MB / DI: `SimplePlayModeSimHost.Start` only; no Tick method edit
- Editor: `UNITY_MCP_READY` not in player defines; MCP remains Editor-session
- Tests: PlayModeSmokeHarness 25+; SmokeContactsListHonesty + host contract; UI suite 145; UiIa 11; ReplayGolden 17
- Plugins: netstandard2.1 copy documented
