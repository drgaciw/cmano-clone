# OSINT ingestion — advisory catalog proposals

This subsystem turns open-source-intelligence (OSINT) findings into **staged,
advisory** catalog proposals. It implements the OSINT slice of
[requirement 05 — Dynamic Systems Agent](../../../Game-Requirements/requirements/05-Dynamic-Systems-Agent.md)
(`DSA-1.x` / `DSA-2.1`).

OSINT is strictly advisory: it **never** writes to the live catalog. Discoveries
above a confidence threshold become a *proposed* batch through the
[write gate](../WriteGate/README.md); promotion to the live catalog is always a
separate, human/TL-approved decision.

## Pipeline

```
IOsintConnector.Fetch()            // File / Rss / InMemory — deterministic, no network in hot path
        │  OsintDiscoveryRecord[]
        ▼
OsintDigestRunner.Run / DedupeDiscoveries
        │  dedupe by CanonicalId (keep highest RelevanceScore)
        ▼
OsintProposalGate.Partition(threshold = 0.65)
        │  → Proposals (score ≥ threshold)   → staged
        │  → LogOnly  (score <  threshold)   → recorded, not staged
        ▼
OsintCatalogMapper / MapProposalsToBindings
        │  OsintDiscoveryRecord → CatalogSensorBinding (provisional, InterpretedValue tier)
        ▼
IWriteGate.ProposeSensorBatch(...)  // staging only — awaits human/TL approval
```

## Key types

| Type | Responsibility |
|------|----------------|
| `OsintDiscoveryRecord` | Normalized hit: `CanonicalId, SourceUrl, Snippet, RelevanceScore, TargetDoc, ProposedTrl, ObservedUtcTicks`. |
| `Connectors/IOsintConnector` | `OsintDiscoveryRecord[] Fetch()`. All impls must be deterministic (stable sort by `SourceUrl` then `CanonicalId`). |
| `Connectors/FileOsintConnector` | Reads a local JSON array fixture; returns empty (never throws) on missing/invalid input. |
| `Connectors/RssOsintConnector` | RSS/HTTP-style stub; reads a JSON fixture when a path is given, otherwise yields a deterministic demo record. No network in the hot path. |
| `Connectors/InMemoryOsintConnector` | Demo/test fixture connector with a built-in default set. |
| `OsintProposalGate` | Confidence gate: partitions discoveries into proposals vs log-only at `DefaultProposalConfidenceThreshold = 0.65`. |
| `OsintDigestRunner` | Orchestrates read → dedupe → gate → map → propose. `RunFromDigestFile` runs the full path to a SQLite catalog. |
| `OsintCatalogMapper` | Maps a discovery to a `CatalogSensorBinding` and resolves TRL/doc routing. |

## Confidence gate (`OsintProposalGate`)

- Threshold defaults to **0.65** and is clamped to `[0, 1]`.
- `RelevanceScore >= threshold` → **proposal** (staged); below → **log-only**
  (counted, never staged).
- Output is sorted by `(SourceUrl, CanonicalId)` for deterministic batches.

## TRL / doc routing (`OsintCatalogMapper`)

Findings are routed by their **target doc gate** and **technology readiness
level** so speculative data lands in the right provenance branch:

- `ResolveTrlLevel(proposedTrl)` → clamped to **1–9** for the staged
  `CatalogSensorBinding.TrlLevel`.
- `ResolveBranchTag(targetDoc)` → `branch:doc-09` (near-future) or
  `branch:doc-10` (speculative); unknown/blank `targetDoc` normalizes to `10`.
- `ResolveSourceFactId(targetDoc)` → `osint:{09|10}` provenance fact id.
- Staged rows are written as `CatalogReviewStates.Provisional` with
  `CatalogProvenanceTier.InterpretedValue` — i.e. clearly marked as
  unverified interpretation, not authoritative fact.

## Digest runner (`OsintDigestRunner`)

- `EnableRealtimeSocialStream = false` — by policy (DSA-1.3) the MVP must not run
  a 24/7 real-time social listener; ingestion is digest/on-demand only.
- `RunFromDigestFile(databasePath, digestPath, clock?)`:
  reads + dedupes a digest JSON, partitions via the proposal gate, and — only if
  there are proposals — seeds a Baltic-patrol catalog if the DB is missing, maps
  proposals to sensor bindings, and stages one batch via
  `CatalogWriteGate.ProposeSensorBatch`. Returns
  `OsintDigestRunResult(ParsedTotal, ProposalCount, LogOnlyCount, BatchId?)`.
- Determinism: dedupe keeps the highest-scoring record per `CanonicalId`; time
  comes from an injected `ICatalogClock` (default `FixedCatalogClock(0)`).

### Digest JSON shape

```json
{
  "discoveries": [
    {
      "canonicalId": "platform-x/radar-y",
      "sourceUrl": "https://example.org/report",
      "snippet": "observed long-range AESA",
      "relevanceScore": 0.78,
      "targetDoc": "09",
      "proposedTrl": 7,
      "observedUtcTicks": 0
    }
  ]
}
```

Property names are case-insensitive (camelCase by default). A fixture lives at
`src/ProjectAegis.Data.Tests/Fixtures/osint-digest-fixture.json`
(`OsintDigestRunner.ResolveFixtureDigestPath()`).

## CLI / MCP surface

The mission-editor CLI exposes the on-demand search and the reviewer flow
(see [`MissionEditor.Cli` README](../../ProjectAegis.MissionEditor.Cli/README.md)):

```bash
# On-demand search (FileOsintConnector over data/osint_facts.json) — MCP search_osint
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- osint_search [--db <fixture.json>]

# Review staged OSINT proposals; optionally approve one through the write gate
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- osint_staging_review --db <catalog.db> [--approve <batchId>]
```

## Constraints

- **Advisory only.** OSINT stages proposals; it never commits to the live
  catalog. Approval is a separate write-gate decision.
- **Deterministic & offline in the hot path.** Connectors sort stably and never
  throw on missing/invalid input (return empty); no network or wall-clock reads
  during `Fetch`.
- **Provisional provenance.** Staged rows are `Provisional` /
  `InterpretedValue` and carry `osint:*` source-fact ids and doc-branch tags.

## Tests

`src/ProjectAegis.Data.Tests/Osint/` (`OsintDigestRunnerTests`,
`OsintConnectorTests`, mapper/gate tests).

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter Osint -v minimal
```

## See also

- [ProjectAegis.Data overview](../README.md)
- [WriteGate — staged catalog writes](../WriteGate/README.md)
- [Requirement 05 — Dynamic Systems Agent](../../../Game-Requirements/requirements/05-Dynamic-Systems-Agent.md)
