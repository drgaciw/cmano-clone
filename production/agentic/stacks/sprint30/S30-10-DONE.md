# S30-10 story-done — Datalink Share Lag (TR-sensor-004)

**Story:** `production/epics/sprint-30-combat-domains-phase4/story-030-10-datalink-lag.md`  
**Status:** Complete  
**Completed:** 2026-06-18  
**Branch:** `stack/sprint30/datalink-lag`  
**Builds on:** S29-11 `DatalinkSidePictureMerger` deterministic merge

## Verdict: COMPLETE

| AC | Test / Evidence | Status |
|----|-----------------|--------|
| Scenario JSON supports `datalink.shareLagTicks` (default 0) | `ScenarioPolicyJsonLoaderTests.Loads_baltic_patrol_datalink_lag_fixture_with_shareLagTicks` | COVERED |
| Shared contact transitions deferred by `shareLagTicks` | `DatalinkSidePictureMergerTests.Share_lag_defers_peer_merge_until_apply_tick` | COVERED |
| Deterministic merge order preserved | `Share_lag_deterministic_merge_order_preserved_after_deferral`, `Identical_merge_inputs_yield_identical_shared_sequence` | COVERED |
| Deferred transitions emit `ContactChange` at apply tick | `BalticReplayHarnessDatalinkLagTests.Lag_fixture_defers_datalink_contact_change_until_after_detection_tick` | COVERED |
| `Combat\|Domain\|Datalink\|Contact` tests PASS | 58/58 PASS | COVERED |
| Isolated fixture `baltic-patrol-datalink-lag` | `data/scenarios/baltic-patrol-datalink-lag.policy.json` | COVERED |
| `ReplayGoldenSuiteTests` — 6/6 default path | unchanged production Baltic pins | COVERED |
| Bounded scope — no ECCM Phase 2 | tick-based lag queue only; default `shareLagTicks=0` = S29-11 | COVERED |
| ZERO touch `DelegationBridge.cs` | empty diff | COVERED |

## QA test cases

| Case | Test | Status |
|------|------|--------|
| AC-1 Share lag defers peer merge | `Share_lag_defers_peer_merge_until_apply_tick` | COVERED |
| AC-1 `shareLagTicks = 0` matches S29-11 | `Share_lag_zero_matches_immediate_peer_merge` | COVERED |
| AC-1 lag exceeds scenario length | `Share_lag_exceeding_scenario_length_never_shares` | COVERED |
| AC-1 contact lost before lag elapses | `Share_lag_cancels_pending_share_when_contact_lost_before_apply_tick` | COVERED |
| AC-2 deterministic merge order | `Share_lag_deterministic_merge_order_preserved_after_deferral` | COVERED |
| AC-3 default path replay unchanged | `ReplayGoldenSuiteTests` 6/6; lag fixture not in catalog | COVERED |

## Implementation summary

- `ScenarioDatalinkDoctrine.ShareLagTicks` — scenario JSON `datalink.shareLagTicks` (non-negative; default 0)
- `DatalinkSidePictureMerger` — pending shareable organic queue; flush at `detectTick + shareLagTicks`; cancel pending on Lost
- Deterministic flush order: `(applyTick, observerId, targetId, lifecycleRank)`
- Merge sort keys unchanged: `(observerId, sensorId, targetId)`
- Fixture: `data/scenarios/baltic-patrol-datalink-lag.policy.json` (`shareLagTicks: 2`; isolated from ReplayGolden catalog)

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Combat|Domain|Datalink|Contact" -v minimal
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj \
  --filter "ContactChange" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
```

## Test counts (2026-06-18)

| Suite | Filter | Result |
|-------|--------|--------|
| `ProjectAegis.Sim.Tests` | `Combat\|Domain\|Datalink\|Contact` | **58/58 PASS** (+12 vs S29-11 46/46) |
| `ProjectAegis.Sim.Tests` | `Datalink` | **14/14 PASS** (+6 vs S29-11 8/8) |
| `ProjectAegis.Delegation.Tests` | `ContactChange` | **3/3 PASS** |
| `ProjectAegis.Delegation.UnityAdapter.Tests` | `DatalinkLag` | **4/4 PASS** |
| `ProjectAegis.Delegation.UnityAdapter.Tests` | `ReplayGoldenSuiteTests` | **6/6 PASS** |