# UI Maturity Wave 3 Parallel Kickoff — 2026-08-01

**Branch base:** `stack/ui-maturity/wave2-cmd-24-27-33-36`  
**Integration:** `stack/ui-maturity/wave3-log08-campaign-layers-globe`  
**Stage:** Release. Zero `DelegationBridge.Tick` rewrite. CatalogWriteGate untouched. OrderKind **append-only**.

## Lanes

| Lane | Scope | Surface (allowed) | Forbidden |
|------|-------|-------------------|-----------|
| **A AirOpsN** | LOG-08 / CMD-24 Phase N | `Sim/Logistics/AirOps*`, extend `AirOpsEntry`/`Projection`/`ApplyState`/`AirOpsPanelHost`, OrderKind append Launch/AbortLaunch, C2CommandIssuance air ops ids, tests | Campaign*, MapLayer*, Cesium* |
| **C Campaign** | CMD-27.12 | `Data/Scenario/Campaign*`, ScenarioLibrary campaign rows, library apply-state, host campaign section, tests | AirOps FSM, Map layers, Cesium |
| **L Layers** | CMD-28 basemap + chrome polish | `Projection/MapLayer*`, `C2Menu*`, chrome collapse projection, MapPlaceholder layer HUD additive, tests | AirOps FSM, Campaign schema, Cesium package |
| **G Globe** | Product globe contracts (ADR-007 B) | `Projection/Globe*`, CesiumBillboard/theater jump extend, Unity Cesium host contracts (non-package code paths), scene builder CesiumSpike optional, tests | Campaign, AirOps FSM body, CatalogWriteGate |

## Acceptance

1. **LOG-08:** Pure deterministic AirOps FSM: OnGround→Prepping→Taxiing→TakingOff→Airborne; time-to-ready; Launch / AbortLaunch with refusal reasons; UI rows show phase + ETA.
2. **CMD-27.12:** Campaign is first-class JSON artifact (id, ordered scenario membership, completion); library lists campaigns separate from flat scenarios; sequence not filename-encoded.
3. **CMD-28:** Basemap layer stack model (UI-local) with toggle + persist bag; menu items expose shortcut labels; chrome collapse presentation.
4. **Globe:** Theater camera/bookmark + WGS84 marker projection product surface; headless tests; no ion token secrets committed.

## Merge order

A → C → L → G

---
*Kickoff UI Maturity Wave 3.*
