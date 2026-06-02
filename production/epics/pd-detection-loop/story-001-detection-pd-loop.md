# Story 001: Deterministic Pd detection loop

> **Epic**: Pd Detection Loop  
> **Status**: Complete (PR 1)  
> **Type**: Logic  
> **TR**: TR-sensor-002

## Acceptance Criteria

- [x] `DetectionProbability.ComputePd` clamps `basePd × envMask × (1 − jamStrength)`.
- [x] `DeterministicDetectionLoop` sorts trials `(observerId, sensorId, targetId)`.
- [x] Draw uses `SeededRng.UnitFloat(Detection, entityId, simTick, drawIndex)`.
- [x] `detected ⇔ draw < Pd`; same inputs → identical roll list.

## Test Evidence

- `src/ProjectAegis.Sim.Tests/Sensors/DeterministicDetectionLoopTests.cs`