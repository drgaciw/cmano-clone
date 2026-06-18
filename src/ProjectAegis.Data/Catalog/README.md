# Catalog (read model + import gate + schema)

`ProjectAegis.Data.Catalog` — the **catalog core** of the Database Intelligence
layer: the read interface the simulation consumes (`ICatalogReader`), the SQLite
schema/migrations behind it, the deterministic **TRL/confidence import gate**, and
the file/seed importers that populate a catalog db. Every other Data subsystem
([Write Gate](../WriteGate/README.md), [Import](../Import/README.md),
[Osint](../Osint/README.md), [Platform](../Platform/README.md), [Agents](../Agents))
builds on the types defined here — this is the keystone reference.

**Requirement trace:** Req-06 §6 (provenance tiers), DBI-1.x / DBI-4.x
(`Game-Requirements/requirements/06-Database-Intelligence.md`); ADR-006 (read/write
boundary), ADR-008 (scenario `dbRef` resolution); Req-09 (near-future archetypes).
Story trace: DATA-2 (JSON import), DATA-4 (weapon envelope), Sprint-19 P2 (provenance
+ snapshots). **Posture:** *read is pure and deterministic; writes never bypass the
[Write Gate](../WriteGate/README.md).*

## Intent

The simulation must resolve catalog facts — sensor `base_pd`, combat radius, weapon
envelopes — reproducibly, so replays and exports match bit-for-bit. The catalog
therefore separates three concerns:

1. **Read** — `ICatalogReader` is the only surface Sim/Delegation depend on. All
   iteration is **sorted ordinally** (`platform_id`, then `sensor_id`) so traversal
   order is stable regardless of storage.
2. **Gate** — `CatalogImportGate` partitions candidate rows into *approved* vs
   *quarantined* by confidence, TRL, and review state. This same gate runs at JSON
   import time *and* at write-gate approve time, so the rule lives in one place.
3. **Persist** — importers (`CatalogJsonImporter`, `CatalogBulkImporter`,
   `CatalogSeedBootstrap`) and `SqliteCatalogReader`'s migrations own the schema.
   No LLM, no `DateTime.Now` on these paths.

## Architecture

```
candidate rows (JSON drop / markdown / agent proposal)
        │
        ▼
  CatalogImportGate.PartitionForImport          ┌─ approved  ──► sensor table
   confidence ≥ 0.5, TRL ≥ 4, review=approved ──┤
        │  (ordinal sort)                        └─ quarantined ► sensor_quarantine
        ▼                                               │ (+ rejection_reason)
  SqliteCatalogReader (applies migrations 001–007)      │
        │  GetSortedSensorBindings() / TryGet*()        ▼
        ▼                                    CatalogQuarantinePromoter
  Sim / Delegation consume ICatalogReader    (reviewer-approved → sensor)
```

| Type | Role |
|------|------|
| `ICatalogReader` | Read surface: `GetSortedSensorBindings`, `TryGetBasePd`, and (default-implemented) `TryResolveDbRef` (ADR-008), `TryGetCombatRadiusNm`, `TryGetPlatformPosition`, `TryGetWeaponEnvelope` (DATA-4). |
| `SqliteCatalogReader` | SQLite-backed reader. Opens one non-pooled connection, applies `assets/data/catalog/migrations/*.sql` on construct (idempotent skips), caches sorted bindings. `IDisposable` (clears SQLite pools on dispose). |
| `InMemoryCatalogReader` | Fixture reader for headless tests; `BalticPatrolFixture()` returns the canonical two-sensor Baltic set. |
| `NullCatalogReader` | Empty singleton (`Instance`) for scaffolding/`p0-scaffold` paths. |
| `CatalogReaderFactory` | Resolves the Baltic patrol db path and returns a seeded reader, or `null` when the repo root can't be located. |
| `CatalogImportGate` | The gate. `PartitionForImport` (approved vs quarantined), plus `ApplyMinimumConfidence` / `ApplyTlReviewGate` / `ApplyAllGates`. Defaults: confidence `0.5`, TRL `4`, `requireApproved = true`. |
| `CatalogJsonImporter` | Reads a `sensors_*.json` drop into `CatalogSensorBinding[]` and writes them (gated) into SQLite. |
| `CatalogBulkImporter` | Merges every `*.json` in a directory (last-writer-wins per `platform/sensor`), gates, and writes one db. |
| `CatalogSeedBootstrap` | Seeds the deterministic Baltic fixture rows + platforms into a fresh db (delegates to the JSON importer when `sensors_baltic.json` exists). |
| `CatalogQuarantinePromoter` | Re-checks `sensor_quarantine` rows and promotes any now-passing approved rows into `sensor`. |
| `CatalogArchetypeGate` / `NearFutureArchetypeCatalog` | Req-09 scenario gates: filter near-future archetypes by technology level and swarm-tier entity caps. |

### Record / constant types

