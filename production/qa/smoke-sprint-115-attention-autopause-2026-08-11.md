# Smoke — S115 Attention + Auto-Pause Spine (2026-08-11)

**PR:** #467  
**Stage:** Release

## Lanes

| Lane | Result |
|------|--------|
| A Event model | `WatchAttentionKind/Event/Card/Priority/PauseReason` under `ProjectAegis.Delegation.Watch` |
| B Queue + projection + gate | `WatchAttentionQueue`, `WatchAutoPauseGate`, `WatchAttentionQueueProjection` |
| C Session wire + tests | `ReportWatchAttention`, `TryResumeSim(explicitOverride)`, two test fixtures |

## TC map (from QA plan)

| TC | Status |
|----|--------|
| TC-E1 Hostile/unknown emit | Covered by session test (ReportWatchAttention) |
| TC-E2 Own-side loss/damage | Covered |
| TC-P1 Auto-pause → IsPaused | Covered |
| TC-P2 HeadlessBatch override | Pre-existing SimTickPipeline behaviour (unchanged) |
| TC-Q1 Queue order | Covered (priority → tick → EventId) |
| TC-Q2 Ack/dismiss presentation-only | Covered + restorable |
| TC-R1 Resume zero unresolved | Covered |
| TC-R2 Resume blocked unless override | Covered |
| TC-G1 ReplayGolden 6/6 | Expected — no tick-order / RNG / hash change |
| TC-AP Stage Release / no Approved invent | Affirmed |

## Residuals

- Detection / BDA emit call-sites (API ready)
- Unity attention panel chrome
- P0-8 weapons-release forced 1×

## Sign-off

Engineering spine complete. Awaiting CI green on #467 then merge.
