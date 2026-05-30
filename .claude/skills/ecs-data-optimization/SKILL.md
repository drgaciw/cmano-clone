---
name: ecs-data-optimization
description: "Design DOTS/ECS runtime data exports from ProjectAegis.Data for Unity 6.3 LTS: Burst-compatible blittable structs, cache-efficient SoA/AoS layout, BlobAssets, NativeContainers, and no per-tick SQLite reads."
argument-hint: "[system|data-model|snapshot]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Bash, Task, AskUserQuestion
model: sonnet
agent: sim-data-specialist
---

# ECS Data Optimization

Use this skill to design or review the runtime data shape exported from SQLite and
consumed by `ProjectAegis.Sim` or Unity DOTS/ECS.

## Phase 1: Load Architecture and Engine Context

Read:
- `docs/architecture/architecture.md` — Data, Simulation, Bridge, Presentation layers.
- `docs/engine-reference/unity/VERSION.md` — Unity 6.3 LTS DOTS package versions.
- Requirement 06 technical considerations: SQLite for dev, DOTS-friendly formats,
  clear API, provenance/audit.

## Phase 2: Classify Data by Access Pattern

Classify each dataset:

- **Authoring/query data** — citations, evidence, reviewer notes, rationale.
- **Load-time domain data** — validated platform/weapon/sensor/policy records.
- **Hot-path simulation data** — detection, engagement, logistics, policy lookup.
- **Presentation data** — display names, descriptions, UI filters.

Only hot-path simulation data must be Burst/blittable. Keep evidence and text out of
per-tick containers.

## Phase 3: Choose Layout

Use SoA when systems scan one or two fields across many entities. Use AoS when all
fields are consumed together for a small set. Prefer:

- `BlobAssetReference<T>` for immutable scenario-bound lookup tables.
- `NativeArray<T>` sorted by canonical ID for dense scans.
- `NativeParallelHashMap<TKey,TValue>` only when lookup is required; never depend on
  its iteration order for simulation outcomes.
- `FixedString` only at the boundary where text is unavoidable, not in hot loops.

## Phase 4: Burst Compatibility Checklist

Reject hot-path structs containing:

- `string`, `object`, `class`, `List<T>`, delegates, or managed arrays.
- Unbounded variable-length data outside BlobAssets or buffers.
- Culture-dependent parsing or display formatting.
- References to SQLite, repositories, services, or UnityEngine objects.

Require numeric canonical IDs, explicit units, fixed-size primitives, and stable
version/hash fields.

## Phase 5: Cache and Snapshot Strategy

The runtime path should be:

`SQLite → validated domain models → deterministic sorted snapshot → BlobAssets / NativeArrays → sim systems`

Validate that the snapshot records:

- DB release version and TL branch.
- Scenario DB binding.
- Content hash over sorted records.
- Export schema version.
- Replay/golden-baseline compatibility notes.

## Output

Produce a recommendation with:

- Data classification table.
- Proposed SoA/AoS/BlobAsset layout.
- Burst compatibility verdict.
- Determinism risks and required ordering keys.
- Follow-up work for `database-modeler` or `database-engineer`.
