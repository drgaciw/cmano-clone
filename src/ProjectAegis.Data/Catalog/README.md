# Catalog — records, readers, quarantine & provenance

The catalog is the authoritative store of platform / sensor / weapon / loadout
data the simulation consumes. This directory holds the **record types**, the
**read API** (`ICatalogReader` and its implementations), the **import/quarantine
plumbing**, and the **provenance + review** metadata that the
[write gate](../WriteGate/README.md) enforces (req-06 / [ADR-006](../../../docs/architecture/adr-006-data-layer-boundary.md)).

> Writes never happen here directly. Importers and agents stage rows through
> `IWriteGate`; this layer owns reading, the quarantine/promotion mechanics, and
> the record shapes those flows operate on.

## Read API (`ICatalogReader`)

The sim and validation engine depend only on `ICatalogReader`, never on SQLite:

| Member | Purpose |
|--------|---------|
| `LayerVersion` | Catalog schema/layer version string. |
| `GetSortedSensorBindings()` | Sensor rows sorted by `platform_id`, `sensor_id` (ordinal) — deterministic iteration. |
| `TryGetBasePd(platformId, sensorId, out basePd)` | Detection base probability lookup. |
| `TryResolveDbRef(dbRef, out snapshotId)` | ADR-008: resolve a scenario's `dbRef` to a catalog snapshot. |
| `TryGetCombatRadiusNm(platformId, out nm)` | Round-trip combat radius for reachability checks. |
| `TryGetPlatformPosition(platformId, out lat, out lon)` | WGS84 position for distance math. |
| `TryGetWeaponEnvelope(weaponId, out envelope)` | DATA-4: weapon min/max range (m) for engage wiring. |

The interface uses default-implemented `*Core` methods so readers can opt into the
newer lookups incrementally (the defaults return `false`).

### Implementations

| Reader | Use |
|--------|-----|
| `SqliteCatalogReader` | Production read path; SQLite-backed, applies migrations on open. |
| `InMemoryCatalogReader` | Deterministic fixture (e.g. `BalticPatrolFixture()`) for headless tests and the Baltic harness. |
| `NullCatalogReader` | Empty reader for default/abstention paths. |
| `CatalogReaderFactory` | Resolves the Baltic reader for CI/harness — seeds via `CatalogSeedBootstrap` when the DB is missing, else returns `null`. |

## Quarantine & promotion

New rows are confidence/TRL/review gated before they reach live tables
(`CatalogImportGate`):

- `ApplyMinimumConfidence` — drops rows below `DefaultMinimumConfidence` (`0.5`).
- `ApplyTlReviewGate` — drops rows below `DefaultMinimumTrl` (`4`) and, when
  `requireApproved`, rows whose `ReviewState` is not `approved`.
- `ApplyAllGates` — confidence then TRL/review, composed.
- `PartitionForImport` — splits an input into `(Approved[], Quarantined[])`,
  tagging each rejected row with a reason: `confidence_below_minimum`,
  `trl_below_minimum`, or `review_state_{state}`. All outputs are sorted by
  `platform_id`, `sensor_id`.

`CatalogQuarantinePromoter.PromoteApproved(databasePath)` moves reviewer-approved
rows from `sensor_quarantine` into the live `sensor` table — but only rows that
still pass `PartitionForImport`, so an approval cannot smuggle a sub-threshold row
past the gate. It returns the count promoted.

Import entry points: `CatalogJsonImporter` (single JSON drop), `CatalogBulkImporter`
(every `*.json` in a directory into one SQLite catalog), and `CatalogSeedBootstrap`
(deterministic Baltic fixture rows).

## Provenance & review metadata

Every row carries provenance and review state so the gate and audit trail can
reason about trust:

- `CatalogProvenanceTier` — `source_fact`, `interpreted_value`,
  `gameplay_abstraction` (req-06 §6). `Normalize` defaults unknown tiers to
  `gameplay_abstraction`.
- `CatalogReviewStates` — `approved`, `provisional`, `rejected`.
- `CatalogSensorBinding` is the canonical record and carries the full envelope:
  `BasePd`, `Confidence`, `SourceFactId`, `ImportBatchId`, `SourceFile`,
  `ReviewState`, `TrlLevel`, `ValueTier`, `ReviewerId`, `RevisedUtcTicks`,
  `CitationRef`.
- `CatalogEntityMap` is the req-06 entity-to-table map for the SQLite schema.
- `CatalogChangeLogEntry` / `DbReleaseRecord` capture audit and release rows.

Other record types here cover the broader catalog model: `CatalogPlatformEntry`,
`CatalogWeaponRecord`, `CatalogMount`, `CatalogLoadout`, `CatalogMagazineEntry`,
the `Catalog*Binding` families, archetype bindings/runtime, and
`WeaponEnvelopeDto`.

## Determinism

- Readers, gates, and promoters sort by canonical keys (ordinal) — iteration and
  output order never depend on insertion order.
- `SqliteCatalogReader` uses `Pooling=false` and clears pools to keep file access
  reproducible across test runs.
- These guarantees feed the [snapshot hasher](../Snapshots/README.md), so a given
  catalog content always produces the same snapshot hash.

## Tests

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter Catalog -v minimal
```

`src/ProjectAegis.Data.Tests/Catalog/` covers readers, the import/TL gates,
quarantine + promotion, provenance migration, JSON round-trip, and seed
bootstrap.

## See also

- [ProjectAegis.Data overview](../README.md)
- [WriteGate — staged catalog writes](../WriteGate/README.md)
- [Snapshots — deterministic snapshots & release train](../Snapshots/README.md)
- [Validation — scenario & catalog validation](../Validation/README.md)
- [ADR-006 — data-layer boundary](../../../docs/architecture/adr-006-data-layer-boundary.md)
- [Requirement 06 — Database Intelligence](../../../Game-Requirements/requirements/06-Database-Intelligence.md)
