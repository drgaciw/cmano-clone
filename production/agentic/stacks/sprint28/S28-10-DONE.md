# S28-10 story-done evidence — Balance Drift Telemetry Consumer

**Story:** `production/epics/sprint-28-combat-domains-phase2/story-028-10-balance-drift-consumer.md`  
**Status:** Complete  
**Trunk evidence:** `main` @ `d210d3d`  
**Completed:** 2026-06-18

## Deliverables

- `BalanceDriftAdvisoryConsumer` — advisory-only sim consumer over `IBalanceTelemetrySink` / `BalanceTelemetryAccumulator`
- `ScenarioBalanceTelemetrySettings` + `ScenarioBalanceTrial` — scenario policy surface with `enableBalanceDrift` default **false**
- `ScenarioPolicyJsonDto.telemetry` + loader parse path
- `SimulationSession` wires consumer from `orchestrator.ScenarioPolicy.BalanceTelemetry`; records engagement outcomes (no world mutation)
- Test-only fixture `data/scenarios/baltic-patrol-balance-drift.policy.json` (not in ReplayGolden 6/6)
- Tests: `BalanceDriftAdvisoryConsumerTests`, `ScenarioPolicyBalanceTelemetryJsonTests`
- **ZERO** touch `DelegationBridge.cs`

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone

dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Telemetry|Balance" -v minimal
# Passed: 7/7

dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "Telemetry|Balance" -v minimal
# Passed: 10/10

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
# Passed: 6/6 — Baltic production fixtures unchanged (telemetry default off)

git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
# (empty)
```

## Acceptance criteria traceability

| AC | Evidence | Status |
|----|----------|--------|
| Sim consumer reads balance drift advisory when flag enabled | `BalanceDriftAdvisoryConsumer` + `Balance_enabled_consumer_emits_drift_advisory_beyond_eight_percent` | **PASS** |
| Telemetry tests PASS | Sim **7/7** + Data **10/10** | **PASS** |
| `enableBalanceDrift` default **false** | `ScenarioBalanceTelemetrySettings.Disabled` + `Balance_telemetry_defaults_to_disabled_when_omitted` + factory tests | **PASS** |
| ReplayGoldenSuiteTests 6/6 | 6/6 PASS | **PASS** |
| No `IWriteGate` bypass | Advisory-only sink; existing `Telemetry_does_not_mutate_write_gate_state` | **PASS** |
| ZERO touch `DelegationBridge.cs` | Empty diff vs HEAD | **PASS** |

## Per-project counts (story filters)

| Project | Filter | Passed |
|---------|--------|--------|
| ProjectAegis.Sim.Tests | Telemetry\|Balance | 7 |
| ProjectAegis.Data.Tests | Telemetry\|Balance | 10 |
| ProjectAegis.Delegation.UnityAdapter.Tests | ReplayGoldenSuiteTests | 6 |

## Verdict

**COMPLETE** — S22-06 balance drift advisory wired into sim session path; default off; replay 6/6 unchanged; no write-gate bypass.