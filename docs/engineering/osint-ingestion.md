# OSINT ingestion pipeline — developer guide

The **OSINT ingestion pipeline** turns open-source discovery records (fixtures, files, or RSS-style
feeds) into **staged, provisional** catalog sensor proposals that a human must approve through the
[catalog write gate](catalog-write-gate.md). It never writes the catalog directly, never opens a
real-time social listener, and is fully deterministic — the same digest always produces the same
proposal batch.

- **Source:** [`src/ProjectAegis.Data/Osint/`](../../src/ProjectAegis.Data/Osint/) —
  [`OsintDigestRunner`](../../src/ProjectAegis.Data/Osint/OsintDigestRunner.cs) (orchestrator),
  [`OsintProposalGate`](../../src/ProjectAegis.Data/Osint/OsintProposalGate.cs) (confidence gate),
  [`OsintCatalogMapper`](../../src/ProjectAegis.Data/Osint/OsintCatalogMapper.cs) (TL-routing map),
  [`OsintDiscoveryRecord`](../../src/ProjectAegis.Data/Osint/OsintDiscoveryRecord.cs) (the record),
  and [`Connectors/`](../../src/ProjectAegis.Data/Osint/Connectors/) (File / RSS / InMemory).
  The CLI/MCP proxy is
  [`OsintStagingReviewCommand`](../../src/ProjectAegis.MissionEditor.Cli/OsintStagingReviewCommand.cs).
- **Related:** the propose → approve → commit workflow this feeds is
  [`catalog-write-gate.md`](catalog-write-gate.md); the `osint_search` / `osint_staging_review`
  verbs are in [`mission-editor-cli.md`](mission-editor-cli.md); the public-citation firewall this
  pipeline respects is [`dual-track-cmo-analysis-and-catalog.md`](dual-track-cmo-analysis-and-catalog.md);
  scratch-DB bootstrap is [`catalog-seeding.md`](catalog-seeding.md). Requirements/decisions:
  doc 05 (Dynamic Systems Agent, DSA-1.x/2.1) and
  [`docs/adr/s41-structural-debt-decision-telemetry-osint.md`](../adr/s41-structural-debt-decision-telemetry-osint.md).

> **Provenance discipline (doc 05 / dual-track).** OSINT is a **public-citation feed**: every staged
> row is `Provisional` review state, `InterpretedValue` provenance tier, and carries a `CitationRef`
> back to its source URL. It reaches the production catalog only via a human write-gate approval —
> the pipeline itself only *stages*. Never wire it to proprietary CMO `*.db3` data.

---

## The discovery record

Every connector and the digest reader normalize to one immutable record
([`OsintDiscoveryRecord`](../../src/ProjectAegis.Data/Osint/OsintDiscoveryRecord.cs)):

```csharp
public sealed record OsintDiscoveryRecord(
    string CanonicalId,   // "<platformId>/<sensorId>" (split on first '/')
    string SourceUrl,     // citation ref
    string Snippet,       // human-readable evidence text
    double RelevanceScore,// confidence in [0,1] → the proposal gate
    string TargetDoc,     // provenance gate: "09" near-future / "10" speculative
    int    ProposedTrl,   // Technology Readiness Level (clamped 1–9)
    long   ObservedUtcTicks = 0);
```

---

## Connectors

All connectors implement [`IOsintConnector.Fetch()`](../../src/ProjectAegis.Data/Osint/Connectors/IOsintConnector.cs)
and are **deterministic**: a stable `OrderBy(SourceUrl).ThenBy(CanonicalId)` (Ordinal), no network
call on the hot path, and **no throw** — a missing/invalid source returns an empty array.

| Connector | Source | Behaviour |
|-----------|--------|-----------|
| [`FileOsintConnector`](../../src/ProjectAegis.Data/Osint/Connectors/FileOsintConnector.cs) | Local JSON array file | Tolerant parser (case-insensitive keys, sensible defaults); empty on any parse error. |
| [`RssOsintConnector`](../../src/ProjectAegis.Data/Osint/Connectors/RssOsintConnector.cs) | RSS/HTTP-style (stub) | With a path: same JSON-array parse as File. **Without** a path: one deterministic demo record for MCP/demo. |
| [`InMemoryOsintConnector`](../../src/ProjectAegis.Data/Osint/Connectors/InMemoryOsintConnector.cs) | In-memory list | Supplied records, or a built-in 3-row demo fixture. |

---

## The confidence gate

[`OsintProposalGate.Partition`](../../src/ProjectAegis.Data/Osint/OsintProposalGate.cs) splits
discoveries into **proposals** vs **log-only** by a confidence threshold
(`DefaultProposalConfidenceThreshold = 0.65`, clamped to `[0,1]`):

- `RelevanceScore >= threshold` → *proposal* (will be staged);
- otherwise → *log-only* (recorded, never staged).

Both partitions are emitted in stable `SourceUrl` → `CanonicalId` order, so the batch is
reproducible.

---

## The digest runner

[`OsintDigestRunner`](../../src/ProjectAegis.Data/Osint/OsintDigestRunner.cs) is the headless
orchestrator. Two entry points:

- **`Run(discoveries)`** — pure partition (sort → `OsintProposalGate.Partition`); used by the
  connector / MCP `search_osint` path. Returns `(Proposals, LogOnly)`.
