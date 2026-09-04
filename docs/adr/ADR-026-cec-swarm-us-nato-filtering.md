# ADR-026: Generic Side-Affiliated CEC Swarm Participation Without Runtime Nationality Filtering

- **Status:** Accepted
- **Date:** 2026-09-04
- **Deciders:** Conductor Requirements Steward, Architecture Review Board, Simulation & Swarm Stewards
- **Technical Story:** AEGIS-311 / DRG-241 (Audit Finding A-04, SWARM-31 §1, REQ-22)

## Context and Problem Statement

Audit finding **A-04** identified that `Game-Requirements/requirements/22-Drone-Swarm-Platforms.md` (SWARM-31 §1) specified that US/NATO catalog platforms declare `cecCapable`, while Phase A generic presets remain non-CEC by default, and marked the capability status in the document footer as "landed".

Code audit of `src/ProjectAegis.Sim/Cec/` (`CecMeshController`, `CecMeshEvaluator`, `CecRemoteEngageGate`, `CecCompositeTrack`) confirms that Cooperative Engagement Capability (CEC) mesh participation is evaluated purely on:
1. Per-node `CecCapable` boolean capability flag.
2. Operational state (node alive and not jammed).
3. Spatial peer range (`bestPeerRangeDeg <= 2.0°` for connected mesh, `<= 4.0°` for degraded).
4. **Generic same-side affiliation** (`SideId`).

There is **zero** runtime nationality, alliance, or country-of-origin filtering logic in `ProjectAegis.Sim.Cec`. Any platform marked `CecCapable = true` can form or join a CEC mesh with friendly units on its side, regardless of nationality metadata stored in the catalog.

The catalog authoring gate, implemented via `PlatformWorkbookImporter` and validated by tests such as `Phase_B_generic_is_not_cec_capable_and_usn_exemplar_is`, ensures that generic presets are non-CEC by default while US/NATO exemplars declare `CecCapable = true`. However, this is an authoring/curation convention rather than a runtime Sim-layer nationality policing pass.

How should Project Aegis document this architectural boundary between catalog authoring defaults and runtime simulation affiliation?

## Decision Drivers

* **Domain Separation**: Simulation core physics and networking should deal with sides, kinematics, and electronic warfare, rather than geopolitical taxonomies or national alliance memberships.
* **Algorithmic Efficiency**: Keeping `CecMeshEvaluator` free of string comparisons, nationality tables, or alliance matrices ensures optimal execution speed during mesh evaluations.
* **Faction Flexibility**: Scenario creators must be able to assign CEC capabilities to custom coalitions, neutral forces, or fictional OPFOR platforms without fighting hardcoded nationality checks.
* **Audit Transparency**: Clearly delineate shipped catalog curation gates from unbuilt runtime nationality enforcement.

## Considered Options

* **Option 1**: Build a runtime nationality and alliance validator in `ProjectAegis.Sim.Cec` that inspects platform country metadata and disallows CEC mesh membership for platforms not belonging to recognized US/NATO allied factions.
* **Option 2**: Document and ratify generic side-based affiliation in `ProjectAegis.Sim.Cec`, maintain `CecCapable` as a pure capability flag, and affirm that US/NATO distinction is an authoring/catalog concern rather than an engine-level runtime gate.

## Decision Outcome

Chosen option: **Option 2**, because it preserves domain separation, ensures maximum simulation performance, supports flexible scenario authoring, and accurately reflects the delivered architecture.

### Decision Details

1. **De-scope Runtime Nationality Filtering**:
   - Formally de-scope runtime nationality / alliance enforcement from `ProjectAegis.Sim.Cec`.
   - Ratify that CEC mesh membership at simulation runtime is strictly governed by:
     - `node.CecCapable == true`
     - Same side (`SideId`)
     - Operational status (`alive && !jammed`)
     - Kinematic proximity (`range <= 2.0°` connected, `<= 4.0°` degraded)

2. **Retain Catalog Curation Authoring Gate**:
   - Retain the shipped catalog authoring convention where `cecCapable` is explicitly curated per platform in the workbook/catalog.
   - Maintain catalog defaults: generic baseline presets remain non-CEC by default; US/NATO exemplars explicitly declare `cecCapable = true`.

3. **Requirements Synchronization**:
   - Update `Game-Requirements/requirements/22-Drone-Swarm-Platforms.md` (SWARM-31 §1) mapping rows to distinguish **Shipped** catalog eligibility (explicit flags and curated defaults) from **GAP / De-scoped** runtime nationality policing.

### Positive Consequences

* Clean, side-agnostic simulation engine architecture without geopolitical coupling.
* Highly efficient, deterministic mesh evaluation in `CecMeshEvaluator`.
* Allows scenario designers to configure cross-national allied task forces or fictional adversaries with advanced networking capabilities.
* Eliminates audit finding A-04 by aligning requirement status with code reality.

### Negative Consequences

* Automated catalog ingestion does not reject `CecCapable = true` on non-allied platforms based on country metadata; catalog curators must ensure data fidelity if historical realism is required.

## Pros and Cons of the Options

### Option 1: Implement Runtime Nationality Gate in Sim

* Good, because it would enforce real-world alliance exclusivity at the engine level.
* Bad, because it pollutes the headless simulation layer with geopolitical taxonomy.
* Bad, because it breaks fictional, future, or coalition scenarios where platforms from different nations interoperate.
* Bad, because it introduces string/lookup overhead into the CEC mesh evaluation cycle.

### Option 2: Generic Side Affiliation in Sim & Catalog Gate (Chosen)

* Good, because it reflects the shipped, tested, and high-performance implementation.
* Good, because it adheres to the single-responsibility principle for the Sim assembly.
* Good, because it offers full scenario authoring versatility.
* Bad, because catalog data curation bears the responsibility for historical nationality consistency.

## Implementation Notes

* **IMP-026.1**: `ProjectAegis.Sim.Cec.CecMeshEvaluator` remains focused solely on boolean capability, spatial distance, and operational health.
* **IMP-026.2**: `PlatformWorkbookImporter` continues to parse the `CecCapable` column on the `Swarms` sheet without enforcing country constraints.
* **IMP-026.3**: Verification is maintained by `CecMeshEvaluatorTests.cs`, `CecRemoteEngageGateTests.cs`, and `PlatformWorkbookPeIntegrationHardeningTests.cs`.

## References

* **REF-026.1**: `Game-Requirements/reviews/audit-findings-2026-09-02.md` (Finding A-04)
* **REF-026.2**: `Game-Requirements/requirements/22-Drone-Swarm-Platforms.md` (SWARM-31 §1)
* **REF-026.3**: `src/ProjectAegis.Sim/Cec/CecMeshEvaluator.cs`
* **REF-026.4**: `src/ProjectAegis.Sim/Cec/CecRemoteEngageGate.cs`
* **REF-026.5**: `src/ProjectAegis.Sim.Tests/Cec/CecMeshEvaluatorTests.cs`
