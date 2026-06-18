#!/usr/bin/env bash
# Mirrors .github/workflows/dotnet-reusable.yml and tools/verify-ci-local.ps1
# Policy: production/qa/sprint-29-ci-hygiene-2026-06-18.md (S29-12)
# Baseline (Release @ trunk): >=878 solution tests; ReplayGolden 6/6; PlayModeSmoke 17/17
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
