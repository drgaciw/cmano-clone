# 06 - Database Intelligence Layer

**Last Updated:** 2026-06-04  
**Related:** [05-Dynamic-Systems-Agent.md](05-Dynamic-Systems-Agent.md) · [09-Near-Future-Technologies.md](09-Near-Future-Technologies.md) · [10-Speculative-Systems.md](10-Speculative-Systems.md) · [11-Agentic-Mission-Editor.md](11-Agentic-Mission-Editor.md) · [15-Sensor-Detection-And-EW.md](15-Sensor-Detection-And-EW.md) · [16-Logistics-And-Magazines.md](16-Logistics-And-Magazines.md) · [18-Combat-Domains.md](18-Combat-Domains.md)  
**Status:** Locked  
**Locked spec:** [Database Intelligence P0 design](../../docs/superpowers/specs/2026-05-30-database-intelligence-p0-design.md)  
**Research basis:** [Agentic CMO Research](../../docs/research/agentic-cmano-research.md) (database workflow), [Near-Future Tech Research](../../docs/research/near-future-tech-research.md), [Speculative Systems Research](../../docs/research/speculative-systems-research.md)

## Purpose

Provide a robust, agent-driven layer that maintains the integrity, consistency, provenance, and balance of the entire game database (units, weapons, sensors, platforms, and speculative systems) as new content is added or existing data is modified.

## Vision

A self-maintaining, auditable database treated as a **first-class product** — not support files hidden behind the executable. The layer detects inconsistencies, normalizes values, validates cross-system relationships, tracks provenance per field, and keeps the simulation balanced even as hundreds of new systems are proposed by agents. Military data is noisy and contradictory; agents **propose, never auto-merge**.

**P0 slice (locked):** `ProjectAegis.Data` assembly with SQLite catalog, snapshot versioning, staged write gate, read API, and scenario `dbSnapshotId` binding — deterministic services only (no LLM in the commit loop). Full “five-agent” autonomy, balance drift, TL branching, and cloud sync are **sequenced after** mission editor and sim can bind to versioned catalog. See locked P0 spec.

## Dual-Track Content Pipeline *(from CMO public DB workflow)*

Modeled on the observable Command database request process:

| Track | Purpose | Access |
|-------|---------|--------|
| **Public intake** | Community evidence-backed requests (new units, corrections, doctrine) | Issue-only public channel; templates; no direct DB edit |
| **Internal curation** | Coordinated work, overhauls, non-public priorities | Private queue with triage states |

**Triage states:** `accepted`, `needs_evidence`, `deferred`, `merged_into_overhaul`, `blocked`, `rejected`

Handling is **not FIFO** — driven by broader database objectives and **release trains**.

### Release trains

- Database drops ship **separate from engine releases** so data can update more frequently than code.
- Each drop is versioned, diffable, and tied to scenario DB binding (req 11).
- **P0:** `db_release` rows (`ReleaseVersion` → `SnapshotId` → `SchemaVersion`) via `DbSnapshotStore.GetSortedReleases()`; immutable `snapshotId` (e.g. `db-20260530-001`) per commit.
- **Post-P0:** diff reports between release versions; shallow/deep scenario rebuild (P0 spec §3.3) — deep auto-fix deferred.

## Functional Requirements

### 1. Automated Re-indexing Agent

- Monitors all changes to the database (new systems, stat edits, deletions)
- Automatically updates search indexes, relationship graphs, and dependency maps
- Ensures fast lookup for simulation systems and AI decision engines

**P0 note:** No separate “re-indexing agent” process — **materialized SQLite views and sorted export caches** rebuild on successful write-gate commit (`SqliteCatalogReader` invalidates cache on open; commit path triggers deterministic reload).

**Acceptance**

- [ ] **DBI-1.1** After `CatalogWriteGate.ApproveBatch` commits, subsequent `ICatalogReader.GetSortedSensorBindings()` reflects new rows in stable `(platformId, sensorId)` order.
- [ ] **DBI-1.2** Catalog exports and agent runs use fixed sort keys — no `DateTime.Now` in commit or export paths (`ICatalogClock` injectable).
- [ ] **DBI-1.3** `DatabaseIntelligenceOrchestrator.Run` completes on Baltic default catalog without manual re-index step.
- [ ] **DBI-1.4** Relationship lookups (`CatalogEntityMap`, detection keys) remain consistent after batch commit (no orphan staging rows).
- [ ] **DBI-1.5** Post-P0: dedicated dependency graph index for weapon→mount→sensor chains (tracked in P1 plan).

