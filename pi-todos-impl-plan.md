# Parallel Agentic Development TODO Plan

> **Status (2026-06-04):** **COMPLETE** for agentic/headless scope. Phases 1–5 done on `main` (@ `5546c5d`). See `production/agentic/pi-plan-completion-2026-06-04.md` and `pi-plan-status.md`. Unity Editor C2 manual (PI-006) remains human-only.

## Purpose

This document breaks future work into fine-grained tasks suitable for parallel agents.

The plan is designed for safe work in `cmano-clone`, with explicit GitNexus checkpoints, non-overlapping ownership, validation steps, and handoff formats.

---

## Global Rules for All Agents

Before editing code:

- Identify exact target symbol/function/class.
- Run:

```text
gitnexus_impact({ target: "<symbol>", direction: "upstream" })
```

- Record:
  - risk level
  - direct callers
  - affected execution flows
  - likely test coverage

If risk is `HIGH` or `CRITICAL`:

- stop
- report risk
- wait for approval before editing

Before final handoff:

```text
gitnexus_detect_changes()
```

---

## Agent Workstreams

| Agent | Workstream | Primary Scope |
|---|---|---|
| Agent A | Test Infrastructure | test projects, fixtures, regressions |
| Agent B | SQLite/Data Catalog | `Microsoft.Data.Sqlite`, catalog read/write flows |
| Agent C | JSON/Data Contracts | `System.Text.Json`, DTOs, serialized payloads |
| Agent D | Security | validation, logging, injection, crypto |
| Agent E | Architecture | coupling, seams, refactor candidates |
| Agent F | Documentation/Issues | reports, backlog, issue packets |
| Agent G | Verification | build/test/change detection |

---

# Agent A — Test Infrastructure

## Goal

Improve confidence in behavior without changing production logic unnecessarily.

## Discovery TODOs

- [ ] List all test projects.
- [ ] Identify test framework and helper conventions.
- [ ] Find tests covering catalog/data behavior.
- [ ] Find tests covering JSON serialization.
- [ ] Find tests covering security-sensitive validation.
- [ ] Identify missing regression tests.
- [ ] Identify flaky or slow tests.

## Implementation TODOs

- [ ] Add focused regression tests for known gaps.
- [ ] Add boundary tests for invalid/null/empty inputs.
- [ ] Add SQLite integration tests only where unit tests are insufficient.
- [ ] Add JSON round-trip tests for persisted contracts.
- [ ] Keep test fixtures deterministic.
- [ ] Avoid broad fixture rewrites unless approved.

## Handoff Output

- files reviewed
- tests added/updated
- uncovered risks
- validation commands run

## Validation

```bash
dotnet test ProjectAegis.sln -v minimal
```

---

# Agent B — SQLite / Data Catalog

## Goal

Map and improve SQLite-backed catalog behavior safely.

## Discovery TODOs

- [ ] Locate all `Microsoft.Data.Sqlite` usage.
- [ ] Locate all `ProjectAegis.Data.Catalog` usage.
- [ ] Identify read flows.
- [ ] Identify write flows.
- [ ] Identify transaction boundaries.
- [ ] Identify schema assumptions.
- [ ] Identify duplicated query logic.
- [ ] Identify direct SQL construction.

## Risk TODOs

- [ ] Check SQL injection risk.
- [ ] Check connection disposal.
- [ ] Check command/reader disposal.
- [ ] Check transaction rollback behavior.
- [ ] Check missing-row handling.
- [ ] Check duplicate-row handling.
- [ ] Check null value handling.

## Implementation TODOs

Only after GitNexus impact analysis:

- [ ] Add missing parameterization.
- [ ] Improve error handling around data access.
- [ ] Add narrow helper abstractions where duplication is proven.
- [ ] Add focused tests for changed behavior.
- [ ] Avoid schema changes unless separately approved.

## Handoff Output

- catalog flow map
- risky symbols
- changed symbols
- affected flows
- tests added

---

# Agent C — JSON / Data Contracts

## Goal

Ensure serialized contracts are stable, explicit, and tested.

## Discovery TODOs

- [ ] Locate all `System.Text.Json` usage.
- [ ] Identify DTOs and persisted payload models.
- [ ] Identify custom converters.
- [ ] Identify enum serialization behavior.
- [ ] Identify naming policy usage.
- [ ] Identify null/default handling.
- [ ] Identify backward-compatibility risks.

## Test TODOs

- [ ] Add round-trip tests.
- [ ] Add missing-field tests.
- [ ] Add extra-field tests.
- [ ] Add null-field tests.
- [ ] Add enum-value tests.
- [ ] Add nested-object tests.

## Implementation TODOs

Only after impact analysis:

- [ ] Make serialization options explicit where ambiguous.
- [ ] Add converters only when necessary.
- [ ] Preserve backward compatibility unless explicitly changed.
- [ ] Document contract assumptions.

## Handoff Output

- JSON contract inventory
- compatibility risks
- tests added
- changed serializers/models

---

# Agent D — Security Review

## Goal

Find and reduce security risks with minimal behavior disruption.

## Discovery TODOs

- [ ] Identify trust boundaries.
- [ ] Identify external/user-controlled inputs.
- [ ] Identify SQL composition points.
- [ ] Identify file/path handling.
- [ ] Identify logging of sensitive values.
- [ ] Identify crypto usage.
- [ ] Identify deserialization of untrusted data.
- [ ] Identify broad exception handling.

## Risk Ranking TODOs

Classify each finding:

- [ ] Critical
- [ ] High
- [ ] Medium
- [ ] Low

