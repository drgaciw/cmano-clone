#!/usr/bin/env bash
# Build-only stage of the Buildkite pipeline (Task 3 build/test split, 2026-07-09 CI
# optimization pass). Restores + builds ProjectAegis.sln in Release and preserves the
# S67 hash + DelegationBridge-ZERO verification-before check that used to live inline
# in dotnet-ci.sh. Test execution now happens in run-tests-sharded.sh (parallelism: 4),
# which depends_on this step and reuses its Release build via the shared `cache:` key
# (falls back to a local per-project build on a cache miss).
#
# tools/buildkite/dotnet-ci.sh is kept as-is for full local-parity / legacy runs
# (mirrors tools/verify-ci-local.ps1) — it is not called by .buildkite/pipeline.yml
# anymore, but still works standalone.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo_root"

# shellcheck source=agent-bootstrap-dotnet.sh
source "$repo_root/tools/buildkite/agent-bootstrap-dotnet.sh"

echo "=== Buildkite .NET CI — Build (Release) ==="
echo "=== verification-before RUN+READ (release-train-scope-boundary-2026-06-24.md S67 §7 + S66 closeout) ==="
echo "=== CI toolchain: dotnet=$(command -v dotnet 2>/dev/null || echo missing) arch=$(uname -m) ==="

dotnet restore ProjectAegis.sln
dotnet build ProjectAegis.sln -c Release --no-restore

# S67 hash check + bridge check (verification-before) — preserved from the previous
# monolithic dotnet-ci.sh build+test step; runs once here rather than per test shard.
echo "=== verification-before hash + bridge (release-train-scope-boundary-2026-06-24.md) ==="
grep -r "17144800277401907079" --include="*.md" --include="*.txt" tests/ production/ .buildkite/ tools/ | head -3 || true
BRIDGE_REFS=$(grep -r "class DelegationBridge\|DelegationBridge" --include="*.cs" src/ | wc -l || echo "?")
echo "DelegationBridge src refs: $BRIDGE_REFS (ZERO behavior change invariant per boundary)"
echo "CITE: release-train-scope-boundary-2026-06-24.md (hash immutable, ZERO DelegationBridge, full gates)"
echo "=== verification-before END (build) ==="

echo "=== BUILD PASS ==="
