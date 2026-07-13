---
id: S30-10
status: Complete
type: Integration
priority: nice-to-have
graphite_branch: stack/sprint30/datalink-lag
estimate_days: 1.5
dependencies:
  - S30-01 green baseline
owner: team-simulation
sprint: 30
req_trace: TR-sensor-004; Req 15 Sensor Detection & EW
---

# Story 030-10 — Datalink Share Lag (TR-sensor-004)

> **Epic:** sprint-30-combat-domains-phase4  
> **GDD:** `design/gdd/sensor-detection-ew.md` (TR-sensor-004)  
> **ADR:** ADR-001 (deterministic merge order)  
> **QA Classification:** Integration + Logic

## Summary

Bounded **TR-sensor-004 extension**: add scenario **`datalink.shareLagTicks`** delay before shared contacts merge into peer side picture. Preserves S29-11 **deterministic merge order** (`observerId`, `sensorId`, `targetId`). Isolated fixture `baltic-patrol-datalink-lag`; ReplayGolden 6/6 on default path. Not full ECCM Phase 2 or arbitrary delay model.

## Acceptance Criteria

- [x] Scenario JSON supports `datalink.shareLagTicks` (non-negative integer; default 0 = S29-11 behavior)
- [x] Shared contact transitions deferred by `shareLagTicks` before peer merge
- [x] Contact merge retains stable sorted order — deterministic across replays
- [x] Deferred transitions still emit `ContactChange` in order log at apply tick
- [x] `Datalink` tests PASS (`Combat|Domain|Datalink|Contact` filters)
- [x] Isolated fixture `baltic-patrol-datalink-lag` demonstrates lag > 0 path
- [x] `ReplayGoldenSuiteTests` — 6/6 PASS on default path (production pins unchanged)
- [x] Bounded scope — no full datalink ECCM Phase 2 or wall-clock delay
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Share lag defers peer merge
  - Given: two observers on same side; `datalink.shareLagTicks = N` (N > 0)
  - When: observer A detects contact at tick T
  - Then: peer B receives shared contact at tick T + N (not before)
  - Edge cases: `shareLagTicks = 0` matches S29-11; lag exceeds scenario length; contact lost before lag elapses

- **AC-2**: Deterministic merge order preserved
  - Given: multiple contacts eligible for share after lag window
  - When: merge runs twice with identical seed
  - Then: identical `ContactChange` sequence and world hash; sort keys unchanged
  - Edge cases: tie-break on `(observerId, sensorId, targetId)`; empty contact set after lag filter

- **AC-3**: Default path replay unchanged
  - Given: production Baltic fixtures without `shareLagTicks` override (default 0)
  - When: ReplayGolden 6/6 suite runs
  - Then: 6/6 PASS; isolated `baltic-patrol-datalink-lag` pin not in golden catalog
  - Edge cases: accidental lag on production policy; `organicOnly` still suppresses sharing

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Combat|Domain|Datalink|Contact" -v minimal
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj \
  --filter "ContactChange" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `DatalinkSidePictureMerger` | HIGH — extend with lag queue |
| `ScenarioDatalinkDoctrine` | HIGH — `shareLagTicks` field |
| `ContactChange` (order log) | HIGH — deterministic ordering |
| `BalticReplayHarness` | MEDIUM — wiring only; ZERO bridge touch |
| `DelegationBridge.cs` | ZERO touch |

## References

- GDD: `design/gdd/sensor-detection-ew.md` (TR-sensor-004)
- TR registry: `docs/architecture/tr-registry.yaml` (TR-sensor-004 gap)
- S29-11 pattern: `production/epics/sprint-29-combat-domains-phase3/story-029-11-datalink-side-picture.md`
- S29-11 evidence: `production/agentic/stacks/sprint29/S29-11-DONE.md`
- Isolated fixture (S29): `data/scenarios/baltic-patrol-datalink.policy.json`
- New fixture: `data/scenarios/baltic-patrol-datalink-lag.policy.json`
- Kickoff: `production/sprints/sprint-30-tl-bind-corpus-scale.md` (S30-10)
- Track plan: `production/agentic/sprint-30-plan-sim-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-30-2026-10-16.md`