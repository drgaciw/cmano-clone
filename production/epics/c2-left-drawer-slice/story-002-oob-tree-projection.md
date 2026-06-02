# Story 002 — OOB tree projection

**Epic:** c2-left-drawer-slice  
**Priority:** Must Have  
**Status:** In Progress  
**TR-ID:** Doc 20 left drawer — OOB tab

## Acceptance

1. `OobTreeProjection` orders members by `TargetId` (ordinal).  
2. `OobTreeBridge` combines `TargetRegistry` + `ISimWorldSnapshot.IsMemberAlive`.  
3. `OobTreePanelBinder` + `OobTreePanelHost` (UI Toolkit) render unit rows.  
4. Unit tests cover alive/dead and sort order.