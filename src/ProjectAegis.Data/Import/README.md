# Import — CMO markdown → staged catalog rows

This subsystem parses **CMO markdown** (cmano-db.com exports) into catalog records
and **stages them through the [write gate](../WriteGate/README.md)** as
`propose` batches. It never writes SQLite directly — parsing and staging are kept
separate so the importer is a pure text transform and all mutation stays gated.

Two pieces:

| Type | Responsibility |
|------|----------------|
| `CmoMarkdownImporter` | Pure parser: markdown text → sorted `Catalog*` records. No SQLite. |
| `CmoMarkdownImportProposer` | Orchestrates parse → import-gate partition → write-gate `Propose*Batch`. |

## Parsing (`CmoMarkdownImporter`)

A static, side-effect-free parser. It walks `### `-delimited sections, extracts the
numeric entity id from the resource path, and emits records sorted by canonical key.
All number parsing uses `CultureInfo.InvariantCulture`.

| Method | Produces | Notes |
|--------|----------|-------|
| `ReadSensorBindings` / `…FromText` | `CatalogSensorBinding[]` | `sensor_id = cmo-sensor-{n}`; `base_pd` from `InferBasePd(range, unit)`; default confidence `0.85`; `ReviewState=Approved`, `TrlLevel=9`, `ValueTier=InterpretedValue`. |
| `ReadPlatformBindings` / `…FromText` | `CatalogPlatformBinding[]` | Domain from `InferDomain(type)`; `ReviewState=Provisional`, `TrlLevel=9`. Optional `mapBalticIds` remaps fixture slugs to Baltic ids. |
| `ReadWeaponBindings` / `…FromText` | `CatalogWeaponRecord[]` | `weapon_id = cmo-weapon-{n}`; max range parsed from the `**Weapons**` block (`… Max: N km` → meters); `ReviewState=Provisional`. |
| `ReadPlatformMounts` / `…FromText` | `CatalogMount[]` | One mount per weapon line under `**Weapons**`; `MountType` from `InferMountType(weaponType)`; `ReviewState=Provisional`. |

### Inference helpers (deterministic)

- `InferBasePd(range, unit)` — normalizes detection range to a probability and clamps to `[0.05, 1.0]`: `nm`→`/200`, `km`→`/300`, `m`→`/370400`, unknown→`0.5`.
- `SlugPlatformId(title)` / `SlugWeaponMountId(name)` — lowercase, non-word→`-`, trimmed, capped at 64 chars (empty → `unknown-platform`/`unknown-mount`).
- `InferDomain(class)` — `air` (aircraft/helicopter), `subsurface` (submarine), `land` (facility/land), else `surface`.
- `InferMountType(type)` — `gun`, `tube` (torpedo/tube), `vls`, else `rail`.
- `BalticPlatformIds` — fixed slug→id map applied only when `mapBalticIds: true` (e.g. `patrol-frigate-u1`→`u1`).

Fixture/reference path resolvers: `ResolveMiniFixturePath`,
`ResolveBalticPlatformFixturePath`, `ResolveMiniWeaponFixturePath`,
`ResolveReferenceSensorMarkdownPath`.

## Staging (`CmoMarkdownImportProposer`)

The proposer is the bridge from parsed records to staged write-gate batches. It
**auto-seeds** the Baltic Patrol catalog if the target DB does not exist, then opens
a `CatalogWriteGate` under an injected `ICatalogClock` (default `FixedCatalogClock(0)`
so staging is reproducible).

### `ProposeFromMarkdown(databasePath, markdownPath, maxRecords?, chunkSize=500, clock?)`

Sensor import path:

1. `CmoMarkdownImporter.ReadSensorBindings(markdownPath, maxRecords)`.
2. `CatalogImportGate.PartitionForImport` → `(approved, quarantined)`.
3. Chunk approved rows (`ChunkBindings`, ordinal-sorted, `chunkSize` per batch) and
   `gate.ProposeSensorBatch(chunk, "agent", "cmo-markdown-import", "catalog_import_markdown:<file>")`.
4. Write quarantined rows to the quarantine table (`CatalogJsonImporter.WriteQuarantineRows`).
5. Return `CmoMarkdownImportResult(ParsedCount, ApprovedCount, QuarantinedCount, Batches, QuarantineReport)`.

`QuarantineReport` is a sorted list of `CmoMarkdownQuarantineReportEntry(PlatformId,
SensorId, Reason, SourceFile)` — the structured JSON shape consumed by the
`catalog_import_markdown` CLI verb (S19-02).

### `ProposePlatformWeaponMounts(databasePath, platformMarkdownPath, weaponMarkdownPath?, mapBalticPlatformIds=false, clock?)`

S22-04 path: parses platforms, mounts, and (optionally) weapons, then stages each
non-empty set as its own batch via `ProposePlatformBatch` / `ProposeWeaponBatch` /
`ProposeMountBatch`. Returns `CmoMarkdownPlatformImportResult` with the parsed counts
and the (nullable) batch ids. **No auto-commit** — batches remain pending for review.

## Determinism & invariants

- The parser is pure and emits ordinally-sorted output; identical markdown yields
  identical records on any machine.
- The proposer never commits: it only stages `propose` batches and writes quarantine
  rows. Promotion to the live catalog is a separate write-gate approve.
- Time comes from the injected `ICatalogClock`, never `DateTime.UtcNow`.

## Tests

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter CmoMarkdown -v minimal
```

`src/ProjectAegis.Data.Tests/Import/` covers the parser (`CmoMarkdownImporterTests`,
`CmoMarkdownImporterUnitTests`), staging (`CmoMarkdownImportProposerTests`), bulk
import (`CmoMarkdownBulkImportTests`), and an end-to-end smoke (`CmoMarkdownImportSmokeTests`).

## See also

- [ProjectAegis.Data overview](../README.md)
- [WriteGate — staged catalog writes](../WriteGate/README.md) (`Propose*Batch`, `ICatalogClock`)
- [Catalog — records, readers & quarantine](../Catalog/README.md) (`CatalogImportGate`, record types)
- [OSINT ingestion pipeline](../Osint/README.md) (the other proposer source)
- [Requirement 06 — Database Intelligence](../../../Game-Requirements/requirements/06-Database-Intelligence.md)
- [ADR-006 — data-layer boundary](../../../docs/architecture/adr-006-data-layer-boundary.md)
