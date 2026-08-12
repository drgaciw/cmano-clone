# QA Plan — S115 Attention + Auto-Pause Spine

| TC | Check |
|----|-------|
| TC-E1 | Hostile/unknown first-contact emits pause-class event with stable ID |
| TC-E2 | Own-side loss/damage emits pause-class event |
| TC-P1 | Event causes `IsPaused` true (non-HeadlessBatch) |
| TC-P2 | HeadlessBatch still advances while logically paused |
| TC-Q1 | WatchAttentionQueueProjection order: priority, tick, event ID |
| TC-Q2 | Acknowledge/dismiss do not mutate sim policy; restorable |
| TC-R1 | Resume with zero unresolved cards succeeds |
| TC-R2 | Resume with unresolved cards fails unless explicit override |
| TC-G1 | ReplayGoldenSuite 6/6 |
| TC-AP | No asset marked Approved; stage Release |

Sign-off: `production/qa/smoke-sprint-115-*.md`
