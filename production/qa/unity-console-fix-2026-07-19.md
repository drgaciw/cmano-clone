# Unity Console review (2026-07-19)

Desktop Commander + Editor.log review of live DelegationSmoke Editor.

## Project-owned fixes

| Console message | Fix |
|-----------------|-----|
| Font not found: Fonts/RobotoMono-Regular | Removed `-unity-font: resource(...)` from MessageLogPanel.uss |
| AudioListener component deleted (disabled built-in package) | Removed AudioListener from scene; builder strips on rebuild |

## Environmental (not product code)

| Message | Notes |
|---------|-------|
| McpManagerClientHub / hub/mcp-server | External `ai-game.dev` MCP 502 / reconnect exhausted — needs MCP server or disable cloud AI Game Developer connection |
| Account API not accessible in 30s | Unity Services network/focus — environmental |
| WebSocket closed without handshake | Same MCP hub |

## Verification

`dotnet test --filter UnityUiAssetIntegrity` → 2 passed.
