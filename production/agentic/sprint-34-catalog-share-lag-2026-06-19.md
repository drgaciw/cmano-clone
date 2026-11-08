# S34-04 — Catalog-Derived Datalink Share Lag Evidence

**Date:** 2026-06-19  
**Story:** S34-04

## Implementation

- `DatalinkShareLagResolver` (`ProjectAegis.Sim`)
- `ScenarioDatalinkDoctrine.ShareLagTicksSpecified`
- Wired in `BalticReplayHarness` only — **ZERO** `DelegationBridge.cs` diff
- `baltic-patrol-datalink-comms` policy pinned with explicit `"shareLagTicks": 0` to preserve S33 isolated golden

## Formula

`ShareLagTicks = ceil(latencyMs / (1000/60))` at 60 Hz

## Verification

```bash
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "DatalinkShareLag|Datalink" -v minimal
# Passed: 26/26

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGolden" -v minimal
# Passed: 17/17 (6/6 catalog)
```