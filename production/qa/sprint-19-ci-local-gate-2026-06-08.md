# Sprint 19 — CI local gate SOP (S19-07)

**Date:** 2026-06-08  
**Build:** `main` @ `afd2e1a`  
**Verdict:** **FALLBACK ACTIVE** — GitHub Actions blocked by billing; local gate is authoritative  
**Producer sign-off:** **FALLBACK ACTIVE** (billing unresolved)

## Problem

Private repo `drgaciw/cmano-clone` workflows abort in ~3s with:

> The job was not started because recent account payments have failed or your spending limit needs to be increased.

This is unchanged since PR #69 triage. See [pr-69-ci-triage-2026-06-04.md](pr-69-ci-triage-2026-06-04.md) (Sprint 19 status update appended).

Branch protection required checks **cannot be API-verified** on a free private plan (`gh api …/branches/main/protection` → HTTP 403). Confirm check names in the GitHub UI when billing is restored. See [docs/engineering/ci-and-branch-protection.md](../../docs/engineering/ci-and-branch-protection.md).

## Local gate (required when Actions billing blocked)

Run from repo root. Mirrors [.github/workflows/dotnet-reusable.yml](../../.github/workflows/dotnet-reusable.yml) and `tools/verify-ci-local.ps1`.

```powershell
dotnet restore ProjectAegis.sln
dotnet build ProjectAegis.sln -c Release --no-restore
dotnet test ProjectAegis.sln -c Release --no-build -v minimal
dotnet test `
    src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj `
    -c Release --no-build -v minimal `
    --filter FullyQualifiedName~ReplayGoldenSuiteTests
dotnet test `
    src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj `
    -c Release --no-build -v minimal `
    --filter FullyQualifiedName~PlayModeSmokeHarnessTests
```

**Bash equivalent:**

```bash
dotnet restore ProjectAegis.sln
dotnet build ProjectAegis.sln -c Release --no-restore
dotnet test ProjectAegis.sln -c Release --no-build -v minimal
dotnet test \
  src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  -c Release --no-build -v minimal \
  --filter FullyQualifiedName~ReplayGoldenSuiteTests
dotnet test \
  src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  -c Release --no-build -v minimal \
  --filter FullyQualifiedName~PlayModeSmokeHarnessTests
```

**One-liner (PowerShell):**

```powershell
.\tools\verify-ci-local.ps1
```

## Pass criteria

| Gate | Expected (2026-06-08 baseline) |
|------|--------------------------------|
| Full solution `dotnet test` | **403/403 PASS**, 0 failed |
| PlayMode smoke | **7/7 PASS** (`PlayModeSmokeHarnessTests`) |
| Replay golden | **PASS** (`ReplayGoldenSuiteTests`) |

Any regression (failed test, new skip without approval) blocks merge until fixed or baseline is intentionally updated with producer sign-off.

Optional after C# edits: `npx gitnexus detect_changes --repo cmano-clone`

## PR evidence (when merging without green Actions)

Attach to the PR body:

1. Link to this SOP.
2. Commit SHA tested.
3. Paste or attach terminal output showing all gates above **PASS**.
4. Note billing blocker if GitHub checks are red.

Use the PR template checkbox: *If GitHub Actions billing blocked, attach local gate evidence per production/qa/sprint-19-ci-local-gate-2026-06-08.md*.

## Producer actions (billing resolution)

1. Org owner: resolve failed payment or raise **Actions spending limit** for private repos (`drgaciw`).
2. Re-run **`.NET CI`** on `main` after billing restore.
3. Confirm `build_test / build_test` passes on a run that executes real steps (not ~3s abort).
4. In GitHub UI: align branch protection with checks that actually run (see ci-and-branch-protection doc).
5. When Actions is green on `main`, document billing resolution and consider retiring Option B fallback.

## Agent rule

Do **not** treat skipped or billing-aborted GitHub checks as product failures. Local gate output is the merge-quality signal until Actions runs restore → build → test.

## References

- [pr-69-ci-triage-2026-06-04.md](pr-69-ci-triage-2026-06-04.md) — root cause + Sprint 19 status
- [ci-and-branch-protection.md](../../docs/engineering/ci-and-branch-protection.md) — workflows, required checks, Sprint 19 fallback
- [sprint-18-ci-local-gate-2026-06-04.md](sprint-18-ci-local-gate-2026-06-04.md) — prior SOP (superseded baseline counts)