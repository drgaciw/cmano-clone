#!/usr/bin/env bash
# Node.js + GitNexus CLI for CI agents (no docker-in-docker).
# GitNexus is pinned. A copy already on PATH is used only when its version
# matches the pin and this is not a GitHub Actions self-hosted run.
# Self-hosted runs install under $RUNNER_TEMP, not ~/.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
# shellcheck disable=SC1091
source "$repo_root/tools/buildkite/agent-bootstrap-node.sh"

GITNEXUS_VERSION="1.6.12"
# sha256 of https://registry.npmjs.org/gitnexus/-/gitnexus-1.6.12.tgz
GITNEXUS_TARBALL_SHA256="96391a6d66d448bf0978084951e9cee37e543b75e445cf9ad2cd1358d7d141e1"

gitnexus_version_matches_pin() {
  local raw token
  raw="$(gitnexus --version 2>/dev/null || true)"
  token="$(printf '%s\n' "$raw" | tr -s '[:space:]' '\n' | tail -n 1)"
  token="${token#v}"
  [[ "$token" == "$GITNEXUS_VERSION" ]]
}

install_pinned_gitnexus() {
  local root prefix archive
  if [[ -n "${RUNNER_TEMP:-}" ]]; then
    root="${RUNNER_TEMP}/aegis-tools"
  else
    root="${HOME}/.cache/aegis-tools"
  fi
  prefix="${root}/gitnexus-${GITNEXUS_VERSION}"
  archive="${root}/gitnexus-${GITNEXUS_VERSION}.tgz"
  mkdir -p "$root"
  echo "=== Installing GitNexus ${GITNEXUS_VERSION} under ${prefix} ==="
  if ! curl -fsSL "https://registry.npmjs.org/gitnexus/-/gitnexus-${GITNEXUS_VERSION}.tgz" -o "$archive"; then
    echo "ERROR: gitnexus tarball download failed" >&2
    exit 1
  fi
  if ! echo "${GITNEXUS_TARBALL_SHA256}  ${archive}" | sha256sum -c -; then
    echo "ERROR: gitnexus tarball sha256 mismatch" >&2
    rm -f "$archive"
    exit 1
  fi
  export NPM_CONFIG_PREFIX="$prefix"
  mkdir -p "${prefix}/bin"
  if ! npm install -g "$archive"; then
    echo "ERROR: gitnexus npm install failed" >&2
    exit 1
  fi
  export PATH="${prefix}/bin:${PATH}"
  hash -r
}

if [[ -n "${RUNNER_TEMP:-}" ]]; then
  # Self-hosted: do not trust a gitnexus that happens to be on PATH or under ~/.
  install_pinned_gitnexus
elif command -v gitnexus >/dev/null 2>&1 && gitnexus_version_matches_pin; then
  echo "=== gitnexus ${GITNEXUS_VERSION} already on PATH ==="
else
  if command -v gitnexus >/dev/null 2>&1; then
    echo "=== ignoring gitnexus on PATH ($(gitnexus --version 2>/dev/null || echo unknown)); need ${GITNEXUS_VERSION} ==="
  fi
  install_pinned_gitnexus
fi

export GITNEXUS_SKIP_OPTIONAL_GRAMMARS="${GITNEXUS_SKIP_OPTIONAL_GRAMMARS:-1}"
export GITNEXUS_WORKER_POOL_SIZE="${GITNEXUS_WORKER_POOL_SIZE:-4}"

if ! gitnexus_version_matches_pin; then
  echo "ERROR: gitnexus ${GITNEXUS_VERSION} not on PATH after install (got: $(gitnexus --version 2>/dev/null || echo missing))" >&2
  exit 1
fi
gitnexus --version

# retrigger: ensure Buildkite posts buildkite/cmano-clone for PR #279
