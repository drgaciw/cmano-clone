#!/usr/bin/env bash
# DRG-766 runner helper.
#
#   bash tools/ci/selfhosted-cleanup.sh prepare-caches
#     Set NUGET_PACKAGES and PIP_CACHE_DIR under $RUNNER_TOOL_CACHE for this
#     repository. Later steps inherit them through $GITHUB_ENV.
#
#   bash tools/ci/selfhosted-cleanup.sh
# Docker: remove only containers, networks, and volumes labeled for this
# GitHub run. Both labels are required:
#   aegis.ci.repository=<owner>/<repo>
#   aegis.ci.run=<run_id>-<run_attempt>
# There is no prune. A shared label such as org.testcontainers=true is not
# a filter, so another repo's containers stay in place.
#
# Work: delete this job's repository directory under the runner _work folder.
# _work/_tool (NuGet and pip caches) and _work/_temp are left in place.

set -u

# Point NuGet and pip at a directory named for this repository under the
# runner tool cache (_work/_tool). Cleanup below does not delete _tool.
prepare_caches() {
  local tool_cache="${RUNNER_TOOL_CACHE:-}"
  local repo="${GITHUB_REPOSITORY:-}"
  if [[ -z "$tool_cache" || ! -d "$tool_cache" ]]; then
    echo "prepare-caches: RUNNER_TOOL_CACHE is unset" >&2
    exit 1
  fi
  if [[ ! "$repo" =~ ^[A-Za-z0-9._-]+/[A-Za-z0-9._-]+$ ]]; then
    echo "prepare-caches: repository is not owner/name" >&2
    exit 1
  fi
  local nuget="${tool_cache}/aegis/${repo}/nuget"
  local pip="${tool_cache}/aegis/${repo}/pip"
  mkdir -p "$nuget" "$pip"
  echo "NUGET_PACKAGES=${nuget}" >> "${GITHUB_ENV:?GITHUB_ENV is unset}"
  echo "PIP_CACHE_DIR=${pip}" >> "$GITHUB_ENV"
  echo "prepare-caches: NUGET_PACKAGES=${nuget}"
  echo "prepare-caches: PIP_CACHE_DIR=${pip}"
}

if [[ "${1:-}" == "prepare-caches" ]]; then
  prepare_caches
  exit 0
fi

cleanup_docker() {
  if ! command -v docker >/dev/null 2>&1; then
    echo "cleanup: docker not installed; skipping"
    return 0
  fi
  if ! docker info >/dev/null 2>&1; then
    echo "cleanup: docker daemon not reachable; skipping"
    return 0
  fi

  local run_id="${GITHUB_RUN_ID:-}"
  local run_attempt="${GITHUB_RUN_ATTEMPT:-}"
  local repo="${GITHUB_REPOSITORY:-}"
  if [[ ! "$run_id" =~ ^[0-9]+$ || ! "$run_attempt" =~ ^[0-9]+$ ]]; then
    echo "cleanup: run id is not numeric; skipping docker"
    return 0
  fi
  if [[ ! "$repo" =~ ^[A-Za-z0-9._-]+/[A-Za-z0-9._-]+$ ]]; then
    echo "cleanup: repository is not owner/name; skipping docker"
    return 0
  fi

  local repo_label="aegis.ci.repository=${repo}"
  local run_label="aegis.ci.run=${run_id}-${run_attempt}"
  echo "cleanup: docker resources with ${repo_label} and ${run_label}"

  docker ps -aq --filter "label=${repo_label}" --filter "label=${run_label}" \
    | xargs -r docker rm -f >/dev/null 2>&1 || true
  docker network ls -q --filter "label=${repo_label}" --filter "label=${run_label}" \
    | xargs -r docker network rm >/dev/null 2>&1 || true
  docker volume ls -q --filter "label=${repo_label}" --filter "label=${run_label}" \
    | xargs -r docker volume rm >/dev/null 2>&1 || true
}

cleanup_work() {
  local workspace="${GITHUB_WORKSPACE:-}"
  if [[ -z "$workspace" || ! -d "$workspace" ]]; then
    echo "cleanup: no GITHUB_WORKSPACE; skipping work clean"
    return 0
  fi
  case "$workspace" in
    *..*)
      echo "cleanup: refusing workspace path that contains .."
      return 0
      ;;
  esac

  if [[ -d "${workspace}/.git" ]]; then
    git -C "$workspace" config --local --unset-all http.extraheader >/dev/null 2>&1 || true
  fi

  local parent work_root
  parent="$(dirname "$workspace")"
  work_root="$(dirname "$parent")"
  if [[ "$(basename "$work_root")" != "_work" || ! -d "$parent" ]]; then
    echo "cleanup: workspace is not a _work checkout; removing its contents"
    chmod -R u+w "$workspace" 2>/dev/null || true
    find "$workspace" -mindepth 1 -delete 2>/dev/null || true
    return 0
  fi

  echo "cleanup: removing ${parent}"
  chmod -R u+w "$parent" 2>/dev/null || true
  rm -rf "$parent"
}

cleanup_docker
cleanup_work
echo "cleanup: done"
exit 0
