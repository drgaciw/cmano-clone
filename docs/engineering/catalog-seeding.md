# Catalog seeding & reader resolution — how headless runs get a catalog

Every headless run and every `dotnet test` that touches the catalog needs a populated
SQLite database to read from. This doc is the developer reference for that *bootstrap* path:
how [`CatalogReaderFactory`](../../src/ProjectAegis.Data/Catalog/CatalogReaderFactory.cs)
resolves a reader, how [`CatalogSeedBootstrap`](../../src/ProjectAegis.Data/Catalog/CatalogSeedBootstrap.cs)
writes a deterministic Baltic fixture into a fresh DB, and how the committed
`assets/data/catalog/baltic_patrol.db` fits in.

> **Scope.** This is the *seed* path — the small, deterministic Baltic fixture that makes a
> DB usable (sensors + platforms + the `u1` engage chain). It is **not** the multi-thousand
> record CMO corpus import; that enriches the committed DB and is documented separately in
> [`cmo-markdown-import.md`](cmo-markdown-import.md). The write-gate propose/approve internals
> are in [`catalog-write-gate.md`](catalog-write-gate.md).

---

## Why this exists

`ProjectAegis.Sim` and `ProjectAegis.Delegation` never open SQLite directly — they consume an
`ICatalogReader` (ADR-006). Tests and the headless Baltic harness need *some* reader with a
real, kill-chain-complete Baltic order of battle without wiring a curator through the write
gate first. `CatalogSeedBootstrap` is that shortcut: it writes a known-good, deterministic set
of rows so the `u1` blue unit can actually detect, engage, and fire.

---

## Reader resolution (`CatalogReaderFactory`)

Consumers call `ResolveForScenario(scenarioPolicyId)`; the factory picks the reader:

```
ResolveForScenario(policyId, catalogOverride?)
   │
   ├─ catalogOverride != null      ─────────────►  use it (test isolation)
   │
   ├─ policyId starts "baltic-v3-" ─────────────►  InMemoryCatalogReader.BalticV3Fixture()
   │
   └─ otherwise ─► TryCreateBalticPatrolReader()
                      │  resolve baltic_patrol.db path
                      │  • DB file exists  → open it in place (no re-seed)
                      │  • DB file missing → CatalogSeedBootstrap.SeedBalticPatrol(db, true), then open
                      │  • repo root unresolvable → null
                      └─ null  ─────────────────►  InMemoryCatalogReader.BalticPatrolFixture()
```

Two nuances that trip people up:

- **The committed DB is used as-is.** `TryCreateBalticPatrolReaderCore` only calls the seeder
  when the DB file does **not** exist. Because `assets/data/catalog/baltic_patrol.db` is
  committed (a full multi-domain catalog built by the CMO import waves), runtime/CI opens it
  in place — `SeedBalticPatrol` is *not* re-run against it and does not overwrite it.
- **Path precedence.** `ResolveBalticPatrolDatabasePath()` puts the DB next to
  `sensors_baltic.json` when that JSON is found on the `AppContext.BaseDirectory` walk-up;
  otherwise it falls back to the repo-relative `assets/data/catalog/baltic_patrol.db`.

So `CatalogSeedBootstrap` mostly runs against **fresh / temp / scratch** DBs — unit tests,
the importer/OSINT/CLI bootstrap of a missing scratch DB — not against the committed catalog.

---

## `CatalogSeedBootstrap` API

Static, deterministic, idempotent-per-call. All three write directly with parameterized SQL
after ensuring the schema is present.

| Method | What it does |
|--------|--------------|
| `SeedBalticPatrol(dbPath, overwrite = true)` | Seeds the base Baltic Patrol fixture: sensors, platforms, `u1` damage, and the `u1` engage catalog. |
| `SeedBalticV3(dbPath, overwrite = true)` | Layers the Baltic v3 OOB **on top of** a patrol seed (calls `SeedBalticPatrol(dbPath, overwrite: false)` first). |
| `EnrichBalticEngageCatalog(dbPath)` | Idempotently adds only the engage rows (weapons/mounts/loadouts/magazines) to an **existing** production seed without wiping sensors/platforms. |

### Two seed sources

`SeedBalticPatrol` picks its sensor source at runtime:

1. **JSON present** — if `assets/data/catalog/sensors_baltic.json` resolves, it imports those
   sensor rows via `CatalogJsonImporter.ImportToSqlite` (which runs them through
   `CatalogImportGate.PartitionForImport`, quarantining bad rows), then seeds platforms/damage/engage.
2. **JSON absent** — it falls back to the in-code `InMemoryCatalogReader.BalticPatrolFixture()`
   sensor bindings, then seeds platforms/damage/engage.

Either way the result is the same shape. The shipped `sensors_baltic.json` is intentionally
tiny — two `u1` radars (`radar-1` Pd `1.0`, `radar-2` Pd `0.75`) — pinned by
[`CatalogSeedBootstrapTests`](../../src/ProjectAegis.Data.Tests/Catalog/CatalogSeedBootstrapTests.cs).

### What a patrol seed contains

| Rows | Source | Notes |
|------|--------|-------|
| `sensor` | JSON or in-memory fixture | sorted by `(platform_id, sensor_id)`; `approved`, TRL-9 |
| `platform` | `CatalogValidationDefaults.BalticPlatforms()` | positions + combat radius; all pinned to snapshot `baltic_patrol` |
| `catalog_snapshot` | — | `INSERT OR IGNORE` of `BalticSnapshotId` (`"baltic_patrol"`) |
| `platform_damage` | hardcoded for `u1` | `max_hp 100`, withdraw 25%, `gameplay_abstraction` tier |
| engage chain (`weapon_catalog`, `platform_mount`, `platform_loadout`, `platform_magazine`) | `SeedBalticEngageCatalog` | the `u1` ASUW default loadout — see below |

