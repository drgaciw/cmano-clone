#!/usr/bin/env bash
# S32-03 — Off-CI curator triage for mount/loadout quarantine child rows (ship/facility/submarine).
# Audits pending staging batches, applies bounded FK repair via CatalogWriteGate, emits before/after counts.
set -euo pipefail

export PATH="${HOME}/.dotnet:${PATH}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
cd "${REPO_ROOT}"

usage() {
  cat <<'EOF'
Usage: tools/cmo-mount-loadout-quarantine-triage.sh [options]

Off-CI mount/loadout quarantine triage companion for nightly propose→approve artifacts.
Not wired into dotnet test CI — uses scratch DB + curated slices only in tests.

Options:
  --entity <platform|submarine|facility>   Domain filter (default: all child-row domains)
  --run-date <YYYYMMDD>                    Scratch dir date (default: today UTC)
  --scratch-dir <path>                     Override scratch directory
  --propose-json <path>                    Optional *-propose.json for fitting quarantine counts
  --apply                                  Commit bounded repairs via CatalogWriteGate (default: dry-run)
  --dry-run                                Audit only (default)
  -h, --help                               Show this help

Environment overrides:
  ENTITY, RUN_DATE, SCRATCH_DIR, APPLY

Examples:
  ./tools/cmo-mount-loadout-quarantine-triage.sh --entity submarine --run-date 20260618
  ./tools/cmo-mount-loadout-quarantine-triage.sh --entity facility --apply
EOF
}

ENTITY="${ENTITY:-}"
RUN_DATE="${RUN_DATE:-$(date -u +%Y%m%d)}"
SCRATCH_DIR="${SCRATCH_DIR:-${REPO_ROOT}/scratch/nightly-cmo-${RUN_DATE}}"
APPLY="${APPLY:-0}"
PROPOSE_JSON="${PROPOSE_JSON:-}"
DRY_RUN=1
CLI_PROJECT="src/ProjectAegis.MissionEditor.Cli/ProjectAegis.MissionEditor.Cli.csproj"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --entity)
      ENTITY="${2:?--entity requires platform|submarine|facility}"
      shift 2
      ;;
    --run-date)
      RUN_DATE="${2:?--run-date requires YYYYMMDD}"
      SCRATCH_DIR="${REPO_ROOT}/scratch/nightly-cmo-${RUN_DATE}"
      shift 2
      ;;
    --scratch-dir)
      SCRATCH_DIR="${2:?--scratch-dir requires a path}"
      shift 2
      ;;
    --propose-json)
      PROPOSE_JSON="${2:?--propose-json requires a path}"
      shift 2
      ;;
    --apply)
      APPLY=1
      DRY_RUN=0
      shift
      ;;
    --dry-run)
      DRY_RUN=1
      APPLY=0
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

DB_PATH="${SCRATCH_DIR}/catalog-proposed.db"
if [[ -z "${PROPOSE_JSON}" && -n "${ENTITY}" && -f "${SCRATCH_DIR}/${ENTITY}-propose.json" ]]; then
  PROPOSE_JSON="${SCRATCH_DIR}/${ENTITY}-propose.json"
fi

if [[ "${DRY_RUN}" -eq 0 && ! -f "${DB_PATH}" ]]; then
  echo "Missing scratch DB: ${DB_PATH}" >&2
  echo "Run tools/cmo-nightly-import.sh first." >&2
  exit 1
fi

REPORT_OUT="${SCRATCH_DIR}/mount-loadout-quarantine-triage"
if [[ -n "${ENTITY}" ]]; then
  REPORT_OUT="${REPORT_OUT}-${ENTITY}"
fi
REPORT_OUT="${REPORT_OUT}.json"

echo "==> Mount/loadout quarantine triage @ ${RUN_DATE}"
echo "    DB: ${DB_PATH}"
if [[ -n "${ENTITY}" ]]; then
  echo "    Entity: ${ENTITY}"
fi
if [[ -n "${PROPOSE_JSON}" ]]; then
  echo "    Propose: ${PROPOSE_JSON}"
fi
if [[ "${DRY_RUN}" -eq 1 ]]; then
  echo "    Mode: dry-run"
else
  echo "    Mode: apply (WriteGate repair)"
fi

cmd=(dotnet run --project "${CLI_PROJECT}" --no-build -- catalog_mount_loadout_quarantine_triage --db "${DB_PATH}")
if [[ -n "${ENTITY}" ]]; then
  cmd+=(--entity "${ENTITY}")
fi
if [[ -n "${PROPOSE_JSON}" ]]; then
  cmd+=(--propose-json "${PROPOSE_JSON}")
fi
if [[ "${DRY_RUN}" -eq 0 ]]; then
  cmd+=(--apply)
fi

if [[ "${DRY_RUN}" -eq 0 ]]; then
  dotnet build "${CLI_PROJECT}" -v minimal -nologo
fi

"${cmd[@]}" | tee "${REPORT_OUT}"
echo "==> Report: ${REPORT_OUT}"