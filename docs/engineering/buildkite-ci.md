# Buildkite CI

> **Last updated:** 2026-06-13  
> **Replaces:** `.NET CI`, `Graphite CI`, `Post-Merge CI`, and Gitleaks in GitHub Actions  
> **Graphite workflow:** [graphite-github-substitute-plan.md](./graphite-github-substitute-plan.md)

## Overview

Primary blocking CI runs on **Buildkite hosted Linux agents** using repo-committed steps in [`.buildkite/pipeline.yml`](../../.buildkite/pipeline.yml). Build and scan steps invoke bash wrappers under `tools/buildkite/` (SDK and gitleaks are installed on the agent when missing).

**Agent skills:** Official Buildkite skills and project agents are documented in [buildkite-agent-skills.md](./buildkite-agent-skills.md). Refresh skills with `bash tools/buildkite/install-buildkite-skills.sh`.

| Step | When | Purpose |
|------|------|---------|
| Graphite CI optimizer | PR builds (when token set) | Skips redundant stack runs via [graphite-ci-buildkite-plugin](https://github.com/withgraphite/graphite-ci-buildkite-plugin) |
| .NET build and test | All builds (unless optimizer skips) | `restore` → Release `build` → full `test` → replay golden suite → PlayMode smoke |
| Gitleaks | All builds | Secret scan (moved from `gitnexus-security.yml`; `soft_fail: true`) |
| Baltic replay golden | `main` only | Post-merge `ReplayGolden*` filter |
| GitNexus PR analysis | Pull requests | `analyze` + `detect_changes`; Buildkite annotation (+ optional `gh pr comment`) |
| GitNexus reindex | `main` only | Knowledge graph refresh; skips doc-only pushes (parity with GH workflow) |

Shell entrypoints (parity with local dev):

- [`tools/buildkite/agent-dotnet-ci.sh`](../../tools/buildkite/agent-dotnet-ci.sh) — bootstrap .NET SDK + [`dotnet-ci.sh`](../../tools/buildkite/dotnet-ci.sh)
- [`tools/buildkite/agent-gitleaks.sh`](../../tools/buildkite/agent-gitleaks.sh) — bootstrap gitleaks binary
- [`tools/buildkite/agent-baltic-replay.sh`](../../tools/buildkite/agent-baltic-replay.sh) — bootstrap .NET + [`baltic-replay.sh`](../../tools/buildkite/baltic-replay.sh)
- [`tools/buildkite/agent-gitnexus-pr-analysis.sh`](../../tools/buildkite/agent-gitnexus-pr-analysis.sh) — bootstrap Node + GitNexus + [`gitnexus-pr-analysis.sh`](../../tools/buildkite/gitnexus-pr-analysis.sh)
- [`tools/buildkite/agent-gitnexus-reindex.sh`](../../tools/buildkite/agent-gitnexus-reindex.sh) — bootstrap Node + GitNexus + [`gitnexus-reindex.sh`](../../tools/buildkite/gitnexus-reindex.sh)
- [`tools/buildkite/agent-gitnexus-wiki.sh`](../../tools/buildkite/agent-gitnexus-wiki.sh) — manual wiki job (requires `OPENAI_API_KEY`; not in default pipeline)
- [`tools/buildkite/dotnet-ci.sh`](../../tools/buildkite/dotnet-ci.sh) — core dotnet commands (also used by agents)
- [`tools/buildkite/baltic-replay.sh`](../../tools/buildkite/baltic-replay.sh)
- [`tools/buildkite/agent-bootstrap-gitnexus.sh`](../../tools/buildkite/agent-bootstrap-gitnexus.sh) — Node 20 + global `gitnexus` CLI
- [`tools/verify-ci-local.ps1`](../../tools/verify-ci-local.ps1) (Windows local gate)

## One-time Buildkite setup (human)

Complete these steps **before** merging the migration PR, or coordinate cutover so `main` is not briefly unprotected.

### 1. Create pipeline

1. [Buildkite](https://buildkite.com) → **New pipeline** → connect GitHub repo `drgaciw/cmano-clone`
2. **Pipeline slug:** recommend `cmano-clone` (status context becomes `buildkite/cmano-clone`)
3. **Pipeline settings → GitHub:**
   - Build pull requests: **on**
   - Build branches: **main** (optional: `stack/*`)
   - **Skip builds with existing commits:** on (recommended for future Graphite Option 1 optimizer pipeline)
4. **Pipeline settings → Steps:** choose **Read pipeline configuration from the repository**
   - Path: `.buildkite/pipeline.yml`

### 2. Secrets

Buildkite → Pipeline → **Environment**:

| Variable | Source |
|----------|--------|
| `GRAPHITE_CI_OPTIMIZER_TOKEN` | Copy from local [`.env.example`](../../.env.example) → `.env`, or create at [Graphite CI settings](https://app.graphite.com/settings/ci). Paste into Buildkite pipeline **Environment** (not into committed files). |
| `OPENAI_API_KEY` | Optional — only for manual **GitNexus wiki** builds (`agent-gitnexus-wiki.sh`). |
| `GITNEXUS_FORCE_REINDEX` | Optional — set to `1` to reindex on doc-only `main` pushes. |
| `GITNEXUS_WIKI_PUSH` | Optional — set to `1` to `git push` wiki output from Buildkite (default: generate only). |

Commit [`.env.example`](../../.env.example) with an **empty** value only. Keep the real token in `.env` (gitignored) and Buildkite env.

### 3. Graphite CI Optimizations

1. Graphite dashboard → **CI Optimizations** → **Add new** for this repo
2. Configure bottom-of-stack / top-of-stack rules per [Graphite stacking + CI docs](https://graphite.com/docs/stacking-and-ci)
3. The pipeline uses **Option 2** (inline optimizer step). Upgrade to **Option 1** (separate optimizer pipeline) later if you want clearer skip visibility on GitHub.

### 4. Branch protection

GitHub → **Settings → Branches → `main`**:

1. **Require status checks to pass**
2. **Require branches to be up to date** (recommended)
3. **Remove** old contexts: `build_test`, `build`
4. **Add** required check: `buildkite/cmano-clone` (verify exact string from first green Buildkite build on a PR)
5. **Do not** enable “Dismiss stale pull request approvals when new commits are pushed” — keep [graphite-dismiss-stale-approvals.yml](../../.github/workflows/graphite-dismiss-stale-approvals.yml)

Or apply via CLI when tier allows:

```powershell
.\tools\apply-branch-protection.ps1
```

Uses [.github/branch-protection.main.json](../../.github/branch-protection.main.json).

### 5. Retire duplicate GitHub secrets (optional)

After cutover, `GRAPHITE_CI_OPTIMIZER_TOKEN` in GitHub Actions is unused unless you keep a GH Actions optimizer workflow. Remove from GitHub Actions secrets when comfortable.

## What stays on GitHub Actions

| Workflow | Why |
|----------|-----|
| [graphite-dismiss-stale-approvals.yml](../../.github/workflows/graphite-dismiss-stale-approvals.yml) | PR approval governance (Graphite-compatible) |
| [gitnexus-security.yml](../../.github/workflows/gitnexus-security.yml) | CodeQL + Dependency Review (GitHub Security tab) — **still active** |
| [gitnexus-reindex.yml](../../.github/workflows/gitnexus-reindex.yml) | **Disabled** (`if: false`) — use Buildkite `gitnexus-reindex` step |
| [gitnexus-wiki.yml](../../.github/workflows/gitnexus-wiki.yml) | **Disabled** — use manual Buildkite `agent-gitnexus-wiki.sh` |
| [gitnexus-pr-analysis.yml](../../.github/workflows/gitnexus-pr-analysis.yml) | **Disabled** — use Buildkite `gitnexus-pr` step |
| [unity-ci.yml](../../.github/workflows/unity-ci.yml) | Manual Unity Editor tests (`UNITY_LICENSE`) |

### GitNexus on Buildkite (mirrors GitHub workflows)

| Buildkite script | GitHub workflow |
|------------------|-----------------|
| [`gitnexus-pr-analysis.sh`](../../tools/buildkite/gitnexus-pr-analysis.sh) | [gitnexus-pr-analysis.yml](../../.github/workflows/gitnexus-pr-analysis.yml) |
| [`gitnexus-reindex.sh`](../../tools/buildkite/gitnexus-reindex.sh) | [gitnexus-reindex.yml](../../.github/workflows/gitnexus-reindex.yml) |
| [`gitnexus-wiki.sh`](../../tools/buildkite/gitnexus-wiki.sh) | [gitnexus-wiki.yml](../../.github/workflows/gitnexus-wiki.yml) |

**Manual wiki build (Buildkite UI → New build):**

```bash
bash tools/buildkite/agent-gitnexus-wiki.sh
```

Set `OPENAI_API_KEY` in pipeline Environment. Add `GITNEXUS_WIKI_PUSH=1` only if the build should commit and push wiki output.

**Local parity (requires Node 20 + `npm install -g gitnexus`):**

```bash
bash tools/buildkite/gitnexus-reindex.sh
bash tools/buildkite/gitnexus-pr-analysis.sh
```

## Cutover automation (Desktop Commander / local)

```powershell
# Opens Buildkite, Graphite CI, and GitHub branch/secret settings; checks bk + branch protection
.\tools\buildkite\setup-cutover.ps1

# Non-interactive (no browser, skip long test run)
.\tools\buildkite\setup-cutover.ps1 -SkipVerify -NoBrowser
```

Requires `winget install Buildkite.CLI` for `bk` checks. Set `BUILDKITE_API_TOKEN` to list pipelines via API.

## Verification

After pipeline is live:

```powershell
# Local parity (should pass before relying on CI)
.\tools\verify-ci-local.ps1

# PR checks (read-only)
gh pr checks
```

Checklist:

- [ ] PR build: optimizer → build → gitleaks → all green
- [ ] Stacked PR: upper stack PR may skip via optimizer (see Graphite / Buildkite UI)
- [ ] `main` push: Baltic replay step runs
- [ ] `gh pr checks` shows `buildkite/cmano-clone` (or your slug)
- [ ] GitHub still runs dismiss-stale-approvals and CodeQL (`gitnexus-security.yml`)
- [ ] GitNexus CLI workflows disabled on GitHub Actions (reindex / PR analysis / wiki)
- [ ] PR build: GitNexus impact annotation appears (soft-fail step)
- [ ] `main` push with code changes: GitNexus reindex runs (doc-only push skips)

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| Pipeline not found | Confirm `.buildkite/pipeline.yml` on default branch; re-save pipeline “read from repo” setting |
| Graphite optimizer always runs full CI | Token missing/wrong in Buildkite env; step is skipped when unset; optimizer fails open when present |
| First build slow on hosted agents | Expected: `agent-dotnet-ci.sh` downloads .NET SDK 8.0.400 on cold agents |
| `CmoCatalogExportTests` fails with `node` not found | Checked-in golden at `tools/cmano-db-crawler/fixtures/sensor-mini-export.golden.json` (copied to test output); live `node` export is optional |
| Build fails ~1m with no `:hammer:` log | Graphite optimizer on **main** pipeline can `pipeline upload --replace` with empty steps; merge branch `.buildkite/pipeline.yml` to `main` or disable `GRAPHITE_CI_OPTIMIZER_TOKEN` until then |
| Agent has dotnet 6/7 on PATH | `agent-bootstrap-dotnet.sh` installs 8.0.400 when major &lt; 8 |
| Required check name mismatch | Copy exact context from GitHub PR checks tab after first build |
| Gitleaks false positive | Add allowlist in `.gitleaks.toml` if needed (not present today) |

## Phase 2 (not implemented)

- Separate Graphite optimizer pipeline (Option 1)
- Unity pipeline on mac/self-hosted agent
