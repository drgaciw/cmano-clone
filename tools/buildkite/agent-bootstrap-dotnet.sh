#!/usr/bin/env bash
# Ensures dotnet 8.0.400 is on PATH for Buildkite hosted agents (no docker-in-docker).
set -euo pipefail

if ! command -v dotnet >/dev/null 2>&1; then
  echo "=== Installing .NET SDK 8.0.400 ==="
  curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version 8.0.400 --install-dir "$HOME/.dotnet"
  export DOTNET_ROOT="$HOME/.dotnet"
  export PATH="$DOTNET_ROOT:$PATH"
fi

dotnet --info
