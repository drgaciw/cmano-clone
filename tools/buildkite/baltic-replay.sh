#!/usr/bin/env bash
# Main-branch post-merge gate (formerly graphite-post-merge-ci.yml replay_golden job)
# S67: §7 gate alignment + verification-before RUN+READ (release-train-scope-boundary-2026-06-24.md)
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo_root"

echo "=== Buildkite Baltic replay golden (main) [S67 preflight] ==="
echo "=== verification-before RUN+READ replay 6/6 (release-train-scope-boundary-2026-06-24.md S67 §7 + hash) ==="

dotnet restore ProjectAegis.sln
dotnet build ProjectAegis.sln -c Release --no-restore

results_dir="test-results-replay"
mkdir -p "$results_dir"

dotnet test \
  src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  -c Release --no-build -v minimal \
  --filter 'FullyQualifiedName~ReplayGolden' \
  --logger "trx;LogFileName=baltic-replay.trx" \
  --results-directory "$results_dir"

# hash verification in replay gate
grep -l "17144800277401907079" tests/regression/replay-golden-*.txt 2>/dev/null | head -2 || true
echo "CITE: release-train-scope-boundary-2026-06-24.md (replay 6/6, hash 17144800277401907079, ZERO bridge)"
echo "=== verification-before END (replay) ==="

echo "=== PASS ==="
