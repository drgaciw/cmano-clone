#!/usr/bin/env bash
# Mirrors .github/workflows/dotnet-reusable.yml and tools/verify-ci-local.ps1
# Policy: production/qa/sprint-33-ci-hygiene-2026-06-19.md (S33-12)
# Day-1 baseline (Release @ trunk): >=1046 solution tests (S32 closeout floor); closeout target >=1086
# Current sln ~1143 tests (S33-12 verify); ReplayGolden 6/6; PlayModeSmoke 17/17
# PowerShell parity: pwsh -File tools/verify-ci-local.ps1
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo_root"

echo "=== Buildkite .NET CI (Release) ==="

dotnet restore ProjectAegis.sln
dotnet build ProjectAegis.sln -c Release --no-restore
dotnet test ProjectAegis.sln -c Release --no-build -v minimal

dotnet test \
  src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  -c Release --no-build -v minimal \
  --filter 'FullyQualifiedName~ReplayGoldenSuiteTests'

dotnet test \
  src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  -c Release --no-build -v minimal \
  --filter 'FullyQualifiedName~PlayModeSmokeHarnessTests'

echo "=== PASS ==="
