# Claude Agent Setup — Project Aegis

This guide covers the Claude-specific integrations configured for Project Aegis: **Unity-MCP** (real-time Unity Editor bridge) and **Claude-Code-Game-Studios** (studio agent hierarchy and workflow skills).

## Status Summary

| Integration | Repo config | Runtime ready |
|-------------|-------------|---------------|
| Unity project | `unity/ProjectAegis/` (6000.3.14f1) | Yes |
| Unity-MCP CLI | `npx unity-mcp-cli` (pin **0.86.x** to match package; optional global) | Partial — not on PATH unless installed |
| MCP server config | `.cursor/mcp.json`, `.mcp.json` → `type: http` + `http://localhost:8080` | Yes (needs Editor + **local pin** + plugin active) |
| Game-Studios agents/skills | `.claude/` vendored | Yes (Claude Code) |
| Unity Editor plugin | `com.ivanmurzak.unity.mcp` **0.86.0** direct dependency in `Packages/manifest.json` | Package on disk; Editor session still local-only |
| MCP HTTP server | `http://localhost:8080` | **No** until Editor running with **Custom** pin + server up |

---

## A. Unity-MCP

Unity-MCP connects Claude and Cursor to a running Unity Editor via MCP.

**Project convention:** clients always use `http://localhost:8080` (`ai-game-developer`).

**Package ≥0.86 default (do not rely on it):** Cloud mode + a path-hashed port in **20000–29999**. That mismatches committed client configs unless you pin local Custom + `:8080`.

### Already configured (repo)

- **Unity project path**: `unity/ProjectAegis` (no spaces — required by CLI)
- **Package**: `com.ivanmurzak.unity.mcp` **0.86.0** in `unity/ProjectAegis/Packages/manifest.json` (OpenUPM scopes present)
- **CLI**: `npx --yes unity-mcp-cli@0.86.0` (optional global: `npm install -g unity-mcp-cli`)
- **MCP config** (both editors point at the same local HTTP server):
  - `.cursor/mcp.json` — Cursor project-scoped (`"type": "http"`, `"url": "http://localhost:8080"`)
  - `.mcp.json` — Claude Code project-scoped (same shape)
- **Pin helpers** (UserSettings is gitignored — run once per clone):
  - `./tools/pin-unity-mcp-8080.sh` or `./tools/pin-unity-mcp-8080.ps1`
  - Editor menu: **Project Aegis → MCP → Pin Local Host :8080**

```json
{
  "mcpServers": {
    "ai-game-developer": { "type": "http", "url": "http://localhost:8080" }
  }
}
```

### Activation steps (from repo root)

1. **Pin local Custom + `:8080`** (required on every fresh clone):
   ```bash
   ./tools/pin-unity-mcp-8080.sh
   # Windows: .\tools\pin-unity-mcp-8080.ps1
   ```
   Writes `unity/ProjectAegis/UserSettings/AI-Game-Developer-Config.json` with
   `connectionMode=Custom`, `host=http://localhost:8080`, `keepServerRunning=true`,
   `keepConnected=true`, `authOption=none`.

2. **Open Unity 6.3 LTS** (`6000.3.14f1`) and let Package Manager resolve the MCP package:
   ```bash
   npx --yes unity-mcp-cli@0.86.0 open ./unity/ProjectAegis
   ```
   Or Unity Hub → editor `6000.3.14f1`. Confirm **Window → AI Game Developer** shows Custom / `:8080`.

3. **Authenticate only if using Cloud / ai-game.dev relay** (optional for local `:8080` pin):
   ```bash
   npx --yes unity-mcp-cli@0.86.0 login ./unity/ProjectAegis
   ```

4. **Do not** run bare `setup-mcp cursor` without URL overrides — 0.86 writers may emit a hashed `/mcp/p/<pin>` URL and clobber other MCP servers. Prefer the committed mcp.json + pin script. If you must regenerate:
   ```bash
   npx --yes unity-mcp-cli@0.86.0 setup-mcp cursor ./unity/ProjectAegis \
     --transport http --url http://localhost:8080 --no-pin
   ```
   Then re-check that sibling servers in `.cursor/mcp.json` are intact and `ai-game-developer` still has `"type": "http"`.

5. **Install editor skills** (optional; regenerates skill bodies):
   ```bash
   npx --yes unity-mcp-cli@0.86.0 setup-skills cursor ./unity/ProjectAegis
   ```
   Restore every `<!-- PROJECT-AEGIS:BEGIN -->` … `<!-- PROJECT-AEGIS:END -->` block afterward (see `unity/ProjectAegis/.claude/README.md`).

6. **Verify MCP** — with Editor running and plugin connected:
   ```bash
   curl -sS -o /dev/null -w "%{http_code}\n" --max-time 5 http://localhost:8080
   # or: npx unity-mcp-cli@0.86.0 status ./unity/ProjectAegis
   ```
   Cursor connects via `.cursor/mcp.json` → `ai-game-developer`. Restart Cursor MCP after the first successful pin if the client was already loaded.

### Unity-MCP limitations

- Requires Unity Editor **6000.3.14f1** running with the plugin compiled
- Fresh clones need the **pin script / menu** — UserSettings is not committed
- Without the pin, the plugin uses Cloud + a machine-specific hashed port (not `:8080`)
- Headless CI / Cloud Agents do not need `:8080`; use `dotnet test` per [AGENTS.md](../AGENTS.md)
- NuGet MCP DLLs under `Assets/Plugins/NuGet/` are gitignored (metas only); first Editor open restores them via the package resolver

