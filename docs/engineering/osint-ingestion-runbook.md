# OSINT ingestion — proposal pipeline runbook

Developer/operator guide for the OSINT (open-source intelligence) ingestion
pipeline: how raw discoveries become **staged** catalog proposals that a human
approves through the existing catalog write gate. The product intent lives in
[`05-Dynamic-Systems-Agent.md`](../../Game-Requirements/requirements/05-Dynamic-Systems-Agent.md)
(the "Dynamic Systems Agent", DSA); this doc is the **how-to**: the pipeline
stages, the CLI/MCP verbs, the data shapes, and the constraints so you don't
expect behavior that isn't wired yet.

The headless services live in `src/ProjectAegis.Data/Osint/` (namespace
`ProjectAegis.Data.Osint`); the CLI verbs are in
`src/ProjectAegis.MissionEditor.Cli/`.

## Intent

Requirement 05 wants an agent that monitors open-source signals for emerging and
speculative military tech, then **proposes** new catalog systems for review. The
agent is the discovery/proposal **front door** onto the catalog governance — the
staged write gate (`IWriteGate`), provenance, and human approval — not a parallel
write path. Per Req 05 and the Database Intelligence layer (Req 06):

- **Propose, never commit.** OSINT never writes live catalog tables. Discoveries
  are staged through `IWriteGate.ProposeSensorBatch` and only become live when a
  human runs `ApproveBatch`.
- **Confidence-gated.** Only discoveries at or above the proposal confidence
  threshold (`0.65` default) are staged; everything below is log-only (DSA-2.1).
- **No 24/7 social listener (MVP).** `OsintDigestRunner.EnableRealtimeSocialStream`
  is hard-`false` (DSA-1.3). Ingestion is batch/on-demand, not a live stream.
- **Deterministic.** Connectors and the runner do no network I/O or wall-clock
  reads in the hot path and emit a stable sort (`SourceUrl`, then `CanonicalId`),
  so the same input always produces the same staged batch.

## Pipeline

```
fetch (connector or digest file)
  →  dedupe by canonicalId
  →  proposal gate (confidence ≥ threshold)
  →  map to CatalogSensorBinding
  →  ProposeSensorBatch  (staging only)
  →  human ApproveBatch  →  commit
```

| Stage | Type | Notes |
|-------|------|-------|
| Fetch | `IOsintConnector` / `OsintDigestRunner.RunFromDigestFile` | Reads a JSON source into `OsintDiscoveryRecord[]`. |
| Dedupe | `OsintDigestRunner.DedupeDiscoveries` | Groups by `CanonicalId`, keeps the highest `RelevanceScore` (tie-break on `SourceUrl`). Digest-file path only. |
| Gate | `OsintProposalGate.Partition` | Splits into `(Proposals, LogOnly)` at `minimumConfidence` (default `0.65`, clamped 0–1). |
| Map | `OsintCatalogMapper` / `OsintDigestRunner.MapProposalsToBindings` | Converts proposals to `CatalogSensorBinding` rows (see "Mapping paths"). |
| Stage | `CatalogWriteGate.ProposeSensorBatch` | Returns a `batchId`; nothing is committed. |
| Approve | `CatalogWriteGate.ApproveBatch` | Human-gated commit (via `osint_staging_review --approve`). |

## Data shapes

`OsintDiscoveryRecord` (`src/ProjectAegis.Data/Osint/OsintDiscoveryRecord.cs`) is
the normalized hit every connector and digest file produces:

| Field | Meaning |
|-------|---------|
| `CanonicalId` | Stable id for the discovered system (dedupe key; sort tiebreak). |
| `SourceUrl` | Citation URL (primary sort key; `CitationRef`/`SourceFile` on the staged row). |
| `Snippet` | Human-readable evidence excerpt. |
| `RelevanceScore` | `0–1` confidence used by the proposal gate. |
| `TargetDoc` | Routing gate: `"09"` (near-future) or `"10"` (speculative). |
| `ProposedTrl` | Technology Readiness Level; clamped to `1–9` on the `OsintCatalogMapper` path. |
| `ObservedUtcTicks` | Optional observation timestamp (provenance only). |

Connector JSON fixtures are an **array** of objects with camelCase or PascalCase
keys (both tolerated). The digest-file format wraps records under a
`"discoveries"` array (see `OsintDigestRunner.OsintDigestFile`). Reference
fixtures: [`data/osint_facts.json`](../../data/osint_facts.json) (connector array)
and `src/ProjectAegis.Data.Tests/Fixtures/osint-digest-fixture.json` (digest).

## Connectors

All connectors implement `IOsintConnector.Fetch()` and **must be deterministic
and never throw** — parse errors return an empty array, not an exception.

| Connector | Source | Use |
|-----------|--------|-----|
| `FileOsintConnector` | Local JSON array file | CLI/MCP `osint_search`, tests. |
| `RssOsintConnector` | JSON fixture, or a built-in demo record when no path is given | RSS/HTTP stub for demos (no live network yet). |
| `InMemoryOsintConnector` | In-process records (or a built-in fixture) | Unit tests / demos. |

> Network-backed RSS/HTTP fetching is a stub. `RssOsintConnector` reads a fixture
> file or returns a single deterministic demo record; it does **not** open a live
> feed. Treat the connector boundary as the seam where a real fetcher will land.

## Mapping paths

