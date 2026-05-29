---
name: deterministic-data-access
description: "Audit and enforce deterministic database and cache access for ProjectAegis.Data and ProjectAegis.Sim: stable ordering, no wall-clock reads, no unordered Dictionary iteration, seeded RNG only, and reproducible snapshot hashes."
argument-hint: "[data|sim|cache|full]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Bash, Task, AskUserQuestion
model: sonnet
agent: sim-data-specialist
---

# Deterministic Data Access

Use this skill before merging any Data/Sim change that can affect replay output,
snapshot construction, policy lookup, engagement resolution, logistics, or caches.

## Phase 1: Establish Deterministic Boundary

Read `docs/architecture/architecture.md` and classify:

- Deterministic: `ProjectAegis.Data` snapshot export, `ProjectAegis.Sim`, policy,
  engagement, sensors, logistics, and delegation decision paths.
- Non-deterministic allowed only off-path: tooling UI, audit display formatting,
  editor reports, and human-facing diagnostics.

## Phase 2: Forbidden Pattern Scan

Scan in-scope files for:

- Wall-clock reads: `DateTime.Now`, `DateTime.UtcNow`, `DateTime.Today`,
  `Stopwatch`, `Environment.TickCount`, Unity `Time.*` in sim/data paths.
- Unseeded/random identity: `new Random()`, `Guid.NewGuid()`, `UnityEngine.Random`,
  `Random.Range`, `Random.value`.
- Unordered iteration: `Dictionary<`, `HashSet<`, `.Keys`, `.Values`, `foreach`
  over maps/sets, `GroupBy`, `Distinct`, `ToLookup` without stable ordering.
- Query instability: SQL queries feeding simulation without explicit `ORDER BY`.
- Culture-sensitive parsing/formatting: `float.Parse`, `double.Parse`, `ToString()`
  on persisted numeric values without invariant culture.

## Phase 3: Required Patterns

- Use `SeededRng` for all simulation randomness.
- Use `DeterministicHash` for stable IDs, sub-stream salts, and snapshot hashes.
- Sort by canonical ID plus version/variant key before exporting runtime snapshots.
- Convert unordered collections to sorted lists before any result-affecting loop.
- Inject an audit clock for metadata; never let audit timestamps affect simulation.

## Phase 4: Data Snapshot Gate

Every snapshot that can feed replay must include:

- Database release/version.
- TL branch.
- Scenario binding ID.
- Export schema version.
- Stable content hash over sorted records.

The same scenario and seed must load identical snapshot data on every machine.

## Output

Produce a PASS/CONCERNS/FAIL report:

- **PASS** — no deterministic data access blockers.
- **CONCERNS** — non-blocking risks, document before merge.
- **FAIL** — wall-clock, unseeded RNG, unordered result-affecting iteration, or
  unstable simulation SQL found.

List each finding with file:line, pattern, why it can diverge, and remediation.
