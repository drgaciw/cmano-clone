# Story 001 — Spoof track runtime

**Epic:** wave5-engage-cyber-logistics-slice  
**Sprint:** 13  
**Priority:** Must Have  
**Status:** Complete  
**TR-ID:** req-19 P0 `SpoofTracks`

## Acceptance

1. `ScenarioPolicyJsonLoader` parses `spoofTracks` → `ScenarioSpoofTransition[]`.  
2. `SpoofTrackTimelineSimulator` activates contact at configured sim tick.  
3. Engage abort `TrackSpoofed` logs `CYBER_SPOOF_TRACK`.  
4. `BalticReplayHarnessSpoofTests` PASS; replay golden updated if needed.  
5. GitNexus impact on `DelegationBridge` documented (CRITICAL).