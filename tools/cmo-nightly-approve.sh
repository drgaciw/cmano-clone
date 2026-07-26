#!/usr/bin/env bash
# S29-03 / S30-04 / S30-11 / S31-09 — Off-CI nightly CMO corpus approve path after propose-only import (S28-02).
# Runs curator ApproveBatch + RecordRelease via catalog_write_approve. Not wired into dotnet test CI.
set -euo pipefail

export PATH="${HOME}/.dotnet:${PATH}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
cd "${REPO_ROOT}"

usage() {
  cat <<'EOF'
Usage: tools/cmo-nightly-approve.sh [options]

Off-CI nightly approve companion for tools/cmo-nightly-import.sh.
Reads *-propose.json artifacts, approves each staged batch through CatalogWriteGate,
and records snapshot hash + release metadata via catalog_write_approve.

Options:
  --entity <sensor|weapon|platform|aircraft|submarine|facility|ground-unit|all>  Entities to approve (default: all)
  --run-date <YYYYMMDD>                  Scratch dir date (default: today UTC)
  --scratch-dir <path>                   Override scratch directory
  --snapshot-id <id>                     Snapshot id for RecordRelease (optional)
  --release-version <ver>                Release version prefix (optional)
  --dry-run                              Print planned approvals only
  --enable-balance-drift                 Surface balance drift advisory in summary JSON (default: off)
  -h, --help                             Show this help

Environment overrides:
  ENTITY, RUN_DATE, SCRATCH_DIR, SNAPSHOT_ID, RELEASE_VERSION, ENABLE_BALANCE_DRIFT

Examples:
  ./tools/cmo-nightly-approve.sh --entity platform --run-date 20260618
  ./tools/cmo-nightly-approve.sh --entity aircraft --dry-run
  MAX_RECORDS=12 ./tools/cmo-nightly-import.sh --entity aircraft --propose-only
  ./tools/cmo-nightly-approve.sh --entity aircraft --dry-run
EOF
}

ENTITY="${ENTITY:-all}"
RUN_DATE="${RUN_DATE:-$(date -u +%Y%m%d)}"
SCRATCH_DIR="${SCRATCH_DIR:-${REPO_ROOT}/scratch/nightly-cmo-${RUN_DATE}}"
SNAPSHOT_ID="${SNAPSHOT_ID:-}"
RELEASE_VERSION="${RELEASE_VERSION:-}"
DRY_RUN=0
ENABLE_BALANCE_DRIFT="${ENABLE_BALANCE_DRIFT:-0}"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --entity)
      ENTITY="${2:?--entity requires sensor|weapon|platform|aircraft|submarine|facility|ground-unit|all}"
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
    --snapshot-id)
      SNAPSHOT_ID="${2:?--snapshot-id requires a value}"
      shift 2
      ;;
    --release-version)
      RELEASE_VERSION="${2:?--release-version requires a value}"
      shift 2
      ;;
    --dry-run)
      DRY_RUN=1
      shift
      ;;
    --enable-balance-drift)
      ENABLE_BALANCE_DRIFT=1
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

DB_PATH="${SCRATCH_DIR}/catalog-proposed.db"
CLI_PROJECT="src/ProjectAegis.MissionEditor.Cli/ProjectAegis.MissionEditor.Cli.csproj"
SUMMARY_OUT="${SCRATCH_DIR}/nightly-approve-summary.json"

entity_selected() {
  local name="$1"
  [[ "${ENTITY}" == "all" || "${ENTITY}" == "${name}" ]]
}

