#!/usr/bin/env bash
# S28-02 / S29-03 / S30-04 / S30-11 — Off-CI nightly CMO corpus propose-only import.
# Not wired into dotnet test CI. Approve via tools/cmo-nightly-approve.sh or catalog_write_approve.
set -euo pipefail

export PATH="${HOME}/.dotnet:${PATH}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
cd "${REPO_ROOT}"

usage() {
  cat <<'EOF'
Usage: tools/cmo-nightly-import.sh [options]

Off-CI nightly CMO corpus propose-only import. Stages batches in a scratch DB;
approve via tools/cmo-nightly-approve.sh (or catalog_write_approve) before any catalog commit.

Options:
  --entity <sensor|weapon|platform|aircraft|submarine|facility|ground-unit|all>  Entities to import (default: all)
  --chunk-size <N>                       Records per batch (default: 500)
  --max-records <N>                      Cap parsed records per entity (smoke/CI gate)
  --propose-only                         Propose-only mode (default; no auto-approve)
  --dry-run                              Validate paths and print planned runs only
  -h, --help                             Show this help

Environment overrides:
  CHUNK_SIZE, MAX_RECORDS, PLATFORM_MD, AIRCRAFT_MD, SUBMARINE_MD, FACILITY_MD, GROUND_UNIT_MD

Examples:
  MAX_RECORDS=12 ./tools/cmo-nightly-import.sh
  ./tools/cmo-nightly-import.sh --entity platform --chunk-size 500 --propose-only
  ./tools/cmo-nightly-import.sh --entity aircraft --dry-run
  AIRCRAFT_MD=tools/cmano-db-crawler/fixtures/aircraft-slice-100.md \
    MAX_RECORDS=12 ./tools/cmo-nightly-import.sh --entity aircraft --propose-only
EOF
}

ENTITY="${ENTITY:-all}"
CHUNK_SIZE="${CHUNK_SIZE:-500}"
MAX_RECORDS="${MAX_RECORDS:-}"
PROPOSE_ONLY=1
DRY_RUN=0

while [[ $# -gt 0 ]]; do
  case "$1" in
    --entity)
      ENTITY="${2:?--entity requires sensor|weapon|platform|aircraft|submarine|facility|ground-unit|all}"
      shift 2
      ;;
    --chunk-size)
      CHUNK_SIZE="${2:?--chunk-size requires a number}"
      shift 2
      ;;
    --max-records)
      MAX_RECORDS="${2:?--max-records requires a number}"
      shift 2
      ;;
    --propose-only)
      PROPOSE_ONLY=1
      shift
      ;;
    --dry-run)
      DRY_RUN=1
      shift
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      usage >&2
      exit 1
      ;;
  esac
done

case "${ENTITY}" in
  sensor|weapon|platform|aircraft|submarine|facility|ground-unit|all) ;;
  *)
    echo "Invalid --entity '${ENTITY}'. Use sensor, weapon, platform, aircraft, submarine, facility, ground-unit, or all." >&2
    exit 1
    ;;
esac

MAX_RECORDS_FLAG=()
if [[ -n "${MAX_RECORDS}" ]]; then
  MAX_RECORDS_FLAG=(--max-records "${MAX_RECORDS}")
fi

RUN_DATE="$(date -u +%Y%m%d)"
SCRATCH_DIR="${REPO_ROOT}/scratch/nightly-cmo-${RUN_DATE}"
DB_PATH="${SCRATCH_DIR}/catalog-proposed.db"
SENSOR_MD="${REPO_ROOT}/docs/reference/cmano-db/sensor.md"
WEAPON_MD="${REPO_ROOT}/docs/reference/cmano-db/weapon.md"
PLATFORM_MD="${PLATFORM_MD:-${REPO_ROOT}/docs/reference/cmano-db/ship.md}"
AIRCRAFT_MD="${AIRCRAFT_MD:-${REPO_ROOT}/docs/reference/cmano-db/aircraft.md}"
SUBMARINE_MD="${SUBMARINE_MD:-${REPO_ROOT}/docs/reference/cmano-db/submarine.md}"
FACILITY_MD="${FACILITY_MD:-${REPO_ROOT}/docs/reference/cmano-db/facility.md}"
GROUND_UNIT_MD="${GROUND_UNIT_MD:-${REPO_ROOT}/docs/reference/cmano-db/ground-units.md}"
SHIP_SLICE_MD="${REPO_ROOT}/tools/cmano-db-crawler/fixtures/ship-slice-100.md"
AIRCRAFT_SLICE_MD="${REPO_ROOT}/tools/cmano-db-crawler/fixtures/aircraft-slice-100.md"
SUBMARINE_SLICE_MD="${REPO_ROOT}/tools/cmano-db-crawler/fixtures/submarine-slice-100.md"
FACILITY_SLICE_MD="${REPO_ROOT}/tools/cmano-db-crawler/fixtures/facility-slice-100.md"
CLI_PROJECT="src/ProjectAegis.MissionEditor.Cli/ProjectAegis.MissionEditor.Cli.csproj"

entity_selected() {
  local name="$1"
  [[ "${ENTITY}" == "all" || "${ENTITY}" == "${name}" ]]
}

import_entity() {
  local entity="$1"
  local markdown="$2"
  local report_out="${SCRATCH_DIR}/${entity}-quarantine.json"
  local propose_out="${SCRATCH_DIR}/${entity}-propose.json"

  echo "==> Import ${entity}"
  echo "    Markdown: ${markdown}"
  echo "    Report:   ${report_out}"

  if [[ "${DRY_RUN}" -eq 1 ]]; then
    return 0
  fi

  dotnet run --project "${CLI_PROJECT}" --no-build -- \
    catalog_import_markdown \
    --db "${DB_PATH}" \
    --markdown "${markdown}" \
    --entity "${entity}" \
    --chunk-size "${CHUNK_SIZE}" \
    "${MAX_RECORDS_FLAG[@]}" \
    --report-out "${report_out}" \
    > "${propose_out}"
}

