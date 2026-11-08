# SWARM Phase B — Wave 3 parallel kickoff (2026-08-09)

**Umbrella:** DRG-92 / DRG-83 · **Parent status:** B1–B6a **merged** (PRs #420–#425)  
**Skill:** `dispatching-parallel-agents` · surface-disjoint lanes only

## Prior wave (done)

| Lane | Issue | PR | Status |
|------|-------|-----|--------|
| B1 modes/host/link | DRG-94 | #420 | MERGED |
| B2 catalog CEC | DRG-93 | #421 | MERGED |
| B3 C2 panel | DRG-95 | #422 | MERGED |
| B4 regen | DRG-97 | #423 | MERGED |
| B5 contact class | DRG-96 | #424 | MERGED |
| B6a CEC mesh | DRG-102 | #425 | MERGED |

## Wave 3 lanes (surface-disjoint)

| Lane | Issue | Branch | Surface | Do NOT touch |
|------|-------|--------|---------|--------------|
| **B6b** remote engage | DRG-103 | `drgamtd/drg-103-swarm-b6b-cec-remote-engage-on-remote-data-swarm-31-engage` | `src/ProjectAegis.Sim/Engage/**` + Engage tests | `Cec/**` (read-only), Swarm, Policy, Data, Delegation |
| **B7** doctrine/WRA | DRG-99 | `drgamtd/drg-99-swarm-b7-doctrinewra-for-swarm-auto-engage-swarm-15` | `src/ProjectAegis.Sim/Policy/**` + Policy tests | Engage, Cec, Swarm, Data, Delegation |
| **B8** agent intents | DRG-100 | `drgamtd/drg-100-swarm-b8-agent-delegation-for-swarm-intents-swarm-23` | `src/ProjectAegis.Delegation/**` (not Projection swarm) + Delegation.Tests | Projection/Swarm*, Sim/*, Data/* |
| **B9** scenario place | DRG-101 | `drgamtd/drg-101-swarm-b9-scenario-editor-placeconfigure-swarm-swarm-22` | `src/ProjectAegis.Data/Scenario/**` + Data.Tests/Scenario | Sim, Delegation, Cec |

## Dispatch rule

Never co-edit intersecting surfaces. B6b may **read** `CecMeshController` / `CecCompositeTrack` but must not modify B6a files.

## Exit

Each lane: TDD, green `dotnet test` on owned test filter, QA under `production/qa/swarm-b*.md`, PR against main, Linear Done on merge.
