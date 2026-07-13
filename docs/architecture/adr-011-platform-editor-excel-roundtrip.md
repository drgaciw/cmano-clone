# ADR-011: Platform Editor Excel Roundtrip (Phase A)

**Status:** Accepted

**Date:** 2026-06-14

**Decision Makers:** Data + architecture per Sprint 22 kickoff and GitNexus impacts.

## Summary

Phase A of the platform catalog Excel roundtrip (Req 21) uses ClosedXML for export/import in PlatformWorkbookExporter/Importer. All modifications are staged exclusively through the write-gate (new ProposePlatformBatch + reuse of ProposeMount/Loadout/Magazine/CommsBatch → ApproveBatch). No auto-commit to live tables. Supports platforms + mounts/loadouts/magazines/comms. Migration 007 is extended additively with staging tables. CLI verbs (platform_export/import/diff_xlsx) added for headless/MCP use. Phase B is deferred (full diff provenance, UI integration, additional entity types).

## Context

- Req 21 Platform Editor requires user-friendly editing of complex catalog data (platforms, weapons, fittings) beyond sensor-focused markdown.
- Prior state: 007 added live platform_* / weapon / mount tables; PlatformWorkbookImporter treated "Platforms" as UnsupportedSheets; write-gate was sensor + 4 mount types only; no Excel roundtrip.
- Safety/determinism requirements (DBI-1.x, 7.x, 8.3): changes must go through propose/approve (quarantine, snapshot, provenance), never direct DB writes or bypass. GitNexus flagged CatalogWriteGate as CRITICAL (extend-only only).
- Existing pattern (sensor import, CmoMarkdownImportProposer, write-gate staging) must be mirrored for platforms.
- ClosedXML chosen for Excel I/O (no other dependencies in Data layer).

## Decision

- **Exporter (PlatformWorkbookExporter)**: Uses ClosedXML to write sheets for platforms + related entities (mounts, loadouts, magazines, comms, weapons) from catalog data. Deterministic ordering (PlatformId + composite keys, Ordinal).

- **Importer (PlatformWorkbookImporter)**: Reads xlsx via ClosedXML, produces Catalog* rows (PlatformEntry, Mount, etc.), stages via the gate. Extends to treat platforms as supported.

- **Write-gate staging pattern (core)**:
  - Additive only: `string ProposePlatformBatch(IReadOnlyList<CatalogPlatformEntry> proposed, string actorType, string actorId, string rationale = "")` on IWriteGate + impl in CatalogWriteGate.
  - New private `InsertStagingPlatform` (mirrors InsertStagingMount exactly).
  - Extended `DeleteStagingRows` to cover *all* staging_* tables (sensor + mount + loadout + magazine + comms + platform) on reject — enforces DBI-1.4 no-orphan guard.
  - No signature/behavior changes to existing ProposeSensorBatch/Propose* or Approve/Reject/List paths.
  - Empty batch guard (DBI-7.1), stable `OrderBy(p => p.PlatformId, StringComparer.Ordinal)` + composite for determinism.

- **ClosedXML adapter boundary**: Contained in the Platform/ workbook classes (exporter/importer). Adapter handles row mapping, validation, sheet naming. No ClosedXML leakage into gate or catalog layer.

- **Migration (007_platform_editor_phase_a.sql)**: Additive `CREATE TABLE IF NOT EXISTS catalog_staging_platform (...)` (and the mount/loadout/magazine/comms staging for completeness/consistency). Idempotent (CREATE IF NOT EXISTS), FK to batch, deterministic columns. Complements the live tables added earlier in 007. No app-logic table creation in C#.

- **CLI/MCP roundtrip (S22-02)**: New verbs `platform_export_xlsx` / `platform_import_xlsx` / `platform_diff_xlsx` in ProjectAegis.MissionEditor.Cli (exact pattern of CatalogImportMarkdownCommand). Update tools/mission-editor/mcp-tools.json + McpToolsManifestTests. Propose-only (no auto-commit); returns batch ids for approve via existing gate flow.

- **Tests & determinism**: Additive only. Use Baltic fixtures (CatalogValidationDefaults + in-memory markdown/xlsx equivalents), FixedCatalogClock, temp DB + cleanup, [Collection("CatalogSqlite")], explicit stable OrderBy. Cross tests for mixed types, orphan guard (staging row counts match batch, 0 after reject), roundtrip, no sensor regression. CLI manifest + execution tests.

- **Phase B scope (deferred)**: Full workbook diff (export vs current), richer provenance/trl on all rows, UI panel integration (beyond CLI), additional sheets if needed, conflict resolution UX. This ADR locks Phase A boundaries.

The ADR is referenced in Game-Requirements/requirements/21-Platform-Editor.md and the Sprint 22 kickoff. Implementation follows GitNexus (extend-only on CRITICAL WriteGate), data/engine/test rules, and Graphite (gt create only).

## Consequences

- **Positive**: User-friendly Excel editing for complex data while preserving headless determinism, write safety, and provenance. Unblocks S22-04 (platform support in markdown importer too) and future UI. CLI/MCP verbs enable automation without direct DB access.

- **Risks & mitigations**: CatalogWriteGate CRITICAL blast radius (18 impacted, 7 processes per impact analysis) — strictly extend-only (new overloads only; no existing Propose*/Approve changes). DBI-1.4 orphan risk mitigated by extended Delete + explicit tests + migration tables. No bypass of gate (DBI-8.3). Windows long-ref issues noted for some temp branch names (use short -m).

- **Trade-offs**: Additive migration + staging tables increase schema surface temporarily (Phase B will clean). ClosedXML is the boundary — future format changes isolated there.

- **Review gates**: Full build + targeted Data tests (PlatformWorkbook|Importer|WriteGate|Catalog) + CLI smoke + gitnexus detect_changes before each gt create + submit. Evidence in commits, test output, this ADR.

- **Next (Phase B / S23)**: Workbook diff command, richer provenance, UI binding, any additional entity types.

See also: Sprint 22 kickoff (S22-01/02/03 ACs + risks + Quality Gates), qa-plan-sprint-22-2026-06-09.md (22-1/2/3 cases), 007 migration, IWriteGate/CatalogWriteGate, PlatformWorkbook* classes, MissionEditor.Cli verbs, mcp-tools.json.

All per AGENTS.md (impacts before edits, detect before commit), Graphite workflow (gt only), and .claude/rules.
