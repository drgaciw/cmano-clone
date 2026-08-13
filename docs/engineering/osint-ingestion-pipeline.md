# OSINT ingestion pipeline — developer guide

**OSINT** (open-source intelligence) is one of the sanctioned Track-B feeds for the platform
catalog: public-source *discoveries* about near-future / speculative sensors are normalized,
confidence-filtered, mapped to catalog sensor rows, and **proposed as staged `provisional` rows
through the extend-only catalog write gate** — never committed directly, always human-approved. It
is the automated cousin of the [CMO markdown import](cmo-markdown-import.md): a different source, the
**same write-gate discipline**.

This page documents the ingestion path end to end — the connectors, the discovery record, the
confidence gate, the digest runner, and the two catalog mappers — and the constraints that keep it
deterministic and firewall-compliant. It complements, and does not duplicate, the CLI verb reference
in [mission-editor-cli.md](mission-editor-cli.md) and the provenance policy in the
[dual-track firewall doc](dual-track-cmo-analysis-and-catalog.md).

- **Source:** [`src/ProjectAegis.Data/Osint/`](../../src/ProjectAegis.Data/Osint/) (the runner,
  gate, mapper, discovery record) and
  [`Osint/Connectors/`](../../src/ProjectAegis.Data/Osint/Connectors/) (the source adapters). The
  CLI proxy is [`OsintStagingReviewCommand`](../../src/ProjectAegis.MissionEditor.Cli/OsintStagingReviewCommand.cs).
- **Related:** the write gate the proposals land in is
  [catalog-write-gate.md](catalog-write-gate.md); the provenance firewall (why OSINT is allowed and
  CMO `*.db3` is not) is [dual-track-cmo-analysis-and-catalog.md](dual-track-cmo-analysis-and-catalog.md)
  ([ADR-013](../architecture/adr-013-cmo-scenario-import-policy.md)); the `BasePd` these rows carry
  is consumed by the [detection pipeline](detection-pipeline.md).

> **Staging only, human-gated, firewall-bound.** OSINT never writes an approved catalog row. It
> `Propose*`s staged rows as `provisional` / `interpreted_value` provenance under the actor
> `"agent"`; a human must `Approve*` them (`"human"`). The MVP explicitly does **not** run a 24/7
> real-time social listener (`OsintDigestRunner.EnableRealtimeSocialStream = false`, DSA-1.3), and
> OSINT is one of the whitelisted Track-B feeds — proprietary CMO `*.db3` is never a source.

---

## Where it lives

| File | Role |
|------|------|
| [`OsintDiscoveryRecord.cs`](../../src/ProjectAegis.Data/Osint/OsintDiscoveryRecord.cs) | The normalized hit: `(CanonicalId, SourceUrl, Snippet, RelevanceScore, TargetDoc, ProposedTrl, ObservedUtcTicks)`. |
| [`Connectors/IOsintConnector.cs`](../../src/ProjectAegis.Data/Osint/Connectors/IOsintConnector.cs) | The one-method source contract: `OsintDiscoveryRecord[] Fetch()`. Every impl must be deterministic. |
| [`Connectors/FileOsintConnector.cs`](../../src/ProjectAegis.Data/Osint/Connectors/FileOsintConnector.cs) | Local JSON-array fixture source (tolerant parse, empty on missing/error). |
| [`Connectors/RssOsintConnector.cs`](../../src/ProjectAegis.Data/Osint/Connectors/RssOsintConnector.cs) | RSS/HTTP-style stub: fixture-driven when a path is given, else one deterministic demo record. |
| [`Connectors/InMemoryOsintConnector.cs`](../../src/ProjectAegis.Data/Osint/Connectors/InMemoryOsintConnector.cs) | In-memory / default-fixture source for demos and tests. |
| [`OsintProposalGate.cs`](../../src/ProjectAegis.Data/Osint/OsintProposalGate.cs) | Confidence partition: `Partition(discoveries, minConfidence=0.65)` → `(Proposals, LogOnly)`. |
| [`OsintDigestRunner.cs`](../../src/ProjectAegis.Data/Osint/OsintDigestRunner.cs) | The headless digest → dedupe → gate → map → **propose** driver (`RunFromDigestFile`). |
| [`OsintCatalogMapper.cs`](../../src/ProjectAegis.Data/Osint/OsintCatalogMapper.cs) | S22-07 doc/TL routing mapper: TRL clamp + `branch:doc-09/10` tag + `osint:*` source-fact id. |
| [`OsintStagingReviewCommand.cs`](../../src/ProjectAegis.MissionEditor.Cli/OsintStagingReviewCommand.cs) | CLI proxy: list pending staged batches / approve one by id. |

