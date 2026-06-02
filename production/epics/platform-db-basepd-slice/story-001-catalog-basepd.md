# Story 001 — Catalog basePd for detection loop

> **Epic:** platform-db-basepd-slice  
> **Status:** In progress (DATA-2 on `stack/data/basepd`)  
> **TR-ID:** TR-sensor-002 (MVP)  
> **ADR:** ADR-006

## Summary

Extend `ICatalogReader` with sorted platform/sensor queries returning `basePd`; `DeterministicDetectionLoop` uses catalog when scenario trial omits override.

## Acceptance criteria

1. SQLite schema migration adds `sensor.base_pd` (or equivalent) with provenance columns per ADR-006.
2. `NullCatalogReader` replaced or augmented with in-memory fixture for Baltic harness tests.
3. Harness scenario without `detection[]` still produces deterministic ContactChange rows using catalog `basePd`.
4. `dotnet test` — all green; new tests assert sort order (e.g. by `platform_id` ordinal).

## Test evidence

`src/ProjectAegis.Data.Tests/` + `src/ProjectAegis.Sim.Tests/Sensors/`