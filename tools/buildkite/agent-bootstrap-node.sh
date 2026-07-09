#!/usr/bin/env bash
# Node.js on PATH for Buildkite hosted agents (Data.Tests CMO export + GitNexus CLI).
set -euo pipefail

NODE_VERSION="${BUILDKITE_NODE_VERSION:-${GITNEXUS_NODE_VERSION:-20.18.0}}"
NODE_DIR="${HOME}/.cache/buildkite/node-v${NODE_VERSION}-linux-x64"

node_major_version() {
  node -p "process.versions.node.split('.')[0]" 2>/dev/null || echo 0
}

ensure_node_on_path() {
  if command -v node >/dev/null 2>&1 && node --version >/dev/null 2>&1; then
    if [[ "$(node_major_version)" -ge 18 ]]; then
      return 0
    fi
    echo "WARN: system node $(node --version 2>/dev/null || echo unknown) < 18; installing ${NODE_VERSION}"
  fi

  if [[ -x "${NODE_DIR}/bin/node" ]] && "${NODE_DIR}/bin/node" --version >/dev/null 2>&1; then
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
npm --version || echo "WARN: npm unavailable (node export tests do not require npm)"
