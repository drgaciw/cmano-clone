# Story 005 — Catalog P2-2 bulk chunked import + quarantine report

**Epic:** platform-db-basepd-slice  
**Sprint:** 19  
**Priority:** Must Have  
**Type:** Integration  
**Status:** Complete
**TR-ID:** req-06 §DBI-3  
**ADR:** ADR-006 (Accepted)  
**Plan:** `docs/superpowers/plans/2026-06-04-catalog-phase2-import.md` (P2-2 slice)

## Goal

Complete Catalog Phase 2 bulk import policy: verify 500-sensor chunking at scale, emit a structured quarantine report from `catalog_import_markdown`, and keep all writes on the write-gate path.

## Acceptance Criteria

1. [x] `catalog_import_markdown` JSON output includes a `quarantineReport` array when `quarantinedCount > 0` (each entry: `platformId`, `sensorId`, `reason`, `sourceFile`).
2. [x] Optional `--report-out <path.json>` writes the full import summary (counts, batches, quarantineReport) for CI artifacts.
3. [x] `CmoMarkdownBulkImportTests` proves chunk boundary: curated fixture with **≥501** approved bindings produces **2** propose batches when `--chunk-size 500`.
4. [x] Quarantine rows land in `sensor_quarantine` via existing `WriteQuarantineRows` path (no direct INSERT).
5. [x] `dotnet test ProjectAegis.sln -v minimal` green; `gitnexus impact CatalogWriteGate` reviewed before merge (HIGH, 7 upstream — proposer/CLI only, no gate signature changes).

## Files to Create / Modify

| File | Change |
|------|--------|
| `src/ProjectAegis.Data/Import/CmoMarkdownImportProposer.cs` | Return quarantine detail for report |
| `src/ProjectAegis.MissionEditor.Cli/CatalogImportMarkdownCommand.cs` | `quarantineReport` + `--report-out` |
| `src/ProjectAegis.MissionEditor.Cli/Program.cs` | Parse `--report-out` |
| `src/ProjectAegis.Data.Tests/Import/CmoMarkdownBulkImportTests.cs` | Chunk boundary + quarantine report tests |
| `src/ProjectAegis.MissionEditor.Cli.Tests/CatalogImportMarkdownCommandTests.cs` | CLI JSON shape |

## Dependencies

- P2-1 on `main`: `CmoMarkdownImportProposer`, `catalog_import_markdown` CLI
- `CatalogImportGate.PartitionForImport`, `CatalogWriteGate.ProposeSensorBatch`

## GitNexus

Impact `CatalogWriteGate` (**HIGH**) upstream before edits. No `DelegationBridge` changes.

## QA Test Cases

- Unit: `ChunkBindings` with 501 items → 2 chunks (500 + 1)
- Integration: CLI run on test fixture → `batchCount == 2`, `quarantineReport` non-empty when fixture has low-confidence rows
- Regression: existing `CmoMarkdown` / `CatalogImportMarkdownCommand` tests remain green

## Test Evidence

- `src/ProjectAegis.Data.Tests/Import/CmoMarkdownBulkImportTests.cs` — 3 tests (501 chunk boundary, quarantine report + `sensor_quarantine`)
- `src/ProjectAegis.MissionEditor.Cli.Tests/CatalogImportMarkdownCommandTests.cs` — 3 tests (JSON shape, `quarantineReport`, `--report-out`)
- Full suite: `dotnet test ProjectAegis.sln -v minimal` — PASS (2026-06-08)
- GitNexus: `npx gitnexus impact CatalogWriteGate -d upstream` — HIGH, no `CatalogWriteGate` edits