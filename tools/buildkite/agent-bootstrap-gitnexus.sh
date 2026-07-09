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
  npm install -g gitnexus
fi

export GITNEXUS_SKIP_OPTIONAL_GRAMMARS="${GITNEXUS_SKIP_OPTIONAL_GRAMMARS:-1}"
export GITNEXUS_WORKER_POOL_SIZE="${GITNEXUS_WORKER_POOL_SIZE:-4}"

gitnexus --version
