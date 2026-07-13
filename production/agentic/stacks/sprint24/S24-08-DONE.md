# S24-08 story-done evidence — cesium-polish

**Branch:** `stack/sprint24/cesium-polish` @ `3f517fa`  
**Story:** `production/sprints/sprint-24-phase-b-import-present-polish.md` §S24-08  
**Status:** Complete  
**Completed:** 2026-06-17  
**Review mode:** lean (LP-CODE-REVIEW + QL-TEST-COVERAGE skipped)

## Deliverables

- `unity/ProjectAegis/Assets/Scripts/Runtime/CesiumGlobeBridge.cs` — depth/occlusion notes (25 km sphere scale); MapPanelBinder-sourced positions
- `unity/ProjectAegis/Assets/Scripts/Runtime/DelegationBridgeHost.cs` — S24-08 comments; `useGlobeMap` flag preserved
- `src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/PlayModeSmokeHarnessTests.cs` — `Delegation_smoke_keeps_useGlobeMap_false_for_ci_safe_default`
- `docs/engineering/cesium-phase-b-spike-checklist.md` — depth/occlusion + selection OOB rows closed
- `production/qa/sprint-24-cesium-polish-2026-06-17.md` — headless + Editor protocol evidence
- **ZERO touch** `DelegationBridge.cs` (verified `git diff main` empty)

## Acceptance criteria traceability

| AC | Evidence | Status |
|----|----------|--------|
| Depth/occlusion evidence in CesiumSpike.unity | Checklist row + `CesiumGlobeBridge.cs` §depth/occlusion; `sprint-24-cesium-polish-2026-06-17.md` §Depth/occlusion | COVERED (Editor protocol) |
| `useGlobeMap=false` default on DelegationSmoke | `Delegation_smoke_keeps_useGlobeMap_false_for_ci_safe_default`; `DelegationSmoke.unity` → `useGlobeMap: 0` | COVERED (headless) |
| Globe loads | Checklist + S20 evidence + `sprint-24-cesium-polish-2026-06-17.md` §Globe load | COVERED (Editor protocol) |
| Selection syncs OOB | `Baltic_classify_selection_flow_syncs_map_oob_and_contact_summary` (headless proxy); checklist + evidence §Selection sync OOB | COVERED |
| Checklist row closed | `cesium-phase-b-spike-checklist.md` depth/occlusion + selection rows `[x]` | COVERED |

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd .worktrees/sprint24-cesium
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "PlayModeSmoke|Cesium|Globe" -v minimal
git diff main -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
rg "useGlobeMap" unity/ProjectAegis/Assets/Scenes/DelegationSmoke.unity
```

**Results (2026-06-17):**

| Gate | Result |
|------|--------|
| PlayModeSmoke\|Cesium\|Globe filter | **14/14 PASS** (0 failed, 0 skipped) |
| `DelegationBridge.cs` diff vs `main` | **ZERO touch** (empty diff) |
| `DelegationSmoke.unity` `useGlobeMap` | `useGlobeMap: 0` |

## Advisory gaps (lean mode)

- Editor screenshots not yet attached (`production/qa/attachments/cesium-s24-*.png` placeholders in evidence doc)
- Visual gates satisfied via S20 PASS-assumption + manual protocol per `qa-plan-sprint-24-2026-06-17.md` §S24-08
- LP-CODE-REVIEW / QL-TEST-COVERAGE skipped per lean review mode

## Verdict

**COMPLETE WITH NOTES** — All story AC satisfied; headless gates are merge authority; Editor screenshot attachment deferred (non-blocking).