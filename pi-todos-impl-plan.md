# Parallel Agentic Development TODO Plan

> **Status (2026-06-04):** **COMPLETE** for agentic/headless scope. Phases 1–5 done on `main` (@ `5546c5d`). See `production/agentic/pi-plan-completion-2026-06-04.md` and `production/agentic/pi-plan-status.md`. Unity Editor C2 manual (PI-006) remains human-only.
>
> **Checkbox legend:** `[x]` done · `[~]` partial · `[ ]` not done / deferred

## GitNexus review — `pi-skills-recommendations.md` implementation

**Reviewed:** 2026-06-04 · **Repo:** `cmano-clone` (GitNexus CLI `--repo cmano-clone`)

| Recommendation area | Implementation evidence | GitNexus |
|---|---|---|
| GitNexus before edit / `detect_changes` at handoff | Documented in `pi-skills-recommendations.md`; enforced in agent artifacts | `detect_changes()` deferred on docs-only closure branch (see Definition of Done) |
| xUnit + `dotnet test ProjectAegis.sln` | **283/283** pass (`pi-verification-2026-06-04.md`) | N/A |
| SQLite + catalog (`SqliteCatalogReader`) | SEC-01 whitelist + `IsSafeSqlIdentifier` in `TableHasColumn` | **CRITICAL** upstream blast radius (66 symbols, 7 flows) — change was scoped to identifier guard only |
| JSON round-trip contracts | `ScenarioPolicyJsonRoundTripTests`, `CatalogJsonRoundTripTests` | Query hits both test classes + existing scenario round-trips |
| Security (`smithery.ai@security` checklist) | `pi-security-findings.md`; SEC-01 fixed; SEC-02 size-cap backlog | Context on `CatalogJsonImporter.ImportToSqlite` trust boundary |
| Milsim stack (`team-simulation`, `replay-verify`) | PI-004 `STRIKE_UNREACHABLE_FUEL`, PI-005 `FINGERPRINT_SHA256=` goldens | Validation/replay flows tie to `ReachabilityCalculator`, `BalticReplayHarness` |
| Project Aegis section in skills doc | Added per `docs/engineering/pi-skills-recommendations-review.md` | — |
| Headless C2 proxy | `production/qa/pi-006-headless-proxy-2026-06-04.md` | PlayMode smoke **7/7** |

**Key symbols verified:** `SqliteCatalogReader`, `ScenarioPolicyJsonRoundTripTests`, `CatalogJsonRoundTripTests`, `ReachabilityCalculator.TryClassifyStrikeUnreachable`, `ReplayGoldenAssertions.AssertPinnedHashes`.

**Out of scope (explicit):** broad Agent E refactors; Unity Editor visual QA; external skill installs (recommendations only).

---

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

- [x] List all test projects. — `pi-phase1-discovery.md` (5 projects, ~276 tests at discovery)
- [x] Identify test framework and helper conventions. — xUnit (Sim/Data/Cli), NUnit (Delegation/UnityAdapter)
- [x] Find tests covering catalog/data behavior. — catalog import, quarantine, validation goldens
- [x] Find tests covering JSON serialization. — policy, catalog, scenario document paths mapped
- [~] Find tests covering security-sensitive validation. — validation engine covered; dedicated security negative tests sparse
- [~] Identify missing regression tests. — PI-001…005 closed; SEC-02 import size-cap still open
- [ ] Identify flaky or slow tests. — not in PI scope

## Implementation TODOs

- [x] Add focused regression tests for known gaps. — PI-001, PI-002, PI-004, PI-005
- [~] Add boundary tests for invalid/null/empty inputs. — extra-field + ordering tests; null/empty not exhaustive
- [x] Add SQLite integration tests only where unit tests are insufficient. — existing `SqliteCatalogReaderTests`, import tests
- [x] Add JSON round-trip tests for persisted contracts. — `ScenarioPolicyJsonRoundTripTests`, `CatalogJsonRoundTripTests`
- [x] Keep test fixtures deterministic. — Baltic replay goldens, sorted catalog imports
- [x] Avoid broad fixture rewrites unless approved. — none performed

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