mkdir -p "${SCRATCH_DIR}"

if entity_selected sensor; then
  if [[ ! -f "${SENSOR_MD}" ]]; then
    echo "Missing sensor corpus: ${SENSOR_MD}" >&2
    exit 1
  fi
fi

if entity_selected weapon; then
  if [[ ! -f "${WEAPON_MD}" ]]; then
    echo "Missing weapon corpus: ${WEAPON_MD}" >&2
    exit 1
  fi
fi

if entity_selected platform; then
  if [[ ! -f "${PLATFORM_MD}" ]]; then
    echo "Missing platform corpus: ${PLATFORM_MD}" >&2
    exit 1
  fi
  if [[ ! -f "${SHIP_SLICE_MD}" ]]; then
    echo "Missing curated platform fixture: ${SHIP_SLICE_MD}" >&2
    exit 1
  fi
fi

if entity_selected aircraft; then
  if [[ ! -f "${AIRCRAFT_MD}" ]]; then
    echo "Missing aircraft corpus: ${AIRCRAFT_MD}" >&2
    exit 1
  fi
  if [[ ! -f "${AIRCRAFT_SLICE_MD}" ]]; then
    echo "Missing curated aircraft fixture: ${AIRCRAFT_SLICE_MD}" >&2
    exit 1
  fi
fi

if entity_selected submarine; then
  if [[ ! -f "${SUBMARINE_MD}" ]]; then
    echo "Missing submarine corpus: ${SUBMARINE_MD}" >&2
    exit 1
  fi
  if [[ ! -f "${SUBMARINE_SLICE_MD}" ]]; then
    echo "Missing curated submarine fixture: ${SUBMARINE_SLICE_MD}" >&2
    exit 1
  fi
fi

if entity_selected facility; then
  if [[ ! -f "${FACILITY_MD}" ]]; then
    echo "Missing facility corpus: ${FACILITY_MD}" >&2
    exit 1
  fi
  if [[ ! -f "${FACILITY_SLICE_MD}" ]]; then
    echo "Missing curated facility fixture: ${FACILITY_SLICE_MD}" >&2
    exit 1
  fi
fi

if entity_selected ground-unit; then
  if [[ ! -f "${GROUND_UNIT_MD}" ]]; then
    echo "Missing ground-unit corpus: ${GROUND_UNIT_MD}" >&2
    exit 1
  fi
fi

echo "==> Nightly CMO import (propose-only) @ ${RUN_DATE}"
echo "    DB: ${DB_PATH}"
echo "    Entity: ${ENTITY}"
echo "    Chunk: ${CHUNK_SIZE}"
echo "    Propose-only: ${PROPOSE_ONLY}"
if [[ -n "${MAX_RECORDS}" ]]; then
  echo "    Max records: ${MAX_RECORDS}"
fi
if [[ "${DRY_RUN}" -eq 1 ]]; then
  echo "    Mode: dry-run (no dotnet invocations)"
fi

if [[ "${DRY_RUN}" -eq 0 ]]; then
  dotnet build "${CLI_PROJECT}" -v minimal -nologo
fi

if entity_selected sensor; then
  import_entity sensor "${SENSOR_MD}"
fi

if entity_selected weapon; then
  import_entity weapon "${WEAPON_MD}"
fi

if entity_selected platform; then
  import_entity platform "${PLATFORM_MD}"
  echo "    Curated fixture (CI gate): ${SHIP_SLICE_MD}"
fi

if entity_selected aircraft; then
  import_entity aircraft "${AIRCRAFT_MD}"
  echo "    Curated fixture (CI gate): ${AIRCRAFT_SLICE_MD}"
fi

if entity_selected submarine; then
  import_entity submarine "${SUBMARINE_MD}"
  echo "    Curated fixture (CI gate): ${SUBMARINE_SLICE_MD}"
fi

if entity_selected facility; then
  import_entity facility "${FACILITY_MD}"
  echo "    Curated fixture (CI gate): ${FACILITY_SLICE_MD}"
fi

if entity_selected ground-unit; then
  import_entity ground-unit "${GROUND_UNIT_MD}"
fi

if [[ "${DRY_RUN}" -eq 1 ]]; then
  echo "==> Dry-run complete. Paths validated; no batches staged."
  exit 0
fi

echo "==> Done. Review staged batches in ${DB_PATH}."
echo "    Approve: ./tools/cmo-nightly-approve.sh --entity ${ENTITY} --run-date ${RUN_DATE}"
if entity_selected sensor; then
  echo "    Sensor report: ${SCRATCH_DIR}/sensor-quarantine.json"
fi
if entity_selected weapon; then
  echo "    Weapon report: ${SCRATCH_DIR}/weapon-quarantine.json"
fi
if entity_selected platform; then
  echo "    Platform report: ${SCRATCH_DIR}/platform-quarantine.json"
fi
if entity_selected aircraft; then
  echo "    Aircraft report: ${SCRATCH_DIR}/aircraft-quarantine.json"
fi
if entity_selected submarine; then
  echo "    Submarine report: ${SCRATCH_DIR}/submarine-quarantine.json"
fi
if entity_selected facility; then
  echo "    Facility report: ${SCRATCH_DIR}/facility-quarantine.json"
fi
if entity_selected ground-unit; then
  echo "    Ground-unit report: ${SCRATCH_DIR}/ground-unit-quarantine.json"
fi