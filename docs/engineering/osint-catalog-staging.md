# OSINT discovery → catalog staging subsystem

> **Engineering reference + runbook.** This page documents how the OSINT ingestion path (`src/ProjectAegis.Data/Osint/`) behaves today and how to drive it. The discovery/proposal model derives from [Req 05 — Dynamic Systems Agent](../../Game-Requirements/requirements/05-Dynamic-Systems-Agent.md) (doc 05, DSA-1.x/2.x); the staged-write contract it rides on is [ADR-006 — Data layer boundary](../architecture/adr-006-data-layer-boundary.md) and [Req 06 — Database Intelligence](../../Game-Requirements/requirements/06-Database-Intelligence.md). It shares the [`CatalogWriteGate`](../../src/ProjectAegis.Data/WriteGate/CatalogWriteGate.cs) with the [CMO markdown import](cmo-markdown-catalog-import.md) and [Platform Editor Excel round-trip](platform-editor-excel-roundtrip.md) paths.

This subsystem turns **open-source intelligence (OSINT) discoveries** — RSS/HTTP hits, fixture files, or MCP on-demand searches — into provisional `sensor` catalog rows and **stages them through the write gate**. It never writes SQLite directly and never auto-commits: a confidence gate splits discoveries into *proposals* (high-relevance, staged) and *log-only* (below threshold, discarded), and every proposal lands as a pending batch a human must review and approve.

## Intent

| Goal | How it is met |
|------|---------------|
| Public reference data, never blind writes | Discoveries become `CatalogSensorBinding` rows staged via `IWriteGate.ProposeSensorBatch`; there is no path from a connector to a live table that bypasses the gate (ADR-006, DBI-1) |
| Signal vs noise | `OsintProposalGate.Partition` keeps only discoveries with `RelevanceScore >= threshold` (default `0.65`); the rest are counted as log-only and dropped |
| No 24/7 social listener (DSA-1.3) | `OsintDigestRunner.EnableRealtimeSocialStream` is a compile-time `false`; ingestion is pull-based (digest file or on-demand fetch), never a standing real-time stream |
| Provenance on every row | Staged rows carry `SourceFactId` (`osint:<doc>`), `CitationRef`/`SourceFile` (origin URL/file), `ValueTier = InterpretedValue`, and `ReviewState = Provisional` |
| Deterministic | Every connector, the proposal gate, the runner, and the mapper sort `Ordinal` by `SourceUrl`/`CanonicalId` (or `PlatformId`/`SensorId`); no wall clock — the write gate's timestamp comes from an injected `ICatalogClock` |

## Architecture at a glance

```
IOsintConnector.Fetch()                              ← source (File / Rss / InMemory), deterministic
        │  OsintDiscoveryRecord[]
        ▼
OsintProposalGate.Partition(discoveries, threshold)  (confidence gate, DSA-2.1)
        │  (Proposals[], LogOnly[])                   proposals = RelevanceScore >= threshold
        ▼
OsintDigestRunner.MapProposalsToBindings(...)        (proposal → CatalogSensorBinding, provisional)
        │  CatalogSensorBinding[]
        ▼
CatalogWriteGate.ProposeSensorBatch(...)             ← staged batch (catalog_staging_sensor), NOT committed
        │  batchId
        ▼
OsintStagingReviewCommand.Run(db, approve)           (headless review proxy — list / approve)
        ▼
CatalogWriteGate.ApproveBatch(batchId)               ← commit path (import gate quarantines unqualified rows)
```

### Key types

