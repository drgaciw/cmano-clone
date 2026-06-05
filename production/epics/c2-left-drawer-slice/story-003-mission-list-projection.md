# Story 003 — Mission list projection

**Epic:** c2-left-drawer-slice  
**Priority:** Should Have  
**Status:** Complete
**TR-ID:** Doc 20 left drawer — missions tab

## Acceptance

1. `MissionListProjection` projects `ScenarioMissionTimeline` events in tick + id order.  
2. Empty timeline returns empty list (no throw).  
3. `MissionListPanelBinder` formats rows for ListView binding.