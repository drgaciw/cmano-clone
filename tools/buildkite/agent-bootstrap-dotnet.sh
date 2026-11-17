#!/usr/bin/env bash
# Installs the pinned .NET SDK for CI agents (Buildkite + GitHub Actions).
#
# global.json pins 11.0.100 with rollForward latestFeature. This script does
# not accept a dotnet that is already on PATH. It downloads dotnet-install.sh,
# checks a pinned sha256, and installs SDK 11.0.100. GitHub Actions uses
# $RUNNER_TEMP. Buildkite keeps $HOME/.dotnet.
#
# DOTNET_ROLL_FORWARD=LatestMajor stays so net10.0 test hosts still start on
# an SDK 11 agent (Buildkite #2989). Decision DRG-724.
set -euo pipefail

PINNED_SDK="11.0.100"
# Immutable copy of src/dotnet-install.sh. The moving alias https://dot.net/v1/dotnet-install.sh
# is not used: a later edit there would fail this checksum and block every gate.
DOTNET_INSTALL_URL="https://raw.githubusercontent.com/dotnet/install-scripts/5147e32300a8e908f5d737c8cff63a76b4b63531/src/dotnet-install.sh"
DOTNET_INSTALL_SHA256="082f7685e156738a1b2e2ed8381a621870d4ce8e8c59278034556f05c186eb2e"

if [[ -n "${RUNNER_TEMP:-}" ]]; then
  DOTNET_ROOT="${RUNNER_TEMP}/aegis-dotnet"
  install_script="${RUNNER_TEMP}/dotnet-install.sh"
else
  DOTNET_ROOT="${HOME}/.dotnet"
  install_script="${HOME}/.cache/aegis-tools/dotnet-install.sh"
fi
export DOTNET_ROOT
mkdir -p "$DOTNET_ROOT" "$(dirname "$install_script")"

echo "=== Installing .NET SDK ${PINNED_SDK} via checksummed dotnet-install.sh ==="
if ! curl -fsSL "$DOTNET_INSTALL_URL" -o "$install_script"; then
  echo "ERROR: dotnet-install.sh download failed" >&2
  exit 1
fi
if ! echo "${DOTNET_INSTALL_SHA256}  ${install_script}" | sha256sum -c -; then
  echo "ERROR: dotnet-install.sh sha256 mismatch; not executing" >&2
  rm -f "$install_script"
  exit 1
fi
if ! bash "$install_script" --version "$PINNED_SDK" --install-dir "$DOTNET_ROOT"; then
  echo "ERROR: dotnet-install.sh failed" >&2
  exit 1
fi

export PATH="${DOTNET_ROOT}:${PATH}"
hash -r

resolved="$("${DOTNET_ROOT}/dotnet" --version 2>/dev/null || true)"
if [[ "$resolved" != "$PINNED_SDK" ]]; then
  echo "ERROR: expected .NET SDK ${PINNED_SDK} from ${DOTNET_ROOT}; got ${resolved:-missing}" >&2
  exit 1
fi
echo "=== dotnet SDK resolved: ${resolved} (pinned installer, not PATH) ==="

export DOTNET_ROLL_FORWARD="${DOTNET_ROLL_FORWARD:-LatestMajor}"
echo "=== DOTNET_ROLL_FORWARD=${DOTNET_ROLL_FORWARD} (net10.0 apps on SDK 11 agents) ==="

"${DOTNET_ROOT}/dotnet" --info
