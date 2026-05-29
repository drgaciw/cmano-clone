---
name: team-unity
description: "Orchestrate Unity engine and Unity-MCP work: scene/prefab changes, UI Toolkit, DOTS/ECS, Addressables, shaders/VFX, package settings, play-mode validation, and editor-side safety gates."
argument-hint: "[scene|prefab|ui-toolkit|dots|addressables|shaders|mcp|build-profile|performance|full-feature] [scope] [--review full|lean|solo]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Edit, Bash, Task, AskUserQuestion
model: sonnet
agent: unity-specialist
---

# Team Unity — Unity Engine / MCP Orchestration

Use this skill when work crosses Unity Editor assets, runtime presentation,
Unity-MCP operations, package configuration, or engine-specific implementation.
It coordinates Unity specialists while enforcing the architecture rule that Unity
Presentation and Adapter code do not leak into pure Data or Simulation assemblies.

## Phase 0: Resolve Mode and Review Level

Modes:

- `scene` — scene hierarchy, lighting, cameras, test scenes, play-mode setup.
- `prefab` — prefab structure, variants, component references, and serialization safety.
- `ui-toolkit` — UXML/USS, PanelSettings, runtime UI Toolkit, focus/navigation.
- `dots` — Entities, Burst, Jobs, NativeContainers, ECS world-state presentation.
- `addressables` — asset groups, labels, loading/unloading, memory and catalog strategy.
- `shaders` — Shader Graph, HLSL, VFX Graph, render-pipeline customization.
- `mcp` — Unity-MCP editor operations and real-time editor automation planning.
- `build-profile` — Unity packages, project settings, platforms, build profiles.
- `performance` — Unity Profiler, frame budget, GC, rendering, asset memory.
- `full-feature` — engine architecture → implementation → validation → QA handoff.
- No mode — infer from request; if ambiguous, ask one focused question.

Review mode:
1. If `--review [full|lean|solo]` is present, use it.
2. Else read `production/review-mode.txt` if present.
3. Else default to `lean`.

## Team Composition

- **unity-specialist** — Unity architecture lead and engine API authority.
- **unity-ui-specialist** — UI Toolkit, UXML/USS, UGUI exceptions, input/focus.
- **unity-dots-specialist** — DOTS/ECS, Jobs, Burst, NativeContainers.
- **unity-addressables-specialist** — asset groups, catalogs, async loading, memory lifecycle.
- **unity-shader-specialist** — Shader Graph, HLSL, VFX Graph, render pipeline.
- **technical-artist** — art-to-engine pipeline, import settings, visual optimization.
- **c-sharp-devops-engineer** — Unity batchmode, package/build automation, CI.
- **c-sharp-reviewer / c-sharp-test-engineer** — Unity C# review and test coverage.
- **qa-lead / qa-tester** — play-mode, smoke, and manual QA evidence.

## Phase 1: Load Required Context

Read only relevant context:

- `Tech-Stack.md`, `.mcp.json`, `.cursor/mcp.json`, Unity package manifests.
- `docs/engine-reference/unity/VERSION.md` and package-specific notes.
- `docs/architecture/architecture.md` layer rules.
- Unity project paths under `unity/ProjectAegis/**` and adapter code in scope.
- Existing scene, prefab, UXML, USS, shader, material, Addressables, or test assets.

Report the affected Unity subsystem, owning specialists, and required validation.

## Phase 2: Select Workflow Pipeline

### MCP / Editor Pipeline

1. Confirm Unity Editor activation and MCP connectivity are available.
2. Present planned scene/prefab/asset operations before execution.
3. Require explicit user approval before editor-side mutations.
4. Validate saved assets, references, and play-mode smoke where possible.

### UI Toolkit Pipeline

1. Coordinate with `/team-ui` and `ui-experience-lead` for UX and command-boundary gates.
2. Spawn `unity-ui-specialist` for UXML/USS, PanelSettings, focus, input, and performance.
3. Route UI C# controllers through `/team-csharp` review/test gates.

### DOTS Pipeline

1. Spawn `unity-dots-specialist` for ECS/Burst/Jobs architecture.
2. Coordinate with `/team-simulation` for deterministic sim boundaries.
3. Confirm no pure sim code references UnityEngine and no per-frame managed allocations.

### Addressables / Asset Pipeline

1. Spawn `unity-addressables-specialist` for groups, labels, catalogs, loading strategy.
2. Spawn `technical-artist` for import settings and asset budget review.
3. Run `/asset-audit` when assets or import settings are in scope.

### Shader / VFX Pipeline

1. Spawn `unity-shader-specialist` for shader/VFX architecture and render pipeline fit.
2. Spawn `performance-analyst` when frame budget or GPU cost is at risk.
3. Ensure visual effects do not become hidden gameplay-state owners.

### Build / Package Pipeline

1. Spawn `c-sharp-devops-engineer` for package, asmdef, batchmode, or CI changes.
2. Ask before package changes or build profile changes.
3. Validate with the smallest safe Unity or dotnet command available.

## Phase 3: Blocking Gates

Stop and ask before proceeding when a change affects:

- Unity-MCP scene/prefab/asset mutation.
- UPM package versions or ProjectSettings.
- Pure Sim/Data/Delegation dependency direction.
- Addressables catalogs, remote content, or memory strategy.
- DOTS/Burst hot paths or deterministic simulation behavior.
- Shader/render pipeline selection or platform quality tiers.

## Output

Produce a concise report:

- Mode and Unity subsystem.
- Agents/skills invoked or recommended.
- Files/assets likely affected.
- Architecture, performance, and asset risks.
- Validation plan and next approved action.