extract_batch_ids() {
  local propose_json="$1"
  local entity="$2"
  python3 - "${propose_json}" "${entity}" <<'PY'
import json
import sys

path, entity = sys.argv[1:3]
with open(path, encoding="utf-8") as handle:
    doc = json.load(handle)

if not doc.get("ok", False):
    raise SystemExit(f"propose artifact not ok: {path}")

batch_ids = []
seen = set()
for batch in doc.get("batches", []):
    batch_id = batch.get("batchId")
    if batch_id and batch_id not in seen:
        seen.add(batch_id)
        batch_ids.append(batch_id)

if entity in {"platform", "aircraft", "submarine", "facility", "ground-unit"}:
    prefix_order = (
        "batch-platform-",
        "batch-mount-",
        "batch-loadout-",
        "batch-magazine-",
    )

    def sort_key(batch_id: str):
        for idx, prefix in enumerate(prefix_order):
            if batch_id.startswith(prefix):
                return (idx, batch_id)
        return (99, batch_id)

    batch_ids.sort(key=sort_key)

for batch_id in batch_ids:
    print(batch_id)
PY
}

write_entity_summary() {
  local entity="$1"
  local propose_json="${SCRATCH_DIR}/${entity}-propose.json"
  local summary_json="${SCRATCH_DIR}/${entity}-approve-summary.json"
  python3 - "${entity}" "${propose_json}" "${SCRATCH_DIR}" "${summary_json}" "${ENABLE_BALANCE_DRIFT}" <<'PY'
import glob
import json
import os
import sys

entity, propose_path, scratch_dir, summary_path, enable_balance_drift = sys.argv[1:6]
enable_balance_drift = enable_balance_drift == "1"
with open(propose_path, encoding="utf-8") as handle:
    propose = json.load(handle)

pattern = os.path.join(scratch_dir, f"{entity}-approve-*.json")
approvals = []
for path in sorted(glob.glob(pattern)):
    if path.endswith("-summary.json"):
        continue
    with open(path, encoding="utf-8") as handle:
        approvals.append(json.load(handle))

def merge_balance_drift_advisories(items):
    enabled = False
    findings = []
    notes = []
    state_hash = ""
    for item in items:
        advisory = item.get("balanceDriftAdvisory")
        if not advisory:
            continue
        enabled = enabled or advisory.get("driftDetectionEnabled", False)
        findings.extend(advisory.get("findings", []))
        notes.extend(advisory.get("advisoryNotes", []))
        if advisory.get("stateHash"):
            state_hash = advisory["stateHash"]
    if not enabled:
        return None
    deduped_findings = []
    seen = set()
    for finding in sorted(findings, key=lambda f: (f.get("entityId", ""), f.get("code", ""))):
        key = (finding.get("entityId", ""), finding.get("code", ""))
        if key in seen:
            continue
        seen.add(key)
        deduped_findings.append(finding)
    return {
        "driftDetectionEnabled": True,
        "findingCount": len(deduped_findings),
        "findings": deduped_findings,
        "stateHash": state_hash,
        "advisoryNotes": sorted(set(notes)),
    }

summary = {
    "ok": all(item.get("ok") for item in approvals),
    "entity": propose.get("entity", entity),
    "parsedCount": propose.get("parsedCount"),
    "batchCount": len(approvals),
    "batches": [
        {
            "batchId": item.get("batchId"),
            "snapshotId": item.get("snapshotId"),
            "releaseVersion": item.get("releaseVersion"),
            "contentHashSha256": item.get("contentHashSha256"),
            "sensorRowCount": item.get("sensorRowCount"),
            **(
                {"balanceDriftAdvisory": item["balanceDriftAdvisory"]}
                if enable_balance_drift and item.get("balanceDriftAdvisory") is not None
                else {}
            ),
        }
        for item in approvals
    ],
}
if enable_balance_drift:
    summary["enableBalanceDrift"] = True
    merged = merge_balance_drift_advisories(approvals)
    if merged is not None:
        summary["balanceDriftAdvisory"] = merged
with open(summary_path, "w", encoding="utf-8") as handle:
    json.dump(summary, handle, indent=2)
    handle.write("\n")
PY
}

