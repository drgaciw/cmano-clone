# ADR-017: Editor Topology — In-Client Front-End vs Standalone Scenario Lab

## Status

**Proposed**

## Date

2026-07-01

## Last Verified

2026-07-01

## Decision Makers

Enterprise architect (DRGAMTD); milsim architecture review. **Resolution owner: Technical Director.** **Target decision date: 2026-10-01.** Resolves Open Question 5 of requirement [11-Agentic-Mission-Editor](../../Game-Requirements/requirements/11-Agentic-Mission-Editor.md).

**Review date:** 2026-10-01 — Technical Director must decide before any standalone Scenario Lab epic is scheduled.

## Summary

The editor requirement asks whether authoring runs only inside the game client or also as a standalone "Scenario Lab" desktop app sharing the core library. This ADR proposes a **headless-first shared core** (`ProjectAegis.Data` + CLI/MCP, per ADR-010) as the single system of record, with the in-client Unity editor as the v1 surface and a possible standalone Scenario Lab as an **optional later front-end onto the same core — never a fork**.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS client; core logic in engine-free `ProjectAegis.Data` (.NET 8 / netstandard2.1) |
| **Domain** | Core |
| **Knowledge Risk** | LOW — architecture/topology decision; core is deliberately engine-free |
| **References Consulted** | ADR-010 headless-first; ADR-006 data-layer boundary; ADR-001 sim assembly boundary; GDD §3.1, §3.3; requirement doc 11 §1 |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | None |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | [ADR-010 Headless-First Command-Driven UI](adr-010-headless-first-command-driven-ui.md); [ADR-006 Data Layer Boundary](adr-006-data-layer-boundary.md) |
| **Enables** | An optional standalone Scenario Lab front-end reusing the same core |
| **Blocks** | Any "Scenario Lab" desktop-app epic — until this topology is Accepted |
| **Ordering Note** | The shared core and MCP/CLI ship first; UI front-ends (in-client, then optionally standalone) layer on top |

## Context

### Problem Statement

Committing prematurely to "in-client only" or to a separate desktop app risks either boxing scenario authoring inside the runtime or forking the codebase into two divergent editors. The topology must be decided so the core, MCP tools, and any UI can be sequenced without rework.

### Current State

The GDD already treats headless and UI as **the same code path with different front-ends** (§3.1: Play / Edit / Headless-edit modes all operate on the identical canonical file; §3.3 load-bearing invariant: no front-end holds private state read by sim/validation). ADR-010 establishes headless-first, command-driven UI; ADR-006 keeps `ProjectAegis.Data` engine-free. The core MCP suite (GDD §3.7) already makes authoring front-end-agnostic.

### Constraints

- ADR-010: headless-first — the command/MCP layer is the system of record, UIs are thin front-ends.
- ADR-006 / ADR-001: `ProjectAegis.Data`/`.Sim` must stay engine-free (no `UnityEngine`), so the core is reusable by a non-Unity host.
- Load-bearing invariant (GDD §3.3): all front-ends emit the same canonical objects; none may hold authoritative private state.
- Resourcing: one core team; a second full editor UI is expensive.

### Requirements

- One canonical core (`ProjectAegis.Data` + CLI/MCP) as the single source of truth.
- v1 authoring surface ships in the Unity client (Edit mode) plus headless CLI/MCP.
- Any standalone Scenario Lab is an additional front-end on the same core, not a parallel implementation.
- Headless CI and batch agent generation must run with no UI (GDD §3.1 headless-edit; AC-5).

## Decision

**Proposed.** Adopt a headless-first, shared-core topology, subject to Technical Director sign-off:

1. **Shared core is the system of record.** `ProjectAegis.Data` (engine-free, ADR-006) plus the CLI/MCP command layer (ADR-010) own the canonical `*.aegis-scenario` file, validation, and all mutations. This is where authoring "lives".
2. **In-client Unity editor is the v1 surface.** Edit mode (GDD §3.1) is a thin front-end over the core — it emits canonical objects, holds only derived-only `editorState` (§3.3), and adds no authoritative state.
3. **Standalone "Scenario Lab" is an optional later front-end onto the SAME core — not a fork.** If product demand justifies a desktop authoring app, it reuses the identical engine-free core and MCP/CLI; it is a UI shell, not a second editor. No duplicate validation, schema, or mutation logic.
4. **No topology that violates the invariant.** Any front-end (in-client or standalone) that would hold private state read by sim/validation is rejected (GDD §3.3).

### Architecture

