# jev-playwright-mcp (local browser MCP)

[drgaciw/jev-playwright-mcp](https://github.com/drgaciw/jev-playwright-mcp) is a
stdio proxy around the official `@playwright/mcp` (0.0.81). It exposes the same
Playwright tools plus `browser_set_goal` / `browser_jev_status`, and adds Jev
page-state triage, prompt-injection masking, snapshot pruning, and risky-action
gating. `browser_run_code_unsafe` is always blocked.

It is agent tooling only — not a CI gate and not part of the .NET solution.

## Install

Requires Node.js >= 20 and git. From the repo root:

```bash
bash tools/mcp/install-jev-playwright-mcp.sh      # Linux / macOS / Cloud VM
.\tools\mcp\install-jev-playwright-mcp.ps1        # Windows
```

The script clones the proxy into `tools/mcp/jev-playwright-mcp/` (gitignored),
checks out the pinned commit, runs `npm ci` + `npm run build`, installs
Playwright chromium into the ms-playwright cache, and creates the proxy's
`.env` from its template. Re-run to update. Overrides:
`JEV_PLAYWRIGHT_MCP_REF=<sha|branch>` (`-Ref`), `JEV_PLAYWRIGHT_MCP_SKIP_BROWSER=1`
(`-SkipBrowser`).

## Verify

```bash
node tools/mcp/verify-jev-playwright-mcp.mjs
```

Launches the proxy over stdio, drives headless chromium against a local page,
and checks `tools/list`, `browser_navigate`, `browser_snapshot`, and
`browser_jev_status`. Prints `RESULT: PASS` and exits 0 when healthy.

## Agent registration

Registered as `jev-playwright` (headless, `--jev-mode=all`):

| Client | File | Path form |
|--------|------|-----------|
| Claude Code | `.mcp.json` | `tools/mcp/jev-playwright-mcp/cli.js` (relative to project root) |
| Cursor | `.cursor/mcp.json` | `${workspaceFolder}/tools/mcp/jev-playwright-mcp/cli.js` |

Reload MCP servers in the client after installing. Remove `--headless` from the
args to watch the browser. The existing `playwright-local` HTTP entry
(`:8931`) is a separate server and is unchanged.

## Jev API key (optional)

Without a key the proxy is pure passthrough (stock Playwright MCP behavior).
To enable Jev, set `TYPESAFE_API_KEY` in either:

- `tools/mcp/jev-playwright-mcp/.env` (gitignored, auto-loaded by the proxy), or
- the environment the agent is launched from (Cloud Agents: Cursor Dashboard →
  Cloud Agents → Secrets).

Never put the key in `.mcp.json` / `.cursor/mcp.json` — both are committed.
Check status with the `browser_jev_status` tool (`"enabled": true`).
