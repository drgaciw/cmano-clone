# ADR-024: De-scoping Balance-Critical Approval Gate in Catalog Write Gate

## Status

**Proposed** (2026-09-02)

## Date

2026-09-02

## Context

Audit finding **A-02** identified that requirements documents (`Game-Requirements/requirements/21-Platform-Editor.md` item PLE-3.3 and `06-Database-Intelligence.md` item DBI-2.4) asserted the existence of a `balanceCritical` / `BalanceCritical` approval gate as completed (`[x]`).

Code search (`rg -i 'balance.?critical' src/`) yields zero occurrences across the codebase. `CatalogWriteGate` currently implements bulk operation review based on record thresholds (e.g., >10 records requiring approval), but does not inspect or enforce a platform/sensor/weapon `balanceCritical` column or metadata attribute during ingestion or proposals.

## Decision

1. **De-scope** the fine-grained `balanceCritical` field requirement from the catalog schema and write gate for the current release.
2. Keep `CatalogWriteGate`'s existing size-based threshold mechanism (>10 records) without adding a new database column or schema migration.
3. Update requirement trackers and specification documents (PLE-3.3 and DBI-2.4) to untick the completed state and mark the capability as **GAP / Backlog** pending dedicated balance governance work.

## Consequences

### Positive
- Prevents unnecessary schema migration and catalog table churn for unused flags.
- Requirements documentation honestly reflects the operational mechanics of `CatalogWriteGate`.

### Negative
- Curators and balance teams cannot mark individual critical units to force manual approval independent of batch size; governance relies on the general proposal review process.
