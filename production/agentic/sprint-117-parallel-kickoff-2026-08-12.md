# S117 parallel kickoff — 2026-08-12

**Epic:** DRG-149 · **Lanes:** DRG-150 / 151 / 152  
**Branch:** `swarm-pressure-gauntlet`  
**Dispatch:** file-disjoint; one PR (surfaces do not collide on `src/` production)

| Lane | Writes | Does not write |
|------|--------|----------------|
| A | `Sim.Tests/Swarm/SwarmPressureTests.cs` | mutants, saboteur, axes |
| B | `tools/qa-gauntlet/mutants/*` | tests, saboteur.py body |
| C | `saboteur.py`, `stress-axes.yaml`, sprint/qa docs | test class, mutant patch bodies (except catalog) |

**Merged as single PR** because B patches are generated from A-killable defects and C wires the runner. Serial merge not required.

**Invariants:** ZERO DelegationBridge · Catalog extend-only · Stage Release.
