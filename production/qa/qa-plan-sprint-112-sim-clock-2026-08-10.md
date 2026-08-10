# QA Plan — S112 Sim Clock

| TC | Check |
|----|-------|
| TC-CLK-1 | Pause then TickOnce(RealTime) → SimTick unchanged, hash unchanged |
| TC-CLK-2 | Resume then TickOnce → advances |
| TC-CLK-3 | AccelerationFactor=4 Accelerated ×1 call ≡ RealTime ×4 for hash |
| TC-CLK-4 | Paused blocks Accelerated too |
| TC-CLK-5 | HeadlessBatch: documented override (advances while paused for CI batch) OR same pause rules — pick one and test |
| TC-CLK-6 | Session Pause/Resume/SetAcceleration round-trip |
| TC-CLK-7 | ReplayGolden 6/6 |

Sign-off: smoke-sprint-112.
