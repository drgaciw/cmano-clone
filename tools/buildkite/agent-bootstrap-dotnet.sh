#!/usr/bin/env bash
# Ensures a .NET SDK that satisfies global.json is on PATH for CI agents
# (Buildkite + GitHub Actions). Hosted images may ship an older SDK on PATH.
#
# global.json pins 11.0.100 with "rollForward": "latestFeature". A higher 11.0.x
# SDK satisfies the pin. An older major cannot roll forward across majors, so
# `dotnet --version` fails until 11.0.100 (or a newer 11.0.x) is installed.
# Do not require an exact 11.0.100 string here: latestFeature selects servicing
# updates within 11.0.x when present (Decision DRG-724).
set -euo pipefail

DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
PINNED_SDK="11.0.100"
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
  echo "ERROR: .NET SDK satisfying global.json (${PINNED_SDK}, rollForward=latestFeature) required after bootstrap; got $(dotnet --version 2>/dev/null || echo missing)"
  exit 1
fi
echo "=== dotnet SDK resolved: $(dotnet --version) (global.json pins ${PINNED_SDK}, rollForward=latestFeature) ==="

# Agents that install only SDK 11.0.100 ship Microsoft.NETCore.App 11.0.x and no
# 10.0 shared framework. PR 1 still targets net10.0 (TFMs move in PR 2), so
# framework-dependent `dotnet test` hosts need LatestMajor roll-forward or they
# exit immediately after build with no test output (Buildkite #2989).
export DOTNET_ROLL_FORWARD="${DOTNET_ROLL_FORWARD:-LatestMajor}"
echo "=== DOTNET_ROLL_FORWARD=${DOTNET_ROLL_FORWARD} (net10.0 apps on SDK 11 agents) ==="

dotnet --info
