# QA Plan — S116 WatchAttention emit call-sites

| TC | Check |
|----|-------|
| TC-C1 | Unknown→Detected hostile emits HostileOrUnknownContact with stable EventId |
| TC-C2 | Second transition on same target does not produce a new event (idempotent id) |
| TC-C3 | Own-side (blue/u1) first-detect does **not** emit HostileOrUnknownContact |
| TC-L1 | Own-side loss emits OwnSideLossOrDamage with `watch:loss:{id}` |
| TC-L2 | Hostile loss does **not** emit OwnSideLossOrDamage |
| TC-S1 | Session.ReportContactTransitions enqueues + auto-pauses on first hostile |
| TC-S2 | BDA MarkLost path on own-side unit reports loss event |
| TC-G1 | No Bridge edit; ReplayGolden expected 6/6 |

Sign-off: `production/qa/smoke-sprint-116-*.md`
