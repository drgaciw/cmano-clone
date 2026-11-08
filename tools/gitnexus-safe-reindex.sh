#!/usr/bin/env bash
# DRG-323 / DRG-404 guard — safe GitNexus reindex.
# Stops gitnexus MCP servers (which can hold .gitnexus/lbug open), runs a full
# `analyze --force --embeddings --skip-agents-md --skip-skills` (never incremental,
# never --drop-embeddings), reports unexpected file changes, and post-checks the
# index including a non-zero embedding count. Exits non-zero on a bad index.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
cd "${REPO_ROOT}"

usage() {
  cat <<'USAGE'
Usage: tools/gitnexus-safe-reindex.sh [options]

Options:
  --allow-dirty        Run in a checkout with local changes (they are never reverted)
  --allow-branch       Run on a branch other than main
  --name <alias>       Registry alias passed to analyze --name (default: none)
  --skip-analyze       Only run the post-check against the existing index
  --no-kill-mcp        Do not stop gitnexus mcp processes (refused unless --skip-analyze)
  -h, --help           Show this help

Exit codes: 0 good index, 1 refused/usage, 2 analyze failed, 3 bad index.
USAGE
}

ALLOW_DIRTY=0; ALLOW_BRANCH=0; NAME=""; SKIP_ANALYZE=0; KILL_MCP=1
while [[ $# -gt 0 ]]; do
  case "$1" in
    --allow-dirty) ALLOW_DIRTY=1 ;;
    --allow-branch) ALLOW_BRANCH=1 ;;
    --name) NAME="${2:?--name needs a value}"; shift ;;
    --skip-analyze) SKIP_ANALYZE=1 ;;
    --no-kill-mcp) KILL_MCP=0 ;;
    -h|--help) usage; exit 0 ;;
    *) echo "unknown option: $1" >&2; usage >&2; exit 1 ;;
  esac
  shift
done

die() { echo "gitnexus-safe-reindex: $1" >&2; exit "${2:-1}"; }
# A registry alias alone does not isolate sibling clones on newer GitNexus releases.
# This documented opt-out is also harmless on legacy versions without shared stores.
export GITNEXUS_SHARED_STORE=off
gn() {
  if [[ -f "${REPO_ROOT}/.gitnexus/run.cjs" ]] && command -v node >/dev/null 2>&1; then
    node "${REPO_ROOT}/.gitnexus/run.cjs" "$@"
  elif command -v gitnexus >/dev/null 2>&1; then
    gitnexus "$@"
  elif command -v pnpm >/dev/null 2>&1; then
    pnpm --allow-build=@ladybugdb/core --allow-build=gitnexus --allow-build=tree-sitter dlx gitnexus@latest "$@"
  else
    npx --yes gitnexus "$@"
  fi
}

# --- refusal gates ---------------------------------------------------------
BRANCH="$(git rev-parse --abbrev-ref HEAD)"
if [[ "${BRANCH}" != "main" && ${ALLOW_BRANCH} -eq 0 ]]; then
  die "on branch '${BRANCH}', not main (pass --allow-branch to override)"
fi
PRE_STATUS="$(git status --porcelain)"
if [[ -n "${PRE_STATUS}" && ${ALLOW_DIRTY} -eq 0 ]]; then
  echo "${PRE_STATUS}" >&2
  die "working tree is not clean (pass --allow-dirty to override)"
fi
if [[ ${SKIP_ANALYZE} -eq 0 && ${KILL_MCP} -eq 0 ]]; then
  die "--no-kill-mcp requires --skip-analyze (an MCP server holding the DB corrupts analyze)"
fi
if [[ -e "${REPO_ROOT}/.gitnexus/store.json" ]]; then
  die "shared-store checkout detected; use a standalone indexing clone (store left untouched)"
fi

# --- stop MCP, analyze -----------------------------------------------------
ANALYZE_FAILED=0
if [[ ${SKIP_ANALYZE} -eq 0 ]]; then
  if pgrep -f 'gitnexus.*mcp' >/dev/null 2>&1; then
    echo "stopping gitnexus mcp processes"
    pkill -f 'gitnexus.*mcp' || true
    sleep 2
  fi
  if pgrep -f 'gitnexus.*mcp' >/dev/null 2>&1; then
    die "gitnexus mcp still running after kill" 2
  fi
  # Full rebuild only. Incremental analyze corrupted Method/File/Property IDs (DRG-323)
  # even with no MCP running. --force does not generate embeddings on a cold index
  # (count 0); --embeddings does. Never --watch and never --drop-embeddings.
  ARGS=(analyze --force --embeddings --skip-agents-md --skip-skills)
  [[ -n "${NAME}" ]] && ARGS+=(--name "${NAME}" --allow-duplicate-name)
  gn "${ARGS[@]}" || ANALYZE_FAILED=1
  if pgrep -f 'gitnexus.*mcp' >/dev/null 2>&1; then
    echo "WARNING: a gitnexus mcp process respawned during analyze; re-run if the check fails" >&2
  fi
fi

