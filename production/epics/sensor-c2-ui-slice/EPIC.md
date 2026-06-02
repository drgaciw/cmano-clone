# Epic: Sensor C2 UI Slice (Sprint 2)

> **Status:** Complete (headless view model + Unity OnGUI host)  
> **Layer:** Presentation (Unity) + Projection (Delegation)

## Goal

Minimal sensor C2 HUD: contact list with lifecycle state, EMCON active/off, fire-control track indicator — driven from order log + sim snapshot (no UI state in gameplay layer).

## Acceptance

1. `ContactPictureProjection` rebuilds active contacts from `ContactChange` rows.  
2. `SensorC2Bridge` merges picture with `ISimWorldSnapshot` indicators.  
3. `DelegationBridgeHost.LastSensorC2` updated each tick; `SensorC2HudHost` renders list.  
4. Tests: `ContactPictureProjectionTests`, `SensorC2BridgeTests`, PlayMode smoke extension.