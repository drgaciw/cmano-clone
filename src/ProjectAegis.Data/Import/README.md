# CMO Markdown Import (parse → gate → write gate)

`ProjectAegis.Data.Import` — turns **CMO markdown** (a [cmano-db.com](https://cmano-db.com)
crawler export) into staged catalog proposals. The importer *parses* markdown into
catalog bindings, the import gate *partitions* them into approved vs quarantined, and
survivors are handed to the [Catalog Write Gate](../WriteGate/README.md) as batches.
**Nothing here writes the live catalog directly** — every commit still goes through
propose → approve.

**Requirement trace:** DBI-1.x, DBI-4.x, DBI-7.1
(`Game-Requirements/requirements/06-Database-Intelligence.md`); ADR-006; catalog
Phase 2 import plan (`docs/superpowers/plans/2026-06-04-catalog-phase2-import.md`).
Story trace: DATA-5 (sensor P0), S19-02 (quarantine report), S22-04 (platform / weapon /
mount staging). **Posture:** *parse, never auto-commit* — markdown is a noisy source, so
rows are staged and human-approved like every other write path.

## Intent

The reference crawler in `tools/cmano-db-crawler/` produces markdown digests of CMO
sensors, ships, and weapons. Rather than load that markdown straight into SQLite, this
subsystem keeps the ingest deterministic and auditable:

1. **Parse** markdown sections into typed bindings (`CmoMarkdownImporter`). Parsing is
   pure — it touches no database and never writes SQLite.
2. **Gate** sensor rows through `CatalogImportGate.PartitionForImport`: high-quality
   rows are *approved*, the rest are *quarantined* (written to `catalog_quarantine` and
   surfaced in a structured report).
3. **Propose** approved rows as write-gate batches (chunked at 500), where they wait for
   human approval. The catalog db is seeded with the Baltic patrol scenario if missing.

The whole path is deterministic — stable ordinal sort, injectable `ICatalogClock`, no
wall-clock, no network — so imports and replays are reproducible.

## Architecture

```
CMO markdown (cmano-db crawler export)
        │  CmoMarkdownImporter.ReadSensorBindings / ReadPlatformBindings /
        │  ReadWeaponBindings / ReadPlatformMounts        (pure parse, sorted ordinally)
        ▼
CmoMarkdownImportProposer.ProposeFromMarkdown(...)            (sensor P0 path)
        │
   CatalogImportGate.PartitionForImport(parsed)
        ├──────────────► quarantined → CatalogJsonImporter.WriteQuarantineRows
        │                              + CmoMarkdownQuarantineReportEntry[]
        ▼ approved
   ChunkBindings(approved, chunkSize = 500)                   (deterministic chunks)
        │  per chunk
        ▼
   CatalogWriteGate.ProposeSensorBatch(chunk, "agent", "cmo-markdown-import", ...)
        ▼
   catalog_staging_sensor (state = "proposed")  ──►  human approve via write gate
```

| Type | Role |
|------|------|
| `CmoMarkdownImporter` | Static parser. `ReadSensorBindings`, `ReadPlatformBindings`, `ReadWeaponBindings`, `ReadPlatformMounts` (plus `*FromText` overloads) turn `### `-delimited markdown sections into catalog bindings. Pure; never writes SQLite. |
| `CmoMarkdownImportProposer` | Orchestrates parse → gate → propose. `ProposeFromMarkdown` is the sensor P0 path; `ProposePlatformWeaponMounts` (S22-04) stages platform/weapon/mount batches. |
| `CmoMarkdownImportResult` | `(ParsedCount, ApprovedCount, QuarantinedCount, Batches, QuarantineReport)` for the sensor path. |
| `CmoMarkdownPlatformImportResult` | `(PlatformCount, WeaponCount, MountCount, PlatformBatchId?, WeaponBatchId?, MountBatchId?)` for S22-04. |
| `CmoMarkdownImportBatch` | `(BatchId, RecordCount)` — one per staged chunk. |
| `CmoMarkdownQuarantineReportEntry` | `(PlatformId, SensorId, Reason, SourceFile)` — structured quarantine row for the JSON report (S19-02). |

The approve-time quality gate lives in
`ProjectAegis.Data.Catalog.CatalogImportGate.PartitionForImport` (default minimum
confidence `0.5`, minimum TRL `4`, `ReviewState == approved`).

## Parsing rules

Each `### ` heading starts a section; fields are read from markdown table rows / links
and flushed when the next heading (or EOF) is reached.

| Entity | Id scheme | Key fields | Review state | Notes |
|--------|-----------|------------|--------------|-------|
| Sensor | `cmo-sensor-<n>` from `/sensor/<n>/` | `BasePd` inferred from `Range Max`; `Confidence` (default `0.85`); `TrlLevel = 9` | **Approved** | Cleared by the approve gate by default. |
| Platform | slug of title (or Baltic alias) from `/ship\|aircraft\|submarine\|facility/<n>/` | `Domain` inferred from `Type`; `Nationality`; `TrlLevel = 9` | Provisional | `mapBalticIds` remaps `patrol-frigate-u1 → u1`, etc. |
| Weapon | `cmo-weapon-<n>` from `/weapon/<n>/` | `MaxRangeMeters` = max `… Max: N km` × 1000 | Provisional | Parsed only inside a `**Weapons**` block. |
| Mount | `<platformId>/<weaponSlug>` | `MountType` inferred (`gun`/`tube`/`vls`/`rail`) | Provisional | One per `- name — type —` weapon line. |

`InferBasePd` maps range units deterministically: `nm → /200`, `km → /300`,
`m → /370400`, all clamped to `0.05–1.0` (unknown unit → `0.5`). Platform IDs are
slugged (lowercased, non-word → `-`, trimmed, max 64 chars).

## Usage

### Sensor import (parse → stage → approve)

```csharp
using ProjectAegis.Data.Import;
using ProjectAegis.Data.WriteGate;

// Parses sensor markdown, gates rows, seeds the Baltic catalog if the db is missing,
// and proposes one or more sensor batches (chunked at 500).
CmoMarkdownImportResult result = CmoMarkdownImportProposer.ProposeFromMarkdown(
    databasePath: "catalog.db",
    markdownPath: CmoMarkdownImporter.ResolveMiniFixturePath(),
    maxRecords:   null,
    chunkSize:    CmoMarkdownImportProposer.DefaultChunkSize,   // 500
    clock:        new FixedCatalogClock(42));

Console.WriteLine($"{result.ApprovedCount} staged, {result.QuarantinedCount} quarantined");

// Each batch then approves through the write gate (sensors commit to the live table).
using var gate = new CatalogWriteGate("catalog.db", new FixedCatalogClock(43));
foreach (var batch in result.Batches)
    gate.ApproveBatch(batch.BatchId, "human", "reviewer");
```

### Platform / weapon / mount staging (S22-04, library only)

```csharp
// Stages platform, weapon, and mount batches in one pass. Each may be null when the
// corresponding markdown yields no rows. No CLI verb wraps this path yet.
CmoMarkdownPlatformImportResult r = CmoMarkdownImportProposer.ProposePlatformWeaponMounts(
    databasePath:        "catalog.db",
    platformMarkdownPath: CmoMarkdownImporter.ResolveBalticPlatformFixturePath(),
    weaponMarkdownPath:   CmoMarkdownImporter.ResolveMiniWeaponFixturePath(),
    mapBalticPlatformIds: true);
```

### Parse only (no database)

```csharp
IReadOnlyList<CatalogSensorBinding> rows =
    CmoMarkdownImporter.ReadSensorBindings("docs/reference/cmano-db/sensor.md", maxRecords: 50);
```

Fixture resolvers: `ResolveMiniFixturePath`, `ResolveBalticPlatformFixturePath`,
`ResolveMiniWeaponFixturePath` (under `tools/cmano-db-crawler/fixtures/`), and
`ResolveReferenceSensorMarkdownPath` (`docs/reference/cmano-db/sensor.md`).

## CLI / operational runbook

The headless mission-editor exposes the **sensor** path as one verb
(`src/ProjectAegis.MissionEditor.Cli`, see
[`tools/mission-editor/README.md`](../../../tools/mission-editor/README.md)):

```bash
# Parse sensor markdown, gate it, and stage batches (seeds Baltic db if missing).
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  catalog_import_markdown --db catalog.db --markdown docs/reference/cmano-db/sensor.md \
  [--max-records N] [--chunk-size 500] [--report-out report.json]
# → { "ok": true, "parsedCount": .., "approvedCount": .., "quarantinedCount": ..,
#     "batchCount": N, "batches": [ { "batchId": .., "recordCount": .. } ],
#     "nextStep": "catalog_write_approve --db <path> --batch <batchId>",
#     "quarantineReport": [ ... ] }   ← only present when quarantinedCount > 0

# Then approve each staged batch through the write gate.
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  catalog_write_approve --db catalog.db --batch <batchId>
```

`--report-out` also writes the JSON payload to disk for QA evidence. The
platform/weapon/mount path (`ProposePlatformWeaponMounts`) has **no CLI verb** — it is
invoked from library code and tests only.

## Constraints & gotchas

- **Only sensors reach the live catalog on approve.** Sensor batches commit to the
  live `sensor` table; platform/weapon/mount batches *stage* but have **no commit
  path** in the write gate yet (see the [Write Gate README](../WriteGate/README.md)).
  Approving a platform/weapon/mount batch returns `staging_batch_not_found`.
- **Sensors are pre-approved, others are provisional.** Parsed sensors get
  `ReviewState = Approved` + `TrlLevel = 9`, so they clear the approve gate by default.
  Platforms, weapons, and mounts are `Provisional` — they would quarantine at approve
  time even if a commit path existed.
- **Quarantine is sensor-only.** `ProposeFromMarkdown` writes `catalog_quarantine` rows
  and a `QuarantineReport` only for sensors that fail the import gate (confidence
  `< 0.5`, TRL `< 4`, or `ReviewState != approved`). The report is sorted ordinally by
  `(PlatformId, SensorId)`.
- **Parsing never writes SQLite.** `CmoMarkdownImporter` is pure; only the *Proposer*
  opens the database (and seeds Baltic patrol if the file is absent — even with zero
  approved rows, since seeding happens before chunking).
- **Missing markdown throws; missing db is seeded.** A missing markdown path raises
  `FileNotFoundException`; a missing/blank `databasePath` raises `ArgumentException`.
  A missing weapon markdown path is treated as "no weapons" (empty), not an error.
- **Chunking is deterministic.** `ChunkBindings` sorts ordinally by
  `(PlatformId, SensorId)` then slices at `chunkSize` (min 1). 501 rows at the default
  500 → two batches; the same input always yields the same batch ids (given the clock).
- **Weapon ranges round up across lines.** `MaxRangeMeters` is the max of every
  `Air/Surface/Land/Sub Max: N km` line in the `**Weapons**` block, ×1000.

## Tests

| Area | Test |
|------|------|
| Unit: `InferBasePd` units + `SlugPlatformId` | `ProjectAegis.Data.Tests/Import/CmoMarkdownImporterUnitTests` |
| Parse platforms / weapons / mounts (+ Baltic ids) | `ProjectAegis.Data.Tests/Import/CmoMarkdownImporterTests` |
| Sensor parse + propose/approve round-trip | `ProjectAegis.Data.Tests/Import/CmoMarkdownImportProposerTests` |
| ≥10-row mini fixture + reference-subset smoke | `ProjectAegis.Data.Tests/Import/CmoMarkdownImportSmokeTests` |
| Bulk chunking + quarantine report population | `ProjectAegis.Data.Tests/Import/CmoMarkdownBulkImportTests` |
| CLI verb payload + report-out | `ProjectAegis.MissionEditor.Cli.Tests/CatalogImportMarkdownCommandTests` |

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "FullyQualifiedName~Import" -v minimal
```
