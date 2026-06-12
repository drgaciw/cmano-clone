# CI and branch protection

> **Last updated:** 2026-06-12  
> **Graphite-first workflow:** [graphite-github-substitute-plan.md](./graphite-github-substitute-plan.md) — use `gt submit`, not `gh pr create`, for stack work.

## Active workflows

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| [.NET CI](../../.github/workflows/dotnet-ci.yml) | PR + `main` push | **Blocking** — `restore`, Release `build`, full `dotnet test`, PlayMode smoke |
| [Graphite CI](../../.github/workflows/graphite-ci.yml) | PR + `main` | Same dotnet gate when Graphite optimizer does not skip |
| [Post-Merge CI (Graphite)](../../.github/workflows/graphite-post-merge-ci.yml) | `main` push | Post-merge dotnet + smoke (stack cost control) |
| [GitNexus Security Checks](../../.github/workflows/gitnexus-security.yml) | PR + `main` + weekly | CodeQL (C# + JS/TS), dependency review, Gitleaks |
| [Unity CI (manual)](../../.github/workflows/unity-ci.yml) | `workflow_dispatch` | Editor tests when `UNITY_LICENSE` is configured |

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

**Post-merge only:** replay golden tests (`FullyQualifiedName~ReplayGolden`) run on `main` in Post-Merge CI.

## Sprint 19 local gate fallback (Option B — billing blocked)

**Status (2026-06-08):** GitHub Actions on private repo `drgaciw/cmano-clone` is still blocked by account billing. Workflows abort in ~3s before checkout; this is **not** a code failure. **Option B is active:** the ratified local gate SOP is the authoritative product merge gate until billing is restored.

**SOP:** [production/qa/sprint-19-ci-local-gate-2026-06-08.md](../../production/qa/sprint-19-ci-local-gate-2026-06-08.md)

**Triage / history:** [production/qa/pr-69-ci-triage-2026-06-04.md](../../production/qa/pr-69-ci-triage-2026-06-04.md) (Sprint 19 status update)

When Actions is blocked, run before merge:

```powershell
.\tools\verify-ci-local.ps1
```

**Pass criteria (baseline @ `afd2e1a`):** **403/403** full-solution tests, **7/7** PlayMode smoke, replay golden suite **PASS**. Attach terminal evidence to the PR per the SOP.

**Producer sign-off:** **FALLBACK ACTIVE** (billing unresolved). Retire this section when `.NET CI` runs real steps and `build_test` is green on `main`.

## Required status checks (branch protection)

**Private repos on the free plan** cannot set or read branch protection via the API (HTTP 403 — *Upgrade to GitHub Pro or make this repository public*). Required status check names **cannot be API-verified** on the current plan. Enable and audit checks manually in the GitHub UI when the repo is **public** or the org has **GitHub Pro/Team**. Until then, use the Sprint 19 local gate SOP when merging with red Actions checks.

Tracking issue: [Enable branch protection required checks on main](https://github.com/drgaciw/cmano-clone/issues/37).

### Settings → Branches → Add rule for `main`

1. **Require status checks to pass**
2. **Require branches to be up to date before merging** (recommended)
3. Select these contexts (exact names from Actions):

   | Context | Source |
   |---------|--------|
   | `build_test` | `.NET CI` |
   | `build` | `Graphite CI` (PRs through Graphite stacks) |

4. **Do not** enable “Dismiss stale pull request approvals when new commits are pushed” — Graphite rebases break that rule. Use [Dismiss Stale Approvals (Graphite-compatible)](../../.github/workflows/graphite-dismiss-stale-approvals.yml) instead.

### Apply via CLI (Pro / public repo)

```powershell
# From repo root — requires permission to manage branch protection
.\tools\apply-branch-protection.ps1
```

Uses [.github/branch-protection.main.json](../../.github/branch-protection.main.json).

## Unity Editor CI (optional)

Headless **Play Mode smoke** already runs in `.NET CI` without a Unity license.

Full Editor batchmode (EditMode / PlayMode in the Unity project) needs:

1. GitHub secrets: `UNITY_LICENSE` (or `UNITY_EMAIL` + `UNITY_PASSWORD` per [game-ci](https://game.ci/))
2. Run **Actions → Unity CI (manual) → Run workflow**

See [unity/ProjectAegis/PLAYMODE-SMOKE.md](../../unity/ProjectAegis/PLAYMODE-SMOKE.md) for Editor setup.

## Graphite + CI

- **Graphite CI** runs on **pull requests only** (not on `main` push) to avoid triple test runs.
- Stacked PRs: Graphite optimizer may skip redundant `Graphite CI` runs; **`.NET CI` still runs** on every PR.
- **Post-Merge CI** on `main`: reusable dotnet gate + **replay golden** filter (`ReplayGolden*` tests).
- Shared steps live in [dotnet-reusable.yml](../../.github/workflows/dotnet-reusable.yml).

## Dependabot

[dependabot.yml](../../.github/dependabot.yml) — weekly NuGet and GitHub Actions update PRs.