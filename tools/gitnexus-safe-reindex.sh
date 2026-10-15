#!/usr/bin/env bash
# DRG-323 guard — safe GitNexus reindex.
# Stops gitnexus MCP servers (which can hold .gitnexus/lbug open), runs
# `analyze --skip-agents-md --skip-skills`, reverts generated tracked changes,
# and post-checks the index. Exits non-zero on a bad index.
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

die() { echo "gitnexus-safe-reindex: $*" >&2; exit "${2:-1}"; }
gn() { npx --yes gitnexus "$@"; }

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

# --- stop MCP, analyze -----------------------------------------------------
if [[ ${SKIP_ANALYZE} -eq 0 ]]; then
  if pgrep -f 'gitnexus.*mcp' >/dev/null 2>&1; then
    echo "stopping gitnexus mcp processes"
    pkill -f 'gitnexus.*mcp' || true
    sleep 2
  fi
  if pgrep -f 'gitnexus.*mcp' >/dev/null 2>&1; then
    die "gitnexus mcp still running after kill" 2
  fi
  ARGS=(analyze --skip-agents-md --skip-skills)
  [[ -n "${NAME}" ]] && ARGS+=(--name "${NAME}" --allow-duplicate-name)
  gn "${ARGS[@]}" || die "analyze failed" 2
  if pgrep -f 'gitnexus.*mcp' >/dev/null 2>&1; then
    echo "WARNING: a gitnexus mcp process respawned during analyze; re-run if the check fails" >&2
  fi
fi

# --- generated tracked changes: revert (only when we started clean) --------
if [[ -z "${PRE_STATUS}" ]]; then
  CHANGED="$(git diff --name-only)"
  if [[ -n "${CHANGED}" ]]; then
    echo "reverting generated tracked changes:"; echo "${CHANGED}"
    git checkout -- ${CHANGED}
  fi
  UNTRACKED="$(git ls-files --others --exclude-standard)"
  if [[ -n "${UNTRACKED}" ]]; then
    die "analyze left untracked files (remove manually; not deleting automatically):
${UNTRACKED}" 3
  fi
fi

# --- post-check ------------------------------------------------------------
FAIL=0
cy() { gn cypher "$1" 2>&1 | python3 -c 'import sys,json,re
t=sys.stdin.read();d=json.loads(t[t.find("{"):]);m=d["markdown"].split("\n")
print(m[-1].strip("| ").split(" | ")[0] if len(m)>2 else "")'; }

META_COMMIT="$(python3 -c 'import json;print(json.load(open(".gitnexus/meta.json"))["lastCommit"])')"
META_NODES="$(python3 -c 'import json;print(json.load(open(".gitnexus/meta.json"))["stats"]["nodes"])')"
META_EDGES="$(python3 -c 'import json;print(json.load(open(".gitnexus/meta.json"))["stats"]["edges"])')"
HEAD_SHA="$(git rev-parse HEAD)"
G_NODES="$(cy 'MATCH (n) RETURN count(n) AS c')"
G_EDGES="$(cy 'MATCH ()-[r]->() RETURN count(r) AS c')"
echo "indexed=${META_COMMIT:0:8} head=${HEAD_SHA:0:8} nodes meta/graph=${META_NODES}/${G_NODES} edges meta/graph=${META_EDGES}/${G_EDGES}"

[[ "${META_COMMIT}" == "${HEAD_SHA}" ]] || { echo "FAIL: indexed commit != HEAD" >&2; FAIL=1; }
[[ "${META_NODES}" == "${G_NODES}" ]] || { echo "FAIL: node count metadata != graph" >&2; FAIL=1; }
[[ "${META_EDGES}" == "${G_EDGES}" ]] || { echo "FAIL: edge count metadata != graph" >&2; FAIL=1; }

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