---

## The pipeline at a glance

```text
IOsintConnector.Fetch()            data/osint_facts.json (array)  OR  *.digest.json ({ "discoveries": [...] })
        │  OsintDiscoveryRecord[]  (stable Ordinal sort: SourceUrl → CanonicalId)
        ▼
OsintDigestRunner.DedupeDiscoveries      keep max RelevanceScore per CanonicalId (tie: SourceUrl desc)
        ▼
OsintProposalGate.Partition(min=0.65)    RelevanceScore ≥ min → Proposals ; else → LogOnly (dropped/logged)
        ▼
MapProposalsToBindings / OsintCatalogMapper   → CatalogSensorBinding[]  (provisional, interpreted_value)
        ▼
CatalogWriteGate.ProposeSensorBatch(actorType:"agent", actorId:"osint-digest-runner")   → staged batchId
        ▼
[human] osint_staging_review --approve <batchId>   → CatalogWriteGate.ApproveBatch(actorType:"human")
```

Everything above the human-approval line is automated and deterministic; the approval line is the
hard gate that keeps a speculative public-source claim from silently becoming authoritative catalog
data.

---

## The discovery record & connectors

A [`OsintDiscoveryRecord`](../../src/ProjectAegis.Data/Osint/OsintDiscoveryRecord.cs) is the single
normalized currency of the pipeline:

| Field | Meaning |
|-------|---------|
| `CanonicalId` | Stable id for the discovery; also the dedupe key and the `platform/sensor` split source (below). |
| `SourceUrl` | Public citation URL — copied to the staged row's `CitationRef`. |
| `Snippet` | Free-text evidence excerpt (not written to the catalog). |
| `RelevanceScore` | `[0,1]` confidence — the value the proposal gate thresholds on. |
| `TargetDoc` | Provenance-doc routing tag: `"09"` (near-future) / `"10"` (speculative). |
| `ProposedTrl` | Suggested technology-readiness level (staged as `TrlLevel`, clamped `1–9` by the mapper). |
| `ObservedUtcTicks` | When the fact was observed (data only; not folded into any sim hash). |

**Connectors** ([`IOsintConnector`](../../src/ProjectAegis.Data/Osint/Connectors/IOsintConnector.cs))
have a single method — `OsintDiscoveryRecord[] Fetch()` — and three rules that every implementation
follows: **deterministic** output (always sorted `SourceUrl → CanonicalId`, `StringComparer.Ordinal`),
**never throw** (a missing file or parse error yields an empty array, not an exception), and **no
network in the hot path** (the "RSS/HTTP" connector is a fixture-driven stub for now).

| Connector | `Fetch()` behaviour |
|-----------|---------------------|
| `FileOsintConnector(path)` | Parses a JSON **array** of discovery objects (tolerant of casing / missing fields — defaults `relevanceScore 0.5`, `targetDoc "10"`, `proposedTrl 5`). Empty when the path is blank / missing / not an array. |
| `RssOsintConnector(path="")` | With a path, same array parse as `File`; with **no** path, returns one deterministic demo record (`rss-demo-hypersonic`, `0.76`, doc `10`). |
| `InMemoryOsintConnector(records?)` | Returns the supplied records (or a built-in three-record demo fixture) sorted for determinism. |

> **Two JSON shapes.** Connector fixtures (e.g. [`data/osint_facts.json`](../../data/osint_facts.json),
> the `osint_search` verb) are a **flat array** `[ {...}, {...} ]`. The digest-file path
> (`OsintDigestRunner.RunFromDigestFile` /
> [`osint-digest-fixture.json`](../../src/ProjectAegis.Data.Tests/Fixtures/osint-digest-fixture.json))
> is an **object** `{ "discoveries": [ {...} ] }`. Same field names, different envelope — pick the
> shape that matches the entry point.

---

## The confidence gate (`OsintProposalGate`)

`OsintProposalGate.Partition(discoveries, minimumConfidence = 0.65)` is a pure split
(**DSA-2.1 / doc 05**). It clamps the threshold to `[0,1]`, iterates in the same stable
`SourceUrl → CanonicalId` Ordinal order, and routes each record by

```
RelevanceScore ≥ minimumConfidence  →  Proposals   (staged for write-gate propose)
RelevanceScore <  minimumConfidence  →  LogOnly     (recorded but never proposed)
```

The default `0.65` is `DefaultProposalConfidenceThreshold`; the digest runner accepts an override in
its constructor. `LogOnly` discoveries are the audit tail — low-confidence chatter that is counted
(`OsintDigestRunResult.LogOnlyCount`) but kept out of the catalog.

