# Epic: EMCON Headless Slice

> **Status:** Complete  
> **Created:** 2026-06-02  
> **Priority:** MVP  
> **Layer:** Foundation (policy + sensor gate)  
> **Depends on:** [sensor-headless-slice](../sensor-headless-slice/EPIC.md) (Complete)

## Goal

Scenario **radar EMCON** gates active-radar contacts and engage aborts: `Off` suppresses detection and yields explainable `EmconOff` / no-track outcomes in the order log.

## Scope (in)

- `EmconState` + scenario JSON `emcon.units`
- `ScenarioContactSimulator` skips active-radar seeds when observer radar ≠ Active
- `EngageContext.RadarEmconActive` → `EngagementAbortReason.EmconOff`
- `baltic-patrol-emcon-off` scenario + harness tests

## Scope (out)

- Per-emitter class sonar/ESM/datalink
- Mid-tick EMCON transitions
- C2 EMCON UI

## Governing docs

- [policy-roe-emcon-wra.md](../../design/gdd/policy-roe-emcon-wra.md)
- [sensor-detection-ew.md](../../design/gdd/sensor-detection-ew.md)

## Acceptance (epic-level)

1. Radar `Off` → no `ContactChange` for gated seeds; fingerprint stable across runs.
2. Engage with radar `Off` logs `EmconOff` (not silent failure).
3. `dotnet test ProjectAegis.sln` green.