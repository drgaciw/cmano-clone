#!/usr/bin/env bash
# Install official Buildkite agent skills into this repo.
# Source: https://github.com/buildkite/skills (MIT)
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
TMP="${TMPDIR:-/tmp}/buildkite-skills-install"
REPO_URL="${BUILDKITE_SKILLS_REPO:-https://github.com/buildkite/skills.git}"
REF="${BUILDKITE_SKILLS_REF:-main}"

SKILLS=(
  buildkite-pipelines
  buildkite-migration
  buildkite-preflight
  buildkite-agent-runtime
  buildkite-cli
  buildkite-api
)

echo "==> Fetching ${REPO_URL}@${REF}"
rm -rf "$TMP"
git clone --depth 1 --branch "$REF" "$REPO_URL" "$TMP"

for skill in "${SKILLS[@]}"; do
  src="$TMP/skills/$skill"
  if [[ ! -d "$src" ]]; then
    echo "Missing skill directory: $skill" >&2
    exit 1
  fi
  for dest_root in "$ROOT/.claude/skills" "$ROOT/.cursor/skills"; do
    mkdir -p "$dest_root"
    rm -rf "$dest_root/$skill"
    cp -r "$src" "$dest_root/$skill"
    echo "Installed $skill -> $dest_root/$skill"
  done
done

echo "==> Done. Agents: .claude/agents/buildkite-*.md"
echo "    Docs: docs/engineering/buildkite-agent-skills.md"
