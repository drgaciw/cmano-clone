# OSINT ingestion → catalog staging

> **Subsystem:** `ProjectAegis.Data.Osint` (+ `ProjectAegis.MissionEditor.Cli` verbs)
> **Decision of record:** [ADR-006 — Data layer boundary](../architecture/adr-006-data-layer-boundary.md)
> **Requirements:** Req 05 (OSINT — DSA-1.x / DSA-2.1), Req 06 (Database Intelligence)

OSINT ingestion turns external open-source discoveries (a digest file, a connector feed, or an
on-demand MCP search) into **staged, never-committed** catalog proposals. Every discovery is
score-gated, deduplicated, mapped to a `CatalogSensorBinding`, and handed to the
[catalog write gate](catalog-write-gate.md) as a *pending* batch. Nothing reaches the live
catalog without a separate human approve — this page documents the pipeline exactly as it
behaves in source today, including the two mapping paths that produce **different** staged
metadata.

## Pipeline at a glance

```
source ──▶ connector / digest ──▶ proposal gate ──▶ mapper ──▶ write gate (PROPOSE)
(RSS/file/      Fetch()           confidence ≥ θ    binding     staged batch, pending
 MCP/JSON)    dedupe by id        else log-only                 │
                                                                ▼
                                                     human approve (separate step)
                                                                │
                                                                ▼
                                                          live catalog
```

- **Source / connector** — `IOsintConnector.Fetch()` returns `OsintDiscoveryRecord[]`, always
  deterministically sorted by `(SourceUrl, CanonicalId)` ordinal. Implementations:
  `FileOsintConnector`, `RssOsintConnector`, `InMemoryOsintConnector`
  (`src/ProjectAegis.Data/Osint/Connectors/`). All are deterministic and never throw on a parse
  error — they return an empty array instead.
- **Proposal gate** — `OsintProposalGate.Partition` splits records into `Proposals` and
  `LogOnly` at a confidence threshold.
- **Mapper** — converts a proposal to a `CatalogSensorBinding` (see the two paths below).
- **Write gate** — `CatalogWriteGate.ProposeSensorBatch` stages the bindings as one pending
  batch; commit is a separate approve.

## The discovery record

`OsintDiscoveryRecord` (`src/ProjectAegis.Data/Osint/OsintDiscoveryRecord.cs`) is the normalized
unit that flows through the whole pipeline:

| Field | Meaning |
|-------|---------|
| `CanonicalId` | Stable identity; drives dedupe and (in the digest path) `platform/sensor` split |
| `SourceUrl` | Citation; also the secondary deterministic sort key |
| `Snippet` | Human-readable evidence text |
| `RelevanceScore` | `[0, 1]` confidence — the value the proposal gate compares |
| `TargetDoc` | Provenance routing gate — `"09"` (near-future) or `"10"` (speculative) |
| `ProposedTrl` | Technology Readiness Level to stage on the row |
| `ObservedUtcTicks` | Observation time (optional; not used for gating) |

## Confidence gate (`OsintProposalGate`)

`Partition(discoveries, minimumConfidence = 0.65)` keeps a discovery as a **proposal** when
`RelevanceScore >= threshold`, otherwise it is **log-only** and never staged. The threshold is
clamped to `[0, 1]`, and the default is `OsintProposalGate.DefaultProposalConfidenceThreshold =
0.65` (DSA-2.1). The gate is inclusive — a score exactly equal to the threshold is a proposal.

## Two mapping paths — they are not interchangeable

⚠️ This is the single most important thing to understand about this subsystem. There are two
mappers that produce **different staged metadata**, and the one you get depends on the entry
point you call.

### Path A — `OsintCatalogMapper` (S22-07, TL-aware)

`OsintCatalogMapper.ToSensorBinding` is the routing-aware mapper. Use it when you need TRL
clamping and doc-routing branch tags on the staged row.

