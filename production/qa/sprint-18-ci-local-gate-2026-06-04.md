# Sprint 18 — CI local gate SOP (S18-05)

**Date:** 2026-06-04  
**Build:** `main` @ `0ef5c89`  
**Verdict:** **CONCERNS** — GitHub Actions blocked by billing; local gate is authoritative

## Problem

Private repo `drgaciw/cmano-clone` workflows abort in ~3s:

> The job was not started because recent account payments have failed or your spending limit needs to be increased.

See `production/qa/pr-69-ci-triage-2026-06-04.md` (still applicable).

## Local gate (required before merge)

Run from repo root:

```powershell
dotnet restore ProjectAegis.sln
dotnet build ProjectAegis.sln -v minimal
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj --filter "FullyQualifiedName~ReplayGolden|FullyQualifiedName~ReplayOrderLog"
```

**Pass criteria:** 0 failed tests (baseline **380** solution + **7** PlayMode + **7** replay as of 2026-06-04).

Optional: `npx gitnexus detect_changes --repo cmano-clone` after C# edits.

## Producer actions

1. Restore GitHub billing / spending limit for the org.
2. Re-run failed workflows on `main`.
3. Confirm branch protection lists only checks that actually run (or document admin merge with local evidence).

## Agent rule

Do not treat skipped GitHub checks as product failures. Attach this SOP + fresh test output to PR bodies when merging without CI.