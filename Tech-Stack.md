# Tech Stack - Agentic Game Development

**Core Game Engine:** Unity **6.3 LTS** (editor `6000.3.22f1`) — project at `unity/ProjectAegis/`

**Programming Language:** C# (Microsoft C#)

**Primary IDEs / Editors:**
- Cursor (preferred for agentic workflow)
- Visual Studio 2022
- Visual Studio Code with C# Dev Kit

**AI Coding & Agent Tools:**
- Cursor (primary AI-first editor)
- GitHub Copilot
- Claude Code / Claude Desktop
- Grok Build (project MCP: `.grok/config.toml` → `ai-game-developer`)

## Unity Project (working reference)

Pinned packages in `unity/ProjectAegis/Packages/manifest.json` (do not invent versions elsewhere):

| Package | Version | Role |
|---------|---------|------|
| `com.unity.ui` | 2.0.0 | UI Toolkit (C2) |
| `com.unity.addressables` | 2.3.16 | Addressables |
| `com.unity.burst` | 1.8.29 | Transitive (Sentis / Inference); not sim hot-path |
| `com.unity.ai.assistant` | 2.13.0-pre.2 | Unity AI Assistant — present, not agent-owned |
| `com.unity.ai.inference` | 2.6.1 | Unity Inference — present, not agent-owned |
| `com.ivanmurzak.unity.mcp` | 0.86.0 | Unity-MCP editor bridge (pin local Custom + `:8080`) |

**Not in manifest (removed 2026-07-07):** `com.unity.entities`, `com.unity.entities.graphics` — world state is managed/headless-first (ADR-005 reversed). See [VERSION.md](docs/engine-reference/unity/VERSION.md) and [unity integration review](docs/reports/unity-integration-review-2026-07-07.md) §3.

Engine pin and managed-sim notes: [docs/engine-reference/unity/VERSION.md](docs/engine-reference/unity/VERSION.md). Headless sim stays under `src/` (`net8.0`); Unity plugins target `netstandard2.1`.

**Project Aegis Unity invariants** (Input Manager, Built-in RP, Unity-MCP Custom `:8080`, headless-vs-Editor): [`unity/ProjectAegis/.claude/README.md`](unity/ProjectAegis/.claude/README.md).

### Unity / C2 agent routing

Use these Cursor/Claude agents from `.claude/agents/` (do not invent specialists):

| Concern | Agent |
|---------|--------|
| General Unity patterns / MonoBehaviour (presentation layer) | `unity-specialist` |
| Historical DOTS notes / Jobs / Burst only if reintroduced | `unity-dots-specialist` |
| UI Toolkit (UXML/USS), runtime UI perf | `unity-ui-specialist` |
| Addressables / content loading | `unity-addressables-specialist` |
| Shaders / VFX (Built-in RP; do not assume URP/HDRP) | `unity-shader-specialist` |
| C2 presentation orchestration (UI Toolkit + mil-sim UX) | `ui-experience-lead` |
| Mil-sim information density / cognitive load | `user-experience-military-analyst` |
| UI framework code / screen flow (non-Editor-specialist) | `ui-programmer` |

### Skill locations (two trees)

| Tree | Path | Purpose |
|------|------|---------|
| Studio workflow | `.claude/skills/` | Repo-root Game-Studios + Project Aegis skills (GitNexus, Hindsight, `/dev-story`, mil-sim UX, etc.) |
| Unity Editor MCP | `unity/ProjectAegis/.claude/skills/` | Generated from Unity-MCP tools (`unity-skill-generate`); Editor session ops (scenes, GameObjects, assets, profiler, `tests-run`) |

Start Editor MCP setup with `unity-initial-setup` / `unity-tool-list` under the Unity skills tree. Do not confuse these with repo-root studio skills.

## Claude-Specific Integrations

> **Status: Configured** — client MCP + package **0.86.0** on disk; **local Custom `:8080` pin + Editor session** still required per machine.
> See [Claude Agent Setup](Game-Requirements/Claude-Agent-Setup.md) for activation steps.

**[Unity-MCP](https://github.com/IvanMurzak/Unity-MCP)** — *manifest + clients configured; live `:8080` requires Editor + per-clone pin*
- CLI: `npx unity-mcp-cli@0.86.0` (optional global `npm install -g unity-mcp-cli`)
- MCP config: `.cursor/mcp.json`, `.mcp.json`, `.grok/config.toml` → `http://localhost:8080` (`ai-game-developer`)
- Package: `com.ivanmurzak.unity.mcp` **0.86.0** is a direct dependency in `unity/ProjectAegis/Packages/manifest.json` (OpenUPM scopes present)
- **Pin (required per clone):** `./tools/pin-unity-mcp-8080.sh` (or `.ps1`) / Editor menu **Project Aegis → MCP → Pin Local Host :8080** — package default is Cloud + hashed 20000–29999 port
- **Interactive machine:** open Editor (`6000.3.22f1`), confirm Custom/`http://localhost:8080`, then `curl` / `status`. If `:8080` is down, stay headless (`/team-unity`).
- Generated Editor skills land under `unity/ProjectAegis/.claude/skills/` after `setup-skills` / `unity-skill-generate`

**.NET:** SDK pin **8.0.400** (`global.json`); headless `net8.0` + Unity plugins `netstandard2.1` — [dotnet reference](docs/engine-reference/dotnet/README.md)

**[Claude-Code-Game-Studios](https://github.com/Donchitos/Claude-Code-Game-Studios)** — *configured*
- Studio agents in `.claude/agents/` (Unity engine set + Project Aegis specialists; see routing table above)
- Workflow skills in `.claude/skills/` (includes preserved `gitnexus/` and `hindsight/`)
- Master config in `CLAUDE.md`; hooks, rules, and templates in `.claude/`

## Additional Agentic Tools

- **Graphite** — stacked PRs (`gt create` / `gt submit`); see [graphite-github-substitute-plan.md](docs/engineering/graphite-github-substitute-plan.md)
- **GitNexus** — code intelligence MCP + `.claude/skills/gitnexus/` (impact before edit; `detect_changes` before commit)
- **Hindsight** — local session memory (`localhost:8888`) + `.claude/skills/hindsight/`; pair with GitNexus for agentic loops (never inside sim `Tick()`)

## Version Control

Git + GitHub (Graphite-first for stacked PRs)

## Stack Rationale

This stack is intended to maximize leverage for agentic development while staying within standard industry tools. C# and Unity provide a strong production ecosystem for simulation-heavy game development, while Claude/Cursor agents, Unity-MCP, GitNexus, and Hindsight enable agent-driven work across headless .NET and the Unity Editor.