- **`RunFromDigestFile(databasePath, digestPath, clock?)`** — the full file → stage flow:
  1. `ReadDiscoveries` parses the digest JSON (`{ "discoveries": [...] }`, camelCase).
  2. `DedupeDiscoveries` collapses duplicate `CanonicalId`s, keeping the highest `RelevanceScore`
     (tie-break: `SourceUrl` desc).
  3. `OsintProposalGate.Partition` selects proposals.
  4. If any proposals: bootstrap a missing DB (`CatalogSeedBootstrap.SeedBalticPatrol`,
     `overwrite: false`), map proposals to `CatalogSensorBinding`s, and **`ProposeSensorBatch`**
     through a [`CatalogWriteGate`](catalog-write-gate.md) as actor `("agent", "osint-digest-runner")`
     with rationale `osint_digest:<file>`.
  5. Return an `OsintDigestRunResult(ParsedTotal, ProposalCount, LogOnlyCount, BatchId)`.

> **DSA-1.3 invariant:** `EnableRealtimeSocialStream = false` — the MVP must **not** open a 24/7
> real-time social listener. Ingestion is pull-based (fixtures/files/on-demand), not a live stream.

`ResolvePlatformSensorIds(canonicalId)` splits the id on the first `/` into
`(platformId, sensorId)` (falling back to the whole id for both when there is no slash).
`MapProposalsToBindings` produces `Provisional` / `InterpretedValue` sensor bindings with a fixed
`BasePd = 0.5`, `SourceFactId = osint:<targetDoc>`, `Confidence = RelevanceScore`, `TrlLevel =
ProposedTrl`, and the source URL as the citation ref, sorted by `(PlatformId, SensorId)`.

---

## TL / provenance routing

[`OsintCatalogMapper`](../../src/ProjectAegis.Data/Osint/OsintCatalogMapper.cs) (S22-07) is the
richer mapping used when routing by target-doc gate matters:

- `ResolveTrlLevel(proposedTrl)` clamps the staged `TrlLevel` to `1–9`.
- `ResolveBranchTag(targetDoc)` → `branch:doc-09` / `branch:doc-10` (stamped on the staged row's
  `ImportBatchId`) so near-future (doc 09) and speculative (doc 10) facts land on distinct branches.
- `ResolveSourceFactId(targetDoc)` → `osint:09` / `osint:10`; `NormalizeTargetDoc` defaults unknown
  values to `"10"` (speculative).
- `ToSensorBinding` clamps `BasePd` to `[0.1, 0.95]`, sets `ReviewState = Provisional`,
  `ValueTier = InterpretedValue`, `ReviewerId = "osint-digest"`, and the citation ref.

---

## Human review & approval (CLI / MCP)

Staged proposals are **never** auto-committed. A human reviews and approves them through
[`OsintStagingReviewCommand`](../../src/ProjectAegis.MissionEditor.Cli/OsintStagingReviewCommand.cs),
the headless proxy for what a Unity staging UI would call:

```bash
# List pending OSINT batches:
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- osint_staging_review --db <catalog.db>
# Approve a batch (human actor):
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- osint_staging_review --db <catalog.db> --approve <batchId>
```

- With no `--approve`, it lists pending batches (`ListPendingBatches`) as JSON.
- With `--approve <batchId>`, it calls `gate.ApproveBatch(batchId, "human", "osint-ui-reviewer")`
  and reports `Committed` + any errors.
- `osint_search` runs the connector/partition path against a fixture (default `data/osint_facts.json`)
  at the `0.65` threshold and prints proposals + `logOnlyCount`. The MCP aliases
  `osint_digest`, `osint_list_staging_proposals`, `osint_get_proposal_detail`, and
  `osint_submit_review_decision` delegate to these same verbs — see
  [`mission-editor-cli.md`](mission-editor-cli.md).

---

## Determinism & invariants

1. **Deterministic end-to-end** — connectors, dedupe, gate, and mapping all sort Ordinal by
   `SourceUrl` / `CanonicalId` (or `PlatformId` / `SensorId`); no wall-clock or RNG on the path.
   `RunFromDigestFile` takes an injectable `ICatalogClock` (default `FixedCatalogClock(0)`).
2. **Stage-only, human-gated** — the runner only `Propose`s; commit requires a human
   `ApproveBatch`. This is the [write-gate](catalog-write-gate.md) contract, extend-only.
3. **Provisional provenance** — staged rows are `Provisional` / `InterpretedValue` with a
   `CitationRef`; TL is clamped to `1–9` and routed to the doc-09/doc-10 branch.
4. **No live stream / no proprietary data** — `EnableRealtimeSocialStream = false`; feeds are
   public-citation fixtures/files, never CMO `*.db3` (dual-track firewall).

## Tests

OSINT behaviour is pinned in `src/ProjectAegis.Data.Tests/` (gate partition, dedupe, connector
determinism, digest→propose mapping) and the MissionEditor CLI tests for the `osint_search` /
`osint_staging_review` verbs.

## Extending it

- **A new source** implements `IOsintConnector.Fetch()` and MUST stay deterministic (stable sort,
  no throw, no network on the hot path) so digests remain reproducible.
- **New provenance routing** goes in `OsintCatalogMapper` (branch tag / TL clamp / source-fact id);
  keep staged rows `Provisional` with a citation.
- **Never bypass the write gate** — always route new staged rows through `ProposeSensorBatch`, and
  keep approval human-gated.
