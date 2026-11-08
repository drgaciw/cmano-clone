# S29-11 story-done — Datalink Side Picture (TR-sensor-004)

**Story:** `production/epics/sprint-29-combat-domains-phase3/story-029-11-datalink-side-picture.md`  
**Status:** Complete  
**Completed:** 2026-06-18

## Verdict: COMPLETE

| AC | Test / Evidence | Status |
|----|-----------------|--------|
| Observers on same side share contacts per datalink doctrine | `DatalinkSidePictureMergerTests.Peer_on_same_side_receives_shared_detected_transition` | COVERED |
| `organicOnly` suppresses sharing | `DatalinkSidePictureMergerTests.Organic_only_flag_suppresses_sharing` | COVERED |
| Deterministic merge order (stable sort keys) | `DatalinkSidePictureMergerTests.Identical_merge_inputs_yield_identical_shared_sequence`, `Shared_transitions_sort_by_observer_sensor_target` | COVERED |
| Shared transitions emit `ContactChange` in order log | `ContactChangeOrderLogTests`, `ContactPictureProjectionTests`, `BalticReplayHarness` wiring | COVERED |
| `ContactChange` order-log tests PASS | 3/3 PASS | COVERED |
| `ReplayGoldenSuiteTests` — 6/6 PASS | unchanged production Baltic pins | COVERED |
| Bounded scope — no delay/ECCM Phase 2 | merge-only post-processor; default doctrine organic-only | COVERED |
| ZERO touch `DelegationBridge.cs` | empty diff | COVERED |

## Implementation summary

- `ScenarioDatalinkDoctrine` — scenario JSON `datalink.organicOnly` + `datalink.unitSides`
- `DatalinkSidePictureMerger` — deterministic side-picture merge sorted by `(observerId, sensorId, targetId)`
- `BalticReplayHarness` — appends shared transitions after organic Pd detection (no bridge change)
- Fixture: `data/scenarios/baltic-patrol-datalink.policy.json` (isolated; not in ReplayGolden catalog)

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj --filter "Combat|Domain|Datalink|Contact" -v minimal
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj --filter "ContactChange" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests --filter "ReplayGoldenSuiteTests" -v minimal
git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
```

## Test counts (2026-06-18)

| Suite | Filter | Result |
|-------|--------|--------|
| `ProjectAegis.Sim.Tests` | `Combat\|Domain\|Datalink\|Contact` | **46/46 PASS** |
| `ProjectAegis.Delegation.Tests` | `ContactChange` | **3/3 PASS** |
| `ProjectAegis.Delegation.Tests` | `ContactPicture` | **5/5 PASS** |
| `ProjectAegis.Delegation.UnityAdapter.Tests` | `ReplayGoldenSuiteTests` | **6/6 PASS** |
| `ProjectAegis.Sim.Tests` | `Datalink` | **8/8 PASS** |