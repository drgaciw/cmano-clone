---
name: sqlite-schema-management
description: "Validate and evolve ProjectAegis.Data SQLite schemas against Requirement 06: entity-to-table mapping, field-level provenance, audit columns, constraints, migrations, rollback, and deterministic read ordering."
argument-hint: "[schema-file|migration-dir|model-dir|audit]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Bash, Task, AskUserQuestion
model: sonnet
agent: database-architect
---

# SQLite Schema Management

Use this skill when designing or reviewing the SQLite development schema for
`ProjectAegis.Data`. It validates the schema against Requirement 06 and the master
architecture's Data layer constraints.

## Phase 1: Load Required Context

Read:
- `docs/architecture/architecture.md` — Data layer responsibility and no-Unity rule.
- `Game-Requirements/requirements/06-Database-Intelligence.md` — provenance,
  validation, branching, audit, and agent routing requirements.
- Existing `src/**/*.csproj`, `src/**/*.cs`, and migration/schema files if present.

State whether `ProjectAegis.Data` exists. If absent, produce a scaffold-ready schema
contract, not implementation code.

## Phase 2: Validate Entity-to-Table Mapping

For each C# persistence model or proposed entity:

| Check | Requirement |
|-------|-------------|
| Canonical ID | Immutable stable key present and not derived from display name |
| TL branch | Supports TL-0..TL-5 database branches where applicable |
| Temporal validity | `valid_from` / `valid_to` or equivalent variant window |
| Units | Numeric fields identify units and basis |
| Source separation | Values classified as source fact, interpreted value, or gameplay abstraction |
| Runtime export key | Has deterministic sort/export key for simulation snapshots |

Flag unmapped C# fields, unmapped DB columns, and any many-to-many relationship
without an explicit compatibility/constraint table.

## Phase 3: Enforce Provenance and Audit

Every content table must either include or link to provenance metadata:

- Source link/citation or evidence reference.
- Confidence score and confidence basis.
- Reviewer identity and revision date.
- Author actor type: human, agent, import, migration.
- Rationale/change reason.
- TRL and Technology Level gate where applicable.

Audit/change-log tables must capture: database version, transaction/batch ID, actor,
timestamp, previous value, new value, approval state, and rollback target.

## Phase 4: Validate Constraints

Check for constraints covering Requirement 06:

- Mount/sensor/date/magazine compatibility.
- Sensor vs. stealth and kill-chain compatibility hooks.
- Loadout and platform-generation incompatibilities.
- Scenario binding to a database version/release train.
- Human approval gate for large changes and out-of-tolerance normalization.

## Phase 5: Deterministic Query Rules

Any query feeding `ProjectAegis.Sim` must define a total ordering. Require explicit
`ORDER BY` using canonical IDs and variant/version keys. Reject reliance on rowid,
insertion order, or unordered in-memory post-processing.

## Output

Produce a report:

- **PASS** — schema satisfies Requirement 06 for current scope.
- **CONCERNS** — safe to prototype, but list gaps before production data entry.
- **FAIL** — missing audit/provenance/routing constraints or deterministic exports.

Include a table of missing columns/tables, affected requirement lines, and suggested
migration names. Do not edit schema files without explicit approval.