### 2. Consistency & Normalization Agent

- Enforces consistent units and scales across all systems (e.g., all ranges in nautical miles, all speeds in knots or Mach)
- Normalizes cost, signature, and performance values relative to era and technology level
- Flags outliers that deviate significantly from established norms

**P0 note:** **Detect-only** — emit `ValidationFinding` codes for unit mismatches and sanity bounds; **no auto-apply** of normalized values. `CatalogConsistencyAgent` reports findings; human or gated commit applies changes.

**Acceptance**

- [ ] **DBI-2.1** Validation pipeline flags range/speed unit enums outside allowed sets (nm, knots, Mach) with explainable finding codes.
- [ ] **DBI-2.2** Sanity rules reject or flag commits where max range ≤ 0 or Mach > 25 (configurable `CatalogValidationDefaults`).
- [ ] **DBI-2.3** Outlier detection produces findings without mutating source rows until approved batch commit.
- [ ] **DBI-2.4** P0 commits affecting **> 10 records** or any `balanceCritical` field require explicit human `ApproveBatch` (no silent bulk normalize).
- [ ] **DBI-2.5** Post-P0: auto-normalize **within tolerance bands** only (see Resolved Design Decisions §2); values outside band remain human-gated.

### 3. Cross-System Validation Agent

- Checks logical relationships between related systems (e.g., a missile’s range must be compatible with its launch platform’s sensors)
- Validates sensor vs. stealth interactions
- Detects impossible combinations (e.g., a subsonic aircraft with Mach 5 missile)
- Prevents broken kill chains or illogical detection probabilities

**P0 note:** Referential integrity (weapon→platform loadouts, sensor mounts) + range sanity. Cross-system “impossible combo” rule pack → **P1**.

**Acceptance**

- [ ] **DBI-3.1** `CatalogRulesValidationAgent` fails or warns when sensor bindings reference unknown `platformId` / `sensorId`.
- [ ] **DBI-3.2** Scenario load fails with resolve error when `dbRef` / catalog ID missing from bound snapshot (`ICatalogReader.TryResolveDbRef`).
- [ ] **DBI-3.3** `ScenarioValidationEngine` produces deterministic `ValidationReport` for Baltic patrol fixture (golden hash stable).
- [ ] **DBI-3.4** Quarantined bindings (`CatalogQuarantinePromoter`, `sensor_quarantine` table) never appear in sim export until promoted.
- [ ] **DBI-3.5** Post-P0: P1 rule pack for kill-chain impossibilities (doc 06 §3 full vision).

### 4. Version Control & Change Tracking

- Maintains full history of every change with timestamps, author (human or agent), and rationale
- Supports rollback to previous database versions
- Generates “what changed” reports after every batch of updates

**P0 note:** Append-only `CatalogChangeLogEntry` per field patch; `DbSnapshotStore` lists immutable snapshots; scenario packages bind `dbSnapshotId` / `dbRef` (doc 11). Rollback = load prior snapshot ID (not in-place undo).

**Acceptance**

- [ ] **DBI-4.1** Every approved batch writes change-log rows with `actorType`, `actorId`, `rationale`, `ApprovalState`, `RevisedUtcTicks`.
- [ ] **DBI-4.2** `GetSortedSnapshotIds()` returns monotonic, immutable snapshot IDs; scenario JSON `dbRef` resolves via `TryResolveDbRef`.
- [ ] **DBI-4.3** `DbReleaseRecord` links `ReleaseVersion` to `SnapshotId` for release-train drops.
- [ ] **DBI-4.4** Rejecting a staging batch (`RejectBatch`) leaves live tables unchanged and marks batch discarded.
- [ ] **DBI-4.5** Post-P0: generated diff report between two `ReleaseVersion` values for curators.

### 5. Balance Drift Detection

- Continuously monitors win rates and engagement statistics from agent-vs-agent runs
- Flags systems that have become over- or under-powered over time
- Works closely with the Balance Tuning Agent to propose corrections

**P0 note:** **Out of scope** for P0 implementation — reserve `IBalanceTelemetrySink` no-op hook. Full ±8% threshold applies post-P0 (Resolved Design Decisions §3).

**Acceptance**

