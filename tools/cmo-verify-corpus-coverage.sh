#!/usr/bin/env bash
# Compare public corpus H3 counts vs live catalog rows (enterprise >= 99% gate).
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
DB_PATH=""
MIN_COVERAGE="99"
CORPUS_ROOT=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --db) DB_PATH="${2:?}"; shift 2 ;;
    --min-coverage) MIN_COVERAGE="${2:?}"; shift 2 ;;
    --corpus-root) CORPUS_ROOT="${2:?}"; shift 2 ;;
    *) echo "Unknown arg: $1" >&2; exit 1 ;;
  esac
done

if [[ -z "${DB_PATH}" || ! -f "${DB_PATH}" ]]; then
  echo "Missing or invalid --db" >&2
  exit 1
fi

args=(--db "${DB_PATH}" --min-coverage "${MIN_COVERAGE}")
if [[ -n "${CORPUS_ROOT}" ]]; then
  args+=(--corpus-root "${CORPUS_ROOT}")
fi

python3 "${SCRIPT_DIR}/cmo_verify_corpus_coverage.py" "${args[@]}"
