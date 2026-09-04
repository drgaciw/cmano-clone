# ADR-025: Decoupling of Technology Level 5 and Black Project Mode Enforcement

- **Status:** Accepted
- **Date:** 2026-09-04
- **Deciders:** Conductor Requirements Steward, Architecture Review Board, Simulation Stewards
- **Technical Story:** AEGIS-311 / DRG-241 (Audit Finding A-03, DRG-47, REQ-10)

## Context and Problem Statement

Audit finding **A-03** identified that `Game-Requirements/requirements/10-Speculative-Systems.md` originally stated that *"TL-5 requires `BLACK_PROJECT_MODE`"* as an invariant global requirement across all simulation systems.

However, verification against the implementation shows that `SpeculativeEngageGate.Evaluate` (`src/ProjectAegis.Sim/Scenario/SpeculativeEngageGate.cs`) evaluates weapon capability attributes against scenario settings orthogonally:
```csharp
if (context.WeaponTechnologyLevel > settings.MaxTechnologyLevel)
{
    return EngagementAbortReason.TechnologyLevelExceeded;
}

if (context.WeaponRequiresBlackProject && !settings.BlackProjectMode)
{
    return EngagementAbortReason.BlackProjectRequired;
}
```

The gate evaluates pure scalar and boolean fields (`WeaponTechnologyLevel` and `WeaponRequiresBlackProject`) on `EngageContext` directly in memory without performing runtime database queries or joins against the platform catalog. Consequently:
1. A scenario configured with `ScenarioSpeculativeSettings(blackProjectMode: false, maxTechnologyLevel: 5)` is completely valid and tested in the test suite (`ScenarioSpeculativeGateTests`, `SpeculativeHonestyPinsTests`).
2. Weapons at Technology Level 5 that do not carry `WeaponRequiresBlackProject = true` are permitted to engage freely without enabling black project mode.
3. This behavior aligns with the **DRG-47** owner decision (2026-08-09), which scopes the shipped speculative spine to orthogonal boolean engagement gates while deferring speculative platform simulation to Phase N.

How should Project Aegis ratify this decoupled evaluation model and update requirements documentation?

## Decision Drivers

* **Simulation Performance**: Engagement gating occurs on the critical sim tick path (`MvpEngagementResolver`); it must remain zero-allocation and must never issue catalog I/O.
* **Authoring Flexibility**: Scenario authors should be able to field unclassified or publicly acknowledged near-future/TL-5 systems (e.g., operational hypersonic glide bodies or directed energy air defense) without being forced to enable fictional or deeply classified "black project" mechanics.
* **Scoping Alignment (DRG-47)**: Respect the established boundary between the shipped headless gate spine and deferred Phase N speculative behaviors.
* **Deterministic Verification**: Preserve existing golden and honesty test pins that validate orthogonal checks.

## Considered Options

* **Option 1**: Introduce a global validator rule in `ScenarioValidationEngine` or `SpeculativeEngageGate` that hard-couples TL-5 and Black Project Mode, aborting any engagement or scenario load where `MaxTechnologyLevel == 5` and `BlackProjectMode == false`.
* **Option 2**: Ratify the decoupled boolean check in `SpeculativeEngageGate` per DRG-47, documenting `WeaponTechnologyLevel` and `WeaponRequiresBlackProject` as independent evaluation axes, and updating Doc 10 requirements text accordingly.

## Decision Outcome

Chosen option: **Option 2**, because it preserves hot-path simulation performance, provides realistic scenario authoring flexibility, honors the DRG-47 product scoping decision, and eliminates the discrepancy identified in audit finding A-03.

### Decision Details

1. **Ratify Decoupled Evaluation Axes**:
   - Formally ratify that `TechnologyLevel` (1 through 5) and `BlackProjectMode` (boolean) are independent doctrinal axes.
   - Technological maturity governs whether an asset's technology generation exceeds the scenario ceiling (`TechnologyLevelExceeded`).
   - Program classification governs whether special access / black project systems are permitted in the theater (`BlackProjectRequired`).

2. **Preserve Zero-Catalog Runtime Invariant**:
   - `SpeculativeEngageGate` remains a pure, static gate that evaluates in-memory fields on `EngageContext` without querying SQLite catalog tables during tick resolution.

3. **Requirements Harmonization**:
   - Update `Game-Requirements/requirements/10-Speculative-Systems.md` to reword the former blanket coupling ("TL-5 unconditionally implies `BLACK_PROJECT_MODE`") to per-weapon orthogonal semantics matching `SpeculativeEngageGate`.
   - Affirm DRG-47 authority over the speculative spine boundary.

### Positive Consequences

* Scenario authors can configure near-future (TL-5) battles without enabling exotic black project weaponry.
* Zero performance degradation on the engagement resolution pipeline.
* Eliminates audit finding A-03 and harmonizes specification text with existing unit tests.

### Negative Consequences

* Scenario authors who specifically desire strict coupling between TL-5 and Black Project mode must configure both settings (`maxTechnologyLevel: 5`, `blackProjectMode: true`) explicitly in their scenario policy definitions.

## Pros and Cons of the Options

### Option 1: Enforce Synthetic Coupling Rule

* Good, because it adheres strictly to early conceptual text in Doc 10.
* Bad, because it artificially restricts scenario design (e.g., standard TL-5 systems cannot be used in conventional near-future scenarios).
* Bad, because it breaks existing tests (`ScenarioSpeculativeGateTests`) that verify valid `(blackProjectMode: false, maxTechnologyLevel: 5)` configurations.

### Option 2: Ratify Decoupled Boolean Check per DRG-47 (Chosen)

* Good, because it accurately reflects the shipped and tested simulation code.
* Good, because it provides clear separation of concerns between technology generation and security classification.
* Good, because it maintains ultra-fast in-memory engagement evaluation.
* Bad, because documentation requires targeted revisions to clarify the two-axis model.

## Implementation Notes

* **IMP-025.1**: `SpeculativeEngageGate.Evaluate` remains unchanged, returning `TechnologyLevelExceeded` and `BlackProjectRequired` as distinct, independent abort codes.
* **IMP-025.2**: `ScenarioSpeculativeSettings.CampaignDefault` continues to provide the baseline configuration (`MaxTechnologyLevel = 4`, `BlackProjectMode = false`).
* **IMP-025.3**: Verification is maintained by `SpeculativeHonestyPinsTests.cs` and `ScenarioSpeculativeGateTests.cs`.

## References

* **REF-025.1**: `Game-Requirements/reviews/audit-findings-2026-09-02.md` (Finding A-03)
* **REF-025.2**: `Game-Requirements/requirements/10-Speculative-Systems.md` (Speculative Systems)
* **REF-025.3**: `production/agentic/drg-47-phase-n-scoping-decision-2026-08-09.md` (DRG-47 Decision)
* **REF-025.4**: `src/ProjectAegis.Sim/Scenario/SpeculativeEngageGate.cs`
* **REF-025.5**: `src/ProjectAegis.Sim.Tests/Scenario/ScenarioSpeculativeGateTests.cs`
