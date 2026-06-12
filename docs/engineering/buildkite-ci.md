# Buildkite CI

> **Last updated:** 2026-06-12  
> **Replaces:** `.NET CI`, `Graphite CI`, `Post-Merge CI`, and Gitleaks in GitHub Actions  
> **Graphite workflow:** [graphite-github-substitute-plan.md](./graphite-github-substitute-plan.md)

## Overview

Primary blocking CI runs on **Buildkite hosted Linux agents** using Docker. Pipeline definition is committed at [`.buildkite/pipeline.yml`](../../.buildkite/pipeline.yml).

| Step | When | Purpose |
|------|------|---------|
| Graphite CI optimizer | PR builds | Skips redundant stack runs via [graphite-ci-buildkite-plugin](https://github.com/withgraphite/graphite-ci-buildkite-plugin) |
| Gitleaks | All builds | Secret scan (moved from `gitnexus-security.yml`) |
| .NET build and test | All builds (unless optimizer skips) | `restore` → Release `build` → full `test` → replay golden suite → PlayMode smoke |
| Baltic replay golden | `main` only | Post-merge `ReplayGolden*` filter |

Shell entrypoints (parity with local dev):

- [`tools/buildkite/dotnet-ci.sh`](../../tools/buildkite/dotnet-ci.sh)
- [`tools/buildkite/baltic-replay.sh`](../../tools/buildkite/baltic-replay.sh)
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
| `GRAPHITE_CI_OPTIMIZER_TOKEN` | Same token as former GitHub Actions secret; create at [Graphite CI settings](https://app.graphite.com/settings/ci) |

Do not commit secret values to the repo.

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
| [gitnexus-security.yml](../../.github/workflows/gitnexus-security.yml) | CodeQL + Dependency Review (GitHub Security tab) |
| [gitnexus-reindex.yml](../../.github/workflows/gitnexus-reindex.yml) | Knowledge graph reindex on `main` |
| [gitnexus-wiki.yml](../../.github/workflows/gitnexus-wiki.yml) | Release-triggered wiki + `git push` |
| [gitnexus-pr-analysis.yml](../../.github/workflows/gitnexus-pr-analysis.yml) | PR blast-radius comments |
| [unity-ci.yml](../../.github/workflows/unity-ci.yml) | Manual Unity Editor tests (`UNITY_LICENSE`) |

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

- [ ] PR build: optimizer → gitleaks → dotnet → all green
- [ ] Stacked PR: upper stack PR may skip via optimizer (see Graphite / Buildkite UI)
- [ ] `main` push: Baltic replay step runs
- [ ] `gh pr checks` shows `buildkite/cmano-clone` (or your slug)
- [ ] GitHub still runs dismiss-stale-approvals, CodeQL, GitNexus workflows

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| Pipeline not found | Confirm `.buildkite/pipeline.yml` on default branch; re-save pipeline “read from repo” setting |
| Graphite optimizer always runs full CI | Token missing/wrong in Buildkite env; optimizer fails open (CI still runs) |
| Docker pull slow on first build | Expected on hosted agents; SDK image `mcr.microsoft.com/dotnet/sdk:8.0.400` |
| Required check name mismatch | Copy exact context from GitHub PR checks tab after first build |
| Gitleaks false positive | Add allowlist in `.gitleaks.toml` if needed (not present today) |

## Phase 2 (not implemented)

- Separate Graphite optimizer pipeline (Option 1)
- GitNexus reindex on Buildkite
- Unity pipeline on mac/self-hosted agent
