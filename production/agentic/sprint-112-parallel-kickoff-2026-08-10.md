# S112 parallel kickoff (2026-08-10)

**Skill:** dispatching-parallel-agents  
**Workflow:** agentic-workflow-sprint-series-2026-08-09.md

| Lane | Surface | Must not |
|------|---------|----------|
| A | Sim Time + Core tick runner/pipeline | SimulationSession, DetectionTrialResolver |
| B | SimulationSession + Delegation.Tests Orchestration | SimTickRunner body (only consume public Clock API) |
| C | DetectionTrialResolver modality map (optional) | SimClock |

Merge order: A first if B needs new Clock API; if B only uses Pause/Resume already on Clock after A lands, stack B on main after A. Prefer B starts after A merges OR B implements against planned API and rebases.

**Orchestrator note:** A and B share conceptual Clock API — **serial merge A→B** if conflict; dispatch in parallel with B depending on documented API contract below.

### Clock API contract (for parallel B)
```
SimClock.IsPaused : bool
SimClock.Pause() / Resume()
SimClock.AccelerationFactor : int (default 1, clamp 1..256)
SimClock.SetAccelerationFactor(int)
ISimTickRunner.TickOnce(TimeCompressionMode):
  if IsPaused && mode != HeadlessBatch → no-op
  steps = mode==Accelerated ? AccelerationFactor : 1
  for i in steps: advance one tick + subsystem work
```
