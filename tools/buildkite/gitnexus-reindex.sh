#!/usr/bin/env bash
# Mirrors .github/workflows/gitnexus-reindex.yml (main push, skip doc-only paths)
#
# Task 6 (2026-07-09 CI optimization pass): the pipeline used to wrap this whole step
# in a blanket `soft_fail: true`, which swallowed every non-zero exit indiscriminately
# — including real infra bugs (permission errors, disk full writing .gitnexus/logs,
# etc.), not just the GitNexus CLI's own known best-effort failure modes. This script
# now catches the two commands that can legitimately fail in a "reindex is best-effort,
# don't block main" way (`gitnexus analyze` / `gitnexus status`) and exits with the
# dedicated sentinel GITNEXUS_SOFT_FAIL_EXIT (75 = EX_TEMPFAIL, sysexits.h — a
# well-known "temporary, non-fatal failure" code). .buildkite/pipeline.yml scopes
# `soft_fail:` to exactly that exit status, so any OTHER exit code now genuinely fails
# the build instead of being silently hidden.
set -euo pipefail

GITNEXUS_SOFT_FAIL_EXIT=75

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

upload_logs() {
  if command -v buildkite-agent >/dev/null 2>&1; then
    buildkite-agent artifact upload ".gitnexus/logs/*" || true
  fi
}

if ! gitnexus analyze 2>&1 | tee .gitnexus/logs/analyze.log; then
  echo "WARN: gitnexus analyze failed — known non-blocking GitNexus CLI failure mode (reindex is best-effort by design, does not gate PR merges)"
  annotate "**GitNexus reindex degraded** — \`gitnexus analyze\` failed; see job artifacts (\`.gitnexus/logs/analyze.log\`). Non-blocking (best-effort)." "warning"
  upload_logs
  exit "$GITNEXUS_SOFT_FAIL_EXIT"
fi

if ! gitnexus status 2>&1 | tee .gitnexus/logs/status.log; then
  echo "WARN: gitnexus status failed — known non-blocking GitNexus CLI failure mode (reindex is best-effort by design, does not gate PR merges)"
  annotate "**GitNexus reindex degraded** — \`gitnexus status\` failed after a successful analyze; see job artifacts. Non-blocking (best-effort)." "warning"
  upload_logs
  exit "$GITNEXUS_SOFT_FAIL_EXIT"
fi

upload_logs
if command -v buildkite-agent >/dev/null 2>&1; then
  buildkite-agent artifact upload "gitnexus-version.txt" || true
fi

annotate "**GitNexus reindex complete** — see job artifacts for \`.gitnexus/logs/\`." "success"
echo "=== PASS ==="