| Type | Shape |
|------|-------|
| `CatalogSensorBinding` | `(PlatformId, SensorId, BasePd, SourceFactId, Confidence, ImportBatchId, SourceFile, ReviewState, TrlLevel, ValueTier, ReviewerId, RevisedUtcTicks, CitationRef)` — defaults are `Approved` / TRL 9 / `gameplay_abstraction`. |
| `CatalogPlatformEntry` | `(PlatformId, LatDeg, LonDeg, CombatRadiusNm)`. |
| `WeaponEnvelopeDto` | `(MinRangeMeters, MaxRangeMeters)` (DATA-4 engage envelope). |
| `CatalogChangeLogEntry` / `DbReleaseRecord` | Audit + release-binding rows written by the write gate / snapshot binder. |
| `CatalogReviewStates` | `approved` · `provisional` · `rejected`. |
| `CatalogProvenanceTier` | `source_fact` · `interpreted_value` · `gameplay_abstraction` (Req-06 §6; `Normalize` falls back to gameplay). |
| `CatalogEntityMap` | Req-06 entity → table mapping (table name, PK columns, deterministic `ORDER BY`, runtime DTO) for the P0 entities. |

## Usage

```csharp
using ProjectAegis.Data.Catalog;

// 1. Read — seeds the Baltic catalog if the db is missing, then resolves base_pd.
using var reader = (ICatalogReader)new SqliteCatalogReader(databasePath);
if (reader.TryGetBasePd("u1", "radar-1", out var basePd))
    Console.WriteLine($"base_pd = {basePd}");

foreach (var b in reader.GetSortedSensorBindings())   // stable platform/sensor order
    Console.WriteLine($"{b.PlatformId}/{b.SensorId} ({b.ReviewState}, TRL {b.TrlLevel})");

// 2. Gate a batch of candidate rows before persisting them.
var (approved, quarantined) = CatalogImportGate.PartitionForImport(candidateBindings);
foreach (var q in quarantined)
    Console.WriteLine($"rejected {q.Binding.PlatformId}/{q.Binding.SensorId}: {q.RejectionReason}");

// 3. Import a JSON drop into a fresh db (approved → sensor, rest → sensor_quarantine).
CatalogJsonImporter.ImportToSqlite(
    jsonPath: CatalogJsonImporter.ResolveBalticSensorsJsonPath(),
    databasePath: databasePath);
```

`PartitionForImport` rejection reasons are stable strings:
`confidence_below_minimum`, `trl_below_minimum`, `review_state_<state>`.

## Schema & migrations

`SqliteCatalogReader` applies `assets/data/catalog/migrations/*.sql` in filename
order on open and **skips already-applied migrations** by probing for the table/column
they introduce — so opening an existing db is safe and idempotent:

| Migration | Adds |
|-----------|------|
| `001_sensor_base_pd` | `sensor` (platform/sensor → `base_pd` + provenance columns). |
| `002_sensor_review_trl` | `review_state`, `trl_level` on `sensor`. |
| `003_sensor_quarantine` | `sensor_quarantine` (rejected rows + `rejection_reason`). |
| `004_platform_validation` | `platform` (lat/lon, combat radius). |
| `005_req06_provenance_audit_staging` | `catalog_change_log`, staging tables, provenance columns. |
| `006_snapshot_content_hash` | `catalog_snapshot.content_hash_sha256` (replay binding). |
| `007_platform_editor_phase_a` | `platform_mount` / loadout / magazine / comms (ADR-011). |

The reader reads the richer 13-field `CatalogSensorBinding` when the `value_tier`
provenance column is present, and the 9-field shape otherwise.

## Constraints & gotchas

- **Read is the public contract; writes go through the gate.** Sim/Delegation depend
  on `ICatalogReader` only. To change rows use [`IWriteGate`](../WriteGate/README.md) —
  never `INSERT` into `sensor` directly outside the importers/seed paths.
- **Determinism is load-bearing.** Every iteration is ordinal-sorted and time is
  injected (`ICatalogClock`), so catalog exports and replays reproduce. Do not
  introduce `DateTime.Now`, hash-set iteration order, or culture-sensitive parsing
  on these paths (run `replay-verify` if you touch them).
- **The gate is shared, so defaults matter.** `PartitionForImport` defaults
  (confidence `0.5`, TRL `4`, `requireApproved`) are applied at JSON import *and*
  write-gate approve. Loosening them affects both; prefer per-call overrides.
- **Quarantine blocks, it doesn't drop.** Rejected rows land in `sensor_quarantine`
  with a reason and can be promoted later via `CatalogQuarantinePromoter` once a
  reviewer marks them `approved` (and they pass the gate again).
- **One connection, not thread-safe.** `SqliteCatalogReader` wraps a single
  `Pooling=false` connection; use one instance per logical reader and dispose it.
- **Factory can return `null`.** `CatalogReaderFactory.TryCreateBalticPatrolReader`
  returns `null` when the repo root / db path can't be resolved (e.g. an unexpected
  CWD); callers must handle it rather than assume a reader.

## Tests

| Area | Test |
|------|------|
| Gate partition / confidence / TRL / review | `ProjectAegis.Data.Tests/Catalog/CatalogImportGateTests`, `CatalogTlReviewGateTests` |
| JSON import + round-trip + bulk merge | `CatalogJsonImporterTests`, `CatalogJsonRoundTripTests`, `CatalogBulkImporterTests` |
| SQLite reader, seed, migrations, provenance | `SqliteCatalogReaderTests`, `CatalogSeedBootstrapTests`, `CatalogProvenanceMigrationTests` |
| Quarantine write + promote | `CatalogQuarantineTests`, `CatalogQuarantinePromoteTests` |
| Weapon envelope, entity map, archetypes, in-memory/null readers | `CatalogWeaponEnvelopeTests`, `CatalogEntityMapTests`, `CatalogArchetypeGateTests`, `InMemoryCatalogReaderTests`, `NullCatalogReaderTests` |

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "FullyQualifiedName~Catalog" -v minimal
```
