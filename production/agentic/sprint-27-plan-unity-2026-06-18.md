# Sprint 27 — Unity / Presentation Track Plan

**Date:** 2026-06-18  
**Kickoff:** `production/sprints/sprint-27-cmo-corpus-combat-bounded.md`  
**Maps to:** S27-07..08, S27-10..11, S27-15

## Goal

Promote ADR-011 Phase C from S26 browse spike to UXML/USS platform viewer with search/filter; wire live Addressables for `Map/App6FrameAtlas`; close S26 presentation deferrals.

## Stories

| Story | Priority | Days | Focus |
|-------|----------|------|-------|
| S27-07 | should-have | 1.0 | Addressables package + `Map/App6FrameAtlas` group |
| S27-08 | should-have | 2.0 | Platform catalog UXML/USS panel + search/filter (read-only) |
| S27-11 | should-have | 0.5 | PlayMode smoke harness / scene row |
| S27-10 | should-have | 0.5 | Editor PNG evidence (advisory) |
| S27-15 | nice-to-have | 0.5 | Row detail pane expansion |

## Rules (all stories)

- **ZERO touch** `DelegationBridge.cs`
- Headless proxy = merge authority; Editor = advisory sign-off
- `useGlobeMap=false` on default `DelegationSmoke` unchanged

## Producer cut

If capacity < 4d: drop S27-15 → S27-10 → S27-11 → narrow S27-08 to list-only → keep S27-07 minimum.

## Verify

```bash
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlayModeSmoke|PlatformCatalog|App6|Cesium|Doctrine" -v minimal
rg "useGlobeMap" unity/ProjectAegis/Assets/Scenes/DelegationSmoke.unity
git diff main -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
```

## Graphite branches

`stack/sprint27/addressables-app6-atlas` → `platform-viewer-panel` → `platform-viewer-harness` → `presentation-evidence`

*Condensed from parallel Unity planning agent — 2026-06-18.*