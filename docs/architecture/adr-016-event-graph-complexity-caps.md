# ADR-016: Event-Graph Complexity Caps — Soft Warnings, Hard Per-Event Cap

## Status

**Accepted**

## Date

2026-07-01

## Last Verified

2026-07-01

## Decision Makers

Enterprise architect (DRGAMTD); milsim architecture review. Records a decision already made in the GDD (§4.3). Closes Open Question 4 of requirement [11-Agentic-Mission-Editor](../../Game-Requirements/requirements/11-Agentic-Mission-Editor.md).

## Summary

Large event graphs threaten the deterministic evaluation loop and frame budget, and the editor requirement asks for a maximum complexity before performance warnings. This ADR records the GDD-decided policy: **complexity and tick-density are soft warnings that never block export**, with a **hard cap of 32 conditions per event** (validation error above) as the only hard limit.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS / .NET 8 headless — validation is engine-free (`ProjectAegis.Data`/`.Sim`) |
| **Domain** | Core |
| **Knowledge Risk** | LOW — deterministic rule/tuning decision, no engine API surface |
| **References Consulted** | GDD `design/gdd/agentic-mission-editor.md` §4.3, §4.2, §7; requirement doc 11 §5, NFR |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | None |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | [ADR-008 Mission Editor Validation Engine](adr-008-mission-editor-validation-engine.md) (Accepted) |
| **Enables** | Event-graph perf budgeting against the 5,000-unit ORBAT NFR |
| **Blocks** | None |
| **Ordering Note** | Thresholds are tuning knobs (GDD §7); finalize the defaults during perf budgeting |

## Context

### Problem Statement

A pathological event graph — e.g. "400 events all firing at T+0", or a single event with hundreds of conditions — can stall the deterministic evaluation loop and blow the frame budget. The editor must warn authors without silently blocking legitimately large scenarios.

### Current State

The GDD §4.3 already defines the model: a `complexity` score, a `peak_tick_density` measure, soft warning thresholds, and a hard per-event condition cap. This ADR ratifies that resolution against Open Q4.

### Constraints

- Determinism: warnings must be pure functions of the canonical file (same file → same warnings); no LLM, no runtime state.
- Non-blocking: designers must be able to author and export large-but-valid graphs; only genuinely invalid input (a single over-cap event) is an error.
- Frame budget / NFR: doc 11 requires editing a 5,000-unit ORBAT without freeze and deterministic headless samples.

### Requirements

- Surface a soft warning when a graph is expensive by topology or by tick concentration.
- Enforce exactly one hard limit: per-event condition count.
- All thresholds are data-driven tuning knobs.

## Decision

Adopt the GDD §4.3 policy verbatim as the accepted architecture:

1. **Complexity score (soft warning):**
   `complexity = E + sum(conditions_per_event) + C * cross_refs`, warn if `complexity > WARN_THRESHOLD`.
2. **Peak tick density (soft warning):**
   `peak_tick_density = max over ticks of (events with trigger_time_resolved == that tick)`, warn if `> DENSITY_THRESHOLD`. This catches the "everything at T+0" case the topology score alone misses, protecting the deterministic evaluation loop (§4.2) and frame budget.
3. **Soft warnings never block export** (doc Open Q4). They inform; the export gate is unaffected.
4. **Hard cap: 32 conditions per event** — a **validation error** above this (GDD §4.3 table). This is the single hard limit, and it also prevents one event from exceeding `WARN_THRESHOLD` on its own.
5. **Tuning knobs** (GDD §7, `assets/data/editor/validation-config.json`):

| Knob | Range | Default |
|---|---|---|
| `WARN_THRESHOLD` (complexity) | 200–1000 | 400 |
| `DENSITY_THRESHOLD` (events/tick) | 10–50 | 20 |
| `C` cross-ref weight | 1–4 | 2 |
| `conditions_per_event` hard cap | fixed | 32 |

### Implementation Guidelines

Compute `complexity` and `peak_tick_density` in the deterministic Validation Engine over the canonical file. Emit soft findings at `warning` severity (do not block export at the default `error` severity floor, GDD §7). Emit the 32-condition breach at `error` severity. Confirm final `WARN_THRESHOLD`/`DENSITY_THRESHOLD` during perf budgeting against the 5,000-unit ORBAT NFR (GDD-review action item).

