#!/usr/bin/env bash
# Gitleaks on Buildkite hosted agents without docker-in-docker.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo_root"

gitleaks_bin="${HOME}/.cache/gitleaks/gitleaks"
gitleaks_version="8.24.2"

if [[ ! -x "$gitleaks_bin" ]]; then
  echo "=== Installing gitleaks ${gitleaks_version} ==="
  mkdir -p "$(dirname "$gitleaks_bin")"
  archive="/tmp/gitleaks_${gitleaks_version}_linux_x64.tar.gz"
  curl -sSL "https://github.com/gitleaks/gitleaks/releases/download/v${gitleaks_version}/gitleaks_${gitleaks_version}_linux_x64.tar.gz" -o "$archive"
  tar -xzf "$archive" -C "$(dirname "$gitleaks_bin")" gitleaks
  mv "$(dirname "$gitleaks_bin")/gitleaks" "$gitleaks_bin"
  chmod +x "$gitleaks_bin"
fi

"$gitleaks_bin" detect --source="$repo_root" --verbose
