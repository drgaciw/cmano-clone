# Project Aegis

Project Aegis is a working-title near-future hardcore military simulation inspired by Command: Modern Air Naval Operations. The project focuses on theater-level command, autonomous systems, drone swarms, advanced AI agents, and emerging military technologies set roughly 5-10 years in the future.

## Vision

Create a next-generation wargame that combines traditional military simulation depth with agentic AI capabilities. The player acts as a theater commander, directing human and AI-driven forces while delegating tactical decisions to specialized agents where appropriate.

## Current Status

The repository contains requirements documentation and an initial **agent delegation framework** implementation:

- **Design spec:** `docs/superpowers/specs/2026-05-28-agent-delegation-framework-design.md`
- **Implementation plan:** `docs/superpowers/plans/2026-05-28-agent-delegation-framework.md`
- **Library:** `src/ProjectAegis.Delegation/` (engine-agnostic .NET 8, NUnit tests)
- **Unity bridge:** `src/ProjectAegis.Delegation.UnityAdapter/` (`ISimWorldSnapshot` in, `IOrderSink` out)
- **Unity wiring:** `unity/ProjectAegis/` (DLL copy + optional `DelegationBridgeHost`)
- **Console demo:** `src/ProjectAegis.Delegation.Demo/`

**CI:** Buildkite primary pipeline — [buildkite-ci.md](docs/engineering/buildkite-ci.md)

**Engineering docs:** subsystem references, runbooks, CI, and workflow guides are indexed in [docs/engineering/README.md](docs/engineering/README.md).

Build and test (requires [.NET 8 SDK](https://dotnet.microsoft.com/download) **8.0.400**, see `global.json`):

```bash
dotnet test ProjectAegis.sln -v minimal
dotnet run --project src/ProjectAegis.Delegation.Demo
```

**Local CI parity** (same steps as [`.buildkite/pipeline.yml`](.buildkite/pipeline.yml) / [`tools/buildkite/dotnet-ci.sh`](tools/buildkite/dotnet-ci.sh)):

```powershell
.\tools\verify-ci-local.ps1
```

**CI / branch protection:** [docs/engineering/ci-and-branch-protection.md](docs/engineering/ci-and-branch-protection.md) — Buildkite blocking gate (`buildkite/cmano-clone`), Graphite optimizer, post-merge replay golden on `main`, GitHub Actions for CodeQL/GitNexus/Unity. Setup: [buildkite-ci.md](docs/engineering/buildkite-ci.md). Manual branch protection: [issue #37](https://github.com/drgaciw/cmano-clone/issues/37).

**Cursor Cloud agents:** see the [Cursor Cloud specific instructions](AGENTS.md#cursor-cloud-specific-instructions) section in `AGENTS.md` (headless build/test, Play Mode smoke harness, `.cursor/cloud-install.sh` bootstrap via `.cursor/environment.json`).

Headless simulation and delegation spine are implemented (`ProjectAegis.Sim`, Baltic replay harness, sensor classify FSM, UI Toolkit C2/message log). Unity project lives under `unity/ProjectAegis/` (Editor optional; headless smoke in CI). Requirements live under `Game-Requirements/`.

## Tech Stack

The planned development stack is Unity LTS with C#, supported by agentic development tools including Cursor, GitHub Copilot, Claude Code / Claude Desktop, Unity-MCP, and Claude-Code-Game-Studios.

Claude-specific integrations are **configured** at the repo level (MCP config, Game-Studios agents/skills). Global **[obra/superpowers](https://github.com/obra/superpowers)** methodology (TDD, plans, debugging) is installed via `.\tools\install-superpowers.ps1` — see `docs/engineering/superpowers-setup.md`. Unity Editor activation is pending until a Unity project is scaffolded.

- [Tech Stack - Agentic Game Development](Tech-Stack.md)
- [Claude Agent Setup](Game-Requirements/Claude-Agent-Setup.md)

## Requirements

Start here:

- [Game Requirements Master Index](Game-Requirements/Game-Requirements-Index.md)

Core documents:

- [Project Overview](Game-Requirements/requirements/01-Project-Overview.md)
- [Core Gameplay Loop](Game-Requirements/requirements/02-Core-Gameplay-Loop.md)
- [Simulation Modes](Game-Requirements/requirements/03-Simulation-Modes.md)

Agent and intelligence systems:

- [Agent Delegation System](Game-Requirements/requirements/04-Agent-Delegation.md)
- [Dynamic Speculative Systems Agent](Game-Requirements/requirements/05-Dynamic-Systems-Agent.md)
- [Database Intelligence Layer](Game-Requirements/requirements/06-Database-Intelligence.md)
- [Agentic Infrastructure Framework](Game-Requirements/requirements/07-Agentic-Infrastructure.md)
- [Agentic Architecture Layer](Game-Requirements/requirements/08-Agentic-Architecture.md)

Content and systems:

- [Near-Future Technologies](Game-Requirements/requirements/09-Near-Future-Technologies.md)
- [Speculative & Black Project Systems](Game-Requirements/requirements/10-Speculative-Systems.md)

## Key Concepts

- Human, mixed, and fully autonomous simulation modes
- AI agent delegation for units, groups, and task forces
- Dynamic discovery and proposal of emerging military systems
- Database intelligence agents for validation, normalization, and change tracking
- Near-future systems including loyal wingman UAVs, drone swarms, hypersonic weapons, directed energy weapons, autonomous underwater vehicles, advanced electronic warfare, and quantum sensors

## Project Phase

Requirements detailing is in progress. The next major step is to expand each requirements document into a complete specification covering purpose, vision, functional requirements, non-functional requirements, technical considerations, agentic capabilities, extensibility, and open questions.
