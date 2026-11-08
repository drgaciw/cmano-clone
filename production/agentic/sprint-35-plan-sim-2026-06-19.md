# Sprint 35 Plan — Simulation / Perf Track

**Date:** 2026-06-19  
**Agent:** team-simulation parallel dispatch

## Stories

| ID | Title | Priority | Est | Key files |
|----|-------|----------|-----|-----------|
| S35-05 | Detection hot path P0 | must-have | 3d | `PdDetectionContactSimulator.cs`, `DeterministicDetectionLoop.cs` |
| S35-10 | DecisionLog + Datalink P1 | should-have | 2.5d | `DecisionLog.cs`, `DatalinkSidePictureMerger.cs` |
| S35-17 | Perf re-profile appendix | nice-to-have | 0.5d | `perf-profile-polish-baseline-2026-06-19.md` |

## Hard gates

- ReplayGolden 6/6; Baltic hash `17144800277401907079` unchanged  
- `/replay-verify` on every sim merge  
- ZERO `DelegationBridge.cs` touch  

## Test filters

```bash
dotnet test --filter "FullyQualifiedName~ReplayGolden"
dotnet test --filter "FullyQualifiedName~PdDetection|FullyQualifiedName~DeterministicDetection"
dotnet test --filter "FullyQualifiedName~DecisionLog|FullyQualifiedName~DatalinkSidePicture"
dotnet test --filter "FullyQualifiedName~BalticReplayHarness"
```

## Cut line

Drop S35-17 → S35-10 → keep S35-05 at all costs.