---

## The digest runner (`OsintDigestRunner`)

The headless driver, wired to the write gate (**DSA-1.x / S19-05**).

`RunFromDigestFile(databasePath, digestPath, clock?)` is the end-to-end entry point:

1. **Read** the `{ "discoveries": [...] }` digest (`ReadDiscoveries` → `OsintDiscoveryRecord[]`).
2. **Dedupe** by `CanonicalId` — `DedupeDiscoveries` keeps the highest `RelevanceScore` per id, tie-broken by `SourceUrl` descending (Ordinal).
3. **Gate** via `OsintProposalGate.Partition` (default `0.65`).
4. **If any proposals**: seed the Baltic Patrol catalog when the DB is absent
   (`CatalogSeedBootstrap.SeedBalticPatrol(..., overwrite: false)`), map proposals to sensor bindings
   (`MapProposalsToBindings`), and open a `CatalogWriteGate` on an injected `ICatalogClock`
   (`FixedCatalogClock(0)` by default, so runs are reproducible) to
   `ProposeSensorBatch(bindings, "agent", "osint-digest-runner", "osint_digest:{file}")`.
5. **Return** `OsintDigestRunResult(ParsedTotal, ProposalCount, LogOnlyCount, BatchId?)` — `BatchId`
   is `null` when nothing cleared the gate.

Two helpers are worth knowing:

- **`ResolvePlatformSensorIds(canonicalId)`** — splits on the first `'/'` into `(platformId,
  sensorId)`; a `canonicalId` without a slash maps to `(id, id)`. So `"u-hypersonic/radar-glide"` →
  platform `u-hypersonic`, sensor `radar-glide`.
- **`MapProposalsToBindings(proposals, sourceFile)`** — the runner's inline mapper. Each staged
  [`CatalogSensorBinding`](../../src/ProjectAegis.Data/Catalog/CatalogSensorBinding.cs) carries a
  fixed `BasePd = 0.5`, `SourceFactId = "osint:{TargetDoc}"`, `Confidence = RelevanceScore`,
  `SourceFile = {digest file}`, `ReviewState = provisional`, `TrlLevel = ProposedTrl`,
  `ValueTier = interpreted_value`, `CitationRef = SourceUrl`; the batch is sorted `PlatformId →
  SensorId` Ordinal for a stable batch id.

---

## The doc/TL routing mapper (`OsintCatalogMapper`)

`OsintCatalogMapper` is the **S22-07** mapper used by the connector / MCP `osint_search` path (and
available for direct mapping). It differs from the runner's inline mapper in three deliberate ways —
it derives `BasePd` from confidence, tags the target doc branch, and clamps the TRL:

| Helper | Behaviour |
|--------|-----------|
| `ResolveTrlLevel(proposedTrl)` | Clamp to `[1, 9]`. |
| `ResolveBranchTag(targetDoc)` | `"branch:doc-" + normalized`, where normalize maps blank → `"10"`, `"09"`/`"9"` → `"09"`, `"10"` → `"10"`, else the trimmed value. Routes staged rows to the doc-09 (near-future) / doc-10 (speculative) provenance branch. |
| `ResolveSourceFactId(targetDoc)` | `"osint:" + normalized`. |
| `ToSensorBinding(record, platformId="osint-platform")` | `SensorId = "osint-" + lower-kebab(CanonicalId)`, `BasePd = clamp(RelevanceScore, 0.1, 0.95)`, `ImportBatchId = ResolveBranchTag(...)`, `ReviewState = provisional`, `ValueTier = interpreted_value`, `ReviewerId = "osint-digest"`, `CitationRef = SourceUrl`. |

`ToSensorBindings(records, platformId)` maps and returns them sorted `PlatformId → SensorId` Ordinal.
Both mappers land **`provisional` / `interpreted_value`** rows — the difference is only in how
`BasePd`, the batch/branch tag, and TRL are derived.

---

## CLI / MCP runbook

