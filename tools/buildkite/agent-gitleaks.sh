#!/usr/bin/env bash
# Gitleaks without docker-in-docker.
# The release archive is pinned by sha256. A gitleaks binary already on PATH
# is never used. GitHub Actions installs under $RUNNER_TEMP, not ~/.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo_root"

gitleaks_version="8.24.2"
gitleaks_machine="$(uname -m)"
case "${gitleaks_machine}" in
  x86_64|amd64)
    gitleaks_arch="x64"
    gitleaks_sha256="fa0500f6b7e41d28791ebc680f5dd9899cd42b58629218a5f041efa899151a8e"
    ;;
  aarch64|arm64)
    gitleaks_arch="arm64"
    gitleaks_sha256="574a6d52573c61173add7ddb5e3cc68c0e82cb0735818a1eeb9a0a2de1643fbc"
    ;;
  *)
    echo "WARN: unsupported gitleaks architecture ${gitleaks_machine}; skipping scan"
    exit 0
    ;;
esac

if [[ -n "${RUNNER_TEMP:-}" ]]; then
  gitleaks_root="${RUNNER_TEMP}/aegis-tools/gitleaks"
else
  gitleaks_root="${HOME}/.cache/gitleaks"
fi
mkdir -p "$gitleaks_root"
archive="${gitleaks_root}/gitleaks_${gitleaks_version}_linux_${gitleaks_arch}.tar.gz"
gitleaks_bin="${gitleaks_root}/gitleaks-${gitleaks_version}-${gitleaks_arch}"

echo "=== Installing gitleaks ${gitleaks_version} (${gitleaks_arch}) ==="
if ! curl -fsSL "https://github.com/gitleaks/gitleaks/releases/download/v${gitleaks_version}/gitleaks_${gitleaks_version}_linux_${gitleaks_arch}.tar.gz" -o "$archive"; then
  echo "WARN: gitleaks download failed (non-blocking per pipeline soft_fail)"
  exit 0
fi
if ! echo "${gitleaks_sha256}  ${archive}" | sha256sum -c -; then
  echo "ERROR: gitleaks archive sha256 mismatch; not executing" >&2
  rm -f "$archive"
  exit 1
fi
extract_dir="${gitleaks_root}/extract-${gitleaks_arch}"
rm -rf "$extract_dir"
mkdir -p "$extract_dir"
if ! tar -xzf "$archive" -C "$extract_dir" gitleaks; then
  echo "WARN: gitleaks extract failed (non-blocking per pipeline soft_fail)"
  exit 0
fi
mv "$extract_dir/gitleaks" "$gitleaks_bin"
chmod +x "$gitleaks_bin"
rm -rf "$extract_dir"

"$gitleaks_bin" detect --source="$repo_root" --verbose || {
  echo "WARN: gitleaks reported findings (non-blocking per pipeline soft_fail)"
  exit 0
}
