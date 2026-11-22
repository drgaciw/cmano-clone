#!/usr/bin/env bash
# Bash equivalent of copy-delegation-assemblies.ps1 for hosts without pwsh.
# Publishes netstandard2.1 UnityAdapter + transitive deps into Assets/Plugins/ProjectAegis.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PLUGINS="$ROOT/unity/ProjectAegis/Assets/Plugins/ProjectAegis"
PUBLISH="$ROOT/.tmp-unity-plugin-publish"
TFM=netstandard2.1

mkdir -p "$PLUGINS"
rm -rf "$PUBLISH"

export PATH="${HOME}/.dotnet:${PATH}"
dotnet publish "$ROOT/src/ProjectAegis.Delegation.UnityAdapter/ProjectAegis.Delegation.UnityAdapter.csproj" \
  -c Release -f "$TFM" -o "$PUBLISH" -v minimal

cp -f "$PUBLISH"/*.dll "$PLUGINS"/
if [ -f "$PUBLISH/runtimes/linux-x64/native/libe_sqlite3.so" ]; then
  cp -f "$PUBLISH/runtimes/linux-x64/native/libe_sqlite3.so" "$PLUGINS"/
else
  SQLITE_SO="$(find "${HOME}/.nuget/packages/sqlitepclraw.lib.e_sqlite3" -name "libe_sqlite3.so" -path "*linux-x64*" 2>/dev/null | sort -V | tail -n 1 || true)"
  if [ -n "$SQLITE_SO" ] && [ -f "$SQLITE_SO" ]; then
    cp -f "$SQLITE_SO" "$PLUGINS"/
  fi
fi
rm -rf "$PUBLISH"

echo "Copied delegation assemblies and dependencies to $PLUGINS"
ls -1 "$PLUGINS"/*.dll | wc -l | xargs -I{} echo "DLL count: {}"
