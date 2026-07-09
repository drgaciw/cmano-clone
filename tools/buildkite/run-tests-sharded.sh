#!/usr/bin/env bash
# Sharded test runner for the Buildkite ":test_tube: Test %n" step (Task 3 build/test
# split, 2026-07-09 CI optimization pass). Shards the co-located src/*/*.Tests.csproj
# projects across BUILDKITE_PARALLEL_JOB / BUILDKITE_PARALLEL_JOB_COUNT and runs each
# with `--no-build` when a Release DLL is already present.
#
# Replay/C2 filter coverage (ReplayGoldenSuiteTests, PlayModeSmokeHarnessTests) lives
# inside ProjectAegis.Delegation.UnityAdapter.Tests and is exercised by that project's
# normal full-suite run once it lands in a shard — no separate redundant filtered pass
# (that redundancy in the old dotnet-ci.sh is exactly what this split removes).
#
# Cross-job sharing is NuGet-only (hosted cache volume on `.nuget/packages` via
# NUGET_PACKAGES). bin/obj are intentionally NOT cached (incremental-compile poison).
# Each shard builds its assigned projects when Release output is missing (normal on
# ephemeral agents after the :hammer: Build step).
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo_root"

# shellcheck source=agent-bootstrap-dotnet.sh
source "$repo_root/tools/buildkite/agent-bootstrap-dotnet.sh"

# Workspace-relative packages folder (matches pipeline env; safe default for local runs).
export NUGET_PACKAGES="${NUGET_PACKAGES:-$repo_root/.nuget/packages}"
mkdir -p "$NUGET_PACKAGES"

job_index="${BUILDKITE_PARALLEL_JOB:-0}"
job_count="${BUILDKITE_PARALLEL_JOB_COUNT:-1}"
# Defensive: empty/0 would divide-by-zero when sharding (review feedback).
if ! [[ "$job_count" =~ ^[0-9]+$ ]] || (( job_count <= 0 )); then
  job_count=1
fi
if ! [[ "$job_index" =~ ^[0-9]+$ ]] || (( job_index < 0 )); then
  job_index=0
fi
if (( job_index >= job_count )); then
  echo "ERROR: BUILDKITE_PARALLEL_JOB=${job_index} >= BUILDKITE_PARALLEL_JOB_COUNT=${job_count}"
  exit 1
fi

echo "=== Buildkite .NET CI — Test shard ${job_index}/${job_count} ==="

results_dir="$repo_root/test-results"
mkdir -p "$results_dir"

# Co-located test projects (src/*/*.Tests.csproj). Keep in sync with
# docs/engine-reference/dotnet/README.md's test project list.
mapfile -t all_projects < <(find src -maxdepth 2 -iname "*.Tests.csproj" | sort)

if [[ ${#all_projects[@]} -eq 0 ]]; then
  echo "ERROR: no *.Tests.csproj projects found under src/"
  exit 1
fi

shard_projects=()
for i in "${!all_projects[@]}"; do
  if (( i % job_count == job_index )); then
    shard_projects+=("${all_projects[$i]}")
  fi
done

if [[ ${#shard_projects[@]} -eq 0 ]]; then
  echo "No projects assigned to shard ${job_index}/${job_count} (parallelism > project count) — nothing to do."
  exit 0
fi

echo "Shard ${job_index}/${job_count} projects:"
printf '  - %s\n' "${shard_projects[@]}"

overall_exit=0
for proj in "${shard_projects[@]}"; do
  proj_name="$(basename "${proj%.csproj}")"
  proj_dir="$(dirname "$proj")"

  # Ephemeral agents do not share bin/ with the build step — build this project
  # when its Release test DLL is missing (warm NuGet cache keeps this fast).
  if ! find "$proj_dir/bin/Release" -maxdepth 2 -iname "${proj_name}.dll" 2>/dev/null | grep -q .; then
    echo "INFO: ${proj_name} Release build not found — building locally (NuGet cache)"
    dotnet build "$proj" -c Release
  fi

  echo "--- Testing ${proj_name} ---"
  set +e
  dotnet test "$proj" -c Release --no-build -v minimal \
    --logger "trx;LogFileName=${job_index}-${proj_name}.trx" \
    --results-directory "$results_dir"
  proj_exit=$?
  set -e

  if [[ $proj_exit -ne 0 ]]; then
    echo "ERROR: ${proj_name} tests failed with exit ${proj_exit}"
    overall_exit=1
  fi
done

echo "=== Test shard ${job_index}/${job_count} $([[ $overall_exit -eq 0 ]] && echo PASS || echo FAIL) ==="
exit $overall_exit
