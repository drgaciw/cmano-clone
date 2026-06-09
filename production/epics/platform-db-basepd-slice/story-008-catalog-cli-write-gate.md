# Story 008 — Catalog CLI write gate (Sprint 16)

**Epic:** platform-db-basepd-slice  
**Sprint:** 16  
**Type:** Integration  
**Status:** Complete  
**Last Updated:** 2026-06-08  
**TR-ID:** TR-editor-001 (partial), req-06 DBI MCP tools  
**ADR:** ADR-006

## Acceptance Criteria

1. `catalog_write_propose` returns `{ ok, batchId, recordCount }` JSON.
2. `catalog_write_approve` commits batch and binds snapshot metadata.
3. `catalog_entity_map` lists deterministic entity→table mapping (sorted by entity name).
4. `catalog_intelligence_run` runs `DatabaseIntelligenceOrchestrator` headless.
5. All four covered by `ProjectAegis.MissionEditor.Cli.Tests` + existing `CatalogWriteGateTests`.

## Test Evidence

- `src/ProjectAegis.MissionEditor.Cli.Tests/CatalogWriteCommandTests.cs`
- `src/ProjectAegis.MissionEditor.Cli.Tests/CatalogEntityMapCommandTests.cs`
- `src/ProjectAegis.MissionEditor.Cli.Tests/CatalogIntelligenceRunCommandTests.cs`
- `src/ProjectAegis.Data.Tests/WriteGate/CatalogWriteGateTests.cs`

## Completion Notes

**Completed:** 2026-06-08  
**Criteria:** 5/5 passing  
**Test Evidence:** Integration — CLI tests above + `CatalogWriteGateTests`  
**Code Review:** Full mode — LP-CODE-REVIEW APPROVED WITH NOTES (extend-only `CatalogEntityMapCommand` sort)  
**QL-TEST-COVERAGE:** ADEQUATE — each AC mapped to named test method