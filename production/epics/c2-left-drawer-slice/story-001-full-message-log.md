# Story 001 — Full message log projection

**Epic:** c2-left-drawer-slice  
**Priority:** Must Have  
**Status:** Complete
**TR-ID:** C2-UI (requirements doc 18 / 20 bottom strip)

## Acceptance

1. `DelegationBridgeHost.LastMessageLog` uses `MessageLogBridge.ProjectFrom` (all categories).  
2. `MessageLogBridgeTests` asserts CONTACT lines on `baltic-patrol-classify`.  
3. Combat strip helper `ProjectCombatMessages` retained for optional compact HUD mode.

## Test traceability

| Acceptance criterion | Test file / method | Status |
|----------------------|-------------------|--------|
| LastMessageLog all categories | `MessageLogBridgeTests` | COVERED |
| CONTACT on classify | `MessageLogBridgeTests` + `MessageLogProjectionTests` | COVERED |
| Combat strip helper retained | `MessageLogBridge.ProjectCombatMessages` (grep) | COVERED |