The headless verbs (full table in [mission-editor-cli.md](mission-editor-cli.md#osint)):

```bash
# 1. Ingest the committed fixture and see what clears the 0.65 gate (proposals + logOnlyCount)
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- osint_search [--db <fixture.json>]

# 2. List staged OSINT proposal batches awaiting review
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- osint_staging_review --db <catalog.db>

# 3. Human approves a specific staged batch (commits the provisional rows)
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- osint_staging_review --db <catalog.db> --approve <batchId>
```

- `osint_search` builds a `FileOsintConnector` over `data/osint_facts.json` (empty, gracefully, if
  absent) and runs `OsintDigestRunner(0.65)`.
- `osint_staging_review` with no `--approve` lists pending batches via
  `CatalogWriteGate.ListPendingBatches()`; with `--approve <batchId>` it calls
  `ApproveBatch(batchId, "human", "osint-ui-reviewer")` — the **human** actor is what distinguishes
  approval from the agent-authored proposal.
- MCP aliases collapse onto these two verbs: `osint_digest` → `osint_search`, and
  `osint_list_staging_proposals` / `osint_get_proposal_detail` / `osint_submit_review_decision` →
  `osint_staging_review`.

---

## Constraints & determinism

- **No real-time social listener in MVP** — `OsintDigestRunner.EnableRealtimeSocialStream = false`
  (DSA-1.3). Ingestion is digest / on-demand only.
- **Deterministic throughout** — connectors sort Ordinal and never throw; the runner injects a
  `FixedCatalogClock(0)` so a given digest yields a reproducible batch id; there is no wall-clock or
  RNG in the ingestion path.
- **Extend-only write gate** — OSINT only ever calls `Propose*`; it must not alter approved rows or
  add a new write path into `CatalogWriteGate` (see [catalog-write-gate.md](catalog-write-gate.md)).
- **Provenance is explicit** — staged rows are `review_state = provisional`,
  `value_tier = interpreted_value`, with `CitationRef` = the public `SourceUrl`. That keeps OSINT
  data visibly distinct from `source_fact` / curated fixtures under the
  [dual-track firewall](dual-track-cmo-analysis-and-catalog.md).
- **Human approval is mandatory** — nothing OSINT proposes is authoritative until a `"human"` actor
  approves the batch.

---

## Common pitfalls

- **Wrong JSON envelope.** The connector path wants a flat array; `RunFromDigestFile` wants
  `{ "discoveries": [...] }`. Feeding one to the other yields an empty ingest (connectors) rather
  than an error — check `ParsedTotal` if a run proposes nothing.
- **Expecting direct commit.** OSINT proposes; it never approves. A proposal with `BatchId != null`
  is *staged*, not live — approval is a separate human step.
- **Assuming a single mapper.** The runner's `MapProposalsToBindings` (fixed `BasePd 0.5`) and
  `OsintCatalogMapper.ToSensorBinding` (confidence-derived `BasePd`, branch tag, TRL clamp) are
  distinct paths; know which one your entry point uses.
- **Slash semantics in `CanonicalId`.** A `platform/sensor` id splits on the *first* slash; an id
  without a slash becomes both the platform and the sensor id.
- **Low-confidence surprise.** Records below the threshold land in `LogOnly` and never reach the
  catalog — raise `RelevanceScore` in the source, not the code, to promote them.

---

## Tests

| Test file | Covers |
|-----------|--------|
| [`OsintConnectorTests`](../../src/ProjectAegis.Data.Tests/Osint/OsintConnectorTests.cs) | Deterministic `Fetch()` across File / Rss / InMemory, tolerant parse, empty-on-missing. |
| [`OsintProposalGateTests`](../../src/ProjectAegis.Data.Tests/Osint/OsintProposalGateTests.cs) | Threshold partition + clamp, stable ordering. |
| [`OsintDigestRunnerTests`](../../src/ProjectAegis.Data.Tests/Osint/OsintDigestRunnerTests.cs) | Dedupe, gate, `platform/sensor` split, propose-batch wiring, `OsintDigestRunResult`. |
| [`OsintCatalogMapperTests`](../../src/ProjectAegis.Data.Tests/Osint/OsintCatalogMapperTests.cs) | TRL clamp, branch/source-fact tags, binding fields. |

Run the data suite after any change here:

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj -v minimal
```

---

## See also

| Topic | Doc |
|-------|-----|
| The write gate OSINT proposals land in (propose/approve, staging) | [catalog-write-gate.md](catalog-write-gate.md) |
| Provenance firewall — why OSINT is allowed, CMO `*.db3` is not | [dual-track-cmo-analysis-and-catalog.md](dual-track-cmo-analysis-and-catalog.md) · [ADR-013](../architecture/adr-013-cmo-scenario-import-policy.md) |
| The other Track-B feed with the same gate discipline | [cmo-markdown-import.md](cmo-markdown-import.md) |
| CLI / MCP verb reference (incl. the OSINT verbs) | [mission-editor-cli.md](mission-editor-cli.md) |
| How the staged `BasePd` is consumed at runtime | [detection-pipeline.md](detection-pipeline.md) |
| The `ProjectAegis.Data` catalog layer overview | [`src/ProjectAegis.Data/README.md`](../../src/ProjectAegis.Data/README.md) |
