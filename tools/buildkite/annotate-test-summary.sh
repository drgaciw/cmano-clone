#!/usr/bin/env bash
# GROUNDWORK ONLY — not called by live .buildkite/pipeline.yml.
# Summarize pass/fail/skip from .trx <Counters/> for a future annotation step.
# Empty/unparsed totals use warning (never false success). Run fixture checks via
# tools/buildkite/test-annotate-test-summary.sh.
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
# Count of .trx files that yielded at least one parseable total="N" attribute.
# Files present with unparseable/missing <Counters/> must not be reported as success.
parsed_files=0

for f in "${trx_files[@]}"; do
  # dotnet test TRX summary line, e.g.:
  # <Counters total="42" executed="42" passed="40" failed="1" error="0" ... notExecuted="1" .../>
  # Portable bash [[ =~ ]] — avoid grep -oP (GNU PCRE; missing on some agents / macOS).
  line="$(grep -o '<Counters[^/]*/>' "$f" || true)"
  [[ -z "$line" ]] && continue
  t=""
  p=0
  fl=0
  sk=0
  if [[ "$line" =~ total=\"([0-9]+)\" ]]; then
    t="${BASH_REMATCH[1]}"
  else
    # Counters tag present but total= missing — treat as unparsed for this file.
    continue
  fi
  [[ "$line" =~ passed=\"([0-9]+)\" ]] && p="${BASH_REMATCH[1]}"
  [[ "$line" =~ failed=\"([0-9]+)\" ]] && fl="${BASH_REMATCH[1]}"
  [[ "$line" =~ notExecuted=\"([0-9]+)\" ]] && sk="${BASH_REMATCH[1]}"
  total=$((total + t))
  passed=$((passed + p))
  failed=$((failed + fl))
  skipped=$((skipped + sk))
  parsed_files=$((parsed_files + 1))
done

style="success"
icon=":white_check_mark:"
note=""
if [[ $failed -gt 0 ]]; then
  style="error"
  icon=":x:"
elif [[ $parsed_files -eq 0 || $total -eq 0 ]]; then
  # Never green-annotate empty/unparsed aggregates (false success on corrupt TRX).
  style="warning"
  icon=":warning:"
  if [[ $parsed_files -eq 0 ]]; then
    note=$'\n\n**Warning:** found '"${#trx_files[@]}"' .trx file(s) but no parseable `<Counters total=…/>` — summary is empty/unparsed, not a pass.'
  else
    note=$'\n\n**Warning:** parsed total is 0 across '"$parsed_files"' file(s) — not treating as success.'
  fi
fi

summary="### Test Results ${icon}

| Total | Passed | Failed | Skipped |
|---|---|---|---|
| ${total} | ${passed} | ${failed} | ${skipped} |

Aggregated from ${#trx_files[@]} .trx file(s) (${parsed_files} with parseable counters) across \`BUILDKITE_PARALLEL_JOB_COUNT\` shards. Flaky-test detection needs Buildkite Test Analytics wired (\`BUILDKITE_ANALYTICS_TOKEN\` — not yet set, see docs/engineering/buildkite-ci.md).${note}"

if command -v buildkite-agent >/dev/null 2>&1; then
  buildkite-agent annotate "$summary" --style "$style" --context test-summary
else
  echo "$summary"
fi

echo "=== Test summary: total=${total} passed=${passed} failed=${failed} skipped=${skipped} ==="