- [ ] **DBI-5.1** P0 catalog commit path does not auto-adjust stats from sim telemetry.
- [ ] **DBI-5.2** Balance-related findings are advisory only; never bypass write gate.
- [ ] **DBI-5.3** Post-P0: flag when platform/weapon win-rate delta exceeds **±8%** over **≥ 500** agent-vs-agent runs (tuneable).
- [ ] **DBI-5.4** Post-P0: balance proposals enter staging as patch bundles, same human approval as doc 05.

### 6. Provenance & Evidence Model *(new)*

Every database field must support optional provenance metadata:

- Source link or citation (manual, OSINT, program doc)
- Confidence score (agent-assigned, human-overridable)
- Reviewer identity and revision date
- TRL and Technology Level gate (docs 09/10)

**Data separation:**

- `source_facts` — directly cited values
- `interpreted_values` — analyst/agent inference
- `gameplay_abstractions` — simplified sim parameters

**P0 implementation:** `CatalogProvenanceTier` constants; `CatalogSensorBinding` carries `ValueTier`, `Confidence`, `CitationRef`, `ReviewerId`, `RevisedUtcTicks`, `TrlLevel`, `ReviewState`.

**Acceptance**

- [ ] **DBI-6.1** Persisted sensor rows store `ValueTier` ∈ {`source_fact`, `interpreted_value`, `gameplay_abstraction`}; unknown tiers normalize to `gameplay_abstraction`.
- [ ] **DBI-6.2** Import path (`CatalogJsonImporter`) preserves provenance fields from JSON fixtures.
- [ ] **DBI-6.3** `ReviewState` ∈ {`approved`, `provisional`, `rejected`} (`CatalogReviewStates`); rejected rows do not export to sim.
- [ ] **DBI-6.4** TRL and TL routing metadata present on bindings used by docs 09/10 gates (`CatalogArchetypeGate`, `NearFutureArchetypeCatalog`).
- [ ] **DBI-6.5** Agent-proposed values from doc 05 remain `interpreted_value` or `gameplay_abstraction` until human reviewer sets `approved`.

### 7. Schema-Aware Editing & Constraint Rules *(new)*

- Prevent invalid mount/sensor/date/magazine combinations at edit time
- Temporal validity windows for capabilities and variants (canonical immutable IDs)
- Constraint rules for incompatible loadouts and platform generations
- Branching support: separate DB snapshots for TL-0 through TL-5 (doc 10)

**P0 note:** Write-time validation via `IWriteGate` + `CatalogRulesValidationAgent`; **TL branching deferred** — single `main` catalog with tagged snapshots; `branch` column reserved on snapshot metadata (P0 spec §4).

**Acceptance**

- [ ] **DBI-7.1** `ProposeSensorBatch` rejects empty batches; staging schema enforced before approve.
- [ ] **DBI-7.2** Approve path runs validation agents; failed validation returns `WriteGateDecision.Committed == false` with errors.
- [ ] **DBI-7.3** Canonical `PlatformId` / `SensorId` stable across releases; aliases resolved by `CatalogEntityResolutionAgent`.
- [ ] **DBI-7.4** `ScenarioEditVersionGuard` prevents concurrent scenario edits against stale catalog binding.
- [ ] **DBI-7.5** Post-P0: TL-0–TL-5 branch snapshots selectable per scenario/mode (doc 10 `BLACK_PROJECT_MODE` at TL-5).

### 8. Database Research Agent Workflow *(new)*

Agent pipeline for content updates — all outputs require human approval:

1. **Retrieval agent** — gathers articles, manuals, procurement notices, prior issue history
2. **Entity resolution agent** — maps aliases to canonical platform IDs
3. **Diff agent** — proposes additions/edits against current DB state
4. **Rules agent** — schema and temporal consistency checks
5. **Human reviewer** — approves or rejects patch bundle

Posture: **propose, not auto-merge.** Agents function as research assistants and validators, not final authority.

**P0 orchestration:** `DatabaseIntelligenceOrchestrator` runs entity resolution → rules → consistency → diff (retrieval skipped P0). Doc 05 proposals enter via `IWriteGate.ProposeSensorBatch` only.

**Acceptance**

- [ ] **DBI-8.1** `DatabaseIntelligenceOrchestrator.Run` executes agents in fixed order and returns `DatabaseIntelligenceRunResult.Passed` only if all agents pass.
- [ ] **DBI-8.2** `CatalogDiffProposalAgent` emits patch proposals without writing live tables.
- [ ] **DBI-8.3** Dynamic Speculative Systems (doc 05) has no code path that bypasses `IWriteGate` for catalog writes.
- [ ] **DBI-8.4** Human `ApproveBatch` required before sim-visible data changes.
- [ ] **DBI-8.5** Post-P0: retrieval agent connector feeds same orchestrator with cited source bundles.

