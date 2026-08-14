# Buildkite CI

> **Last updated:** 2026-08-14
> **Replaces:** `.NET CI`, `Graphite CI`, `Post-Merge CI`, and Gitleaks in GitHub Actions
> **Graphite workflow:** [graphite-github-substitute-plan.md](./graphite-github-substitute-plan.md)

## Overview

Primary blocking CI runs on **Buildkite hosted Linux agents** using repo-committed steps in [`.buildkite/pipeline.yml`](../../.buildkite/pipeline.yml). Build and scan steps invoke bash wrappers under `tools/buildkite/` (SDK and gitleaks are installed on the agent when missing).

**Agent skills:** Official Buildkite skills and project agents are documented in [buildkite-agent-skills.md](./buildkite-agent-skills.md). Refresh skills with `bash tools/buildkite/install-buildkite-skills.sh`.

### Live pipeline (authoritative)

The committed [`.buildkite/pipeline.yml`](../../.buildkite/pipeline.yml) blocking gate is:

| Step | When | Queue | Purpose |
|------|------|-------|---------|
| `:hammer: Build and test` | All builds | `linux-medium` | **`agent-dotnet-ci.sh`** → `dotnet-ci.sh` — Release restore/build/test + Replay/C2 filters |
| Gitleaks | All builds | `linux-small` (default) | Secret scan (`soft_fail: true` — blanket) |
| Baltic replay golden | `main` only | `linux-medium` | Post-merge `ReplayGolden*` filter |
| GitNexus PR analysis | Pull requests | `linux-medium` | `analyze` + `detect_changes`; annotation; `soft_fail: true` (blanket) |
| GitNexus reindex | `main` only | `linux-medium` | Knowledge graph refresh; **`soft_fail: true` (blanket)** — not yet scoped to exit 75 |

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

### Agent sizing (2026-08-14)

The default cluster exposes five hosted queues. Until 2026-08-14 `.buildkite/pipeline.yml`
named none of them, so **every step ran on the `linux-small` cluster default**:

| Queue | Shape | Rate | Use |
|-------|-------|------|-----|
| `linux-small` | 2 vCPU · 4 GB | $0.008/min | Cluster default — Gitleaks only |
| `linux-medium` | 4 vCPU · 16 GB | $0.016/min | Build/test, Baltic replay, both GitNexus steps |
| `linux-large` | 8 vCPU · 32 GB | $0.032/min | **Not used** — see below |
| `macos-medium` / `macos-large` | 6/12 vCPU | $0.12 / $0.24/min | Reserved for future Unity Editor builds |

Linux hosted agents bill at **$0.004 per vCPU-minute, metered to the second**, so cost scales
strictly linearly with vCPU count. A larger shape only pays for itself if it finishes
proportionally faster, which it never quite does.

