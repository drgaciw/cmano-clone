# ADR-006: ProjectAegis.Data Layer Boundary

**Status:** Proposed (P0 stack DATA-0)  
**Date:** 2026-05-30  
**Deciders:** Database Intelligence P0 design  
**Related:** ADR-001 (sim boundary), req 06, req 11

## Context

`ProjectAegis.Data` is planned in the master architecture but not yet implemented. Requirements doc 06 demands auditable, agent-safe catalog writes; doc 11 requires scenario ↔ database version binding. Scenario policy loading currently lives in `ProjectAegis.Sim`, which blurs the data/sim boundary.

## Decision

1. Introduce **`ProjectAegis.Data`** as a .NET 8 class library with **no Unity dependency**.
2. All **catalog mutations** go through **`IWriteGate`** (staging → validate → commit → optional new snapshot).
3. **Snapshots are immutable**; scenarios reference `dbSnapshotId` and fail load on resolve errors.
4. **Sim and Delegation** consume read-only APIs (`ICatalogReader`, DTO export); they do not open SQLite directly.
5. Move **scenario policy repository** from Sim to Data in the P0 stack (DATA-3).
6. P0 validation is **schema + referential + sanity detect**; auto-normalization and balance drift are out of scope.

## Consequences

### Positive

- Clear seam for mission editor and speculative-systems staging (doc 05).
- Headless tests for catalog integrity without Unity.
- Aligns with ADR-001 layering.

### Negative

- Extra project reference and migration work when moving `ScenarioPolicyRepository`.
- SQLite dev DB requires seed/import discipline in CI.

## Compliance

- Run `gitnexus impact` before moving `ScenarioPolicyRepository` or changing `WeaponEnvelope` consumers.
- No `DateTime.UtcNow` in snapshot commit path without injectable clock (determinism).
