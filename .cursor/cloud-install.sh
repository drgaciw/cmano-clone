#!/usr/bin/env bash
# Idempotent Cloud Agent setup: ensure an SDK that satisfies global.json
# (pin 10.0.403, rollForward latestMajor), then restore.
set -euo pipefail

DOTNET_VERSION="10.0.403"
export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$DOTNET_ROOT:${PATH:-}"

if ! dotnet --version >/dev/null 2>&1; then
  curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version "${DOTNET_VERSION}" --install-dir "$DOTNET_ROOT"
  export PATH="$DOTNET_ROOT:${PATH:-}"
fi

dotnet --version
dotnet restore ProjectAegis.sln
