# Sprint 117 — Land swarm pressure gauntlet

**Dates:** 2026-08-12  
**Program:** Release Product Progress 2 (S117–S121)  
**Predecessor:** S116 WatchAttention COMPLETE · UCA-P1 / DRG-148 COMPLETE  
**Stage:** **Release** · **Not Launch**  
**Linear epic:** [DRG-149](https://linear.app/drgamtd-workspace/issue/DRG-149)  
**Children:** [DRG-150](https://linear.app/drgamtd-workspace/issue/DRG-150) · [DRG-151](https://linear.app/drgamtd-workspace/issue/DRG-151) · [DRG-152](https://linear.app/drgamtd-workspace/issue/DRG-152)  
**Authority:** this plan + `production/agentic/agentic-workflow-sprint-series-2026-08-09.md` §2  
**QA:** `production/qa/swarm-pressure-gauntlet-2026-08-12.md`

## Goal

Land the already-designed swarm pressure suite on `main`: unit pressure tests, six saboteur mutants, `--swarm-filter` kill path, and four config-only `swarm_*` axes. No sim production behavior change.

## Tracks (file-disjoint)

| Track | Story | Surface |
|-------|-------|---------|
| A Tests | S117-a / DRG-150 | `src/ProjectAegis.Sim.Tests/Swarm/SwarmPressureTests.cs` |
| B Mutants | S117-b / DRG-151 | `tools/qa-gauntlet/mutants/**` |
| C Filter + axes + docs | S117-c / DRG-152 | `tools/qa-gauntlet/saboteur.py` · `production/qa/gauntlet/corpus/stress-axes.yaml` · this file |

## Must Have

| ID | AC |
|----|-----|
| S117-01 | Swarm filter ≥133 passed (124 prior + 12 pressure methods/theory cases) |
| S117-02 | 6 defect mutants catalogued; mutant 12 kills Extreme_attrition |
| S117-03 | `saboteur.py --swarm-filter` present |
| S117-04 | 4 `swarm_*` axes validate as config-only |

## Non-goals

Promote swarm axes off config-only · Phase N · DelegationBridge · CatalogWriteGate · Launch · S118 residuals

## Definition of Done

- [ ] Tracks merged to `main` with green CI
- [ ] ReplayGolden 6/6 / hash preserved / ZERO bridge
- [ ] Linear DRG-150/151/152 Done with PR link
- [ ] Evidence note under `production/qa/`
