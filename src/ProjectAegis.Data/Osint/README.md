# OSINT Digest → Proposal Gate → Write Gate

`ProjectAegis.Data.Osint` — turns open-source intelligence (OSINT) discoveries into
**staged** catalog sensor proposals. Connectors fetch normalized hits, a confidence
gate splits them into *propose* vs *log-only*, and survivors are handed to the
[Catalog Write Gate](../WriteGate/README.md) as a sensor batch. **Nothing here writes
the live catalog directly** — every commit still goes through propose → approve.

**Requirement trace:** DSA-1.x, DSA-2.1
(`Game-Requirements/requirements/05-Dynamic-Systems-Agent.md`); doc-09 near-future /
doc-10 speculative routing (`09-Near-Future-Technologies.md`, `10-Speculative-Systems.md`).
**Posture:** *advisory ingestion, human-gated commit* — agents surface candidates, the
reviewer approves them.

## Intent

OSINT feeds are noisy and mostly speculative. Rather than mutate the catalog from a
scraper, this subsystem:

1. **Fetches** discoveries from a connector (file, RSS stub, or in-memory fixture).
2. **Partitions** them by relevance: high-confidence hits become *proposals*, the rest
   are *log-only* (kept for audit, never staged).
3. **Maps** proposals to `CatalogSensorBinding` rows and **proposes** them as a sensor
   batch through `CatalogWriteGate`, where they wait for human approval.

The whole path is deterministic — stable ordinal sort, injectable clock, no wall-clock
and no live social stream — so digest runs and replays are reproducible.

> **No real-time listener (DSA-1.3).** `OsintDigestRunner.EnableRealtimeSocialStream`
> is a hard-coded `false`. The MVP runs on batched digests / on-demand fetches only.

## Architecture

```
IOsintConnector.Fetch()                 (File / Rss / InMemory — all deterministic)
        │  OsintDiscoveryRecord[]
        ▼
OsintProposalGate.Partition(records, minConfidence = 0.65)
        │  RelevanceScore >= min ? proposal : log-only
        ├──────────────► LogOnly[]  (audit only, never staged)
        ▼
   Proposals[]
        │  map to CatalogSensorBinding (sorted ordinally)
        ▼
CatalogWriteGate.ProposeSensorBatch(bindings, "agent", "osint-digest-runner", "osint_digest:<file>")
        ▼
   catalog_staging_sensor  (state = "proposed")  ──►  human approve via write gate
```

| Type | Role |
|------|------|
| `OsintDiscoveryRecord` | Normalized hit: `(CanonicalId, SourceUrl, Snippet, RelevanceScore, TargetDoc, ProposedTrl, ObservedUtcTicks)`. |
| `IOsintConnector` | `Fetch() → OsintDiscoveryRecord[]`. All implementations must be deterministic and **never throw** (empty array on error). |
| `FileOsintConnector` | Reads a JSON **array** fixture (tolerant of camelCase / PascalCase keys). Missing file, non-array, or parse error → empty. |
| `RssOsintConnector` | RSS/HTTP stub. **Empty path** → one deterministic demo record; **explicit-but-missing path** → empty; otherwise parses a JSON array like the file connector. |
| `InMemoryOsintConnector` | Demo/test connector: supplied records, or a built-in 3-record fixture. |
| `OsintProposalGate` | Confidence gate (DSA-2.1). `Partition` keeps `RelevanceScore >= minimumConfidence` (default `0.65`). |
| `OsintDigestRunner` | Orchestrates fetch/dedupe → partition → propose. Static `RunFromDigestFile` is the full headless path. |
| `OsintCatalogMapper` | **S22-07 TRL/branch routing**: maps a record to a `CatalogSensorBinding` with TRL clamped to 1–9 and a doc-09/10 branch tag. |
| `OsintDigestRunResult` | `(ParsedTotal, ProposalCount, LogOnlyCount, BatchId?)` returned by `RunFromDigestFile`. |

## Usage

### Headless digest run (file → staging)

```csharp
using ProjectAegis.Data.Osint;

// Parses {"discoveries":[...]}, dedupes by CanonicalId, partitions at 0.65,
// seeds the Baltic catalog if the db is missing, and proposes a sensor batch.
OsintDigestRunResult result = OsintDigestRunner.RunFromDigestFile(
    databasePath: "catalog.db",
    digestPath:   OsintDigestRunner.ResolveFixtureDigestPath());

Console.WriteLine($"{result.ProposalCount} proposed, {result.LogOnlyCount} log-only");
if (result.BatchId is not null)
    Console.WriteLine($"awaiting approval: {result.BatchId}"); // null when no proposals
```

