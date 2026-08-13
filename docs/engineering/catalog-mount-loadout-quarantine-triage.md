# Catalog mount/loadout quarantine triage & repair — developer guide

When the nightly catalog corpus proposes new **child rows** (mounts, loadouts, weapon fittings)
for ships, submarines, and facilities, some of those rows arrive with a broken or ambiguous
foreign key — an orphaned platform reference, a circular FK, a duplicate loadout key. Rather than
committing bad data or silently dropping it, the importer leaves those rows *quarantined* in the
staging tables. This guide covers the curator seam that inspects and (within a bounded envelope)
repairs them: `MountLoadoutQuarantineTriage`, its `MountLoadoutQuarantineRepairEnvelope`, the
read-only Platform Editor projection, and the `catalog_mount_loadout_quarantine_triage` CLI verb.

> **Scope & authority.** S32-03 / DBI-3.2 / PLE-2.3. The triage engine lives in
> [`ProjectAegis.Data/Catalog/`](../../src/ProjectAegis.Data/Catalog/) and only ever *writes*
> through [`CatalogWriteGate`](../../src/ProjectAegis.Data/WriteGate/CatalogWriteGate.cs) — it
> approves already-staged batches, it never mutates a table directly. It therefore inherits the
> extend-only write-gate contract ([catalog-write-gate.md](catalog-write-gate.md), ADR-006). This is
> a **different** quarantine from the sensor **import-floor** quarantine
> (`quarantine:{platform}/{sensor}:{reason}`) documented in the write-gate guide and the TRL/orphan
> quarantine surfaced by the [platform workbook](platform-workbook-roundtrip.md): those gate a row
> *into* staging; this one triages child rows *already* in staging.

| Concern | Type | File |
|---------|------|------|
| Nightly child-row domains (`platform` / `submarine` / `facility`) | `static` | [`MountLoadoutQuarantineDomain.cs`](../../src/ProjectAegis.Data/Catalog/MountLoadoutQuarantineDomain.cs) |
| Bounded FK repair rules + classification | `static` + `record` | [`MountLoadoutQuarantineRepairEnvelope.cs`](../../src/ProjectAegis.Data/Catalog/MountLoadoutQuarantineRepairEnvelope.cs) |
| Result / row / per-domain-count DTOs | `sealed record` | [`MountLoadoutQuarantineReport.cs`](../../src/ProjectAegis.Data/Catalog/MountLoadoutQuarantineReport.cs) |
| The triage engine (audit + repair-plan + apply) | `static` | [`MountLoadoutQuarantineTriage.cs`](../../src/ProjectAegis.Data/Catalog/MountLoadoutQuarantineTriage.cs) |
| Read-only editor surfacing | `static` + `record` | [`MountLoadoutQuarantineProjection.cs`](../../src/ProjectAegis.Delegation/Projection/MountLoadoutQuarantineProjection.cs) |
| The `catalog_mount_loadout_quarantine_triage` CLI verb | `static` | [`MountLoadoutQuarantineTriageCommand.cs`](../../src/ProjectAegis.MissionEditor.Cli/MountLoadoutQuarantineTriageCommand.cs) |

---

## Domains

Quarantine is bucketed into three nightly corpus **domains** so a curator can triage one entity
family at a time. `MountLoadoutQuarantineDomain` is the single source of truth:

| Domain | Covers | `platform.domain` values that map here |
|--------|--------|----------------------------------------|
| `platform` | Surface ships (the default) | `surface`, anything unknown |
| `submarine` | Subsurface platforms | `subsurface` |
| `facility` | Fixed land facilities | `land` |

Two mappers feed the domain:

- **`FromPlatformDomain(platformDomain)`** — used when the triage engine reads the actual
  `platform.domain` column for a quarantined row's parent (`subsurface → submarine`,
  `land → facility`, `surface` / anything else `→ platform`).
- **`FromEntityHint(entityHint)`** — used for the CLI `--entity` filter; it is forgiving about
  plurals and synonyms (`ship`/`platforms → platform`, `submarines → submarine`,
  `facilities → facility`), falling back to `platform` for anything it does not recognize.

`IsKnown` and the `ChildRowDomains` list (`[platform, submarine, facility]`) drive the audit's
per-domain fan-out.

---

## The repair envelope

`MountLoadoutQuarantineRepairEnvelope` is deliberately small — the doc comment reads *"No scope
creep beyond these documented rules."* It answers exactly one question: **can this child row's
platform FK be repaired, and if so under which rule?**

There are three (and only three) repair rules:

| Rule constant | When it applies |
|---------------|-----------------|
| `platform_live_fk` | The referenced platform already exists in the **live** `platform` table. |
| `platform_staging_fk` | The platform exists only in a **proposed staging** batch (which must be approved *first*). |
| `baltic_seed_fk` | The platform is one of the deterministic Baltic seed platforms (`CatalogValidationDefaults.BalticPlatforms()`). |

`ClassifyPlatformFk(platformId, livePlatformExists, stagingPlatformExists, balticSeedExists)`
returns a `MountLoadoutRepairClassification(Repairable, Rule, Reason)` by checking those in order.
An empty `platformId`, or a platform found in none of the three sources, is **not repairable** and
carries the `orphan_platform` reason.

Rows that fall outside the envelope keep a machine-readable **reason** instead of a rule:

| Reason constant | Meaning |
|-----------------|---------|
| `orphan_platform` | Platform FK resolves to nothing repairable. |
| `circular_fk` | The child id equals its own platform id. |
| `duplicate_loadout_key` | The same `(platform_id, loadout_id)` appears with more than one distinct `loadout_name`. |
| `out_of_envelope` | Default — a quarantined row the envelope does not know how to fix. |

Anything with a non-null `RepairRule` counts as **repairable**; everything else is
**out-of-envelope** and stays quarantined for a human.

---

## What the triage engine does

`MountLoadoutQuarantineTriage.Run(databasePath, dryRun = true, entityHint?, proposeJsonPath?,
clock?)` is the entry point. It always **audits**, and only **repairs** when `dryRun` is false.

### 1. Audit (always)

`Audit` reads every *proposed* child row from `catalog_staging_mount` and
`catalog_staging_loadout` (joined to `catalog_staging_batch WHERE approval_state = 'proposed'`),
ordinal-sorted by `(platform_id, child_id, batch_id)`. For each row it:

1. Resolves the parent **domain** — the live `platform` table first (lowest `snapshot_id`), then
   `catalog_staging_platform` (lowest `batch_id`), defaulting to `platform`.
2. Classifies the row: `duplicate_loadout_key` → `circular_fk` → otherwise `ClassifyPlatformFk`
   (which yields either a repair rule or an `orphan_platform` / `out_of_envelope` reason).

It folds these into one `MountLoadoutDomainQuarantineCounts` per domain:

```
(Domain, MountQuarantined, LoadoutQuarantined, FittingQuarantined, Repairable, OutOfEnvelope)
```

**Fitting** counts are not in the DB — they come from an optional `--propose-json` file
(`quarantinedCount` + `entity`), surfaced as a synthetic `fitting` row with reason
`orphan_weapon_id` and no repair rule (fittings are report-only here).

### 2. Repair (only when `--apply`)

When `dryRun` is false, `BuildRepairPlan` walks the same rows and gathers two ordinal-sorted batch
sets: the **staging platform batches** that must land first (only for `platform_staging_fk` rows,
via `FindStagingPlatformBatches`) and the **child batches** that carry the repairable mount/loadout
rows. It then opens **one** `CatalogWriteGate` (with the injected `clock`, or a
`FixedCatalogClock(32031)` fallback) and calls:

```csharp
gate.ApproveBatch(batchId, "human", "mount-loadout-quarantine-triage");
```