## Alternatives Considered

### Alternative 1: Hard cap on total event count / complexity (block export)

- **Description**: Refuse to export graphs above a complexity ceiling.
- **Pros**: Guarantees a bounded worst case.
- **Cons**: Blocks legitimately large scenarios; brittle; punishes power users.
- **Rejection Reason**: Contradicts the soft-cap intent of Open Q4; a warning plus the per-event hard cap is sufficient.

### Alternative 2: Topology-only complexity score (no tick density)

- **Description**: Warn on `complexity` alone.
- **Pros**: Simpler.
- **Cons**: Misses the "400 events at T+0" concentration case that most threatens the eval loop and frame budget.
- **Rejection Reason**: `peak_tick_density` is specifically needed to catch same-tick pathologies (GDD §4.3).

## Consequences

### Positive

- Authors get actionable performance feedback without losing the ability to ship large scenarios.
- Deterministic, reproducible warnings (pure function of the file).
- One clear hard limit (32 conditions/event) keeps individual events bounded.

### Negative

- Default thresholds are provisional until perf budgeting confirms them against the 5,000-unit ORBAT NFR.

### Neutral

- Warnings are advisory; teams can tune thresholds per project via config.

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|-----------|
| Default thresholds mis-tuned for real scenarios | Medium | Low | Data-driven knobs; confirm during perf budgeting (GDD-review action item) |
| Authors ignore soft warnings and hit frame issues | Low | Medium | Tick-density warning targets the worst case; thresholds tunable downward |

## Performance Implications

| Metric | Before | Expected After | Budget |
|--------|--------|---------------|--------|
| Warning computation (validation pass) | N/A | O(E + total conditions + cross_refs) per validate | Well within a background validation pass |

The check is a linear scan of the event graph in the (already-required) validation pass; it adds no runtime sim cost and exists precisely to protect the frame budget under the 5,000-unit ORBAT NFR.

## Migration Plan

No migration — this formalizes behavior already specified in the GDD. Existing/authored scenarios simply begin receiving soft warnings when above threshold and an error only if an event exceeds 32 conditions.

**Rollback plan**: Thresholds are config; raise them to effectively disable warnings without code change. The 32-condition hard cap stays for eval-loop safety.

## Validation Criteria

- [ ] A graph above `WARN_THRESHOLD` emits a soft warning and still exports.
- [ ] A graph with `peak_tick_density > DENSITY_THRESHOLD` emits a soft warning and still exports.
- [ ] An event with >32 conditions produces a blocking validation error.
- [ ] Warnings are byte-identical for the same canonical file across runs (determinism).
- [ ] Thresholds read from `assets/data/editor/validation-config.json`.

## GDD Requirements Addressed

| GDD Document | System | Requirement | How This ADR Satisfies It |
|-------------|--------|-------------|--------------------------|
| `design/gdd/agentic-mission-editor.md` | Agentic Mission Editor (11) | §4.3 "Event-graph complexity warning (soft cap)" — soft warnings never block; 32-condition hard cap; `peak_tick_density` | Ratifies the formula, the soft-warning policy, and the single hard cap as accepted architecture |
| `design/gdd/agentic-mission-editor.md` | Agentic Mission Editor (11) | §7 Tuning Knobs (`WARN_THRESHOLD`, `DENSITY_THRESHOLD`, `C`) | Records ranges/defaults as data-driven config |
| `Game-Requirements/requirements/11-Agentic-Mission-Editor.md` | Agentic Mission Editor (11) | Open Question 4; NFR "Edit 5,000+ unit ORBAT without UI freeze"; "same file + seed → identical event order" | Closes Q4 and ties the caps to the perf/determinism NFRs |

## Related

- Requirement [11-Agentic-Mission-Editor](../../Game-Requirements/requirements/11-Agentic-Mission-Editor.md) (Open Question 4 — closed)
- GDD [agentic-mission-editor](../../design/gdd/agentic-mission-editor.md) §4.3, §4.2, §7
- [ADR-008 Mission Editor Validation Engine](adr-008-mission-editor-validation-engine.md)
- [ADR-014 Lua Compatibility Scope](adr-014-lua-compatibility-scope.md) (typed DSL that these caps measure)
