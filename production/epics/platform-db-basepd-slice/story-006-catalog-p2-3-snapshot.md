# Story 006 — Catalog P2-3 snapshot binding

**Epic:** platform-db-basepd-slice  
**Sprint:** 19  
**Priority:** Must Have  
**Type:** Integration  
**Status:** Complete  
**TR-ID:** req-06 §DBI-3  
**ADR:** ADR-006 (Accepted)  
**Plan:** `docs/superpowers/plans/2026-06-04-catalog-phase2-import.md` (P2-3 slice)

## Goal

After catalog write-gate approve, record deterministic snapshot content hash and release-train metadata; `ScenarioPackage` binds explicit `dbSnapshotId`.

## Acceptance Criteria

1. [x] `catalog_write_approve` records `contentHashSha256` + `releaseVersion` + `snapshotId` in JSON output.
2. [x] `DbSnapshotStore.RecordRelease` writes `catalog_snapshot.content_hash_sha256` and `db_release` row.
3. [x] `CatalogSnapshotHasher` stable across identical approve cycles (two DBs, same fixture).
4. [x] `ScenarioPackage.FromDocument` honors explicit `metadata.dbSnapshotId`.
5. [x] `dotnet test ProjectAegis.sln -v minimal` green; replay golden unchanged.

## Test Evidence

- `src/ProjectAegis.Data.Tests/Snapshots/DbSnapshotBindingTests.cs`
- `src/ProjectAegis.Data.Tests/Scenario/ScenarioPackageTests.cs` (explicit snapshot id)