```
                +-------------------------------+
                |  Shared engine-free core       |
                |  ProjectAegis.Data (ADR-006)   |
                |  canonical file · validation   |
                +-------------------------------+
                    ^            ^            ^
                    | CLI/MCP    | CLI/MCP    | CLI/MCP  (ADR-010 command layer)
        +-----------+      +-----+-----+      +----------------+
        | Headless  |      | In-client |      | Scenario Lab   |
        | CI / agent|      | Unity Edit|      | desktop app    |
        | (v1)      |      | mode (v1) |      | (optional, later)|
        +-----------+      +-----------+      +----------------+
```

### Implementation Guidelines

Keep all authoring logic in the engine-free core behind MCP/CLI. Ship the in-client Unity front-end and headless path for v1. Defer any Scenario Lab; if scoped, build it strictly as a new front-end binding to the same core — verify via the ADR-006 boundary test (no `UnityEngine` in `ProjectAegis.Data`) and the §3.3 schema lint.

## Alternatives Considered

### Alternative 1: In-client only

- **Description**: Authoring exists solely inside the Unity client.
- **Pros**: One surface to build; simplest v1.
- **Cons**: Couples authoring to the runtime; weaker headless/CI story if logic leaks into Unity.
- **Rejection Reason**: Violates headless-first (ADR-010) if logic lands in the client; the shared-core model already gives the in-client surface for free without foreclosing a standalone app.

### Alternative 2: Standalone Scenario Lab as a separate codebase

- **Description**: A dedicated desktop editor with its own implementation.
- **Pros**: Decoupled from the game client; tailored authoring UX.
- **Cons**: Forks validation/schema/mutation logic → divergence, double maintenance, determinism drift.
- **Rejection Reason**: A fork breaks the single-source-of-truth guarantee; a front-end onto the shared core delivers the same benefit safely.

## Consequences

### Positive

- One canonical core; every surface (headless, in-client, future Scenario Lab) stays consistent by construction.
- v1 gets in-client + headless authoring with no premature desktop-app investment.
- A Scenario Lab remains a low-risk, additive option later.

### Negative

- Discipline required to keep authoring logic out of the Unity front-end (enforced by ADR-006 boundary test).
- A standalone app, if built, still needs its own UI shell effort (just not core logic).

### Neutral

- "The editor" is really the core plus interchangeable front-ends, not a single app.

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|-----------|
| Authoring logic leaks into the Unity client | Medium | High | ADR-006 boundary test (no `UnityEngine` in `.Data`); §3.3 schema lint; code review |
| Future Scenario Lab drifts into a fork | Low | High | Mandate reuse of the same core + MCP/CLI; no duplicate validation/schema |
| Product never needs Scenario Lab | Medium | Low | It is optional; no v1 cost incurred |

## Performance Implications

N/A — a topology/architecture decision. Determinism and frame-budget properties come from the shared core and are unaffected by which front-end drives it.

## Migration Plan

No migration in v1 — the shared core + in-client + headless topology is the initial design. A future Scenario Lab binds to the existing core with no change to the canonical format or validation.

**Rollback plan**: If a Scenario Lab is built and later dropped, remove the front-end; the core, in-client editor, and headless path are unaffected.

## Validation Criteria

- [ ] Technical Director records an accept/reject decision by 2026-10-01.
- [ ] All authoring mutations/validation live in engine-free `ProjectAegis.Data` behind CLI/MCP (ADR-006 boundary test passes).
- [ ] v1 authors via both in-client Edit mode and headless MCP/CLI (AC-5, AC-8).
- [ ] Any Scenario Lab prototype reuses the same core with zero duplicated validation/schema logic.

## GDD Requirements Addressed

| GDD Document | System | Requirement | How This ADR Satisfies It |
|-------------|--------|-------------|--------------------------|
| `design/gdd/agentic-mission-editor.md` | Agentic Mission Editor (11) | §3.1 "Headless and UI are the same code path with different front-ends" | Formalizes the shared-core, multi-front-end topology |
| `design/gdd/agentic-mission-editor.md` | Agentic Mission Editor (11) | §3.3 load-bearing invariant (no front-end holds authoritative private state) | Rejects any topology where a front-end holds sim/validation-visible state |
| `Game-Requirements/requirements/11-Agentic-Mission-Editor.md` | Agentic Mission Editor (11) | Open Question 5; §1 editor modes; §8 "Editor is the human-facing surface; batch generator uses same file format" | Answers Q5: shared core, in-client v1, optional Scenario Lab front-end |

## Related

- Requirement [11-Agentic-Mission-Editor](../../Game-Requirements/requirements/11-Agentic-Mission-Editor.md) (Open Question 5)
- GDD [agentic-mission-editor](../../design/gdd/agentic-mission-editor.md) §3.1, §3.3
- [ADR-010 Headless-First Command-Driven UI](adr-010-headless-first-command-driven-ui.md)
- [ADR-006 Data Layer Boundary](adr-006-data-layer-boundary.md), [ADR-001 Sim Assembly Boundary](adr-001-sim-assembly-boundary.md)
