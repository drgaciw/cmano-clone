# Story 001 — Catalog basePd for detection loop

> **Epic:** platform-db-basepd-slice  
> **Status:** Complete  
> **Last Updated:** 2026-06-08  
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

- `src/ProjectAegis.Data.Tests/Catalog/SqliteCatalogReaderTests.cs`
- `src/ProjectAegis.Data.Tests/Catalog/InMemoryCatalogReaderTests.cs`
- `src/ProjectAegis.Sim.Tests/Scenario/DetectionTrialResolverTests.cs`
- `src/ProjectAegis.Sim.Tests/Sensors/DeterministicDetectionLoopTests.cs`
- `src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/BalticReplayHarnessCatalogTests.cs`
- `src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/BalticReplayHarnessScenarioGoldenTests.cs`
- `assets/data/catalog/migrations/001_sensor_base_pd.sql`

## Completion Notes

**Completed:** 2026-06-08  
**Criteria:** 4/4 passing  
**Test Evidence:** Paths above; `dotnet test` filters green (Data 3, Sim 7, UnityAdapter 3 for catalog harness)  
**Code Review:** Full mode — LP-CODE-REVIEW APPROVED (ADR-006: `ICatalogReader` in Data, Sim consumes read-only via `DetectionTrialResolver`, deterministic `ORDER BY platform_id, sensor_id`, no Sim SQLite access)  
**QL-TEST-COVERAGE:** ADEQUATE — AC1→`001_sensor_base_pd.sql`+`SqliteCatalogReaderTests`; AC2→`InMemoryCatalogReaderTests`; AC3→`DetectionTrialResolverTests`+`BalticReplayHarnessCatalogTests`+`BalticReplayHarnessScenarioGoldenTests` (ContactChange indirect via detection hash/fingerprint); AC4→sort-order tests in Data+Sim