### The `u1` engage chain (GAME-01 / KILLCHAIN-03)

`SeedBalticEngageCatalog` seeds a complete, kill-chain-valid weapons path for `u1`, guarded by
`TableExists` so it degrades cleanly on partial schemas:

- **Weapons** — `RIM-66 Standard MR` (SARH, 1–74 km) and `76mm OTO Melara` gun (0–16 km),
  keyed by `CatalogWeaponIds.BalticRim66` / `BalticOto76`.
- **Mounts** — `vls-fwd` (VLS, 360°, cap 8) and `gun-76` (gun, 300°, cap 1).
- **Loadout** — one default `asuw-default` (`ASUW Default`, role `asuw`).
- **Magazines** — RIM-66 ×8 in `vls-fwd`, OTO-76 ×1 in `gun-76`.

Deliberate constraints baked in (do not "fix" these without understanding the gate they satisfy):

- **Magazine qty ≤ mount capacity** — exceeding it trips `PLE-MAG-CAPACITY` on propose/export diffs.
- **No `platform_mobility` row** — the kill-chain speed rule *skips* when mobility is absent;
  seeding real ship max speeds would raise warnings/errors (weapon flight-speed heuristic) and
  break the clean Baltic golden/report emptiness.
- **Weapon ranges stay inside the `u1` combat radius** so kill-chain rule **R4** stays green.

### Baltic v3 layering

`SeedBalticV3` seeds the patrol base first (no overwrite), then adds the v3 sensor bindings and
v3 platforms — **skipping** `u1`, `hostile-1`, and `hostile-far` so it never clobbers the shared
patrol rows. This matches the v3 OOB (`u1`, `hostile-1`, `ucav-blue`, `ucav-red`). Note the
`ResolveForScenario` path prefers the in-memory `BalticV3Fixture()` for `baltic-v3-*` policies;
`SeedBalticV3` is for tests/tools that want the layered v3 catalog on disk.

---

## Schema & migrations

The seeder never hand-creates tables. Opening a `SqliteCatalogReader` (which the seed path does
via a throwaway `using` before writing) calls `ApplyMigrations()`, applying the ordered SQL in
[`assets/data/catalog/migrations/`](../../assets/data/catalog/migrations/)
(`001_sensor_base_pd.sql` … `011_link_catalog_staging.sql`). This is why `SeedBalticPatrol`
works on an empty path — the first reader open is what bootstraps the schema.

---

## Usage

**In a test** — seed a temp DB, then read it back:

```csharp
var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-seed-{Guid.NewGuid():N}.db");
CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
using var reader = new SqliteCatalogReader(dbPath, "test");
reader.TryGetBasePd("u1", "radar-1", out var pd);   // 1.0
```

Most `ProjectAegis.Data.Tests` / adapter tests use exactly this pattern (`overwrite: true` to a
per-test temp path). The `[Collection("CatalogSqlite")]` xUnit collection serializes tests that
share SQLite connection pools.

**Bootstrapping a missing scratch DB** (importer / OSINT / CLI) uses `overwrite: false` so it
seeds only when the file is new and leaves an existing scratch DB intact:

```csharp
CatalogSeedBootstrap.SeedBalticPatrol(databasePath, overwrite: false);
```

---

## Common pitfalls

| Symptom | Cause / fix |
|---------|-------------|
| Edited the committed `baltic_patrol.db` and runtime didn't change | It should — but note the factory opens the committed file in place; it does **not** re-seed over it. To regenerate the *minimal* fixture, delete the file and let `TryCreateBalticPatrolReader` re-seed, or seed to a temp path. |
| `SeedBalticPatrol` produced only 2 sensors | Expected for the minimal fixture (`sensors_baltic.json`). The full multi-domain catalog comes from the CMO import waves, not the seeder — see [`cmo-markdown-import.md`](cmo-markdown-import.md). |
| Kill-chain report has warnings after adding mobility to the seed | Intentional: the seed omits `platform_mobility` so the speed rule is skipped. Adding real speeds surfaces the flight-speed heuristic. |
| `PLE-MAG-CAPACITY` on a propose/export diff | A seeded/edited magazine quantity exceeds its mount capacity. Keep qty ≤ mount `capacity`. |
| `baltic-v3-*` scenario read the patrol catalog, not v3 | `ResolveForScenario` routes `baltic-v3-*` to the in-memory `BalticV3Fixture()`. Use `SeedBalticV3`/an override reader if you need the on-disk v3 catalog. |
| Seeded DB opened but rows missing on a partial schema | Engage/damage seeding is `TableExists`-guarded; if a migration is missing the row is skipped rather than throwing. Confirm migrations `001`–`011` are present. |

---

## See also

| Topic | Doc |
|-------|-----|
| Data-layer map (readers, snapshots, scenario binding) | [`ProjectAegis.Data/README.md`](../../src/ProjectAegis.Data/README.md) |
| Write-gate propose → approve → commit + error codes | [`catalog-write-gate.md`](catalog-write-gate.md) |
| CMO markdown corpus import (enriches the committed DB) | [`cmo-markdown-import.md`](cmo-markdown-import.md) |
| Data-layer boundary decision (read-only consumers) | [`adr-006-data-layer-boundary.md`](../architecture/adr-006-data-layer-boundary.md) |
| Determinism rules (clock, ordering, hashing) | [`determinism-and-replay.md`](determinism-and-replay.md) |
