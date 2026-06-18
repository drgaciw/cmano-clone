# OSINT Discovery Pipeline (Connectors → Digest → Staging)

> **Scope:** `ProjectAegis.Data.Osint` — turning external OSINT sources into *staged* sensor proposals.
> **Authoritative ADR:** [ADR-006 — Data Layer Boundary](../architecture/adr-006-data-layer-boundary.md)
> **Requirements:** doc 05 — Dynamic Systems Agent (`DSA-1.x` discovery, `DSA-2.1` confidence gate), Req 06 — Database Intelligence
> **Last updated:** 2026-06-18

This runbook covers the **OSINT discovery half** of the catalog data flow: fetching
discoveries from a connector or a digest file, gating them by relevance, mapping survivors
to `CatalogSensorBinding` records, and staging them through the write gate for human review.
It complements [Catalog Ingestion Pipeline](catalog-ingestion-pipeline.md) (which documents
CMO-markdown parsing and the single-record `OsintCatalogMapper.ToSensorBinding`) and
[Catalog Write-Gate & Determinism](catalog-write-gate-determinism.md) (the propose → approve →
commit half).

It exists because the connector abstraction (`IOsintConnector` + File/Rss/InMemory impls,
S20-01), the digest runner (`OsintDigestRunner`, S19-05), and the proposal gate
(`OsintProposalGate`, DSA-2.1) had only scattered sprint-plan references and two CLI verbs —
no developer-facing description of the JSON shapes, the **two different mapping paths**, or the
determinism guarantees every connector must uphold.

## End-to-end flow

```text
RSS / file / MCP source              digest file (.json)
        │                                    │
        ▼                                    ▼
 IOsintConnector.Fetch()            OsintDigestRunner
 (File | Rss | InMemory)            .RunFromDigestFile(db, path)
        │                                    │
        │ OsintDiscoveryRecord[]             │ ReadDiscoveries → DedupeDiscoveries
        ▼                                    ▼
 OsintDigestRunner.Run(...)         OsintProposalGate.Partition (≥ 0.65 relevance)
        │                                    │
        ▼ (Proposals, LogOnly)               ▼ proposals only
 (caller decides)                    MapProposalsToBindings  ──►  CatalogSensorBinding[]
                                             │  (provisional, TRL clamp, osint:<doc> fact id)
                                             ▼
                                     CatalogWriteGate.ProposeSensorBatch ──► catalog_staging_sensors (batchId)
                                             │
                                             ▼
                                     osint_staging_review --approve <batchId>  (human commit)
```

**Key boundary:** connectors and the runner never touch SQLite directly except through
[`CatalogWriteGate`](../../src/ProjectAegis.Data/WriteGate/CatalogWriteGate.cs). Everything OSINT
emits is **`provisional`** and only lands in `catalog_staging_sensors` — a human (or an explicit
`--approve`) is always required to commit (ADR-006 §2). `OsintDigestRunner.EnableRealtimeSocialStream`
is a hard-coded `false` (DSA-1.3: the MVP must not open a 24/7 real-time social listener).

## Connectors: [`IOsintConnector`](../../src/ProjectAegis.Data/Osint/Connectors/IOsintConnector.cs)

The connector contract is a single method, `OsintDiscoveryRecord[] Fetch()`. **Every
implementation must be deterministic** — a stable `Fetch()` with no network call or wall-clock
read in the hot path, and results always sorted by `SourceUrl` then `CanonicalId` (ordinal).

| Connector | Source | Behavior |
|-----------|--------|----------|
| [`FileOsintConnector`](../../src/ProjectAegis.Data/Osint/Connectors/FileOsintConnector.cs) | local JSON **array** | parses a top-level `[ {…}, … ]` array; missing/unparseable file → empty array (never throws) |
| [`RssOsintConnector`](../../src/ProjectAegis.Data/Osint/Connectors/RssOsintConnector.cs) | RSS/HTTP stub | with a path, parses the same array shape; with **no** path, returns one deterministic demo record (`rss-demo-hypersonic`) for MCP/demo |
| [`InMemoryOsintConnector`](../../src/ProjectAegis.Data/Osint/Connectors/InMemoryOsintConnector.cs) | in-process records | returns supplied records (or a 3-row default fixture) sorted; for tests/demo |

All three parse tolerantly: property names are case-insensitive and missing fields fall back to
defaults (`relevanceScore → 0.5`, `targetDoc → "10"`, `proposedTrl → 5`). On **any** parse error
they return an empty array rather than throwing, so a bad source degrades gracefully instead of
breaking a run.

### Connector fixture shape (top-level array)

```json
[
  { "canonicalId": "hypersonic-glide-s20", "sourceUrl": "https://ex.com/hg-s20", "snippet": "observed boost-glide", "relevanceScore": 0.81, "targetDoc": "10", "proposedTrl": 7 },
  { "canonicalId": "low-conf-example",     "sourceUrl": "https://ex.com/low",    "snippet": "below threshold",     "relevanceScore": 0.40, "targetDoc": "09", "proposedTrl": 4 }
]
```

