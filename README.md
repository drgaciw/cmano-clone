# Project Aegis

Project Aegis is a working-title near-future hardcore military simulation inspired by Command: Modern Air Naval Operations. The project focuses on theater-level command, AI agent delegation, and a deterministic, replayable simulation core, set roughly 5-10 years in the future.

Its near-future and speculative technology layer — drone swarms, hypersonics, directed energy, quantum sensing and the rest — is **design-stage roadmap, not shipped gameplay**. What exists today is the data spine for it: platform archetypes, swarm tiers, and tech-level gating. See [Near-future & speculative scope](#near-future--speculative-scope) below before assuming any specific system is playable.

## Vision

Create a next-generation wargame that combines traditional military simulation depth with agentic AI capabilities. The player acts as a theater commander, directing human and AI-driven forces while delegating tactical decisions to specialized agents where appropriate.

## Current Status

The repository contains requirements documentation and an initial **agent delegation framework** implementation:

- **Design spec:** `docs/superpowers/specs/2026-05-28-agent-delegation-framework-design.md`
- **Implementation plan:** `docs/superpowers/plans/2026-05-28-agent-delegation-framework.md`
- **Delegation framework:** `src/ProjectAegis.Delegation/` — engine-agnostic agent delegation core (tick → decision → autonomy/ROE gate → order log) ([README](src/ProjectAegis.Delegation/README.md))
- **Simulation core:** `src/ProjectAegis.Sim/` — deterministic tick pipeline, sensors, engagement, policy ([README](src/ProjectAegis.Sim/README.md))
- **Data / catalog layer:** `src/ProjectAegis.Data/` — SQLite catalog, staged write gate, immutable snapshots, scenario↔DB binding ([README](src/ProjectAegis.Data/README.md))
- **Unity bridge:** `src/ProjectAegis.Delegation.UnityAdapter/` (`ISimWorldSnapshot` in, `IOrderSink` out) ([README](src/ProjectAegis.Delegation.UnityAdapter/README.md))
- **Unity wiring:** `unity/ProjectAegis/` (DLL copy + optional `DelegationBridgeHost`)
- **Console demo:** `src/ProjectAegis.Delegation.Demo/` ([README](src/ProjectAegis.Delegation.Demo/README.md))
- **Mission Editor CLI / MCP tools:** `src/ProjectAegis.MissionEditor.Cli/` — headless scenario authoring, validation, and catalog verbs ([README](src/ProjectAegis.MissionEditor.Cli/README.md), [full reference](docs/engineering/mission-editor-cli.md))

**CI:** Buildkite primary pipeline — [buildkite-ci.md](docs/engineering/buildkite-ci.md)

Build and test (requires [.NET 8 SDK](https://dotnet.microsoft.com/download) **8.0.400**, see `global.json`):

```bash
dotnet test ProjectAegis.sln -v minimal
dotnet run --project src/ProjectAegis.Delegation.Demo
```

**Local CI parity** (same steps as [`.buildkite/pipeline.yml`](.buildkite/pipeline.yml) / [`tools/buildkite/dotnet-ci.sh`](tools/buildkite/dotnet-ci.sh)):

```powershell
.\tools\verify-ci-local.ps1
```

### VS Code on Linux — file watcher (ENOSPC)

Opening this workspace in VS Code on Linux can trigger **"Visual Studio Code is unable to watch for file changes in this large workspace" (error ENOSPC)** because the kernel's `inotify` watch budget is exhausted by `obj/`, `.git/objects/`, Unity caches, and other generated trees. Workspace exclusions are already committed in [`.vscode/settings.json`](.vscode/settings.json); the one host-side step is to raise the kernel watch budget:

```bash
echo 'fs.inotify.max_user_watches=524288' | sudo tee /etc/sysctl.d/99-vscode-inotify.conf
sudo sysctl --system
cat /proc/sys/fs/inotify/max_user_watches   # should print 524288
```

Full explanation, diagnostics, and other-editor notes: [docs/engineering/local-dev-environment.md](docs/engineering/local-dev-environment.md).

**CI / branch protection:** [docs/engineering/ci-and-branch-protection.md](docs/engineering/ci-and-branch-protection.md) — Buildkite blocking gate (`buildkite/cmano-clone`), Graphite optimizer, post-merge replay golden on `main`, GitHub Actions for CodeQL/GitNexus/Unity. Setup: [buildkite-ci.md](docs/engineering/buildkite-ci.md). Manual branch protection: [issue #37](https://github.com/drgaciw/cmano-clone/issues/37).

**Determinism & replay:** the sim is bit-for-bit reproducible per `(scenario, seed)`. Rules, the world-state/order-log hashing model, the golden-fixture workflow, and common pitfalls: [docs/engineering/determinism-and-replay.md](docs/engineering/determinism-and-replay.md).

**Engineering docs:** subsystem guides, CI/branch-protection, Graphite workflow, and local setup are indexed in [docs/engineering/README.md](docs/engineering/README.md).

**Work tracking:** delivery work is tracked in **Linear** ([cmano-clone project](https://linear.app/drgamtd-workspace/project/cmano-clone-7f6a00e4c1c9)), design/specs in Notion, and files in Git. GitHub Issues remains open for inbound bug reports and security findings, but is not the planning board — see [production/agentic/linear-parallel-dispatch-playbook.md](production/agentic/linear-parallel-dispatch-playbook.md) for the tracking contract and the parallel-agent dispatch loop. Sprint history through S107 lives in `production/epics/` and `production/agentic/`.

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
- Near-future systems — loyal wingman UAVs, drone swarms, hypersonic weapons, directed energy weapons, autonomous underwater vehicles, advanced electronic warfare, quantum sensors — **as roadmap**; see [Near-future & speculative scope](#near-future--speculative-scope)

## Near-future & speculative scope

Requirements [09 — Near-Future Technologies](Game-Requirements/requirements/09-Near-Future-Technologies.md) and [10 — Speculative & Black Project Systems](Game-Requirements/requirements/10-Speculative-Systems.md) describe the project's long-term technology ambition. **Most of it is not implemented.** To avoid over-reading the requirements corpus as a delivery commitment:

**Shipped today** — the data and gating spine only:

- `NearFutureArchetypeCatalog` — platform archetype rows with tech-level gates
- `SwarmTier` + `CatalogArchetypeGate` — swarm tier caps and archetype gating
- `HypersonicEngageGate` — a boolean engagement gate (not intercept geometry)
- `SpeculativeEngageGate` + tech-level / black-project scenario gates
- General-purpose EW/jamming and salvo deconfliction, which predate this layer

**Not implemented** — design text only, no runtime behaviour: loyal-wingman autonomy modes, swarm behaviour runtime, hypersonic intercept modelling, directed-energy thermal/power simulation, AUV simulation, quantum sensing, cognitive EW, orbital DEW, escalation ladders.

This split is tracked in Linear under milestone **H7 — Requirement Coverage Gaps**. Nothing in the store page or launch material depends on the unimplemented set.

## Project Phase

Requirements detailing is in progress. The next major step is to expand each requirements document into a complete specification covering purpose, vision, functional requirements, non-functional requirements, technical considerations, agentic capabilities, extensibility, and open questions.
