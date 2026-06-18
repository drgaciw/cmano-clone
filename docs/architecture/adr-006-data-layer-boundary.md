# ADR-006: ProjectAegis.Data Layer Boundary

**Status:** Accepted (P0 stack DATA-0; retrofit 2026-06-02)
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

## Summary

`ProjectAegis.Data` is the authoritative catalog and scenario-package layer: immutable snapshots, gated writes, and read-only DTO export into Sim and Delegation. Simulation rules stay in `ProjectAegis.Sim`; presentation never opens SQLite.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS (6000.3.14f1) + .NET 8 headless |
| **Domain** | Core / Data |
| **Knowledge Risk** | LOW — Data assembly has no UnityEngine dependency |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`, `docs/engine-reference/dotnet/README.md` |
| **Post-Cutoff APIs Used** | None in Data layer |
| **Verification Required** | Deterministic catalog iteration order; injectable clock on snapshot commit |

`ProjectAegis.Data` targets **net8.0** for headless CI; **netstandard2.1** build exists only so `ProjectAegis.Delegation.UnityAdapter` can reference catalog readers without pulling Unity into Sim.

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-001 (Sim vs Delegation boundary — Data sits below Sim) |
| **Enables** | `platform-db-basepd-slice` epic; mission-editor `dbSnapshotId` binding; DATA-3 policy repository move |
| **Blocks** | Full `IWriteGate` / `DbSnapshotStore` implementation until Accepted (now cleared); editor export gate that mutates catalog |
| **Ordering Note** | Accept before ADR for mission-editor Validation Engine; combat-domain ADR does not depend on Data writes for MVP engage |

## GDD Requirements Addressed

| GDD / Req | Requirement | How this ADR addresses it |
|-----------|-------------|---------------------------|
| Req 06 — Database Intelligence | Auditable catalog, agent-safe writes | `IWriteGate`, provenance columns, no direct table mutation |
| Req 11 — Mission / scenario | Scenario ↔ DB version binding | `dbSnapshotId` on packages; fail load on resolve errors |
| `sensor-detection-ew.md` | Catalog-backed `basePd` | `ICatalogReader` sorted reads; scenario override optional |
| `logistics-magazines.md` | Platform DB magazine chains | Catalog DTO export; engage consumes via Sim |
| `agentic-mission-editor.md` | Canonical file + validation | Editor stages through write gate; sim reads snapshots only |

## Key Interfaces

| Interface | Responsibility |
|-----------|----------------|
| `ICatalogReader` | Read-only, deterministic-order catalog queries (`basePd`, bindings, platforms) |
| `IWriteGate` | Staging → validate → commit → optional new snapshot (P0 stack — not all paths implemented; see [catalog write gate runbook](../engineering/catalog-write-gate.md)) |
| `CatalogImportGate` / importers | Approved import path for seed and bulk JSON (implemented) |
| DTO export | Plain records consumed by `ProjectAegis.Sim` and `ProjectAegis.Delegation` — no ECS types in Data |

**Temporary exception (migration):** `ScenarioPolicyRepository` remains in `ProjectAegis.Sim` until DATA-3; new code must not add further policy persistence in Sim.

## Migration Plan

| Phase | Scope | Status |
|-------|--------|--------|
| DATA-0 | Assembly scaffold + `ICatalogReader` | Done on `main` |
| DATA-2 | Catalog `basePd`, SQLite reader, import gates | In progress / `platform-db-basepd-slice` |
| DATA-3 | `ScenarioDataPaths` in Data; repository JSON load uses Data paths | Partial (2026-06-02) |
| DATA-4 | `IWriteGate`, `DbSnapshotStore`, scenario package bind | Planned |
| DATA-5 | CI import smoke + provenance columns at scale | Planned |

Run `gitnexus impact` before moving `ScenarioPolicyRepository` or changing public `WeaponEnvelope` / catalog DTO shapes.

## Validation Criteria

- No `ProjectAegis.Sim` or `ProjectAegis.Delegation` code opens SQLite directly.
- Catalog reads use stable sort keys; replay harness fingerprint unchanged when scenario omits catalog override.
- Snapshot commit path uses injectable clock (no wall-clock in deterministic tests).
- `dotnet test` green for Data + Sim + Delegation after each DATA phase merge.

## Alternatives Considered

### Alternative 1: Policy and catalog stay in Sim
- **Rejection:** Blurs ADR-001; blocks mission editor and agent write workflows per req 06.

### Alternative 2: Separate ADR-008 for snapshots/write gate
- **Rejection:** Duplicates this decision; snapshots and write gate are phases of DATA-0, not a competing boundary.

## Related Decisions

- ADR-001 — assembly boundary
- ADR-005 — DOTS world state (orthogonal; catalog crosses as DTOs only)
- ADR-007 — C2 projections read sim/delegation, not Data directly

**Last verified:** 2026-06-02
