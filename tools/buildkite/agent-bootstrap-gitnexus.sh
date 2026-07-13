#!/usr/bin/env bash
# Node.js + GitNexus CLI for Buildkite hosted agents (no docker-in-docker).
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
# shellcheck source=agent-bootstrap-node.sh
source "$repo_root/tools/buildkite/agent-bootstrap-node.sh"

if ! command -v gitnexus >/dev/null 2>&1; then
  echo "=== Installing GitNexus CLI ==="
  export NPM_CONFIG_PREFIX="${HOME}/.npm-global"
  mkdir -p "${NPM_CONFIG_PREFIX}/bin"
  export PATH="${NPM_CONFIG_PREFIX}/bin:${PATH}"
  if ! npm install -g gitnexus; then
    # Exit 1 (not 0): missing CLI is infra. gitnexus-pr uses soft_fail: true;
    # gitnexus-reindex soft-fails only on exit 75 from the reindex script itself.
    echo "ERROR: gitnexus npm install failed" >&2
    exit 1
  fi
fi

export GITNEXUS_SKIP_OPTIONAL_GRAMMARS="${GITNEXUS_SKIP_OPTIONAL_GRAMMARS:-1}"
export GITNEXUS_WORKER_POOL_SIZE="${GITNEXUS_WORKER_POOL_SIZE:-4}"

if ! gitnexus --version; then
  echo "ERROR: gitnexus --version failed after install" >&2
  exit 1
fi
