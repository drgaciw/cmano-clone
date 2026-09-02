# ADR-026: De-scoping Nationality-Based Cooperative Engagement Capability (CEC) Gate

## Status

**Proposed** (2026-09-02)

## Date

2026-09-02

## Context

Audit finding **A-04** identified that `Game-Requirements/requirements/22-Drone-Swarm-Platforms.md` (SWARM-31 §1) specified a gate stating *"US/NATO catalog platforms expose CEC"* and marked the capability as landed in the document footer.

Code audit confirms that `CecCapable` is implemented as a boolean attribute on `CatalogSwarmPlatform` and in `Sim/Cec/*` (`CecMeshController`, `CecMeshEvaluator`, `CecRemoteEngageGate`). However, there is no nationality, alliance, or affiliation restriction in `ProjectAegis.Sim.Cec`. Any platform marked `CecCapable = true` can participate in same-side CEC meshes regardless of catalog country or allegiance.

## Decision

1. **De-scope** the nationality/alliance CEC validation gate from `Sim` and `Data` catalog layers.
2. Maintain `CecCapable` as an explicit capability flag configured per platform in the catalog/workbook (e.g. `PlatformWorkbookImporter`/`Exporter`) rather than inferring or restricting it based on country of origin metadata.
3. Downgrade SWARM-31 §1 requirement status to **Partial** in doc 22 to accurately reflect that CEC functionality is fully operational via explicit capability configuration, but without automatic nationality-based gating.

## Consequences

### Positive
- Sim logic remains simple, orthogonal, and side-based without coupling to national alliance taxonomy.
- Scenario and catalog authors retain full flexibility to assign CEC capabilities to any platform or faction.

### Negative
- Automated catalog ingestion does not automatically gate CEC assignment by national affiliation; catalog curation must ensure non-NATO/unsupported factions do not have `CecCapable` set if historical fidelity is required.
