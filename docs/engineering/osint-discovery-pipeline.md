# OSINT discovery pipeline — developer reference

Developer/operator reference for the **OSINT discovery → proposal → staging pipeline**
(`src/ProjectAegis.Data/Osint/`) — the headless path that turns open-source-intelligence hits
(connector fixtures, RSS/file sources, or MCP on-demand search) into staged `CatalogSensorBinding`
rows. Like the [CMO markdown importer](cmo-markdown-catalog-import.md) and the platform editor's
[Excel round-trip](platform-editor-excel-roundtrip.md), it is a *front door* to the catalog: it
never writes SQLite directly. Every proposed row flows `discover → gate → propose → approve → commit`
through **`IWriteGate`** ([reference](catalog-write-gate.md)). The architectural boundary lives in
[ADR-006](../architecture/adr-006-data-layer-boundary.md); requirements are
[req-05 — Dynamic Systems Agent](../../Game-Requirements/requirements/05-Dynamic-Systems-Agent.md)
(the DSA-1.x / DSA-2.1 acceptance criteria) and
[req-06 — Database Intelligence](../../Game-Requirements/requirements/06-Database-Intelligence.md).

| Question | Answer |
|----------|--------|
| What does it do? | Normalizes OSINT hits into `OsintDiscoveryRecord`s, partitions them by confidence, and stages the high-confidence ones as write-gate **sensor** batches. |
| Where does the code live? | `src/ProjectAegis.Data/Osint/{OsintDiscoveryRecord,OsintProposalGate,OsintDigestRunner,OsintCatalogMapper}.cs` and `Osint/Connectors/`. |
| Does it touch SQLite? | Connectors and the proposal gate are pure (no DB). `OsintDigestRunner.RunFromDigestFile` opens the catalog only to call `IWriteGate.ProposeSensorBatch` — it commits nothing. |
| How do I drive it? | CLI verbs `osint_search` (discover + partition, no DB write) and `osint_staging_review` (list/approve staged batches). Both are also exposed as MCP tools. |
| What actually reaches the live catalog? | **Sensor rows only**, and only after a separate `osint_staging_review --approve`. See [Governance](#governance-propose--approve--commit). |
| Is there a real-time social listener? | **No.** `OsintDigestRunner.EnableRealtimeSocialStream = false` by design (DSA-1.3). |

## Why it exists

The Dynamic Systems Agent needs a way to fold new open-source observations (e.g. a near-future or
speculative sensor capability) into the catalog without bypassing governance. The pipeline reuses the
same staged write gate, quarantine, immutable snapshots, and append-only change log every other
catalog client uses, so OSINT-sourced rows are reviewable and reversible like any other proposal. It
is split into deterministic, single-purpose pieces (ADR-006):

- **Connectors** (`IOsintConnector`) — turn a source (in-memory fixture, local JSON file, RSS/HTTP
  stub) into a sorted `OsintDiscoveryRecord[]`. Pure, deterministic, never throw.
- **`OsintProposalGate`** — the doc-05 confidence gate (DSA-2.1): partition discoveries into
  *proposals* vs *log-only*.
- **`OsintDigestRunner`** — orchestration: read a digest file, dedupe, gate, seed a catalog if
  missing, and propose a sensor batch through `IWriteGate`.
- **`OsintCatalogMapper`** — TRL/branch routing (S22-07): map a record's `ProposedTrl` and
  `TargetDoc` (09 near-future / 10 speculative) onto a staged `CatalogSensorBinding`.

## Pipeline

```
OSINT source (fixture / data/osint_facts.json / RSS stub / MCP search)
      │  IOsintConnector.Fetch()  (pure, sorted by SourceUrl then CanonicalId)
      ▼
OsintDiscoveryRecord[]  ──►  OsintDigestRunner.DedupeDiscoveries  (by CanonicalId, keep highest score)
      │
      ▼
OsintProposalGate.Partition(minConfidence = 0.65)
      ├─ RelevanceScore ≥ threshold  → proposals
      └─ RelevanceScore <  threshold → logOnly (never staged)
      │  MapProposalsToBindings / OsintCatalogMapper.ToSensorBinding  (TRL + branch routing)
      ▼
IWriteGate.ProposeSensorBatch(bindings, "agent", "osint-digest-runner", "osint_digest:<file>")
      │  osint_staging_review --approve  ← separate, human-gated step (sensors commit today)
      ▼
committed catalog sensor rows
```

## Connectors (`Osint/Connectors/`)

All connectors implement `IOsintConnector.Fetch()` and **must be deterministic** — stable sort by
`SourceUrl` then `CanonicalId`, no network in the hot path, never throw (parse errors return empty).

| Connector | Source | Notes |
|-----------|--------|-------|
| `InMemoryOsintConnector` | constructor records, or a built-in 3-record demo fixture | Sprint 19 demo/test path. |
| `FileOsintConnector` | local JSON array file | Tolerant parser (case-insensitive keys, defaults). Missing/invalid file → empty. |
| `RssOsintConnector` | JSON array file, **or** a built-in demo record when no path is given | RSS/HTTP *stub*; fixture-driven. Explicit-but-missing path → empty. |

Field defaults applied by the file/RSS parsers when a key is absent: `relevanceScore` `0.5`,
`targetDoc` `"10"`, `proposedTrl` `5`.

## Discovery record (`OsintDiscoveryRecord`)

```csharp
record OsintDiscoveryRecord(
    string CanonicalId,   // "platformId/sensorId" or a single token
    string SourceUrl,     // citation; also the primary sort key
    string Snippet,       // human-readable evidence
    double RelevanceScore,// 0–1 confidence; drives the proposal gate
    string TargetDoc,     // "09" near-future, "10" speculative
    int    ProposedTrl,   // 1–9 technology readiness
    long   ObservedUtcTicks = 0);
```

## Proposal gate (`OsintProposalGate`)

`Partition(discoveries, minimumConfidence = 0.65)` clamps the threshold to `[0, 1]`, sorts inputs
(`SourceUrl` then `CanonicalId`), and splits on `RelevanceScore >= threshold`:

| Bucket | Condition | Fate |
|--------|-----------|------|
| `Proposals` | `RelevanceScore ≥ threshold` | mapped to bindings and staged |
| `LogOnly` | `RelevanceScore < threshold` | counted only; **never staged** |

`DefaultProposalConfidenceThreshold = 0.65`. The CLI/MCP `osint_search` path and the digest runner
both use this default.

## Digest runner (`OsintDigestRunner`)

`RunFromDigestFile(databasePath, digestPath, clock?)` is the staging entry point:

1. `ReadDiscoveries` parses the digest JSON (`{ "discoveries": [ … ] }`, camelCase, case-insensitive).
2. `DedupeDiscoveries` groups by `CanonicalId` and keeps the highest `RelevanceScore` (tie-break by
   `SourceUrl`, descending ordinal).
3. `OsintProposalGate.Partition` splits proposals vs log-only.
4. If there are proposals: seed a `Baltic-patrol` catalog via
   `CatalogSeedBootstrap.SeedBalticPatrol(overwrite: false)` when `databasePath` is missing, then
   `MapProposalsToBindings` and `ProposeSensorBatch(actorType "agent", actorId
   "osint-digest-runner", source "osint_digest:<file>")`.
5. Returns `OsintDigestRunResult(ParsedTotal, ProposalCount, LogOnlyCount, BatchId?)` — `BatchId` is
   `null` when nothing was proposed.

`ResolvePlatformSensorIds(canonicalId)` splits a `"platform/sensor"` id on the first `/`; an id with
no `/` becomes `(id, id)`. `MapProposalsToBindings` stages rows as `Provisional`, `BasePd 0.5`,
`SourceFactId "osint:<targetDoc>"`, `ValueTier InterpretedValue`, sorted by `(PlatformId, SensorId)`.

## TRL / branch routing (`OsintCatalogMapper`)

S22-07 added doc-aware routing so near-future (09) and speculative (10) provenance is encoded on the
staged row. This is the most recently changed piece of the subsystem.

| Helper | Behavior |
|--------|----------|
| `ResolveTrlLevel(proposedTrl)` | `Math.Clamp(proposedTrl, 1, 9)`. |
| `ResolveBranchTag(targetDoc)` | `"branch:doc-" + normalized` (carried in `ImportBatchId`). |
| `ResolveSourceFactId(targetDoc)` | `"osint:" + normalized`. |
| `NormalizeTargetDoc(targetDoc)` | blank → `"10"`; `"09"`/`"9"` → `"09"`; `"10"` → `"10"`; otherwise the trimmed value verbatim. |
| `ToSensorBinding(record, platformId = "osint-platform")` | builds a `Provisional` binding: `SensorId "osint-<canonical-id>"`, `BasePd` = `RelevanceScore` clamped to `[0.1, 0.95]`, `Confidence` = `RelevanceScore`, `ReviewerId "osint-digest"`, `TrlLevel` via `ResolveTrlLevel`, branch tag via `ResolveBranchTag`. |

> **Two mapping paths exist.** `OsintDigestRunner.MapProposalsToBindings` (used by
> `RunFromDigestFile`) splits the `CanonicalId` into platform/sensor and uses a fixed `BasePd 0.5`.
> `OsintCatalogMapper.ToSensorBinding` is the richer S22-07 mapper (relevance-scaled `BasePd`, branch
> tag, clamped TRL) intended for the doc-09/10 routing path. Pick the one whose provenance you need.

## Governance: propose → approve → commit

The runner and connectors only **stage**. Committing is the same separate, human-gated step as every
gate client:

- `osint_staging_review --db <catalog.db>` lists pending batches (`batchId`, `recordCount`,
  `actorType`, `approvalState`) via `IWriteGate.ListPendingBatches()`.
- `osint_staging_review --db <catalog.db> --approve <batchId>` calls
  `ApproveBatch(batchId, "human", "osint-ui-reviewer")` and returns `{ ok, batchId, errors }`.
- **Approve commits sensors only (today).** OSINT stages **sensor** batches, which the write gate
  *can* materialize — so the OSINT path is not blocked by the gate's
  [commit asymmetry](catalog-write-gate.md#commit-asymmetry-read-this), unlike platform/weapon/mount
  batches.
- **Determinism** — pass an `ICatalogClock` to `RunFromDigestFile` in tests; the default
  `FixedCatalogClock(0)` collapses every batch id to `…-0`, which collides across batches in a run.

## CLI / MCP

`osint_search` and `osint_staging_review` are CLI verbs
(`src/ProjectAegis.MissionEditor.Cli/Program.cs`, `OsintStagingReviewCommand.cs`) and are exposed in
the MCP manifest (`tools/mission-editor/mcp-tools.json`). Several MCP tool names are thin aliases over
these two verbs (see the table below).

```bash
# Discover + partition (no DB write). Uses data/osint_facts.json unless --db points at an existing file.
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  osint_search [--db <fixture.json>]

# List staged OSINT proposals
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  osint_staging_review --db <catalog.db>

# Approve (commit) a staged sensor batch — the commit step
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  osint_staging_review --db <catalog.db> --approve <batchId>
```

`osint_search` prints `{ ok, proposals[ { canonicalId, sourceUrl, relevanceScore, snippet } ],
logOnlyCount }`. Note its `--db` is a **fixture path**, not a catalog DB, and `osint_search` does not
stage anything — it is the discover/preview step. Use `OsintDigestRunner.RunFromDigestFile`
(library) to actually stage from a digest file.

| MCP tool | Backing verb | Purpose |
|----------|--------------|---------|
| `osint_search` | `osint_search` | discover + partition (preview) |
| `osint_digest` | `osint_search` | alias of search today |
| `osint_list_staging_proposals` | `osint_staging_review` | list pending batches |
| `osint_get_proposal_detail` | `osint_staging_review` | list pending batches |
| `osint_submit_review_decision` | `osint_staging_review --approve` | approve a batch |

The default digest fixture path is `OsintDigestRunner.ResolveFixtureDigestPath()` →
`src/ProjectAegis.Data.Tests/Fixtures/osint-digest-fixture.json`.

## Common pitfalls

- **`osint_search` is preview, not staging.** It runs the connector + proposal gate and prints
  proposals; nothing reaches the catalog. Staging happens in `RunFromDigestFile` /
  `ProposeSensorBatch`, committing in `osint_staging_review --approve`.
- **`--db` means different things per verb.** For `osint_search` it is a *fixture JSON* (overrides
  `data/osint_facts.json` only if it exists); for `osint_staging_review` it is the *catalog DB*.
- **Below-threshold hits vanish from staging.** `RelevanceScore < 0.65` goes to `logOnly` and is only
  counted. A lower-than-expected `ProposalCount` usually means low relevance, not a parse failure.
- **Dedup keeps one row per `CanonicalId`.** The highest-relevance hit wins; duplicate ids in a
  digest collapse silently.
- **Connectors never throw.** Missing/invalid source files return an empty array deterministically, so
  an empty result can mean "bad fixture" rather than "no hits".
- **No real-time stream.** `EnableRealtimeSocialStream` is a hard `false` (DSA-1.3); do not wire a
  live listener into the hot path.
- **Two binding mappers, two `BasePd` policies.** `MapProposalsToBindings` uses fixed `0.5`;
  `OsintCatalogMapper.ToSensorBinding` scales `BasePd` from relevance and adds the branch tag. Choose
  deliberately.
- **Determinism.** Inject an `ICatalogClock` for multi-batch runs in tests; the default fixed clock
  produces colliding batch ids.

## Related

- [Catalog write gate — developer reference](catalog-write-gate.md)
- [CMO markdown catalog import — developer reference](cmo-markdown-catalog-import.md)
- [Platform editor — Excel round-trip runbook](platform-editor-excel-roundtrip.md)
- [ADR-006 — Data layer boundary](../architecture/adr-006-data-layer-boundary.md)
- Requirement [05 — Dynamic Systems Agent](../../Game-Requirements/requirements/05-Dynamic-Systems-Agent.md),
  [06 — Database Intelligence](../../Game-Requirements/requirements/06-Database-Intelligence.md)
- Code: `src/ProjectAegis.Data/Osint/`,
  `src/ProjectAegis.MissionEditor.Cli/{Program.cs,OsintStagingReviewCommand.cs}`,
  `tools/mission-editor/mcp-tools.json`