for the platform batches **then** the child batches. A batch that the gate refuses (e.g. the write
gate's own orphan-platform FK check, DBI-3.2) is skipped and recorded in `AdvisoryNotes` as
`platform_batch_failed:{batch}:{errors}` or `child_batch_failed:…`; the run does **not** throw.

The engine re-audits afterward. `Result.Ok` (apply mode) is true when the remaining quarantine is
empty **or** the total mount+loadout quarantined count strictly decreased — progress, not
perfection, so a partially-repairable corpus still reports success.

The full return shape is `MountLoadoutQuarantineTriageResult`:

```
(Ok, DryRun, DatabasePath, Before[], After[], RemainingQuarantine[], RepairedBatchIds[], AdvisoryNotes[])
```

In dry-run, `After == Before`, `RepairedBatchIds` is empty, and the advisory notes just restate the
envelope.

---

## Read-only surfacing (Platform Editor)

`MountLoadoutQuarantineProjection` (S40-03, in `ProjectAegis.Delegation/Projection/`) is the pure,
allocation-light presentation seam the Platform Editor binds to. It never touches the DB — it takes
the DTOs the triage engine already produced and formats them:

- `FormatDomainSummary` → `DOMAIN {domain} mount=… loadout=… fitting=… repairable=… out_of_envelope=…`
- `FormatQuarantineRow` → `QUARANTINE {domain}/{childKind} platform=… child=… batch=… reason=… repair={rule|none}`
- `BindFromAudit(audit, dryRun)` and `BindFromTriage(result)` roll those into a
  `MountLoadoutQuarantinePanelState` (status line + domain/detail lines + `TotalQuarantined`
  = mount + loadout + fitting, plus `Repairable` / `OutOfEnvelope` / `DryRun`).

Because it is a read-model with deterministic ordinal sorting, it stays on the safe side of the
projection/no-mutation contract described in [c2-projection-layer.md](c2-projection-layer.md).

---

## CLI verb

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  catalog_mount_loadout_quarantine_triage \
  --db <catalog.db> \
  [--entity platform|submarine|facility] \
  [--propose-json <path>] \
  [--apply]
```

- **Dry-run is the default** — you only mutate the catalog with `--apply`.
- `--entity` narrows both the audit and the repair plan to a single domain.
- `--propose-json` supplies the fitting-quarantine count for reporting.
- `MountLoadoutQuarantineTriageCommand.Run` serializes a camelCase JSON payload — `ok`, `dryRun`,
  `databasePath`, `repairEnvelope` (the three rule names), `before` / `after` per-domain counts,
  `remainingQuarantine` rows, `repairedBatchIds`, and `advisoryNotes` — and returns exit code `0`
  iff `result.Ok` (else `1`, including on a caught exception which is reported as
  `{ ok: false, error }`).

The operational verb table lives in [mission-editor-cli.md](mission-editor-cli.md); this page
documents *what the verb computes*.

---

## Determinism & safety

- **Write-gate-only writes.** Every mutation goes through `CatalogWriteGate.ApproveBatch`; the
  triage never issues its own `UPDATE`/`INSERT`. It cannot widen the write path (ADR-006,
  extend-only).
- **Bounded by design.** The repair envelope is three FK rules; anything else stays quarantined for
  a human. There is no heuristic “best guess” repair.
- **Deterministic ordering.** Rows and batches are `Ordinal`-sorted, so a given DB state produces a
  byte-identical report and the same batch-approval order every run.
- **Off the sim fingerprint.** This is a catalog-authoring tool and a read-model; it does not run in
  the sim tick and does not touch the Baltic v2 replay hash `17144800277401907079`.

---

## Tests

| Test | Covers |
|------|--------|
| `MountLoadoutQuarantineTriageTests.Audit_identifies_pending_mount_loadout_child_rows_by_domain` | Per-domain audit fan-out. |
| `…Apply_repairs_in_envelope_rows_via_WriteGate_only` | Repairs go through `CatalogWriteGate`. |
| `…Orphan_mount_rows_remain_quarantined_out_of_envelope` | Non-repairable rows are left alone. |
| `…ApproveBatch_rejects_orphan_platform_mount_and_loadout_DBI_3_2` | The write gate's orphan-FK refusal is respected. |
| `…Curated_slice_fixture_triage_reports_zero_pending_child_rows_after_full_approve` (`[Theory]`) | End-to-end per-domain clean slice. |
| `…Repair_envelope_documents_bounded_rules_only` | The envelope stays the three documented rules. |
| `MountLoadoutQuarantineProjectionTests` (4 `[Test]`) | Domain-summary / row formatting and panel bind. |

`MountLoadoutQuarantineTriageTests` is xUnit under `ProjectAegis.Data.Tests/Catalog/`;
`MountLoadoutQuarantineProjectionTests` is NUnit under `ProjectAegis.Delegation.Tests/Projection/`.

---

## Extending it

- **New repairable FK source?** Add a rule constant + branch to
  `MountLoadoutQuarantineRepairEnvelope.ClassifyPlatformFk`, extend `RepairRules`, and thread the
  new existence check through `ReadPendingChildRows`. Keep it *bounded* — the whole point of the
  envelope is that unknown cases stay quarantined.
- **New quarantine reason?** Add a reason constant and detect it in `ReadPendingChildRows`; it will
  flow through as out-of-envelope automatically.
- **Never** bypass `CatalogWriteGate` to “just fix the row.” The extend-only write path is the
  invariant that keeps the catalog auditable.

## See also

- [catalog-write-gate.md](catalog-write-gate.md) — the propose → approve → commit write path this
  triage drives, and the *import-floor* quarantine it is distinct from.
- [catalog-integrity-reports.md](catalog-integrity-reports.md) — the read-only inspection verbs
  (dependency graph, kill-chain, entity map) that answer “is the catalog consistent?”.
- [platform-workbook-roundtrip.md](platform-workbook-roundtrip.md) — the workbook TRL/orphan-FK
  quarantine on the *import* side.
- [mission-editor-cli.md](mission-editor-cli.md) — the operational CLI/MCP verb reference.
