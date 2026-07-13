# S24-08 progress — cesium-polish

**Branch:** `stack/sprint24/cesium-polish`  
**Story:** `production/sprints/sprint-24-phase-b-import-present-polish.md` §S24-08  
**Status:** in-progress (headless AC complete; Editor evidence protocol filed)

## Completed

- Headless regression: `Delegation_smoke_keeps_useGlobeMap_false_for_ci_safe_default` in `PlayModeSmokeHarnessTests`
- Checklist: depth/occlusion + selection OOB rows updated in `docs/engineering/cesium-phase-b-spike-checklist.md`
- QA evidence: `production/qa/sprint-24-cesium-polish-2026-06-17.md` (manual Editor steps + checklist status)
- Verified `DelegationSmoke.unity` → `useGlobeMap: 0`; scene builder uses `MapPlaceholderPanelHost`
- ZERO touch `DelegationBridge.cs`

## Evidence path

`production/qa/sprint-24-cesium-polish-2026-06-17.md`

## Remaining (human / Editor)

- Attach screenshots to §Globe load, §Depth/occlusion, §Selection sync OOB when Unity Editor available
- Optional: run CesiumSpike Play Mode and paste console excerpt into evidence doc

## Gates

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "PlayModeSmoke" -v minimal
rg "useGlobeMap" unity/ProjectAegis/Assets/Scenes/DelegationSmoke.unity
```