# Catalog Phase 2 — Bulk CMO import (S18-04)

**Date:** 2026-06-04  
**Predecessor:** DATA-5 `CmoMarkdownImporter` smoke on `main`  
**Branch:** `stack/sprint18/catalog-phase2-plan`  
**Requirements:** doc 06 DBI, `Game-Requirements/Data-Population-CMAODB.md`

## Goal

Import **representative CMO sensor markdown** at scale through the **write gate** and bind approved drops to **DB snapshots** for scenario replay — without bypassing provenance or human approval.

## Stack topology (proposed)

```
main
 └── stack/data/phase2-cli          (P2-1)  catalog_import_markdown CLI
      └── stack/data/phase2-bulk    (P2-2)  chunked sensor.md import + quarantine report
           └── stack/data/phase2-bind (P2-3)  snapshot hash + scenario package pin
```

## Slice P2-1 — Import CLI

| Field | Value |
|-------|-------|
| **Entry** | `ProjectAegis.MissionEditor.Cli` subcommand `catalog_import_markdown` |
| **Input** | Path to `docs/reference/cmano-db/sensor.md` or curated slice directory |
| **Flow** | `CmoMarkdownImporter.ReadSensorBindings` → `CatalogImportGate.PartitionForImport` → `CatalogWriteGate.ProposeSensorBatch` |
| **Output** | Batch id + counts (approved staged / quarantined) |
| **Approve** | Reuse `catalog_write_approve` (existing) |

**GitNexus:** impact `CatalogWriteGate` (**HIGH**) — call only, no signature changes.

## Slice P2-2 — Bulk import policy

| Rule | Implementation |
|------|----------------|
| Chunk size | Default **500** sensors per propose batch (human approval per batch) |
| Determinism | Stable sort before chunking; `maxRecords` for smoke/CI |
| Quarantine | Rows failing TRL/confidence → `sensor_quarantine` via existing path |
| Quarantine report | CLI JSON `quarantineReport[]` (`platformId`, `sensorId`, `reason`, `sourceFile`); optional `--report-out <path.json>` for CI artifacts (S19-02 / story-005) |
| Provenance | `source_file`, `import_batch_id`, `citation_ref` from markdown sub-path |
| No direct INSERT | Importer never calls `CatalogJsonImporter.WriteSqlite` in production path |

## Slice P2-3 — Snapshot binding

| Field | Value |
|-------|-------|
| **After approve** | `DbSnapshotStore` records `ReleaseVersion` + content hash |
| **Scenario** | `ScenarioPackage` carries `dbSnapshotId` (DATA-3 seam) |
| **Replay** | Harness loads snapshot id from policy package; `/replay-verify` unchanged |

Aligns with `database-branching-release-train` skill — TL branching still **deferred**.

## Acceptance (Phase 2 complete)

- [x] CLI proposes ≥100 sensors from reference markdown without Node toolchain (mini fixture + `--max-records`; full `sensor.md` via same command)
- [x] P2-2 bulk chunk boundary (501 → 2 batches) + `quarantineReport` JSON + `--report-out` (story-005, 2026-06-08)
- [x] Human approve commits; `ICatalogReader.GetSortedSensorBindings()` reflects rows in stable order (P2-2 via gate + reader tests)
- [x] Snapshot manifest hash stable across two imports of same approved batch (P2-3 `RecordApprovedImport` + `CatalogSnapshotHasher` tests, 2026-06-08)
- [x] `dotnet test ProjectAegis.sln` green; replay golden unchanged unless catalog affects engage fixtures (403/403 pass; replays 17/17; PlayMode 7/7 @ smoke-2026-06-08)

## Deferred

- Full 7208-record sensor.md load in CI (use curated slice + nightly job)
- Weapon/platform markdown importers (ship/sensor only for P2)
- TL-0–TL-5 branch databases (doc 06 post-P0)

## Verification

```powershell
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter CmoMarkdown
npx gitnexus detect_changes --repo cmano-clone
```

**Completed in Sprint 18 closeout (superpowers plan 2026-06-05):** P2-2/P2-3 implemented + tests + gate auto-record snapshot on approve. See docs/superpowers/plans/2026-06-05-sprint-18-closeout.md Task 3.