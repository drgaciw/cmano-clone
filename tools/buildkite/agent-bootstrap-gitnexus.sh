#!/usr/bin/env bash
# Node.js + GitNexus CLI for Buildkite hosted agents (no docker-in-docker).
set -euo pipefail

NODE_VERSION="${GITNEXUS_NODE_VERSION:-20.18.0}"
NODE_DIR="${HOME}/.cache/buildkite/node-v${NODE_VERSION}-linux-x64"

if ! command -v node >/dev/null 2>&1; then
  if [[ ! -x "${NODE_DIR}/bin/node" ]]; then
    echo "=== Installing Node.js ${NODE_VERSION} ==="
    mkdir -p "$(dirname "$NODE_DIR")"
    archive="/tmp/node-v${NODE_VERSION}-linux-x64.tar.xz"
    curl -fsSL "https://nodejs.org/dist/v${NODE_VERSION}/node-v${NODE_VERSION}-linux-x64.tar.xz" -o "$archive"
    tar -xJf "$archive" -C "$(dirname "$NODE_DIR")"
    mv "$(dirname "$NODE_DIR")/node-v${NODE_VERSION}-linux-x64" "$NODE_DIR"
  fi
  export PATH="${NODE_DIR}/bin:${PATH}"
fi

node --version
npm --version

if ! command -v gitnexus >/dev/null 2>&1; then
  echo "=== Installing GitNexus CLI ==="
  npm install -g gitnexus
fi

export GITNEXUS_SKIP_OPTIONAL_GRAMMARS="${GITNEXUS_SKIP_OPTIONAL_GRAMMARS:-1}"
export GITNEXUS_WORKER_POOL_SIZE="${GITNEXUS_WORKER_POOL_SIZE:-4}"

gitnexus --version