| Staged field | Value | Source |
|--------------|-------|--------|
| `SensorId` | `"osint-" + canonicalId` (spaces → `-`, lowercased) | `ToSensorBinding` |
| `BasePd` | `RelevanceScore` clamped to `[0.1, 0.95]` | `ToSensorBinding` |
| `TrlLevel` | `ProposedTrl` clamped to `[1, 9]` | `ResolveTrlLevel` |
| `ImportBatchId` | `"branch:doc-09"` / `"branch:doc-10"` (target-doc branch tag) | `ResolveBranchTag` |
| `SourceFactId` | `"osint:09"` / `"osint:10"` (normalized target doc) | `ResolveSourceFactId` |
| `ReviewState` | `Provisional` | constant |
| `ValueTier` | `InterpretedValue` | constant |

Target-doc normalization is forgiving: `"9"` and `"09"` both map to `"09"`; anything blank
defaults to `"10"` (treat as speculative). `ToSensorBindings(records)` returns the bindings in
deterministic `(PlatformId, SensorId)` ordinal order regardless of input order
(`OsintCatalogMapperTests.ToSensorBindings_orders_deterministically_by_platform_then_sensor`).

### Path B — `OsintDigestRunner.MapProposalsToBindings` (digest-file path)

`OsintDigestRunner.RunFromDigestFile` uses its **own** internal mapper, not
`OsintCatalogMapper`. The differences are real and easy to miss:

| Staged field | Digest-runner value | vs. `OsintCatalogMapper` |
|--------------|---------------------|--------------------------|
| `SensorId` | from `ResolvePlatformSensorIds` — splits `CanonicalId` on the first `/` into `(platform, sensor)`; no `/` → both equal the id | different scheme |
| `BasePd` | hard-coded `0.5` | not relevance-derived |
| `TrlLevel` | raw `ProposedTrl` — **not clamped** | unclamped |
| `ImportBatchId` | not set (no branch tag) | **no doc routing** |
| `SourceFactId` | `"osint:{TargetDoc}"` (raw, un-normalized) | not normalized |

So a digest discovery with `canonicalId: "u-hypersonic/radar-glide"` stages as platform
`u-hypersonic`, sensor `radar-glide`, `basePd 0.5`, raw TRL — **without** a `branch:doc-*` tag.
If you need TL routing on staged rows, map through `OsintCatalogMapper` explicitly and propose
those bindings yourself (as the S22-07 test does), rather than relying on `RunFromDigestFile`.

## TRL gate interaction (why low-TRL proposals never promote)

Staging is not the same as promotion. A binding that clears the confidence gate can still be
**quarantined** by the catalog import gate if its `TrlLevel` is below the minimum
(`CatalogImportGate.PartitionForImport` → `RejectionReason = "trl_below_minimum"`,
`OsintCatalogMapperTests.Low_trl_binding_quarantined_by_import_gate_never_promotes`). A
high-confidence but speculative (low-TRL) discovery is therefore staged for the record but
cannot be approved into the live catalog until its TRL is raised. This is intentional layered
defense: confidence gates *entry*, TRL gates *promotion*.

## Operational runbook (headless CLI / MCP)

All commands run through `ProjectAegis.MissionEditor.Cli` and emit camelCase JSON. The MCP
verbs in `tools/mission-editor/mcp-tools.json` delegate to these same commands.

### On-demand search (no DB write)

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- osint_search
# → { "ok": true, "proposals": [ … ], "logOnlyCount": N }
```

`osint_search` (MCP `osint_search` / `osint_digest`) reads the committed fixture
`data/osint_facts.json` via `FileOsintConnector`, runs the proposal gate at `0.65`, and
**returns** proposals + a log-only count. It does **not** stage or write anything. A `--db
<path>` that exists overrides the fixture; a missing fixture yields an empty, deterministic
result (the connector never throws).

### Stage a digest file into pending proposals

`OsintDigestRunner.RunFromDigestFile(databasePath, digestPath)` parses a digest
(`{ "discoveries": [ … ] }`), dedupes by `CanonicalId` (keeping the highest `RelevanceScore`,
then highest `SourceUrl`), partitions at `0.65`, and — only if there is at least one proposal —
seeds the Baltic patrol fixture when the DB is absent and stages a single sensor batch via
`ProposeSensorBatch(..., actorType: "agent", "osint-digest-runner", "osint_digest:<file>")`.
It returns `OsintDigestRunResult(ParsedTotal, ProposalCount, LogOnlyCount, BatchId?)`. `BatchId`
is `null` when nothing cleared the gate.

### List pending proposals and approve one

```bash
# List pending batches (MCP osint_list_staging_proposals)
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  osint_staging_review --db catalog.db
# → { "ok": true, "pending": [ { "batchId": "…", "recordCount": N, … } ] }

