---
id: S29-11
status: Complete
type: Integration
priority: nice-to-have
graphite_branch: stack/sprint29/datalink-side-picture
estimate_days: 1
dependencies:
  - S29-01 green baseline
owner: team-simulation
sprint: 29
req_trace: TR-sensor-004; Req 15 Sensor Detection & EW
---

# Story 029-11 — Datalink Side Picture (TR-sensor-004)

> **Epic:** sprint-29-combat-domains-phase3  
> **GDD:** `design/gdd/sensor-detection-ew.md` (TR-sensor-004)  
> **ADR:** ADR-001 (deterministic merge order)

## Summary

Bounded **contact sharing** via datalink with **deterministic merge order**. `ContactChange` order-log tests PASS; ReplayGolden 6/6. Closes TR-sensor-004 gap at P1 bounded scope — not full ECCM Phase 2.

## Acceptance Criteria

- [x] Observers on a side share contacts per scenario datalink doctrine (unless `organicOnly`)
- [x] Contact merge uses stable sorted order — deterministic across replays
- [x] Every shared contact transition emits `ContactChange` in order log
- [x] `ContactChange` order-log tests PASS
- [x] `ReplayGoldenSuiteTests` — 6/6 PASS
- [x] Bounded scope — no full datalink delay model or ECCM Phase 2
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Side contact sharing
  - Given: two observers on same side with overlapping sensor coverage
  - When: one observer detects contact not yet held by peer
  - Then: peer receives shared contact per datalink doctrine
  - Edge cases: `organicOnly` flag suppresses sharing; stale contact merge

- **AC-2**: Deterministic merge order
  - Given: multiple contacts detected same tick across observers
  - When: merge runs twice with identical seed
  - Then: identical `ContactChange` sequence and world hash
  - Edge cases: tie-break on `(observerId, sensorId, targetId)` sort; empty contact set

- **AC-3**: Order-log contract
  - Given: shared contact state transition (detected → classified)
  - When: order log replayed
  - Then: `ContactChange` rows present in documented sort order
  - Edge cases: contact lost after share; duplicate share suppressed

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Combat|Domain|Datalink|Contact" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `ContactChange` (order log) | HIGH — deterministic ordering |
| `SensorC2Bridge` | MEDIUM — read projection only |
| `DelegationBridge.cs` | ZERO touch |

## References

- GDD: `design/gdd/sensor-detection-ew.md` (TR-sensor-004)
- TR registry: `docs/architecture/tr-registry.yaml` (TR-sensor-004 gap)
- Kickoff: `production/sprints/sprint-29-operationalize-data-fight-loop.md` (S29-11)
- Track plan: `production/agentic/sprint-29-plan-data-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-29-2026-10-02.md` *(create before implementation)*