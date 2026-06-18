---
id: S28-10
status: Not Started
type: Logic
priority: nice-to-have
graphite_branch: stack/sprint28/combat-phase2
estimate_days: 0.5
dependencies:
  - S28-01 green baseline
owner: team-simulation
sprint: 28
req_trace: S22-06 carryover; IBalanceTelemetrySink
---

# Story 028-10 — Balance Drift Telemetry Consumer

> **Epic:** sprint-28-combat-domains-phase2  
> **ADR:** ADR-001 (deterministic default path)

## Summary

Wire `enableBalanceDrift` advisory from S22-06 `IBalanceTelemetrySink` / `BalanceTelemetryAccumulator` into a sim consumer. Telemetry tests PASS; default `enableBalanceDrift=false`; golden hash pinned.

## Acceptance Criteria

- [ ] Sim consumer reads balance drift advisory when flag enabled
- [ ] Telemetry tests PASS
- [ ] `enableBalanceDrift` default **false**
- [ ] `ReplayGoldenSuiteTests` — 6/6 unchanged on default path
- [ ] No `IWriteGate` bypass
- [ ] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Advisory when flag on
  - Given: isolated fixture with `enableBalanceDrift=true`
  - When: sim tick with drift beyond ±8% threshold
  - Then: advisory emitted; no world-state mutation
  - Edge cases: exactly at threshold; zero trials

- **AC-2**: Default flag off
  - Given: Baltic fixture (default false)
  - When: replay golden runs
  - Then: 6/6 PASS; no telemetry side effects
  - Edge cases: accidental default flip

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Telemetry|Balance" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `BalanceTelemetryAccumulator` | MEDIUM |
| `DelegationBridge.cs` | ZERO touch |

## References

- S22-06 pattern: `production/sprint-status.yaml` (sprint22_stories 22-6)
- Kickoff: `production/sprints/sprint-28-corpus-write-combat-v2.md` (S28-10)
- QA plan: `production/qa/qa-plan-sprint-28-2026-09-18.md` *(create before implementation)*