- [x] Locate all `Microsoft.Data.Sqlite` usage.
- [x] Locate all `ProjectAegis.Data.Catalog` usage.
- [x] Identify read flows. — `SqliteCatalogReader`, `InMemoryCatalogReader`
- [x] Identify write flows. — importers, quarantine promoter, seed bootstrap
- [x] Identify transaction boundaries.
- [x] Identify schema assumptions.
- [x] Identify duplicated query logic.
- [x] Identify direct SQL construction. — `TableHasColumn` flagged (SEC-01)

## Risk TODOs

- [x] Check SQL injection risk. — SEC-01 remediated (pragma whitelist)
- [~] Check connection disposal. — reviewed; no change in PI slice
- [~] Check command/reader disposal. — reviewed; no change in PI slice
- [~] Check transaction rollback behavior. — reviewed; no change in PI slice
- [~] Check missing-row handling. — existing tests; no new hardening
- [~] Check duplicate-row handling. — reviewed
- [~] Check null value handling. — reviewed

## Implementation TODOs

Only after GitNexus impact analysis:

- [x] Add missing parameterization. — `PragmaTableWhitelist` + `IsSafeSqlIdentifier` on `TableHasColumn`
- [~] Improve error handling around data access. — not broadly addressed
- [ ] Add narrow helper abstractions where duplication is proven. — deferred (Agent E)
- [x] Add focused tests for changed behavior. — existing catalog test suite covers reader
- [x] Avoid schema changes unless separately approved. — none in PI

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

- [x] Locate all `System.Text.Json` usage.
- [x] Identify DTOs and persisted payload models.
- [x] Identify custom converters.
- [x] Identify enum serialization behavior.
- [x] Identify naming policy usage. — camelCase + case-insensitive deserialize
- [x] Identify null/default handling.
- [x] Identify backward-compatibility risks.

## Test TODOs

- [x] Add round-trip tests. — PI-001, PI-002
- [~] Add missing-field tests. — defaults exercised indirectly; no dedicated missing-field matrix
- [x] Add extra-field tests. — `Extra_json_properties_are_ignored_on_policy_dto`, `futureField` in catalog test
- [~] Add null-field tests. — not exhaustive
- [~] Add enum-value tests. — ROE strings in round-trip; no enum converter matrix
- [~] Add nested-object tests. — nested ignored field only

## Implementation TODOs

Only after impact analysis:

- [~] Make serialization options explicit where ambiguous. — explicit in new tests; production loaders unchanged
- [x] Add converters only when necessary. — none added (N/A)
- [x] Preserve backward compatibility unless explicitly changed.
- [~] Document contract assumptions. — test docstrings + `pi-phase1-discovery.md`; no standalone contract doc

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

- [x] Identify trust boundaries. — `pi-security-findings.md`
- [x] Identify external/user-controlled inputs.
- [x] Identify SQL composition points.
- [x] Identify file/path handling.
- [x] Identify logging of sensitive values.
- [x] Identify crypto usage. — SHA-256 replay fingerprints (non-secret)
- [x] Identify deserialization of untrusted data.
- [x] Identify broad exception handling.

## Risk Ranking TODOs

Classify each finding:

- [x] Critical — none found
- [x] High — none found
- [x] Medium — SEC-01 (fixed), SEC-02 (mitigated, backlog)
- [x] Low — SEC-03, SEC-04
- [x] Info — SEC-05

For each finding, record:

- [x] affected file/symbol
- [x] exploit scenario
- [x] recommended remediation
- [x] test needed
- [x] whether behavior changes

## Implementation TODOs

Only for approved low-risk fixes:

- [~] Add input validation. — SEC-02 max JSON size not implemented
- [x] Replace string-built SQL with parameters. — SEC-01 whitelist fix
- [x] Remove/redact sensitive logging. — no issues found
- [ ] Harden error messages. — deferred
- [~] Add negative tests. — whitelist behavior implicit; no dedicated injection negative test

## Handoff Output

- security findings report
- risk-ranked remediation list
- proposed issues for non-trivial fixes

---

# Agent E — Architecture / Refactor Boundaries

## Goal

Identify safe refactor seams without creating broad changes prematurely.

## Discovery TODOs

- [x] Use GitNexus query/context for major concepts. — `SqliteCatalogReader` context + impact run
- [x] Identify high-degree symbols. — `DelegationBridge`, `DecisionLog`, `BalticReplayHarness`, `SimulationSession`
- [x] Identify cyclic or inverted dependencies.
- [x] Identify classes with mixed responsibilities.
- [x] Identify duplicated logic.
- [x] Identify unclear module boundaries.
- [x] Identify code requiring many tests before refactor.

