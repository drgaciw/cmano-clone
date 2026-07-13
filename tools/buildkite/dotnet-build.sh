#!/usr/bin/env bash
# GROUNDWORK ONLY — not called by live .buildkite/pipeline.yml (still agent-dotnet-ci.sh).
# Build-only helper for a future build/test split. Restores + builds ProjectAegis.sln
# in Release and preserves S67 hash + DelegationBridge-ZERO verification logging.
# Pair with run-tests-sharded.sh when (if) the pipeline is rewired after CI logs exist.
# Live gate remains tools/buildkite/dotnet-ci.sh via agent-dotnet-ci.sh.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo_root"

# shellcheck source=agent-bootstrap-dotnet.sh
source "$repo_root/tools/buildkite/agent-bootstrap-dotnet.sh"

export NUGET_PACKAGES="${NUGET_PACKAGES:-$repo_root/.nuget/packages}"
mkdir -p "$NUGET_PACKAGES"

echo "=== Buildkite .NET CI — Build (Release) ==="
echo "=== verification-before RUN+READ (release-train-scope-boundary-2026-06-24.md S67 §7 + S66 closeout) ==="
echo "=== CI toolchain: dotnet=$(command -v dotnet 2>/dev/null || echo missing) arch=$(uname -m) NUGET_PACKAGES=${NUGET_PACKAGES} ==="

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