| Type | Location | Responsibility |
|------|----------|----------------|
| `OsintDiscoveryRecord` | `src/ProjectAegis.Data/Osint/OsintDiscoveryRecord.cs` | Normalized digest/on-demand hit: `CanonicalId`, `SourceUrl`, `Snippet`, `RelevanceScore`, `TargetDoc`, `ProposedTrl`, `ObservedUtcTicks` |
| `IOsintConnector` | `src/ProjectAegis.Data/Osint/Connectors/IOsintConnector.cs` | `Fetch() => OsintDiscoveryRecord[]`; all impls must be deterministic |
| `FileOsintConnector` / `RssOsintConnector` / `InMemoryOsintConnector` | `src/ProjectAegis.Data/Osint/Connectors/` | Fixture/JSON-array source, RSS/HTTP stub (fixture-driven), and in-memory source. None hit the network in the hot path |
| `OsintProposalGate` | `src/ProjectAegis.Data/Osint/OsintProposalGate.cs` | Static confidence gate: `Partition` → `(Proposals, LogOnly)` at `DefaultProposalConfidenceThreshold = 0.65` |
| `OsintDigestRunner` | `src/ProjectAegis.Data/Osint/OsintDigestRunner.cs` | Orchestrates connector/file → gate → write-gate staging; dedupe, parsing, `MapProposalsToBindings`, `RunFromDigestFile` |
| `OsintCatalogMapper` | `src/ProjectAegis.Data/Osint/OsintCatalogMapper.cs` | **S22-07 TL-routing mapper**: TRL clamp, doc-09/10 branch tag, `osint:<doc>` source-fact, relevance-derived `BasePd` (see "Two mappers" below) |
| `OsintStagingReviewCommand` | `src/ProjectAegis.MissionEditor.Cli/OsintStagingReviewCommand.cs` | Headless review proxy: list pending batches, approve by id (mirrors what a Unity staging UI would call) |
| `CatalogWriteGate` / `IWriteGate` | `src/ProjectAegis.Data/WriteGate/` | Staged propose → approve → commit gate (req-06) |
| `CatalogImportGate` | `src/ProjectAegis.Data/Catalog/CatalogImportGate.cs` | TRL/confidence/review-state quarantine applied at `ApproveBatch` time |

## Discovery model

`OsintDiscoveryRecord` is the unit of ingestion:

| Field | Meaning |
|-------|---------|
| `CanonicalId` | Catalog identity. The runner splits it on the first `/` into `(PlatformId, SensorId)` via `ResolvePlatformSensorIds` (e.g. `u-hypersonic/radar-glide` → platform `u-hypersonic`, sensor `radar-glide`); with no `/`, both default to the whole id |
| `SourceUrl` | Origin link; becomes `CitationRef` / `SourceFile` and drives ordinal ordering |
| `Snippet` | Human-facing context (surfaced by `osint_search`, not persisted on the staged row) |
| `RelevanceScore` | `[0,1]` confidence; the proposal-gate threshold and the staged `Confidence` |
| `TargetDoc` | Provenance gate `09` (near-future) or `10` (speculative); normalized so `"9"`/`"09"` → `09`, blank → `10` |
| `ProposedTrl` | Proposed Technology Readiness Level (1–9), staged as `TrlLevel` |

## Connectors (source)

All connectors implement `IOsintConnector` and return a **stable ordinal-sorted** array (`SourceUrl`, then `CanonicalId`); none throw and none touch the network in the hot path:

- **`FileOsintConnector(path)`** — reads a JSON **array** of discovery objects (tolerant of camel/Pascal casing, missing fields default). Missing/unreadable/non-array file → empty array (deterministic).
- **`RssOsintConnector(path = "")`** — RSS/HTTP **stub**. With no path it returns a single deterministic demo record; with a path it parses the same JSON-array shape as `FileOsintConnector`; an explicit-but-missing path → empty.
- **`InMemoryOsintConnector`** — in-memory records for tests/demos.

> The digest-**file** path (`OsintDigestRunner.RunFromDigestFile`) reads a different JSON shape — an object with a `discoveries` array (see fixture below) — via `ReadDiscoveries`, not via a connector.

## Confidence gate (signal vs noise)

`OsintProposalGate.Partition(discoveries, minimumConfidence = 0.65)`:

- Clamps the threshold to `[0,1]`, iterates in ordinal order, and routes each record to **Proposals** (`RelevanceScore >= threshold`) or **LogOnly** (below). Log-only records are counted, never staged.
- `OsintDigestRunner` exposes the threshold via its constructor (`new OsintDigestRunner(0.65)`); the CLI and demos use `0.65`.

## Staging (write path)

`OsintDigestRunner.RunFromDigestFile(databasePath, digestPath, clock?)`:

