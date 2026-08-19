# Tech Stack - Agentic Game Development

**Core Game Engine:** Unity **6.3 LTS** (editor `6000.3.14f1`) — project at `unity/ProjectAegis/`

**Programming Language:** C# (Microsoft C#)

**Primary IDEs / Editors:**
- Cursor (preferred for agentic workflow)
- Visual Studio 2022
- Visual Studio Code with C# Dev Kit

**AI Coding & Agent Tools:**
- Cursor (primary AI-first editor)
- GitHub Copilot
- Claude Code / Claude Desktop

## Claude-Specific Integrations

> **Status: Configured** — client MCP + package **0.86.0** on disk; **local Custom `:8080` pin + Editor session** still required per machine.
> See [Claude Agent Setup](Game-Requirements/Claude-Agent-Setup.md) for activation steps.

**[Unity-MCP](https://github.com/IvanMurzak/Unity-MCP)** — *client + manifest configured*
- CLI: `npx unity-mcp-cli@0.86.0`; optional global `npm install -g unity-mcp-cli`
- MCP config: `.cursor/mcp.json`, `.mcp.json` → `"type": "http"`, `http://localhost:8080`
- Plugin: `com.ivanmurzak.unity.mcp` **0.86.0** in `unity/ProjectAegis/Packages/manifest.json`
- **Pin:** `./tools/pin-unity-mcp-8080.sh` / `.ps1` or Editor menu **Project Aegis → MCP → Pin Local Host :8080**
- **Pending on interactive machine:** Open Editor (`6000.3.14f1`), confirm Custom/`http://localhost:8080`, then verify curl/`status`

**.NET:** SDK pin **8.0.400** (`global.json`); headless `net8.0` + Unity plugins `netstandard2.1` — [dotnet reference](docs/engine-reference/dotnet/README.md)

**[Claude-Code-Game-Studios](https://github.com/Donchitos/Claude-Code-Game-Studios)** — *configured*
- 39 studio agents vendored in `.claude/agents/` (Unity engine set only)
- 73 workflow skills in `.claude/skills/` plus preserved GitNexus skills
- Unity specialists: DOTS/ECS, Shaders/VFX, Addressables, UI Toolkit
- Master config in `CLAUDE.md`; hooks, rules, and templates in `.claude/`

## Additional Agentic Tools

- Aider (command-line Git agent)
- Devon (autonomous agent framework - monitoring)

## Version Control

Git + GitHub

## Stack Rationale

This stack is intended to maximize leverage for agentic development while staying within standard industry tools. C# and Unity provide a strong production ecosystem for simulation-heavy game development, while Claude and MCP integrations enable agent-driven development inside the Unity Editor.
