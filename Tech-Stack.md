# Tech Stack - Agentic Game Development

**Core Game Engine:** Unity (Latest LTS Version)

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

> **Status: Configured** — repo wiring complete; Unity Editor activation pending.
> See [Claude Agent Setup](Game-Requirements/Claude-Agent-Setup.md) for remaining steps and workflow guide.

**[Unity-MCP](https://github.com/IvanMurzak/Unity-MCP)** — *configured (client-side)*
- Global CLI installed (`unity-mcp-cli`); MCP config committed in `.cursor/mcp.json` and `.mcp.json`
- Connects Claude and Cursor directly to Unity Editor via `http://localhost:8080`
- Provides 100+ MCP tools for real-time scene editing, asset management, and runtime control
- **Pending**: Unity plugin install, login, and Editor session (requires Unity project)

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