1. Parses the digest file (`ReadDiscoveries`), then **dedupes** by `CanonicalId` (`DedupeDiscoveries` keeps the highest `RelevanceScore`, breaking ties by `SourceUrl` descending).
2. Partitions via `OsintProposalGate.Partition`.
3. If there are proposals: seeds the DB with `CatalogSeedBootstrap.SeedBalticPatrol` when the file is missing, maps proposals to `CatalogSensorBinding[]` (`MapProposalsToBindings`), and stages **one batch** through `CatalogWriteGate.ProposeSensorBatch(bindings, "agent", "osint-digest-runner", "osint_digest:<file>")`.
4. Returns `OsintDigestRunResult(ParsedTotal, ProposalCount, LogOnlyCount, BatchId?)`. `BatchId` is `null` when there are no proposals — **the write gate is never opened for an empty/all-log-only digest.**

`MapProposalsToBindings` stages each proposal as a provisional sensor row: `BasePd = 0.5`, `SourceFactId = "osint:<targetDoc>"`, `Confidence = RelevanceScore`, `TrlLevel = ProposedTrl` (no clamp on this path), `ReviewState = Provisional`, `ValueTier = InterpretedValue`, `CitationRef = SourceUrl`.

### Two mappers — important

There are **two** discovery→binding mappers, and only one is currently wired into the runner:

| Mapper | Used by runner today? | TRL | `BasePd` | `ImportBatchId` (branch tag) |
|--------|-----------------------|-----|----------|------------------------------|
| `OsintDigestRunner.MapProposalsToBindings` | **Yes** (file/CLI path) | `ProposedTrl` (unclamped) | constant `0.5` | not set |
| `OsintCatalogMapper.ToSensorBinding(s)` (S22-07) | **No — only exercised by tests** | `Clamp(ProposedTrl, 1, 9)` | `clamp(RelevanceScore, 0.1, 0.95)` | `branch:doc-09` / `branch:doc-10` (doc routing) |

`OsintCatalogMapper` is the newer **TL-routing** mapper: it clamps TRL, derives `BasePd` from relevance, and tags staged rows with a `branch:doc-<09|10>` import batch for doc-09/10 provenance branches (`ResolveBranchTag`, `ResolveTrlLevel`, `ResolveSourceFactId`). It is **not yet** the path `RunFromDigestFile`/the CLI use — do not document branch-tag routing as the live digest behavior until the runner adopts it.

## Approve (commit path)

`OsintStagingReviewCommand.Run(databasePath, batchIdToApprove?, output)`:

- Missing DB → `{ ok: false, error: "database_not_found" }`, exit `1`.
- **No `--approve`** → lists pending batches (`gate.ListPendingBatches()`) as `{ ok: true, pending: [...] }`.
- **`--approve <batchId>`** → `gate.ApproveBatch(batchId, "human", "osint-ui-reviewer")` and emits `{ ok, batchId, errors }`. Exit code is `0` only when `Committed == true`.

`ApproveBatch` runs the staged rows through `CatalogImportGate.PartitionForImport` (defaults: `minimumConfidence = 0.5`, `minimumTrl = 4`, `requireApproved = true`). **If any row is quarantined the whole batch is rejected** (`Committed = false`) with reasons; otherwise approved rows are upserted into `sensor` with a `catalog_change_log` entry and a deterministic snapshot is recorded.

> **Provisional rows quarantine by default.** OSINT rows are staged with `ReviewState = Provisional`, but `ApproveBatch` defaults to `requireApproved = true`, so a freshly staged OSINT batch quarantines with `review_state_provisional` (also `trl_below_minimum` when `ProposedTrl < 4`, `confidence_below_minimum` when relevance `< 0.5`). Promotion of provisional rows to `approved` is a separate concern (see `CatalogQuarantinePromoter`); the staging-review approve does not itself flip review state.

## Operational runbook

