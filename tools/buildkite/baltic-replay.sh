#!/usr/bin/env bash
# Main-branch post-merge gate (formerly graphite-post-merge-ci.yml replay_golden job)
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo_root"

echo "=== Buildkite Baltic replay golden (main) ==="

dotnet restore ProjectAegis.sln
dotnet build ProjectAegis.sln -c Release --no-restore

dotnet test \
  src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  -c Release --no-build -v minimal \
  --filter 'FullyQualifiedName~ReplayGolden'

echo "=== PASS ==="
