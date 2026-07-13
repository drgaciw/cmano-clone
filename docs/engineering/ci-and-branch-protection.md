# CI and branch protection

> **Last updated:** 2026-06-25 (S67)  
> **Cites:** production/release-train-scope-boundary-2026-06-24.md (S67 row: Buildkite preflight ∥ Regression baseline lock ∥ Branch-protection; .buildkite/ alignment with §7 gates; ci-and-branch-protection update) + future-sprint-roadpmap-062426.md §7 (standing invariants) + roadmap-execute-plan-062426.md §S67  
> **Graphite-first workflow:** [graphite-github-substitute-plan.md](./graphite-github-substitute-plan.md) — use `gt submit`, not `gh pr create`, for stack work.  
> **Buildkite setup:** [buildkite-ci.md](./buildkite-ci.md)

## Primary CI (Buildkite — blocking)

| Pipeline | Trigger | Purpose |
|----------|---------|---------|
| [`.buildkite/pipeline.yml`](../../.buildkite/pipeline.yml) | PR + `main` push | **Blocking** — Graphite optimizer (when token set), build/test via `agent-dotnet-ci.sh`, Gitleaks (`soft_fail`), replay golden + PlayMode smoke |
| Baltic replay golden step | `main` only | Post-merge `FullyQualifiedName~ReplayGolden` tests |

**Local parity:**

```powershell
.\tools\verify-ci-local.ps1
```

Or manually:

```bash
dotnet build ProjectAegis.sln -c Release
dotnet test ProjectAegis.sln -c Release -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj -c Release --filter PlayModeSmokeHarnessTests
```

Baltic replay (main parity): `bash tools/buildkite/baltic-replay.sh`

