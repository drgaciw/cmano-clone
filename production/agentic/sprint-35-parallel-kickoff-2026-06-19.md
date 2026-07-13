# Sprint 35 Parallel Kickoff

**Date:** 2026-06-19 (post-S34 QA APPROVED + gate r2 CONCERNS uplifted)  
**Trunk:** `main` @ `8de98b1` — **1193/1193**, ReplayGolden 6/6  
**Sprint plan:** `production/sprints/sprint-35-polish-phase-1-entry.md`  
**QA plan:** **MISSING** — **S35-02** blocks all feature waves

## Wave plan

| Wave | Stories | Track | Est. | Notes |
|------|---------|-------|------|-------|
| Day-1 | S35-01 | DevOps baseline | 1d | **READY** — S34 closeout @ `8de98b1` |
| W0 | S35-02 | QA plan | 1d | **BLOCKS** S35-03 through S35-14 |
| W1 | S35-03, S35-04 | UX foundation ∥ Unity Profiler | 2d / 1.5d | Parallel after S35-02 |
| W2 | S35-05 | Sim perf P0 | 3d | `/replay-verify` every PR |
| W2b | S35-06, S35-08 | C2 tooltips ∥ AegisTokens | 2d / 1.5d | After S35-04 |
| W3 | S35-10 | Sim perf P1 (optional) | 2.5d | After S35-05 |
| W3b | S35-09 | Live Editor evidence | 2d | After S35-08; lean skip OK |
| W4 | S35-07, S35-11 | C2 sign-off ∥ Playtest 7 | 1d / 1d | After S35-06 |
| W4b | S35-12 | Data validation polish | 1d | Parallel if capacity |
| Closeout | S35-14, S35-13, S35-15 | DevOps + stage | 1d | S35-13 optional stage advance |

## Track ownership

| Track | Team | Stories | Stack prefix |
|-------|------|---------|--------------|
| **DevOps** | c-sharp-devops-engineer | S35-01, S35-14, S35-15, S35-13 | `stack/sprint35/full-sln-gate`, `stack/sprint35/closeout` |
| **QA / UX** | team-qa + team-ui | S35-02, S35-03, S35-07, S35-11 | `stack/sprint35/qa-plan`, `stack/sprint35/ux-foundation` |
| **Simulation** | team-simulation | S35-05, S35-10 | `stack/sprint35/sim-perf-p0`, `stack/sprint35/sim-perf-p1` |
| **Unity** | team-unity | S35-04, S35-06, S35-08, S35-09 | `stack/sprint35/c2-profiler`, `stack/sprint35/c2-polish` |
| **Data** | team-data | S35-12, S35-16 | `stack/sprint35/validation-polish` |

## Hard gates (every merge)

- `dotnet test ProjectAegis.sln` — ≥**1193**
- `ReplayGoldenSuiteTests` — **6/6** on sim/delegation merges
- ZERO touch `DelegationBridge.cs`
- `CatalogWriteGate` extend-only on data merges
- C2 headless proxy **18/18** (61/61 + 58/58 filters)
- Production Baltic hash `17144800277401907079` unchanged
- `/replay-verify` on S35-05, S35-10

## Cut line

1. S35-15 (CI hygiene — 6th deferral OK)  
2. S35-16 (dependency-graph plan-only)  
3. S35-09 (live Editor — lean placeholders OK)  
4. S35-11 (playtest session 7)  
5. S35-10 (P1 LINQ)  

**Minimum shippable beyond must-have:** **S35-08** + **S35-13** (stage advance if user confirms).

## Dispatch order (recommended)

```bash
# Blocking
/dev-story dispatch S35-01
/dev-story dispatch S35-02    # /qa-plan sprint

# W1 parallel
/dev-story dispatch S35-03 S35-04

# W2
/dev-story dispatch S35-05

# W2b parallel
/dev-story dispatch S35-06 S35-08

# W3 optional
/dev-story dispatch S35-10

# W3b
/dev-story dispatch S35-09

# W4
/dev-story dispatch S35-07 S35-11

# Closeout
/dev-story dispatch S35-14
# Optional: S35-13 stage advance after user confirms gate CONCERNS
```