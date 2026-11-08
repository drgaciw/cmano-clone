# SWARM Phase C — Wave 1 parallel kickoff (2026-08-09)

**Umbrella:** Phase C · **Epic:** DRG-83 · **Prior:** Phase B closeout (DRG-92 Done)  
**Skill:** `dispatching-parallel-agents` · surface-disjoint lanes only

## Wave 1 lanes (surface-disjoint)

| Lane | Req | Surface | Do NOT touch |
|------|-----|---------|--------------|
| **C1** Formations | SWARM-16 / DRG-105 | `src/ProjectAegis.Sim/Swarm/Formation/**` + Swarm formation tests; thin `SwarmController` SetFormation/IssueSetFormation only | Engage, Policy, SoftKill, Data, Delegation, Cec |
| **C2** Multi-axis split | SWARM-17 / DRG-106 | `src/ProjectAegis.Sim/Swarm/Assault/**` + assault tests (new dir only; read SwarmController public API) | Formation/**, SoftKill/**, Policy, Data, Delegation |
| **C3** EMP/jam soft-kill | SWARM-18 / DRG-107 | `src/ProjectAegis.Sim/Swarm/SoftKill/**` + soft-kill tests; may call `ApplyLinkState` / mode freeze APIs | Formation/**, Assault/**, Engage rewrite, Data |
| **C4** Expend pulse | SWARM-19 / DRG-108 (Wave 2 hold) | `src/ProjectAegis.Sim/Swarm/Expend/**` + controller `IssueExpend` + tests; respect Policy `ExpendAuthorized` (read-only Policy) | Formation/**, Assault/**, SoftKill/**, Data authoring |
| **C5** Mission types | SWARM-20 / DRG-109 | `src/ProjectAegis.Data/Scenario/**` mission/swarm defaults + Data.Tests (no Sim) | All of Sim/**, Delegation |

## Dispatch rule

Never co-edit intersecting surfaces. C1/C4 both need `SwarmController` — if collision risk, **serialize** controller patches: C1 first (Formation field), then C4 (Expend) stacks on main; C2/C3/C5 stay file-isolated.

**Recommended parallel set (no controller collision):** C2 ∥ C3 ∥ C5 first, then C1, then C4 — OR C1+C2+C3+C5 with C4 held.

## Exit

Each lane: TDD, green `dotnet test` filter, QA under `production/qa/swarm-c*.md`, PR against main, Linear Done on merge.
