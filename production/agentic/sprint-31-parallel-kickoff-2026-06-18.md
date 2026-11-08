# Sprint 31 Parallel Kickoff

**Date:** 2026-06-18  
**Trunk:** `main` @ `3406bc4`  
**Baseline:** 956/956; ReplayGolden 6/6; GitNexus 13,496 / 27,690  
**Sprint plan:** `production/sprints/sprint-31-corpus-combat-polish.md`  
**QA plan:** `production/qa/qa-plan-sprint-31-2026-10-30.md`

## Wave plan

| Wave | Stories | Track | Est. |
|------|---------|-------|------|
| Day-1 | S31-01 | DevOps baseline | 1d |
| Wave 1 | S31-02, S31-04 | Data sensor approve ∥ Sim mine validator | 2d / 1.5d |
| Wave 2 | S31-03 | Data TL release-train load | 2d |
| Wave 3 | S31-05, S31-06, S31-07 | Sim facility/BDA ∥ Unity presentation | 2d / 1.5d / 1d |
| Wave 4 | S31-08, S31-09..11 | QA sign-off ∥ Data nice-to-have | parallel |
| Closeout | S31-12, S31-13 | DevOps | 0.75d |

## Track ownership

| Track | Team | Stories | Graphite prefix |
|-------|------|---------|-----------------|
| **Data** | team-data | S31-02, S31-03, S31-09, S31-10, S31-11 | `stack/sprint31/corpus-approve`, `stack/sprint31/tl-release-train` |
| **Simulation** | team-simulation | S31-04, S31-05, S31-06 | `stack/sprint31/combat-phase5` |
| **Unity** | team-unity | S31-07 | `stack/sprint31/presentation-evidence` |
| **QA** | team-qa | S31-08 | `stack/sprint31/c2-signoff-refresh` |
| **DevOps** | c-sharp-devops-engineer | S31-01, S31-12, S31-13 | `stack/sprint31/full-sln-gate`, `stack/sprint31/closeout` |

## Hard gates (every merge)

- `dotnet test ProjectAegis.sln` — ≥956 (closeout ≥996)
- `ReplayGoldenSuiteTests` — 6/6 on sim/delegation merges
- ZERO touch `DelegationBridge.cs`
- `CatalogWriteGate` extend-only on data merges
- No 7208-record sensor in CI

## Cut line

Drop nice-to-have (S31-09..12) before should-have S31-08/06 if Wave 2 slips.  
**Minimum shippable beyond must-have:** S31-05 + S31-13.

## Dispatch order (recommended)

```bash
# Day-1
/dev-story dispatch S31-01

# Wave 1 (parallel)
/dev-story dispatch S31-02 S31-04

# Wave 2 (sequential — TL gate)
/dev-story dispatch S31-03

# Wave 3 (parallel)
/dev-story dispatch S31-05 S31-06 S31-07

# Wave 4 + closeout
/dev-story dispatch S31-08
/dev-story dispatch S31-09 S31-10 S31-11  # if capacity
/dev-story dispatch S31-13
```