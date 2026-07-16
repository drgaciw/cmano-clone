#!/usr/bin/env bash
# Fixture checks for annotate-test-summary.sh (drives the real script; no Buildkite agent).
# Exit 0 only if all cases match expected styles/messages.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
script="$repo_root/tools/buildkite/annotate-test-summary.sh"
tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT

# Isolate PATH so a real buildkite-agent cannot annotate remotely during local tests.
export PATH="/usr/bin:/bin"
hash -r 2>/dev/null || true

run_case() {
  local name="$1"
  local setup="$2"
  local expect_re="$3"
  local work="$tmp/$name"
  mkdir -p "$work/test-results"
  # shellcheck disable=SC2086
  eval "$setup"
  # Script cds to its repo_root; run from a copy that uses REPO overlay via symlink? 
  # annotate uses dirname of itself → real repo. Point test-results via cwd override:
  # We run with a wrapper that replaces test-results in repo — better: run script after
  # placing fixtures under repo_root/test-results and cleaning up.
}

# Use real script against temporary test-results under repo_root, cleaned each case.
results_dir="$repo_root/test-results"
cleanup_results() {
  rm -rf "$results_dir"
}
trap 'cleanup_results; rm -rf "$tmp"' EXIT
cleanup_results

fail=0
pass=0

assert_case() {
  local name="$1"
  local setup_fn="$2"
  local must_match="$3"
  local must_not_match="${4:-}"
  cleanup_results
  mkdir -p "$results_dir"
  "$setup_fn"
  local out
  out="$(bash "$script" 2>&1)" || true
  if ! grep -Eq "$must_match" <<<"$out"; then
    echo "FAIL $name: expected /$must_match/ in output:"
    echo "$out"
    fail=$((fail + 1))
    return
  fi
  if [[ -n "$must_not_match" ]] && grep -Eq "$must_not_match" <<<"$out"; then
    echo "FAIL $name: must not match /$must_not_match/:"
    echo "$out"
    fail=$((fail + 1))
    return
  fi
  echo "PASS $name"
  pass=$((pass + 1))
  # Save last output for evidence when caller sets ANNOTATE_TEST_LOG
  if [[ -n "${ANNOTATE_TEST_LOG:-}" ]]; then
    {
      echo "=== $name ==="
      echo "$out"
    } >>"$ANNOTATE_TEST_LOG"
  fi
}

setup_no_trx() {
  : # empty test-results
}

setup_unparsed() {
  # TRX present but no parseable Counters total=
  printf '%s\n' '<?xml version="1.0"?><TestRun><Results/></TestRun>' \
    >"$results_dir/broken.trx"
}

setup_failed() {
  printf '%s\n' \
    '<?xml version="1.0"?>' \
    '<TestRun>' \
    '  <ResultSummary outcome="Failed">' \
    '    <Counters total="10" executed="10" passed="8" failed="2" error="0" timeout="0" aborted="0" inconclusive="0" passedButRunAborted="0" notExecuted="0" notRunnable="0" disconnected="0" warning="0" completed="0" inProgress="0" pending="0"/>' \
    '  </ResultSummary>' \
    '</TestRun>' >"$results_dir/fail.trx"
}

setup_success() {
  printf '%s\n' \
    '<?xml version="1.0"?>' \
    '<TestRun>' \
    '  <ResultSummary outcome="Completed">' \
    '    <Counters total="5" executed="5" passed="5" failed="0" error="0" timeout="0" aborted="0" inconclusive="0" passedButRunAborted="0" notExecuted="0" notRunnable="0" disconnected="0" warning="0" completed="0" inProgress="0" pending="0"/>' \
    '  </ResultSummary>' \
    '</TestRun>' >"$results_dir/ok.trx"
}

setup_zero_total() {
  printf '%s\n' \
    '<?xml version="1.0"?>' \
    '<TestRun>' \
    '  <ResultSummary outcome="Completed">' \
    '    <Counters total="0" executed="0" passed="0" failed="0" error="0" notExecuted="0"/>' \
    '  </ResultSummary>' \
    '</TestRun>' >"$results_dir/zero.trx"
}

assert_case "no_trx" setup_no_trx "no \\.trx files found" "white_check_mark"
assert_case "unparsed" setup_unparsed "warning|:warning:|unparsed|empty" "white_check_mark"
assert_case "failed" setup_failed ":x:|style error|failed=2|Failed" "white_check_mark"
assert_case "success" setup_success "white_check_mark|passed=5" "unparsed"
assert_case "zero_total" setup_zero_total "warning|:warning:|total is 0|not treating as success" "white_check_mark"

echo "=== test-annotate-test-summary: $pass passed, $fail failed ==="
[[ $fail -eq 0 ]]
