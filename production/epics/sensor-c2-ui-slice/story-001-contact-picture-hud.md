# Story 001 — Contact picture + sensor C2 HUD

**Status:** Complete

## Acceptance

- [x] Contact picture projection from order log
- [x] Sensor C2 snapshot (EMCON, track, contacts)
- [x] Unity `SensorC2HudHost` + bridge host wiring
- [x] Automated tests green

## Test traceability

| Acceptance criterion | Test file / method | Notes |
|----------------------|-------------------|-------|
| Contact picture projection from order log | `ContactPictureProjectionTests.Empty_log_yields_empty_picture` | Empty `DecisionLog` → empty picture |
| Same | `ContactPictureProjectionTests.Detect_classify_identify_updates_single_contact_in_order` | Lifecycle string progression |
| Same | `ContactPictureProjectionTests.Lost_removes_contact_from_picture` | Lost transition drops contact |
| Same | `ContactPictureProjectionTests.Multiple_contacts_sorted_by_contact_id` | Stable sort order |
| Sensor C2 snapshot (EMCON, track, contacts) | `SensorC2BridgeTests.Baltic_patrol_sensor_c2_lists_detected_contact_with_emcon_active` | `BalticReplayHarness` end-to-end |
| Same (classify lifecycle) | `SensorC2BridgeTests.Baltic_patrol_classify_sensor_c2_shows_classified_then_identified` | `baltic-patrol-classify` → Classified/Identified |
| Same | `PlayModeSmokeHarnessTests.Baltic_patrol_sensor_c2_snapshot_matches_harness_run` | Harness ↔ replay parity |
| Unity `SensorC2HudHost` + bridge host wiring | `DelegationBridgeHost.LastSensorC2` (`unity/.../DelegationBridgeHost.cs`) | Updated each tick via `SensorC2Bridge` |
| Same (legacy OnGUI) | `SensorC2HudHost` (`unity/.../SensorC2HudHost.cs`) | Obsolete; superseded by UI Toolkit panel |
| Automated tests green | Sprint 2 closeout smoke (`production/qa/smoke-sprint2-closeout-2026-06-08.md`) | **430/430** solution; 12/12 UnityAdapter scoped |