---

## B. Claude-Code-Game-Studios

The full Game-Studios template is vendored under `.claude/` with Unity-only engine agents.

### What was installed

| Path | Contents |
|------|----------|
| `.claude/agents/` | 39 studio agents (Godot/Unreal sets removed) |
| `.claude/skills/` | 73 workflow skills + preserved `gitnexus/` (6 skills) |
| `.claude/hooks/` | Session, validation, and notification bash hooks |
| `.claude/rules/` | Agent coordination rules |
| `.claude/docs/` | Templates, workflow catalog, coding standards |
| `.claude/settings.json` | Hooks and permissions config |
| `.claude/statusline.sh` | Status line script |
| `CLAUDE.md` | Game-Studios master config + GitNexus block |

### Unity engine agents kept

- `unity-specialist.md`
- `unity-dots-specialist.md` (DOTS/ECS)
- `unity-shader-specialist.md` (Shaders/VFX)
- `unity-addressables-specialist.md` (Addressables)
- `unity-ui-specialist.md` (UI Toolkit)

### Getting started workflow

Run these slash commands in **Claude Code** (not Cursor chat):

| Command | Purpose |
|---------|---------|
| `/start` | First-time onboarding — detects project state, routes to the right workflow |
| `/setup-engine unity` | Pin Unity LTS version, populate engine reference docs |
| `/help` | List all available slash commands |
| `/brainstorm` | Guided game ideation (if starting from scratch) |
| `/onboard` | Adopt existing/brownfield project |
| `/dev-story` | Implement a user story |
| `/code-review` | Request code review |
| `/gate-check` | Phase transition quality gate |

See `.claude/docs/workflow-catalog.yaml` for the full command catalog.

### Recommended path for Project Aegis

This repo already has requirements docs under `Game-Requirements/`. A practical starting sequence:

1. `/setup-engine unity` — configure Unity LTS and engine reference docs
2. `/onboard` or `/reverse-document` — map existing requirements into Game-Studios design structure
3. `/create-stories` — break requirements into implementable stories

### Game-Studios limitations

- **Hooks require bash**: `.claude/settings.json` invokes bash scripts (`session-start.sh`, validation hooks, etc.). On Windows, Git Bash must be on PATH. If bash is unavailable, hooks fail gracefully — agents and skills still work.
- **Claude Code features**: Slash commands, agents, hooks, and `settings.json` are Claude Code features. Cursor uses `.cursor/mcp.json` for MCP only; Game-Studios slash commands run in Claude Code CLI.
- **Unity path**: `unity/ProjectAegis/`; headless sim code remains under `src/`.

---

## C. GitNexus (preserved)

GitNexus code intelligence is preserved alongside Game-Studios:

- `.claude/skills/gitnexus/` — 6 skills (exploring, impact-analysis, debugging, refactoring, guide, cli)
- `AGENTS.md` — GitNexus block plus Cursor Cloud dev instructions
- `CLAUDE.md` — GitNexus block appended verbatim at end

Before committing code changes, run `gitnexus_detect_changes()` via the GitNexus MCP server.

---

## D. Verification checklist

- [x] `.cursor/mcp.json` and `.mcp.json` valid JSON with `ai-game-developer` (`type: http`, `:8080`)
- [x] `.claude/` Game-Studios template vendored
- [x] GitNexus skills preserved (6 SKILL.md files)
- [x] Godot/Unreal agents removed
- [x] No nested `.git` directories
- [x] Unity project at `unity/ProjectAegis/` (Editor pin `6000.3.14f1`)
- [x] OpenUPM scopes for `com.ivanmurzak` in `Packages/manifest.json`
- [x] `com.ivanmurzak.unity.mcp` **0.86.0** as a direct `Packages/manifest.json` dependency
- [x] Pin helpers: `tools/pin-unity-mcp-8080.sh` / `.ps1` + Editor menu `Project Aegis/MCP/Pin Local Host :8080`
- [x] Delegation plugin DLLs (`tools/copy-delegation-assemblies.ps1` + guardrail)
- [ ] `unity-mcp-cli` on PATH (optional — `npx` works)
- [ ] `./tools/pin-unity-mcp-8080.sh` run on this machine (UserSettings local)
- [ ] Unity Editor opened; Window → AI Game Developer shows Custom / `:8080`
- [ ] Unity Editor running; `http://localhost:8080` reachable
- [ ] Git Bash on PATH for hooks (optional on Windows)

### Environment audit (2026-08-18)

| Check | Result |
|-------|--------|
| Unity `6000.3.14f1` on disk | Local machine only (Cloud VM typically absent) |
| Package `com.ivanmurzak.unity.mcp` | **0.86.0** in manifest |
| Client mcp.json | `type: http` + `:8080` |
| Pin script dry-run | Writes gitignored UserSettings Custom/`http://localhost:8080` |
| MCP `:8080` live | Fail until Editor + pin on an interactive machine |
| Global `unity-mcp-cli` | Optional — use `npx unity-mcp-cli@0.86.0` |

**.NET / C#:** See [docs/engine-reference/dotnet/README.md](../docs/engine-reference/dotnet/README.md).

---

## References

- [Unity-MCP](https://github.com/IvanMurzak/Unity-MCP) · [Configuration wiki](https://github.com/IvanMurzak/Unity-MCP/wiki/Configuration)
- [Claude-Code-Game-Studios](https://github.com/Donchitos/Claude-Code-Game-Studios)
- [Tech Stack](../Tech-Stack.md)
- [Game Requirements Index](Game-Requirements-Index.md)
