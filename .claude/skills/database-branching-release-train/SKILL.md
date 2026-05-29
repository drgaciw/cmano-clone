---
name: database-branching-release-train
description: "Plan and validate TL-gated database branches and release trains for ProjectAegis.Data: scenario DB binding, diffable drops, rollback, snapshot hashes, and separate data-versus-engine releases."
argument-hint: "[branch|release|scenario-binding|rollback]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Bash, Task, AskUserQuestion
model: sonnet
agent: database-architect
---

# Database Branching and Release Train

Use this skill to design or review database versioning, TL-0 through TL-5 branches,
scenario bindings, and release drops.

## Phase 1: Load Context

Read Requirement 06 sections on release trains, schema-aware editing, branching, and
resolved decisions. Read `docs/architecture/architecture.md` for Data and Simulation
responsibilities.

## Phase 2: Branch Model

Validate that the database model supports:

- Shared canonical IDs across TL branches.
- Branch-local interpreted/gameplay values.
- Temporal validity windows for platform variants.
- Diffing between branches and releases.
- Explicit merge/approval states.

## Phase 3: Release Train Model

Database releases must be:

- Versioned separately from engine releases.
- Diffable with generated "what changed" reports.
- Bound to scenarios by DB version and content hash.
- Reversible via rollback metadata.
- Re-indexable after approved changes.

## Phase 4: Simulation Compatibility

Each release must produce a deterministic snapshot manifest containing DB version,
TL branch, scenario binding, export schema version, and stable content hash.

## Output

Produce a release/branch checklist, risks, missing schema fields, and whether the
current release train is safe for scenario/replay binding.
