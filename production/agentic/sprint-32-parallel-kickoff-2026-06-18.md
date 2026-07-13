# Sprint 32 Parallel Kickoff

**Date:** 2026-06-18  
**Trunk:** `main` @ `3406bc4`  
**Baseline:** 1006/1006; ReplayGolden 6/6; GitNexus 14,160 / 28,928  
**Sprint plan:** `production/sprints/sprint-32-release-train-combat-phase6-platform-phase-f.md`  
**QA plan:** *(pending — run `/qa-plan sprint` before S32-02+)*

## Wave plan

| Wave | Stories | Track | Est. |
|------|---------|-------|------|
| Day-1 | S32-01 | DevOps baseline | 1d |
| Wave 1 | S32-02, S32-04 | Data manifest ∥ Sim facility validator | 2d / 1.5d |
| Wave 2 | S32-03, S32-05 | Data quarantine ∥ Sim ECCM | 2.5d / 1.5d |
| Wave 3 | S32-06, S32-08 | Unity Phase F ∥ Sim mine transit | 2d / 2d |
| Wave 4 | S32-07, S32-09, S32-10 | Data diff ∥ Sim BDA ∥ Unity evidence | parallel |
| Wave 5 | S32-11 | QA C2 sign-off upgrade | 1d |
| Closeout | S32-12, S32-13 | DevOps | 0.75d |

## Track ownership

| Track | Team | Stories | Graphite prefix |
|-------|------|---------|-----------------|
| **Data** | team-data | S32-02, S32-03, S32-07 | `stack/sprint32/release-train-ops` |
| **Simulation** | team-simulation | S32-04, S32-05, S32-08, S32-09 | `stack/sprint32/combat-phase6` |
| **Unity** | team-unity | S32-06, S32-10 | `stack/sprint32/platform-phase-f` |
| **QA** | team-qa | S32-11 | `stack/sprint32/c2-signoff-upgrade` |
| **DevOps** | c-sharp-devops-engineer | S32-01, S32-12, S32-13 | `stack/sprint32/full-sln-gate`, `stack/sprint32/closeout` |

## Hard gates (every merge)

- `dotnet test ProjectAegis.sln` — ≥1006 (closeout ≥1046)
- `ReplayGoldenSuiteTests` — 6/6 on sim/delegation merges
- ZERO touch `DelegationBridge.cs`
- `CatalogWriteGate` extend-only on data merges
- No full corpora in CI; `rg TlBranchDatabase|BranchDatabase` → zero

## Cut line

Drop nice-to-have (S32-12) before should-have S32-10/11 if Wave 3 slips.  
**Minimum shippable beyond must-have:** **S32-07** release diff CLI + **S32-13** closeout.

## Dispatch order (recommended)

```bash
# Day-1
/dev-story dispatch S32-01

# Wave 1 (parallel)
/dev-story dispatch S32-02 S32-04

# Wave 2 (parallel after manifest schema stable)
/dev-story dispatch S32-03 S32-05

# Wave 3 (parallel)
/dev-story dispatch S32-06 S32-08

# Wave 4 (parallel)
/dev-story dispatch S32-07 S32-09 S32-10

# Wave 5 + closeout
/dev-story dispatch S32-11
/dev-story dispatch S32-13
# S32-12 if capacity
```