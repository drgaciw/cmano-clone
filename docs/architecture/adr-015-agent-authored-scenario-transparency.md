# ADR-015: Agent-Authored Scenario Transparency Labeling

## Status

**Proposed**

## Date

2026-07-01

## Last Verified

2026-07-01

## Decision Makers

Enterprise architect (DRGAMTD); milsim architecture review. **Resolution owner: Design.** **Target decision date: 2026-09-01.** Resolves Open Question 3 of requirement [11-Agentic-Mission-Editor](../../Game-Requirements/requirements/11-Agentic-Mission-Editor.md).

**Review date:** 2026-09-01 — Design must decide before any player-facing agent-authorship labels ship.

## Summary

Phase 2/3 introduces LLM authoring agents (Mission Planner, Red Force, Briefing Writer, Balance) that draft scenario content as reviewable proposals. This ADR proposes — leaning **yes** — that agent-authored or agent-assisted scenarios be **labeled for transparency** in multiplayer and briefing surfaces, backed by the provenance record the GDD already mandates (prompt, rationale, diff hash, approving id).

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS / .NET 8 headless — not engine-API-dependent |
| **Domain** | Core |
| **Knowledge Risk** | LOW — policy/UX transparency decision, no engine API surface |
| **References Consulted** | GDD `design/gdd/agentic-mission-editor.md` §3.6, §2, GDD-review action items; requirement doc 11 §6 |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | None |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | None (provenance record already specified in GDD §3.6) |
| **Enables** | Phase 2/3 authoring agents shipping with a transparency guarantee |
| **Blocks** | Multiplayer/briefing "authored-by" surfacing epic — until Accepted |
| **Ordering Note** | v1 has no authoring agents; labeling activates when Phase 2/3 agents ship |

## Context

### Problem Statement

When AI staff can draft ORBAT, missions, and triggers, players in multiplayer and readers of a briefing have a legitimate interest in knowing whether — and how much — a scenario was machine-authored. Deciding the transparency stance now lets the provenance schema and UI carry the labeling from the first agent release rather than retrofitting it.

### Current State

v1 ships **no** authoring agents (GDD §2 — "Agent" is reserved for Phase 2/3 LLM systems). The GDD already requires that **every agent edit records prompt, rationale, diff hash, and approving user/agent id** (§3.6, provenance, requirement §6). The GDD-review action items note Open Q3 recommends **yes**, stored in `metadata`/provenance. Nothing currently surfaces this to players.

### Constraints

- Provenance is already mandated (GDD §3.6) — labeling is a surfacing decision, not new tracking.
- Human authority: agents propose, humans approve (GDD §2, doc 11 §6) — a scenario may be *assisted* rather than fully machine-authored, so labeling must express degree/role, not a naive binary.
- Multiplayer fairness and research reproducibility (doc 11 NFR "Audit: full edit log per scenario") both benefit from transparency.
- Localization: any player-facing label string is externalized (doc 11 NFR).

### Requirements

- Scenarios record whether agents contributed and in what role (from existing provenance).
- Multiplayer and briefing surfaces can display an "agent-authored / agent-assisted" indicator.
- The indicator derives from the authoritative provenance record, not a hand-set flag.

## Decision

**Proposed, leaning yes.** Recommend that Project Aegis label agent-authored and agent-assisted scenarios for transparency, subject to Design sign-off:

1. **Derive labeling from existing provenance.** The label is computed from the GDD §3.6 provenance records (which agent, what edits, approving id) — no new authoritative store. A denormalized summary in `metadata` (e.g. an `authoringProvenance` block) may cache it for cheap display, consistent with the derived-only discipline (GDD §3.3: `metadata` summary must not become a validation/sim input).
2. **Express role and degree, not a binary.** Distinguish "agent-assisted" (human-driven with agent proposals accepted) from "agent-authored" (predominantly agent-drafted), because agents propose and humans approve.
3. **Surface in multiplayer and briefing.** Show a non-intrusive indicator in scenario listing/multiplayer lobby and (per Design) in the exported briefing.
4. **Tamper-evident.** Because the label derives from the append-only provenance/edit log, it cannot be silently stripped by editing a display flag.