The committed real fixture is [`data/osint_facts.json`](../../data/osint_facts.json); the
`osint_search` CLI verb reads it by default.

## Discovery record: [`OsintDiscoveryRecord`](../../src/ProjectAegis.Data/Osint/OsintDiscoveryRecord.cs)

```text
OsintDiscoveryRecord(CanonicalId, SourceUrl, Snippet, RelevanceScore, TargetDoc, ProposedTrl, ObservedUtcTicks = 0)
```

- **`CanonicalId`** — stable key. For the digest runner it may be `platform/sensor`
  (e.g. `u-hypersonic/radar-glide`); the slash splits into platform vs sensor id (below).
- **`RelevanceScore`** — `[0,1]` confidence; drives the proposal gate and the staged `Confidence`.
- **`TargetDoc`** — `"09"` (near-future) or `"10"` (speculative); routes provenance.
- **`ProposedTrl`** — raw technology-readiness level, clamped to `[1,9]` when staged.

## Proposal gate: [`OsintProposalGate`](../../src/ProjectAegis.Data/Osint/OsintProposalGate.cs)

`Partition` splits discoveries into `(Proposals, LogOnly)` on a single threshold:

| Default | Constant | Rule |
|---------|----------|------|
| Relevance ≥ `0.65` | `DefaultProposalConfidenceThreshold` | `RelevanceScore >= min` → proposal; below → log-only |

