# Buildkite CI

> **Last updated:** 2026-07-13
> **Replaces:** `.NET CI`, `Graphite CI`, `Post-Merge CI`, and Gitleaks in GitHub Actions
> **Graphite workflow:** [graphite-github-substitute-plan.md](./graphite-github-substitute-plan.md)

## Overview

Primary blocking CI runs on **Buildkite hosted Linux agents** using repo-committed steps in [`.buildkite/pipeline.yml`](../../.buildkite/pipeline.yml). Build and scan steps invoke bash wrappers under `tools/buildkite/` (SDK and gitleaks are installed on the agent when missing).

**Agent skills:** Official Buildkite skills and project agents are documented in [buildkite-agent-skills.md](./buildkite-agent-skills.md). Refresh skills with `bash tools/buildkite/install-buildkite-skills.sh`.

### Live pipeline (authoritative)

The committed [`.buildkite/pipeline.yml`](../../.buildkite/pipeline.yml) blocking gate is:

| Step | When | Purpose |
|------|------|---------|
| `:hammer: Build and test` | All builds | **`agent-dotnet-ci.sh`** → `dotnet-ci.sh` — Release restore/build/test + Replay/C2 filters |
| Gitleaks | All builds | Secret scan (`soft_fail: true` — blanket) |
| Baltic replay golden | `main` only | Post-merge `ReplayGolden*` filter |
| GitNexus PR analysis | Pull requests | `analyze` + `detect_changes`; annotation; `soft_fail: true` (blanket) |
| GitNexus reindex | `main` only | Knowledge graph refresh; **`soft_fail: true` (blanket)** — not yet scoped to exit 75 |

PR #263 ships **groundwork scripts and docs only**. It does **not** change the live gate
to a build/test split, parallelism, native cache, or exit-75-only soft_fail. Treat
`pipeline.yml` as source of truth over any historical optimization notes below.

### PR #263 groundwork + bisect (not the live gate)

| Build | What | Result |
|-------|------|--------|
| #535 | `cache:` + `key`/`{{ checksum }}` | ~3s **upload reject** |
| #541 | `cache:` `paths`/`name`/`size` only | ~3s **upload reject** |
| #552 | no cache; retries/timeouts/analytics/plugin | ~2s **upload reject** |
| #554 | simplified YAML + `depends_on` + `parallelism:4` | uploaded; failed ~1m29s |
| #558 | single-agent new build/shard/annotate scripts | uploaded; failed ~1m32s |
| #559 | main-identical gate (`agent-dotnet-ci.sh`) | **passed** ~1m28s |
| #571–#573 | tip after catalog revert / pipeline pin | **FAILURE** (logs unavailable without token) |

Native `cache:` volumes are **not** used (see Caching). Graphite CI optimizer remains removed.

**Groundwork only (not referenced by `pipeline.yml` today):** `agent-dotnet-build.sh`,
`dotnet-build.sh`, `run-tests-sharded.sh` (honors `BUILDKITE_PARALLEL_JOB{,_COUNT}`),
`annotate-test-summary.sh`. Keep until a follow-up with Buildkite job logs re-enables
optimizations in the phased order below.

### Caching

**Native step-level `cache:` is intentionally NOT used** in `.buildkite/pipeline.yml`.

