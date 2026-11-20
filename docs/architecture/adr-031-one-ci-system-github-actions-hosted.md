# ADR-031: One CI System — GitHub Actions on GitHub-Hosted Runners; Buildkite Retired After Parity

## Status

**Proposed** — 2026-11-20. Accepting this ADR is the owner's call (drg amtd). An agent's approval does not count as acceptance.

## Date

2026-11-20

## Last Verified

2026-11-20. Checked against `main` at `99a4d6e4` (#879). On GitHub Actions, `dotnet-ci` / Build and test, `gauntlet-pytest` and the ReSharper regression gate are green for that push ([runs](https://github.com/drgaciw/cmano-clone/actions?query=branch%3Amain+event%3Apush)). Buildkite `buildkite/cmano-clone` is green on push build [#3123](https://buildkite.com/drgaciw/cmano-clone/builds/3123) and on nightly builds #3124–#3126. In every one of those Buildkite builds the GitNexus reindex step soft-failed (see §Context).

## Decision Makers

- Ruling: Decision bot, Fri 2026-11-20 6:14 AM CT, confidence 0.7 (P1-4, routed by Projects Manager on drg's behalf).
- Author: CI config lane (Release).
- Acceptance: drg amtd (owner / Technical Director). Still pending.
- Decision acceptance (2026-11-20 6:22 AM CT): no job stays on Buildkite, with the exit-75 evidence for Buildkite builds #2837–#3126. The box index at `/workspace/gitnexus-index` stays box-local and Build owns refreshing it. This line does not accept the ADR; Status remains **Proposed**.
- Notion: [P1-4 One CI system](https://app.notion.com/p/41ef7cb4e4df811daf52c0278ce4aa55). Linear: [DRG-766](https://linear.app/drgamtd-workspace/issue/DRG-766) (the self-hosted port, reverted by #878) and [DRG-769](https://linear.app/drgamtd-workspace/issue/DRG-769) (hardening follow-ups). GitNexus sub-part: P1-5 / [DRG-404](https://linear.app/drgamtd-workspace/issue/DRG-404).

## Summary

cmano-clone runs the same .NET gate twice on every push. Buildkite runs it as `buildkite/cmano-clone` and GitHub Actions runs it as `dotnet-ci` / Build and test, and both are required checks. The GitHub-only gates (gauntlet, ReSharper) already live on Actions. This ADR makes **GitHub Actions on GitHub-hosted runners** the single CI system and retires Buildkite **only after a job-for-job parity soak**. No self-hosted runners are allowed while the repo is public.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS client (6000.3.27f2) + .NET 11 headless (SDK 11.0.100 via `global.json`) |
| **Domain** | Core / build infrastructure (no runtime code) |
| **Knowledge Risk** | LOW. CI configuration only. No engine APIs. |
| **References Consulted** | `.buildkite/pipeline.yml`, `.buildkite/preflight-s67.yml`, `tools/buildkite/*.sh`, `.github/workflows/*.yml`, `docs/engineering/buildkite-ci.md`, `docs/engineering/gitnexus-index-health.md`, ruleset 17086544 |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | Parity soak (§Validation Criteria). The replay hash `17144800277401907079` must keep being asserted by the hosted gate. |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | None |
| **Enables** | Removing duplicate CI spend. A single required-check set. Re-enabling `gitnexus-reindex.yml` (P1-5). |
| **Blocks** | Turning Buildkite off and swapping required checks. Both are NEEDS_OWNER and cannot happen until this ADR is Accepted **and** the parity exit criteria are met. |
| **Ordering Note** | #870 (self-hosted Actions, DRG-766) merged and was then reverted by #878 (`d1208846`, 2026-11-16 9:27 PM CT). This ADR supersedes the self-hosted direction of DRG-766. DRG-769 items that apply to self-hosted runners only become moot. Hosted hardening items, such as unity-ci `persist-credentials` and timeouts, stay. |

## Context

### Problem Statement

Every PR and every push to `main` runs the full Release build and test twice: on a Buildkite hosted Linux agent and on a GitHub-hosted `ubuntu-latest` runner. Both are required checks. `dotnet-ci.yml` was added on 2026-07-26 because Buildkite failed at 0 s on every branch for over a week and blocked every merge. That shows Buildkite is a single point of failure that Actions already covers. Hosted Actions minutes are free for public repositories. Buildkite hosted agents are a paid service.

### Current State

**Buildkite.** Pipeline `drgaciw/cmano-clone`, cluster "Default cluster", default queue `linux-small` (hosted, LINUX_AMD64_2X4). No step sets `agents:`, so every step runs on `linux-small`. The pipeline setting is `buildkite-agent pipeline upload`, which uploads `.buildkite/pipeline.yml`. Builds run for branches and for PRs, with forks off and filter `build.branch !~ /^(graphite-base\/|gtmq_)/`. A nightly schedule runs `0 3 * * *` UTC (9 PM CT) on `main`. `.buildkite/preflight-s67.yml` is **not** uploaded by any step. It is a manual, all-`soft_fail` S67 artifact.

| Buildkite step (key) | Command | Trigger | Gate | Secrets / env |
|---|---|---|---|---|
| `:hammer: Build and test` (`build`) | `tools/buildkite/agent-dotnet-ci.sh` → `dotnet-ci.sh` | all builds | **blocking**, auto-retry 1 | none. `DOTNET_*` telemetry opt-outs, clone depth 250 |
| `:gitleaks: Secret scan` (`gitleaks`) | `agent-gitleaks.sh` (gitleaks 8.24.2, `detect` on the full history) | all builds | `soft_fail` (findings also exit 0) | none |
| `:repeat: Baltic replay golden (main)` (`baltic-replay`) | `agent-baltic-replay.sh` → `baltic-replay.sh` (`ReplayGolden*` filter, TRX) | `main` only (push + nightly) | blocking on main | none |
| `:gitnexus: PR impact analysis` (`gitnexus-pr`) | `agent-gitnexus-pr-analysis.sh` (`analyze` + `detect_changes`, annotation) | PR builds only | `soft_fail` | none |
| `:gitnexus: Reindex knowledge graph (main)` (`gitnexus-reindex`) | `agent-gitnexus-reindex.sh` → `gitnexus-reindex.sh` (bare `gitnexus analyze` + `status`, skips doc-only tips) | `main` only | `soft_fail` | none. Uploads **logs only** as Buildkite artifacts |

**GitHub Actions** (all `runs-on` GitHub-hosted, none self-hosted):

| Workflow / job (check name) | Triggers | runs-on | Covers | Required? |
|---|---|---|---|---|
| `dotnet-ci.yml` / **Build and test** | PR, push `main`, dispatch | ubuntu-latest | Same `tools/buildkite/dotnet-ci.sh` as Buildkite, with LFS: restore, catalog policy, build, full test, ReplayGolden 6/6, PlayModeSmoke, hash/bridge grep | **yes** |
| `gauntlet-pytest.yml` / **tools/qa-gauntlet pytest** | PR, push `main`, dispatch | ubuntu-latest | qa-gauntlet guards + `test_gitnexus_safe_reindex.py` | **yes** |
| `gauntlet-oracle.yml` / oracle | PR, dispatch | ubuntu-latest | Demo batch CSV + `gauntlet_oracle_eval --profile ci` | no |
| `resharper.yml` / inspect | PR, push `main`, dispatch | **windows-latest** | ReSharper CLI 2026.3.0 SARIF regression gate | no |
| `actionlint.yml` | PR touching `.github/workflows/**`, dispatch | ubuntu-latest | workflow lint | no |
| `unity-ci.yml` / unity_tests | **dispatch only** | ubuntu-latest | game-ci Unity editmode/playmode. Needs `UNITY_LICENSE` / `UNITY_EMAIL` / `UNITY_PASSWORD` | no |
| `gitnexus-reindex.yml` | dispatch only. **Disabled 2026-06-13** ("migrated to Buildkite") | ubuntu-latest | `analyze` + `status`, uploads `.gitnexus/` as a 14-day artifact | no |
| `gitnexus-pr-analysis.yml` | dispatch only. Disabled 2026-06-13 | ubuntu-latest | analyze + `detect_changes` + PR comment (`pull-requests: write`) | no |
| `gitnexus-wiki.yml` | dispatch only. Disabled 2026-06-13 | ubuntu-latest | Wiki generation with `OPENAI_API_KEY` (paid) and pushes to the repo | no |
| `gitnexus-security.yml` | PR, dispatch. CodeQL jobs `if: false` since 2026-06-20 (billing) | ubuntu-latest | dependency-review (`continue-on-error`) | no |
| `graphite-dismiss-stale-approvals.yml` | dispatch only. Disabled 2026-06-20 | ubuntu-latest | Graphite approval dismissal | no |

**Required checks today** (ruleset 17086544 on `main`): `buildkite/cmano-clone`, `Build and test`, `tools/qa-gauntlet pytest`. Strict policy is off, 0 approvals, and non-fast-forward and deletion are blocked.

**GitNexus reindex has served nobody for weeks.** The Buildkite reindex step exited 75 (soft-failed) on every `main` build checked, from #2837 (2026-11-01) through the #3126 nightly (2026-11-20). The cause is `StorageRequirementError: Storage path is in state "unowned"` in GitNexus 1.6.12, raised by a bare `analyze` on a warm hosted agent. Even when it succeeds, the step builds the index in an ephemeral agent workspace and uploads only `.gitnexus/logs/*`. The index itself is thrown away, and it never had embeddings or the DRG-323 integrity post-check. The index agents actually query is the box-local `cmano-clone` index at `/workspace/gitnexus-index`. Build maintains it locally with `GITNEXUS_SHARED_STORE=off tools/gitnexus-safe-reindex.sh`. Its last index was at `7bf96594` (2026-11-02) with `embeddings: 0`, and Build re-ran the guarded reindex on 2026-11-20 (06:08–06:20 CT): now at `99a4d6e4` with 24,857 embeddings, `vectorSearch: vector-index`.

### Constraints

- The repo is **public**, so self-hosted runners are not allowed (#870 → #878). PR jobs from forks must never see secrets.
- No paid services. Hosted Actions are free for public repos. No paid embedding endpoints (local embeddings only, no `--embedding-base-url`).
- Least privilege: top-level `permissions: contents: read`, `persist-credentials: false`, actions pinned to full commit SHAs in any new or re-enabled workflow.
- The replay hash `17144800277401907079` and ReplayGolden 6/6 must stay asserted by a required check.
- Required-check swaps, turning Buildkite off, and cancelling the Buildkite plan are **NEEDS_OWNER**: drg, in his own words.

### Requirements

- Every Buildkite step has a hosted Actions equivalent, or the ADR names it as staying on Buildkite and says why.
- Both systems run in parallel until the parity exit criteria hold.
- No change in what blocks a merge until the owner swaps the required checks.

## Decision

1. **Target system:** GitHub Actions on GitHub-hosted runners (`ubuntu-latest`, plus `windows-latest` for ReSharper).
2. **Buildkite is retired only after parity**, per the table and exit criteria below. Until then both run, and `buildkite/cmano-clone` stays required.
3. **No self-hosted runners** while the repo is public. Any proposal for them needs a new ADR and the repo going private first.
4. **Gates stay script-shared.** Actions keeps calling `tools/buildkite/*.sh`, so the two systems cannot disagree during the soak. The scripts can be renamed (`tools/ci/`) only after Buildkite is off, in a separate PR.
5. **GitNexus:** re-enable `.github/workflows/gitnexus-reindex.yml` on `push` to `main` as a hosted, non-required, best-effort job that runs the guarded `tools/gitnexus-safe-reindex.sh` and publishes the index as a workflow artifact. That artifact is the CI health signal. It does **not** update the box index (see §GitNexus below).

### Parity table

| Buildkite step | GHA equivalent (file / job) | Hosted-feasible? | Gap to close | Stays on Buildkite? |
|---|---|---|---|---|
| `:hammer: Build and test` (`build`, all builds) | `dotnet-ci.yml` / **Build and test** (PR + push main). Same `dotnet-ci.sh` | **Yes.** Green at `99a4d6e4` on both systems | None functional. Actions lacks Buildkite's auto-retry-once (acceptable, or add a rerun policy). Pin `actions/checkout` / `setup-dotnet` to SHAs (DRG-769). | No |
| Replay hash `17144800277401907079`: **where it is asserted** | Both systems run the *same* assertions. The hard assertion lives in tests that `dotnet-ci.sh` runs: `BalticCombatDomainsPolicyTests.PinnedWorldHash`, `Wave4RtmIndexHonestyPinsTests`, the ReplayGolden suites (6/6) against `tests/regression/replay-golden-*.txt`. The `grep … \|\| true` lines in `dotnet-ci.sh` / `baltic-replay.sh` are informational and never fail. | **Yes** | Optional: make the grep fail-closed (drop `\|\| true`) in a separate PR. Today the hash is enforced by tests, not by grep. | No |
| `:repeat: Baltic replay golden (main)` (`baltic-replay`, main + nightly) | **None as a separate job.** `dotnet-ci.sh` already runs `ReplayGoldenSuiteTests` (6/6) on PR and push main, but the broader `ReplayGolden*` filter with a TRX artifact and the 9 PM CT nightly have no Actions equivalent. | **Yes** | Add a `baltic-replay` job (push `main` + `schedule: '0 3 * * *'`, hosted, `contents: read`) that calls `tools/buildkite/baltic-replay.sh` and uploads the TRX. Non-required. | No |
| `:gitleaks: Secret scan` (`gitleaks`, soft_fail) | **None.** `gitnexus-security.yml` only has dependency-review. #870's gitleaks port was reverted. | **Yes.** Static binary, no secrets | Add a `gitleaks` job calling `tools/buildkite/agent-gitleaks.sh` (or a sha256-pinned binary), `contents: read`, non-blocking to match Buildkite. Consider GitHub secret scanning / push protection (free for public repos) as well. | No |
| `:gitnexus: PR impact analysis` (`gitnexus-pr`, PR, soft_fail) | `gitnexus-pr-analysis.yml`, **disabled** 2026-06-13 | **Yes** for analysis plus a step summary. The PR-comment variant needs `pull-requests: write`, which does not work on fork PRs under `pull_request`. | Re-enable as summary-only (`contents: read`, no comment) or drop it. P0-3's duplicate-Surface check may subsume it. Never `pull_request_target`. | No |
| `:gitnexus: Reindex knowledge graph (main)` (`gitnexus-reindex`, soft_fail) | `gitnexus-reindex.yml`, **disabled** 2026-06-13 → re-enable on push `main` (this ADR's draft PR) | **Yes for the CI job.** Clean checkout, no secrets, local embeddings, about 0.6 GB artifact. **No for serving the box index**: a hosted runner cannot write `/workspace/gitnexus-index`. | Run the guarded `gitnexus-safe-reindex.sh` (`--force --embeddings` + post-check) instead of a bare `analyze`. Publish `.gitnexus/` as an artifact. The box refresh stays box-local (Build). | **No.** The Buildkite step is broken (exit 75 since ≥ 2026-11-01), discards its index, and serves no consumer, so keeping it buys nothing. See §GitNexus. |
| `.buildkite/preflight-s67.yml` (manual upload only, all soft_fail) | None needed. Its steps duplicate `dotnet-ci.sh` / `baltic-replay.sh` / grep. | Yes | Delete with Buildkite retirement (docs-only). | No |
| Nightly schedule `0 3 * * *` UTC on `main` (full pipeline) | **None** | Yes | Add `schedule:` to the new `baltic-replay` job (and optionally `dotnet-ci`). | No |
| *(Actions-only)* gauntlet: `gauntlet-pytest` (required), `gauntlet-oracle` (PR) | Already Actions-only. No Buildkite step. | Yes, green | None. Consider adding push-main to `gauntlet-oracle` for post-merge signal. | n/a |
| *(Actions-only)* ReSharper `inspect` (windows-latest) | Already Actions-only. Green at `99a4d6e4`. | Yes | Pin the floating `dot.net/v1/dotnet-install.ps1` to a fixed install-scripts commit + sha256 (DRG-769). | n/a |
| *(Actions-only)* `unity-ci.yml` (dispatch only, Unity license secrets) | No Buildkite equivalent. **Buildkite runs no Unity Editor step.** The "PlayMode smoke" in Buildkite is the headless `PlayModeSmokeHarnessTests` inside `dotnet-ci.sh`, already on Actions. | Technically yes (game-ci on ubuntu-latest), but it needs a Unity Personal `.ulf` plus email and password secrets, and Personal-licence use in CI is subject to Unity's terms. | No parity gap. Keep it dispatch-only so secrets are never exposed to PR/fork events. Hardening (`persist-credentials: false`, `timeout-minutes`) stays in DRG-769. | n/a. Nothing to keep on Buildkite. |

**Net result:** no job has to stay on Buildkite. Unity licensing does not block retirement because Buildkite never ran the Unity Editor.

### GitNexus: what each system actually produces

| | Buildkite step today | Re-enabled hosted `gitnexus-reindex.yml` (proposed) | Box index (`/workspace/gitnexus-index`, name `cmano-clone`) |
|---|---|---|---|
| Command | bare `gitnexus analyze` + `status` (no `--force`, no embeddings, no post-check) | `GITNEXUS_SHARED_STORE=off tools/gitnexus-safe-reindex.sh` → `analyze --force --embeddings --skip-agents-md --skip-skills` + integrity post-check | same guarded command, run by Build on a clean `main` checkout |
| Where the index lands | ephemeral Buildkite agent workspace, **discarded** | workflow artifact `gitnexus-index-<sha>` (`.gitnexus/`, 14-day retention) + diagnostics | durable, on the shared box. Queried by agents via GitNexus MCP |
| Current health | **soft-fails exit 75** on every main build checked 2026-11-01 → 11-20 (`StorageRequirementError … "unowned"`) | untested. First run is the draft PR's push-to-main after merge. | `99a4d6e4`, 2026-11-20, 24,857 embeddings, `vector-index` (was `7bf96594` / 0 embeddings until that run) |
| Who consumes it | nobody (only `analyze.log` is uploaded) | a **health signal**: "the guarded reindex passes on this main commit". Optionally an agent can pull the artifact (see below). | all agents |

A hosted runner cannot push into the box, and the box must not expose an inbound endpoint or a runner. So the box index stays **pull-based and box-local**:

- **Primary (recommended):** a box routine owned by Build runs the guarded reindex on the clean `main` checkout after merges, or on a schedule. CI only proves the command passes on that commit.
- **Optional:** a box agent pulls the latest `gitnexus-index-<sha>` artifact through the GitHub connector's `download_workflow_run_artifact` (the box `gh` token is invalid) and swaps it in. This is unverified: the index metadata records the runner's absolute repo path (`/home/runner/work/cmano-clone/cmano-clone`), the artifact is about 0.6 GB, and the box may run a different GitNexus version. Do not adopt it without a test swap.

Neither system serves the box index today, so retiring the Buildkite reindex step loses nothing.

### Parallel-run plan and exit criteria

1. **Phase 0 (this ADR's draft PR):** add the ADR (Proposed). Re-enable `gitnexus-reindex.yml` on push `main` as hosted and non-required. Buildkite and required checks are untouched.
2. **Phase 1 (follow-up draft PRs, non-required):** add hosted `baltic-replay` (push main + nightly schedule) and `gitleaks` jobs. Optionally re-enable summary-only GitNexus PR analysis. Pin actions to SHAs. Both systems keep running.
3. **Parity soak — exit criteria, all of which must hold:**
   - ≥ 10 consecutive push-to-`main` commits and ≥ 7 nightly runs where every Actions equivalent in the table reaches the same conclusion as its Buildkite step on the same SHA. A Buildkite failure that Actions misses, or the reverse, resets the count.
   - ReplayGolden 6/6 and the pinned hash tests pass on Actions on every one of those commits.
   - ≥ 20 PRs where `Build and test` and `buildkite/cmano-clone` agree.
   - No Actions-side regression in wall-clock beyond 2× Buildkite for the blocking gate.
   - Decision scores the soak and records it on P1-4.
4. **Phase 2 (NEEDS_OWNER, drg, each in his own words):**
   a. Remove `buildkite/cmano-clone` from ruleset 17086544. Required becomes `Build and test` + `tools/qa-gauntlet pytest` (+ any new check drg chooses).
   b. Turn off the Buildkite pipeline: disable the GitHub webhook / commit status and the nightly schedule.
   c. Cancel or downgrade the Buildkite plan.
5. **Phase 3 (docs-only, after 4b):** delete `.buildkite/`, move `tools/buildkite/*.sh` to `tools/ci/`, update `docs/engineering/buildkite-ci.md` → `ci.md`, and flip this ADR's Status (own docs-only PR per `docs/architecture/README.md`).

**Rollback:** until Phase 2, nothing is removed, so rollback is just reverting the draft PRs. After Phase 2, re-adding `buildkite/cmano-clone` to the ruleset and re-enabling the webhook restores the old gate. The pipeline YAML stays in git history.

## Alternatives Considered

### Alternative 1: Keep Buildkite as the only system

- **Pros:** hosted agents already configured. Annotations.
- **Cons:** paid. A single point of failure (the 2026-07-18 → 07-26 0-second outage). The ReSharper (Windows) and gauntlet gates are on Actions anyway.
- **Rejection Reason:** we would still need Actions, so we would keep two systems.

### Alternative 2: GitHub Actions on self-hosted runners (DRG-766 / #870)

- **Rejection Reason:** the repo is public, and fork PRs could run code on owner hardware. It was reverted in #878. Prohibited by this ADR while the repo is public.

### Alternative 3: Keep both systems permanently

- **Rejection Reason:** it doubles build minutes and cost and keeps two required checks for one gate.

## Consequences

### Positive

- One required gate per concern. No Buildkite cost. No dependency on Buildkite availability.
- The GitNexus CI reindex finally runs the guarded, embeddings-enabled path, and failures are visible instead of soft-failed into silence.

### Negative

- We lose Buildkite annotations and auto-retry. Actions step summaries replace them.
- GitHub-hosted queue times and runner sizes (4 vCPU / 16 GB on public-repo ubuntu-latest) are outside our control.
- About 0.6 GB GitNexus artifact per main push (free for public repos, 14-day retention).

### Neutral

- Gate scripts keep their `tools/buildkite/` paths until Phase 3.

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|-----------|
| GitHub Actions outage or billing block (as on 2026-06-20 when the repo was private) | Low (public = free) | High | Keep Buildkite until parity. Rollback path above. |
| Embeddings model / onnxruntime downloads at reindex time (supply chain, flakiness) | Medium | Low (non-required) | Pin the `gitnexus` version. Best-effort job. Cache the model with `actions/cache`. |
| Hosted reindex exceeds runner RAM or time | Low–Medium | Low | `timeout-minutes: 45`. Measured box runtime below. Non-required. |
| Fork PR secret exposure | Low | High | No `pull_request_target`. Secrets only in dispatch-only `unity-ci.yml`. `contents: read`. |

## Performance Implications

| Metric | Before | Expected After | Budget |
|--------|--------|---------------|--------|
| Blocking gate wall-clock (PR) | Buildkite ~2m20s, Actions ~2m50s (both run) | Actions only, ~3 min | ≤ 2× Buildkite |
| GitNexus main reindex | Buildkite ~40 s, then fails (exit 75) | guarded + embeddings: **11.5 min measured on the box** (8 vCPU / 15 GB, 2026-11-20 06:08–06:20 CT, peak RSS ≈ 3.0 GB, 46,140 nodes, 24,857 embeddings, `vector-index`, 593 MB `.gitnexus/`); estimate ~15–25 min on a 4 vCPU / 16 GB hosted runner | ≤ 45 min |

## Migration Plan

See §Parallel-run plan (Phases 0–3). **Rollback plan:** revert the draft PRs, and after Phase 2 re-add `buildkite/cmano-clone` to the ruleset.

## Validation Criteria

- [ ] Every row in the parity table has a green hosted equivalent (or is n/a).
- [ ] Soak exit criteria met and scored by Decision.
- [ ] Hosted `gitnexus-reindex.yml` passes the safe-reindex post-check (embeddings > 0, indexed commit == HEAD) on ≥ 3 main pushes.
- [ ] drg completes Phase 2a–c in his own words.

## GDD Requirements Addressed

Foundational: no GDD requirement. This is CI infrastructure. It constrains every system's merge gate and preserves the replay determinism invariant (hash `17144800277401907079`, ReplayGolden 6/6).

## Related

- #870 (DRG-766, self-hosted Actions port; merged then reverted) · #878 (revert) · DRG-769 (hardening follow-ups)
- `docs/engineering/buildkite-ci.md`, `docs/engineering/gitnexus-index-health.md`, `tools/gitnexus-safe-reindex.sh`
- P1-5 / DRG-404 (GitNexus embeddings, Build), DRG-323 (index corruption guard)
