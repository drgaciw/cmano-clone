# Sprint 25 — Unity / Presentation Track Plan

**Date:** 2026-06-17  
**Kickoff:** `production/sprints/sprint-25-phase-b-damage-assurance.md`  
**Maps to:** S25-08..11, S25-14

## Goal

Close Sprint 24 QA conditions: Cesium Editor evidence, C2 tri-batch sign-off, S24-10 EMCON merge; advance APP-6 Phase C to atlas-backed glyphs.

## Stories

| Story | Priority | Days | Focus |
|-------|----------|------|-------|
| S25-08 | should-have | 1.5 | APP-6 atlas MVP (`App6Sidc` + USS/sprites) |
| S25-09 | should-have | 1.0 | Cesium Editor PNG evidence (S24-08 protocol) |
| S25-10 | should-have | 0.5 | Merge `doctrine-emcon-readonly` (S24-10) |
| S25-11 | should-have | 0.5 | `Invoke-C2PlayModeSignoffBatch.ps1` × 3 |
| S25-14 | nice-to-have | 1.0 | Cesium billboards + `App6Sidc` |

## Rules (all stories)

- **ZERO touch** `DelegationBridge.cs`
- Headless proxy = merge authority; Editor = sign-off for S25-09/11
- `useGlobeMap=false` on default `DelegationSmoke`

## Producer cut

If capacity &lt; 4d: drop S25-14 → narrow S25-08 → keep S25-09 + S25-10 + S25-11 minimum.

## Verify

```bash
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlayModeSmoke|Doctrine|MapPanelBinder|App6" -v minimal
pwsh tools/unity/Invoke-C2PlayModeSignoffBatch.ps1 -Scenario doctrine
```

## Graphite branches

`stack/sprint25/doctrine-emcon-readonly` → `app6-atlas-phase-c` → `cesium-editor-evidence` → `c2-editor-tri-batch`

*Condensed from parallel Unity planning agent — 2026-06-17.*