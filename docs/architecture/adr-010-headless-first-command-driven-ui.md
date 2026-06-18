# ADR-010: Headless-First / Command-Driven UI Architecture

## Status

**Accepted**

## Date

2026-06-03

## Decision Makers

Architecture review 2026-06-03; Project Aegis UI, mission-editor, simulation, and tooling assessment.

## Summary

Project Aegis will remain **headless-first** at its deterministic core while adding Unity UI as a command-driven presentation client. `ProjectAegis.Data`, `ProjectAegis.Sim`, and `ProjectAegis.Delegation` stay pure C# assemblies with no `UnityEngine` dependency. Unity UI, editor surfaces, DOTS systems, and optional tools interact with those assemblies only through validated commands, DTOs, read-only projections, and adapter seams. Third-party inspector/serializer tooling such as Odin is rejected for core simulation, replay, scenario, and catalog data and postponed for editor-only convenience use.

## Context

ADR-001 created `ProjectAegis.Sim` as the world-truth assembly and kept delegation consuming snapshots and emitting orders. ADR-005 selected Unity DOTS/ECS for scalable world-state execution, while preserving pure C# rules where possible. ADR-006 established `ProjectAegis.Data` as a Unity-free catalog/scenario layer. ADR-007 showed the C2 map UI already binding read-only projections rather than owning game state. ADR-008 made mission-editor validation deterministic, headless, and independent of Unity.

Recent architecture review raised whether a headless-first structure becomes a liability once Project Aegis builds a full UI. The conclusion is that the UI should not replace headless architecture. Instead, the same command and projection contracts should support CLI, MCP, Unity runtime UI, Unity editor tools, and future agent/NL authoring flows.

The project has non-negotiable constraints:

- Same scenario file + same seed must reproduce simulation, validation, replay, and sample outputs.
- CI must validate scenario, data, policy, delegation, and replay behavior without opening Unity.
- Mission editor UI, MCP tools, and agent-generated edits must emit the same canonical scenario objects.
- UI state such as selection, panel layout, camera position, and layer toggles must never become authoritative simulation input.
- DOTS/ECS may execute world-state systems, but deterministic rules and domain contracts must remain testable outside the Unity player loop.

## Decision

### 1. Headless-first core remains authoritative

`ProjectAegis.Data`, `ProjectAegis.Sim`, and `ProjectAegis.Delegation` remain pure C# assemblies with **no `UnityEngine` dependency**.

These assemblies own:

- canonical catalog/scenario DTOs, immutable snapshots, and validation gates;
- deterministic simulation rules, policy/ROE/EMCON evaluation, sensors, engagement decisions, replay hashes, and order-log contracts;
- delegation controllers, autonomy gates, decision logs, and command/order contracts.

They must be runnable through `dotnet test`, CLI tools, MCP bindings, and replay harnesses without Unity Editor or Player.

### 2. UI is a client, not an authority

The Unity Presentation layer is a **decoupled client** over the headless core.

Unity UI may:

- render read-only projections and view models;
- maintain presentation-only state such as selection, focus, tabs, filters, camera, layer toggles, and panel layout;
- submit user intent as commands such as `IssueOrder`, `SetReferencePoint`, `AssignUnitToMission`, or `SetDoctrineOverride`;
- display validation findings, order-log projections, map symbols, contact summaries, and mission timelines.

Unity UI must not:

- mutate simulation, scenario, catalog, or delegation internals directly;
- store authoritative gameplay or scenario state in scene objects, `MonoBehaviour` fields, UI widgets, or `ScriptableObject` assets;
- make export/play decisions from cached UI validation state;
- read or write `DecisionLog`, catalog tables, replay state, or scenario packages except through approved services, adapters, and commands.

All authoritative UI actions must be expressible as deterministic commands that can also be driven by tests, CLI, MCP, or future agent authoring.

### 3. Read models and projections are the UI contract

Presentation surfaces consume read-only view models built from snapshots, order logs, scenario DTOs, and validation reports.

Examples include:

- `MapPictureProjection` / `MapSymbolEntry` for map display;
- sensor/contact projections for C2 panels;
- mission-list and timeline projections for editor/runtime mission views;
- unit-detail, doctrine inheritance, and validation-finding view models;
- message-log, replay, scoring, and AAR projections.

Projection DTOs are allowed to be rebuilt per tick or per edit. They do not participate in replay hashes unless explicitly defined as replay artifacts.

### 4. Unity-specific execution is isolated behind seams

The architecture distinguishes **engine-agnostic simulation rules** from **Unity-specific execution**.

