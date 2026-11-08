# Swarm Pressure Gauntlet — 2026-08-12 (S117)

**Branch:** `swarm-pressure-gauntlet`  
**Linear:** DRG-149 / 150 / 151 / 152  
**Scope:** Orthogonal pressure for drone-swarm Sim surface + saboteur pure-Sim path  
**Invariants:** Determinism, aggregate SoT, authorized integrity only, zero DelegationBridge / CatalogWriteGate / locked-eval touch

## Deliverables

| Track | Artifact |
|-------|----------|
| S117-a | `src/ProjectAegis.Sim.Tests/Swarm/SwarmPressureTests.cs` (12 cases (9 methods)) |
| S117-b | 6 Swarm mutants + patches |
| S117-c | `--swarm-filter` + 4× `swarm_*` axes (config-only) |

## Verify

```bash
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj --filter "FullyQualifiedName~Swarm"
# expect 136 passed
python3 tools/qa-gauntlet/saboteur.py --help | grep swarm-filter
```

## Mutants

10 integrity no-clamp · 11 regen ignores max · 12 dead still moves · 14 EMP freeze zero · 15 caps no clamp · 17 assault never split