The threshold is clamped to `[0,1]`, and output is sorted by `SourceUrl` then `CanonicalId`.
**This is *not* the catalog import gate.** The proposal gate only decides *staging vs discard*;
it does not check TRL or review-state. The TRL/confidence/review-state quarantine described in
the [catalog ingestion runbook](catalog-ingestion-pipeline.md#the-import-gate-sensor-quarantine-catalogimportgate)
applies to the CMO-markdown sensor path, not to the digest runner.

## Digest runner: [`OsintDigestRunner`](../../src/ProjectAegis.Data/Osint/OsintDigestRunner.cs)

Two entry points, used by different callers:

### 1. `Run(discoveries)` — partition only (connector / MCP path)

Sorts the input, calls `OsintProposalGate.Partition`, and returns `(Proposals, LogOnly)`
**without** touching the database. This is what `osint_search` uses to preview a connector's
output. The constructor takes an optional `proposalThreshold` (defaults to `0.65`).

### 2. `RunFromDigestFile(db, digestPath, clock?)` — stage to the write gate

The full headless path:

1. `ReadDiscoveries` — parse the digest file (see shape below).
2. `DedupeDiscoveries` — group by `CanonicalId`, keep the row with the **highest** `RelevanceScore`
   (tie-break: highest `SourceUrl` ordinal). De-dup happens **before** gating.
3. `OsintProposalGate.Partition` — keep proposals ≥ threshold.
4. If any proposals remain: seed the DB with `SeedBalticPatrol` when it is missing, then
   `MapProposalsToBindings` → `CatalogWriteGate.ProposeSensorBatch(actor "agent",
   "osint-digest-runner", "osint_digest:<file>")`.
5. Return `OsintDigestRunResult(ParsedTotal, ProposalCount, LogOnlyCount, BatchId?)`.
   `BatchId` is `null` when nothing cleared the gate.

Pass a fixed `ICatalogClock` (the default is `new FixedCatalogClock(0)`) to keep staged
timestamps deterministic.

### Digest file shape (wrapped object — note the difference)

```json
{
  "discoveries": [
    { "canonicalId": "u-hypersonic/radar-glide", "sourceUrl": "https://example.org/a", "snippet": "…", "relevanceScore": 0.72, "targetDoc": "09", "proposedTrl": 6 },
    { "canonicalId": "dedupe-target", "sourceUrl": "https://example.org/dup-high", "snippet": "…", "relevanceScore": 0.8, "targetDoc": "09", "proposedTrl": 6 }
  ]
}
```

> ⚠️ The **digest file** is a wrapped object (`{ "discoveries": [...] }`); the **connector
> fixture** is a bare array (`[...]`). They are read by different code paths and are not
> interchangeable. The canonical digest example is
> [`src/ProjectAegis.Data.Tests/Fixtures/osint-digest-fixture.json`](../../src/ProjectAegis.Data.Tests/Fixtures/osint-digest-fixture.json).

## Two mapping paths (do not confuse them)

OSINT discoveries become sensor bindings through **two distinct mappers** with different field
rules. Pick based on the entry point you are using:

| Aspect | `OsintDigestRunner.MapProposalsToBindings` (digest path) | `OsintCatalogMapper.ToSensorBinding` (single-record / Unity panel) |
|--------|----------------------------------------------------------|--------------------------------------------------------------------|
| `PlatformId` / `SensorId` | split `CanonicalId` on the first `/` (`platform/sensor`); no slash → both equal `CanonicalId` | `PlatformId` arg (default `osint-platform`); `SensorId` = `osint-<canonicalId>` |
| `BasePd` | hard-coded `0.5` | `RelevanceScore` clamped to `[0.1, 0.95]` |
| `SourceFactId` | `osint:<targetDoc>` (raw `TargetDoc`) | `osint:<normalized doc>` via `ResolveSourceFactId` |
| `ImportBatchId` | not set | `branch:doc-<09\|10>` branch tag |
| `ReviewerId` | not set | `osint-digest` |
| `ReviewState` / `ValueTier` | `provisional` / `interpreted_value` | `provisional` / `interpreted_value` |
| `TrlLevel` | raw `ProposedTrl` | `ResolveTrlLevel` clamp `[1,9]` |

`OsintCatalogMapper` is the one documented in the
[catalog ingestion runbook](catalog-ingestion-pipeline.md#osint-source-osintcatalogmapper) and is
used by the Unity staging panel host; `MapProposalsToBindings` is internal to the digest runner.
Both emit `provisional` rows, so neither auto-commits.

## Running OSINT from the CLI / MCP

Two verbs are wired in [`Program.cs`](../../src/ProjectAegis.MissionEditor.Cli/Program.cs)
(full table in [`tools/mission-editor/README.md`](../../tools/mission-editor/README.md#osint)):

### `osint_search` — preview connector output (no DB writes)

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  osint_search [--db <fixture.json>]
```

Builds a `FileOsintConnector` (default `data/osint_facts.json`, or `--db <path>` if it exists),
runs it through `OsintDigestRunner.Run`, and prints `{ proposals[], logOnlyCount }`. A missing
fixture yields zero proposals deterministically — it never errors.

### `osint_staging_review` — list / approve staged proposals

```bash
# list pending batches
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  osint_staging_review --db catalog.db

# approve (commit) one batch as a human reviewer
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  osint_staging_review --db catalog.db --approve <batchId>
```

Without `--approve` it prints pending batches (`batchId`, `recordCount`, `actorType`,
`approvalState`) via `CatalogWriteGate.ListPendingBatches`. With `--approve` it calls
`ApproveBatch(batchId, "human", "osint-ui-reviewer")` — the same path a Unity staging UI would
use ([`OsintStagingPanelHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/OsintStagingPanelHost.cs)).

> The headless `RunFromDigestFile` staging path is exercised by tests rather than a dedicated
> CLI verb today; `Program.cs` flags `osint_digest` as a pending S21 verb. To stage a digest now,
> call `OsintDigestRunner.RunFromDigestFile` directly (see the runner tests) or use `osint_search`
> + the write-gate verbs.

## Testing & verification

| Test file | Covers |
|-----------|--------|
| `Osint/OsintConnectorTests.cs` | File/Rss/InMemory parsing, deterministic sort, graceful empties (S20-01) |
| `Osint/OsintDigestRunnerTests.cs` | partition determinism, dedupe-by-canonical-id, empty input |
| `Osint/OsintProposalGateTests.cs` | threshold partition into proposals vs log-only |
| `Osint/OsintCatalogMapperTests.cs` | TRL clamp, branch-tag / `SourceFactId` routing |

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "Osint" -v minimal
```

## Common pitfalls

| Pitfall | Symptom | Fix |
|---------|---------|-----|
| Mixing the two JSON shapes | digest read returns 0, or connector returns 0 | digest = `{ "discoveries": [...] }`; connector = bare `[...]` |
| Expecting OSINT rows to commit | rows stay in `catalog_staging_sensors` | OSINT is always `provisional`; approve via `osint_staging_review --approve` |
| Confusing the proposal gate with the import gate | TRL filtering not happening | the proposal gate only checks relevance (≥ 0.65); TRL/review-state quarantine is the CMO-markdown sensor path |
| Assuming dedupe keeps the first row | a lower-relevance duplicate is staged | `DedupeDiscoveries` keeps the **highest** `RelevanceScore` per `CanonicalId` |
| Expecting `osint_search` to write the DB | nothing staged | `osint_search` only previews; staging needs `RunFromDigestFile` or the write-gate verbs |
| Non-deterministic connector | replay/world-hash drift | connectors must sort by `SourceUrl` then `CanonicalId` and avoid network/wall-clock in `Fetch()` |
| Locale-sensitive number parsing | relevance/TRL mis-parsed | keep source values `.`-decimal |

## See also

- [Catalog Ingestion Pipeline](catalog-ingestion-pipeline.md) — CMO-markdown parsing and the single-record `OsintCatalogMapper`
- [Catalog Write-Gate & Determinism](catalog-write-gate-determinism.md) — propose → approve → commit, sort keys, snapshots
- [Balance Drift Telemetry (Advisory)](balance-drift-telemetry.md) — advisory drift sink; never writes the catalog
- [ADR-006 — Data Layer Boundary](../architecture/adr-006-data-layer-boundary.md)
- `Game-Requirements/requirements/05-Dynamic-Systems-Agent.md`, `06-Database-Intelligence.md`
- Mission Editor CLI/MCP reference: [`tools/mission-editor/README.md`](../../tools/mission-editor/README.md#osint)