## Non-Functional Requirements

- Must handle thousands of systems without performance degradation
- All changes must be auditable and reversible
- Agent decisions must be explainable (why a value was normalized or flagged)
- Zero tolerance for data corruption or inconsistent states
- **P0:** No `UnityEngine` in `ProjectAegis.Data`; Sim/Delegation may reference Data; Data references neither (ADR-001)
- **P0:** Deterministic headless tests in `ProjectAegis.Data.Tests` (validation, snapshot immutability, write gate, scenario binding)

## Agentic Capabilities

- Claude/Cursor (via Unity-MCP) can:
  - Ask the Database Intelligence Layer to “normalize all new speculative systems added this week”
  - Request a full consistency report across the entire database
  - Trigger a validation pass after approving new speculative content
  - View change history and rollback specific updates

- All agents in the system (Dynamic Speculative Systems Agent, Balance Tuning Agent, etc.) must route their database writes through this layer

**P0 guardrail:** MCP and code share the same `ICatalogReader` / `IWriteGate` APIs — no special auto-commit path.

## Technical Considerations

- Built on a structured database (SQLite for development, scalable solution for production)
- Uses Unity DOTS-friendly data formats for fast simulation access
- Implements a clear API so other agents can safely read/write data
- Strong emphasis on data provenance and audit logging

**P0 layout:** `data/catalog/aegis-catalog.dev.sqlite` (gitignored; seed via `CatalogSeedBootstrap` / CI). Runtime export: read-only DTO batches from SQLite; BlobAssets built in Unity layer, not in Data assembly.

## Future Extensibility

- Cloud-based database with real-time synchronization for multiplayer and research use cases
- Machine learning models to predict balance impact of new systems before they are added
- Public API for modders to contribute new systems with automatic validation
- Integration with external military databases (when licensing allows)

## Cross-Domain Traceability

How catalog fields and snapshots feed simulation domains (headless read path first).

| Domain doc | Catalog consumption | Key fields / APIs |
|------------|---------------------|-------------------|
| [15 Sensors / EW](15-Sensor-Detection-And-EW.md) | Detection tick reads `basePd`, sensor IDs, platform positions from catalog | `ICatalogReader.TryGetBasePd`, `GetSortedSensorBindings`, `CatalogSensorBinding` (frequency/processing P1 columns); `DetectionBindingKey`; EW degradation uses same binding keys as doc 15 §Sensor Model |
| [16 Logistics / magazines](16-Logistics-And-Magazines.md) | Magazine→mount→weapon chain, fuel curves, readiness (partial P0) | `CatalogPlatformEntry.CombatRadiusNm` for ferry/validation; `UnitReadinessMapFactory` + scenario `ScenarioUnitReadinessDto`; full magazine counts post-P0 import |
| [18 Combat domains](18-Combat-Domains.md) | Domain validators consume weapon envelopes, PK tables, damage flags from catalog export | `TryGetCombatRadiusNm` for air package reachability; platform HP / critical flags (P1); near-future archetypes via `NearFutureArchetypeCatalog` / `CatalogArchetypeBinding` |

**Scenario binding:** Doc 11 `metadata.dbRef` → `TryResolveDbRef` → snapshot ID; mismatch hard-fails load (P0 spec `CatalogResolveException` pattern).

**Near-future / speculative:** Doc 09 TL-0–TL-3 and doc 10 TL-3–TL-5 content gated by `TrlLevel`, `CatalogArchetypeGate`, and scenario flags — catalog rows tagged at import, not inferred at runtime.

## Open Questions / Decisions Needed

