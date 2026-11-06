# ADR-025: Alignment of Technology Level 5 and Black Project Mode Enforcement

## Status

**Proposed** (2026-09-02)

## Date

2026-09-02

## Context

Audit finding **A-03** identified that `Game-Requirements/requirements/10-Speculative-Systems.md` claimed *"TL-5 requires `BLACK_PROJECT_MODE`"* as a global requirement.

In the actual implementation, `SpeculativeEngageGate.Evaluate` checks weapon capability attributes against scenario settings independently:
- If `context.WeaponTechnologyLevel > settings.MaxTechnologyLevel`, engagement aborts with `TechnologyLevelExceeded`.
- If `context.WeaponRequiresBlackProject && !settings.BlackProjectMode`, engagement aborts with `BlackProjectRequired`.

A configuration with `ScenarioSpeculativeSettings(blackProjectMode: false, maxTechnologyLevel: 5)` is valid at the scenario level, and weapons at TL-5 that do not have `WeaponRequiresBlackProject = true` are allowed to fire.

## Decision

1. **De-scope / reword** the requirement from a blanket rule ("TL-5 unconditionally implies `BLACK_PROJECT_MODE`") to per-weapon / per-platform semantics matching `SpeculativeEngageGate`.
2. Do not introduce a synthetic validator rule in `ScenarioValidationEngine` that forbids `maxTechnologyLevel = 5` without `blackProjectMode = true`.
3. Align requirement documentation in doc 10 with the shipped two-axis gate in `SpeculativeEngageGate` and `SpeculativeHonestyPinsTests`.

## Consequences

### Positive
- Allows fine-grained scenario authoring where near-future / TL-5 equipment that is unclassified can be permitted without enabling experimental black project systems.
- Matches shipped simulation behavior and test assertions in `ScenarioSpeculativeGateTests`.

### Negative
- Authors desiring strict coupling between TL-5 and Black Project mode must set both scenario flags (`MaxTechnologyLevel = 5` and `BlackProjectMode = true`) explicitly in policy.