Measured before the change (builds #1640, #1687, #1703, #1704):

| Step | PR #1704 | main #1703 |
|------|----------|------------|
| pipeline upload | 7s | 9s |
| Build and test | 1m 3s | 1m 24s |
| Gitleaks | 9s | 12s |
| Baltic replay | — | 49s |
| GitNexus | **1m 24s** | **1m 18s** |
| **wall clock** | 1m 36s | 1m 40s |

Measured **after** the change, on branch `ci/agent-sizing-trial`:

| Build | Config | `:hammer: Build and test` |
|-------|--------|---------------------------|
| baseline #1704 / #1703 | `linux-small`, duplicate Debug compile | 63s / **84s** |
| #1723 | `linux-medium` + gate reorder | **51s** |
| #1737 | + cache volume, **cold** (SDK downloaded) | **50s** |
| #1740 | + cache volume, **warm** (SDK cache hit) | **39s** |

**39s vs an 84s baseline — a 54% cut on the blocking gate.** #1740's log confirms
`=== dotnet SDK reused from …/.dotnet (cache hit) ===` with no `dotnet-install` at all.
Note the baseline itself varied 63–84s for identical work, so treat single samples with care.

Two conclusions drove the sizing:

1. **`linux-medium`, not `linux-large`.** Hosted-compute job metrics show CPU sustained at
   75–95% but peak memory of only **1.7 GB against the 4 GB ceiling**, with I/O wait under 10%.
   vCPU is the constraint; the 32 GB in `linux-large` would be paid for and unused.
2. **Gitleaks stays on `linux-small`.** At 9–12s there is nothing to gain. `.buildkite/pipeline.yml`
   carries a comment saying so — do not "helpfully" promote it.

Steps run fully in parallel after the upload, so build wall time is *upload + the single longest
step*. On every build sampled that step is a **GitNexus** step — which is `soft_fail: true`, i.e.
advisory. Resizing build-and-test alone therefore moves PR wall time by zero. See
"GitNexus split" below.

### Catalog gate ordering (2026-08-14)

`dotnet-ci.sh` used to run the CmoMarkdown Import test *before* `dotnet build -c Release`.
With no `-c` flag that defaults to **Debug** and compiled `ProjectAegis.Data`, `Data.Excel` and
`Data.Tests` from scratch (build #1703 log, 03:52:00→03:52:09), after which the Release build
recompiled the whole graph at 03:52:12. The Debug output was never used — **~12s of an 84s job**.

The gate is now split:

- the `*.db3` tracked-file policy check stays **before** the build (a ~0s `git ls-files` grep, fails fast);
- the CmoMarkdown test runs **after** the Release build as `-c Release --no-build`, still ahead of the
  full solution run so it fails fast on catalog regressions.

`scripts/verify-catalog-import.ps1` gained `-Configuration`, `-NoBuild` and `-Db3CheckOnly`
so it still works standalone (default invocation unchanged) while `tools/verify-ci-local.ps1`
calls the two halves separately for local parity.

### `.NET` bootstrap: sourced twice by design

`agent-bootstrap-dotnet.sh` is sourced **twice per job**: `agent-dotnet-ci.sh` sources it and
then `exec`s `dotnet-ci.sh`, which sources it again to re-apply PATH for test hosts that spawn
`dotnet`/`node` subprocesses (`dotnet-ci.sh:14`). The replay path has the same shape. Because
`exec` preserves the environment, the second pass used to re-run the whole tail and emit a
second full `dotnet --info` — visible in build #1703 as a duplicated
`=== dotnet SDK resolved ... ===` line.

The script now carries an idempotency guard keyed on exported `DOTNET_BOOTSTRAP_DONE` /
`DOTNET_BOOTSTRAP_DIR`: the second and later sources re-export PATH, log
`=== dotnet bootstrap already applied (PATH re-exported) ===`, and skip the probe/echo/`--info`.

**The SDK download is still the biggest remaining item.** The hosted image ships no `dotnet`, so
every job pulls the 212 MB SDK 8.0.400 tarball (~10s/job, roughly 11 hours of billed agent time a
month). `DOTNET_ROOT` now normalises a relative path to absolute so it *can* point at a cached,
workspace-relative directory, but nothing opts in yet. Two routes, **both requiring console work**:

1. **Bake SDK 8.0.400 into a Buildkite Agent Image** (preferred — needs no pipeline YAML at all;
   the existing `>= 8` early-return then short-circuits the download).
2. Set `DOTNET_ROOT: .dotnet` in step `env:` plus a matching `cache:` block — see Caching below
   before attempting this.

### GitNexus split (staged, NOT active)

`.buildkite/gitnexus.yml` stages the two GitNexus steps as a standalone pipeline, and
`.buildkite/pipeline.yml` carries a **commented-out** `trigger` step with `async: true` showing
how to switch over. This is deliberately inert: a `trigger` naming a pipeline that does not exist
fails every build.

To activate: create a pipeline in org `drgaciw` with slug **`cmano-clone-gitnexus`**, steps sourced
from `.buildkite/gitnexus.yml`; smoke-test it with a manual build; then in one change uncomment
`gitnexus-trigger` and delete the two inline GitNexus steps from `pipeline.yml`.

### `BUILDKITE_GIT_CLONE_FLAGS` was removed

It is a protected variable and Buildkite ignores it when set via pipeline `env:` — every job logged
`Warning: Ignored BUILDKITE_GIT_CLONE_FLAGS`, so the intended `--depth=250` never took effect. Set
`git-clone-flags` in the cluster's agent configuration instead, or enable the git-mirror volume
(5 GB quota already available).

### Caching

**A step-level `cache:` IS now used** on the build step — the historical prohibition below is
obsolete.

> **Correction (2026-08-14), proven in CI:** Cached Storage is enabled on this cluster (Platform
> Pro; 50 GB container-cache and 5 GB git-mirror quotas). The blocker recorded below — "wait for a
> human to confirm Cache Storage is active" — **is resolved**, and the #535/#541 upload rejections
> were caused by the YAML shape (`key` / `{{ checksum }}`), *not* by the feature being unavailable.
>
> Build **#1725** settled it: the pipeline upload **passed** and the job picked up
> `nsc-cache-tag=…/dotnet-sdk-and-nuget`, so the `cache:` block was accepted. It failed for an
> unrelated reason — NuGet rejects a relative `NUGET_PACKAGES`
> (`NuGet.targets(745,5): 'NUGET_PACKAGES' must contain an absolute path`). Fixed by absolutising
> it in `agent-bootstrap-dotnet.sh` alongside `DOTNET_ROOT`.
>
> Result: build **#1740** hit the warm volume, logged
> `=== dotnet SDK reused from …/.dotnet (cache hit) ===`, skipped the 212 MB download entirely,
> and ran the gate in **39s**.

Rules that still hold: only `paths` / `name` / `size` — **never** `key` or `{{ checksum }}`; and
never cache `bin/` or `obj/` (it breaks .NET's timestamp-based incremental compilation).

Evidence (PR #263):

| Build | Config | Result |
|-------|--------|--------|
| #535 | `cache:` with `key` + `{{ checksum }}` (CircleCI/GHA syntax) | Failed ~3s — pipeline upload rejection |
| #541 | `cache:` with only `paths` / `name` / `size` (official volume syntax) | Failed ~3s — still upload rejection |
| Main PRs | No `cache:` at all | SUCCESS |

Hosted [cache volumes](https://buildkite.com/docs/agent/buildkite-hosted/cache-volumes)
are a **Pro/Enterprise** feature and must be enabled on the cluster. ~~Until a human
confirms **Agents → cluster → Cache Storage** is active for this org, do not re-add
native `cache:` blocks.~~ **Superseded 2026-08-14** — the org is on Platform Pro and
Cached Storage is confirmed active (see the correction above). The remaining risk is
the YAML shape, not the entitlement.

When re-enabling cache, set `NUGET_PACKAGES: ".nuget/packages"` in pipeline `env` so a
volume or [cache plugin](https://github.com/buildkite-plugins/cache-buildkite-plugin)
can mount a workspace-relative path without path churn. The sharded runner scripts
already default to that path.

**Do not cache `bin/` or `obj/`.** Restoring them across commits/agents breaks .NET
incremental compilation (timestamp-based) and is a known anti-pattern.

No `packages.lock.json` exists in this repo today. Preferred re-enable path once
approved:

1. ~~Confirm hosted cache volumes are enabled~~ — **done, confirmed active 2026-08-14**
2. Cache **only** `.nuget/packages` (never `bin/`/`obj/`), and optionally a
   workspace-relative `DOTNET_ROOT` such as `.dotnet` to kill the 212 MB per-job SDK
   download (see "`.NET` bootstrap" above)
3. If using volumes: only `paths` / `name` / `size` — **never** `key` or `{{ checksum }}`
4. Land it on a throwaway branch first — an upload rejection fails in ~3s with no step logs

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
- [`.buildkite/gitnexus.yml`](../../.buildkite/gitnexus.yml) — **staged, intentionally inert.** Not referenced by `pipeline.yml` until the `cmano-clone-gitnexus` pipeline exists in Buildkite; see "GitNexus split" above
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
