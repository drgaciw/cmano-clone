# Epic: Pd Detection Loop

> **Status:** Complete (PR #23)  
> **Created:** 2026-06-02  
> **Priority:** MVP  
> **Layer:** Core (sim tick step 4)  
> **Depends on:** [emcon-headless-slice](../emcon-headless-slice/EPIC.md), [sensor-headless-slice](../sensor-headless-slice/EPIC.md)

## Goal

Replace schedule-only contacts with a **deterministic Pd detection loop**: sorted `(observer, sensor, target)` trials, `SeededRng` Detection domain, logged `ContactChange` on success.

## Scope (in)

- `DetectionProbability` MVP formula (basePd × env × (1−jam))
- `DeterministicDetectionLoop` sorted iteration + stable draws
- Scenario `detection[]` in `*.policy.json`
- `PdDetectionContactSimulator` + Baltic harness wiring

## Scope (out)

- Platform DB `basePd` tables
- Full emitter geometry / range propagation
- EW beyond jamStrength scalar
- Classify/identify FSM beyond Detected

## Governing docs

- [sensor-detection-ew.md](../../design/gdd/sensor-detection-ew.md) — TR-sensor-002
- [policy-roe-emcon-wra.md](../../design/gdd/policy-roe-emcon-wra.md) — EMCON gate

## Acceptance (epic-level)

1. Same seed + scenario → identical detection outcomes at tick T.
2. Sorted trial order documented and property-tested.
3. `baltic-patrol` can use `detection[]` with stable `ContactChange` + engage.
4. `dotnet test ProjectAegis.sln` green.