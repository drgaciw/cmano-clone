<!-- gitnexus:start -->
# GitNexus — Code Intelligence

This project is indexed by GitNexus as **cmano-clone** (11192 symbols, 22977 relationships, 300 execution flows). Use the GitNexus MCP tools to understand code, assess impact, and navigate safely.

> Index stale? Run `node .gitnexus/run.cjs analyze` from the project root — it auto-selects an available runner. No `.gitnexus/run.cjs` yet? `npx gitnexus analyze` (npm 11 crash → `npm i -g gitnexus`; #1939).

## Always Do

- **MUST run impact analysis before editing any symbol.** Before modifying a function, class, or method, run `impact({target: "symbolName", direction: "upstream"})` and report the blast radius (direct callers, affected processes, risk level) to the user.
- **MUST run `detect_changes()` before committing** to verify your changes only affect expected symbols and execution flows. For regression review, compare against the default branch: `detect_changes({scope: "compare", base_ref: "main"})`.
- **MUST warn the user** if impact analysis returns HIGH or CRITICAL risk before proceeding with edits.
- When exploring unfamiliar code, use `query({query: "concept"})` to find execution flows instead of grepping. It returns process-grouped results ranked by relevance.
- When you need full context on a specific symbol — callers, callees, which execution flows it participates in — use `context({name: "symbolName"})`.

## Never Do

- NEVER edit a function, class, or method without first running `impact` on it.
- NEVER ignore HIGH or CRITICAL risk warnings from impact analysis.
- NEVER rename symbols with find-and-replace — use `rename` which understands the call graph.
- NEVER commit changes without running `detect_changes()` to check affected scope.

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

<!-- hindsight:start -->
# Hindsight — Session Memory (local)

Episodic memory on **localhost:8888** complements GitNexus: GitNexus answers *what the code is*; Hindsight answers *what we already tried and decided* across agent sessions.

> Health check: `.\tools\hindsight\Test-HindsightServer.ps1` — if down, use GitNexus only.

## Recommended loop (with GitNexus)

1. `gitnexus://repo/cmano-clone/context` + `gitnexus_impact` before editing symbols.
2. `.\tools\hindsight\Invoke-Hindsight.ps1 -Operation recall -BankId dev-cmano-clone -Query "…"`.
3. Implement (user-approved paths).
4. `retain` summary with `[OUTCOME:]` and symbol names; `FAILED:` for dead ends.
5. `gitnexus_detect_changes()` before commit.

Read **`.claude/skills/hindsight/hindsight-gitnexus/SKILL.md`** for the full checklist.

## Dev memory banks

| Bank | Use |
|------|-----|
| `dev-cmano-clone` | Default repo-wide agent memory |
| `dev-story-{slug}` | Active production story |
| `dev-pr-{number}` | PR / review cycle |
| `balance-tuning` | Trait and attention experiments |

Simulation runtime banks (`agent-*`, `aar-*`, `agent-xp-*`) are populated by `HindsightIntegration` when enabled — see `src/ProjectAegis.Delegation/Hindsight/README.md`.

## Local agents

| Agent | Role |
|-------|------|
| `hindsight-dev-memory-lead` | GitNexus + Hindsight implementation loop |
| `hindsight-aar-analyst` | Post-run AAR over simulation banks |
| `balance-tuning-memory-agent` | Cross-session trait tuning memory |

## Skills

| Task | Skill file |
|------|------------|
| GitNexus + Hindsight together | `.claude/skills/hindsight/hindsight-gitnexus/SKILL.md` |
| Retain / recall / reflect | `.claude/skills/hindsight/hindsight-{retain,recall,reflect}/SKILL.md` |
| Dev bank conventions | `.claude/skills/hindsight/hindsight-dev-memory/SKILL.md` |
| Simulation AAR | `.claude/skills/hindsight/hindsight-aar/SKILL.md` |
| Local server setup | `.claude/skills/hindsight/hindsight-local-setup/SKILL.md` |
| Hub / bank reference | `.claude/skills/hindsight/hindsight-guide/SKILL.md` |
| Team orchestration | `.claude/skills/team-hindsight-dev/SKILL.md` |

## Never

- Do not use Hindsight **recall/reflect** inside simulation `Tick()` or policy code (determinism).
- Do not skip GitNexus impact because Hindsight recalled a prior attempt.
- Do not retain secrets or credentials.

<!-- hindsight:end -->

## Superpowers (global methodology)

[obra/superpowers](https://github.com/obra/superpowers) v5.1.0 is installed for **all agents** on this machine (Cursor, Grok, Claude Code). Install/refresh: `.\tools\install-superpowers.ps1` — see `docs/engineering/superpowers-setup.md`.

**Skill priority:** user instructions → GitNexus rules → Hindsight rules (above) → Project Aegis `.claude/skills/` → Superpowers global skills → defaults.

**Invoke before coding:** `brainstorming` (new work), `systematic-debugging` (bugs), `test-driven-development` (implementation), `writing-plans` + `subagent-driven-development` (multi-step plans). Project specs/plans live in `docs/superpowers/` (local docs, not the plugin).

## Graphite-first PR workflow

This repo is Graphite-initialized (trunk `main`, [`.graphite_repo_config`](.graphite_repo_config)). **Minimize direct GitHub PR operations.**

| Do | Do not (without explicit user fallback approval) |
|----|---------------------------------------------------|
| `gt create`, `gt submit`, `gt submit --stack --no-interactive` | `gh pr create`, `gh pr merge` |
| `gt sync`, `gt restack` for trunk integration | Raw `git push origin <stack-branch>` on tracked stacks |
| Review queue at [app.graphite.com](https://app.graphite.com) | Manual stacked PRs via GitHub UI |

Full guide: [`docs/engineering/graphite-github-substitute-plan.md`](docs/engineering/graphite-github-substitute-plan.md). Read-only `gh pr checks` / `gh pr diff` is fine for CI triage.

## Cursor Cloud specific instructions

Headless **.NET 8** development is the supported Cloud Agent path. Unity Editor 6.3 LTS (`unity/ProjectAegis`) is optional and usually not installed in the VM; use the headless Play Mode harness instead of opening the Editor.

Cloud VMs run `.cursor/cloud-install.sh` on startup via `.cursor/environment.json` (installs .NET SDK 8.0.400 when missing, then `dotnet restore`). See [Cloud agent setup](https://cursor.com/docs/cloud-agent/setup).

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
| Branch protection API 403 | Private repo on free plan — enable required checks manually; see `docs/engineering/ci-and-branch-protection.md` |
| `dotnet format` fails on `Program.cs` | Known pre-existing whitespace in `ProjectAegis.Delegation.Demo/Program.cs`; unrelated to most PRs. |
| Environment install fails immediately | Confirm `.cursor/cloud-install.sh` is executable; run `bash .cursor/cloud-install.sh` manually from repo root. |

### Services

No Docker compose or long-running servers. The “application” is in-process: `dotnet test` or the console demo. Unity-MCP (`http://localhost:8080`) and GitNexus MCP are agent tooling only, not required for CI-style verification.
