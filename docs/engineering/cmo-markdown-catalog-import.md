# CMO markdown catalog import — developer reference

Developer/operator reference for the **CMO markdown importer** (`src/ProjectAegis.Data/Import/`) —
the parser-plus-proposer that turns [cmano-db.com](https://cmano-db.com) markdown exports into
catalog rows and **stages them through the write gate**. Like the platform editor's Excel
round-trip ([runbook](platform-editor-excel-roundtrip.md)), it is a *front door* to the catalog: it
never writes SQLite directly. Every row flows `parse → propose → approve → commit` through
**`IWriteGate`** ([reference](catalog-write-gate.md)). The architectural boundary lives in
[ADR-006](../architecture/adr-006-data-layer-boundary.md); requirement coverage is
[req-06 — Database Intelligence](../../Game-Requirements/requirements/06-Database-Intelligence.md).

| Question | Answer |
|----------|--------|
| What does it do? | Parses CMO sensor / platform / weapon markdown into `ProjectAegis.Data.Catalog` records and stages them as write-gate batches. |
| Where does the code live? | `src/ProjectAegis.Data/Import/{CmoMarkdownImporter,CmoMarkdownImportProposer,CmoMarkdownQuarantineReportEntry}.cs`. |
| Does it touch SQLite? | The parser is pure (no DB). The **proposer** opens the catalog only to call `IWriteGate.Propose*Batch` — it commits nothing. |
| How do I drive it? | CLI verb `catalog_import_markdown` (**sensors only**). Platform/weapon/mount staging is library-only today (`CmoMarkdownImportProposer.ProposePlatformWeaponMounts`). |
| What actually reaches the live catalog? | **Sensor rows only**, and only after a separate `catalog_write_approve`. See [Governance](#governance-propose--approve--commit). |

## Why it exists

The catalog has deterministic governance (a staged write gate, quarantine, immutable snapshots, an
append-only change log) but its bulk source of truth is the cmano-db markdown corpus under
[`docs/reference/cmano-db/`](../reference/cmano-db/). The importer bridges the two: it reads that
corpus deterministically and hands rows to the same `propose → approve` gate every other client
uses. It is split in two so the parsing stays pure and unit-testable (ADR-006):

- **`CmoMarkdownImporter`** — static, dependency-free parser. Markdown text in, sorted catalog
  records out. No SQLite, no clock, no gate.
- **`CmoMarkdownImportProposer`** — Phase-2 orchestration. Parses, applies the TL/quarantine gate
  (sensors), seeds a catalog if missing, and proposes batches through `IWriteGate`.

## Pipeline

```
docs/reference/cmano-db/*.md  (or tools/cmano-db-crawler/fixtures/*.md)
      │  CmoMarkdownImporter.Read{Sensor,Platform,Weapon}Bindings / ReadPlatformMounts  (pure)
      ▼
sorted Catalog* records  ──(sensors only)──►  CatalogImportGate.PartitionForImport
      │                                              ├─ approved   → staged
      │                                              └─ quarantined → catalog_quarantine + report
      │  CmoMarkdownImportProposer  (seeds Baltic-patrol catalog if --db is missing)
      ▼
IWriteGate.Propose{Sensor,Platform,Weapon,Mount}Batch  ──►  staged batch id(s)
      │  catalog_write_approve  ← separate, human-gated step (sensors commit today)
      ▼
committed catalog rows
```

## Parsing rules (`CmoMarkdownImporter`)

A `### heading` opens a section; the section flushes to a record only when its mandatory id is
found. Sections without a matching id path are silently skipped. All numeric parsing uses
`InvariantCulture`; all outputs are sorted by their canonical key(s) with `StringComparer.Ordinal`.

| Reader | Id path regex | Produces | Key ReviewState / defaults |
|--------|---------------|----------|----------------------------|
| `ReadSensorBindings` | `/sensor/(\d+)/` | `CatalogSensorBinding` (`cmo-sensor-{n}`) | `Approved`, TRL 9, `Confidence` 0.85 (or `\| Confidence \| … \|` row), `InterpretedValue` |
| `ReadPlatformBindings` | `/(ship\|aircraft\|submarine\|facility)/(\d+)/` | `CatalogPlatformBinding` | `Provisional`, TRL 9, `Domain` via `InferDomain` |
| `ReadWeaponBindings` | `/weapon/(\d+)/` | `CatalogWeaponRecord` (`cmo-weapon-{n}`) | `Provisional`, `MaxRangeMeters` = max `… Max: N km` × 1000 |
| `ReadPlatformMounts` | (platform `**Weapons**` bullets) | `CatalogMount` | `Provisional`, `MountType` via `InferMountType` |

Derived values:

- **`InferBasePd(range, unit)`** — `nm → range/200`, `km → range/300`, `m → range/370400`, else `0.5`;
  clamped to `[0.05, 1.0]`.
- **`InferDomain(class)`** — `aircraft`/`helicopter → air`, `submarine → subsurface`,
  `facility`/`land → land`, else `surface`.
- **`InferMountType(type)`** — `gun → gun`, `torpedo`/`tube → tube`, `vls → vls`, else `rail`.
- **`SlugPlatformId` / `SlugWeaponMountId`** — lowercase, non-word runs → `-`, trimmed, capped at
  64 chars; the platform slug takes the title up to the first comma.
- **Baltic id mapping** (opt-in `mapBalticIds`): `patrol-frigate-u1 → u1`,
  `hostile-corvette-h1 → hostile-1`, `distant-hostile-frigate → hostile-far`, so reference markdown
  lines up with the seeded `Baltic-patrol` scenario ids.

## Proposing (`CmoMarkdownImportProposer`)

Both entry points default the clock to `FixedCatalogClock(0)` (deterministic) and seed a
`Baltic-patrol` catalog via `CatalogSeedBootstrap.SeedBalticPatrol(overwrite: false)` if `--db` does
not exist.

| Method | Parses | Gate applied at propose? | Batches proposed | Result record |
|--------|--------|--------------------------|------------------|---------------|
| `ProposeFromMarkdown(db, md, maxRecords?, chunkSize=500, clock?)` | sensors | **Yes** — `CatalogImportGate.PartitionForImport`; quarantined rows go to `catalog_quarantine` + report | one `ProposeSensorBatch` per ≤`chunkSize` chunk of approved rows | `CmoMarkdownImportResult(ParsedCount, ApprovedCount, QuarantinedCount, Batches, QuarantineReport)` |
| `ProposePlatformWeaponMounts(db, platformMd, weaponMd?, mapBalticPlatformIds=false, clock?)` | platforms, mounts (from `platformMd`), weapons (from `weaponMd`) | No (records are `Provisional`; these propose paths don't gate) | `ProposePlatformBatch` / `ProposeWeaponBatch` / `ProposeMountBatch`, each only when its list is non-empty | `CmoMarkdownPlatformImportResult(PlatformCount, WeaponCount, MountCount, PlatformBatchId, WeaponBatchId, MountBatchId)` |

Sensor staging is **chunked** (`DefaultChunkSize = 500`) so a large drop becomes several reviewable
batches rather than one. Approved rows are re-sorted by `(PlatformId, SensorId)` before chunking, so
chunk boundaries are deterministic for a given input.

### Quarantine reasons (sensors)

`PartitionForImport` rejects a sensor row with one of (`CatalogImportGate.GetRejectionReason`):

| Reason | Trigger (defaults) |
|--------|--------------------|
| `confidence_below_minimum` | `Confidence < 0.5` (`DefaultMinimumConfidence`) |
| `trl_below_minimum` | `TrlLevel < 4` (`DefaultMinimumTrl`) |
| `review_state_{state}` | `ReviewState != Approved` |

Because parsed sensors default to `Approved` / TRL 9 / confidence 0.85, the usual way a sensor
quarantines is a markdown `Confidence` row below `0.5`. Rejected rows are persisted via
`CatalogJsonImporter.WriteQuarantineRows` and surfaced as `CmoMarkdownQuarantineReportEntry`
(`PlatformId, SensorId, Reason, SourceFile`).

## Governance: propose → approve → commit

The proposer only **stages**. Committing is the same separate, human-gated step as every gate
client:

- `IWriteGate.ApproveBatch(batchId, actorType, actorId)` returns a `WriteGateDecision`
  (`Committed`, `BatchId`, `Errors`). Until then rows stay staged; `ListPendingBatches()` shows them.
- **Approve commits sensors only (today).** `CatalogWriteGate.ApproveBatch` materializes the sensor
  staging table exclusively, so platform/weapon/mount batches stage and are reviewable/rejectable but
  approving one returns `staging_batch_not_found` and commits nothing. This is the gate's
  [commit asymmetry](catalog-write-gate.md#commit-asymmetry-read-this) — not a parser bug.
- **Determinism** — pass an `ICatalogClock` in tests; the default `FixedCatalogClock(0)` makes every
  batch id collapse to `…-0`, which collides across batches in a single run.

## CLI / MCP

`catalog_import_markdown` is a **CLI verb** (`src/ProjectAegis.MissionEditor.Cli/CatalogImportMarkdownCommand.cs`)
and is **not** in the MCP manifest (`tools/mission-editor/mcp-tools.json`). It drives
`ProposeFromMarkdown`, i.e. **sensors only**.

```bash
# Stage sensor edits from CMO markdown (seeds a Baltic-patrol catalog if --db is missing)
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  catalog_import_markdown --db <catalog.db> --markdown <sensor.md> \
  [--max-records N] [--chunk-size 500] [--report-out report.json]

# Approve (commit) a staged sensor batch — the commit step
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  catalog_write_approve --db <catalog.db> --batch <batchId>
```

The verb prints an `McpToolResult`-style JSON object: `ok`, `parsedCount`, `approvedCount`,
`quarantinedCount`, `batchCount`, `batches[]`, a `nextStep` pointing at `catalog_write_approve`, and
(when non-empty) a `quarantineReport[]`. `--report-out` mirrors the same JSON to a file.

Reference markdown lives in [`docs/reference/cmano-db/`](../reference/cmano-db/) (`sensor.md`,
`ship.md`, `weapon.md`, …); small deterministic fixtures used by tests live in
`tools/cmano-db-crawler/fixtures/` (`sensor-mini.md`, `baltic-platform-mini.md`, `weapon-mini.md`).

## Common pitfalls

- **Sensors only via the CLI.** `catalog_import_markdown` ignores platforms/weapons even though the
  parser can read them. Platform/weapon/mount staging is library-only (`ProposePlatformWeaponMounts`)
  and, per the commit asymmetry, would not commit yet anyway.
- **Markdown shape is regex-driven.** Headings must be `### `; ids must match the exact path regex
  (`/sensor/N/`, `/ship/N/`, `/weapon/N/`). A section missing its id path is dropped without error —
  a lower-than-expected `parsedCount` usually means a heading/path didn't match, not a gate reject.
- **Quarantine ≠ commit.** `approvedCount` is the gate-passing count, but those rows are still only
  *staged*. Nothing is in the live catalog until `catalog_write_approve` returns `Committed = true`.
- **Default confidence is 0.85.** Sensors without a `Confidence` row pass the gate; add a row below
  `0.5` to intentionally quarantine, and expect `confidence_below_minimum` in the report.
- **`InvariantCulture` everywhere.** Localised decimals/thousand separators in source markdown will
  fail the numeric regexes and silently drop the value.
- **Determinism.** Inject an `ICatalogClock` for multi-batch runs in tests; the default fixed clock
  produces colliding batch ids.

## Related

- [Catalog write gate — developer reference](catalog-write-gate.md)
- [OSINT discovery pipeline — developer reference](osint-discovery-pipeline.md)
- [Platform editor — Excel round-trip runbook](platform-editor-excel-roundtrip.md)
- [ADR-006 — Data layer boundary](../architecture/adr-006-data-layer-boundary.md)
- Requirement [06 — Database Intelligence](../../Game-Requirements/requirements/06-Database-Intelligence.md)
- Code: `src/ProjectAegis.Data/Import/`,
  `src/ProjectAegis.Data/Catalog/CatalogImportGate.cs`,
  `src/ProjectAegis.MissionEditor.Cli/CatalogImportMarkdownCommand.cs`
