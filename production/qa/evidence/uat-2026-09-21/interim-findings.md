# Interim findings — Phase 0 + Phase 1 (2026-09-21)

Scope executed only. No product code changes. No commit. Final `production/qa/uat-2026-09-21.md` not written (Phase 6).

## Phase 0 — gates

| Check | Result |
|-------|--------|
| Probe `http://localhost:8080` (3s) | **Not Unity MCP.** GET `/` → HTTP **404** JSON error body. POST `/mcp` → HTTP **403** Access Denied. |
| Listener on :8080 | PID **27360** `java.exe` — `gateway-service-1.0.0-SNAPSHOT.jar` (Spring, unrelated). |
| Unity Editor running | **No** (`Get-Process Unity*` empty). |
| Session MCP catalog `ai-game-developer` | **Absent** — tools found: **none** (`ping`, `unity-tool-list`, `editor-application-set-state`, etc. not available). |
| Live `DelegationBridgeHost.scenarioPolicyId` | **Unread** (BLOCKED). Source default in `DelegationBridgeHost.cs` is `baltic-patrol` (not confirmed live). Prefer switch to `baltic-patrol-classify` via MCP component modify + scene-save once Editor is up. |
| `TYPESAFE_API_KEY` / `OPENROUTER_API_KEY` | **Both unset** — Jev key present: **no**. |

Per `.claude/skills/team-unity/SKILL.md`: stop Unity Editor work; do not invent tools; do not hand-edit `.unity` / `.prefab` / `.meta`.

## Phase 1 — C2 chrome rows

All six rows **BLOCKED** (Play Mode not entered).

| Row | Verdict | Evidence |
|-----|---------|----------|
| UAT-START | BLOCKED | `uat-start-blocked.md`, `phase0-mcp-probe.json` |
| UAT-SELECT | BLOCKED | `uat-select-blocked.md`, `phase0-mcp-probe.json` |
| UAT-COMMAND | BLOCKED | `uat-command-blocked.md`, `phase0-mcp-probe.json` |
| UAT-TYPE | BLOCKED | `uat-type-blocked.md`, `phase0-mcp-probe.json` |
| UAT-CLIP | BLOCKED | `uat-clip-blocked.md`, `phase0-mcp-probe.json` |
| UAT-MISSION | BLOCKED | `uat-mission-blocked.md`, `phase0-scenario-policy.json` |

Machine-readable table: `c2-acceptance-table.json`.

## Blockers for later phases

1. **Free or reclaim :8080** — stop/move the Java `gateway-service` occupying the port, or re-pin Unity MCP if the project allows a different Custom host (current pin script targets `:8080`).
2. **Open Unity Editor 6000.3.22f1** on `unity/ProjectAegis`, run `.\tools\pin-unity-mcp-8080.ps1`, confirm Window → AI Game Developer = Custom / `http://localhost:8080`.
3. **Refresh Cursor MCP** so `ai-game-developer` appears and tools (`ping`, `unity-tool-list`, Play Mode, Console, Game view screenshot) are callable.
4. **Re-run Phase 0** — live `scenarioPolicyId`; switch to `baltic-patrol-classify` via MCP if needed + `scene-save`.
5. **Re-run Phase 1** C2 rows with screenshots + Console slices under this evidence folder.
6. **Phase 2 Jev** remains key-gated until `TYPESAFE_API_KEY` or `OPENROUTER_API_KEY` is set (do not invent a key).

Phases 2–6 were **not** run (per scope).
