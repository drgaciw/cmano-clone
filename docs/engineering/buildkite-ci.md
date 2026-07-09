# Buildkite CI

> **Last updated:** 2026-07-09
> **Replaces:** `.NET CI`, `Graphite CI`, `Post-Merge CI`, and Gitleaks in GitHub Actions
> **Graphite workflow:** [graphite-github-substitute-plan.md](./graphite-github-substitute-plan.md)

## Overview

Primary blocking CI runs on **Buildkite hosted Linux agents** using repo-committed steps in [`.buildkite/pipeline.yml`](../../.buildkite/pipeline.yml). Build and scan steps invoke bash wrappers under `tools/buildkite/` (SDK and gitleaks are installed on the agent when missing).

**Agent skills:** Official Buildkite skills and project agents are documented in [buildkite-agent-skills.md](./buildkite-agent-skills.md). Refresh skills with `bash tools/buildkite/install-buildkite-skills.sh`.

**2026-07-09 optimization pass (PR #263, iterated after upload failures):** the previous
monolithic `agent-dotnet-ci.sh` step still had two **redundant filtered re-runs** of
Replay/C2 tests after the full suite. Those re-runs are gone: the blocking gate now
uses `agent-dotnet-build.sh` + `run-tests-sharded.sh` (with `JOB_COUNT=1` on one agent)
+ an inline `annotate-test-summary.sh` pass. Multi-agent `parallelism: 4` + `depends_on`
was attempted but Build #554 failed at runtime without API log access; the scripts still
honor `BUILDKITE_PARALLEL_JOB{,_COUNT}` so sharding can be re-enabled once logs confirm
agent capacity. Native `cache:` volumes are **not** used (pipeline upload rejection —
see Caching). Graphite CI optimizer plugin remains removed.

| Step | When | Purpose |
|------|------|---------|
| `:hammer: Build and test` | All builds | Single blocking step (main-compatible shape): Release build + S67 hash/bridge check, then all `src/*/*.Tests.csproj` via the sharded runner (`JOB_COUNT=1`), then TRX summary annotation. Uploads `test-results/**/*.trx`. No redundant Replay/C2 re-filters (those tests already run inside UnityAdapter.Tests) |
| Gitleaks | All builds | Secret scan (`soft_fail: true`) |
| Baltic replay golden | `main` only | Post-merge `ReplayGolden*` filter |
| GitNexus PR analysis | Pull requests | `analyze` + `detect_changes`; annotation; `soft_fail: true` |
| GitNexus reindex | `main` only | Knowledge graph refresh; `soft_fail: true` (exit-75 sentinel still in script for future scoped soft_fail) |

Blocking step uses the same `retry.automatic: exit_status: "*"` / `limit: 1` as main.
Parallel siblings (Gitleaks, Baltic, GitNexus) still start alongside the blocking gate.

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

Pipeline still sets `NUGET_PACKAGES: ".nuget/packages"` so a future volume or
[cache plugin](https://github.com/buildkite-plugins/cache-buildkite-plugin) can mount
that path without path churn.

**Do not cache `bin/` or `obj/`.** Restoring them across commits/agents breaks .NET
incremental compilation (timestamp-based) and is a known anti-pattern. Test shards
rebuild their assigned projects when Release output is missing.

No `packages.lock.json` exists in this repo today. Preferred re-enable path once
approved:

1. Confirm hosted cache volumes are enabled **or** configure the cache plugin with an
   object-store backend (`s3`/`gcs`/etc.)
2. Cache **only** `.nuget/packages` (never `bin/`/`obj/`)
3. If using volumes: only `paths` / `name` / `size` — **never** `key` or `{{ checksum }}`

### Test Analytics setup (Task 5 — needs a human with Buildkite org UI access)

The `:bar_chart: Test analytics upload` step is **token-gated and currently a no-op
placeholder**. Important constraint from the
[test-collector plugin](https://github.com/buildkite-plugins/test-collector-buildkite-plugin):

> `format` only allows: **`junit`**, **`json`** — **not** `dotnet-trx`.

To activate for real:

1. Emit JUnit (or JSON) from the test shards — e.g. add
   [`JunitXml.TestLogger`](https://www.nuget.org/packages/JunitXml.TestLogger)
   / `--logger "junit;LogFileName=..."` (or a TRX→JUnit conversion step)
2. Buildkite → **Test Suites** → **New suite** (or select existing `cmano-clone`)
3. Copy the suite's **API token**
4. Buildkite → pipeline **Settings → Environment** → add `BUILDKITE_ANALYTICS_TOKEN`
   (pipeline-level secret/env var, **not** a committed file)
5. Re-add the plugin block on the analytics step:

```yaml
plugins:
  - test-collector#v1.11.0:
      files: "test-results/**/*.xml"
      format: "junit"
```

### GitNexus reindex soft-fail scoping (Task 6)

`tools/buildkite/gitnexus-reindex.sh` previously let any failure from `gitnexus analyze`
/ `gitnexus status` propagate under `set -euo pipefail`, and the pipeline masked *all* of
it with a blanket `soft_fail: true` — including real infra problems (permissions, disk
full, script bugs), not just the GitNexus CLI's own best-effort failure modes. The
script now catches those two commands explicitly and exits `75` (`EX_TEMPFAIL`,
`sysexits.h`) as a documented "known, non-blocking-by-design" sentinel. The pipeline
step now soft-fails only on that exact code:

```yaml
soft_fail:
  - exit_status: 75
```

Any other exit code fails the step for real, restoring signal for genuine bugs.

Shell entrypoints (parity with local dev):

- [`tools/buildkite/agent-dotnet-build.sh`](../../tools/buildkite/agent-dotnet-build.sh) — bootstrap .NET SDK + [`dotnet-build.sh`](../../tools/buildkite/dotnet-build.sh) (build-only; used by the `:hammer: Build` step)
- [`tools/buildkite/run-tests-sharded.sh`](../../tools/buildkite/run-tests-sharded.sh) — sharded `--no-build` test runner used by `:test_tube: Test %n`
- [`tools/buildkite/annotate-test-summary.sh`](../../tools/buildkite/annotate-test-summary.sh) — aggregates `.trx` counts into a build annotation
- [`tools/buildkite/agent-dotnet-ci.sh`](../../tools/buildkite/agent-dotnet-ci.sh) + [`dotnet-ci.sh`](../../tools/buildkite/dotnet-ci.sh) — **no longer called by the pipeline**; kept for full local-parity/legacy runs (mirrors `tools/verify-ci-local.ps1`)
- [`tools/buildkite/agent-gitleaks.sh`](../../tools/buildkite/agent-gitleaks.sh) — bootstrap gitleaks binary
- [`tools/buildkite/agent-baltic-replay.sh`](../../tools/buildkite/agent-baltic-replay.sh) — bootstrap .NET + [`baltic-replay.sh`](../../tools/buildkite/baltic-replay.sh)
- [`tools/buildkite/agent-gitnexus-pr-analysis.sh`](../../tools/buildkite/agent-gitnexus-pr-analysis.sh) — bootstrap Node + GitNexus + [`gitnexus-pr-analysis.sh`](../../tools/buildkite/gitnexus-pr-analysis.sh)
- [`tools/buildkite/agent-gitnexus-reindex.sh`](../../tools/buildkite/agent-gitnexus-reindex.sh) — bootstrap Node + GitNexus + [`gitnexus-reindex.sh`](../../tools/buildkite/gitnexus-reindex.sh) (exit-75 sentinel, see above)
- [`tools/buildkite/agent-gitnexus-wiki.sh`](../../tools/buildkite/agent-gitnexus-wiki.sh) — manual wiki job (requires `OPENAI_API_KEY`; not in default pipeline)
- [`tools/buildkite/baltic-replay.sh`](../../tools/buildkite/baltic-replay.sh)
- [`tools/buildkite/agent-bootstrap-gitnexus.sh`](../../tools/buildkite/agent-bootstrap-gitnexus.sh) — Node 20 + global `gitnexus` CLI
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
| First build slow on hosted agents | Expected: `agent-dotnet-build.sh` (and `run-tests-sharded.sh`'s bootstrap) download .NET SDK 8.0.400 on cold agents; NuGet restore is cold until a cache volume/plugin is enabled |
| Build fails in ~3s with no step logs | Classic **pipeline upload rejection**. Most often invalid YAML attributes (historically `cache:` with `key`/`{{ checksum }}`, or native `cache:` when volumes are not enabled on the cluster). Diff against `main`'s `.buildkite/pipeline.yml` and remove unsupported fields |
| `CmoCatalogExportTests` fails with `node` not found | Checked-in golden at `tools/cmano-db-crawler/fixtures/sensor-mini-export.golden.json` (copied to test output); live `node` export is optional |
| Build fails ~1m with no `:hammer:` log | Graphite optimizer on **main** pipeline can `pipeline upload --replace` with empty steps; merge branch `.buildkite/pipeline.yml` to `main` or disable `GRAPHITE_CI_OPTIMIZER_TOKEN` until then |
| Agent has dotnet 6/7 on PATH | `agent-bootstrap-dotnet.sh` installs 8.0.400 when major &lt; 8 |
| Required check name mismatch | Copy exact context from GitHub PR checks tab after first build |
| Gitleaks false positive | Add allowlist in `.gitleaks.toml` if needed (not present today) |

## Phase 2 (not implemented)

- Separate Graphite optimizer pipeline (Option 1)
- Unity pipeline on mac/self-hosted agent
