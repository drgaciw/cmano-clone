#!/usr/bin/env bash
# Node.js on PATH for Buildkite hosted agents (Data.Tests CMO export + GitNexus CLI).
set -euo pipefail

NODE_VERSION="${BUILDKITE_NODE_VERSION:-${GITNEXUS_NODE_VERSION:-20.18.0}}"
NODE_DIR="${HOME}/.cache/buildkite/node-v${NODE_VERSION}-linux-x64"

ensure_node_on_path() {
  if command -v node >/dev/null 2>&1; then
    return 0
  fi

  if [[ -x "${NODE_DIR}/bin/node" ]]; then
    export PATH="${NODE_DIR}/bin:${PATH}"
    return 0
  fi

  echo "=== Installing Node.js ${NODE_VERSION} ==="
  parent_dir="$(dirname "$NODE_DIR")"
  mkdir -p "$parent_dir"
  rm -rf "$NODE_DIR"
  archive="/tmp/node-v${NODE_VERSION}-linux-x64.tar.xz"
  curl -fsSL "https://nodejs.org/dist/v${NODE_VERSION}/node-v${NODE_VERSION}-linux-x64.tar.xz" -o "$archive"
  tar -xJf "$archive" -C "$parent_dir"
  export PATH="${NODE_DIR}/bin:${PATH}"
}

ensure_node_on_path
node --version
npm --version
