#!/usr/bin/env bash
# Mirrors .github/workflows/gitnexus-pr-analysis.yml (PR blast radius / detect_changes)
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo_root"

impact_json="/tmp/gitnexus-impact.json"
impact_md="/tmp/gitnexus-impact.md"

annotate() {
  if command -v buildkite-agent >/dev/null 2>&1; then
    buildkite-agent annotate "$1" --style "${2:-info}" --context gitnexus-pr
  fi
}

echo "=== Buildkite GitNexus PR analysis ==="

mkdir -p .gitnexus/logs

gitnexus analyze 2>&1 | tee .gitnexus/logs/analyze.log || {
  echo "WARN: gitnexus analyze failed (non-blocking per pipeline soft_fail)"
  exit 0
}

set +e
gitnexus detect_changes --scope all --format json > "$impact_json" 2>&1
detect_exit=$?
set -e
cat "$impact_json"

if [[ $detect_exit -ne 0 ]]; then
  echo "WARN: gitnexus detect_changes exited $detect_exit (continuing with captured output)"
fi

{
  echo "## GitNexus Blast Radius Report"
  echo
  if command -v jq >/dev/null 2>&1 && jq empty "$impact_json" 2>/dev/null; then
    risk="$(jq -r '.summary.risk_level // "unknown"' "$impact_json")"
    changed="$(jq -r '.summary.changed_count // 0' "$impact_json")"
    affected="$(jq -r '.summary.affected_count // 0' "$impact_json")"
    echo "- **Risk:** ${risk}"
    echo "- **Changed symbols:** ${changed}"
    echo "- **Affected symbols:** ${affected}"
    echo
    echo '<details><summary>Full JSON</summary>'
    echo
    echo '```json'
    cat "$impact_json"
    echo '```'
    echo '</details>'
  else
    echo '```'
    cat "$impact_json"
    echo '```'
  fi
} > "$impact_md"

annotate "$(cat "$impact_md")" "info"

if [[ -n "${BUILDKITE_PULL_REQUEST:-}" ]] && command -v gh >/dev/null 2>&1; then
  if gh auth status >/dev/null 2>&1; then
    echo "=== Posting PR comment via gh ==="
    gh pr comment "$BUILDKITE_PULL_REQUEST" --body-file "$impact_md" || echo "WARN: gh pr comment failed (non-blocking)"
  else
    echo "SKIP: gh not authenticated — PR comment omitted"
  fi
fi

if command -v buildkite-agent >/dev/null 2>&1; then
  buildkite-agent artifact upload "$impact_json" || true
  buildkite-agent artifact upload "$impact_md" || true
fi

echo "=== PASS ==="
