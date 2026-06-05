#!/usr/bin/env bash
# Idempotent Cloud Agent setup: ensure .NET SDK 8.0.400 (see global.json), then restore.
set -euo pipefail

DOTNET_VERSION="8.0.400"
export PATH="$HOME/.dotnet:${PATH:-}"

if ! command -v dotnet >/dev/null 2>&1; then
  curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version "${DOTNET_VERSION}"
  export PATH="$HOME/.dotnet:${PATH:-}"
fi

dotnet --version
dotnet restore ProjectAegis.sln
