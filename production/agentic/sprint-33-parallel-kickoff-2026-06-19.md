# Sprint 33 Parallel Kickoff

**Date:** 2026-06-19 (refreshed post-S32 closeout)  
**Trunk:** `main` @ `d3db76d` — **1073/1073**, ReplayGolden 6/6, QA APPROVED  
**Sprint plan:** `production/sprints/sprint-33-kill-chain-intelligence-comms-integration.md`  
**QA plan:** `production/qa/qa-plan-sprint-33-2026-11-27.md` (exists — lean gate)

## Wave plan (revised)

| Wave | Stories | Track | Est. | Notes |
|------|---------|-------|------|-------|
| Day-1 | S33-01 | DevOps baseline | 1d | **READY** — S32-13 blocker cleared |
| Wave 1a | S33-02, S33-04 | Data graph ∥ Sim share gate | 2.5d / 1.5d | Parallel after S33-01 |
| Wave 2 | S33-03, S33-07 | Data kill-chain rules ∥ Sim fixture | 2.5d / 1d | S33-07 after S33-04 |
| Wave 2b | S33-06 | Unity Phase G | 2d | After S33-03 + S33-04 (not Wave 1) |
| Wave 3 | S33-05, S33-08 | Data orchestrator ∥ Data CLI | 1.5d / 1d | After S33-03 |
| Wave 4 | S33-09, S33-10 | Sim regression (opt.) ∥ Unity evidence | 0.5d / 1.5d | S33-09 optional; drop before S33-07 |
| Wave 5 | S33-11 | QA C2 sign-off upgrade | 1d | Check 17 comms fittings |
| Closeout | S33-12, S33-13 | DevOps | 0.75d | ≥1086 closeout target |

## Track ownership

| Track | Team | Stories | Graphite prefix |
|-------|------|---------|-----------------|
| **Data** | team-data | S33-02, S33-03, S33-05, S33-08 | `stack/sprint33/kill-chain-intelligence` |
| **Simulation** | team-simulation | S33-04, S33-07, S33-09 | `stack/sprint33/cyber-comms-datalink` |
| **Unity** | team-unity | S33-06, S33-10 | `stack/sprint33/platform-phase-g` |
| **QA** | team-qa | S33-11 | `stack/sprint33/c2-signoff-upgrade` |
| **DevOps** | c-sharp-devops-engineer | S33-01, S33-12, S33-13 | `stack/sprint33/full-sln-gate`, `stack/sprint33/closeout` |

## Hard gates (every merge)

- `dotnet test ProjectAegis.sln` — ≥**1073** (closeout ≥1086)
- `ReplayGoldenSuiteTests` — 6/6 on sim/delegation merges
- ZERO touch `DelegationBridge.cs`
- `CatalogWriteGate` extend-only on data merges
- No full corpora in CI; `rg TlBranchDatabase|BranchDatabase` → zero
- Production Baltic world hash `17144800277401907079` unchanged unless isolated fixture

## Cut line

1. S33-12 (CI hygiene — 4th deferral OK)  
2. S33-09 (Phase 6 regression — optional 0.5d)  
3. S33-10 (live Editor — lean placeholders OK per S32)  
4. S33-08 (kill-chain CLI)  
5. S33-11 (C2 live upgrade)  

**Minimum shippable beyond must-have:** **S33-05** orchestrator gate + **S33-07** isolated fixture + **S33-13** closeout.

## S32 predecessor — resolved

| S32 item | Status at S33 kickoff |
|----------|----------------------|
| Manifest / quarantine / diff (02/03/07) | Done — no S33 preempt |
| Sim Phase 6 + BDA lifecycle (04–09) | Done — S33-09 optional regression only |
| Presentation + C2 (10/11) | Done — S33-10 extends S32-10 pattern |
| Closeout 1073/1073 | Done @ `d3db76d` |

## Dispatch order (recommended)

```bash
# Day-1
/dev-story dispatch S33-01

# Wave 1a (parallel)
/dev-story dispatch S33-02 S33-04

# Wave 2 (parallel)
/dev-story dispatch S33-03 S33-07

# Wave 2b (after sim+data gates)
/dev-story dispatch S33-06

# Wave 3 (parallel)
/dev-story dispatch S33-05 S33-08

# Wave 4 (parallel; S33-09 if capacity)
/dev-story dispatch S33-10
# /dev-story dispatch S33-09   # optional

# Wave 5 + closeout
/dev-story dispatch S33-11
/dev-story dispatch S33-13
# S33-12 if capacity
```

## Track plans

- Data: `production/agentic/sprint-33-plan-data-2026-06-19.md`
- Sim: `production/agentic/sprint-33-plan-sim-2026-06-19.md`
- Unity: `production/agentic/sprint-33-plan-unity-2026-06-19.md`
- DevOps/QA: `production/agentic/sprint-33-plan-devops-qa-2026-06-19.md`

*Planning complete — do not start implementation until S33-01 day-1 baseline is recorded.*