All charter questions for database intelligence are **locked** for Sprint 14 design review. See [Resolved Design Decisions](#resolved-design-decisions) and the [locked P0 spec](../../docs/superpowers/specs/2026-05-30-database-intelligence-p0-design.md). No reopen without user approval.

| Former open question | Resolution location |
|---------------------|---------------------|
| Auto vs human normalization? | [§1 Normalization autonomy](#1-normalization-autonomy) |
| Balance drift tolerance? | [§3 Balance drift](#3-balance-drift-tolerance) |
| Database branching (TL-0–TL-5)? | [§4 TL branches and snapshots](#4-tl-branches-and-database-snapshots) |
| Release train vs engine cadence? | [§5 Release trains](#5-release-trains) + Dual-Track Pipeline above |

## Implementation Mapping (headless)

| Requirement area | Type / path (`ProjectAegis.Data`) | Notes |
|------------------|-----------------------------------|-------|
| Read API | `ICatalogReader`, `SqliteCatalogReader`, `InMemoryCatalogReader`, `CatalogReaderFactory` | Sorted sensor bindings; `TryResolveDbRef`, `TryGetCombatRadiusNm`, `TryGetPlatformPosition` |
| Staged writes | `IWriteGate`, `CatalogWriteGate` | `ProposeSensorBatch` → `ApproveBatch` / `RejectBatch`; `CatalogStagingBatchSummary` |
| Snapshots / release train | `DbSnapshotStore`, `DbReleaseRecord`, `CatalogValidationDefaults` | `catalog_snapshot`, `db_release` tables |
| Import | `CatalogJsonImporter`, `CatalogBulkImporter`, `CatalogImportGate`, `CatalogSeedBootstrap` | JSON file drop → SQLite; review states on import |
| Provenance | `CatalogProvenanceTier`, `CatalogSensorBinding`, `CatalogChangeLogEntry` | Tiers + per-row citation/confidence/reviewer |
| Validation | `ScenarioValidationEngine`, `ValidationFinding`, `ValidationReport`, `CatalogRulesValidationAgent`, `CatalogConsistencyAgent` | Deterministic reports; golden hashes |
| Intelligence pipeline | `DatabaseIntelligenceOrchestrator`, `IDatabaseIntelligenceAgent`, `CatalogEntityResolutionAgent`, `CatalogDiffProposalAgent` | Headless agent chain; `RunBalticDefault()` smoke |
| Scenario authoring | `ScenarioDocumentDto`, `ScenarioDocumentJsonLoader`, `ScenarioMetadataDto`, `ScenarioDataPaths` | `dbRef` in metadata; moved from Sim per DATA-3 |
| Near-future gates | `NearFutureArchetypeCatalog`, `CatalogArchetypeGate`, `SwarmTier`, `CatalogArchetypeBinding` | TL/archetype routing for docs 09/10 |
| Quarantine | `CatalogQuarantinePromoter`, `QuarantinedCatalogBinding` | Failed import/review isolation |
| Clock / determinism | `ICatalogClock`, `FixedCatalogClock` | Injectable time for commits |

**Default DB path:** `CatalogReaderFactory.ResolveBalticPatrolDatabasePath()` → SQLite under `data/catalog/`.

**Consumers (outside Data):** `ProjectAegis.Sim` engage/envelope (DATA-4), `ProjectAegis.Delegation` scenario policy — run `gitnexus impact` before moving `ScenarioPolicyRepository` or weapon envelope symbols.

**Tests:** `src/ProjectAegis.Data.Tests/` — validation, write gate, snapshot binding (P0 plan §7).

## Resolved Design Decisions

Decisions locked **2026-06-04** for Sprint 14 design review. P0 implementation bounds in [locked P0 spec](../../docs/superpowers/specs/2026-05-30-database-intelligence-p0-design.md).

### 1. Normalization autonomy

**Decision:** **Human confirmation** for material changes; **detect-only** in P0; **auto-normalize within tolerance bands** post-P0.

| Mode | When | Behavior |
|------|------|----------|
| **Detect (P0)** | Every staging batch | `CatalogConsistencyAgent` + validation rules emit `ValidationFinding`; no silent field rewrite |
| **Human gate (P0)** | Commit affecting **> 10 records** OR any field tagged `balanceCritical` | Requires explicit `ApproveBatch` with `actorType` / `actorId` |
| **Auto band (post-P0)** | Single-field drift inside configured band | May auto-apply normalization (e.g., unit conversion nm↔km) without full review queue |
| **Human required (post-P0)** | Outside band or cross-era cost/signature outlier | Stays in staging until reviewer approves |

**Tolerance bands (target post-P0 defaults, tuneable):**

| Field class | Band | Example |
|-------------|------|---------|
| Range | ±2% or ±0.5 nm (greater of) | 120 nm vs 118 nm → auto; 120 vs 90 → flag |
| Speed | ±3% or ±0.05 Mach | Knots/Mach enum must match platform domain |
| Cost / signature | ±5% within same TL band | Cross-TL comparison → always flag |
| Confidence | N/A (not normalized) | Human-overridable only |

Aligns with doc 05: AI-proposed stats stay `interpreted_value` until approved.

### 2. Agent write safety (staging)

**Decision:** All catalog writes go through **`IWriteGate`** — no bypass for Dynamic Speculative Systems or MCP tools.

```
Propose(batch) → Staging
Approve(batchId, actor) → Validate (orchestrator + rules) → Commit → New snapshot / release row
Reject(batchId) → discard staging
```

Human approval is **mandatory** for P0 (locks legacy open question #1). Doc 05 approved bundles enter as staged sensor/platform batches, not direct SQL.

### 3. Balance drift tolerance

**Decision (full product):** Flag when win-rate delta exceeds **±8%** over **≥ 500** agent-vs-agent runs (tuneable per platform class).

**Decision (P0):** **Out of scope** — `IBalanceTelemetrySink` reserved no-op; no commit path reads sim telemetry. Balance Tuning Agent proposals use same staging workflow when implemented.

### 4. TL branches and database snapshots

**Decision (full product):** **Yes** — TL-gated DB branches **TL-0 through TL-5** with **shared canonical IDs** across branches (doc 10 `BLACK_PROJECT_MODE` at TL-5).

| TL | Era (glossary) | Branch content |
|----|----------------|----------------|
| TL-0 | 2025 baseline | Fielded systems only |
| TL-1 | 2026–2028 | Early fielding |
| TL-2 | 2028–2030 | Primary near-future target |
| TL-3 | 2030–2032 | Advanced near-future (doc 09) |
| TL-4 | 2035–2040 | Speculative (doc 10) |
| TL-5 | 2040+ | Black-project / Future War — scenario flag required |

**Decision (P0):** **Single `main` catalog** + **tagged snapshots** (`snapshotId`, `db_release`); metadata reserve `branch` column for future TL splits. Scenario selects content via `dbRef` + scenario TL/mode flags (docs 09/10/11), not separate DB forks yet.

### 5. Release trains

**Decision:** Database **release trains** decoupled from engine semver.

- Each train publishes `ReleaseVersion` pointing at immutable `SnapshotId`.
- Public intake triage (`accepted`, `needs_evidence`, …) feeds internal curation queue — not FIFO.
- Scenario packages (doc 11) pin `dbRef` / `dbSnapshotId` so replays and validation use the same drop.
- Engine updates may ship without DB drop; DB drop may ship without engine when schema version compatible (`DbReleaseRecord.SchemaVersion`).

### 6. CMO parity: dual-track intake

**Decision:** Retain **public issue-only intake** + **internal curation** (see Dual-Track Content Pipeline). Agent proposals from doc 05 map to **internal track** with evidence citations; community templates map to **public track** without direct edit rights.

### 7. P0 scope boundary (explicit deferrals)

| In P0 | Deferred |
|-------|----------|
| `ProjectAegis.Data` + SQLite + read API + write gate | Automated re-indexing **agent** (process) |
| Snapshot + audit + `dbRef` binding | TL-0–TL-5 **branch databases** |
| Detect-only normalization + referential validation | Auto-normalize outside bands |
| `DatabaseIntelligenceOrchestrator` (4 agents) | Retrieval agent + full MCP normalize-week |
| CMO subset import (`CatalogJsonImporter`) | Full cmano-db markdown corpus |
| Provenance tiers on sensor rows | Per-field provenance on all entity types |
| — | Balance drift detection |
| — | Cloud sync, modder public API, ML balance prediction |

---

## Traceability

| Epic / FR | This document |
|-----------|---------------|
| Doc 05 Dynamic Systems | Staging → doc 06 write gate; provenance handoff |
| Doc 09 Near-future TL-0–TL-3 | `TrlLevel`, archetype catalog gates |
| Doc 10 Speculative TL-3–TL-5 | Branch design §4; `CatalogArchetypeGate` |
| Doc 11 Mission Editor | `dbRef` / snapshot binding, validation export |
| Doc 15/16/18 | [Cross-Domain Traceability](#cross-domain-traceability) |
| P0 implementation | [Locked spec](../../docs/superpowers/specs/2026-05-30-database-intelligence-p0-design.md), plan `docs/superpowers/plans/2026-05-30-database-intelligence-p0.md` |

---

**Status:** Locked (Sprint 14)