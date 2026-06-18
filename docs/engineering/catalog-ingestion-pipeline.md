# Catalog Ingestion Pipeline (CMO Markdown + OSINT)

> **Scope:** `ProjectAegis.Data` catalog *ingestion* — parsing external sources into staged proposals.
> **Authoritative ADR:** [ADR-006 — Data Layer Boundary](../architecture/adr-006-data-layer-boundary.md)
> **Requirements:** Req 06 — Database Intelligence (`DBI-1.x`), Req 21 — Platform Editor (`PLE`), doc 05 (`DSA-1.x` OSINT)
> **Last updated:** 2026-06-18

This runbook covers the **read/parse half** of the catalog data flow: turning a
cmano-db markdown export or an OSINT digest hit into `Catalog*` records, then handing
them to the write gate. The **write/commit half** (propose → approve → commit,
determinism, snapshots) lives in its companion doc,
[Catalog Write-Gate & Determinism](catalog-write-gate-determinism.md).

It exists because the importer (`CmoMarkdownImporter`, ~560 LOC after the S22-04
platform/weapon/mount work) and the OSINT mapper (`OsintCatalogMapper`, S22-07 TL
routing) had only scattered sprint-plan references — no developer-facing description of
the markdown format they expect, the values they infer, or how rows get quarantined.

## End-to-end flow

```text
cmano-db.com export (.md)          OSINT digest / connector hit
        │                                      │
        ▼                                      ▼
 CmoMarkdownImporter                    OsintCatalogMapper
 Read{Sensor,Platform,Weapon,Mount}     ToSensorBinding(s)
        │                                      │
        ▼                                      │
 Catalog* records ◄────────────────────────────┘
        │
        ▼   (sensor path only)
 CatalogImportGate.PartitionForImport ──► quarantine (confidence / TRL / review-state)
        │ approved
        ▼
 CatalogWriteGate.Propose*Batch  ──►  catalog_staging_*  (returns batchId)
        │
        ▼
 ApproveBatch / RejectBatch   (see write-gate runbook)
```

**Key boundary:** neither parser touches SQLite. They return immutable records; all
persistence goes through [`IWriteGate`](../../src/ProjectAegis.Data/WriteGate/IWriteGate.cs)
(ADR-006 §2). The orchestrator that wires parse → partition → propose is
[`CmoMarkdownImportProposer`](../../src/ProjectAegis.Data/Import/CmoMarkdownImportProposer.cs).

## CMO markdown source: [`CmoMarkdownImporter`](../../src/ProjectAegis.Data/Import/CmoMarkdownImporter.cs)

A cmano-db export is a flat markdown file. Each `### Heading` opens a **section** whose
following lines are scanned until the next heading; the section is then *flushed* into a
record. The importer is line-oriented and regex-driven — no markdown AST.

### Entry points

| Method | Produces | Default review state |
|--------|----------|----------------------|
| `ReadSensorBindings` | `CatalogSensorBinding[]` | `approved` (TRL 9) |
| `ReadPlatformBindings` | `CatalogPlatformBinding[]` | `provisional` (TRL 9) |
| `ReadWeaponBindings` | `CatalogWeaponRecord[]` | `provisional` |
| `ReadPlatformMounts` | `CatalogMount[]` | `provisional` |

