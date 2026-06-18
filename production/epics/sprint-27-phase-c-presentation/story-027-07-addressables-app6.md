---
id: S27-07
status: Ready
type: Integration
priority: should-have
graphite_branch: stack/sprint27/addressables-app6-atlas
estimate_days: 1
dependencies:
  - S27-01 green baseline
owner: team-unity
sprint: 27
req_trace: ADR-007 Phase C; Req 20
---

# Story 027-07 — Addressables + Map/App6FrameAtlas

> **Epic:** sprint-27-phase-c-presentation  
> **ADR:** ADR-007, ADR-010

## Summary

Add `com.unity.addressables` to manifest; promote `App6AtlasAddressablesManifest.json` to live Addressables group; `MapPlaceholderPanelHost` loads atlas when available, degrades to Unicode when unavailable.

## Acceptance Criteria

- [ ] `com.unity.addressables` in `Packages/manifest.json`
- [ ] Key `Map/App6FrameAtlas` resolves to sprite sheet metadata
- [ ] Headless App6 + MapPanel tests PASS
- [ ] Unicode fallback when `App6AtlasCatalog.Unavailable`
- [ ] ZERO touch `DelegationBridge.cs`
- [ ] `useGlobeMap=false` on `DelegationSmoke.unity` unchanged

## QA Test Cases

- **AC-1**: Atlas resolve
  - Given: Addressables group configured
  - When: map panel requests atlas frame
  - Then: sprite metadata returned OR unicode fallback
  - Edge cases: missing addressable key

## Verify Commands

```bash
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "App6|MapPanelBinder" -v minimal
git diff main -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
```

## References

- S26-06: `production/qa/sprint-26-app6-atlas-2026-06-18.md`
- QA plan: `production/qa/qa-plan-sprint-27-2026-06-18.md`