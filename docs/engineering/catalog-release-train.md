# Catalog release train — snapshots, unified manifests & diffs

Once a curator has approved catalog changes through the write gate, the data layer needs a way
to **name a shippable drop, pin it to an immutable snapshot, and diff two drops** so a reviewer
can see exactly what changed between releases. That machinery lives in
[`src/ProjectAegis.Data/Snapshots/`](../../src/ProjectAegis.Data/Snapshots/) and is surfaced by
the `catalog_release_diff` CLI verb. This doc is the operational reference for it.

> **Scope.** This is the *release-train* layer that sits **on top of** the write gate: releases,
> unified curator-drop manifests, TL-tier branch resolution, and the read-only diff. The
> propose → approve → commit internals (and the snapshot *binding* that happens at approve time)
> are documented in [`catalog-write-gate.md`](catalog-write-gate.md); how a scenario *resolves*
> a snapshot at load time is in the [Data README](../../src/ProjectAegis.Data/README.md#snapshots--scenario-binding).
> Everything here is **extend-only** and read-only for diffs — see
> [`production/release-train-scope-boundary-2026-06-24.md`](../../production/release-train-scope-boundary-2026-06-24.md).

---

## Why this exists

`ProjectAegis.Sim` and `ProjectAegis.Delegation` never open SQLite; they read an `ICatalogReader`
bound to an **immutable snapshot** (ADR-006). A snapshot is content-addressed, so "which catalog
did this replay run against?" has a byte-exact answer. The release train adds the *human* layer:

- A **release** (`db_release` row) gives a snapshot a stable, human-named `releaseVersion`.
- Per-domain nightly corpus drops (sensor / weapon / platform / aircraft / submarine / facility)
  are consolidated into one **unified curator-drop manifest** so a single `releaseVersion` can
  describe a multi-domain catalog.
- A deterministic **diff** between two `releaseVersion` values tells a reviewer which domains were
  added, changed, or removed — without mutating anything.

---

## Data model

Two SQLite tables back the release train (created by the req-06 migrations; columns below are the
ones the store reads/writes):

| Table | Key | Columns used | Written by |
|-------|-----|--------------|------------|
| `catalog_snapshot` | `snapshot_id` | `content_hash_sha256`, `branch` (TL tier) | `DbSnapshotStore.RecordApprovedImport` / `RecordRelease` |
| `db_release` | `release_version` | `snapshot_id`, `schema_version`, `created_utc_ticks`, `notes` | `DbSnapshotStore.RecordRelease` |

Two things live in `db_release.notes` rather than dedicated columns (extend-only — no schema
churn):

- **Per-domain content hash**, as a `contentHash=<sha256>;…` segment (see
  `CatalogSnapshotBinder`, which writes `batch=<id>;contentHash=<hash>`).
- **Unified manifests**, as a JSON blob prefixed with `unified-manifest:` (see
  `UnifiedReleaseTrainManifest.ToNotesJson` / `TryParseFromNotes`).

Missing tables degrade gracefully: `GetSortedSnapshotIds` returns the Baltic default
(`baltic_patrol`) and `GetSortedReleases` returns an empty list rather than throwing.

---

## Type map (`Snapshots/`)

| Type | Role |
|------|------|
| [`DbSnapshotStore`](../../src/ProjectAegis.Data/Snapshots/DbSnapshotStore.cs) | `IDisposable` read/write facade over the two tables — record releases, resolve `releaseVersion → snapshotId`, list sorted releases, resolve a snapshot for a TL branch. |
| [`CatalogSnapshotHasher`](../../src/ProjectAegis.Data/Snapshots/CatalogSnapshotHasher.cs) | Deterministic SHA-256 over **sorted sensor bindings** — the snapshot content hash. |
| [`CatalogSnapshotBinder`](../../src/ProjectAegis.Data/Snapshots/CatalogSnapshotBinder.cs) | `BindAfterApprove` — recompute the content hash and record a snapshot + release row after a write-gate approve. |
| [`UnifiedReleaseTrainManifest`](../../src/ProjectAegis.Data/Snapshots/UnifiedReleaseTrainManifest.cs) | Consolidate N per-domain nightly `releaseVersion` rows into one manifest (`Consolidate`), serialize to/from `notes` JSON, compute the deterministic manifest hash. `UnifiedReleaseTrainDomainDrop` is one row of it. |
| [`UnifiedReleaseTrainDiffComparer`](../../src/ProjectAegis.Data/Snapshots/UnifiedReleaseTrainDiffReport.cs) | `Compare(store, from, to)` → `UnifiedReleaseTrainDiffReport` of `Added` / `Changed` / `Removed` rows. |
| [`CatalogReleaseTrainDomains`](../../src/ProjectAegis.Data/Snapshots/CatalogReleaseTrainDomains.cs) | The six canonical nightly domains and `nightly-<domain>-…` release-version parsing. |
| [`CatalogReleaseTrainResolver`](../../src/ProjectAegis.Data/Snapshots/CatalogReleaseTrainResolver.cs) | Deterministic `tlBranch → snapshotId + dbRef` selection from branch-matched candidates. |
| [`CatalogExportManifest`](../../src/ProjectAegis.Data/Snapshots/CatalogExportManifest.cs) | Metadata-only export descriptor `{ dbVersion, tlTier, schemaVersion, contentHash, exportSchemaVersion }` — no runtime scenario binding. |

---

## Lifecycle: from approve to diff

```
write-gate approve (a batch is committed)
   │
   ▼
CatalogSnapshotBinder.BindAfterApprove(db, batchId, clock, [snapshotId] [releaseVersion] [tlTier])
   │   contentHash = CatalogSnapshotHasher over sorted sensor bindings
   │   defaults: snapshotId="baltic_patrol", releaseVersion="catalog-approve-<batchId>", tlTier="TL-0"
   ▼
DbSnapshotStore.RecordRelease(...)  ── upsert catalog_snapshot + db_release, notes="batch=…;contentHash=…"
   │
   ├─ (optional) DbSnapshotStore.RecordUnifiedRelease(unifiedVersion, snapshotId, tlTier, [nightly-*-…], clock)
   │      consolidates per-domain nightly drops → one unified-manifest: JSON note
   ▼
catalog_release_diff --from A --to B  ── UnifiedReleaseTrainDiffComparer.Compare(store, A, B)
       deterministic Added / Changed / Removed rows (read-only)
```

`BindAfterApprove` is what the `catalog_write_approve` CLI verb calls; you rarely invoke the
binder directly. `RecordUnifiedRelease` is the nightly-corpus consolidation step (S32-02) — it
validates that every listed `nightly-<domain>-…` release exists in `db_release` **and** pins the
same `snapshotId`, then throws if a version is unknown, unparseable, or points at a different
snapshot.

---

## The diff (`catalog_release_diff`)

Read-only, deterministic, no `CatalogWriteGate` mutation. It compares either two **unified
manifests** or two **per-domain nightly drops** (falling back to a single synthetic domain when a
`releaseVersion` isn't a unified manifest).

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  catalog_release_diff --db <catalog.db> --from <releaseVersion> --to <releaseVersion>
```

Prefer the named `--from` / `--to` flags. A positional fallback exists
(`catalog_release_diff --db <db> <from> <to>`) but is error-prone: the `--db` value must be
excluded from the positional scan or the DB path gets misread as `<fromReleaseVersion>`.

Output is indented camelCase JSON:

```json
{
  "ok": true,
  "verb": "catalog_release_diff",
  "databasePath": "assets/data/catalog/baltic_patrol.db",
  "fromReleaseVersion": "unified-2026-06-24",
  "toReleaseVersion": "unified-2026-07-01",
  "isEmpty": false,
  "diffCount": 1,
  "canonicalLines": [
    "Changed\tsensor\tnightly-sensor-baltic-v2-0624\tnightly-sensor-baltic-v2-0701\t…"
  ],
  "rows": [
    {
      "kind": "Changed",
      "domain": "sensor",
      "fromReleaseVersion": "nightly-sensor-baltic-v2-0624",
      "toReleaseVersion": "nightly-sensor-baltic-v2-0701",
      "fromSnapshotId": "snap-…",
      "toSnapshotId": "snap-…",
      "fromContentHashSha256": "…",
      "toContentHashSha256": "…"
    }
  ]
}
```

Diff semantics:

- **`Added`** — a domain present only in `to`. **`Removed`** — present only in `from`.
  **`Changed`** — present in both but not semantically equal.
- **Semantic equality compares `snapshotId` + `contentHashSha256` only.** Timestamp- or
  release-version-only metadata changes do **not** produce a `Changed` row.
- Identical `from` and `to` short-circuit to an empty report (`isEmpty: true`, exit `0`).
- Rows are emitted in a stable order (`Kind`, then `Domain`, then release versions, all ordinal);
  `canonicalLines` is the tab-separated, sorted form intended for golden fixtures.

---

## Domains & release-version naming

`RecordUnifiedRelease` and the per-domain diff fallback only recognize the six canonical nightly
domains ([`CatalogReleaseTrainDomains`](../../src/ProjectAegis.Data/Snapshots/CatalogReleaseTrainDomains.cs)):

`sensor`, `weapon`, `platform`, `aircraft`, `submarine`, `facility`.

A per-domain release version must be shaped `nightly-<known-domain>-<corpus-variant>-…`
(e.g. `nightly-sensor-baltic-v2-0701`). `TryParseFromReleaseVersion` extracts `<domain>` from the
first `-`-delimited token after the `nightly-` prefix; anything else (no prefix, unknown domain)
is treated as a single synthetic domain keyed on the whole `releaseVersion`.

---

## TL-tier branch resolution

Exports and release trains slice a snapshot to a **maximum disclosure tier**
(`CatalogTlTier.Tl0`…`Tl5`, default `TL-0`). `DbSnapshotStore.TryResolveSnapshotForBranch`
normalizes the requested branch, gathers `catalog_snapshot` rows with a matching `branch`, and
hands them to `CatalogReleaseTrainResolver.TryResolveFromCandidates`, which:

1. Prefers the snapshot referenced by the **latest matching `db_release` row**
   (`release_version ASC`, take the last match); otherwise
2. Falls back to the **first candidate by `snapshot_id ASC`**.

When the resolved snapshot has a unified manifest, `dbRef` is set to that manifest's
`releaseVersion`. The Baltic default snapshot (`baltic_patrol`) maps to itself as `dbRef`.

---

## Determinism & hashing

Two independent SHA-256 hashes, both order-stable (never rely on insertion order):

- **Snapshot content hash** (`CatalogSnapshotHasher`) — over sensor bindings sorted by
  `PlatformId` then `SensorId`, tab/newline delimited, with floats formatted round-trip
  (`"R"`, `InvariantCulture`). A given sensor set always hashes the same. *Note the content hash
  fingerprints sensor bindings only.*
- **Manifest hash** (`UnifiedReleaseTrainManifest.ComputeManifestHash`) — over domain drops sorted
  by `Domain` then `ReleaseVersion`, joining `domain\treleaseVersion\tsnapshotId\tcontentHash`.

Both feed the replay-binding invariant: reproducible input ⇒ byte-identical snapshot/manifest, so
scenarios pin exactly the catalog they ran against.

---

## Constraints & pitfalls

- **Extend-only.** Add new overloads / manifest fields additively; never alter existing write
  paths or the diff comparison rules (release-train scope boundary + write-gate invariant).
- **Diff is read-only.** `catalog_release_diff` opens the store but never mutates — safe to run in
  CI or against a committed DB.
- **`--db` positional trap.** Always pass `--from` / `--to` explicitly (see above).
- **Unified consolidation is strict.** Every listed domain release must exist in `db_release` and
  share the unified `snapshotId`, or `Consolidate` throws — this is intentional fail-fast, not a
  silent skip.
- **Notes carry structure.** Don't hand-edit `db_release.notes`; the `contentHash=…` segment and
  the `unified-manifest:` JSON are parsed by the store.

---

## See also

| Topic | Where |
|-------|-------|
| Write-gate propose/approve + snapshot binding at approve time | [`catalog-write-gate.md`](catalog-write-gate.md) |
| Scenario-load snapshot resolution (`ScenarioPackage`, ADR-008) | [Data README](../../src/ProjectAegis.Data/README.md#snapshots--scenario-binding) |
| Headless catalog seeding & reader resolution | [`catalog-seeding.md`](catalog-seeding.md) |
| CMO markdown import → nightly corpus drops | [`cmo-markdown-import.md`](cmo-markdown-import.md) |
| CLI verb reference (`catalog_release_diff`, `catalog_write_approve`) | [`mission-editor-cli.md`](mission-editor-cli.md) |
| Data-layer boundary (readers never open SQLite) | [ADR-006](../architecture/adr-006-data-layer-boundary.md) |
| Tests | [`DbSnapshotStoreTests`](../../src/ProjectAegis.Data.Tests/Snapshots/DbSnapshotStoreTests.cs) · [`UnifiedReleaseTrainManifestTests`](../../src/ProjectAegis.Data.Tests/Snapshots/UnifiedReleaseTrainManifestTests.cs) · [`UnifiedReleaseTrainDiffReportTests`](../../src/ProjectAegis.Data.Tests/Snapshots/UnifiedReleaseTrainDiffReportTests.cs) · [`CatalogExportManifestTests`](../../src/ProjectAegis.Data.Tests/Snapshots/CatalogExportManifestTests.cs) · [`DbSnapshotBindingTests`](../../src/ProjectAegis.Data.Tests/Snapshots/DbSnapshotBindingTests.cs) · [`TlReleaseTrainValidationTests`](../../src/ProjectAegis.Data.Tests/Validation/TlReleaseTrainValidationTests.cs) |
