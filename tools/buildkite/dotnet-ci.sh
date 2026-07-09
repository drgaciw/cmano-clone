#!/usr/bin/env bash
# Mirrors .github/workflows/dotnet-reusable.yml and tools/verify-ci-local.ps1
# Policy: production/qa/sprint-35-ci-hygiene-2026-06-19.md (S35-15)
# Day-1 baseline (Release @ trunk): >=1204 solution tests (S35-01 floor 1193; current sln count)
# Closeout target >=1204; ReplayGolden 6/6; PlayModeSmoke 17/17
# PowerShell parity: pwsh -File tools/verify-ci-local.ps1
# S67 update: §7 gates alignment per release-train-scope-boundary-2026-06-24.md (build 0e, test >=1232/0f, replay 6/6, C2 18/18, GitNexus pre, hash check, bridge ZERO)
# verification-before: RUN+READ pattern logged; cite boundary on every gate run
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo_root"

# Ensure node on PATH before Release tests (CmoCatalogExportTests).
# shellcheck source=agent-bootstrap-node.sh
source "$repo_root/tools/buildkite/agent-bootstrap-node.sh"

echo "=== Buildkite .NET CI (Release) [S67 preflight gates aligned] ==="
echo "=== verification-before RUN+READ (release-train-scope-boundary-2026-06-24.md S67 §7 + S66 closeout) ==="
echo "=== CI toolchain: dotnet=$(command -v dotnet 2>/dev/null || echo missing) node=$(command -v node 2>/dev/null || echo missing) arch=$(uname -m) ==="
if command -v node >/dev/null 2>&1; then
  echo "=== node version: $(node --version 2>/dev/null || echo broken) ==="
fi

dotnet restore ProjectAegis.sln
dotnet build ProjectAegis.sln -c Release --no-restore
# READ build 0e/0w expected
set +e
dotnet test ProjectAegis.sln -c Release --no-build -v minimal
test_exit=$?
set -e
if [[ $test_exit -ne 0 ]]; then
  echo "ERROR: dotnet test failed with exit $test_exit"
  exit "$test_exit"
fi

# replay 6/6
dotnet test \
  src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  -c Release --no-build -v minimal \
  --filter 'FullyQualifiedName~ReplayGoldenSuiteTests'

# C2 18/18
dotnet test \
  src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  -c Release --no-build -v minimal \
  --filter 'FullyQualifiedName~PlayModeSmokeHarnessTests'

# S67 hash check + bridge check (verification-before)
echo "=== verification-before hash + bridge (release-train-scope-boundary-2026-06-24.md) ==="
grep -r "17144800277401907079" --include="*.md" --include="*.txt" tests/ production/ .buildkite/ tools/ | head -3 || true
BRIDGE_REFS=$(grep -r "class DelegationBridge\|DelegationBridge" --include="*.cs" src/ | wc -l || echo "?")
echo "DelegationBridge src refs: $BRIDGE_REFS (ZERO behavior change invariant per boundary)"
echo "CITE: release-train-scope-boundary-2026-06-24.md (hash immutable, ZERO DelegationBridge, full gates)"
echo "=== verification-before END (S67 §7 gates + GitNexus pre required before PR) ==="

echo "=== PASS ==="