Each has a `…FromText` overload (used by tests) and accepts an optional `maxRecords`
cap. Results are always returned in the same ordinal sort order the write gate expects
(see the [sort-keys table](catalog-write-gate-determinism.md#sort-keys-per-entity-type)).

### What each parser reads

- **Sensors** — section heading → platform slug; `/sensor/<N>/` link → numeric id;
  `| Range Max | <value> <km\|m\|nm> |` row; optional `| Confidence | <c> |` row
  (defaults to `0.85`). `BasePd` is derived from range via `InferBasePd`. Emitted as
  `approved`, TRL 9, `interpreted_value`, `SourceFactId = cmano-db:sensor/<N>`,
  `CitationRef = /sensor/<N>/`, `ReviewerId = cmo-markdown-import`.
- **Platforms** — heading → display name + slug; `/ship|aircraft|submarine|facility/<N>/`
  link → numeric id; `| Type | … |` → `PlatformClass` and inferred `Domain`;
  `| Nationality | … |`. Emitted as `provisional`, TRL 9.
- **Weapons** — only lines *after* a `**Weapons**` marker within a section are scanned;
  `/weapon/<N>/` link → `WeaponId = cmo-weapon-<N>`; `| Type | … |` → `WeaponType`;
  `MaxRangeMeters` = the largest `(Air|Surface|Land|Sub) Max: <km> km` value × 1000.
- **Mounts** — `**Weapons**` bullet lines of the form `- <name> — <type> — …`; one
  `CatalogMount` per bullet with `MountId` slugged from the weapon name and `MountType`
  inferred from the weapon type.

### Inference rules (verify here before relying on a value)

| Helper | Rule |
|--------|------|
| `InferBasePd(range, unit)` | `nm → range/200`, `km → range/300`, `m → range/370400`, else `0.5`; clamped to `[0.05, 1.0]` |
| `InferDomain(class)` | `aircraft`/`helicopter` → `air`; `submarine` → `subsurface`; `facility`/`land` → `land`; else `surface` |
| `InferMountType(type)` | `gun` → `gun`; `torpedo`/`tube` → `tube`; `vls` → `vls`; else `rail` |
| `SlugPlatformId(title)` | text before first comma, lower-cased, non-word → `-`, trimmed, capped at 64 chars |

### Baltic id remapping

`ReadPlatformBindings`/`ReadPlatformMounts` accept `mapBalticIds: true`, which rewrites a
small fixed set of slugs (e.g. `patrol-frigate-u1 → u1`) so imported platforms line up
with the seeded `SeedBalticPatrol` scenario ids. Leave it `false` for general imports.

### Fixtures

Round-trip fixtures live under `tools/cmano-db-crawler/fixtures/`:
[`baltic-platform-mini.md`](../../tools/cmano-db-crawler/fixtures/baltic-platform-mini.md),
[`weapon-mini.md`](../../tools/cmano-db-crawler/fixtures/weapon-mini.md), and
`sensor-mini.md`. They are the canonical examples of the expected markdown shape — copy
their layout when adding new test data. `ResolveReferenceSensorMarkdownPath()` points at
the larger `docs/reference/cmano-db/sensor.md` reference export.

## OSINT source: [`OsintCatalogMapper`](../../src/ProjectAegis.Data/Osint/OsintCatalogMapper.cs)

OSINT digest/connector hits arrive as
[`OsintDiscoveryRecord`](../../src/ProjectAegis.Data/Osint/OsintDiscoveryRecord.cs)
`(CanonicalId, SourceUrl, Snippet, RelevanceScore, TargetDoc, ProposedTrl, …)`.
`ToSensorBinding` maps one record to a `CatalogSensorBinding`:

| Field | Value |
|-------|-------|
| `SensorId` | `osint-<canonicalId>` (spaces → `-`, lower-cased) |
| `BasePd` / `Confidence` | `RelevanceScore` (BasePd clamped to `[0.1, 0.95]`) |
| `TrlLevel` | `ResolveTrlLevel(ProposedTrl)` → clamped to `[1, 9]` |
| `ImportBatchId` | `ResolveBranchTag(TargetDoc)` → `branch:doc-09` / `branch:doc-10` |
| `SourceFactId` | `osint:<09\|10>` |
| `ReviewState` | `provisional`, `ReviewerId = osint-digest`, tier `interpreted_value` |

`TargetDoc` is normalized: `"9"`/`"09" → "09"` (near-future), `"10" → "10"`
(speculative), blank → `"10"`. This **TL routing** is how doc-09 vs doc-10 provenance
gates are encoded on staged rows — the branch tag and `SourceFactId` carry the routing
downstream, the mapper itself never decides admission.

> ⚠️ OSINT rows are emitted **`provisional`**, so the import gate's default
> `requireApproved` check (below) quarantines them with `review_state_provisional` until a
> reviewer promotes them. This is intentional: OSINT never auto-commits to the catalog.

## The import gate (sensor quarantine): [`CatalogImportGate`](../../src/ProjectAegis.Data/Catalog/CatalogImportGate.cs)

`PartitionForImport` splits sensor bindings into `(Approved, Quarantined)` against three
thresholds. **`ProposeFromMarkdown` runs this on the sensor path only** — platform,
weapon, and mount batches are staged directly (gating for those entities is pending
`S23-04` multi-entity approve).

| Default | Constant | Rejection reason when failed |
|---------|----------|------------------------------|
| Confidence ≥ `0.5` | `DefaultMinimumConfidence` | `confidence_below_minimum` |
| TRL ≥ `4` | `DefaultMinimumTrl` | `trl_below_minimum` |
| `ReviewState == approved` (when `requireApproved`) | — | `review_state_<state>` |

Quarantined rows are written to the quarantine table via
`CatalogJsonImporter.WriteQuarantineRows` and summarized in the result's
`QuarantineReport` ([`CmoMarkdownQuarantineReportEntry`](../../src/ProjectAegis.Data/Import/CmoMarkdownQuarantineReportEntry.cs)).
CMO-parsed sensors pass cleanly because they are emitted `approved` / TRL 9; OSINT and
low-confidence sources are the ones that quarantine.

## Running an import (CLI / MCP)

`ProposeFromMarkdown` is exposed as the `catalog_import_markdown` CLI verb
([`CatalogImportMarkdownCommand`](../../src/ProjectAegis.MissionEditor.Cli/CatalogImportMarkdownCommand.cs);
dispatched in [`Program.cs`](../../src/ProjectAegis.MissionEditor.Cli/Program.cs)):

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  catalog_import_markdown --db catalog.db --markdown docs/reference/cmano-db/sensor.md \
  [--max-records N] [--chunk-size 500] [--report-out report.json]
```

It prints JSON with `parsedCount`, `approvedCount`, `quarantinedCount`, the staged
`batches`, an optional `quarantineReport`, and a `nextStep` pointing at
`catalog_write_approve`. **Propose only** — nothing lands in a live table until you
approve each batch:

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  catalog_write_approve --db catalog.db --batch <batchId>
```

Sensor proposals are chunked at `CmoMarkdownImportProposer.DefaultChunkSize` (500) so a
large export produces several batches; if the target DB is missing, `SeedBalticPatrol`
bootstraps it first.

## Testing & verification

| Test file | Covers |
|-----------|--------|
| `Import/CmoMarkdownImporterUnitTests.cs` | parse rules, inference helpers, slugging |
| `Import/CmoMarkdownImporterTests.cs` | platform/weapon/mount section parsing (S22-04) |
| `Import/CmoMarkdownImportProposerTests.cs` | partition + chunk + propose orchestration |
| `Import/CmoMarkdownImportSmokeTests.cs` / `CmoMarkdownBulkImportTests.cs` | end-to-end propose against a seeded DB |
| `Catalog/CatalogImportGateTests.cs` | quarantine thresholds and reasons |
| `Osint/OsintCatalogMapperTests.cs` | TRL clamping, branch-tag / `SourceFactId` routing |

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "Import|Osint|CatalogImportGate" -v minimal
```

## Common pitfalls

| Pitfall | Symptom | Fix |
|---------|---------|-----|
| Expecting platforms/weapons/mounts to pass the import gate | They are staged regardless of confidence/TRL | Only the **sensor** path runs `PartitionForImport` today; multi-entity gating is `S23-04` |
| Expecting OSINT rows to commit | They quarantine as `review_state_provisional` | Promote/approve first; OSINT is provisional by design |
| Heading without a `/sensor|ship|weapon/<N>/` link | Section silently dropped (no numeric id → no flush) | Ensure every section carries its cmano-db path link |
| Weapon range read as 0 | Range line sits outside a `**Weapons**` block | Weapon ranges are only read inside the `**Weapons**` section |
| Assuming import writes SQLite directly | No rows appear until approval | Import **proposes**; run `catalog_write_approve` per batch |
| Locale-sensitive number parsing | Range/confidence mis-parsed | Parsing uses `InvariantCulture`; keep source values `.`-decimal |

## See also

- [Catalog Write-Gate & Determinism](catalog-write-gate-determinism.md) — the commit half (propose → approve → commit, sort keys, snapshots)
- [Balance Drift Telemetry (Advisory)](balance-drift-telemetry.md) — advisory drift sink; never writes the catalog
- [ADR-006 — Data Layer Boundary](../architecture/adr-006-data-layer-boundary.md)
- `Game-Requirements/requirements/06-Database-Intelligence.md`, `21-Platform-Editor.md`
- Mission Editor CLI/MCP reference: [`tools/mission-editor/README.md`](../../tools/mission-editor/README.md)
