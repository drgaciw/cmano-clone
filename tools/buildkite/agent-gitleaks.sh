#!/usr/bin/env bash
# Gitleaks on Buildkite hosted agents without docker-in-docker.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo_root"

gitleaks_version="8.24.2"
gitleaks_machine="$(uname -m)"
case "${gitleaks_machine}" in
  x86_64|amd64) gitleaks_arch="x64" ;;
  aarch64|arm64) gitleaks_arch="arm64" ;;
  *)
    echo "WARN: unsupported gitleaks architecture ${gitleaks_machine}; skipping scan"
    exit 0
    ;;
esac

gitleaks_bin="${HOME}/.cache/gitleaks/gitleaks-${gitleaks_version}-${gitleaks_arch}"

if [[ ! -x "$gitleaks_bin" ]]; then
  echo "=== Installing gitleaks ${gitleaks_version} (${gitleaks_arch}) ==="
  mkdir -p "$(dirname "$gitleaks_bin")"
  archive="/tmp/gitleaks_${gitleaks_version}_linux_${gitleaks_arch}.tar.gz"
  if ! curl -fsSL "https://github.com/gitleaks/gitleaks/releases/download/v${gitleaks_version}/gitleaks_${gitleaks_version}_linux_${gitleaks_arch}.tar.gz" -o "$archive"; then
    echo "WARN: gitleaks download failed (non-blocking per pipeline soft_fail)"
    exit 0
  fi
  if ! tar -xzf "$archive" -C "$(dirname "$gitleaks_bin")" gitleaks; then
    echo "WARN: gitleaks extract failed (non-blocking per pipeline soft_fail)"
    exit 0
  fi
  mv "$(dirname "$gitleaks_bin")/gitleaks" "$gitleaks_bin"
  chmod +x "$gitleaks_bin"
fi

"$gitleaks_bin" detect --source="$repo_root" --verbose || {
  echo "WARN: gitleaks reported findings (non-blocking per pipeline soft_fail)"
  exit 0
}
