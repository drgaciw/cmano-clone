#!/usr/bin/env bash
# Install pinned PowerShell under $RUNNER_TEMP and prepend it for later steps.
# A pwsh already on PATH is not used.
set -euo pipefail

PWSH_VERSION="7.7.0"
machine="$(uname -m)"
case "${machine}" in
  x86_64|amd64)
    pwsh_arch="x64"
    pwsh_sha256="c89d9c682d2c403dfe726f3df948e359675235a5bd6fab535776de69729ca0cb"
    ;;
  aarch64|arm64)
    pwsh_arch="arm64"
    pwsh_sha256="e59c89776eaa59790d44b3edd14277435225ab08b6e7603763101b89de83d935"
    ;;
  *)
    echo "ERROR: unsupported architecture ${machine}" >&2
    exit 1
    ;;
esac

if [[ -z "${RUNNER_TEMP:-}" ]]; then
  echo "ERROR: RUNNER_TEMP is unset" >&2
  exit 1
fi

root="${RUNNER_TEMP}/pwsh-${PWSH_VERSION}"
archive="${RUNNER_TEMP}/powershell-${PWSH_VERSION}-linux-${pwsh_arch}.tar.gz"
rm -rf "$root"
mkdir -p "$root"
url="https://github.com/PowerShell/PowerShell/releases/download/v${PWSH_VERSION}/powershell-${PWSH_VERSION}-linux-${pwsh_arch}.tar.gz"
echo "=== Installing PowerShell ${PWSH_VERSION} (${pwsh_arch}) under ${root} ==="
curl -fsSL "$url" -o "$archive"
if ! echo "${pwsh_sha256}  ${archive}" | sha256sum -c -; then
  echo "ERROR: PowerShell archive sha256 mismatch; not executing" >&2
  rm -f "$archive"
  exit 1
fi
tar -xzf "$archive" -C "$root"
chmod +x "${root}/pwsh"

# Single quotes are the PowerShell command, not a shell expansion.
# shellcheck disable=SC2016
got="$("${root}/pwsh" -NoProfile -Command '$PSVersionTable.PSVersion.ToString()')"
case "$got" in
  "${PWSH_VERSION}"*) ;;
  *)
    echo "ERROR: expected PowerShell ${PWSH_VERSION}, got ${got}" >&2
    exit 1
    ;;
esac

if [[ -n "${GITHUB_PATH:-}" ]]; then
  echo "$root" >> "$GITHUB_PATH"
fi
echo "=== pwsh ${got} at ${root}/pwsh ==="