```bash
# 1. On-demand search (MCP search_osint fallback) — uses data/osint_facts.json or --db override.
#    Prints proposals (>= 0.65 relevance) + log-only count; does NOT stage.
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- osint_search [--db <fixture.json>]

# 2. Stage a digest file through the write gate (programmatic; seeds DB if missing).
#    OsintDigestRunner.RunFromDigestFile(dbPath, digestPath) -> OsintDigestRunResult.
#    Fixture digest: src/ProjectAegis.Data.Tests/Fixtures/osint-digest-fixture.json

# 3. Review pending batches (list).
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- osint_staging_review --db <catalog.db>

# 4. Approve a specific staged batch (commit if it clears the import gate).
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- osint_staging_review --db <catalog.db> --approve <batchId>
```

### Digest file shape (`RunFromDigestFile` / `ReadDiscoveries`)

```json
{
  "discoveries": [
    {
      "canonicalId": "u-hypersonic/radar-glide",
      "sourceUrl": "https://example.org/a",
      "snippet": "hypersonic glide vehicle radar concept",
      "relevanceScore": 0.72,
      "targetDoc": "09",
      "proposedTrl": 6
    }
  ]
}
```

## Common pitfalls

- **Approve returns `ok: false` with `quarantine:…:review_state_provisional`.** Expected — OSINT rows stage as `Provisional` and `ApproveBatch` requires `approved` by default. This is the gate working, not a bug; promote/qualify rows before expecting a commit.
- **`BatchId` is `null` after a run.** No discovery cleared the `0.65` threshold (all log-only) or the digest was empty — the write gate is intentionally never opened.
- **Sensor split looks wrong.** `ResolvePlatformSensorIds` splits `CanonicalId` on the **first** `/`; an id without `/` yields `platform == sensor == id`. Encode `platform/sensor` deliberately.
- **Branch-tag (`branch:doc-09/10`) missing on staged rows.** The digest/CLI path uses `MapProposalsToBindings`, which does not set branch tags. Branch routing lives in `OsintCatalogMapper`, which is not yet wired into the runner.
- **Expecting a live social stream.** `EnableRealtimeSocialStream` is `false` by design (DSA-1.3). Ingestion is pull-based; do not add a standing real-time listener under this path.
- **Connector "returned nothing".** Connectors swallow parse/IO errors and return an empty array deterministically (no throw). Check the path, JSON shape (array vs `{ discoveries: [...] }`), and that the file exists.

## Where things live

| Path | Content |
|------|---------|
| `src/ProjectAegis.Data/Osint/OsintDiscoveryRecord.cs` | Discovery record type |
| `src/ProjectAegis.Data/Osint/Connectors/*.cs` | `IOsintConnector` + File/Rss/InMemory sources |
| `src/ProjectAegis.Data/Osint/OsintProposalGate.cs` | Confidence gate (`Partition`) |
| `src/ProjectAegis.Data/Osint/OsintDigestRunner.cs` | Connector/file → gate → write-gate staging |
| `src/ProjectAegis.Data/Osint/OsintCatalogMapper.cs` | S22-07 TL-routing mapper (TRL clamp, doc-09/10 branch tag) |
| `src/ProjectAegis.MissionEditor.Cli/OsintStagingReviewCommand.cs` | Headless list/approve review proxy |
| `src/ProjectAegis.MissionEditor.Cli/Program.cs` | `osint_search`, `osint_staging_review` CLI/MCP verbs |
| `src/ProjectAegis.Data/WriteGate/CatalogWriteGate.cs` | Staged propose/approve/commit gate |
| `src/ProjectAegis.Data/Catalog/CatalogImportGate.cs` | TRL/confidence/review-state quarantine at approve time |
| `src/ProjectAegis.Data.Tests/Fixtures/osint-digest-fixture.json` | Reference digest fixture |
| `Game-Requirements/requirements/05-Dynamic-Systems-Agent.md` | OSINT / dynamic systems requirement (doc 05) |

## Verify

```bash
# OSINT mapping, gate, runner, connectors (pure, no Unity)
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "FullyQualifiedName~Osint" -v minimal

# Confirm the realtime social stream stays disabled (DSA-1.3)
rg "EnableRealtimeSocialStream\s*=\s*false" src/ProjectAegis.Data/Osint/OsintDigestRunner.cs
```
