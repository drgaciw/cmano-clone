# Tech Stack — Agentic Game Development

**Last verified:** 2026-05-29  
**Sources:** [Unity VERSION.md](docs/engine-reference/unity/VERSION.md), [dotnet README.md](docs/engine-reference/dotnet/README.md), [architecture.md](docs/architecture/architecture.md), GitNexus index `cmano-clone`, Context7 (Unity Entities 1.4, Microsoft Learn .NET)

---

## Core Game Engine

| Item | Pin |
|------|-----|
| **Engine** | Unity **6.3 LTS** |
| **Editor** | **6000.3.14f1** |
| **LTS support** | Through December 2027 ([support policy](https://unity.com/releases/unity-6/support)) |
| **Reference** | [docs/engine-reference/unity/VERSION.md](docs/engine-reference/unity/VERSION.md) |

**DOTS / ECS (pin when Unity project is created):**

| Package | Version (6000.3) | Role |
|---------|------------------|------|
| `com.unity.entities` | 1.4.6 | ECS worlds, `FixedStepSimulationSystemGroup` |
| `com.unity.burst` | 1.8.29 | Deterministic hot paths (`FloatMode.Deterministic`) |
| `com.unity.entities.graphics` | 1.4.20 | Client rendering only — not on dedicated server |

**API direction (Entities 1.4):** `Entities.ForEach` / `IAspect` are obsolete; use `IJobEntity` and `SystemAPI.Query`. See [upgrade guide](https://docs.unity3d.com/Packages/com.unity.entities@1.4/manual/upgrade-guide.html) and [dots-ecs-notes.md](docs/engine-reference/unity/dots-ecs-notes.md).

**Headless / agent-vs-agent:** Dedicated Server builds (`StandaloneBuildSubtarget.Server`, `UNITY_SERVER`); sim rules stay in pure C# assemblies, not `UnityEngine`.

---

## Programming Language & .NET Platform

| Item | Choice |
|------|--------|
| **Language** | C# (Microsoft) |
| **Runtime (solution today)** | **.NET 8** (`net8.0`) — all projects in `ProjectAegis.sln` |
| **Test framework** | NUnit via `dotnet test` |

**.NET release landscape (May 2026, Context7 / dotnet/core):**

| Channel | Phase | Notes for this repo |
|---------|--------|---------------------|
| **.NET 8** | LTS maintenance | **Current pin**; support ends **2026-11-10** |
| **.NET 9** | STS maintenance | Not used; ends 2026-11-10 |
| **.NET 10** | LTS active | Optional future migration (support to **2028-11-14**) |

Stay on **.NET 8** until a deliberate migration story exists for `ProjectAegis.Sim`, Delegation, and CI. Re-evaluate before .NET 8 EOL.

**Platform reference:** [docs/engine-reference/dotnet/README.md](docs/engine-reference/dotnet/README.md) — Microsoft Learn topic index, SDK pin, agent routing.

---

## Solution Assemblies (implemented vs planned)

Aligned with [master architecture](docs/architecture/architecture.md) and GitNexus clusters (`Delegation`, `Sim`, `UnityAdapter`).

| Assembly | Status | UnityEngine | Responsibility |
|----------|--------|-------------|----------------|
| `ProjectAegis.Delegation` | **Implemented** | No | Controllers, orchestration, decision pipeline |
| `ProjectAegis.Delegation.UnityAdapter` | **Implemented** | Bridge only | `ISimWorldSnapshot`, `IOrderSink`, `DelegationBridge` |
| `ProjectAegis.Sim` | **Started** | **No** | `SimTickRunner`, `SimClock`, `IPolicyEvaluator`, seeded RNG |
| `ProjectAegis.Sim.DOTS` | Planned | Entities + Burst | ECS systems (future) |
| `ProjectAegis.Data` | Planned | No | Platform DB, scenarios, policy templates |
| `ProjectAegis.Unity` | Planned | Yes | Rendering, C2 UI, editor |

**Invariant:** Simulation logic never references `UnityEngine`. UI never mutates sim state except via commands.

**Build / test:**

```bash
dotnet test ProjectAegis.sln -v minimal
dotnet run --project src/ProjectAegis.Delegation.Demo
```

Unity project path: `unity/ProjectAegis/` (scaffolding; Editor activation pending).

---

## Primary IDEs & Editors

- **Cursor** (preferred for agentic workflow)
- **Visual Studio 2022**
- **Visual Studio Code** with C# Dev Kit

---

## Code Intelligence & Documentation (agents)

| Tool | Role |
|------|------|
| **[GitNexus](https://github.com/abhigyanpatwagner/gitnexus)** | Indexed knowledge graph — impact analysis, execution flows, safe refactors. Stale after large merges → `npx gitnexus analyze`. CLI: `npx gitnexus query "…" --repo cmano-clone`, `npx gitnexus impact "Symbol" --repo cmano-clone`. |
| **[Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/)** | **Canonical .NET/C# docs** for `src/ProjectAegis.*` — pinned links in [dotnet/README.md](docs/engine-reference/dotnet/README.md). Skill: `/microsoft-learn-dotnet`. |
| **Context7** (Cursor plugin) | Live snippets — Unity Entities for `unity/`; library ID `/websites/learn_microsoft_en-us_dotnet` for pure C#. |
| **[Microsoft Learn MCP](https://github.com/microsoftdocs/mcp)** | **On** in `.cursor/mcp.json` / `.mcp.json` (`microsoft-learn`). Restart MCP after pull. Rule: `.cursor/rules/microsoft-learn-dotnet.mdc`. |

---

## AI Coding & Agent Tools

- **Cursor** (primary AI-first editor)
- **GitHub Copilot**
- **Claude Code / Claude Desktop**

### Claude-Specific Integrations

> **Status:** Repo wiring complete; Unity Editor activation pending.  
> See [Claude Agent Setup](Game-Requirements/Claude-Agent-Setup.md).

**[Unity-MCP](https://github.com/IvanMurzak/Unity-MCP)** — *configured (client-side)*

- Global CLI: `unity-mcp-cli` (see setup doc for pinned version)
- MCP: `.cursor/mcp.json` → `ai-game-developer` @ `http://localhost:8080`
- **Pending:** Unity plugin install, login, running Editor session

**[Claude-Code-Game-Studios](https://github.com/Donchitos/Claude-Code-Game-Studios)** — *configured*

- **53** agent definitions in `.claude/agents/` (Unity-focused studio set)
- **91** workflow skill packages in `.claude/skills/` (includes GitNexus, C#, determinism, `replay-verify`, military research)
- Unity specialists: DOTS/ECS, Shaders/VFX, Addressables, UI Toolkit
- Master config: `CLAUDE.md`; hooks, rules, templates under `.claude/`

### Additional Agentic Tools

- **Aider** (CLI Git agent)
- **Devon** (autonomous agent framework — monitoring)

---

## Version Control & Collaboration

- **Git** + **GitHub** (trunk-based development)
- **Design protocol:** [docs/COLLABORATIVE-DESIGN-PRINCIPLE.md](docs/COLLABORATIVE-DESIGN-PRINCIPLE.md) — draft → approval before multi-file writes

---

## Stack Rationale

This stack maximizes **agentic development** while staying on standard production tools:

1. **Unity 6.3 LTS + DOTS** — theater-scale sim performance, dedicated server headless runs, fixed-step determinism for replay/AAR ([requirements 17](Game-Requirements/requirements/17-Replay-AAR-And-Order-Log.md)).
2. **Pure C# sim + delegation assemblies** — testable without Editor; GitNexus traces cross-assembly flows (`SimTickRunner` ↔ policy ↔ delegation).
3. **MCP + studio agents** — in-Editor control (Unity-MCP) and structured design/implementation skills (Game-Studios).
4. **GitNexus + Microsoft Learn + Context7** — safe refactors and verified .NET/Unity APIs as the codebase grows.

---

## Related Documents

| Document | Purpose |
|----------|---------|
| [README.md](README.md) | Project status and quick start |
| [docs/architecture/architecture.md](docs/architecture/architecture.md) | Tick pipeline, layers, ADRs |
| [docs/engine-reference/unity/VERSION.md](docs/engine-reference/unity/VERSION.md) | Unity engine pin and DOTS packages |
| [docs/engine-reference/dotnet/README.md](docs/engine-reference/dotnet/README.md) | .NET SDK pin and Microsoft Learn index |
| [Game-Requirements/Claude-Agent-Setup.md](Game-Requirements/Claude-Agent-Setup.md) | MCP (Unity-MCP, optional Learn MCP) |

---

## Agent Verification Checklist

When updating this file:

1. Run `npx gitnexus status` — index should match `HEAD`.
2. Confirm `docs/engine-reference/unity/VERSION.md` editor and Entities versions.
3. Spot-check Context7 / Microsoft Learn for Unity Entities and .NET support phases if the calendar year advances.
4. Confirm `docs/engine-reference/dotnet/README.md` SDK and test package versions match `global.json` and `*.csproj`.
5. Re-count `.claude/agents/*.md` and `.claude/skills/` top-level folders if the Game-Studios vendor set changes.
