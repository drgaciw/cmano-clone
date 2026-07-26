#!/usr/bin/env bash
# Ensures a .NET SDK of at least 8.x is on PATH for CI agents (Buildkite + GitHub Actions).
# Hosted images may ship dotnet 6/7 on PATH; global.json targets net8.0.
#
# IMPORTANT — why this accepts SDK >= 8 rather than exactly 8.x:
# global.json pins 8.0.400 with "rollForward": "latestMajor", so on any agent that also
# has a newer SDK installed, `dotnet --version` resolves to that newer SDK (e.g. 10.0.302)
# even when 8.0.400 is present. A guard of `grep '^8\.'` therefore can NEVER pass on such
# an agent — it installs 8.0.400 successfully and then rejects its own install. That is the
# failure mode that broke CI once agent images picked up .NET 10:
#     ERROR: .NET 8 SDK required after bootstrap; got 10.0.302
# Building net8.0 with a newer SDK is explicitly sanctioned by the project:
# docs/engine-reference/dotnet/README.md — "Roll-forward: latestMajor (SDK 10.x may build locally)".
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

  # An SDK already resolving to 8 or newer satisfies global.json's latestMajor roll-forward.
  if [[ "$major" -ge 8 ]]; then
    return 0
  fi

  # A previously bootstrapped SDK under DOTNET_ROOT counts too.
  if [[ -x "${DOTNET_ROOT}/dotnet" ]]; then
    export PATH="${DOTNET_ROOT}:${PATH}"
    if [[ "$(dotnet_major_version)" -ge 8 ]]; then
      return 0
    fi
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

resolved_major="$(dotnet_major_version)"
if [[ "$resolved_major" -lt 8 ]]; then
  echo "ERROR: .NET SDK 8 or newer required after bootstrap; got $(dotnet --version 2>/dev/null || echo missing)"
  exit 1
fi
echo "=== dotnet SDK resolved: $(dotnet --version) (global.json pins 8.0.400, rollForward=latestMajor) ==="

dotnet --info
