# Unity remediation baseline — 2026-08-17

Measurement pass only. Nothing was fixed.

| Field | Value |
|---|---|
| Timestamp | 2026-08-17T09:04:19-05:00 |
| Hostname | Ubuntu-Host |
| Repo | `/home/username01/projects/active/cmano-clone/cmano-clone` |
| Branch | `main` (up to date with `origin/main`) |
| HEAD | `fa4db95c2aafd914b03e4ba938d8519c56011561` |
| `dotnet --version` | **8.0.422** (SDKs installed: 8.0.400 and 8.0.422; `global.json` pins 8.0.400 with `rollForward: latestMajor`) |
| Stash list | empty (this clone) |
| Graphite | `gt status` delegated to `git status`; on `main`, no stacked PR state |

## Verdict: **BASELINE GREEN**

Full suite is above the ≥1638 / 0-failure floor. PlayMode, ReplayGolden 6/6, hash, and DelegationBridge hotpath all pass. Do not treat this as permission to land `.gitattributes` or apply housekeeping — those are gated separately.

## GATES

| Gate | Expected | Actual | Result |
|---|---|---|---|
| `dotnet build ProjectAegis.sln` | 0 errors, 0 warnings | 0 errors, 0 warnings (24.56s) | **PASS** |
| `dotnet test ProjectAegis.sln` | ≥1638, 0 failures | **1924** passed, 0 failed, 0 skipped | **PASS** |
| PlayModeSmokeHarnessTests | ≥20/20 | **21/21** (0 failed) | **PASS** |
| ReplayGolden (canonical suite) | 6/6 | **6/6** (`FullyQualifiedName~ReplayGoldenSuiteTests`) | **PASS** |
| ReplayGolden (broad `--filter ReplayGolden`) | n/a (too wide) | 17/17 — includes isolated Baltic fixtures, not the 6/6 catalog | recorded |
| Hash `17144800277401907079` | present | **1613** hits (4 in `*.cs`, including `PinnedWorldHash`) | **PASS** |
| DelegationBridge hotpath | ZERO src diff | `git diff -- src/` has no `DelegationBridge`; `DelegationBridge.cs` unstaged | **PASS** |

### Full-suite breakdown (1924)

| Assembly | Passed |
|---|---|
| ProjectAegis.Data.Tests | 682 |
| ProjectAegis.Delegation.UnityAdapter.Tests | 391 |
| ProjectAegis.Delegation.Tests | 388 |
| ProjectAegis.Sim.Tests | 326 |
| ProjectAegis.MissionEditor.Cli.Tests | 113 |
| ProjectAegis.Data.Excel.Tests | 24 |

## git status --short (at measurement)

```
 M .gitignore
 M AGENTS.md
 M CLAUDE.md
 M unity/ProjectAegis/Packages/manifest.template.json
?? .gitattributes.NEW
?? docs/engineering/unity-ACCELERATOR-SETUP.md
?? docs/engineering/unity-TERMINAL-TODO.md
?? docs/engineering/unity-UPGRADE-RUNBOOK.md
?? docs/engineering/unity-remediation-CHANGESET.md
?? tools/accelerator-docker-compose.yml
?? tools/unity-housekeeping.sh
?? unity/ProjectAegis/Assets/Scripts/Runtime/ScenarioEditorShellHost.cs.meta
?? unity/ProjectAegis/ProjectSettings/EditorSettings.asset.NEW
```

`AGENTS.md` / `CLAUDE.md` are GitNexus index-count refreshes that also deleted the two-checkout `repo:` disambiguation note. Do not include them in the remediation stack.

## Notes (not gate failures)

- SDK used was 8.0.422, not the exact 8.0.400 pin. Both SDKs are installed; `rollForward: latestMajor` selected 8.0.422.
- Broad `--filter ReplayGolden` is not the 6/6 gate. Use `FullyQualifiedName~ReplayGoldenSuiteTests`.
- Housekeeping dry-run is **INVESTIGATE** (9 tracked SKIP files). This baseline does not unblock `--apply`.
