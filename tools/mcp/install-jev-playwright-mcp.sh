#!/usr/bin/env bash
# Install the Jev-augmented Playwright MCP proxy into tools/mcp/jev-playwright-mcp/
# (gitignored local checkout). Idempotent: re-run to update to the pinned ref.
#
#   bash tools/mcp/install-jev-playwright-mcp.sh
#   JEV_PLAYWRIGHT_MCP_REF=main bash tools/mcp/install-jev-playwright-mcp.sh
#   JEV_PLAYWRIGHT_MCP_SKIP_BROWSER=1 bash tools/mcp/install-jev-playwright-mcp.sh
#
# Registered as "jev-playwright" in .mcp.json and .cursor/mcp.json.
# Docs: docs/engineering/jev-playwright-mcp-setup.md
set -euo pipefail

REPO_URL="${JEV_PLAYWRIGHT_MCP_REPO:-https://github.com/drgaciw/jev-playwright-mcp.git}"
REF="${JEV_PLAYWRIGHT_MCP_REF:-db7762fd2ef66af7b38362c98982692bc81f57b7}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEST="${SCRIPT_DIR}/jev-playwright-mcp"

log() { printf '[install-jev-playwright-mcp] %s\n' "$*"; }
die() { printf '[install-jev-playwright-mcp] ERROR: %s\n' "$*" >&2; exit 1; }

command -v git >/dev/null || die "git not found"
command -v node >/dev/null || die "node not found (Node.js >= 20 required)"
command -v npm >/dev/null || die "npm not found"

NODE_MAJOR="$(node -p 'process.versions.node.split(".")[0]')"
[ "${NODE_MAJOR}" -ge 20 ] || die "Node.js >= 20 required (found $(node --version))"

if [ -d "${DEST}/.git" ]; then
  log "updating existing checkout at ${DEST}"
  git -C "${DEST}" fetch --quiet origin
else
  log "cloning ${REPO_URL} -> ${DEST}"
  git clone --quiet "${REPO_URL}" "${DEST}"
fi

if git -C "${DEST}" rev-parse --verify --quiet "origin/${REF}" >/dev/null; then
  git -C "${DEST}" checkout --quiet --detach "origin/${REF}"
else
  git -C "${DEST}" checkout --quiet --detach "${REF}"
fi
log "checked out $(git -C "${DEST}" log -1 --format='%h %cs %s')"

log "npm ci"
(cd "${DEST}" && npm ci --no-audit --no-fund --loglevel=error)

log "npm run build"
(cd "${DEST}" && npm run --silent build)

if [ "${JEV_PLAYWRIGHT_MCP_SKIP_BROWSER:-0}" != "1" ]; then
  log "installing Playwright chromium (set JEV_PLAYWRIGHT_MCP_SKIP_BROWSER=1 to skip)"
  (cd "${DEST}" && npx --no-install playwright install chromium)
fi

if [ ! -f "${DEST}/.env" ]; then
  cp "${DEST}/.env.example" "${DEST}/.env"
  log "created ${DEST}/.env (set TYPESAFE_API_KEY there to enable Jev; empty = passthrough)"
fi

log "done. Verify with: node tools/mcp/verify-jev-playwright-mcp.mjs"
