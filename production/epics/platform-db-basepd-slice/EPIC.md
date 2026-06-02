# Epic: Platform DB basePd Slice (DATA-2)

> **Status:** Ready  
> **Priority:** P0 — blocks authentic sensor trials  
> **Created:** 2026-06-02  
> **Depends on:** `ProjectAegis.Data` scaffold on `main` (DATA-1 complete)  
> **GDD:** Platform Database (req 06), [sensor-detection-ew.md](../../design/gdd/sensor-detection-ew.md)

## Goal

Replace scenario-only `detection[]` trials with **catalog-backed `basePd`** lookups: sorted SQLite reads, provenance columns, deterministic iteration order.

## Acceptance

1. `ICatalogReader` returns platform/sensor rows with `basePd` for Baltic harness units.
2. `DeterministicDetectionLoop` reads `basePd` from catalog when scenario omits per-trial override.
3. `dotnet test` green; `gitnexus impact` run before public API changes.
4. Replay fingerprint stable with catalog-bound detection (golden harness).

## Stories

Run `/create-stories platform-db-basepd-slice` after ADR-006 review.