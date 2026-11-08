# ADR-027: Unity Editor MCP Stack — Ivan Murzak Unity-MCP (not CoplayDev MCP for Unity)

## Status

**Accepted** (2026-09-27)

## Date

2026-09-27

## Context

Agents drive the Unity Editor through an MCP bridge for scene, prefab, UI Toolkit, Console, and screenshot work. Headless `dotnet test` remains the verification authority; the Editor bridge is dev-only tooling.

The repo has run Ivan Murzak's Unity-MCP (`com.ivanmurzak.unity.mcp`, OpenUPM) since the asmdef/bridge integration commit, but no ADR recorded the choice. A review on 2026-09-27 compared it against [CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp) ("MCP for Unity": Unity package `com.coplaydev.unity-mcp` + Python server `mcpforunityserver`).

Current integration surface on the Ivan Murzak stack:

- `unity/ProjectAegis/Packages/manifest.json` pins `com.ivanmurzak.unity.mcp` `0.90.0` (mirrored in `manifest.template.json`).
- 77 generated Editor skills under `unity/ProjectAegis/.claude/skills/` call `unity-mcp-cli run-tool …`.
- `Assets/Editor/McpLocalHostPin.cs` and `tools/pin-unity-mcp-8080.{sh,ps1}` write the package's `UserSettings/AI-Game-Developer-Config.json` to pin Custom mode on `http://localhost:8080`.
- Client configs (`.mcp.json`, `.cursor/mcp.json`, `.grok/config.toml`) register `ai-game-developer` at `http://localhost:8080`.
- `AGENTS.md` session-start protocol probes `:8080` and routes to headless when it is down.

Observed differences in the Coplay stack (server `mcpforunityserver` 10.2.0, checked 2026-09-27):

- Three layers: MCP client → Python server (FastMCP + WebSocket hub) → Unity C# plugin. Requires `uv`/`uvx` and Python ≥3.10 on each dev machine.
- HTTP clients connect to `http://localhost:8080/mcp`; the repo's configs use `http://localhost:8080` with no path.
- The server's default HTTP bind is port 8080 (confirmed by running it with default arguments). In stdio mode it opened no listening port.

## Decision

1. **Keep** Ivan Murzak Unity-MCP (`com.ivanmurzak.unity.mcp`) as the single Unity Editor MCP stack for Project Aegis.
2. **Do not** add `com.coplaydev.unity-mcp` alongside it. The two are separate packages and can technically coexist on different ports, but two overlapping Editor tool surfaces would give agents ambiguous routing, and running both in HTTP mode with defaults collides on `:8080`.
3. **Keep** `manifest.template.json` in lockstep with `manifest.json`; `UnityPackageManifestMirrorTests` fails on drift.
4. **Pin** the CLI to the package version in docs (`npx unity-mcp-cli@<package version>`).

## Consequences

### Positive

- No migration of the 77 skills, the pin script and Editor menu, client configs, or the `AGENTS.md` protocol.
- No Python/`uv` prerequisite on dev machines for Editor MCP.
- One unambiguous tool surface on one port.

### Negative

- Coplay-only features (multi-instance routing, per-session tool groups, Roslyn validation guides) are not available.
- Dependence on a single maintainer's OpenUPM package; version currency must be checked in the in-editor Package Manager.

### Revisit when

- The Ivan Murzak package stops tracking the pinned Unity 6.3 LTS editor, or
- A concrete Editor workflow needs a Coplay-only capability. A switch is a full migration (package, client configs with `/mcp` path, skills, pin tooling, `AGENTS.md`), not an addition.

## Not verified

- `tools/pin-unity-mcp-8080.sh` behaviour on package 0.90.0 (the pin was authored against 0.86). Needs a local Editor: run the pin, then `curl -sS -o /dev/null -w "%{http_code}\n" --max-time 3 http://localhost:8080`.
- Coplay and Ivan Murzak packages loaded in the same Editor at once.
