#!/usr/bin/env bash
# Mirrors .github/workflows/gitnexus-reindex.yml (main push, skip doc-only paths)
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo_root"

annotate() {
  if command -v buildkite-agent >/dev/null 2>&1; then
    buildkite-agent annotate "$1" --style "${2:-info}" --context gitnexus-reindex
  fi
}

should_skip_doc_only_push() {
  if [[ "${BUILDKITE_BRANCH:-}" != "main" ]]; then
    return 1
  fi
  if [[ "${GITNEXUS_FORCE_REINDEX:-}" == "1" ]]; then
    return 1
  fi

  local base head
  if git rev-parse --verify "${BUILDKITE_COMMIT:-HEAD}^" >/dev/null 2>&1; then
    base="${BUILDKITE_COMMIT:-HEAD}^"
    head="${BUILDKITE_COMMIT:-HEAD}"
  elif git rev-parse --verify HEAD~1 >/dev/null 2>&1; then
    base="HEAD~1"
    head="HEAD"
  else
    return 1
  fi

  local changed
  changed="$(git diff --name-only "$base" "$head" || true)"
  if [[ -z "$changed" ]]; then
    return 1
  fi

  while IFS= read -r path; do
    [[ -z "$path" ]] && continue
    if [[ "$path" == *.md ]] \
      || [[ "$path" == docs/* ]] \
      || [[ "$path" == .gitignore ]] \
      || [[ "$path" == production/dashboard-state.yaml ]]; then
      continue
    fi
    return 1
  done <<< "$changed"

  return 0
}

if should_skip_doc_only_push; then
  echo "=== SKIP: doc/metadata-only push (parity with gitnexus-reindex.yml paths-ignore) ==="
  annotate "**GitNexus reindex skipped** — only docs/metadata changed on \`main\`. Set \`GITNEXUS_FORCE_REINDEX=1\` to override." "info"
  exit 0
fi

echo "=== Buildkite GitNexus reindex ==="

mkdir -p .gitnexus/logs

gitnexus analyze 2>&1 | tee .gitnexus/logs/analyze.log
gitnexus status 2>&1 | tee .gitnexus/logs/status.log

if command -v buildkite-agent >/dev/null 2>&1; then
  buildkite-agent artifact upload ".gitnexus/logs/*" || true
  buildkite-agent artifact upload "gitnexus-version.txt" || true
fi

annotate "**GitNexus reindex complete** — see job artifacts for \`.gitnexus/logs/\`." "success"
echo "=== PASS ==="
