<!-- gitnexus:start -->
# GitNexus — Code Intelligence

This project is indexed by GitNexus as **cmano-clone** (20496 symbols, 38203 relationships, 300 execution flows). Use the GitNexus MCP tools to understand code, assess impact, and navigate safely.

> Index stale? Run `node .gitnexus/run.cjs analyze` from the project root — it auto-selects an available runner. No `.gitnexus/run.cjs` yet? `npx gitnexus analyze` (npm 11 crash → `npm i -g gitnexus`; #1939).

## Always Do

- **MUST run impact analysis before editing any symbol.** Before modifying a function, class, or method, run `impact({target: "symbolName", direction: "upstream"})` and report the blast radius (direct callers, affected processes, risk level) to the user.
- **MUST run `detect_changes()` before committing** to verify your changes only affect expected symbols and execution flows. For regression review, compare against the default branch: `detect_changes({scope: "compare", base_ref: "main"})`.
- **MUST warn the user** if impact analysis returns HIGH or CRITICAL risk before proceeding with edits.
- When exploring unfamiliar code, use `query({search_query: "concept"})` to find execution flows instead of grepping. It returns process-grouped results ranked by relevance.
- When you need full context on a specific symbol — callers, callees, which execution flows it participates in — use `context({name: "symbolName"})`.
- For security review, `explain({target: "fileOrSymbol"})` lists taint findings (source→sink flows; needs `analyze --pdg`).

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

## Buildkite CI agents (local)

Official [Buildkite skills](https://github.com/buildkite/skills) are vendored under `.claude/skills/buildkite-*` and `.cursor/skills/buildkite-*`.

| Agent | Use for |
|-------|---------|
| `buildkite-ci-lead` | Default for Buildkite CI; owns `.buildkite/` and `tools/buildkite/` |
| `buildkite-pipelines-engineer` | Pipeline YAML, plugins, step design |
| `buildkite-migration-engineer` | CI migration from GitHub Actions or other providers |
| `buildkite-operations-engineer` | `bk`, API, preflight, build triage |

Refresh: `bash tools/buildkite/install-buildkite-skills.sh` — see [`docs/engineering/buildkite-agent-skills.md`](docs/engineering/buildkite-agent-skills.md).

MCP: `buildkite` → `https://mcp.buildkite.com/mcp` in `.cursor/mcp.json`.

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

**verification-before note for trunk resolution (e.g. "trunk out of date" block):** Before gt sync / restack / submit when blocked: (1) GitNexus pre (search_tool then list_repos + detect_changes(scope=staged) + impact(CatalogWriteGate upstream summaryOnly)); (2) full gates RUN+READ: dotnet build, dotnet test full (0f >=1229), ReplayGolden 6/6, PlayModeSmoke 18/18, hash grep `17144800277401907079`, ZERO DelegationBridge grep, gt status; (3) stage ONLY S66/S67 payload files per smoke-sprint-66-closeout.md list; (4) re-verif post each gt step. Cite production/release-train-scope-boundary-2026-06-24.md + this AGENTS + graphite plan. All RUN outputs READ before proceed. (See production/qa/smoke-sprint-66-closeout.md resolution section for commands.)

### Hermes Agent skills

When running under Hermes Agent, load these skills for Graphite/PR workflows:

| Skill | When to use |
|-------|-------------|
| `graphite-pr-review` | Review/audit Graphite draft PR queues via GitHub CLI/API when browser auth blocks the web loop |
| `github-workflows` | GitHub CLI operations — PRs, issues, code review, CI/CD |
| `reviewer-checklist` | Adversarial review of engineering output |

Load with: `/skill graphite-pr-review` or `hermes -s graphite-pr-review`

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
| Test (full suite, ~1215 tests; hybrid layout retained) | `dotnet test ProjectAegis.sln -v minimal` |
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

## Learned User Preferences

- Use project-local skill `.cursor/skills/git-commit/SKILL.md` when asked to commit, push to `main`, merge branches, or clean worktrees on this repo; when the user says **include active worktrees**, commit the main repo and all dirty sprint stack worktrees under `.worktrees/` on their respective branches.
- Release v1.0 completion means the shippable Baltic vertical slice, not MVP-done on all 21 requirement tracker rows.
- Prefer detailed sprint planning through Release; post–Release trains use numbered sprints (S49+) with locked roadmap snapshots in `docs/reports/future-sprint-roadpmap-YYYYMMDD.md`.
- Do not edit attached `.cursor/plans/` files when implementing plans — treat them as read-only reference.
- Run in-sprint parallel agent tracks via isolated git worktrees and `production/agentic/local-cloud-agent-routing.md` (local: Editor evidence/coordinator; cloud: code/tests/hygiene).
- When subagent API limits block full-sprint orchestrators, split into per-task parallel workers instead of retrying one large orchestrator.
- Maintain stable alias `docs/reports/future-sprint-roadpmap.md` pointing at the latest dated roadmap snapshot (edit dated files, not the alias body).
- Treat v1.0 / Spirit 1 vertical slice as a closed milestone — no ongoing gap-remediation program.
- Internal engineering trains (RC1, S49–S56, Baltic v2) exclude E7 commercial launch scope unless explicitly scoped.

## Learned Workspace Facts

- Production stage is **Release** (`production/stage.txt`; S48 gate PASS 2026-06-20; RC1 cut).
- **S39–S72** programs COMPLETE through commercial launch prep (RC1 S48, MVP exit S56, Baltic v2 S64, release train S68, launch prep S72).
- Headless test baseline is **≥1232** solution tests (ReplayGolden 6/6 v2, C2 proxy 18/18; monotonic per closeout).
- Canonical forward roadmap is dated `docs/reports/future-sprint-roadpmap-*.md` with stable alias `docs/reports/future-sprint-roadpmap.md`; current forward program is **S73–S80 Baltic v3** (`docs/reports/roadmap-execute-plan-062526.01.md`).
- Production Baltic v2 replay hash **`17144800277401907079`** must stay preserved unless an ADR explicitly changes it.
- Baltic v3 uses isolated **`baltic-v3-*`** scenario policies and replay goldens; v2 hash invariant unchanged.
- Baltic v3 baseline OOB: **u1, hostile-1, ucav-blue, ucav-red** (surface + UCAV only; no subs/air beyond UCAV).
- Baltic v2 content on disk includes 10 `baltic-v2-*` scenario policies and 9 v2 replay goldens (post–S64 baseline).
- Sprint stack worktrees use `.worktrees/` under the parent repo path (`/home/username01/cmano-clone/.worktrees/`).
- Exclude from commits: `.cursor/hooks/`, `.pi/settings.json`, `.polly/` (local agent/tooling config).
- Baltic v3 policies use contact-triggered dual-side ASuW/AAA (`mission.triggers`, `MissionContactTriggerRuntime`) with ROE escalation to Weapons Free on recon contact detection.
- `DelegationBridge.cs` remains zero-touch through Release v1.
