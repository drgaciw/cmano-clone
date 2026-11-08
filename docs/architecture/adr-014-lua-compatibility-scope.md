# ADR-014: Lua Compatibility Scope — Typed DSL First, Curated Shim Later

## Status

**Accepted**

## Date

2026-07-01

## Last Verified

2026-07-01

## Decision Makers

Enterprise architect (DRGAMTD); milsim architecture review. Records a decision already made in the GDD (§3.5). Resolves Open Question 2 of requirement [11-Agentic-Mission-Editor](../../Game-Requirements/requirements/11-Agentic-Mission-Editor.md).

## Summary

CMO scenarios script behavior with a `ScenEdit_*` Lua API, and the editor requirement asks whether Year 1 should ship a full Lua shim or a curated subset. This ADR records the GDD-decided answer: **v1 ships a typed, declarative event DSL and no Lua at all**; a *curated* `ScenEdit_*`-compatible subset (not a full shim) is a Phase 2/3 roadmap item.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS / .NET 8 headless — not engine-API-dependent |
| **Domain** | Scripting |
| **Knowledge Risk** | LOW — scoping decision; no dependency on a specific engine scripting API |
| **References Consulted** | GDD `design/gdd/agentic-mission-editor.md` §3.5, §3.7; requirement doc 11 §5 |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | None |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | [ADR-008 Mission Editor Validation Engine](adr-008-mission-editor-validation-engine.md) (Accepted) |
| **Enables** | Phase 2/3 curated `ScenEdit_*` compatibility layer |
| **Blocks** | None (v1 event system proceeds on the typed DSL) |
| **Ordering Note** | Any future Lua layer must compile to, or interoperate with, the same canonical event objects — it may not become a second source of truth |

## Context

### Problem Statement

Lua is a determinism and security hazard in a blocking path: arbitrary embedded scripts undermine reproducibility (the determinism pillar), resist exhaustive validation, and widen the attack surface (doc 11 NFR "no arbitrary code execution in play mode; script sandbox in editor"). The editor must decide its Year 1 scripting surface before the event system is built.

### Current State

The GDD already specifies a **typed declarative event system** — enumerated trigger, condition, and action types (§3.5) that compile to a deterministic evaluation order (§4.2) and are exhaustively checked by the Validation Engine (§3.6). There is no Lua in v1.

### Constraints

- Determinism: same file + seed → identical `fire_order` and world-state hash (GDD §4.2, AC-2). Arbitrary Lua cannot make this guarantee.
- Validation coverage: a deterministic engine can guarantee it catches all six v1 rules (§3.6); an LLM or opaque Lua block cannot.
- Security: no arbitrary code execution in play mode (doc 11 NFR).
- Parity: CMO authors expect `ScenEdit_*`; the requirement lists it as P0 "migrate toward typed event DSL + optional Lua compatibility shim".

### Requirements

- v1 event authoring is fully expressible in the typed DSL (Time / UnitDestroyed / UnitEntersZone / ContactDetected / Variable / MissionComplete / SidePostureChange / ScoreThreshold + unit-state triggers; actions per §3.5).
- No Lua interpreter ships in v1.
- A future Lua compatibility path, if built, is a *curated* subset that maps to canonical objects.

## Decision

1. **v1 ships a typed declarative event DSL and no Lua** (GDD §3.5). All authoring paths — map, NL, MCP — emit the same canonical trigger/condition/action objects.
2. **No full `ScenEdit_*` shim, ever, as an arbitrary-code layer.** A full shim would reintroduce the determinism, validation-coverage, and sandboxing problems the typed DSL exists to eliminate.
3. **A curated `ScenEdit_*`-compatible subset is a Phase 2/3 roadmap item** (doc 11 §5 "P1 Lua/DSL hybrid", Phase 3). If built, each supported call must **compile down to the same canonical event objects** and pass the same Validation Engine — it is a front-end convenience for CMO-familiar authors, not a parallel scripting runtime.

### Implementation Guidelines

