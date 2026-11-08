# UI Maturity Wave 4 Residual Parallel Kickoff — 2026-08-01

**Branch base:** `stack/ui-maturity/wave3-log08-campaign-layers-globe`  
**Integration:** `stack/ui-maturity/wave4-cesium-deck-boat-lod`  
**Stage:** Release. Zero `DelegationBridge.Tick` rewrite. CatalogWriteGate untouched. OrderKind **append-only**. No ion secrets.

## Lanes

| Lane | Scope | Surface | Forbidden |
|------|-------|---------|-----------|
| **C Cesium** | Tile streaming contract + ion visual gate (headless + Editor docs) | CesiumGlobe* additive, `GlobeTileStreaming*`, ion presence gate (no token values), checklist/runbook, tests | Boat FSM, Magazine UI, LOD clusterer |
| **M Magazine/Deck** | Deck/hangar capacity + magazine loadout UI depth | `MagazineLoadout*`, `DeckHangarCapacity*`, AirOps facility fields additive, panel host + UXML, tests | Boat FSM, Cesium package, LOD |
| **B BoatOps** | LOG-09…11 boat FSM + sea-state + embarked | `Sim/Logistics/BoatOps*`, SeaState scalar, embarked load, projection + BoatOps panel, OrderKind append if needed, tests | Cesium, Magazine panel ownership, LOD |
| **L LOD** | APP-6 LOD clustering toward 5k | `App6Lod*`, `MapSymbolCluster*`, MapPanel apply-state optional count, bench tests @ 5k, tests | Cesium ion, Boat, Magazine |

## Acceptance

1. **Cesium:** Headless tile-stream config + ion-token **presence** gate (missing → honest inactive; never stores secrets). Editor visual gate checklist updated with runnable steps. CesiumSpike path remains optional.
2. **Magazine/Deck:** Magazine rows (weapon, remaining, capacity %); deck/hangar capacity model (spots total/used/ready); Air Ops or dedicated panel shows loadout feasibility counts.
3. **Boat:** Pure FSM Stowed→…→Waterborne + recovery asymmetry; scenario sea-state scalar gates launch/recovery; embarked personnel/cargo capacity; `BOAT_NOT_READY` with named limit; UI rows.
4. **LOD:** Deterministic grid cluster of map symbols by zoom/altitude band; 5000 synthetic symbols cluster under budget; APP-6 glyph resolution still available for representatives.

## Merge order

B → M → L → C

---
*Kickoff UI Maturity Wave 4 residual.*
