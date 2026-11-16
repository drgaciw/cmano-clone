#!/usr/bin/env bash
# DRG-766 runner helper.
#
#   bash tools/ci/selfhosted-cleanup.sh prepare-caches
#     Set NUGET_PACKAGES and PIP_CACHE_DIR under $RUNNER_TOOL_CACHE for this
#     repository. Later steps inherit them through $GITHUB_ENV.
#
#   bash tools/ci/selfhosted-cleanup.sh
# Work: delete this job's repository directory under the runner _work folder.
# _work/_tool (NuGet and pip caches) and _work/_temp are left in place.
# These jobs do not start containers, so there is no Docker cleanup.

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

cleanup_work
echo "cleanup: done"
exit 0