# --- report changes on success and failure; never discard work -------------
if [[ ${SKIP_ANALYZE} -eq 0 ]]; then
  POST_STATUS="$(git status --porcelain)"
  if [[ -n "${POST_STATUS}" ]]; then
    echo "gitnexus-safe-reindex: working tree changes retained; review manually:" >&2
    echo "${POST_STATUS}" >&2
    # A clean start does not establish ownership of edits saved during analyze.
    if [[ -z "${PRE_STATUS}" && ${ANALYZE_FAILED} -eq 0 ]]; then
      die "analyze left working tree changes" 3
    fi
  fi
  [[ ${ANALYZE_FAILED} -eq 0 ]] || die "analyze failed" 2
fi

# --- post-check ------------------------------------------------------------
FAIL=0
cy() { gn cypher "$1" --repo "${REPO_ROOT}" | python3 -c 'import sys,json
t=sys.stdin.read();d=json.loads(t[t.find("{"):]);m=d["markdown"].split("\n")
print(int(m[-1].strip("| ").split(" | ")[0]))' || die "index query failed" 3; }

META_COMMIT="$(python3 -c 'import json;print(json.load(open(".gitnexus/meta.json"))["lastCommit"])')" || die "invalid index metadata" 3
META_NODES="$(python3 -c 'import json;print(json.load(open(".gitnexus/meta.json"))["stats"]["nodes"])')" || die "invalid index metadata" 3
META_EDGES="$(python3 -c 'import json;print(json.load(open(".gitnexus/meta.json"))["stats"]["edges"])')" || die "invalid index metadata" 3
META_EMB="$(python3 -c 'import json
d=json.load(open(".gitnexus/meta.json"))
value=(d.get("stats") or {}).get("embeddings", 0)
print(int(0 if value is None else value))')" || die "invalid index metadata" 3
VECTOR_STATUS="$(python3 -c 'import json
d=json.load(open(".gitnexus/meta.json"))
vs=(d.get("capabilities") or {}).get("vectorSearch")
if isinstance(vs, dict):
    print(vs.get("status") or "unavailable")
elif isinstance(vs, str) and vs:
    print(vs)
else:
    print("unavailable")')" || die "invalid index metadata" 3
HEAD_SHA="$(git rev-parse HEAD)"
G_NODES="$(cy 'MATCH (n) RETURN count(n) AS c')"
G_EDGES="$(cy 'MATCH ()-[r]->() RETURN count(r) AS c')"
G_EMB="$(cy 'MATCH (e:CodeEmbedding) RETURN count(e) AS cnt')"
echo "indexed=${META_COMMIT:0:8} head=${HEAD_SHA:0:8} nodes meta/graph=${META_NODES}/${G_NODES} edges meta/graph=${META_EDGES}/${G_EDGES} embeddings meta/graph=${META_EMB}/${G_EMB} vectorSearch=${VECTOR_STATUS}"

[[ "${META_COMMIT}" == "${HEAD_SHA}" ]] || { echo "FAIL: indexed commit != HEAD" >&2; FAIL=1; }
[[ "${META_NODES}" == "${G_NODES}" ]] || { echo "FAIL: node count metadata != graph" >&2; FAIL=1; }
[[ "${META_EDGES}" == "${G_EDGES}" ]] || { echo "FAIL: edge count metadata != graph" >&2; FAIL=1; }
[[ "${META_EMB}" == "${G_EMB}" ]] || { echo "FAIL: embedding count metadata != graph" >&2; FAIL=1; }
[[ "${META_EMB}" -gt 0 ]] || { echo "FAIL: embeddings count is 0 (hybrid vector leg inactive)" >&2; FAIL=1; }
case "${VECTOR_STATUS}" in
  vector-index|exact-scan) ;;
  *) echo "FAIL: vector search unavailable (${VECTOR_STATUS})" >&2; FAIL=1 ;;
esac
if [[ -f .gitnexus/gitnexus.json ]]; then
  GJ_EMB="$(python3 -c 'import json
d=json.load(open(".gitnexus/gitnexus.json"))
value=(d.get("stats") or {}).get("embeddings", 0)
print(int(0 if value is None else value))')" || die "invalid index metadata" 3
  GJ_STATUS="$(python3 -c 'import json
d=json.load(open(".gitnexus/gitnexus.json"))
vs=(d.get("capabilities") or {}).get("vectorSearch")
if isinstance(vs, dict):
    print(vs.get("status") or "unavailable")
elif isinstance(vs, str) and vs:
    print(vs)
else:
    print("unavailable")')" || die "invalid index metadata" 3
  [[ "${GJ_EMB}" == "${META_EMB}" && "${GJ_STATUS}" == "${VECTOR_STATUS}" ]] || { echo "FAIL: gitnexus.json embeddings disagree with meta.json" >&2; FAIL=1; }
fi

for L in Class Method File Property; do
  BAD="$(cy "MATCH (n:${L}) WHERE NOT n.id STARTS WITH '${L}:' RETURN count(n) AS c")"
  EMPTY="$(cy "MATCH (n:${L}) WHERE n.name IS NULL OR n.name = '' RETURN count(n) AS c")"
  echo "${L}: garbage ids=${BAD} empty names=${EMPTY}"
  [[ "${BAD}" == "0" && "${EMPTY}" == "0" ]] || { echo "FAIL: corrupted ${L} nodes" >&2; FAIL=1; }
done

if [[ ${FAIL} -ne 0 ]]; then
  echo "gitnexus-safe-reindex: BAD INDEX (see docs/engineering/gitnexus-index-health.md; DRG-323)" >&2
  exit 3
fi
echo "gitnexus-safe-reindex: OK"
