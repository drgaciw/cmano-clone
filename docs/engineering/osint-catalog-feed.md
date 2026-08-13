# OSINT catalog feed — connector → digest → proposal gate → write gate

> **Scope.** This page documents the **OSINT (open-source-intelligence) catalog feed**
> (`ProjectAegis.Data/Osint/`, DSA-1.x / S18–S22, [ADR s41](../adr/s41-structural-debt-decision-telemetry-osint.md)):
> how public-source "discoveries" (a curated digest file, a local fixture, or an on-demand
> connector hit) are normalized, **confidence-gated**, mapped to catalog sensor rows, and
> **staged** through the extend-only [`CatalogWriteGate`](catalog-write-gate.md) — never committed
> automatically. It is a **Track B** producer of catalog change and a sibling of the
> [CMO markdown import](cmo-markdown-import.md) pipeline. Approval (the step that UPSERTs live
> rows and pins a snapshot) is human-gated and documented in
> [`catalog-write-gate.md`](catalog-write-gate.md).
>
> Like every catalog feed, OSINT stages **provisional, interpreted-value** rows with a citation;
> it does **not** touch the sim hotpath or replay goldens. Start with the
> [Data layer README](../../src/ProjectAegis.Data/README.md) for the surrounding map; this page is
> the deep-dive on the ingest-and-propose stage.

The load-bearing product constraint is set at the top of the runner:
[`OsintDigestRunner.EnableRealtimeSocialStream = false`](../../src/ProjectAegis.Data/Osint/OsintDigestRunner.cs)
(DSA-1.3 — the MVP must **not** open a 24/7 real-time social listener; ingest is batch/on-demand
and deterministic, pinned by `EnableRealtimeSocialStream_remains_false_for_mvp`).

---

## Pipeline at a glance

```text
 source                    ┌─ curated digest file  (osint-digest-fixture.json: { "discoveries": [...] })
 (public OSINT,   ─────────┤
  no proprietary db3)      └─ IOsintConnector.Fetch()  (File | Rss | InMemory)
        │
        ▼
  OsintDiscoveryRecord[]   (CanonicalId, SourceUrl, Snippet, RelevanceScore, TargetDoc, ProposedTrl, ObservedUtcTicks)
        │  DedupeDiscoveries         ← group by CanonicalId, keep max RelevanceScore (tie → max SourceUrl)   [digest-file path only]
        ▼
  OsintProposalGate.Partition(minConfidence = 0.65)     ← DSA-2.1 doc-05 confidence gate
        │
        ├─ RelevanceScore ≥ threshold ─► Proposals ──► map to CatalogSensorBinding ──► IWriteGate.ProposeSensorBatch  ["proposed"; nothing live]
        │
        └─ RelevanceScore <  threshold ─► LogOnly   (dropped from staging; counted only)
        │
        ▼  review the staging DB via CLI
  osint_staging_review  ──►  catalog_write_approve  ──► validate → UPSERT live rows → audit log → snapshot hash
```

Two orderings are worth keeping straight:

- **Digest-file path** ([`RunFromDigestFile`](../../src/ProjectAegis.Data/Osint/OsintDigestRunner.cs)):
  parse → **dedupe** → partition → map (via the runner's own `MapProposalsToBindings`) → propose.
- **Connector / on-demand path** (`osint_search`, MCP): `connector.Fetch()` → `runner.Run(...)`
  (partition only, **no dedupe**) → callers map via
  [`OsintCatalogMapper`](../../src/ProjectAegis.Data/Osint/OsintCatalogMapper.cs) when they want to
  stage. The two mappers differ — see [Two mappers](#two-mappers-a-real-gotcha).

---

## Layering

```
ProjectAegis.Data/Osint/
  OsintDiscoveryRecord        # immutable normalized hit (the unit of currency)
  OsintProposalGate           # pure confidence partition (Proposals / LogOnly)
  OsintCatalogMapper          # record → CatalogSensorBinding (connector path) + TL branch-tag routing
  OsintDigestRunner           # orchestrator: read → dedupe → partition → map → ProposeSensorBatch
  Connectors/
    IOsintConnector           # Fetch() → OsintDiscoveryRecord[]  (deterministic, never throws)
    FileOsintConnector        # local JSON array fixture (tolerant parse, empty on miss/error)
    RssOsintConnector         # fixture-driven stub; deterministic demo record when no path
    InMemoryOsintConnector    # demo/test records; stable sorted Fetch

ProjectAegis.MissionEditor.Cli/
  Program.cs                  # osint_search, osint_staging_review verbs
  OsintStagingReviewCommand   # list pending batches / approve a batch (human-gated)

ProjectAegis.Data/Catalog/
  CatalogTlTierResolver       # consumes OSINT branch tags for export-only TL-tier filtering
```

Everything under `Osint/` is **pure, single-threaded, and I/O-bounded to file reads** — no network in
the hot path, no `DateTime.UtcNow` (staging uses an injected [`ICatalogClock`](../../src/ProjectAegis.Data/Catalog/),
defaulting to `FixedCatalogClock(0)`), and connectors **never throw** (a missing/unparseable source
returns an empty array). Every collection is emitted in a stable `Ordinal` sort so a given input always
produces the same staged batch.

---

## The discovery record

[`OsintDiscoveryRecord`](../../src/ProjectAegis.Data/Osint/OsintDiscoveryRecord.cs) is the normalized
currency of the pipeline:

| Field | Meaning |
|-------|---------|
| `CanonicalId` | Stable id for the observed thing. When it contains a `/`, it splits into `platform/sensor` (see mapping); otherwise the whole id is used for both. |
| `SourceUrl` | Public citation URL. Becomes `CitationRef` on the staged row; also the primary sort key. |
| `Snippet` | Short human-readable evidence text (not persisted to the catalog row). |
| `RelevanceScore` | `[0,1]` confidence. The **only** input the proposal gate reads. |
| `TargetDoc` | Provenance/routing gate: `"09"` (near-future) or `"10"` (speculative); anything blank defaults to `"10"`. Drives the TL branch tag. |
| `ProposedTrl` | Technology-readiness level 1–9 (clamped on the connector mapping path). |
| `ObservedUtcTicks` | Optional observation timestamp (informational; not used for gating or ordering). |

---

## The confidence gate

[`OsintProposalGate.Partition`](../../src/ProjectAegis.Data/Osint/OsintProposalGate.cs) is the whole
policy: split discoveries into `Proposals` (staged) and `LogOnly` (counted, then dropped).

- **Threshold**: `DefaultProposalConfidenceThreshold = 0.65` (DSA-2.1, doc-05). Callers may override;
  the value is `Math.Clamp`ed to `[0,1]`.
- **Rule**: `RelevanceScore >= threshold` ⇒ proposal, else log-only. The boundary is **inclusive** —
  exactly `0.65` is a proposal; `0.64` is log-only (pinned by
  `RunFromDigestFile_all_below_threshold_is_log_only_without_write_gate`).
- **Ordering**: results are sorted by `SourceUrl` then `CanonicalId` (`Ordinal`), so staging order is
  deterministic regardless of input order.

Log-only discoveries are **never staged** — they exist so a run can report "we saw N low-confidence
hits" without polluting the catalog.

---

## Dedupe (digest-file path)

[`OsintDigestRunner.DedupeDiscoveries`](../../src/ProjectAegis.Data/Osint/OsintDigestRunner.cs) collapses
repeated `CanonicalId`s **before** the gate:

- Group by `CanonicalId` (`Ordinal`).
- Keep the record with the **highest** `RelevanceScore`; tie-break on the **highest** `SourceUrl`
  (`Ordinal`).

So two hits for `dedupe-target` at `0.7` and `0.8` collapse to the `0.8` record
(`DedupeDiscoveries_keeps_higher_relevance_and_tie_breaks_source_url`). This dedupe runs only in
`RunFromDigestFile`; `OsintDigestRunner.Run(...)` (the connector path) partitions the caller's list
as-is (connectors already emit sorted, deduped fixtures).

> **`ParsedTotal` counts pre-dedupe.** In the committed fixture, 5 raw discoveries dedupe to 4 unique,
> which the gate splits into 3 proposals + 1 log-only — so `OsintDigestRunResult` reports
> `ParsedTotal = 5`, `ProposalCount = 3`, `LogOnlyCount = 1`
> (`RunFromDigestFile_stages_proposals_via_write_gate_without_commit`).

---

## Mapping to catalog rows

Proposals become [`CatalogSensorBinding`](../../src/ProjectAegis.Data/Catalog/) rows. **All OSINT rows are
staged as provisional, interpreted-value, with a citation** — never as an authoritative fact:

- `ReviewState = CatalogReviewStates.Provisional`
- `ValueTier = CatalogProvenanceTier.InterpretedValue`
- `CitationRef = SourceUrl`

### Two mappers (a real gotcha)

There are **two** mapping functions with deliberately different behavior. Pick the one that matches your
call path.

| Aspect | `OsintDigestRunner.MapProposalsToBindings` (digest-file path) | `OsintCatalogMapper.ToSensorBinding[s]` (connector / on-demand path) |
|--------|--------------------------------------------------------------|---------------------------------------------------------------------|
| PlatformId / SensorId | Split `CanonicalId` on first `/`; flat id ⇒ both equal the id | `PlatformId` defaults to `"osint-platform"`; `SensorId = "osint-" + canonicalId` (spaces→`-`, lower-cased) |
| `BasePd` | fixed `0.5` | `Clamp(RelevanceScore, 0.1, 0.95)` |
| `TrlLevel` | **raw** `ProposedTrl` (not clamped) | `Clamp(ProposedTrl, 1, 9)` |
| `SourceFactId` | `osint:{TargetDoc}` (raw) | `osint:{normalizedTargetDoc}` (`09`/`10`) |
| `SourceFile` | digest file name | `SourceUrl` |
| `ImportBatchId` | *(unset)* | **branch tag** `branch:doc-09` / `branch:doc-10` (TL routing) |
| `ReviewerId` | *(unset)* | `"osint-digest"` |
| Ordering | sorted by `PlatformId` then `SensorId` | sorted by `PlatformId` then `SensorId` |

`RunFromDigestFile` uses the **runner's** `MapProposalsToBindings` (it preserves the human-authored
`canonicalId` as `platform/sensor` and keeps confidence/TRL verbatim for review). The connector-fed
paths use `OsintCatalogMapper` when they need branch-tag TL routing. Both are covered by
`MapProposalsToBindings_splits_canonical_id_and_maps_provenance_fields` and the mapper tests.

---

## TL-tier routing

`OsintCatalogMapper` encodes the near-future/speculative gate as an **import batch tag** that the
export layer reads back:

- [`ResolveBranchTag(targetDoc)`](../../src/ProjectAegis.Data/Osint/OsintCatalogMapper.cs) →
  `branch:doc-09` (near-future) or `branch:doc-10` (speculative; the default when `targetDoc` is blank).
- [`CatalogTlTierResolver.ResolveFromProvenance`](../../src/ProjectAegis.Data/Catalog/CatalogTlTierResolver.cs)
  (S30-02, read-only, **export-only filtering**) maps that tag + TRL to a technology-level tier when the
  platform has no explicit `game_technology_level`:

  | Branch tag | TRL band | TL tier |
  |------------|----------|---------|
  | `branch:doc-10` | `≥ 7` | `Tl5` |
  | `branch:doc-10` | `< 7` | `Tl4` |
  | `branch:doc-09` | `≥ 8` | `Tl3` |
  | `branch:doc-09` | `5–7` | `Tl2` |
  | `branch:doc-09` | `< 5` | `Tl1` |
  | *(none)* | — | `Default` |

This lets OSINT-sourced speculative gear be filtered out of lower-TL exports without changing the sim.

---

## Running it

All commands run from the repo root; the CLI project is
[`ProjectAegis.MissionEditor.Cli`](mission-editor-cli.md).

### Batch ingest a digest file (headless)

```csharp
// ProjectAegis.Data.Osint
var result = OsintDigestRunner.RunFromDigestFile(
    databasePath: "catalog.db",        // seeded with Baltic patrol if it doesn't exist yet
    digestPath:   OsintDigestRunner.ResolveFixtureDigestPath(),  // or your own digest JSON
    clock:        new FixedCatalogClock(0));

// result.ParsedTotal / ProposalCount / LogOnlyCount / BatchId (null when nothing was staged)
```

Behavior worth knowing:

- If **no proposals** clear the gate, the runner **does not** create or seed the database and returns a
  `null` `BatchId` (`RunFromDigestFile_empty_digest_skips_write_gate`).
- Staged rows are **not visible to a catalog reader** until approved — `TryGetBasePd(...)` returns
  `false` for staged-but-unapproved sensors.
- The digest JSON shape is `{ "discoveries": [ { canonicalId, sourceUrl, snippet, relevanceScore,
  targetDoc, proposedTrl, observedUtcTicks } ] }` (camelCase, case-insensitive; see the
  [committed fixture](../../src/ProjectAegis.Data.Tests/Fixtures/osint-digest-fixture.json)).

### On-demand search (MCP / CLI)

```bash
# Fetch → gate; prints { proposals[], logOnlyCount }. Uses data/osint_facts.json (or --db <fixture.json>).
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- osint_search [--db <fixture.json>]
```

`osint_search` wires a `FileOsintConnector` into an `OsintDigestRunner(0.65)` and reports proposals vs a
log-only count. A missing fixture yields an empty (but successful) result — connectors are deterministic
and never throw.

### Review & approve staged batches (human-gated)

```bash
# List pending staged batches
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- osint_staging_review --db catalog.db

# Approve one batch (UPSERTs live rows + pins a snapshot via the write gate)
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- osint_staging_review --db catalog.db --approve <batchId>
```

[`OsintStagingReviewCommand`](../../src/ProjectAegis.MissionEditor.Cli/OsintStagingReviewCommand.cs) is a
thin proxy over the write gate's `ListPendingBatches()` / `ApproveBatch(batchId, "human", "osint-ui-reviewer")`
— the same seam a Unity staging UI would call. Approval is the **only** step that mutates live catalog
rows, and it is always attributed to a `human` actor.

---

## Determinism & provenance invariants

| Invariant | Where enforced |
|-----------|----------------|
| No real-time social listener in the MVP | `EnableRealtimeSocialStream = false` (pinned by test) |
| Connectors never throw; empty on miss/parse-error | `File`/`Rss`/`InMemory` `Fetch()` catch-all → `Array.Empty<>()` |
| No network / no wall-clock in the hot path | connectors read files only; staging uses injected `ICatalogClock` (default `FixedCatalogClock(0)`) |
| Stable, reproducible staging order | every collection sorted by `SourceUrl`→`CanonicalId` or `PlatformId`→`SensorId` (`Ordinal`) |
| OSINT never writes live rows directly | all rows go through `IWriteGate.ProposeSensorBatch`; approval is a separate human step |
| OSINT rows are provisional + cited | `ReviewState=Provisional`, `ValueTier=InterpretedValue`, `CitationRef=SourceUrl` |
| No proprietary CMO `.db3` involved | OSINT is a **public-source** Track-B feed (see [dual-track](dual-track-cmo-analysis-and-catalog.md)) |

Because OSINT only produces **staged catalog rows** (never sim state), none of this participates in the
replay fingerprint; the Baltic v2 replay hash is untouched by any OSINT run.

---

## Tests

Co-located under [`src/ProjectAegis.Data.Tests/Osint/`](../../src/ProjectAegis.Data.Tests/Osint/):

| File | Pins |
|------|------|
| `OsintProposalGateTests.cs` | inclusive-boundary partition, threshold clamping |
| `OsintDigestRunnerTests.cs` | end-to-end stage-without-commit, empty/all-below-threshold short-circuits, dedupe tie-break, canonical-id split mapping, deterministic staging order, `EnableRealtimeSocialStream` false |
| `OsintCatalogMapperTests.cs` | connector-path binding fields, TRL clamp, branch-tag routing |
| `OsintConnectorTests.cs` | `File`/`Rss`/`InMemory` deterministic `Fetch`, empty-on-error |

Run just this slice:

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter FullyQualifiedName~Osint
```

---

## Runbook — add a new OSINT source or field

1. **New source** → implement [`IOsintConnector`](../../src/ProjectAegis.Data/Osint/Connectors/IOsintConnector.cs).
   `Fetch()` must be deterministic (stable `SourceUrl`→`CanonicalId` sort), must **never throw**
   (return an empty array on any error), and must not hit the network in the hot path. Mirror the
   tolerant parse in `FileOsintConnector` (case-insensitive keys, sensible defaults).
2. **New discovery field** → add it to `OsintDiscoveryRecord` and the private `OsintDiscoveryDto`
   (with a `[JsonPropertyName]`), then thread it through `ReadDiscoveriesFromJson`. Only wire it into
   the gate/mapping if it changes staging behavior.
3. **Change the confidence policy** → adjust the caller's threshold, not the enum boundary; keep the
   `>=` inclusive rule (a test pins `0.64` as log-only).
4. **Change how a proposal maps to a row** → prefer extending `OsintCatalogMapper` /
   `MapProposalsToBindings`; keep `ReviewState=Provisional`, `ValueTier=InterpretedValue`, and a
   `CitationRef`. **Never** bypass the write gate — staging must remain propose-only, approval
   human-gated ([extend-only rule](catalog-write-gate.md)).
5. **Verify** with the `~Osint` filter above, then the full `dotnet test ProjectAegis.sln`.

---

## Related docs

| Doc | Relationship |
|-----|--------------|
| [catalog-write-gate.md](catalog-write-gate.md) | The propose → approve → commit gate every OSINT batch stages through. |
| [cmo-markdown-import.md](cmo-markdown-import.md) | Sibling Track-B feed (markdown corpus instead of OSINT hits). |
| [dual-track-cmo-analysis-and-catalog.md](dual-track-cmo-analysis-and-catalog.md) | The firewall that keeps proprietary `.db3` analysis out of these public feeds. |
| [catalog-seeding.md](catalog-seeding.md) | How the Baltic patrol DB the runner seeds-on-miss is built. |
| [catalog-release-train.md](catalog-release-train.md) | The snapshot/release layer above approval. |
| [mission-editor-cli.md](mission-editor-cli.md) | The CLI/MCP host for `osint_search` / `osint_staging_review`. |