Evidence (PR #263):

| Build | Config | Result |
|-------|--------|--------|
| #535 | `cache:` with `key` + `{{ checksum }}` (CircleCI/GHA syntax) | Failed ~3s — pipeline upload rejection |
| #541 | `cache:` with only `paths` / `name` / `size` (official volume syntax) | Failed ~3s — still upload rejection |
| Main PRs | No `cache:` at all | SUCCESS |

Hosted [cache volumes](https://buildkite.com/docs/agent/buildkite-hosted/cache-volumes)
are a **Pro/Enterprise** feature and must be enabled on the cluster. Until a human
confirms **Agents → cluster → Cache Storage** is active for this org, do not re-add
native `cache:` blocks — they reject pipeline upload before any job starts.

When re-enabling cache, set `NUGET_PACKAGES: ".nuget/packages"` in pipeline `env` so a
volume or [cache plugin](https://github.com/buildkite-plugins/cache-buildkite-plugin)
can mount a workspace-relative path without path churn. The sharded runner scripts
already default to that path.

**Do not cache `bin/` or `obj/`.** Restoring them across commits/agents breaks .NET
incremental compilation (timestamp-based) and is a known anti-pattern.

No `packages.lock.json` exists in this repo today. Preferred re-enable path once
approved:

1. Confirm hosted cache volumes are enabled **or** configure the cache plugin with an
   object-store backend (`s3`/`gcs`/etc.)
2. Cache **only** `.nuget/packages` (never `bin/`/`obj/`)
3. If using volumes: only `paths` / `name` / `size` — **never** `key` or `{{ checksum }}`

### Test Analytics setup (future — not in live pipeline)

There is **no** live `:bar_chart: Test analytics upload` step in
[`.buildkite/pipeline.yml`](../../.buildkite/pipeline.yml) today. When re-adding, note
the [test-collector plugin](https://github.com/buildkite-plugins/test-collector-buildkite-plugin)
constraint:

> `format` only allows: **`junit`**, **`json`** — **not** `dotnet-trx`.

To activate later:

1. Emit JUnit (or JSON) from tests — e.g.
   [`JunitXml.TestLogger`](https://www.nuget.org/packages/JunitXml.TestLogger)
2. Buildkite → **Test Suites** → suite API token
3. Pipeline **Settings → Environment** → `BUILDKITE_ANALYTICS_TOKEN` (secret; never commit)
4. Plugin block example:

```yaml
plugins:
  - test-collector#v1.11.0:
      files: "test-results/**/*.xml"
      format: "junit"
```

### GitNexus reindex soft-fail prep (script ready; pipeline not scoped yet)

`tools/buildkite/gitnexus-reindex.sh` maps known best-effort CLI failures
(`gitnexus analyze` / `gitnexus status`) to exit **`75`** (`EX_TEMPFAIL`). Bootstrap
failures (missing CLI) still exit **1**.

**Live pipeline still uses blanket soft_fail:**

```yaml
# .buildkite/pipeline.yml (current)
soft_fail: true
```

**Not yet applied** (follow-up only, after CI logs exist):

```yaml
soft_fail:
  - exit_status: 75
```

Until that YAML change lands, exit 1 and exit 75 are both soft-failed by the step.
Do not document the scoped form as live.

### Re-enable optimizations (phased order)

Do **not** re-land multi-step optimization in one shot. After Buildkite job logs are
available for a red build:

1. **Single-agent** path first (new scripts with `BUILDKITE_PARALLEL_JOB_COUNT=1`, or keep
   `agent-dotnet-ci.sh` until green)
2. **Parallelism** only after single-agent is green
3. **Scoped soft_fail** for gitnexus-reindex (`exit_status: 75` only)
4. **Cache** only after org **Cache Storage** is confirmed (never invent `cache:` if upload rejects)

### Shell entrypoints

**Live (called by `pipeline.yml`):**

- [`tools/buildkite/agent-dotnet-ci.sh`](../../tools/buildkite/agent-dotnet-ci.sh) + [`dotnet-ci.sh`](../../tools/buildkite/dotnet-ci.sh) — **blocking gate**
- [`tools/buildkite/agent-gitleaks.sh`](../../tools/buildkite/agent-gitleaks.sh)
- [`tools/buildkite/agent-baltic-replay.sh`](../../tools/buildkite/agent-baltic-replay.sh) + [`baltic-replay.sh`](../../tools/buildkite/baltic-replay.sh)
- [`tools/buildkite/agent-gitnexus-pr-analysis.sh`](../../tools/buildkite/agent-gitnexus-pr-analysis.sh)
- [`tools/buildkite/agent-gitnexus-reindex.sh`](../../tools/buildkite/agent-gitnexus-reindex.sh) + [`gitnexus-reindex.sh`](../../tools/buildkite/gitnexus-reindex.sh) (exit-75 **prep** in script; pipeline still blanket soft_fail)
- [`tools/buildkite/agent-bootstrap-gitnexus.sh`](../../tools/buildkite/agent-bootstrap-gitnexus.sh)

**Groundwork only (not called by live `pipeline.yml`):**

- [`tools/buildkite/agent-dotnet-build.sh`](../../tools/buildkite/agent-dotnet-build.sh) + [`dotnet-build.sh`](../../tools/buildkite/dotnet-build.sh)
- [`tools/buildkite/run-tests-sharded.sh`](../../tools/buildkite/run-tests-sharded.sh)
- [`tools/buildkite/annotate-test-summary.sh`](../../tools/buildkite/annotate-test-summary.sh)
- [`tools/buildkite/test-annotate-test-summary.sh`](../../tools/buildkite/test-annotate-test-summary.sh) — local fixture checks for annotate

**Other:**

- [`tools/buildkite/agent-gitnexus-wiki.sh`](../../tools/buildkite/agent-gitnexus-wiki.sh) — manual wiki job
- [`tools/verify-ci-local.ps1`](../../tools/verify-ci-local.ps1) (Windows local gate)

### Orphaned / out-of-scope files (not touched by this pass)

- [`.buildkite/preflight-s67.yml`](../../.buildkite/preflight-s67.yml) — never referenced by any `buildkite-agent pipeline upload` command; left as-is pending a separate cleanup decision
- `packages.lock.json` adoption — no lock files exist in this repo today (see "Caching" above)

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
| First build slow on hosted agents | Expected: live gate `agent-dotnet-ci.sh` downloads .NET SDK 8.0.400 on cold agents; NuGet restore is cold (native `cache:` not enabled) |
| Build fails in ~3s with no step logs | Classic **pipeline upload rejection**. Most often invalid YAML attributes (historically `cache:` with `key`/`{{ checksum }}`, or native `cache:` when volumes are not enabled on the cluster). Diff against `main`'s `.buildkite/pipeline.yml` and remove unsupported fields |
| `CmoCatalogExportTests` fails with `node` not found | Checked-in golden at `tools/cmano-db-crawler/fixtures/sensor-mini-export.golden.json` (copied to test output); live `node` export is optional |
| Build fails ~1m with no `:hammer:` log | Graphite optimizer on **main** pipeline can `pipeline upload --replace` with empty steps; merge branch `.buildkite/pipeline.yml` to `main` or disable `GRAPHITE_CI_OPTIMIZER_TOKEN` until then |
| Agent has dotnet 6/7 on PATH | `agent-bootstrap-dotnet.sh` installs 8.0.400 when major &lt; 8 |
| Required check name mismatch | Copy exact context from GitHub PR checks tab after first build |
| Gitleaks false positive | Add allowlist in `.gitleaks.toml` if needed (not present today) |

## Phase 2 (not implemented)

- Separate Graphite optimizer pipeline (Option 1)
- Unity pipeline on mac/self-hosted agent
