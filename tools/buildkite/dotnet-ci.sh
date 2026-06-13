#!/usr/bin/env bash
# Mirrors .github/workflows/dotnet-reusable.yml and tools/verify-ci-local.ps1
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