| Area | Allowed dependencies | Responsibility |
|------|----------------------|----------------|
| `ProjectAegis.Data` | .NET only | Catalog, scenario packages, validation, deterministic read/write gates |
| `ProjectAegis.Sim` | .NET only | Rules, policy, sensors, engagement, replay/order-log semantics |
| `ProjectAegis.Delegation` | .NET only | Controllers, autonomy, decision pipeline |
| `ProjectAegis.Delegation.UnityAdapter` | .NET only unless explicitly revised | Snapshot/order seams such as `ISimWorldSnapshot` and `IOrderSink` |
| Unity DOTS / `ProjectAegis.Sim.Dots` / Unity project systems | Unity Entities/Burst/Jobs | ECS storage and execution, snapshot builders, order application |
| `ProjectAegis.Unity` | UnityEngine/UI Toolkit | UI, scene hosts, editor surfaces, input, map rendering |

DOTS/ECS can own high-performance runtime representation, but it must exchange data with the core through stable DTOs, snapshots, command queues, or adapter interfaces. Domain rules should be prototyped and tested in pure C# before being ported or mirrored into Burst/DOTS hot paths.

### 5. Odin and similar tooling are not core dependencies

Odin Inspector, Odin Serializer, and similar third-party Unity editor/serialization tools are **not approved** for core simulation, catalog, replay, scenario, mission-editor validation, or authoritative save/load paths.

Reasons:

- they are Unity-centric and conflict with the no-`UnityEngine` boundary of `Data`, `Sim`, and `Delegation`;
- Odin Serializer would add a third-party runtime serialization format inside determinism/replay boundaries;
- Inspector-driven authoring does not align with canonical scenario JSON, MCP parity, and headless validation;
- Odin's main strengths target `MonoBehaviour`/`ScriptableObject` workflows, while Project Aegis runtime scale depends on DOTS/ECS and pure DTO exports;
- paid package/version-locking and CI availability add avoidable onboarding and build risk.

Odin may be reconsidered later only for **editor-only convenience tooling**, debug windows, or non-authoritative authoring helpers, protected by assembly boundaries and compile guards. Any such adoption requires a separate ADR or explicit architecture review.

## Consequences

### Positive

- Preserves deterministic replay, golden-hash tests, and CI-first validation.
- Allows Unity UI, CLI, MCP, and future agent/NL tools to share the same command model.
- Prevents UI-only scenario drift and hidden scene-state authority.
- Keeps the simulation kernel insulated from Unity version churn, Asset Store package churn, and editor-only workflows.
- Enables parallel UI development through projections and binders without destabilizing sim/data/delegation logic.

### Negative

- Requires explicit command, service, projection, and adapter layers instead of direct UI-to-state edits.
- Creates mapping work between canonical DTOs, ECS components, UI view models, and scene objects.
- Some Unity inspector conveniences are unavailable for core data without wrapper assets.
- High-performance DOTS implementations may need duplicate or generated data layouts from pure C# domain models.

### Compliance

- New UI features must identify whether each state field is authoritative or presentation-only.
- New authoritative UI actions must route through command/application-service paths that are testable without Unity.
- New projections must be read-only and covered by headless tests where practical.
- New Unity/DOTS systems that mirror core rules must document their DTO/snapshot seam and replay/determinism impact.
- No third-party serializer may enter scenario, catalog, replay, order-log, or sim-state persistence without a new ADR.

## Alternatives Considered

| Alternative | Rejected because |
|-------------|------------------|
| Unity scene objects as authoritative state | Breaks headless CI, MCP parity, replay, and Git-diffable scenarios |
| UI directly mutates sim/data/delegation internals | Bypasses validation, policy, logging, and deterministic command ordering |
| Odin Serializer for save/load or scenario data | Adds non-project serialization semantics inside deterministic boundaries |
| ScriptableObjects as canonical scenario/database source | Conflicts with canonical JSON/packages, SQLite/catalog snapshots, and CI tooling |
| Full DOTS-only domain model | Makes policy, validation, replay, and CLI testing dependent on Unity packages/player loop |

## GDD Requirements Addressed

| GDD / Req | Requirement | How this ADR addresses it |
|-----------|-------------|---------------------------|
| Req 08 — Agentic Architecture | Modular deterministic sim, headless execution | Keeps core assemblies Unity-free and command-driven |
| Req 11 — Mission Editor | Same format for human, MCP, and agent authoring | UI emits the same canonical commands/objects as headless tools |
| Req 20 — Command & Control UI | C2 UI over sim/delegation state | UI consumes projections and submits validated commands |
| Order Log / Replay | Reproducible event/order stream | UI state does not affect replay hashes or authoritative logs |

## ADR Dependencies

| Relationship | ADR / artifact |
|--------------|----------------|
| **Depends on** | ADR-001, ADR-005, ADR-006, ADR-007, ADR-008 |
| **Enables** | Runtime C2 UI expansion, mission-editor UI, agentic scenario authoring, replay/AAR visualization |
| **Conflicts with** | Any design that makes Unity UI, Odin serialization, scene objects, or ScriptableObjects authoritative for sim/scenario state |

## Reference docs

- [Doctrine inheritance panel — developer reference](../engineering/doctrine-inheritance-panel.md) — a worked example of the read-only projection/binder pattern plus its separate write command (`DoctrineOverrideCommand`).