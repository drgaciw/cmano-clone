#!/usr/bin/env bash
# S27-02 — Off-CI nightly CMO corpus propose-only import (sensor + weapon v1).
# Not wired into dotnet test CI. Requires human approve via catalog_write_approve.
set -euo pipefail

export PATH="${HOME}/.dotnet:${PATH}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
cd "${REPO_ROOT}"

CHUNK_SIZE="${CHUNK_SIZE:-500}"
MAX_RECORDS="${MAX_RECORDS:-}"
MAX_RECORDS_FLAG=()
if [[ -n "${MAX_RECORDS}" ]]; then
  MAX_RECORDS_FLAG=(--max-records "${MAX_RECORDS}")
fi
RUN_DATE="$(date -u +%Y%m%d)"
SCRATCH_DIR="${REPO_ROOT}/scratch/nightly-cmo-${RUN_DATE}"
DB_PATH="${SCRATCH_DIR}/catalog-proposed.db"
SENSOR_MD="${REPO_ROOT}/docs/reference/cmano-db/sensor.md"
WEAPON_MD="${REPO_ROOT}/docs/reference/cmano-db/weapon.md"
CLI_PROJECT="src/ProjectAegis.MissionEditor.Cli/ProjectAegis.MissionEditor.Cli.csproj"

mkdir -p "${SCRATCH_DIR}"

if [[ ! -f "${SENSOR_MD}" ]]; then
  echo "Missing sensor corpus: ${SENSOR_MD}" >&2
  exit 1
fi

if [[ ! -f "${WEAPON_MD}" ]]; then
  echo "Missing weapon corpus: ${WEAPON_MD}" >&2
  exit 1
fi

echo "==> Nightly CMO import (propose-only) @ ${RUN_DATE}"
echo "    DB: ${DB_PATH}"
echo "    Chunk: ${CHUNK_SIZE}"

dotnet run --project "${CLI_PROJECT}" --no-build -- \
  catalog_import_markdown \
  --db "${DB_PATH}" \
  --markdown "${SENSOR_MD}" \
  --entity sensor \
  --chunk-size "${CHUNK_SIZE}" \
  "${MAX_RECORDS_FLAG[@]}" \
  --report-out "${SCRATCH_DIR}/sensor-quarantine.json" \
  > "${SCRATCH_DIR}/sensor-propose.json"

dotnet run --project "${CLI_PROJECT}" --no-build -- \
  catalog_import_markdown \
  --db "${DB_PATH}" \
  --markdown "${WEAPON_MD}" \
  --entity weapon \
  --chunk-size "${CHUNK_SIZE}" \
  "${MAX_RECORDS_FLAG[@]}" \
  --report-out "${SCRATCH_DIR}/weapon-quarantine.json" \
  > "${SCRATCH_DIR}/weapon-propose.json"

echo "==> Done. Review staged batches in ${DB_PATH}; approve manually before commit."
echo "    Sensor report: ${SCRATCH_DIR}/sensor-quarantine.json"
echo "    Weapon report: ${SCRATCH_DIR}/weapon-quarantine.json"