# ADR-026: De-scoping Runtime Nationality-Based CEC Validation (Catalog Gate Retained)

## Status

**Proposed** (2026-09-02)

## Date

2026-09-02

## Context

Audit finding **A-04** identified that `Game-Requirements/requirements/22-Drone-Swarm-Platforms.md` (SWARM-31 §1) requires US/NATO catalog platforms to *declare* `cecCapable` while Phase A generic presets remain non-CEC by default.

Code audit confirms that `CecCapable` is implemented as a boolean attribute on `CatalogSwarmPlatform` and in `Sim/Cec/*` (`CecMeshController`, `CecMeshEvaluator`, `CecRemoteEngageGate`). Catalog defaults and tests (`Phase_B_generic_is_not_cec_capable_and_usn_exemplar_is`) establish the shipped authoring gate: generic presets are not CEC-capable; US/NATO exemplars set the flag explicitly.

There is **no** runtime nationality, alliance, or affiliation validator in `ProjectAegis.Sim.Cec`. Any platform marked `CecCapable = true` can participate in same-side CEC meshes regardless of catalog country metadata. SWARM-31 §1 normative text governs catalog eligibility ("may declare" / "do not receive by default"), not a Sim-side nationality enforcement pass.

## Decision

1. **De-scope** a runtime Sim/Data nationality validator that would reject `CecCapable` based on country-of-origin metadata.
2. **Retain** the shipped catalog authoring gate: `cecCapable` remains an explicit per-platform flag in catalog/workbook (`PlatformWorkbookImporter`/`Exporter`); generic presets stay non-CEC; US/NATO exemplars are curated via catalog rows, not inferred at runtime.
3. Update doc 22 mapping rows to distinguish **Shipped** catalog eligibility (explicit flags + defaults) from **GAP** runtime nationality enforcement, without demoting delivered CEC mesh behavior.

## Consequences

### Positive
- Sim logic remains simple, orthogonal, and side-based without coupling to national alliance taxonomy.
- Requirements audit honestly separates catalog authoring (landed) from unbuilt runtime nationality policing.

### Negative
- Automated catalog ingestion does not reject `CecCapable` based on nationality metadata; catalog curation must ensure non-NATO/unsupported factions do not have `CecCapable` set if historical fidelity is required.
