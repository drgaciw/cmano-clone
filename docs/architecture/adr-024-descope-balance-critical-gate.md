# ADR-024: De-scoping Balance-Critical Approval Gate in Catalog Write Gate

## Status

**Proposed** (2026-09-02)

## Date

2026-09-02

## Context

Audit finding **A-02** identified that requirements documents (`Game-Requirements/requirements/21-Platform-Editor.md` item PLE-3.3 and `06-Database-Intelligence.md` item DBI-2.4) asserted the existence of a `balanceCritical` / `BalanceCritical` approval gate as completed (`[x]`).

Code search (`rg -i 'balance.?critical' src/`) yields zero occurrences across the codebase. `CatalogWriteGate` stages proposals and requires `ApproveBatch` for every batch uniformly; it does not implement record-count auto-approval or a `balanceCritical` metadata flag.

Separately, `PlatformWorkbookImporter.Plan` computes an advisory `RequiresHumanApproval` flag when `changes.Count > HumanApprovalRecordThreshold` (10 changed workbook cells, not distinct catalog records). That hint surfaces in import staging UI/CLI output but does not bypass or replace `CatalogWriteGate` propose/approve semantics.

## Decision

1. **De-scope** the fine-grained `balanceCritical` field requirement from the catalog schema and write gate for the current release.
2. Retain `CatalogWriteGate`'s uniform propose/`ApproveBatch` workflow and the existing advisory `PlatformWorkbookImporter.RequiresHumanApproval` change-count hint (>10 cells) without conflating either with a per-record `balanceCritical` gate.
3. Update requirement trackers and specification documents (PLE-3.3 and DBI-2.4) to untick the completed state and mark the capability as **GAP / Backlog** pending dedicated balance governance work.

## Consequences

### Positive
- Prevents unnecessary schema migration and catalog table churn for unused flags.
- Requirements documentation honestly reflects the operational mechanics of `CatalogWriteGate`.

### Negative
- Curators and balance teams cannot mark individual critical units to force manual approval independent of batch size; governance relies on the general proposal review process.