### Implementation Guidelines

Activate only when Phase 2/3 agents ship. Compute the label from provenance at export/list time; keep the `metadata` summary derived and re-computable. Externalize label strings for localization.

## Alternatives Considered

### Alternative 1: No labeling

- **Description**: Track provenance internally but never surface AI authorship.
- **Pros**: Simplest; no UX work.
- **Cons**: Reduced multiplayer transparency and research reproducibility; provenance already exists, so the marginal cost of surfacing is low.
- **Rejection Reason**: Forgoes a cheap, defensible transparency win the GDD already recommends.

### Alternative 2: Binary "AI-generated" flag

- **Description**: A single boolean set at authoring time.
- **Pros**: Trivial to implement.
- **Cons**: Misrepresents human-in-the-loop assisted authoring; hand-set flags drift from reality and are easy to strip.
- **Rejection Reason**: Fails to capture degree/role and is not tamper-evident.

## Consequences

### Positive

- Transparent multiplayer and briefing provenance at low marginal cost (provenance already tracked).
- Tamper-evident via the append-only edit log.
- Supports research reproducibility and the audit NFR.

### Negative

- Requires a small provenance→label mapping and localized UI strings when agents ship.
- Must avoid a stigmatizing UX — "assisted" is common and expected.

### Neutral

- Labeling is inert until Phase 2/3; v1 scenarios are simply unlabeled (no agents).

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|-----------|
| Label misclassifies assisted vs authored | Medium | Low | Derive from per-edit provenance with a documented threshold; show role, not just a badge |
| Display flag diverges from provenance | Low | Medium | Compute from the append-only log; treat `metadata` summary as derived-only |
| Label perceived as stigma | Low | Low | Neutral wording; treat "assisted" as normal |

## Performance Implications

N/A — label computation is a cheap read over existing provenance at export/list time; no runtime sim cost.

## Migration Plan

No migration in v1. When Phase 2/3 agents ship, compute labels from provenance and surface them; pre-existing (human-only) scenarios carry no agent label.

**Rollback plan**: Hide the indicator; provenance records remain intact and re-surfaceable.

## Validation Criteria

- [ ] Design records an accept/reject decision by 2026-09-01.
- [ ] When agents ship, an agent-assisted scenario surfaces an accurate indicator in multiplayer listing and briefing.
- [ ] The indicator is derived from the provenance/edit log and cannot be set to "human-only" while provenance shows agent edits.
- [ ] Label strings are externalized for localization.

## GDD Requirements Addressed

| GDD Document | System | Requirement | How This ADR Satisfies It |
|-------------|--------|-------------|--------------------------|
| `design/gdd/agentic-mission-editor.md` | Agentic Mission Editor (11) | §3.6 "Every agent edit records: prompt, rationale, diff hash, approving user/agent id (provenance)" | Uses that provenance record as the authoritative basis for the label |
| `design/gdd/agentic-mission-editor.md` | Agentic Mission Editor (11) | GDD-review action item: "Confirm doc open Q3 … recommended yes, store in metadata/provenance" | Adopts the recommended yes and specifies derived, tamper-evident surfacing |
| `Game-Requirements/requirements/11-Agentic-Mission-Editor.md` | Agentic Mission Editor (11) | §6 human-in-the-loop provenance; NFR "Audit: full edit log per scenario" | Turns the audit log into a player-visible transparency signal |

## Related

- Requirement [11-Agentic-Mission-Editor](../../Game-Requirements/requirements/11-Agentic-Mission-Editor.md) (Open Question 3)
- GDD [agentic-mission-editor](../../design/gdd/agentic-mission-editor.md) §3.6
- [ADR-016 Event-Graph Complexity Caps](adr-016-event-graph-complexity-caps.md), [ADR-014 Lua Compatibility Scope](adr-014-lua-compatibility-scope.md) (companion editor ADRs)