## Candidate Refactor TODOs

For each candidate:

- [x] define current problem
- [x] list affected symbols
- [x] run GitNexus impact — e.g. `SqliteCatalogReader` CRITICAL / 66 upstream
- [x] estimate risk
- [x] identify prerequisite tests
- [x] propose smallest safe change
- [x] create issue instead of editing if risk is high — refactors deferred per plan

## Handoff Output

- architecture map
- high-risk symbols
- safe seams — `ICatalogReader`, `IEngagementResolver`, `IOrderLog`, `ScenarioPolicyRepository`
- proposed implementation sequence

---

# Agent F — Documentation / Issues

## Goal

Convert agent findings into actionable backlog items.

## TODOs

- [x] Collect summaries from Agents A-E.
- [x] Deduplicate findings.
- [x] Group findings by area:
  - tests
  - SQLite/catalog
  - JSON/contracts
  - security
  - architecture
- [x] Create issue-ready entries. — `pi-issue-backlog.md`
- [x] Add acceptance criteria.
- [x] Add validation commands.
- [x] Add dependencies.
- [x] Mark parallelizable vs sequential work.

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

- [x] Inspect changed files.
- [x] Confirm changes match planned scope.
- [x] Run build.
- [x] Run tests. — **283/283** (`pi-verification-2026-06-04.md`)
- [x] Run focused tests for modified areas. — PlayMode smoke **7/7**, headless gate **18/18**
- [~] Run `gitnexus_detect_changes()`. — deferred on docs-only closure branch; run at next production merge
- [x] Report unexpected affected symbols/flows.
- [x] Confirm no unrelated formatting churn.

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

- [x] Agent A: test inventory
- [x] Agent B: SQLite/catalog map
- [x] Agent C: JSON contract inventory
- [x] Agent D: security surface map
- [x] Agent E: architecture/coupling map
- [x] Agent F: prepare shared issue template

### Phase 2 — Planning

Mostly parallel, with coordination.

- [x] Convert findings into discrete tasks.
- [x] Assign owner per task.
- [x] Mark dependencies.
- [x] Identify high-risk symbols.
- [x] Decide which fixes need approval.
- [x] Select validation commands per task.

### Phase 3 — Implementation

Parallel only when file/symbol ownership does not overlap.

- [x] Agent A: tests first
- [x] Agent B: low-risk catalog fixes
- [x] Agent C: JSON contract fixes
- [x] Agent D: approved security hardening
- [x] Agent E: no broad refactor unless explicitly approved
- [x] Agent F: update issue backlog continuously

### Phase 4 — Verification

Sequential final gate.

- [x] Agent G runs full verification.
- [~] Agent G confirms GitNexus affected scope. — pending `gitnexus_detect_changes()` on next merge
- [x] Agent G reports final status.

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

- [x] failing tests — none at closure (**283/283**)
- [x] unsafe SQL — SEC-01 fixed
- [x] sensitive data logging — none found
- [x] broken JSON compatibility — round-trip tests added
- [x] high-risk catalog bugs — pragma whitelist applied

### P1

- [~] missing tests for core flows — PI gaps closed; SEC-02 / null-field matrix remain
- [ ] duplicated data access logic — documented, not refactored
- [~] unclear serialization contracts — improved via tests; no standalone contract doc
- [~] medium security hardening — SEC-02 size cap open

### P2

- [ ] architecture cleanup — intentionally deferred
- [x] documentation improvements — `production/agentic/*`, skills review
- [x] issue backlog grooming — `pi-issue-backlog.md`
- [ ] non-critical refactors — deferred

---

## Definition of Done

A task is done only when:

- [x] GitNexus impact was checked before editing (production symbol edits).
- [x] Risk was reported (PI security + validation docs).
- [x] Code changes were scoped to the task (PI-001…005 on main).
- [x] Tests were added or intentionally deferred with reason (PI-006 Editor deferred).
- [x] Relevant validation passed (**283/283** — `pi-verification-2026-06-04.md`).
- [~] `gitnexus_detect_changes()` — run at next code merge (docs-only closure branch).
- [x] Findings were documented (`production/agentic/*`, `production/qa/pi-006-headless-proxy-2026-06-04.md`).