## GitHub Actions (secondary / platform-specific)

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| [Dismiss Stale Approvals (Graphite)](../../.github/workflows/graphite-dismiss-stale-approvals.yml) | PR `synchronize` | Graphite-compatible approval dismissal |
| [GitNexus Security Checks](../../.github/workflows/gitnexus-security.yml) | PR + `main` + weekly | CodeQL (C# + JS/TS), dependency review |
| ~~GitNexus Auto-Reindex~~ | **Disabled** — [Buildkite](../../.buildkite/pipeline.yml) `gitnexus-reindex` step | Was: knowledge graph reindex on `main` |
| ~~GitNexus Wiki~~ | **Disabled** — manual [Buildkite wiki job](./buildkite-ci.md) | Was: release-triggered wiki commit |
| ~~GitNexus PR Analysis~~ | **Disabled** — [Buildkite](../../.buildkite/pipeline.yml) `gitnexus-pr` step | Was: PR blast-radius comment |
| [Unity CI (manual)](../../.github/workflows/unity-ci.yml) | `workflow_dispatch` | Editor tests when `UNITY_LICENSE` is configured |

## Sprint 19 local gate fallback (Option B — billing blocked)

**Status (2026-06-08):** GitHub Actions on private repo `drgaciw/cmano-clone` is still blocked by account billing. Workflows abort in ~3s before checkout; this is **not** a code failure. **Option B is active:** the ratified local gate SOP is the authoritative product merge gate until billing is restored.

**SOP:** [production/qa/sprint-19-ci-local-gate-2026-06-08.md](../../production/qa/sprint-19-ci-local-gate-2026-06-08.md)

**Triage / history:** [production/qa/pr-69-ci-triage-2026-06-04.md](../../production/qa/pr-69-ci-triage-2026-06-04.md) (Sprint 19 status update)

When Actions is blocked, run before merge:

```powershell
.\tools\verify-ci-local.ps1
```

**Pass criteria (baseline @ `afd2e1a`):** **403/403** full-solution tests, **7/7** PlayMode smoke, replay golden suite **PASS**. Attach terminal evidence to the PR per the SOP.

**Producer sign-off:** **FALLBACK ACTIVE** (billing unresolved). Retire this section when `buildkite/cmano-clone` is green on `main` and branch protection requires it.

## Required status checks (branch protection)

**Private repos on the free plan** cannot set or read branch protection via the API (HTTP 403 — *Upgrade to GitHub Pro or make this repository public*). Required status check names **cannot be API-verified** on the current plan. Enable and audit checks manually in the GitHub UI when the repo is **public** or the org has **GitHub Pro/Team**. Until then, use the Sprint 19 local gate SOP when merging with red Actions checks.

Tracking issue: [Enable branch protection required checks on main](https://github.com/drgaciw/cmano-clone/issues/37).

### Settings → Branches → Add rule for `main`

1. **Require status checks to pass**
2. **Require branches to be up to date before merging** (recommended)
3. Select this context (exact name from first green Buildkite build):

   | Context | Source |
   |---------|--------|
   | `buildkite/cmano-clone` | Buildkite primary pipeline (adjust slug if yours differs). S67+ enforces §7 gates per release-train-scope-boundary-2026-06-24.md (tests, replay, GitNexus pre, baselines). |

4. **Do not** enable “Dismiss stale pull request approvals when new commits are pushed” — Graphite rebases break that rule. Use [Dismiss Stale Approvals (Graphite-compatible)](../../.github/workflows/graphite-dismiss-stale-approvals.yml) instead.

### Apply via CLI (Pro / public repo)

```powershell
# From repo root — requires permission to manage branch protection
.\tools\apply-branch-protection.ps1
```

Uses [.github/branch-protection.main.json](../../.github/branch-protection.main.json).

## S67 Alignment — §7 Gates + GitNexus Preflight + Baseline Enforcement (per release-train-scope-boundary-2026-06-24.md)

**S67-03 Branch-protection track (isolated).** Aligns branch protection + CI docs with release train §7 standing invariants + S67 deliverables.

**§7 gates enforced via `buildkite/cmano-clone` required status (and local parity `tools/verify-ci-local.ps1`):**
- Test baseline **≥1229** (monotonic; no regression)
- **ReplayGolden 6/6** + **C2 proxy 18/18+**
- Production Baltic hash **`17144800277401907079`** preserved
- **ZERO DelegationBridge** edits (CatalogWriteGate extend-only only)
- **GitNexus preflight discipline** (per AGENTS.md): `impact({target, direction:"upstream", summaryOnly:true})` + `detect_changes({scope:"compare", base_ref:"main"})` before commits/edits to symbols. See GitNexus PR step + buildkite agents.
- Buildkite preflight parity (PR + main) with local gates; post-merge replay on main.
- Graphite compat: no stale-dismiss in protection rule (handled by graphite-dismiss-stale-approvals.yml).

**Protected status context:** `buildkite/cmano-clone` (see .github/branch-protection.main.json and .buildkite/pipeline.yml).

**S67 artifacts (locked baselines post-track):**
- production/sprints/sprint-67-buildkite-baseline-protection.md
- .buildkite/preflight-s67.yml (S67-01: §7 gates preflight + verification-before RUN+READ; cites release-train-scope-boundary-2026-06-24.md; integrated in pipeline)
- production/qa/ (locked baseline evidence)
- tests/regression/README.md (S67-02 locked: 1232/0f, 6/6, hash 17144800277401907079; goldens lists + verif cmds)
- All changes cite boundary + S67 plan + kickoff.

**S67-02 locked baseline (audit vs S66 1232/0f 6/6 replay incl Baltic v2, hash 17144800277401907079):** Tests 1232/0f; Replay 6/6 (core 6 + v2 goldens); C2 18/18; hash preserved. Verif cmds + GitNexus pre in tests/regression/README.md + sprint-67 plan. Cite release-train-scope-boundary-2026-06-24.md .

**Verification-before (mandatory before merge per AGENTS + boundary + sprint plan):**
```bash
dotnet build ProjectAegis.sln -c Release
dotnet test ProjectAegis.sln -c Release -v minimal --filter "ReplayGolden|PlayModeSmoke"
bash tools/buildkite/baltic-replay.sh || true
grep -r "17144800277401907079" --include="*.md" .
# + GitNexus impact + detect_changes
```
**GitNexus pre:** Performed (LOW/MED on doc/CI files; no CRITICAL edits; see detect_changes reports).

See sprint-67-buildkite-baseline-protection.md for full tracks (preflight || baseline lock || branch-protection). S67-02 COMPLETE.

**Prior S19 local fallback:** Retained for free-plan billing issues; prefer Buildkite when green.

## Unity Editor CI (optional)

Headless **Play Mode smoke** already runs in the Buildkite .NET step without a Unity license.

Full Editor batchmode (EditMode / PlayMode in the Unity project) needs:

1. GitHub secrets: `UNITY_LICENSE` (or `UNITY_EMAIL` + `UNITY_PASSWORD` per [game-ci](https://game.ci/))
2. Run **Actions → Unity CI (manual) → Run workflow**

See [unity/ProjectAegis/PLAYMODE-SMOKE.md](../../unity/ProjectAegis/PLAYMODE-SMOKE.md) for Editor setup.

## Graphite + CI

- **Single Buildkite pipeline** replaces former `.NET CI`, `Graphite CI`, and `Post-Merge CI` GitHub workflows.
- **Graphite optimizer** runs as the first Buildkite step; may skip redundant stack runs (see [buildkite-ci.md](./buildkite-ci.md)).
- **`main` push** runs full dotnet gate plus Baltic replay golden tests in one pipeline.
- Buildkite invokes [`tools/buildkite/agent-dotnet-ci.sh`](../../tools/buildkite/agent-dotnet-ci.sh); core dotnet commands live in [`tools/buildkite/dotnet-ci.sh`](../../tools/buildkite/dotnet-ci.sh).

## Dependabot

[dependabot.yml](../../.github/dependabot.yml) — weekly NuGet and GitHub Actions update PRs.
