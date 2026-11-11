# Project Aegis

Project Aegis is a near-future military simulation in the style of Command: Modern Air/Naval Operations. The player commands theater-level forces and delegates tactical decisions to AI agents. The simulation is deterministic: the same scenario and seed reproduce the same order log.

The recorded stage is **Release**. This repository contains a headless .NET simulation, a catalog, a mission-editor CLI, and a Unity presentation project, and the headless path is covered by tests. Owner Play Mode acceptance is still an open gate. See [Current status](#current-status).

## Current status

Recorded stage is **Release** (`production/stage.txt`). The current roadmap snapshot still defers Launch: [`docs/reports/future-sprint-roadpmap.md`](docs/reports/future-sprint-roadpmap.md) (dated file [`future-sprint-roadmap-09302026.md`](docs/reports/future-sprint-roadmap-09302026.md)).

The numbered program in that snapshot is **S122–S127**. [`production/sprint-status-s122-s127.yaml`](production/sprint-status-s122-s127.yaml) (follow-up dated 2026-10-01) marks delivery as not closed and owner Play Mode acceptance ([DRG-208](https://linear.app/drgamtd-workspace/issue/DRG-208)) as pending. The index is [`production/sprints/sprint-122-127-index.md`](production/sprints/sprint-122-127-index.md). That YAML is a planning snapshot. Behavior on `main` is the code and the tests, which have kept moving after 2026-10-01.

### In the tree and covered by headless tests

These assemblies target `net10.0` (shared libraries also target `netstandard2.1`) and do not reference `UnityEngine`:

| Piece | Where | What it does |
| --- | --- | --- |
| Simulation | [`src/ProjectAegis.Sim/`](src/ProjectAegis.Sim/README.md) | Fixed-step tick, seeded RNG, detection, engagement resolution, policy evaluation, fuel, sim clock |
| Delegation | [`src/ProjectAegis.Delegation/`](src/ProjectAegis.Delegation/README.md) | Agent decisions from traits and attention, autonomy and ROE gates, append-only order log |
| Catalog and scenarios | [`src/ProjectAegis.Data/`](src/ProjectAegis.Data/README.md) | SQLite catalog, staged write gate, snapshots, scenario packages and authoring documents |
| Workbook I/O | [`src/ProjectAegis.Data.Excel/`](src/ProjectAegis.Data.Excel/README.md) | Excel round-trip for platform authoring ([ADR-011](docs/architecture/adr-011-platform-editor-excel-roundtrip.md)) |
| Adapter and C2 projections | [`src/ProjectAegis.Delegation.UnityAdapter/`](src/ProjectAegis.Delegation.UnityAdapter/README.md) | `DelegationBridge`, `ISimWorldSnapshot` / `IOrderSink`, read-only C2 view models, Baltic replay harness |
| Console demo | [`src/ProjectAegis.Delegation.Demo/`](src/ProjectAegis.Delegation.Demo/README.md) | Headless Baltic replay from the shell |
| Mission Editor CLI | [`src/ProjectAegis.MissionEditor.Cli/`](src/ProjectAegis.MissionEditor.Cli/README.md) | Headless scenario authoring, validation, and catalog verbs |

Scenario **policies** (`data/scenarios/*.policy.json`) are sim-runtime config. Scenario **documents** (`*.scenario.json`) are the authoring artifact. They are different files: [scenario policy authoring](docs/engineering/scenario-policy-authoring.md), [scenario document authoring](docs/engineering/scenario-document-authoring.md), [policy id list](data/scenarios/scenario-policy-ids.md).

Replay fixtures live under [`tests/regression/`](tests/regression/). The committed floors are in [`production/test-floor.json`](production/test-floor.json): `solutionMinPass` 1638, `replayGoldenMin` 6, `playModeSmokeMin` 20, Baltic v2 hash `17144800277401907079`. Those numbers are minimums recorded in that file, not a live count of the suite.

`PlayModeSmokeHarnessTests` is a **headless** C2 proxy in the UnityAdapter test project. It does not launch the Unity Editor. Editor Play Mode is the checklist in [`unity/ProjectAegis/PLAYMODE-SMOKE.md`](unity/ProjectAegis/PLAYMODE-SMOKE.md).

### Unity presentation

[`unity/ProjectAegis/`](unity/ProjectAegis/) is a Unity 6 project. The checked-in scene is [`Assets/Scenes/DelegationSmoke.unity`](unity/ProjectAegis/Assets/Scenes/DelegationSmoke.unity), with UI Toolkit C2 hosts (top bar, drawers, map placeholder, unit detail, message log) described in the Play Mode checklist. The Editor is a client of the headless sim ([ADR-010](docs/architecture/adr-010-headless-first-command-driven-ui.md)). Plugin DLLs under `Assets/Plugins/` are gitignored; copy them before opening the Editor ([Build and run](#build-and-run)).

World state lives in the managed simulation (`ProjectAegis.Sim`). [`Packages/manifest.json`](unity/ProjectAegis/Packages/manifest.json) has no `com.unity.entities` dependency. Background: [Unity integration review (2026-07-07)](docs/reports/unity-integration-review-2026-07-07.md).

### Specified, and not a runtime system

[Simulation capability gap backlog](docs/engineering/sim-capability-gap-backlog.md) is the maintained list of milsim behavior the sim does not model (support roles such as tanker and AEW that can be authored without a runtime effect, no weather or terrain model, engagements that resolve inside the launch tick, and similar gaps). The July 2026 sweep was not fully re-audited when that page was refreshed; read the page before treating a gap as closed.

Near-future and speculative tech in the requirements is mostly **data and gates**:

- [`NearFutureArchetypeCatalog`](src/ProjectAegis.Data/Catalog/NearFutureArchetypeCatalog.cs) loads archetype rows (tech level, swarm tier, entity cap).
- [`SwarmTierLimits`](src/ProjectAegis.Data/Catalog/SwarmTier.cs) parses those tier labels.
- [`HypersonicEngageGate`](src/ProjectAegis.Sim/Engage/HypersonicEngageGate.cs) allows or aborts a hypersonic target based on a defense-layer flag. It does not simulate an intercept trajectory.
- [`SpeculativeEngageGate`](src/ProjectAegis.Sim/Scenario/SpeculativeEngageGate.cs) aborts when a weapon is above the scenario tech level or needs black-project mode.

Requirements [09](Game-Requirements/requirements/09-Near-Future-Technologies.md) and [10](Game-Requirements/requirements/10-Speculative-Systems.md) still describe loyal-wingman autonomy, swarm behavior, directed-energy weapons, autonomous underwater vehicles, and quantum sensing. There are no runtime types for those systems under `src/`.

Same-build save and resume is specified in [ADR-029](docs/architecture/adr-029-h6-save-resume.md) and the [H6 contract](docs/engineering/h6-save-resume-contract.md). That contract states it does not implement save. `ReplayCheckpointStore` stores tick, world-hash, and log-fingerprint rows for replay checks.

Multiplayer is out of scope in that contract and in the current roadmap.

## Architecture

Headless libraries under `src/` are the authority for world state, orders, and catalog writes. The Unity project renders and sends commands. UI does not mutate sim state except through those commands ([ADR-010](docs/architecture/adr-010-headless-first-command-driven-ui.md), [ADR-001](docs/architecture/adr-001-sim-assembly-boundary.md)).

```text
data/scenarios + assets/data/catalog (SQLite, Git LFS)
        │
        ▼
ProjectAegis.Data  ──read-only DTOs──►  ProjectAegis.Sim
        │                                    │
        └──────────────► ProjectAegis.Delegation
                               │  DelegationOrchestrator.Tick
                               ▼
                  ProjectAegis.Delegation.UnityAdapter
                    ISimWorldSnapshot in, IOrderSink out
                    BalticReplayHarness (headless runner)
                               │
                               ▼
                  unity/ProjectAegis   UI Toolkit C2
                  (netstandard2.1 plugin DLLs)
```

Catalog mutations go through the write gate (propose, validate, approve, commit) and nowhere else ([ADR-006](docs/architecture/adr-006-data-layer-boundary.md)). Tick order is [ADR-004](docs/architecture/adr-004-tick-pipeline-order.md). The narrative write-up, including which steps run only inside `BalticReplayHarness`, is [`docs/architecture/architecture.md`](docs/architecture/architecture.md). Prefer the assembly READMEs linked above when that architecture page and this README disagree about engine versions: the pins in [Prerequisites](#prerequisites) come from `global.json` and `ProjectVersion.txt`.

Other entry points that call the same headless core:

- [`BalticReplayHarness`](src/ProjectAegis.Delegation.UnityAdapter/README.md) — replay goldens, the console demo, and the QA gauntlet runner. Guide: [baltic-replay-harness.md](docs/engineering/baltic-replay-harness.md).
- [`SimulationSession`](src/ProjectAegis.Delegation/README.md) — orchestrator plus engagement resolution for that headless path.
- Mission Editor CLI — same verbs the editor UI is meant to call. Reference: [mission-editor-cli.md](docs/engineering/mission-editor-cli.md).

Determinism rules, hashing, and how golden files are updated: [determinism-and-replay.md](docs/engineering/determinism-and-replay.md).

## Repository layout

| Path | Contents |
| --- | --- |
| [`ProjectAegis.sln`](ProjectAegis.sln) | Headless solution (see projects below) |
| `src/ProjectAegis.*` | Libraries, CLI, demo, benchmark, and co-located `*.Tests` projects |
| [`unity/ProjectAegis/`](unity/ProjectAegis/) | Unity Editor project (scenes, UI Toolkit, ProjectSettings) |
| [`data/scenarios/`](data/scenarios/) | Policy JSON, scenario documents, schema |
| [`assets/data/catalog/`](assets/data/catalog/) | Public catalog SQLite database (Git LFS) |
| [`tests/regression/`](tests/regression/) | Replay golden text fixtures |
| [`docs/architecture/`](docs/architecture/architecture.md) | Architecture narrative and ADRs |
| [`docs/engineering/`](docs/engineering/README.md) | Build, CI, and subsystem guides |
| [`docs/reports/`](docs/reports/future-sprint-roadpmap.md) | Roadmap snapshots (the un-dated `future-sprint-roadpmap.md` is an alias) |
| [`design/gdd/`](design/gdd/systems-index.md) | Game design documents |
| [`Game-Requirements/`](Game-Requirements/Game-Requirements-Index.md) | Requirements corpus |
| [`production/`](production/stage.txt) | Stage file, sprint plans, QA evidence, test floor |
| [`tools/`](tools/buildkite/dotnet-ci.sh) | Local CI gate, assembly copy, catalog checks |
| [`.buildkite/pipeline.yml`](.buildkite/pipeline.yml) | Primary CI pipeline |
| [`.github/workflows/`](.github/workflows/dotnet-ci.yml) | Secondary GitHub Actions workflows |

Solution projects:

| Project | Role |
| --- | --- |
| `ProjectAegis.Data` | Catalog, scenarios, write gate |
| `ProjectAegis.Data.Excel` | Workbook adapter |
| `ProjectAegis.Sim` | Simulation core |
| `ProjectAegis.Sim.Benchmark` | Benchmarks (not part of the test gate) |
| `ProjectAegis.Delegation` | Agent delegation |
| `ProjectAegis.Delegation.UnityAdapter` | Bridge, C2 projections, Baltic harness |
| `ProjectAegis.Delegation.Demo` | Console replay |
| `ProjectAegis.MissionEditor.Cli` | Headless mission editor |
| `ProjectAegis.*.Tests` and `ProjectAegis.Delegation.Demo.Tests` | xUnit projects next to the code they test |

Test projects stay next to their assemblies. Golden files stay under `tests/regression/`.

## Prerequisites

| Requirement | Source of truth | Needed for |
| --- | --- | --- |
| .NET SDK **10.0.403** (`rollForward`: `latestMajor`) | [`global.json`](global.json) | Build, test, demo, CLI |
| Git LFS | [`.gitattributes`](.gitattributes) (`assets/data/catalog/aegis_public_corpus.db`) | Catalog tests. A pointer file fails SQLite open. |
| Unity Editor **6000.3.27f2** | [`ProjectSettings/ProjectVersion.txt`](unity/ProjectAegis/ProjectSettings/ProjectVersion.txt) | Opening the Unity project. Headless `dotnet` work does not need it. |
| PowerShell 7 (`pwsh`) | — | `*.ps1` helpers, including [`tools/verify-ci-local.ps1`](tools/verify-ci-local.ps1) |
| Python 3.15 and `tools/qa-gauntlet/requirements.txt` | [`.github/workflows/gauntlet-pytest.yml`](.github/workflows/gauntlet-pytest.yml) | The pytest CI job only |

Install the SDK if `dotnet` is missing or older than the pin:

```bash
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version 10.0.403
export PATH="$HOME/.dotnet:$PATH"
```

Details and the C# language split (headless C# 12, Unity `Assets/` on the C# 10 opt-in in [`csc.rsp`](unity/ProjectAegis/Assets/csc.rsp)): [docs/engine-reference/dotnet/README.md](docs/engine-reference/dotnet/README.md).

Unity **6000.3.27f2** is the editor build in `ProjectVersion.txt` and the `unityVersion` input of [`.github/workflows/unity-ci.yml`](.github/workflows/unity-ci.yml). Some older guides still name `6000.3.22f1` (`AGENTS.md`, `CLAUDE.md`, `Tech-Stack.md`, `docs/engine-reference/unity/VERSION.md`). Install the `ProjectVersion.txt` build when they disagree.

Editor project facts, from ProjectSettings and the package manifest:

- Render pipeline: [`GraphicsSettings.asset`](unity/ProjectAegis/ProjectSettings/GraphicsSettings.asset) has an empty `m_RenderPipelineGlobalSettingsMap` (Built-in pipeline, no URP/HDRP asset).
- Input: [`ProjectSettings.asset`](unity/ProjectAegis/ProjectSettings/ProjectSettings.asset) sets `activeInputHandler: 0` (legacy Input Manager).
- Direct packages include `com.unity.ui` 2.0.0, `com.unity.addressables` 2.9.1, `com.unity.burst` 1.8.30, `com.ivanmurzak.unity.mcp` 0.90.0, plus the Unity AI packages listed in the manifest. Unity-MCP is an optional Editor bridge ([ADR-027](docs/architecture/adr-027-unity-editor-mcp-stack.md)); pin local port 8080 with [`tools/pin-unity-mcp-8080.sh`](tools/pin-unity-mcp-8080.sh) before expecting that server.

Node.js is not required for the .NET gate. Reference tooling under [`tools/cmano-db-crawler/`](tools/cmano-db-crawler/README.md) is Node.

On Linux, VS Code can hit `ENOSPC` while watching this tree. Workspace exclusions are in [`.vscode/settings.json`](.vscode/settings.json). The host-side `inotify` change is in [local-dev-environment.md](docs/engineering/local-dev-environment.md).

## Build and run

Run from the repository root. The blocking CI script is the command to trust:

```bash
bash tools/buildkite/dotnet-ci.sh
```

That script is what [`.buildkite/pipeline.yml`](.buildkite/pipeline.yml) runs (via [`tools/buildkite/agent-dotnet-ci.sh`](tools/buildkite/agent-dotnet-ci.sh)) and what [`.github/workflows/dotnet-ci.yml`](.github/workflows/dotnet-ci.yml) runs. It restores the solution, fails if any `*.db3` file is tracked, runs the CMO markdown import tests, builds Release, copies plugin DLLs, runs the full solution tests serially (`-m:1`), then runs `ReplayGoldenSuiteTests` and `PlayModeSmokeHarnessTests`.

[`tools/verify-ci-local.ps1`](tools/verify-ci-local.ps1) (PowerShell 7) calls [`scripts/verify-catalog-import.ps1`](scripts/verify-catalog-import.ps1) for the same `*.db3` and CMO markdown checks, then builds Release and runs the same three test commands without `-m:1` and without copying plugin DLLs. On a clean checkout, run the copy command below before the PowerShell script. Plugin tests look for those DLLs, and `Assets/Plugins/**/*.dll` is gitignored.

Day-to-day commands, matching that gate:

```bash
dotnet restore ProjectAegis.sln
dotnet build ProjectAegis.sln -c Release
dotnet test ProjectAegis.sln -c Release -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  -c Release --filter 'FullyQualifiedName~ReplayGoldenSuiteTests'
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  -c Release --filter 'FullyQualifiedName~PlayModeSmokeHarnessTests'
```

Headless Baltic replay (defaults are seed 42, scenario `baltic-patrol`, 4 ticks):

```bash
dotnet run --project src/ProjectAegis.Delegation.Demo -- --scenario baltic-patrol --seed 42 --ticks 4
```

Flag reference: [demo README](src/ProjectAegis.Delegation.Demo/README.md). Mission Editor verbs: [mission-editor-cli.md](docs/engineering/mission-editor-cli.md) and [tools/mission-editor/README.md](tools/mission-editor/README.md).

Before opening the Unity project, copy the `netstandard2.1` plugins (the DLLs are gitignored):

```bash
bash tools/copy-delegation-assemblies.sh
```

Then open `unity/ProjectAegis` in Editor **6000.3.27f2** and follow [PLAYMODE-SMOKE.md](unity/ProjectAegis/PLAYMODE-SMOKE.md). Editor Play Mode, and the manual workflow in [unity-ci.yml](.github/workflows/unity-ci.yml), need a local Editor plus Unity license secrets. They are separate from `dotnet-ci.sh`.

Do not commit proprietary Command: Modern Operations `*.db3` databases. Catalog data in this repo comes from fixtures, workbooks, and OSINT. Policy: [dual-track-cmo-analysis-and-catalog.md](docs/engineering/dual-track-cmo-analysis-and-catalog.md).

## Continuous integration

Primary pipeline is Buildkite, file [`.buildkite/pipeline.yml`](.buildkite/pipeline.yml). Setup notes: [buildkite-ci.md](docs/engineering/buildkite-ci.md). Branch-protection history: [ci-and-branch-protection.md](docs/engineering/ci-and-branch-protection.md) (updated 2026-07-13; required check recorded there is `buildkite/cmano-clone`).

| Step | When | Blocking |
| --- | --- | --- |
| Build and test (`agent-dotnet-ci.sh` → `dotnet-ci.sh`) | Every build | Yes |
| Gitleaks (`agent-gitleaks.sh`) | Every build | No (`soft_fail: true`) |
| Baltic replay golden (`agent-baltic-replay.sh`) | `main` only | Yes for that step. The same replay filter also runs inside `dotnet-ci.sh` on every build. |
| GitNexus PR analysis | Pull requests | No (`soft_fail: true`) |
| GitNexus reindex | `main` only | No (`soft_fail: true`) |

GitHub Actions workflows that run on their own:

| Workflow | Trigger | What it does |
| --- | --- | --- |
| [dotnet-ci.yml](.github/workflows/dotnet-ci.yml) | Pull requests, pushes to `main`, manual | Checks out Git LFS, then `bash tools/buildkite/dotnet-ci.sh` |
| [gauntlet-oracle.yml](.github/workflows/gauntlet-oracle.yml) | Pull requests, manual | Demo batch CSV, then fail-closed `gauntlet_oracle_eval` |
| [gauntlet-pytest.yml](.github/workflows/gauntlet-pytest.yml) | Pull requests, pushes to `main`, manual | `pytest tools/qa-gauntlet/` on Python 3.15 |
| [resharper.yml](.github/workflows/resharper.yml) | Pull requests, pushes to `main`, manual | ReSharper inspect on `windows-latest` |
| [actionlint.yml](.github/workflows/actionlint.yml) | Pull requests that touch workflows, manual | Lints workflow YAML |
| [gitnexus-security.yml](.github/workflows/gitnexus-security.yml) | Pull requests, manual | CodeQL jobs are `if: false`. Dependency review runs with `continue-on-error` |
| [unity-ci.yml](.github/workflows/unity-ci.yml) | Manual only | Editor Edit Mode or Play Mode tests at `6000.3.27f2` when Unity license secrets exist |
| GitNexus reindex, wiki, PR analysis, Graphite dismiss | Manual only | The Buildkite steps above are the ones that run on ordinary pushes. These workflow files do not trigger on pull requests. |

## Contributing

Trunk is `main`. Stacked pull requests in this repo go through Graphite (`gt create`, `gt submit`), documented in [graphite-github-substitute-plan.md](docs/engineering/graphite-github-substitute-plan.md). The review queue is [Graphite](https://app.graphite.com). The pull request body GitHub shows still uses [`.github/pull_request_template.md`](.github/pull_request_template.md).

Before you open a PR, run `bash tools/buildkite/dotnet-ci.sh` (or `tools/verify-ci-local.ps1` when `pwsh` is available). Definition of done for a PR, a sprint, and a release: [definition-of-done.md](docs/engineering/definition-of-done.md).

Invariants called out by the test floor, the CI script, and [`AGENTS.md`](AGENTS.md):

- The Baltic v2 hash in [`production/test-floor.json`](production/test-floor.json) stays `17144800277401907079` unless an ADR changes the golden.
- No tracked proprietary `*.db3` files. `dotnet-ci.sh` exits if `git ls-files '*.db3'` is non-empty.
- `CatalogWriteGate` stays extend-only, and `DelegationBridge` hotpath changes are not in scope for Release work. Read the hard-invariants section of `AGENTS.md` before editing either type.

Issue identifiers in sprint docs and the definition of done are Linear **DRG** issues. The project link used in-repo is the [cmano-clone Linear project](https://linear.app/drgamtd-workspace/project/cmano-clone-7f6a00e4c1c9), via [linear-parallel-dispatch-playbook.md](production/agentic/linear-parallel-dispatch-playbook.md).

Agents working in this checkout should start at [`AGENTS.md`](AGENTS.md) (headless commands, invariants, GitNexus, cloud setup). [`CLAUDE.md`](CLAUDE.md) is the Claude Code studio entry point. Where those files name Unity `6000.3.22f1` or an older test total, use `ProjectVersion.txt` and `dotnet test` output instead.

## Key documents

| Topic | Document |
| --- | --- |
| Agent guide | [`AGENTS.md`](AGENTS.md) |
| Engineering index | [`docs/engineering/README.md`](docs/engineering/README.md) |
| Architecture and ADRs | [`docs/architecture/architecture.md`](docs/architecture/architecture.md) |
| Requirements index | [`Game-Requirements/Game-Requirements-Index.md`](Game-Requirements/Game-Requirements-Index.md) |
| Design systems index | [`design/gdd/systems-index.md`](design/gdd/systems-index.md) |
| Determinism and replay | [`docs/engineering/determinism-and-replay.md`](docs/engineering/determinism-and-replay.md) |
| Scenario policies | [`docs/engineering/scenario-policy-authoring.md`](docs/engineering/scenario-policy-authoring.md) |
| Scenario documents | [`docs/engineering/scenario-document-authoring.md`](docs/engineering/scenario-document-authoring.md) |
| Mission Editor CLI | [`docs/engineering/mission-editor-cli.md`](docs/engineering/mission-editor-cli.md) |
| Capability gaps | [`docs/engineering/sim-capability-gap-backlog.md`](docs/engineering/sim-capability-gap-backlog.md) |
| Current roadmap alias | [`docs/reports/future-sprint-roadpmap.md`](docs/reports/future-sprint-roadpmap.md) |
| Tech stack notes | [`Tech-Stack.md`](Tech-Stack.md) (editor build: use `ProjectVersion.txt`) |
