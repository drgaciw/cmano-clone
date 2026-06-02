# Cesium for Unity — package pin (ADR-007 Phase B)

**Unity:** 6000.3.x LTS (`unity/ProjectAegis`)  
**ADR:** [adr-007-c2-map-presentation.md](../architecture/adr-007-c2-map-presentation.md)  
**Spike checklist:** [cesium-phase-b-spike-checklist.md](cesium-phase-b-spike-checklist.md)

## Recommended pin (verify before install)

| Package | Source | Notes |
|---------|--------|-------|
| `com.cesium.unity` | [Cesium for Unity releases](https://github.com/CesiumGS/cesium-unity/releases) | Match Unity 6 / 6000.x compatibility matrix on release page |
| Cesium ion token | User secret / CI var `CESIUM_ION_TOKEN` | Never commit |

## Install steps (Editor)

1. Open **Window → Package Manager → + → Add package from git URL**  
   Use the git URL documented on the release matching Unity 6.3.
2. **Cesium → Cesium for Unity** — connect ion account.
3. Create `Scenes/CesiumSpike.unity` (do not replace `DelegationSmoke` default).
4. Feature flag: `DelegationBridgeHost.useGlobeMap` (bool, default `false`) when wiring spike.

## Integration contract

- Globe picks forward `C2PresentationController` selection APIs (same as Toolkit map).
- Symbol positions fed from `MapPictureProjection` until sim publishes lat/lon.
- Phase A `MapPlaceholderPanelHost` remains default for MVP vertical slice gate.

## Rollback

Remove package line from `manifest.json` and delete `Scenes/CesiumSpike.unity`; no change to headless `dotnet test` gate.