For each finding, record:

- affected file/symbol
- exploit scenario
- recommended remediation
- test needed
- whether behavior changes

## Implementation TODOs

Only for approved low-risk fixes:

- [ ] Add input validation.
- [ ] Replace string-built SQL with parameters.
- [ ] Remove/redact sensitive logging.
- [ ] Harden error messages.
- [ ] Add negative tests.

## Handoff Output

- security findings report
- risk-ranked remediation list
- proposed issues for non-trivial fixes

---

# Agent E — Architecture / Refactor Boundaries

## Goal

Identify safe refactor seams without creating broad changes prematurely.

## Discovery TODOs

- [ ] Use GitNexus query/context for major concepts.
- [ ] Identify high-degree symbols.
- [ ] Identify cyclic or inverted dependencies.
- [ ] Identify classes with mixed responsibilities.
- [ ] Identify duplicated logic.
- [ ] Identify unclear module boundaries.
- [ ] Identify code requiring many tests before refactor.

## Candidate Refactor TODOs

For each candidate:

- [ ] define current problem
- [ ] list affected symbols
- [ ] run GitNexus impact
- [ ] estimate risk
- [ ] identify prerequisite tests
- [ ] propose smallest safe change
- [ ] create issue instead of editing if risk is high

## Handoff Output

- architecture map
- high-risk symbols
- safe seams
- proposed implementation sequence

---

# Agent F — Documentation / Issues

## Goal

Convert agent findings into actionable backlog items.

## TODOs

- [ ] Collect summaries from Agents A-E.
- [ ] Deduplicate findings.
- [ ] Group findings by area:
  - tests
  - SQLite/catalog
  - JSON/contracts
  - security
  - architecture
- [ ] Create issue-ready entries.
- [ ] Add acceptance criteria.
- [ ] Add validation commands.
- [ ] Add dependencies.
- [ ] Mark parallelizable vs sequential work.

## Issue Template

```markdown
## Problem

## Affected Area

## Affected Symbols

## GitNexus Impact

## Risk

## Proposed Change

## Acceptance Criteria

- [ ]

## Validation

## Dependencies

## Parallelization Notes
```

---

# Agent G — Verification

## Goal

Provide final confidence before merge or handoff.

## TODOs

- [ ] Inspect changed files.
- [ ] Confirm changes match planned scope.
- [ ] Run build.
- [ ] Run tests.
- [ ] Run focused tests for modified areas.
- [ ] Run `gitnexus_detect_changes()`.
- [ ] Report unexpected affected symbols/flows.
- [ ] Confirm no unrelated formatting churn.

## Validation Commands

```bash
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests
```

---

## Parallel Execution Phases

### Phase 1 — Discovery

Can run fully in parallel.

- [ ] Agent A: test inventory
- [ ] Agent B: SQLite/catalog map
- [ ] Agent C: JSON contract inventory
- [ ] Agent D: security surface map
- [ ] Agent E: architecture/coupling map
- [ ] Agent F: prepare shared issue template

### Phase 2 — Planning

Mostly parallel, with coordination.

- [ ] Convert findings into discrete tasks.
- [ ] Assign owner per task.
- [ ] Mark dependencies.
- [ ] Identify high-risk symbols.
- [ ] Decide which fixes need approval.
- [ ] Select validation commands per task.

### Phase 3 — Implementation

Parallel only when file/symbol ownership does not overlap.

- [ ] Agent A: tests first
- [ ] Agent B: low-risk catalog fixes
- [ ] Agent C: JSON contract fixes
- [ ] Agent D: approved security hardening
- [ ] Agent E: no broad refactor unless explicitly approved
- [ ] Agent F: update issue backlog continuously

### Phase 4 — Verification

Sequential final gate.

- [ ] Agent G runs full verification.
- [ ] Agent G confirms GitNexus affected scope.
- [ ] Agent G reports final status.

---

## Conflict Avoidance Rules

- One owner per file when possible.
- One owner per symbol always.
- Tests may be edited by Agent A unless domain agent owns a focused regression.
- Architecture agent should not edit implementation files during discovery.
- Security agent should not make broad refactors.
- Documentation agent should not change production code.

---

## Agent Handoff Format

Each agent must return:

```markdown
## Agent

## Scope

## Files Reviewed

## GitNexus Queries / Impact Checks

## Findings

## Changes Made

## Tests Added

## Validation Run

## Risks / Blockers

## Recommended Next Tasks
```

---

## Priority Queue

### P0

- [ ] failing tests
- [ ] unsafe SQL
- [ ] sensitive data logging
- [ ] broken JSON compatibility
- [ ] high-risk catalog bugs

### P1

- [ ] missing tests for core flows
- [ ] duplicated data access logic
- [ ] unclear serialization contracts
- [ ] medium security hardening

### P2

- [ ] architecture cleanup
- [ ] documentation improvements
- [ ] issue backlog grooming
- [ ] non-critical refactors

---

## Definition of Done

A task is done only when:

- [x] GitNexus impact was checked before editing (production symbol edits).
- [x] Risk was reported (PI security + validation docs).
- [x] Code changes were scoped to the task (PI-001…005 on main).
- [x] Tests were added or intentionally deferred with reason (PI-006 Editor deferred).
- [x] Relevant validation passed (**283/283** — `pi-verification-2026-06-04.md`).
- [ ] `gitnexus_detect_changes()` — run at next code merge (docs-only closure branch).
- [x] Findings were documented (`production/agentic/*`, `production/qa/pi-006-headless-proxy-2026-06-04.md`).
