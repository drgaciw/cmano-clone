# Epic: Sensor Classify / Identify FSM (Sprint 2)

> **Status:** Complete
> **Layer:** Core (`ProjectAegis.Sim.Sensors`)  
> **TR:** TR-sensor-001 remainder

## Goal

Promote sustained contacts **Detected → Classified → Identified** with deterministic tick thresholds from scenario JSON; emit `ContactChange` for each promotion.

## Acceptance

1. Same seed + lifecycle config → identical promotion tick sequence.  
2. Default lifecycle (thresholds 0) → no classify/identify rows (golden replays unchanged).  
3. Lost transitions use actual `PreviousState` (not hardcoded Detected).