# Line of sight, radar ECCM, and off-grid units (DRG-379 / DRG-386 / DRG-390)

All three features are **opt-in, scenario-authored, and Sim-authoritative**. A scenario with no
`lineOfSight`, `radarEccm`, jammer `techGeneration`, or `commsGrid` sections and catalog rows with no
ECCM attributes behaves byte-for-byte as before: no extra RNG draws and unchanged replay goldens.

## ENV-02 — Terrain and radar-horizon LOS (DRG-379)

- `LineOfSightEvaluator` (`Sim/Sensors/LineOfSight.cs`) applies the smooth-earth horizon
  `d = √(2kRe·h₁) + √(2kRe·h₂)` with k = 4/3 for radar and 7/6 for IR/visual, then masks against
  optional terrain samples (ray height vs. sample elevation + earth bulge).
- `DeterministicDetectionLoop.RollTick(..., environment, losBlocks)` skips a blocked trial **before**
  drawing RNG (same as the EMCON skip), so unaffected trials keep identical draws.
- Block reasons are Sensor-family manifest codes: `LOS_RADAR_HORIZON` and `LOS_TERRAIN_MASK`.
  `PdDetectionContactSimulator.LastLosBlocks` exposes the blocks from the latest tick.
- Performance budget: at most `MaxTerrainSamplesPerPair` (256) samples are evaluated for each pair.
  The loader rejects longer profiles.

```json
"lineOfSight": [ { "observerId": "u1", "targetId": "hostile-1",
  "observerHeightMslMeters": 25, "targetHeightMslMeters": 15, "rangeMeters": 30000,
  "terrain": [ { "distanceMeters": 12000, "elevationMslMeters": 140 } ] } ]
```

## EW-01 — Radar ECCM attributes (DRG-386)

- Catalog: `CatalogSensorBinding.RadarScanType` (`Mechanical|Pesa|Aesa`), `FrequencyAgile`, and
  `RadarTechGeneration` (0–6, with 0 meaning unspecified). These are read from catalog JSON and validated by
  `CatalogRadarScanTypes.Validate`. SQLite persistence of these columns is a follow-up; for now the
  SQLite reader returns the defaults.
- Scenario: jammer `techGeneration`, plus `radarEccm` per-sensor overrides.
- `EccmJamMatrix.Effectiveness` = scan factor (Mech 1.0 / PESA 0.75 / AESA 0.5) × 0.8 if agile
  (PESA and AESA always count as agile) × generation factor `clamp(1 − 0.15·(radarGen − jammerGen), 0.1, 1)`,
  with a floor of 0.1. `ScenarioJamResolver` scales each jammer before it takes the max.
- `DetectionEnvironmentFactory.Build(profile, catalog)` merges catalog profiles (the first row in
  sorted order wins for a sensor id) with scenario overrides.

## C3-01 — Off-grid units in Sim (DRG-390)

- `CommsGridRegistry` (`Sim/Comms`, owned by `DelegationOrchestrator.CommsGrid` and resolved lazily from the scenario policy) holds per-unit `OnGrid/OffGrid` state from scenario
  `commsGrid` transitions. Transitions apply in (tick, unit id ordinal) order. The registry never
  rewinds, folds its state into an FNV hash, and freezes position reports while a unit is off grid.
- Enforcement lives in `DelegationOrchestrator`, because `DelegationBridge.cs` is zero-touch through Release v1:
  - Human orders issued at or after a unit's off-grid tick are dropped at drain and logged as a
    `PolicyDenial` with `FireAbortReason.OffGrid` (Doctrine code `COMMS_OFF_GRID`, agent `comms-grid`).
  - Orders issued *before* the unit left the grid, and agent/doctrine intents, still execute.
  - `TryTakeDirectControl` refuses off-grid units.
  - `C2PlayerCommandBridge.TryIssue` returns `COMMS_OFF_GRID` up front for immediate UI feedback.
- `CommsGridProjection` gives the UI off-grid rows with the last-reported position. Presentation
  never computes membership.

```json
"commsGrid": [ { "atTick": 120, "unitId": "sub-1", "membership": "OffGrid", "reason": "below comms depth" },
               { "atTick": 300, "unitId": "sub-1", "membership": "OnGrid",  "reason": "periscope depth" } ]
```