### Partition only (connector / on-demand search path)

```csharp
using ProjectAegis.Data.Osint;
using ProjectAegis.Data.Osint.Connectors;

var connector = new FileOsintConnector("data/osint_facts.json");
var runner = new OsintDigestRunner(proposalThreshold: 0.65);
var (proposals, logOnly) = runner.Run(connector.Fetch());
```

### TRL / branch routing (S22-07)

`OsintCatalogMapper` is the routing-aware mapper used when doc-09/doc-10 provenance
matters:

```csharp
var binding = OsintCatalogMapper.ToSensorBinding(record);          // platformId defaults to "osint-platform"
OsintCatalogMapper.ResolveTrlLevel(12);        // 9   (clamped to 1–9)
OsintCatalogMapper.ResolveBranchTag("9");      // "branch:doc-09"
OsintCatalogMapper.ResolveBranchTag("");       // "branch:doc-10" (blank defaults to speculative)
OsintCatalogMapper.ResolveSourceFactId("10");  // "osint:10"
```

### Digest file schema

`RunFromDigestFile` / `ReadDiscoveries` expect camelCase keys under `discoveries`:

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

`canonicalId` may be `platform/sensor`; `OsintDigestRunner.ResolvePlatformSensorIds`
splits on the **first** `/` (no slash → platform == sensor == the whole id). Connector
fixtures (`FileOsintConnector` / `RssOsintConnector`) instead use a top-level JSON
**array** of the same fields.

## CLI / operational runbook

The headless mission-editor exposes two OSINT verbs (see
[`tools/mission-editor/README.md`](../../../tools/mission-editor/README.md)):

```bash
# On-demand search: reads data/osint_facts.json by default (FileOsintConnector + gate at 0.65).
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- osint_search
# → { "ok": true, "proposals": [ ... ], "logOnlyCount": N }

# Review staged proposals, then approve one through the write gate.
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- osint_staging_review --db catalog.db
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- osint_staging_review --db catalog.db --approve <batchId>
```

The MCP manifest also advertises `osint_digest`, `osint_list_staging_proposals`,
`osint_get_proposal_detail`, and `osint_submit_review_decision`; these map onto the two
CLI verbs above.

## Constraints & gotchas

- **Two binding paths produce different rows — pick deliberately.**
  `OsintDigestRunner.MapProposalsToBindings` (used by `RunFromDigestFile`) sets
  `BasePd = 0.5`, passes `ProposedTrl` through **unclamped**, and leaves the import
  batch tag default. `OsintCatalogMapper.ToSensorBinding` (S22-07) instead derives
  `BasePd` from `RelevanceScore` (clamped `0.1–0.95`), **clamps TRL to 1–9**, sets
  `ImportBatchId` to a `branch:doc-09/10` tag, and stamps `ReviewerId = "osint-digest"`.
  Use the mapper when doc-09/10 branch routing matters.
- **Connectors never throw.** Any I/O or parse failure yields an empty array, by
  design (determinism). A silent empty result usually means a missing or malformed
  fixture — check the path, not an exception.
- **`RssOsintConnector` with no path is a demo.** An empty path returns a single
  canned `rss-demo-hypersonic` record; pass an explicit fixture path for real data.
- **Confidence is the only gate here.** `OsintProposalGate` filters purely on
  `RelevanceScore`. TRL / review-state quarantine is enforced later at **approve**
  time by `CatalogImportGate` inside the write gate, not at propose time.
- **Dedup keeps the strongest hit.** `RunFromDigestFile` dedupes by `CanonicalId`,
  keeping the highest `RelevanceScore` (ties broken by `SourceUrl`, ordinal).
- **Proposals only stage sensors.** Survivors become `catalog_staging_sensor` rows;
  approval commits to the live `sensor` table (the write gate's sensor commit path).
- **Empty proposals → no batch.** When nothing clears the threshold, `RunFromDigestFile`
  returns `BatchId == null` and the catalog db is left untouched (not even seeded).

## Tests

| Area | Test |
|------|------|
| Confidence partition (propose vs log-only) | `ProjectAegis.Data.Tests/Osint/OsintProposalGateTests` |
| Connectors deterministic + tolerant parsing | `ProjectAegis.Data.Tests/Osint/OsintConnectorTests` |
| Digest dedupe / map / propose round-trip | `ProjectAegis.Data.Tests/Osint/OsintDigestRunnerTests` |
| TRL clamp + doc-09/10 branch routing (S22-07) | `ProjectAegis.Data.Tests/Osint/OsintCatalogMapperTests` |

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "FullyQualifiedName~Osint" -v minimal
```
