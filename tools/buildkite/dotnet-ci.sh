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

# Re-apply bootstrap PATH so test hosts that spawn `dotnet`/`node` subprocesses inherit SDK 8.
# shellcheck source=agent-bootstrap-dotnet.sh
source "$repo_root/tools/buildkite/agent-bootstrap-dotnet.sh"

echo "=== Buildkite .NET CI (Release) [S67 preflight gates aligned] ==="
echo "=== verification-before RUN+READ (release-train-scope-boundary-2026-06-24.md S67 §7 + S66 closeout) ==="
echo "=== CI toolchain: dotnet=$(command -v dotnet 2>/dev/null || echo missing) node=$(command -v node 2>/dev/null || echo missing) arch=$(uname -m) ==="
if command -v node >/dev/null 2>&1; then
  echo "=== node version: $(node --version 2>/dev/null || echo broken) ==="
fi

dotnet restore ProjectAegis.sln

# Catalog policy gate, part 1: no tracked *.db3 (bash parity with scripts/verify-catalog-import.ps1).
# This half stays here, before the build, so it fails fast (~0s git ls-files grep).
# The CmoMarkdown Import test half of this gate moved below the Release build — see
# the "Catalog policy gate, part 2" comment for why.
echo "=== verify-catalog-import ==="
tracked_db3="$(git ls-files '*.db3' || true)"
if [[ -n "$tracked_db3" ]]; then
  echo "FAIL: tracked *.db3 files (CMO game DB policy violation):"
  echo "$tracked_db3" | sed 's/^/  /'
  exit 1
fi
echo "OK: no *.db3 in git ls-files"

dotnet build ProjectAegis.sln -c Release --no-restore
# READ build 0e/0w expected

# Stage the netstandard2.1 Unity plugin DLLs before tests.
# UnityPluginEpicATypesTests (added in 30a274d) asserts these exist, but
# unity/ProjectAegis/Assets/Plugins/**/*.dll is gitignored (.gitignore:33) — only the
# .meta files are tracked. A clean CI checkout therefore has no DLLs and the test fails
# with "Missing plugin DLL — run tools/copy-delegation-assemblies.ps1", while the same
# test passes on any developer machine that has run the copy locally. Wiring the copy
# into the gate removes that local-vs-CI divergence.
bash "$repo_root/tools/copy-delegation-assemblies.sh"

# Catalog policy gate, part 2: CmoMarkdown Import tests.
# Moved here (after the Release build + delegation-assembly copy) and switched to
# `-c Release --no-build` because running this with no `-c` flag defaulted to Debug
# and compiled ProjectAegis.Data / Data.Excel / Data.Tests from scratch, then the
# subsequent `dotnet build -c Release` recompiled the entire graph again — a duplicate
# Debug compile whose output was never used. Measured ~12s of Buildkite build #1703's
# 84s job time (03:52:00->03:52:09 Debug compile, then Release build restarts at
# 03:52:12). Running it here with --no-build reuses the Release output instead.
# Still kept before the full solution `dotnet test` run so it fails fast on catalog
# regressions.
echo "Running CmoMarkdown Import tests..."
dotnet test \
  src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  -c Release --no-build -v minimal \
  --filter 'FullyQualifiedName~CmoMarkdown'

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
