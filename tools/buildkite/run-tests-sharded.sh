#!/usr/bin/env bash
# Sharded test runner for the Buildkite ":test_tube: Test %n" step (Task 3 build/test
# split, 2026-07-09 CI optimization pass). Shards the co-located src/*/*.Tests.csproj
# projects across BUILDKITE_PARALLEL_JOB / BUILDKITE_PARALLEL_JOB_COUNT and runs each
# with `--no-build` (the :hammer: Build step already built ProjectAegis.sln in Release).
#
# Replay/C2 filter coverage (ReplayGoldenSuiteTests, PlayModeSmokeHarnessTests) lives
# inside ProjectAegis.Delegation.UnityAdapter.Tests and is exercised by that project's
# normal full-suite run once it lands in a shard — no separate redundant filtered pass
# (that redundancy in the old dotnet-ci.sh is exactly what this split removes).
#
# Cross-job build sharing relies on the `cache:` key shared with the build step
# (paths: ~/.nuget/packages, **/obj, **/bin — same csproj/global.json checksum key).
# If a project's Release output is missing anyway (cold cache, different agent pool),
# this script builds that one project locally rather than failing on --no-build.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo_root"

# shellcheck source=agent-bootstrap-dotnet.sh
source "$repo_root/tools/buildkite/agent-bootstrap-dotnet.sh"

job_index="${BUILDKITE_PARALLEL_JOB:-0}"
job_count="${BUILDKITE_PARALLEL_JOB_COUNT:-1}"

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

  # Cache-miss fallback: build this one project locally if its Release output isn't
  # present, instead of failing on --no-build.
  if ! find "$proj_dir/bin/Release" -maxdepth 2 -iname "${proj_name}.dll" 2>/dev/null | grep -q .; then
    echo "WARN: ${proj_name} Release build not found (cache miss) — building locally"
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
