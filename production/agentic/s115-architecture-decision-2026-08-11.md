# Architecture decision — S115 theme (2026-08-11)

**Decider:** Application architecture / engineering (delegated)  
**Context:** S110–S114 Release Product Progress engineering complete; S114 human ack still open (non-blocking for next Release train).

## Options considered

| Option | Verdict |
|--------|--------|
| Hold until S114 ack only | Rejected as sole action — ack is human paperwork; engineering capacity free |
| Asset wave 4 (009/010/013) | Viable but lower product leverage than playable time-model gap |
| Gauntlet DRG-62/64/65 | Hygiene only — not a product sprint |
| Phase N / Launch | Out of policy |
| **Attention + auto-pause spine (P0-6/P0-7 min)** | **Selected** |

## Rationale

1. S112 left **pause without reason** and no **watch-officer queue** — MVP PRD still open.  
2. Headless projection + clock wire maximizes testability and avoids Unity Editor dependency.  
3. Surfaces can stay disjoint from CRITICAL hubs if events are pure records and Bridge is left alone.  
4. Asset wave 4 remains a **parallel micro-follow-up** if capacity appears mid-sprint; not the title track.

## Namespace rule

`Delegation/Attention/*` = AI cognitive load (**do not extend for player watch queue**).  
New: `WatchAttentionEvent`, `WatchAttentionQueueProjection`, pause-reason codes under Watch/Projection.
