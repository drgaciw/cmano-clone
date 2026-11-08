#!/usr/bin/env bash
# Ensures a .NET SDK that satisfies global.json is on PATH for CI agents
# (Buildkite + GitHub Actions). Hosted images may ship an older SDK on PATH.
#
# global.json pins 10.0.403 with "rollForward": "latestMajor". A higher installed
# SDK satisfies the pin. An older SDK cannot roll backward, so `dotnet --version`
# fails until 10.0.403 (or newer) is installed. Do not require an exact 10.0.403
# string here: latestMajor intentionally selects a newer SDK when one is present.
set -euo pipefail

DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
PINNED_SDK="10.0.403"
export DOTNET_ROOT

sdk_satisfies_global_json() {
  dotnet --version >/dev/null 2>&1
}

ensure_dotnet() {
  if sdk_satisfies_global_json; then
    return 0
  fi

  if [[ -x "${DOTNET_ROOT}/dotnet" ]]; then
    export PATH="${DOTNET_ROOT}:${PATH}"
    if sdk_satisfies_global_json; then
      return 0
    fi
  fi

  echo "=== Installing .NET SDK ${PINNED_SDK} (no SDK satisfies global.json) ==="
  mkdir -p "$DOTNET_ROOT"
  if ! curl -fsSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version "$PINNED_SDK" --install-dir "$DOTNET_ROOT"; then
    echo "ERROR: dotnet-install.sh failed"
    exit 1
  fi
  export PATH="${DOTNET_ROOT}:${PATH}"
}

ensure_dotnet

if ! sdk_satisfies_global_json; then
  echo "ERROR: .NET SDK satisfying global.json (${PINNED_SDK}, rollForward=latestMajor) required after bootstrap; got $(dotnet --version 2>/dev/null || echo missing)"
  exit 1
fi
echo "=== dotnet SDK resolved: $(dotnet --version) (global.json pins ${PINNED_SDK}, rollForward=latestMajor) ==="

dotnet --info
