#!/usr/bin/env bash
# Node.js on PATH for Buildkite hosted agents (GitNexus CLI). Best-effort — never fails the build.
set -euo pipefail

NODE_VERSION="${BUILDKITE_NODE_VERSION:-${GITNEXUS_NODE_VERSION:-20.18.0}}"
NODE_OS="$(uname -s | tr '[:upper:]' '[:lower:]')"
NODE_MACHINE="$(uname -m)"
case "${NODE_MACHINE}" in
  x86_64|amd64) NODE_ARCH="x64" ;;
  aarch64|arm64) NODE_ARCH="arm64" ;;
  *)
    echo "WARN: unsupported agent architecture ${NODE_MACHINE}; skipping node bootstrap"
    exit 0
    ;;
esac
NODE_DIST="node-v${NODE_VERSION}-${NODE_OS}-${NODE_ARCH}"
NODE_DIR="${HOME}/.cache/buildkite/${NODE_DIST}"

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
  archive="/tmp/${NODE_DIST}.tar.xz"
  if ! curl -fsSL "https://nodejs.org/dist/v${NODE_VERSION}/${NODE_DIST}.tar.xz" -o "$archive"; then
    echo "WARN: node download failed; continuing without node"
    return 1
  fi
  if ! tar -xJf "$archive" -C "$parent_dir"; then
    echo "WARN: node extract failed; continuing without node"
    return 1
  fi
  export PATH="${NODE_DIR}/bin:${PATH}"
}

ensure_node_on_path || true
if command -v node >/dev/null 2>&1 && node --version >/dev/null 2>&1; then
  echo "=== node $(node --version) on PATH ==="
  npm --version || echo "WARN: npm unavailable"
else
  echo "WARN: node not on PATH (CMO export tests use golden fallback)"
fi
