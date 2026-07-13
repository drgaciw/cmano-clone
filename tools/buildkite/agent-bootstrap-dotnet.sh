#!/usr/bin/env bash
# Ensures .NET SDK 8.x (8.0.400) is on PATH for Buildkite hosted agents.
# Hosted images may ship dotnet 6/7 on PATH; global.json targets net8.0.
set -euo pipefail

DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export DOTNET_ROOT

dotnet_major_version() {
  if ! command -v dotnet >/dev/null 2>&1; then
    echo 0
    return
  fi
  dotnet --version 2>/dev/null | cut -d. -f1 || echo 0
}

ensure_dotnet_8() {
  local major
  major="$(dotnet_major_version)"
  if [[ -x "${DOTNET_ROOT}/dotnet" ]] && "${DOTNET_ROOT}/dotnet" --version 2>/dev/null | grep -q '^8\.'; then
    export PATH="${DOTNET_ROOT}:${PATH}"
    return 0
  fi

  if [[ "$major" -ge 8 ]] && dotnet --version 2>/dev/null | grep -q '^8\.'; then
    return 0
  fi

  echo "=== Installing .NET SDK 8.0.400 (agent dotnet missing or major=${major}) ==="
  mkdir -p "$DOTNET_ROOT"
  if ! curl -fsSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version 8.0.400 --install-dir "$DOTNET_ROOT"; then
    echo "ERROR: dotnet-install.sh failed"
    exit 1
  fi
  export PATH="${DOTNET_ROOT}:${PATH}"
}

ensure_dotnet_8

if ! dotnet --version 2>/dev/null | grep -q '^8\.'; then
  echo "ERROR: .NET 8 SDK required after bootstrap; got $(dotnet --version 2>/dev/null || echo missing)"
  exit 1
fi

dotnet --info
