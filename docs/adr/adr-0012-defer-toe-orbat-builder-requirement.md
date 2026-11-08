---
title: "ADR-0012: Defer Formal Requirements for Dedicated TO&E / ORBAT Builder/Editor"
status: "Accepted"
date: "2026-06-20"
authors: "Project Aegis architecture discussion (c-sharp-architect context, requirements analysis)"
tags: ["architecture", "requirements", "milsim", "orbat", "to&e", "mission-editor"]
supersedes: ""
superseded_by: ""
---

## Status

Accepted

## Context

The Project Aegis milsim requires robust support for defining force structures in scenarios. The current high-level ORBAT model is defined within the Agentic Mission Editor requirements (see Game-Requirements/requirements/11-Agentic-Mission-Editor.md):

- `orbat` (units, groups, bases, cargo)
- Map-based ORBAT editing and placement
- Assignment of units/groups to missions via `mission_assign_units`
- Validation for ORBAT feasibility (fuel, magazines, empty missions, etc.)
- Performance target: edit 5,000+ unit ORBAT

Equipment and platform details (sensors, mounts, loadouts, magazines, weapons) are managed separately through the Platform Editor and Database Intelligence layers (requirements/21-Platform-Editor.md and /06-Database-Intelligence.md). Units in ORBAT resolve to platform definitions from the catalog.

A clarification question was raised regarding whether the ORBAT supports real-world milsim TO&E (Table of Organization and Equipment) concepts, using the example of US Navy aircraft squadrons (e.g., VAW-113 as an organizational entity containing a specific number of E-2D aircraft with associated equipment and assignments). See conversation tracing the distinction between:
- Organizational hierarchy (squadron/group like VAW-113)
- Explicit equipment counts/assignments (e.g., "4x E-2D" in that squadron)
- Current model using per-unit instances (multiple units of same platformId grouped under a squadron name) + inheritance for loadouts/doctrine from catalog.

The design docs acknowledge P1 scope for "full procedural ORBAT generation" and Phase 2 enhancements (templates, mining/cargo, NL agents proposing ORBAT). MVP is scoped to basic map ORBAT, mission assignment, and validation within the existing units/groups model. No dedicated, standalone "TO&E builder/editor" tool or detailed requirements specification has been authored.

This decision registers the choice to defer authoring a comprehensive requirements document for such a builder/editor at this time, while preserving all known background, analysis, and options for future reference.

## Decision

Defer the generation of a dedicated, detailed requirements specification (or new epic/doc) for a full TO&E / enhanced ORBAT builder/editor. 

All background context from architecture discussions, the ORBAT data model, the squadron/equipment count example, distinctions from Platform Editor, current MVP scope in the Mission Editor, and P1/Phase 2 notes are captured in this ADR. Revisit the topic explicitly when:
- Playtesting and implementation of current ORBAT (units + groups) in the Mission Editor MVP yields concrete feedback.
- Roadmap Phase 2 ORBAT enhancements (templates, procedural generation) are prioritized.
- A specific need arises for reusable squadron-level TO&E templates with authorized quantities, personnel, and equipment assignments separate from scenario instances.

The decision prioritizes delivering and validating the locked MVP scope without expanding requirements prematurely.

## Consequences

### Positive
- **POS-001**: Maintains strict focus on current locked MVP deliverables (map ORBAT basics, mission assignment, validation agent, platform catalog round-trip) without scope creep into unvalidated TO&E modeling.
- **POS-002**: Provides a durable, searchable record of the TO&E/ORBAT discussion (including the specific VAW-113 + E-2D count example and current units/groups vs. quantity-based TO&E analysis) for future architects and requirement authors.
- **POS-003**: Enables empirical validation of the existing ORBAT model (via playtests and agent proposals) before committing to a more complex builder/editor requirements set.
- **POS-004**: Avoids over-specification risk; current design already supports basic squadron-like grouping via "groups" + multiple platform instances + catalog loadouts.

### Negative
- **NEG-001**: The gap between the current "per-unit + group" ORBAT and a formal, reusable military-style TO&E (with explicit authorized equipment counts, squadron tables, etc.) remains unaddressed in a dedicated requirements artifact until revisited.
- **NEG-002**: Risk of future inconsistency or rework if "squadron" modeling (group vs. counted unit bundle) is interpreted differently by scenario authors, AI agents, or simulation consumers.
- **NEG-003**: Defers any UI/UX, data model extensions, or validation rules specific to quantity-based equipment assignment and hierarchical TO&E templates.
- **NEG-004**: May require additional effort later to back-port lessons if the current ORBAT implementation reveals shortcomings in representing real-world unit structures.

