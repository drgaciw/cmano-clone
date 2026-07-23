#!/usr/bin/env bash
# Promote scratch nightly catalog to assets/data/catalog/aegis_public_corpus.db
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
RUN_DATE="${1:?Usage: tools/cmo-promote-corpus-catalog.sh YYYYMMDD}"
SCRATCH_DIR="${SCRATCH_DIR:-${REPO_ROOT}/scratch/nightly-cmo-${RUN_DATE}}"
SOURCE_DB="${SCRATCH_DIR}/catalog-proposed.db"
TARGET_DB="${REPO_ROOT}/assets/data/catalog/aegis_public_corpus.db"
BALTIC_DB="${REPO_ROOT}/assets/data/catalog/baltic_patrol.db"
MIN_COVERAGE="${MIN_COVERAGE:-99}"

if [[ ! -f "${SOURCE_DB}" ]]; then
  echo "Scratch catalog not found: ${SOURCE_DB}" >&2
  exit 1
fi

echo "=== cmo-promote-corpus-catalog ==="
"${SCRIPT_DIR}/cmo-verify-corpus-coverage.sh" --db "${SOURCE_DB}" --min-coverage "${MIN_COVERAGE}"

mkdir -p "$(dirname "${TARGET_DB}")"
cp "${SOURCE_DB}" "${TARGET_DB}"

if [[ "$(realpath "${TARGET_DB}")" == "$(realpath "${BALTIC_DB}")" ]]; then
  echo "Refusing to overwrite baltic_patrol.db" >&2
  exit 1
fi

echo "Promoted to ${TARGET_DB}"
echo "=== PASS ==="