approve_entity() {
  local entity="$1"
  local propose_json="${SCRATCH_DIR}/${entity}-propose.json"

  if [[ ! -f "${propose_json}" ]]; then
    echo "Missing propose artifact: ${propose_json}" >&2
    echo "Run tools/cmo-nightly-import.sh first (propose-only)." >&2
    return 1
  fi

  echo "==> Approve ${entity}"
  echo "    Propose: ${propose_json}"

  mapfile -t batch_ids < <(extract_batch_ids "${propose_json}" "${entity}")
  if [[ "${#batch_ids[@]}" -eq 0 ]]; then
    echo "No batch IDs found in ${propose_json}" >&2
    return 1
  fi

  if [[ "${DRY_RUN}" -eq 1 ]]; then
    for batch_id in "${batch_ids[@]}"; do
      local dry_run_cmd="catalog_write_approve --db ${DB_PATH} --batch ${batch_id}"
      if [[ "${ENABLE_BALANCE_DRIFT}" -eq 1 ]]; then
        dry_run_cmd+=" --enable-balance-drift"
      fi
      echo "    [dry-run] ${dry_run_cmd}"
    done
    return 0
  fi

  local approve_args=()
  if [[ -n "${SNAPSHOT_ID}" ]]; then
    approve_args+=(--snapshot-id "${SNAPSHOT_ID}")
  fi
  if [[ -n "${RELEASE_VERSION}" ]]; then
    approve_args+=(--release-version "${RELEASE_VERSION}")
  fi
  if [[ "${ENABLE_BALANCE_DRIFT}" -eq 1 ]]; then
    approve_args+=(--enable-balance-drift)
  fi

  local failures=0
  for batch_id in "${batch_ids[@]}"; do
    echo "    Approving ${batch_id}"
    local batch_out="${SCRATCH_DIR}/${entity}-approve-${batch_id}.json"
    if ! dotnet run --project "${CLI_PROJECT}" --no-build -- \
      catalog_write_approve \
      --db "${DB_PATH}" \
      --batch "${batch_id}" \
      "${approve_args[@]}" \
      > "${batch_out}"; then
      echo "Approve failed for ${batch_id}; see ${batch_out}" >&2
      failures=$((failures + 1))
    fi
  done

  if [[ "${failures}" -gt 0 ]]; then
    echo "==> ${entity}: ${failures} batch approve failure(s)" >&2
    return 1
  fi

  write_entity_summary "${entity}"
  echo "    Summary: ${SCRATCH_DIR}/${entity}-approve-summary.json"
}

write_final_summary() {
  local entities=("$@")
  python3 - "${SUMMARY_OUT}" "${RUN_DATE}" "${SCRATCH_DIR}" "${DB_PATH}" "${ENABLE_BALANCE_DRIFT}" "${entities[@]}" <<'PY'
import json
import sys

out_path, run_date, scratch_dir, db_path, enable_balance_drift, *entities = sys.argv[1:]
enable_balance_drift = enable_balance_drift == "1"
entity_summaries = []
for entity in entities:
    summary_path = f"{scratch_dir}/{entity}-approve-summary.json"
    with open(summary_path, encoding="utf-8") as handle:
        entity_summaries.append(json.load(handle))

def merge_entity_advisories(items):
    enabled = False
    findings = []
    notes = []
    state_hash = ""
    for item in items:
        advisory = item.get("balanceDriftAdvisory")
        if not advisory:
            continue
        enabled = enabled or advisory.get("driftDetectionEnabled", False)
        findings.extend(advisory.get("findings", []))
        notes.extend(advisory.get("advisoryNotes", []))
        if advisory.get("stateHash"):
            state_hash = advisory["stateHash"]
    if not enabled:
        return None
    deduped_findings = []
    seen = set()
    for finding in sorted(findings, key=lambda f: (f.get("entityId", ""), f.get("code", ""))):
        key = (finding.get("entityId", ""), finding.get("code", ""))
        if key in seen:
            continue
        seen.add(key)
        deduped_findings.append(finding)
    return {
        "driftDetectionEnabled": True,
        "findingCount": len(deduped_findings),
        "findings": deduped_findings,
        "stateHash": state_hash,
        "advisoryNotes": sorted(set(notes)),
    }

summary = {
    "ok": all(item.get("ok", False) for item in entity_summaries),
    "runDate": run_date,
    "scratchDir": scratch_dir,
    "dbPath": db_path,
    "entities": entity_summaries,
}
if enable_balance_drift:
    summary["enableBalanceDrift"] = True
    merged = merge_entity_advisories(entity_summaries)
    if merged is not None:
        summary["balanceDriftAdvisory"] = merged
with open(out_path, "w", encoding="utf-8") as handle:
    json.dump(summary, handle, indent=2)
    handle.write("\n")
print(json.dumps(summary, indent=2))
PY
}

