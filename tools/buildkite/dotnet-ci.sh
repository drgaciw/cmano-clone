#!/usr/bin/env bash
# Mirrors .github/workflows/dotnet-reusable.yml and tools/verify-ci-local.ps1
# Policy: production/qa/sprint-35-ci-hygiene-2026-06-19.md (S35-15)
# Day-1 baseline (Release @ trunk): >=1204 solution tests (S35-01 floor 1193; current sln count)
# Closeout target >=1204; ReplayGolden 6/6; PlayModeSmoke 17/17
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