Two mappers turn proposals into `CatalogSensorBinding` rows. They differ by
caller and provenance detail — pick the right one:

- **`OsintCatalogMapper` (S22-07 TL routing).** Used by the Unity staging panel
  (`OsintStagingPanelHost`). Encodes the doc-routing gate:
  - `ResolveTrlLevel(proposedTrl)` → clamp to `1–9`, stored as `TrlLevel`.
  - `ResolveBranchTag(targetDoc)` → `branch:doc-09` / `branch:doc-10`, stored as
    `ImportBatchId` (the Database Intelligence branch selector).
  - `ResolveSourceFactId(targetDoc)` → `osint:09` / `osint:10`.
  - `BasePd` = `RelevanceScore` clamped to `0.1–0.95`; `SensorId` =
    `osint-<canonicalId>`; `ReviewState` = `Provisional`; `ValueTier` =
    `InterpretedValue`.
- **`OsintDigestRunner.MapProposalsToBindings` (headless digest-file path).** Used
  by `RunFromDigestFile`. Splits `CanonicalId` on `/` into `(platformId,
  sensorId)`, sets a fixed `BasePd` of `0.5`, `SourceFactId` = `osint:<targetDoc>`,
  and `Confidence` = `RelevanceScore`. No branch tag is applied on this path.

Both emit rows sorted by `(PlatformId, SensorId)` for stable batches.

## Workflow (CLI)

1. **Search / propose** from a connector fixture. Runs fetch → gate and prints the
   proposals + log-only count (it does **not** stage on its own — see note).

   ```bash
   dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
     osint_search [--db <fixture.json>]
   ```

   With no `--db`, it reads the committed fixture `data/osint_facts.json` (or
   returns empty if absent). Returns JSON
   `{ ok, proposals[], logOnlyCount }`.

2. **List staging proposals** pending in the write gate.

   ```bash
   dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
     osint_staging_review --db <catalog.db>
   ```

   Returns JSON `{ ok, pending[] }` with `batchId`, `recordCount`, `actorType`,
   `approvalState` for each open batch.

3. **Approve** a staged batch to commit it (human reviewer).

   ```bash
   dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
     osint_staging_review --db <catalog.db> --approve <batchId>
   ```

   Returns JSON `{ ok, batchId, errors[] }`; `ok` is true only when the batch
   committed. Approval is recorded with actor `human` / `osint-ui-reviewer`.

> `osint_search` exercises fetch + the confidence gate and reports what *would*
> stage; the headless digest-file path (`OsintDigestRunner.RunFromDigestFile`,
> exercised by tests) is what actually calls `ProposeSensorBatch` to create a
> batch. Either way, nothing commits without step 3.

### MCP

The verbs are registered in
[`tools/mission-editor/mcp-tools.json`](../../tools/mission-editor/mcp-tools.json)
and route through `Invoke-MissionEditorMcp.ps1`:

| MCP verb | Backing CLI command |
|----------|---------------------|
| `osint_search` | `osint_search` |
| `osint_digest` | `osint_search` |
| `osint_list_staging_proposals` | `osint_staging_review` (list) |
| `osint_get_proposal_detail` | `osint_staging_review` (list) |
| `osint_submit_review_decision` | `osint_staging_review --approve <batchId>` |

`osint_list_staging_proposals` requires `db`; `osint_submit_review_decision`
takes `db` + `batchId`.

## Constraints / pitfalls

- **No auto-commit, ever.** The pipeline only stages. Nothing reaches live
  catalog tables without `osint_staging_review --approve` (`ApproveBatch`). Keep
  it that way.
- **No live feeds.** `EnableRealtimeSocialStream` is `false` and the RSS
  connector is a fixture/demo stub. Don't document or wire a 24/7 listener until
  Req 05 MVP scope changes.
- **Threshold is policy, not magic.** Below `0.65` confidence a discovery is
  log-only and is silently dropped from staging. If proposals "disappear", check
  `RelevanceScore` against the threshold before suspecting a bug.
- **Determinism is load-bearing.** Connectors and the runner must not do network
  I/O or read wall-clock in `Fetch`/`Run`, and must keep the
  `(SourceUrl, CanonicalId)` sort. Breaking either makes staged batches
  non-reproducible.
- **Connectors swallow errors by design.** A malformed fixture yields an empty
  result, not a throw. An empty `osint_search` may mean a bad/missing file, not
  "no discoveries".
- **`TargetDoc` drives branch routing.** Only `"09"` and `"10"` are normalized;
  blank defaults to `"10"`. A wrong `targetDoc` routes a staged row to the wrong
  Database Intelligence branch (`branch:doc-09` vs `branch:doc-10`).
- **Two mappers, two provenance shapes.** Don't assume `osint_search`/digest rows
  carry the S22-07 branch tag — only the `OsintCatalogMapper` (Unity panel) path
  sets `ImportBatchId`.

## Tests

- `src/ProjectAegis.Data.Tests/Osint/` — `OsintProposalGateTests` (threshold
  partitioning), `OsintDigestRunnerTests` (dedupe, mapping, end-to-end staging),
  `OsintCatalogMapperTests` (TL clamp, branch-tag routing, deterministic order),
  and `OsintConnectorTests` (deterministic, no-throw `Fetch`).
- `src/ProjectAegis.MissionEditor.Cli.Tests/` — CLI verb coverage for
  `osint_search` / `osint_staging_review`.
