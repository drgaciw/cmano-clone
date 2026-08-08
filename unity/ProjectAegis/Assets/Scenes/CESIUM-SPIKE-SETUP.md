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

## Wave 4 ion visual gate

1. Set token via env `CESIUM_ION_TOKEN` / `ProjectAegis_CesiumIonToken` or Inspector on `CesiumGlobeHost` — **NEVER commit** the token.
2. Build scene: **Project Aegis → Build CesiumSpike Scene** (adds `GlobeTileStreamingHost`; does not write token).
3. Play Mode: status `GLOBE TILES · ACTIVE` with token+package, else honest `INACTIVE · NO_ION_TOKEN` / `PACKAGE_MISSING`.
4. Full runbook + screenshot path: `docs/engineering/cesium-ion-visual-gate-2026-08-01.md`.

