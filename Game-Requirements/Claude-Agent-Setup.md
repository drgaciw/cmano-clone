# Claude Agent Setup — Project Aegis

This guide covers the Claude-specific integrations configured for Project Aegis: **Unity-MCP** (real-time Unity Editor bridge) and **Claude-Code-Game-Studios** (studio agent hierarchy and workflow skills).

## Status Summary

| Integration | Repo config | Runtime ready |
|-------------|-------------|---------------|
| Unity project | `unity/ProjectAegis/` (6000.3.14f1) | Yes |
| Unity-MCP CLI | `npx unity-mcp-cli` **0.77.0** (global npm install optional) | Partial — not on PATH unless installed |
| MCP server config | `.cursor/mcp.json`, `.mcp.json` | Yes (needs Editor + plugin active on :8080) |
| Game-Studios agents/skills | `.claude/` vendored | Yes (Claude Code) |
| Unity Editor plugin | `com.ivanmurzak.unity.mcp` — **not** a direct `manifest.json` dependency yet (OpenUPM scopes only) | **Pending** — run `install-plugin`, then open Editor once |
| MCP HTTP server | `http://localhost:8080` | **No** until Editor running with plugin logged in |

---

## A. Unity-MCP

Unity-MCP connects Claude and Cursor to a running Unity Editor via MCP on `http://localhost:8080`.

### Already configured (repo)

- **Unity project path**: `unity/ProjectAegis` (no spaces — required by CLI)
- **CLI**: `npx --yes unity-mcp-cli` (v0.77.0 as of 2026-06-02); optional global: `npm install -g unity-mcp-cli`
- **MCP config** (both editors point at the same server):
  - `.cursor/mcp.json` — Cursor project-scoped
  - `.mcp.json` — Claude Code project-scoped

```json
{
  "mcpServers": {
    "ai-game-developer": { "url": "http://localhost:8080" }
  }
}
```

### Activation steps (from repo root)

OpenUPM **scopes** for `com.ivanmurzak` are already in `unity/ProjectAegis/Packages/manifest.json`. The package **`com.ivanmurzak.unity.mcp` is not yet a direct dependency** — install it before expecting `:8080` (matches [`Tech-Stack.md`](../Tech-Stack.md)):

1. **Install the plugin** (adds the package to dependencies):
   ```powershell
   npx --yes unity-mcp-cli install-plugin ./unity/ProjectAegis
   ```

2. **Open Unity 6.3 LTS** (`6000.3.14f1`) and let Package Manager resolve the MCP package (first open after install).

3. **Authenticate** (interactive, requires browser):
   ```powershell
   npx --yes unity-mcp-cli login ./unity/ProjectAegis
   ```

4. **Open the project** (or use Unity Hub):
   ```powershell
   npx --yes unity-mcp-cli open ./unity/ProjectAegis
   ```

5. **Install editor skills** (optional):
   ```powershell
   npx --yes unity-mcp-cli setup-skills cursor
   ```

6. **Verify MCP** — with Editor running and plugin active:
   ```powershell
   Invoke-WebRequest -Uri http://localhost:8080 -UseBasicParsing -TimeoutSec 5
   ```
   Cursor connects via `.cursor/mcp.json` → `ai-game-developer`.

**Do not** claim the plugin is already listed under `dependencies` until `install-plugin` has been run and `manifest.json` shows `com.ivanmurzak.unity.mcp`.

### Unity-MCP limitations

- Requires Unity Editor **6000.3.14f1** running with the plugin compiled
- `login` is interactive (cloud auth)
- `unity-mcp-cli` must be on PATH or invoked via `npx`
- Headless CI does not need `:8080`; use `dotnet test` per [AGENTS.md](../AGENTS.md)

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

- [x] `.cursor/mcp.json` and `.mcp.json` valid JSON with `ai-game-developer` server
- [x] `.claude/` Game-Studios template vendored
- [x] GitNexus skills preserved (6 SKILL.md files)
- [x] Godot/Unreal agents removed
- [x] No nested `.git` directories
- [x] Unity project at `unity/ProjectAegis/` (Editor pin `6000.3.14f1`)
- [x] OpenUPM scopes for `com.ivanmurzak` in `Packages/manifest.json`
- [ ] `com.ivanmurzak.unity.mcp` as a **direct** `Packages/manifest.json` dependency (run `npx unity-mcp-cli install-plugin ./unity/ProjectAegis`)
- [x] Delegation plugin DLLs (`tools/copy-delegation-assemblies.ps1` + guardrail)
- [ ] `unity-mcp-cli` on PATH (optional — `npx` works)
- [ ] Unity Editor opened once after plugin install (Package Manager resolve)
- [ ] `unity-mcp-cli login` completed
- [ ] Unity Editor running; `http://localhost:8080` reachable
- [ ] Git Bash on PATH for hooks (optional on Windows)

### Environment audit (2026-06-02)

| Check | Result |
|-------|--------|
| Unity `6000.3.14f1` on disk | Pass |
| `dotnet` build + filtered tests | Pass (SDK 10.0.300 roll-forward) |
| Plugin DLL guardrail | Pass (17 DLLs) |
| MCP `:8080` | Fail — connection refused (Editor not running) |
| Global `unity-mcp-cli` | Fail — use `npx`; install with `npm i -g unity-mcp-cli` if desired |

**.NET / C#:** See [docs/engine-reference/dotnet/README.md](../docs/engine-reference/dotnet/README.md).

---

## References

- [Unity-MCP](https://github.com/IvanMurzak/Unity-MCP)
- [Claude-Code-Game-Studios](https://github.com/Donchitos/Claude-Code-Game-Studios)
- [Tech Stack](../Tech-Stack.md)
- [Game Requirements Index](Game-Requirements-Index.md)
