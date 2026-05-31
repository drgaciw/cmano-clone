<!-- gitnexus:start -->
# GitNexus — Code Intelligence

This project is indexed by GitNexus as **cmano-clone** (1824 symbols, 3872 relationships, 102 execution flows). Use the GitNexus MCP tools to understand code, assess impact, and navigate safely.

> If any GitNexus tool warns the index is stale, run `npx gitnexus analyze` in terminal first.

## Always Do

- **MUST run impact analysis before editing any symbol.** Before modifying a function, class, or method, run `gitnexus_impact({target: "symbolName", direction: "upstream"})` and report the blast radius (direct callers, affected processes, risk level) to the user.
- **MUST run `gitnexus_detect_changes()` before committing** to verify your changes only affect expected symbols and execution flows.
- **MUST warn the user** if impact analysis returns HIGH or CRITICAL risk before proceeding with edits.
- When exploring unfamiliar code, use `gitnexus_query({query: "concept"})` to find execution flows instead of grepping. It returns process-grouped results ranked by relevance.
- When you need full context on a specific symbol — callers, callees, which execution flows it participates in — use `gitnexus_context({name: "symbolName"})`.

## Never Do

- NEVER edit a function, class, or method without first running `gitnexus_impact` on it.
- NEVER ignore HIGH or CRITICAL risk warnings from impact analysis.
- NEVER rename symbols with find-and-replace — use `gitnexus_rename` which understands the call graph.
- NEVER commit changes without running `gitnexus_detect_changes()` to check affected scope.

## Resources

| Resource | Use for |
|----------|---------|
| `gitnexus://repo/cmano-clone/context` | Codebase overview, check index freshness |
| `gitnexus://repo/cmano-clone/clusters` | All functional areas |
| `gitnexus://repo/cmano-clone/processes` | All execution flows |
| `gitnexus://repo/cmano-clone/process/{name}` | Step-by-step execution trace |

## CLI

| Task | Read this skill file |
|------|---------------------|
| Understand architecture / "How does X work?" | `.claude/skills/gitnexus/gitnexus-exploring/SKILL.md` |
| Blast radius / "What breaks if I change X?" | `.claude/skills/gitnexus/gitnexus-impact-analysis/SKILL.md` |
| Trace bugs / "Why is X failing?" | `.claude/skills/gitnexus/gitnexus-debugging/SKILL.md` |
| Rename / extract / split / refactor | `.claude/skills/gitnexus/gitnexus-refactoring/SKILL.md` |
| Tools, resources, schema reference | `.claude/skills/gitnexus/gitnexus-guide/SKILL.md` |
| Index, status, clean, wiki CLI commands | `.claude/skills/gitnexus/gitnexus-cli/SKILL.md` |

<!-- gitnexus:end -->

## Cursor Cloud specific instructions

Headless **.NET 8** development is the supported Cloud Agent path. Unity Editor 6.3 LTS (`unity/ProjectAegis`) is optional and usually not installed in the VM; use the headless Play Mode harness instead of opening the Editor.

Cloud VMs run `dotnet restore ProjectAegis.sln` on startup via `.cursor/environment.json` (see [Cloud agent setup](https://cursor.com/docs/cloud-agent/setup)).

### Prerequisites

- **.NET SDK 8.0.400** (see `global.json`). If `dotnet` is missing, install to `~/.dotnet` and add it to `PATH` in your shell profile (no root symlink required):

  ```bash
  curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version 8.0.400
  export PATH="$HOME/.dotnet:$PATH"
  # Persist: append the export line to ~/.bashrc (or ~/.zshrc)
  ```

- **Node.js** is only needed for `tools/cmano-db-crawler/` (reference data), not for build/test.

### Common commands (repo root)

| Task | Command |
|------|---------|
| Restore | `dotnet restore ProjectAegis.sln` |
| Build | `dotnet build ProjectAegis.sln` |
| Test (full suite, 68 tests) | `dotnet test ProjectAegis.sln -v minimal` |
| Play Mode smoke (headless) | `dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests` |
| Run delegation demo | `dotnet run --project src/ProjectAegis.Delegation.Demo` |
| Format check | `dotnet format --verify-no-changes` (may report pre-existing whitespace in `ProjectAegis.Delegation.Demo/Program.cs`) |

See `README.md` and `unity/ProjectAegis/PLAYMODE-SMOKE.md` for Unity Editor setup (`./tools/init-unity-project.ps1` requires PowerShell).

### Recommended verification (after code changes)

Run from repo root before marking work complete:

```bash
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests
```

### Troubleshooting

| Symptom | Fix |
|---------|-----|
| `dotnet: command not found` | Run the install block under Prerequisites; confirm `dotnet --version` prints `8.0.400`. |
| Missing `Microsoft.NETCore.App` runtime | Install SDK 8.0.400 (not runtime-only); `dotnet --list-sdks` should include `8.0.400`. |
| Play Mode smoke not found | Use the full project path in the table above; filter name is `PlayModeSmokeHarnessTests`. |
| `dotnet format` fails on `Program.cs` | Known pre-existing whitespace in `ProjectAegis.Delegation.Demo/Program.cs`; unrelated to most PRs. |

### Services

No Docker compose or long-running servers. The “application” is in-process: `dotnet test` or the console demo. Unity-MCP (`http://localhost:8080`) and GitNexus MCP are agent tooling only, not required for CI-style verification.
