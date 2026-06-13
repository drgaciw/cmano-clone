#!/usr/bin/env bash
# Mirrors .github/workflows/gitnexus-wiki.yml (manual / release; requires OPENAI_API_KEY)
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo_root"

if [[ -z "${OPENAI_API_KEY:-}" ]]; then
  echo "ERROR: OPENAI_API_KEY is required for gitnexus wiki generation"
  echo "Set in Buildkite pipeline Environment or run locally with .env"
  exit 1
fi

echo "=== Buildkite GitNexus wiki generation ==="

gitnexus analyze

gitnexus wiki \
  --model "${GITNEXUS_WIKI_MODEL:-gpt-4o-mini}" \
  --timeout "${GITNEXUS_WIKI_TIMEOUT:-120}" \
  --retries "${GITNEXUS_WIKI_RETRIES:-3}"

if [[ "${GITNEXUS_WIKI_PUSH:-}" == "1" ]]; then
  if ! git diff --staged --quiet 2>/dev/null || ! git diff --quiet; then
    git config user.name "${GITNEXUS_WIKI_GIT_NAME:-gitnexus-bot}"
    git config user.email "${GITNEXUS_WIKI_GIT_EMAIL:-gitnexus@users.noreply.github.com}"
    git add wiki/ AGENTS.md CLAUDE.md 2>/dev/null || true
    if ! git diff --staged --quiet; then
      git commit -m "docs: auto-update code wiki [skip ci]"
      git push
    fi
  fi
else
  echo "SKIP git push (set GITNEXUS_WIKI_PUSH=1 to commit wiki like GitHub Actions workflow)"
fi

echo "=== PASS ==="
