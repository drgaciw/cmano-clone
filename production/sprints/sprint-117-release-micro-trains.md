# Sprint 117 — Release micro-trains (harness emit · asset wave 4 · P0-8 spine)

**Dates:** 2026-08-11  
**Stage:** **Release** · **Not Launch**  
**Authority:** S116 residual · asset-manifest wave 4 · PRD P0-8 minimal  

## Tracks (surface-disjoint)

| Track | Theme | Surface |
|-------|-------|--------|
| A | Harness watch emit wire | `BalticReplayHarness` → `session.ReportContactTransitions` |
| B | Asset wave 4 | ASSET-009/010/013 Specced→**Done** USS |
| C | P0-8 forced 1× spine | `SimClock` + `SimulationSession` weapons-release 1× |

## Must Have

| ID | AC |
|----|-----|
| S117-01 | After contact transitions, harness calls `ReportContactTransitions` when Session non-null |
| S117-02 | ASSET-009/010/013 files on disk under `production/assets/c2/` + manifest Done |
| S117-03 | `ForceRealTimeForWeaponsRelease` sets factor 1; player accel >1 blocked while forced |
| S117-04 | Engagement **Launched** path forces 1× |
| S117-05 | Tests green; ZERO Bridge hotpath; stage Release |

## Non-goals / residual

- Full P0-8 DLZ countdown card, approve/deny OrderKinds, hold-fire AI
- Asset **Approved** (human phrase only)
- Unity chrome attention panel
- Phase N / Launch