# Approve a batch — commits via the write gate (MCP osint_submit_review_decision)
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  osint_staging_review --db catalog.db --approve <batchId>
# → { "ok": true, "batchId": "…", "errors": [] }
```

`OsintStagingReviewCommand` is a thin proxy over the write gate: with no `--approve` it calls
`ListPendingBatches()`; with `--approve` it calls `ApproveBatch(batchId, "human",
"osint-ui-reviewer")` — the same all-or-nothing commit a Unity staging UI would invoke. See the
[catalog write gate runbook](catalog-write-gate.md) for approve/quarantine semantics.

## Determinism & safety

- **Deterministic ordering everywhere.** Connectors, the proposal gate, and both mappers sort
  ordinally by `(SourceUrl, CanonicalId)` or `(PlatformId, SensorId)`, so the same input always
  stages the same batch.
- **Connectors never throw.** Parse failures and missing files return empty arrays, not
  exceptions — a bad feed degrades to "no proposals", never a crash.
- **No real-time social stream in MVP.** `OsintDigestRunner.EnableRealtimeSocialStream = false`
  (DSA-1.3) — do not stand up a 24/7 listener.
- **Pure projection.** `OsintCatalogMapper` exposes no `IWriteGate` types in any public
  signature (`Mapper_exposes_only_pure_binding_projection_without_write_gate_types`); mapping
  and committing stay separate concerns (ADR-006).
- **Staging ≠ commit.** Every path stops at a *pending* batch. Commit is always a separate,
  explicit approve (PLE-3.1 / DSA-2.1).

## Constraints & gotchas

- **Pick the mapper deliberately.** The digest-file path (B) does **not** apply TRL clamping or
  `branch:doc-*` routing; only `OsintCatalogMapper` (A) does. Mixing them silently produces
  inconsistent staged metadata.
- **Dedupe keeps the *highest-relevance* duplicate**, not the newest — two records with the same
  `CanonicalId` collapse to the one with the larger `RelevanceScore`
  (`osint-digest-fixture.json` exercises this).
- **A blank `targetDoc` is treated as speculative (`"10"`)** in path A — set it explicitly for
  near-future (`"09"`) discoveries or they route to the speculative branch.
- **Confidence gates entry, TRL gates promotion** — a staged proposal can still be quarantined
  on approve if its TRL is below the import minimum.

## Verify

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "Osint" -v minimal
```

Covers `OsintCatalogMapperTests` (TL routing, TRL clamp, deterministic order, import-gate
quarantine, write-gate round-trip preserving routing) and `OsintDigestRunnerTests` (digest
parse, dedupe, gate partition, staged batch result).

## See also

- [Catalog write gate runbook](catalog-write-gate.md) — propose / approve / quarantine, the
  destination for OSINT-staged batches
- [Balance telemetry drift](balance-telemetry-drift.md) — the other advisory-only feed into the
  same human review loop
- [Platform editor Excel round-trip](platform-editor-excel-roundtrip.md) — the manual authoring
  surface that also stages through the write gate
- [ADR-006 — Data layer boundary](../architecture/adr-006-data-layer-boundary.md) — why mapping
  is a pure projection and commits never bypass review
- Source — `src/ProjectAegis.Data/Osint/` (+ `Connectors/`), CLI verbs in
  `src/ProjectAegis.MissionEditor.Cli/Program.cs` and
  `OsintStagingReviewCommand.cs`; tests in `src/ProjectAegis.Data.Tests/Osint/`
