# CI and branch protection

> **Last updated:** 2026-06-02

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

## Required status checks (branch protection)

**Private repos on the free plan** cannot set branch protection via the API (HTTP 403). Enable checks manually when the repo is **public** or the org has **GitHub Pro/Team**.

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