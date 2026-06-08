# Story 002 — OOB tree projection

**Epic:** c2-left-drawer-slice  
**Priority:** Must Have  
**Status:** Complete
**TR-ID:** Doc 20 left drawer — OOB tab

## Acceptance

1. `OobTreeProjection` orders members by `TargetId` (ordinal).  
2. `OobTreeBridge` combines `TargetRegistry` + `ISimWorldSnapshot.IsMemberAlive`.  
3. `OobTreePanelBinder` + `OobTreePanelHost` (UI Toolkit) render unit rows.  
4. Unit tests cover alive/dead and sort order.

## Test traceability

| Acceptance criterion | Test file / method | Status |
|----------------------|-------------------|--------|
| OobTreeProjection sort | `OobTreeProjectionTests` | COVERED |
| OobTreeBridge alive/dead | `OobTreeBridgeTests` | COVERED |
| OobTreePanelBinder | `OobTreePanelBinderTests` | COVERED |
| Selection sync map+OOB | `C2SelectionFlowTests.Oob_row_click_syncs_map_highlight_for_friendly_unit` | COVERED |