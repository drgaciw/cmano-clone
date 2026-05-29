---
name: database-layer-architecture
description: "Architecture workflow for introducing ProjectAegis.Data: assembly boundaries, repository APIs, write-routing gates, SQLite portability, dependency direction, and integration with Simulation/Delegation layers."
argument-hint: "[full|api|assembly|routing]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Bash, Task, AskUserQuestion
model: sonnet
agent: database-architect
---

# Database Layer Architecture

Use this skill before implementing `ProjectAegis.Data` or changing the Data/Sim
boundary.

## Phase 1: Load Context

Read:
- `docs/architecture/architecture.md` lines covering Data, Simulation, tick pipeline,
  interfaces, and layer rules.
- Requirement 06 for provenance, audit, validation, release trains, and agent routing.
- Existing solution/project layout and any ADRs affecting data, determinism, policy,
  order log, or DOTS core.

## Phase 2: Define Assembly Boundary

Propose:

- `ProjectAegis.Data` namespaces and public contracts.
- References allowed: Data may expose domain DTOs to Sim, but must not reference
  UnityEngine or Presentation.
- Test assembly strategy for migration, repository, validation, and snapshot tests.
- Dependency direction and anti-cycle checks.

## Phase 3: Define Write Routing

All writes must flow through an application service that performs:

1. Schema validation.
2. Cross-system constraint checks.
3. Provenance/audit capture.
4. Human approval state transition.
5. Transactional commit and rollback point creation.
6. Re-index/relationship refresh scheduling.

Reject designs that let agents, importers, or UI write tables directly.

## Phase 4: Define Read Paths

Separate:

- Authoring reads: rich evidence and audit queries.
- Simulation reads: deterministic snapshots with stable ordering.
- UI reads: read-only projections; no direct sim mutation.

Require explicit API boundaries for each.

## Output

Produce an architecture sketch with assembly diagram, repository/service names,
write-routing sequence, read snapshot sequence, open decisions, and whether an ADR
is required before implementation.
