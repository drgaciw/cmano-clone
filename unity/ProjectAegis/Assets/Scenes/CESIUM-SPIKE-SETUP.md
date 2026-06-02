# Cesium spike scene setup (ADR-007 Phase B)

**Do not replace** the `DelegationSmoke` Play Mode stack. This scene validates globe-only risk.

## Steps

1. Install package per `docs/engineering/cesium-unity-package-pin.md`.
2. **File → New Scene** → save as `Assets/Scenes/CesiumSpike.unity`.
3. Add Cesium georeference + globe camera (follow Cesium for Unity quickstart).
4. Optional: duplicate `DelegationBridgeHost` with `useGlobeMap = true` for future wiring.
5. Run checklist: `docs/engineering/cesium-phase-b-spike-checklist.md`.

## Rollback

Delete this scene and remove `com.cesium.unity` from `Packages/manifest.json`. Headless CI unchanged.