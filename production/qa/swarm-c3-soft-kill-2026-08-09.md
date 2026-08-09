# SWARM-C3 / DRG-107 — EMP / jam soft-kill effects

**Date:** 2026-08-09  
**Surface:** `src/ProjectAegis.Sim/Swarm/SoftKill/**`, `src/ProjectAegis.Sim.Tests/Swarm/SoftKill/**`  
**Requirement:** SWARM-18  
**Controller:** Prefer **no** `SwarmController` edits — external `SwarmSoftKillApplicator` uses public APIs (`SetLinkState`, `IssueMode`, `RefreshLinkState`, `GetMode`, `GetLinkState`).

## ACs

| AC | Evidence |
|----|----------|
| EMP freezes mode switches for N sim-seconds | `SwarmEmpEvaluator.ComputeFreezeUntil` + applicator `_modeFreezeUntilByUnit` |
| EMP blocks subsequent mode change until expiry | `Emp_freezes_mode_switches_until_expiry`, `Emp_blocks_subsequent_mode_change_until_expiry_event_log` |
| EMP optionally recommends Scatter via IssueMode | `Emp_recommend_scatter_logs_explicit_reason` |
| Jam → Degraded linkState | `Jam_sets_degraded_link_state` → `SetLinkState(Degraded)` |
| Jam high severity → Lost | `Jam_sets_lost_at_higher_severity` |
| Explicit reason strings on event log | `SwarmSoftKillEvent.Reason`; constants on evaluators |
| Recovery after clear (jam / EMP) | `Jam_recovery_after_clear_restores_connected`, `Emp_clear_allows_mode_change_before_natural_expiry` |
| Deterministic pure evaluators | `Emp_evaluator_*`, `Jam_evaluator_*`, `Same_seed_path_is_deterministic` |
| No Formation/Assault/CEC rewrite | SoftKill-only surface under `Swarm/SoftKill/**` |

## Design notes

- **EMP:** freeze-until simTime tracked on the applicator (C2 soft-kill layer). `TryIssueMode` refuses mode changes while `simTime < freezeUntil` (exclusive). Optional Scatter recommendation is a single `IssueMode` at apply time when link is not Lost.
- **Jam:** pure severity → `SwarmLinkState` map; applied via `SetLinkState`. Clear restores Connected or re-evaluates via `RefreshLinkState`.
- **Mode freeze enforcement:** at applicator boundary (not inside `SwarmController.IssueMode`). Callers that soft-kill must route mode orders through `TryIssueMode`. No controller patch for this slice.
- CEC mesh membership is **out of scope** for C3 (jam here is C2 `linkState` only; SWARM-31 remains independent).

## Tests

```bash
export PATH="/root/.dotnet:$PATH"
export VSTEST_CONNECTION_TIMEOUT=300
cd /workspace/artifacts/cmano-clone/.worktrees/drg-107-c3
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "FullyQualifiedName~SoftKill|FullyQualifiedName~Emp|FullyQualifiedName~SwarmMode"
```

Filter matches SoftKill suite + prior `SwarmModeHostLinkTests`.