if [[ ! -d "${SCRATCH_DIR}" ]]; then
  echo "Missing scratch directory: ${SCRATCH_DIR}" >&2
  echo "Run tools/cmo-nightly-import.sh first." >&2
  exit 1
fi

if [[ "${DRY_RUN}" -eq 0 && ! -f "${DB_PATH}" ]]; then
  echo "Missing scratch DB: ${DB_PATH}" >&2
  echo "Run tools/cmo-nightly-import.sh first." >&2
  exit 1
fi

echo "==> Nightly CMO approve @ ${RUN_DATE}"
echo "    DB: ${DB_PATH}"
echo "    Entity: ${ENTITY}"
if [[ -n "${SNAPSHOT_ID}" ]]; then
  echo "    Snapshot id: ${SNAPSHOT_ID}"
fi
if [[ -n "${RELEASE_VERSION}" ]]; then
  echo "    Release version: ${RELEASE_VERSION}"
fi
if [[ "${DRY_RUN}" -eq 1 ]]; then
  echo "    Mode: dry-run (no dotnet invocations)"
fi
if [[ "${ENABLE_BALANCE_DRIFT}" -eq 1 ]]; then
  echo "    Balance drift advisory: enabled"
fi

if [[ "${DRY_RUN}" -eq 0 ]]; then
  dotnet build "${CLI_PROJECT}" -v minimal -nologo
fi

declare -a approved_entities=()
failures=0

if entity_selected sensor; then
  if approve_entity sensor; then
    approved_entities+=("sensor")
  else
    failures=$((failures + 1))
  fi
fi

if entity_selected weapon; then
  if approve_entity weapon; then
    approved_entities+=("weapon")
  else
    failures=$((failures + 1))
  fi
fi

if entity_selected platform; then
  if approve_entity platform; then
    approved_entities+=("platform")
  else
    failures=$((failures + 1))
  fi
fi

if entity_selected aircraft; then
  if approve_entity aircraft; then
    approved_entities+=("aircraft")
  else
    failures=$((failures + 1))
  fi
fi

if entity_selected submarine; then
  if approve_entity submarine; then
    approved_entities+=("submarine")
  else
    failures=$((failures + 1))
  fi
fi

if entity_selected facility; then
  if approve_entity facility; then
    approved_entities+=("facility")
  else
    failures=$((failures + 1))
  fi
fi

if entity_selected ground-unit; then
  if approve_entity ground-unit; then
    approved_entities+=("ground-unit")
  else
    failures=$((failures + 1))
  fi
fi

if [[ "${DRY_RUN}" -eq 1 ]]; then
  echo "==> Dry-run complete. Planned approvals validated."
  exit 0
fi

if [[ "${#approved_entities[@]}" -gt 0 ]]; then
  write_final_summary "${approved_entities[@]}"
fi

if [[ "${failures}" -gt 0 ]]; then
  echo "==> Approve completed with ${failures} entity failure(s)." >&2
  exit 1
fi

echo "==> Done. Snapshot hashes pinned in ${SUMMARY_OUT}"