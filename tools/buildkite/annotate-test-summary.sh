#!/usr/bin/env bash
# Task 7 (2026-07-09 CI optimization pass): summarize pass/fail/skip counts from the
# sharded test step's .trx results as a buildkite-agent annotation. Runs as its own
# step (depends_on: test, allow_dependency_failure: true) so the summary still shows
# up when a shard fails. Flaky-test detail will come from Buildkite Test Analytics
# once BUILDKITE_ANALYTICS_TOKEN is wired (see docs/engineering/buildkite-ci.md) — for
# now this only aggregates the raw .trx <Counters/> totals.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo_root"

mkdir -p test-results

if command -v buildkite-agent >/dev/null 2>&1; then
  buildkite-agent artifact download "test-results/**/*.trx" . || true
fi

shopt -s nullglob globstar
trx_files=(test-results/**/*.trx)

if [[ ${#trx_files[@]} -eq 0 ]]; then
  echo "WARN: no .trx files found under test-results/ — skipping annotation"
  exit 0
fi

total=0
passed=0
failed=0
skipped=0

for f in "${trx_files[@]}"; do
  # dotnet test TRX summary line, e.g.:
  # <Counters total="42" executed="42" passed="40" failed="1" error="0" ... notExecuted="1" .../>
  # Portable bash [[ =~ ]] — avoid grep -oP (GNU PCRE; missing on some agents / macOS).
  line="$(grep -o '<Counters[^/]*/>' "$f" || true)"
  [[ -z "$line" ]] && continue
  t=0
  p=0
  fl=0
  sk=0
  [[ "$line" =~ total=\"([0-9]+)\" ]] && t="${BASH_REMATCH[1]}"
  [[ "$line" =~ passed=\"([0-9]+)\" ]] && p="${BASH_REMATCH[1]}"
  [[ "$line" =~ failed=\"([0-9]+)\" ]] && fl="${BASH_REMATCH[1]}"
  [[ "$line" =~ notExecuted=\"([0-9]+)\" ]] && sk="${BASH_REMATCH[1]}"
  total=$((total + t))
  passed=$((passed + p))
  failed=$((failed + fl))
  skipped=$((skipped + sk))
done

style="success"
icon=":white_check_mark:"
if [[ $failed -gt 0 ]]; then
  style="error"
  icon=":x:"
fi

summary="### Test Results ${icon}

| Total | Passed | Failed | Skipped |
|---|---|---|---|
| ${total} | ${passed} | ${failed} | ${skipped} |

Aggregated from ${#trx_files[@]} .trx file(s) across \`BUILDKITE_PARALLEL_JOB_COUNT\` shards. Flaky-test detection needs Buildkite Test Analytics wired (\`BUILDKITE_ANALYTICS_TOKEN\` — not yet set, see docs/engineering/buildkite-ci.md)."

if command -v buildkite-agent >/dev/null 2>&1; then
  buildkite-agent annotate "$summary" --style "$style" --context test-summary
else
  echo "$summary"
fi

echo "=== Test summary: total=${total} passed=${passed} failed=${failed} skipped=${skipped} ==="
