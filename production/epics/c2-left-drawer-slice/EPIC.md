# Epic: C2 Left Drawer Slice (Sprint 3)

> **Status:** Complete (Sprint 3 MVP slice)
> **Layer:** Presentation (projection) + Unity UI Toolkit  
> **Requirement:** [20-Command-And-Control-UI.md](../../../Game-Requirements/requirements/20-Command-And-Control-UI.md) — left drawer tabs

## Goal

Headless projections for **OOB tree**, **mission list**, and **full message log** (all order-log categories), bound to UI Toolkit ListView hosts without gameplay-layer UI state.

## Stories

| ID | Story | Status |
|----|-------|--------|
| 001 | [story-001-full-message-log.md](story-001-full-message-log.md) | Complete |
| 002 | [story-002-oob-tree-projection.md](story-002-oob-tree-projection.md) | Complete |
| 003 | [story-003-mission-list-projection.md](story-003-mission-list-projection.md) | Complete |

## Acceptance

1. `MessageLogProjection` categories visible in Unity host (not combat-filtered).  
2. `OobTreeProjection` lists registry members with alive/dead from snapshot.  
3. `MissionListProjection` lists scenario timeline events in deterministic order.  
4. Tests: projection unit tests + Baltic classify CONTACT message test.

## SensorC2 overlap (Sprint 3–6 closeout)

| Surface | Role | Selection |
|---------|------|-----------|
| `C2LeftDrawerPanelHost` Contacts tab | Primary contact list + row click → `IC2PresentationFeed.SelectContact` | **Owner** |
| `SensorC2PanelHost` HUD strip | Read-only EMCON/track/contact density (same `LastSensorC2` binder) | Display only |

Both bind `SensorC2PanelBinder.Bind(feed.LastSensorC2)`; selection state lives in `C2PresentationController` on `DelegationBridgeHost`.