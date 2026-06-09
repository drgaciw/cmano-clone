# Story 002 — UI Toolkit sensor C2 panel

**Status:** Complete

## Acceptance

- [x] `SensorC2PanelBinder` maps snapshot → panel labels (unit tested)
- [x] `SensorC2Panel.uxml` + `SensorC2Panel.uss` military-density styling
- [x] `SensorC2PanelHost` binds `UIDocument` to `DelegationBridgeHost.LastSensorC2`
- [x] PLAYMODE-SMOKE documents asset wiring
- [x] `dotnet test ProjectAegis.sln` green

## Test traceability

| Acceptance criterion | Test file / method | Notes |
|----------------------|-------------------|-------|
| `SensorC2PanelBinder` maps snapshot → panel labels (unit tested) | `SensorC2PanelBinderTests.Bind_maps_emcon_track_and_contact_rows` | EMCON / TRACK / CONTACTS labels + row text |
| Same | `SensorC2PanelBinderTests.Bind_empty_snapshot_shows_zero_contacts` | Zero-contact edge case |
| Same (integration) | `SensorC2PanelBinderIntegrationTests.Baltic_patrol_binder_produces_contact_row_for_hud` | Live `BalticReplayHarness` snapshot |
| Same (classify lifecycle) | `SensorC2PanelBinderIntegrationTests.Baltic_patrol_classify_binder_shows_lifecycle_states` | Row display includes Classified/Identified |
| `SensorC2Panel.uxml` + `SensorC2Panel.uss` military-density styling | `unity/ProjectAegis/Assets/UI/SensorC2/SensorC2Panel.uxml`, `.uss` | Imported via Unity batch compile log |
| `SensorC2PanelHost` binds `UIDocument` to `DelegationBridgeHost.LastSensorC2` | `SensorC2PanelHost` (`unity/.../SensorC2PanelHost.cs`) | `SensorC2PanelBinder.Bind(bridgeHost.LastSensorC2)` in `LateUpdate` |
| PLAYMODE-SMOKE documents asset wiring | `unity/ProjectAegis/PLAYMODE-SMOKE.md` (step 9 `bridgeHost` wiring) | General bridge pattern; panel host uses same `bridgeHost` reference |
| Same (design doc) | `design/gdd/command-and-control-ui.md` | Lists `SensorC2PanelHost` as shipped sensor strip |
| `dotnet test ProjectAegis.sln` green | Sprint 2 closeout smoke (`production/qa/smoke-sprint2-closeout-2026-06-08.md`) | **430/430** solution; 12/12 UnityAdapter scoped |