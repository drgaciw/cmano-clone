# Catalog write gate — propose → approve → commit

Operational reference for [`CatalogWriteGate`](../../src/ProjectAegis.Data/WriteGate/CatalogWriteGate.cs),
the single audited path by which any catalog data (sensors, platforms, weapons, mounts,
loadouts, magazines, comms, links, mobility, signatures, EMCON, damage) reaches the live SQLite
tables that `ProjectAegis.Sim` and `ProjectAegis.Delegation` read. Nothing is written straight
to the live catalog — every mutation is *staged*, *validated*, and *approved* under an
append-only audit change log.

> **Architecture rule (ADR-006, req-06).** Catalog mutations *only* flow through `IWriteGate`
> (propose → validate → approve → commit). The interface is **extend-only**: you add new
> `Propose*Batch` overloads, never alter or delete an existing propose/approve path. This is a
> hard invariant — see [`AGENTS.md`](../../AGENTS.md#hard-invariants--never-break-these). Sim
> and Delegation never open SQLite directly; they take `ICatalogReader` + DTO exports (read
> path documented in [`ProjectAegis.Data/README.md`](../../src/ProjectAegis.Data/README.md)).

For the surrounding data-layer map (readers, snapshots, scenario binding, validation agents)
start with the [Data layer README](../../src/ProjectAegis.Data/README.md); this page is the
deep-dive on the write gate itself.

---

## Where the gate sits

Every producer of catalog change funnels through the same `Propose* → Approve` pair:

```text
 CMO markdown import  ─┐
 platform workbook     │
   (.xlsx / canonical) ├─► IWriteGate.Propose*Batch ──► staging tables (per-kind)
 OSINT digest proposal │        (stable-sorted, batch id, "proposed")
 database-intel agents │
 MissionEditor CLI    ─┘
                                   │  ListPendingBatches() / review
                                   ▼
                        IWriteGate.ApproveBatch  ──► validate → UPSERT live rows
                                   │                 → append audit change log
                                   │                 → record stable snapshot (sensors)
                                   ▼
                        ICatalogReader (Sim / Delegation, read-only)
```

`CatalogWriteGate` is SQLite-backed and constructed against a database path; it calls
`EnsureSchema()` on construction, so a fresh dev/CI DB is self-bootstrapping. It takes an
injectable [`ICatalogClock`](../../src/ProjectAegis.Data/WriteGate/ICatalogClock.cs) (default
`FixedCatalogClock(0)`) so the commit path is deterministic — **never** `DateTime.UtcNow`.

```csharp
using var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(utcTicks: 0));
```

---

## The lifecycle

```csharp
// 1. PROPOSE — rows are stable-sorted, written to the matching staging table, and a batch id
//    is returned. State = "proposed". Throws ArgumentException if the list is empty.
string batchId = gate.ProposeSensorBatch(
    rows, actorType: "agent", actorId: "osint-digest", rationale: "S-band Pd revision");

// 2. REVIEW — list everything still awaiting a decision (deterministic order).
IReadOnlyList<CatalogStagingBatchSummary> pending = gate.ListPendingBatches();

// 3a. APPROVE — validates, UPSERTs live rows, appends an audit change-log entry, marks the
//     batch "approved". WriteGateDecision.Committed == false carries the blocking reasons.
WriteGateDecision decision = gate.ApproveBatch(batchId, actorType: "human", actorId: "tl-lead");
if (!decision.Committed)
{
    // decision.Errors, e.g. ["quarantine:u1/spn-43:trl_below_minimum"]
}

// 3b. REJECT — marks the batch "rejected" and DRAINS its staging rows (no orphans left behind).
gate.RejectBatch(batchId, actorType: "human", actorId: "tl-lead", rationale: "superseded");
```

### `WriteGateDecision` / `CatalogStagingBatchSummary`

```csharp
sealed record WriteGateDecision(bool Committed, string BatchId, IReadOnlyList<string> Errors);

sealed record CatalogStagingBatchSummary(
    string BatchId, string ActorType, string ActorId,
    int RecordCount, string ApprovalState, long ProposedUtcTicks);
```

`Committed` is the only success signal — a rejected/blocked approve returns `Committed = false`
with one or more machine-readable strings in `Errors` (catalog below). `RejectBatch` also
returns `Committed = false` by design (the batch was *not* committed); an empty `Errors` list
means the reject itself succeeded.

`ListPendingBatches()` returns only `approval_state = 'proposed'` batches, ordered by
`proposed_utc_ticks` then `batch_id` (deterministic).

---

## Propose batch kinds

`IWriteGate` currently exposes twelve extend-only propose overloads. Each stages exactly one
row kind, sorts input by the stable `CatalogSortKeyComparer`, and returns a batch id whose
prefix encodes the kind:

| Overload | Row DTO | Batch-id prefix | Origin |
|----------|---------|-----------------|--------|
| `ProposeSensorBatch` | `CatalogSensorBinding` | `batch-{n}-{ticks}` | req-06 P0 (sensor Pd) |
| `ProposeMountBatch` | `CatalogMount` | `batch-mount-…` | S22-01 |
| `ProposeLoadoutBatch` | `CatalogLoadout` | `batch-loadout-…` | S22-01 |
| `ProposeMagazineBatch` | `CatalogMagazineEntry` | `batch-magazine-…` | S22-01 |
| `ProposeCommsBatch` | `CatalogCommsBinding` | `batch-comms-…` | S22-01 |
| `ProposeLinkCatalogBatch` | `CatalogLinkEntry` | `batch-link-…` | S34-02 |
| `ProposePlatformBatch` | `CatalogPlatformBinding` | `batch-platform-…` | S22-04 |
| `ProposeWeaponBatch` | `CatalogWeaponRecord` | `batch-weapon-…` | S22-04 |
| `ProposeMobilityBatch` | `CatalogMobility` | `batch-mobility-…` | S24-03 (Phase B) |
| `ProposeSignatureBatch` | `CatalogSignature` | `batch-signature-…` | S24-03 (Phase B) |
| `ProposeEmconBatch` | `CatalogEmcon` | `batch-emcon-…` | S24-03 (Phase B) |
| `ProposePlatformDamageBatch` | `CatalogPlatformDamage` | `batch-damage-…` | S25-04 (Phase B) |

All overloads share the same signature tail — `(IReadOnlyList<T> proposed, string actorType,
string actorId, string rationale = "")` — and throw `ArgumentException` when `proposed` is
empty. `actorType` / `actorId` are free-form audit strings (convention: `"agent"` /
`"human"` for type; a stable identity such as `"osint-digest"` or `"tl-lead"` for id).

> **One batch = one row kind.** A batch id maps to a single staging table. `ApproveBatch`
> inspects the populated staging tables and rejects a batch that spans more than one kind with
> `ambiguous_staging_batch`. Never hand-craft a batch id or stage mixed kinds under one id.

---

## Approve-time validation (per kind)

`ApproveBatch(batchId, actorType, actorId)` loads the staged content, dispatches on the row
kind, and runs kind-specific guards **before** it UPSERTs. If any guard fails it returns a
non-committed decision and leaves the staging rows intact (so you can fix inputs and re-approve
a fresh batch).

| Row kind | Approve guard(s) | Blocking error(s) |
|----------|------------------|-------------------|
| Sensor | Import partition (`CatalogImportGate.PartitionForImport`): confidence / TRL / review-state floors | `quarantine:{platform}/{sensor}:{reason}` |
| Platform | Kill-chain commit gate (post-staging preview) | `kill_chain:{CODE}` |
| Weapon | Kill-chain commit gate | `kill_chain:{CODE}` |
| Mount | Platform must exist, then kill-chain gate | `orphan_platform:{id}`, `kill_chain:{CODE}` |
| Loadout | Platform must exist | `orphan_platform:{id}` |
| Mobility / Signature / EMCON / Damage | Platform must exist | `orphan_platform:{id}` |
| Magazine / Comms / Link | (row-shape validated at propose / upsert time) | — |

On success each row is UPSERTed and an audit row is appended to the change log (old → new value
for a representative field, tagged with `batchId`, `actorType`, `actorId`), the batch is marked
`approved`, and the catalog dependency graph is notified. For **sensor** batches the gate also
records a stable snapshot (`DbSnapshotStore.RecordApprovedImport`) so a scenario can pin the
exact catalog it ran against — this step is best-effort and never fails the commit.

---

## Error-code catalog

All strings appear in `WriteGateDecision.Errors`. They are stable and machine-readable — match
on the prefix before the first `:`.

| Code | Raised by | Meaning / fix |
|------|-----------|---------------|
| `staging_batch_not_found` | `ApproveBatch`, `RejectBatch` | Unknown/empty batch id, or the batch was already drained. Re-propose. |
| `ambiguous_staging_batch` | `ApproveBatch` | Batch touches more than one staging table. Stage each row kind under its own propose call. |
| `quarantine:{platform}/{sensor}:{reason}` | Sensor approve | A staged sensor row failed an import floor. `reason` ∈ `confidence_below_minimum`, `trl_below_minimum`, `review_state_{state}`. Raise the source quality or fix the row, then re-propose. |
| `orphan_platform:{platformId}` | Mount / Loadout / Mobility / Signature / EMCON / Damage approve | The referenced platform row does not exist yet. Approve (or include) a platform batch first — no orphan child rows. |
| `kill_chain:{CODE}` | Platform / Weapon / Mount approve | Committing would leave a kill-chain dependency error (`CODE` starts `KILL_CHAIN_`, severity `error`). Resolve the missing/broken edge before committing. |

`Propose*Batch` throws `ArgumentException` (not a decision) for an empty input list — this is a
programming error, not a review outcome.

---

## Provenance & TL tiers

Every persisted value carries a `CatalogProvenanceTier` (`source_fact`, `interpreted_value`,
`gameplay_abstraction` — the last is the default when unknown) so downstream tooling can
separate raw facts from tuning, and a `CatalogTlTier` (`Tl0`…`Tl5`, default `TL-0`) so exports
and release trains can slice a snapshot to a maximum disclosure tier. See the
[Data README provenance section](../../src/ProjectAegis.Data/README.md#provenance--tl-tiers)
for the full model.

---

## CLI surface (MCP verbs)

The headless [Mission Editor CLI](mission-editor-cli.md) exposes the gate as two MCP verbs
(both operate on a `--db` SQLite path; `catalog_write_propose` seeds a Baltic Patrol DB if the
file is missing):

| Verb | Args | Behavior |
|------|------|----------|
| `catalog_write_propose` | `--db`, `--platform`, `--sensor`, `--base-pd` | Stages a single-row sensor proposal (`actorType=agent`, `actorId=database-intelligence`); prints `{ ok, batchId, recordCount }`. |
| `catalog_write_approve` | `--db`, `--batch` `[--snapshot-id] [--release-version] [--enable-balance-drift]` | Approves as `human` / `reviewer-mcp`, then binds a snapshot (`CatalogSnapshotBinder.BindAfterApprove`); prints `{ ok, batchId, releaseVersion, snapshotId, contentHashSha256, … }`, or `{ ok:false, errors }` on a non-committed decision. |

Batch bulk paths use the same gate: `platform_import_xlsx` stages workbook edits via
`Propose*Batch` with **no auto-commit** (next step is `catalog_write_approve`), and
`osint_staging_review` lists / approves staged OSINT proposals. See
[`mission-editor-cli.md`](mission-editor-cli.md#catalog-read--extend-only-write-gate) for the
full verb table.

---

## Determinism & constraints

- **Deterministic clock.** Construct with a `FixedCatalogClock`; the batch id and audit
  timestamps derive from `ICatalogClock.UtcTicks`, never wall-clock time.
- **Stable sort at propose time.** Rows are ordered by `CatalogSortKeyComparer` before staging,
  so a given input set always produces byte-identical staging and snapshot content.
- **Golden-hash guards.** `ProjectAegis.Data.Tests` pins deterministic outputs
  (`CatalogSortKeyGoldenHashes`, `LinkCatalogGoldenHashes`, `KillChainGoldenHashes`, …).
  Regenerate them **intentionally**, never silently.
- **Snapshots are immutable.** An approved batch records a stable snapshot; scenarios pin a
  `dbSnapshotId` and fail load on resolve errors (see the Data README snapshot section).

---

## Extending the gate (extend-only runbook)

To add a new catalog row kind without breaking the invariant:

1. **Migration.** Add an ordered SQL file under
   [`assets/data/catalog/migrations/`](../../assets/data/catalog/migrations/) (e.g.
   `012_*.sql`) creating the live table **and** its `catalog_staging_*` counterpart. Bump
   `CatalogTlTier.CatalogSchemaVersion` so export manifests report the new schema version.
2. **DTO + sort key.** Add the row record to `Catalog/` and a `Sort*` method to
   `CatalogSortKeyComparer` for a stable order.
3. **Propose overload.** Add a new `Propose<Kind>Batch(...)` to `IWriteGate` and
   `CatalogWriteGate` following the existing pattern (empty-list guard, sorted insert, batch-id
   prefix). **Do not touch existing overloads.**
4. **Approve path.** Add the kind to `StagingBatchContent`, the dispatch chain in
   `ApproveBatch`, and an `Approve<Kind>Staging` helper with its guards (orphan / kill-chain as
   applicable), an UPSERT, and an audit change-log append.
5. **Tests + goldens.** Add propose/approve tests and, if the kind participates in a hashed
   output, regenerate the relevant golden hash intentionally.

---

## Build & test

```bash
dotnet build src/ProjectAegis.Data/ProjectAegis.Data.csproj
dotnet test  src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj -v minimal
```

`ProjectAegis.Data.Tests` (~406 tests) is part of the ≥1232-test solution baseline.

## See also

| Topic | Doc |
|-------|-----|
| Data-layer map (readers, snapshots, scenario binding) | [`src/ProjectAegis.Data/README.md`](../../src/ProjectAegis.Data/README.md) |
| Data-layer boundary decision | [`adr-006-data-layer-boundary.md`](../architecture/adr-006-data-layer-boundary.md) |
| Scenario ↔ DB binding / validation engine | [`adr-008-mission-editor-validation-engine.md`](../architecture/adr-008-mission-editor-validation-engine.md) |
| CLI verbs (propose/approve, workbook import, OSINT) | [`mission-editor-cli.md`](mission-editor-cli.md) |
| Production `.xlsx` (ClosedXML) adapter | [`src/ProjectAegis.Data.Excel/README.md`](../../src/ProjectAegis.Data.Excel/README.md) |
| Determinism rules (clock, ordering, hashing) | [`determinism-and-replay.md`](determinism-and-replay.md) |
| Hard invariants (extend-only write gate, snapshots) | [`AGENTS.md`](../../AGENTS.md#hard-invariants--never-break-these) |