Build the typed DSL as the single event model. When the curated subset is scoped, express it as a translation layer producing canonical `events[]` — never as an embedded interpreter with private state (this would violate the load-bearing invariant, GDD §3.3).

## Alternatives Considered

### Alternative 1: Full `ScenEdit_*` Lua shim in Year 1

- **Description**: Embed a Lua interpreter with the full CMO API surface.
- **Pros**: Maximum CMO script portability.
- **Cons**: Breaks determinism, defeats exhaustive validation, adds a sandbox/security burden, large implementation.
- **Rejection Reason**: Directly contradicts the determinism and validation pillars (GDD §3.6, §4.2).

### Alternative 2: Curated Lua subset in Year 1

- **Description**: Ship a small `ScenEdit_*` subset in v1 alongside the DSL.
- **Pros**: Some CMO familiarity earlier.
- **Cons**: Splits scripting effort during the foundation phase; two authoring models to validate at once.
- **Rejection Reason**: The GDD sequences the compatibility subset to Phase 2/3 so v1 can ship one trustworthy model first.

## Consequences

### Positive

- v1 event authoring is fully deterministic and exhaustively validatable.
- No script sandbox to build or secure in v1.
- Leaves a clean path for a curated, canonical-object-backed compatibility layer later.

### Negative

- CMO Lua scripts are not portable to v1 — authors re-express logic in the DSL.
- Highly bespoke Lua logic may not map cleanly even to the future curated subset (documented gaps, not silent loss).

### Neutral

- "Scripting" in Aegis means the typed DSL; "Lua" becomes a Phase 2/3 compatibility topic.

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|-----------|
| DSL cannot express a needed CMO behavior | Medium | Medium | Unit-state triggers added for parity (§3.5); track gaps for the curated subset |
| Future subset pressured toward arbitrary code | Low | High | Hard rule: subset compiles to canonical objects, passes the same gate |

## Performance Implications

N/A — the typed DSL compiles to a deterministic evaluation order (§4.2); no interpreter overhead is introduced. Event-graph cost is governed separately by [ADR-016](adr-016-event-graph-complexity-caps.md).

## Migration Plan

No migration in v1. A future curated subset is additive: it emits canonical `events[]` and requires no change to existing scenarios.

**Rollback plan**: N/A for v1 (no Lua to remove). A future subset can be withdrawn without affecting DSL-authored scenarios.

## Validation Criteria

- [ ] v1 ships no Lua interpreter in editor or runtime.
- [ ] All v1 event scenarios are authorable via the typed DSL and MCP suite (AC-5).
- [ ] Any future `ScenEdit_*` subset produces canonical `events[]` and passes the Validation Engine.

## GDD Requirements Addressed

| GDD Document | System | Requirement | How This ADR Satisfies It |
|-------------|--------|-------------|--------------------------|
| `design/gdd/agentic-mission-editor.md` | Agentic Mission Editor (11) | §3.5 "Event system (typed DSL, no Lua in v1)" | Records no-Lua-in-v1 and the typed DSL as the sole v1 event model |
| `design/gdd/agentic-mission-editor.md` | Agentic Mission Editor (11) | §3.6 exhaustive six-rule validation; §4.2 deterministic evaluation order | Justifies excluding arbitrary Lua from the blocking path |
| `Game-Requirements/requirements/11-Agentic-Mission-Editor.md` | Agentic Mission Editor (11) | §5 "migrate toward typed event DSL + optional Lua compatibility shim"; Phase 3 "Full event DSL + Lua shim" | Scopes the shim as a curated Phase 2/3 subset, not a full v1 shim |

## Related

- Requirement [11-Agentic-Mission-Editor](../../Game-Requirements/requirements/11-Agentic-Mission-Editor.md) (Open Question 2)
- GDD [agentic-mission-editor](../../design/gdd/agentic-mission-editor.md) §3.5
- [ADR-008 Mission Editor Validation Engine](adr-008-mission-editor-validation-engine.md)
- [ADR-016 Event-Graph Complexity Caps](adr-016-event-graph-complexity-caps.md)
