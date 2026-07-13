# Sprint 45 Parallel Kickoff — Performance Scale-Out (B4)

**Date:** 2026-06-20 (planning artifact)  
**Sprint plan:** `production/sprints/sprint-45-performance-scale-out.md`  
**Prereq:** B1 locked; S44 complete  

> **⛔ PLANNING ONLY**

## Sprint Goal

B4: Runtime/Sensors/Engage scale-out; profiling; determinism-engineer on all sim tracks.

## Wave plan

| Wave | Track | Est. | Agent env | Stories |
|------|-------|------|-----------|---------|
| W0 | Baseline + QA | 1.5d | Cloud | S45-01 |
| W1 | Runtime/Sensors | 4d | **Local lead** | S45-02 |
| W2 | Engage scale | 3.5d | Cloud + determinism | S45-03 |
| W3 | Perf profile | 2d | Cloud | S45-04 |
| parallel | Replay | ongoing | Cloud | S45-05 |
| W4 | Closeout | 0.5d | Local | S45-06 |

## Track ownership

| Track | Stack prefix | Agent env |
|-------|--------------|-----------|
| Runtime/Sensors | `stack/sprint45/runtime-sensors` | Local lead |
| Engage scale | `stack/sprint45/engage-scale` | Cloud |
| Perf profile | `stack/sprint45/perf-profile` | Cloud |
| Replay | `stack/sprint45/replay` | Cloud |
| Closeout | `stack/sprint45/closeout` | Local |

## File ownership matrix

| Path | Owner |
|------|-------|
| `src/ProjectAegis.Sim/**` (Runtime/Sensors) | Runtime/Sensors |
| `src/**/Engage*` | Engage scale |
| `production/perf/*` | Perf profile |

**GitNexus re-index milestone** at S45 closeout.

---

*Planning only.*