## Alternatives Considered

### ALT-001: Immediately author full TO&E requirements

- **ALT-001**: **Description**: Expand or create new requirements (e.g., as extension to doc 11 or standalone) defining reusable squadron/unit templates, explicit quantity fields (e.g., "4x E-2D" with per-aircraft loadouts), hierarchical org charts, and a dedicated builder/editor surface separate from or layered on the Mission Editor map ORBAT.
- **ALT-001**: **Rejection Reason**: Would expand beyond the explicitly locked MVP scope for the Mission Editor and Platform Editor. Introduces risk of designing for unvalidated use cases before playtest feedback on current units/groups model. Conflicts with phased delivery (MVP basic ORBAT; Phase 2 templates/procedural).

### ALT-002: Declare current ORBAT sufficient and close the topic without formal ADR or deferral

- **ALT-002**: **Description**: Document in existing docs that "units + groups + catalog platforms" fully satisfies milsim TO&E needs (e.g., represent VAW-113 as group containing 4 E-2D units) and move on without capturing the discussion.
- **ALT-002**: **Rejection Reason**: Does not address the explicit clarification raised about squadron organizational structure and quantity-based assignment. Leaves future readers without the background analysis or options considered. Misses opportunity to make the gap visible for roadmap prioritization.

### ALT-003: Implement basic TO&E support in code without authoring formal requirements first

- **ALT-003**: **Description**: Extend the ORBAT schema and editor immediately with quantity fields or squadron templates based on informal understanding, then document after the fact.
- **ALT-003**: **Rejection Reason**: Violates the project's emphasis on documented requirements before implementation (traceability, validation, ADR discipline). Risks building the wrong model without the full context captured here. Defers the decision but without the benefit of a recorded analysis.

### ALT-004: Do nothing and ignore the TO&E/ORBAT distinction

- **ALT-004**: **Description**: Take no action on the query or gap.
- **ALT-004**: **Rejection Reason**: The question was raised in good faith during architecture/requirements review. Ignoring it would lose valuable context about real milsim expectations (squadron tables with aircraft counts) versus current design. This ADR exists precisely to prevent information loss while still deferring work.

## Implementation Notes

- **IMP-001**: When the requirement is eventually generated, use this ADR (plus the full text of the original TO&E/ORBAT discussion) as primary input. Extend the existing `orbat (units, groups, bases, cargo)` model rather than starting from scratch.
- **IMP-002**: Prioritize playtest feedback from MVP ORBAT usage (including agent-proposed ORBATs and map editing) before investing in builder/editor tooling or quantity-based schema changes.
- **IMP-003**: Consider lightweight additions (e.g., explicit `quantity` or `aircraftCount` on group/unit in Phase 2) if the current multi-unit approach proves insufficient for squadron representation.
- **IMP-004**: Any future TO&E work must maintain determinism, provenance through the write gate/catalog, and compatibility with the *.aegis-scenario format.
- **IMP-005**: Revisit in context of P1 procedural ORBAT generation and Phase 2 mission editor enhancements.

## References

- **REF-001**: Game-Requirements/requirements/11-Agentic-Mission-Editor.md (ORBAT model, data model, MVP scope, `mission_assign_units`, validation)
- **REF-002**: Game-Requirements/requirements/21-Platform-Editor.md and 06-Database-Intelligence.md (equipment/platform side of TO&E)
- **REF-003**: design/gdd/agentic-mission-editor.md and agentic-mission-editor-concept.md (orbat details, groups, validation rules, phased delivery)
- **REF-004**: Game-Requirements/requirements/08-Agentic-Architecture.md (scenario system including ORBAT)
- **REF-005**: Game-Requirements/requirements/07-Agentic-Infrastructure.md (P1 procedural ORBAT generation note)
- **REF-006**: Wikipedia example referenced: List of United States Navy aircraft squadrons (VAW-113 squadron with specific E-2D aircraft counts) – used to illustrate real-world milsim TO&E expectations
- **REF-007**: Related ADRs in docs/architecture/ (e.g., adr-008-mission-editor-validation-engine.md, adr-011-platform-editor-excel-roundtrip.md)
- **REF-008**: ADR S41-03 (structural debt context for related modeling discussions)

**This ADR registers the explicit decision to defer while preserving the complete background for future requirement generation.**