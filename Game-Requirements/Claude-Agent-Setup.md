# Claude Agent Setup — Project Aegis

This guide covers the Claude-specific integrations configured for Project Aegis: **Unity-MCP** (real-time Unity Editor bridge) and **Claude-Code-Game-Studios** (studio agent hierarchy and workflow skills).

## Status Summary

| Integration | Repo config | Runtime ready |
|-------------|-------------|---------------|
| Unity-MCP CLI | Installed globally (`unity-mcp-cli@0.76.1`) | Yes |
| MCP server config | `.cursor/mcp.json`, `.mcp.json` | Yes (needs Unity server running) |
| Game-Studios agents/skills | `.claude/` vendored | Yes (Claude Code) |
| Unity Editor plugin | Not installed | **Blocked — no Unity project yet** |

---

## A. Unity-MCP

Unity-MCP connects Claude and Cursor to a running Unity Editor via MCP on `http://localhost:8080`.

### Already configured (repo)

- **Global CLI**: `unity-mcp-cli` installed via npm
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

### Remaining steps (requires Unity project)

These steps cannot be run until a Unity project exists (e.g. after scaffolding `Assets/` and `ProjectSettings/`). Do **not** run them against this docs-only repo.

1. **Install the Unity plugin** (from repo root or Unity project root):
   ```powershell
   unity-mcp-cli install-plugin ./<UnityProjectPath>
   ```
   The project path must contain **no spaces**.

2. **Authenticate** (interactive, requires browser):
   ```powershell
   unity-mcp-cli login
   ```

3. **Open the Unity project** in Unity Editor:
   ```powershell
   unity-mcp-cli open ./<UnityProjectPath>
   ```

4. **Install editor skills** (optional, for Cursor or Claude Code):
   ```powershell
   unity-mcp-cli setup-skills cursor
   # or
   unity-mcp-cli setup-skills claude-code
   ```

5. **Verify the MCP server** is reachable once Unity Editor is running with the plugin active:
   - Server URL: `http://localhost:8080`
   - Cursor and Claude Code will connect automatically via the committed MCP config

### Unity-MCP limitations

- Requires Unity Editor running with the plugin installed
- `login` is interactive (cloud auth)
- No Unity project = no functional MCP tools yet

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
3. `/create-stories` — break requirements into implementable stories (once Unity project exists)

### Game-Studios limitations

- **Hooks require bash**: `.claude/settings.json` invokes bash scripts (`session-start.sh`, validation hooks, etc.). On Windows, Git Bash must be on PATH. If bash is unavailable, hooks fail gracefully — agents and skills still work.
- **Claude Code features**: Slash commands, agents, hooks, and `settings.json` are Claude Code features. Cursor uses `.cursor/mcp.json` for MCP only; Game-Studios slash commands run in Claude Code CLI.
- **Scaffold dirs not created**: `src/`, `assets/`, `design/`, and `production/` were intentionally skipped until a Unity project is scaffolded.

---

## C. Microsoft Learn (.NET / C#)

Pure simulation and delegation code (`src/ProjectAegis.Sim`, `src/ProjectAegis.Delegation`) uses **Microsoft Learn** as the canonical platform reference—not Unity docs.

| Resource | Purpose |
|----------|---------|
| [docs/engine-reference/dotnet/README.md](../docs/engine-reference/dotnet/README.md) | SDK pin, topic → Learn URL table, ADR reference block |
| `/microsoft-learn-dotnet` | Skill: routes agents to Learn + Context7 |
| Context7 | Library ID `/websites/learn_microsoft_en-us_dotnet` in Cursor |

### Microsoft Learn MCP (configured)

Committed in `.cursor/mcp.json` and `.mcp.json`:

```json
"microsoft-learn": { "url": "https://learn.microsoft.com/api/mcp" }
```

- **Restart Cursor / Claude Code MCP** after pulling this change so `microsoft-learn` tools load.
- **Use Learn MCP** for official .NET/C# docs, code samples, and API verification on `src/ProjectAegis.*`.
- **Use Context7** as fallback (`/websites/learn_microsoft_en-us_dotnet`).
- **Use GitNexus** before editing C# symbols (`npx gitnexus impact "SymbolName" --repo cmano-clone`).
- **Cursor rule:** `.cursor/rules/microsoft-learn-dotnet.mdc` (applies to `src/ProjectAegis.*` and test projects).

Agents/skills updated: `c-sharp-engineer`, `c-sharp-test-engineer`, `determinism-audit`, `architecture-decision`.

---

## D. GitNexus (preserved)

GitNexus code intelligence is preserved alongside Game-Studios:

- `.claude/skills/gitnexus/` — 6 skills (exploring, impact-analysis, debugging, refactoring, guide, cli)
- `AGENTS.md` — unchanged GitNexus block
- `CLAUDE.md` — GitNexus block appended verbatim at end

Before committing code changes, run `gitnexus_detect_changes()` via the GitNexus MCP server.

---

## E. Verification checklist

- [x] `unity-mcp-cli` installed and on PATH
- [x] `.cursor/mcp.json` and `.mcp.json` valid JSON with `ai-game-developer` and `microsoft-learn` servers
- [x] `.claude/` Game-Studios template vendored
- [x] GitNexus skills preserved (6 SKILL.md files)
- [x] Godot/Unreal agents removed
- [x] No nested `.git` directories
- [ ] Unity plugin installed (blocked — no Unity project)
- [ ] Unity Editor running with MCP server (blocked — no Unity project)
- [ ] Git Bash on PATH for hooks (optional on Windows)

---

## References

- [Unity-MCP](https://github.com/IvanMurzak/Unity-MCP)
- [Claude-Code-Game-Studios](https://github.com/Donchitos/Claude-Code-Game-Studios)
- [Tech Stack](../Tech-Stack.md)
- [Game Requirements Index](Game-Requirements-Index.md)
