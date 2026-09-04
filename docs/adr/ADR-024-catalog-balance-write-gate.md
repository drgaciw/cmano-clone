# ADR-024: Catalog Balance Write Gate Alignment with Extend-Only Threshold

- **Status:** Accepted
- **Date:** 2026-09-04
- **Deciders:** Conductor Requirements Steward, Architecture Review Board, Data Architecture Stewards
- **Technical Story:** AEGIS-311 / DRG-241 (Audit Finding A-02, PLE-3.3, DBI-2.4)

## Context and Problem Statement

Audit finding **A-02** identified that requirements documents (`Game-Requirements/requirements/21-Platform-Editor.md` item PLE-3.3 and `06-Database-Intelligence.md` item DBI-2.4) asserted the existence of a fine-grained `balanceCritical` / `BalanceCritical` approval gate as completed (`[x]`).

However, code inspection across the entire repository (`rg -i 'balance.?critical' src/`) yields zero occurrences. The actual write gate implementation, `CatalogWriteGate` in `src/ProjectAegis.Data/WriteGate/CatalogWriteGate.cs`, operates as an extend-only SQLite staging ledger. It requires `Propose*` followed by `ApproveBatch` uniformly for all catalog changes; it does not implement per-record approval bypasses or a `balanceCritical` column.

Separately, `PlatformWorkbookImporter.Plan` (`src/ProjectAegis.Data/Platform/PlatformWorkbookImporter.cs`) evaluates an advisory flag:
```csharp
var requiresApproval = changes.Count > HumanApprovalRecordThreshold; // HumanApprovalRecordThreshold = 10
```
This change-count threshold applies to total modified cells across supported sheets, surfacing an advisory warning in the UI/CLI staging report, but it does not alter or replace `CatalogWriteGate` propose/approve transaction semantics.

How should Project Aegis reconcile the unbuilt `balanceCritical` gate specification with the shipped extend-only batch architecture?

## Decision Drivers

* **Specification Accuracy**: Requirements trackers and design documents must faithfully document what the data pipeline actually enforces.
* **Database Schema Stability**: Avoid introducing unneeded columns, table migrations, and schema churn into SQLite platform catalogs during release hardening.
* **Extend-Only Auditability**: Maintain uniform batch provenance, actor tracking, and staging immutability without fragile per-record auto-approval bypasses.
* **Workflow Predictability**: Ensure catalog curators have unambiguous rules for when staging batches require review.

## Considered Options

* **Option 1**: Implement a new `BalanceCritical` boolean column across catalog schema tables, add per-record filtering logic to `CatalogWriteGate`, and auto-approve non-critical records while gating critical ones.
* **Option 2**: De-scope the fine-grained `balanceCritical` gate from current requirements, align all documentation with the shipped uniform `CatalogWriteGate` propose/approve lifecycle and the `changes.Count > 10` advisory threshold, and track per-record balance governance as GAP / Backlog.

## Decision Outcome

Chosen option: **Option 2**, because it aligns requirements documentation with the shipped data pipeline architecture, avoids disruptive SQLite schema migrations, and preserves the robust extend-only batch staging model.

### Decision Details

1. **De-scope Fine-Grained `balanceCritical` Gate**:
   - Formally de-scope the `balanceCritical` per-record schema attribute and gate from the v1.0 data architecture.
   - Designate PLE-3.3 and DBI-2.4 balance-critical requirements as **GAP / Backlog** in requirement tracking sheets.
   - Postpone fine-grained platform criticality tagging to future dedicated balance governance tooling.

2. **Ratify Extend-Only Batch Governance Model**:
   - Ratify `CatalogWriteGate`'s existing architecture where every modification batch must be staged through `Propose*` and signed off via `ApproveBatch`.
   - Ratify `PlatformWorkbookImporter.HumanApprovalRecordThreshold` (`10` cells) as an advisory staging check (`RequiresHumanApproval`) intended to alert operators to large bulk imports.

3. **Requirements Synchronization**:
   - Update `Game-Requirements/requirements/21-Platform-Editor.md` (PLE-3.3) and `06-Database-Intelligence.md` (DBI-2.4) to remove assertions of a completed `balanceCritical` gate, documenting instead the shipped extend-only propose/approve lifecycle and the >10 change advisory threshold.

### Positive Consequences

* Resolves audit finding A-02 cleanly, establishing truth between documentation and code.
* Avoids database migration risks and breaking changes to existing SQLite catalog snapshots.
* Preserves transactional integrity: every catalog modification remains auditable via `CatalogWriteGate` batch headers and staging tables.

### Negative Consequences

* Balance curators cannot flag individual sensitive platforms or sensors to selectively force manual review independent of batch size; governance relies on uniform batch approval.

## Pros and Cons of the Options

### Option 1: Implement `BalanceCritical` Schema Column and Logic

* Good, because it would fulfill the original text of PLE-3.3 and DBI-2.4 literally.
* Bad, because it creates high blast radius across SQLite tables, importer/exporter mappings, DTOs, and test suites.
* Bad, because auto-approving "non-critical" rows undermines the guarantees of the extend-only write gate.

### Option 2: Align Documentation with Shipped Extend-Only Threshold (Chosen)

* Good, because it documents the actual shipped and tested software behavior.
* Good, because it requires zero disruptive schema changes to production catalogs.
* Good, because uniform batch approval is simpler, safer, and less error-prone.
* Bad, because fine-grained balance-specific gating must be deferred to future roadmap phases.

## Implementation Notes

* **IMP-024.1**: `CatalogWriteGate` retains its invariant: no row enters live catalog tables without an explicit `ApproveBatch` call with non-empty actor metadata.
* **IMP-024.2**: `PlatformWorkbookImporter.HumanApprovalRecordThreshold` remains pinned at 10 rows/cells, verified by `PlatformWorkbookImporterTests.cs` and `PlatformWorkbookPeIntegrationHardeningTests.cs`.
* **IMP-024.3**: Future balance telemetry or sensitivity tags will be designed under a unified balance governance ADR rather than ad-hoc catalog column additions.

## References

* **REF-024.1**: `Game-Requirements/reviews/audit-findings-2026-09-02.md` (Finding A-02)
* **REF-024.2**: `Game-Requirements/requirements/21-Platform-Editor.md` (PLE-3.3)
* **REF-024.3**: `Game-Requirements/requirements/06-Database-Intelligence.md` (DBI-2.4)
* **REF-024.4**: `src/ProjectAegis.Data/WriteGate/CatalogWriteGate.cs`
* **REF-024.5**: `src/ProjectAegis.Data/Platform/PlatformWorkbookImporter.cs`
* **REF-024.6**: `docs/engineering/platform-workbook-roundtrip.md`
