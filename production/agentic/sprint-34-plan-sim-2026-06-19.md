# Sprint 34 — Simulation Track Plan

**Owner:** team-simulation  
**Must-have:** S34-04 (~1.5d)  
**Should-have:** S34-07, S34-09 (optional)

## Stories

| ID | Name | Est. | Priority | Deps |
|----|------|------|----------|------|
| S34-04 | Catalog-derived datalink share lag | 1.5d | must | S34-01, S34-02 (`TryGetLinkLatencyMs`) |
| S34-07 | `baltic-patrol-datalink-catalog-latency` fixture | 1d | should | S34-04 |
| S34-09 | Datalink regression smoke (optional) | 0.5d | nice | S34-04 |

## Design

`DatalinkShareLagResolver` at harness bind: scenario `shareLagTicks` overrides catalog `LatencyMsNominal`. No per-tick SQLite reads. No ECCM Phase 2.

## Verify

```bash
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Datalink|Comms|ShareLag|Contact" -v minimal
/replay-verify
git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
```

## Cut line

1. S34-09 regression  
2. — keep S34-07 fixture