# Story 002 — Spoof C2 indicator

**Epic:** wave5-engage-cyber-logistics-slice  
**Sprint:** 13  
**Priority:** Must Have  
**Status:** Complete  
**TR-ID:** req-19 / req-20

## Acceptance

1. `EngagePreviewProjection` exposes spoof abort code to UI context.  
2. Unit detail or engage line shows cannot-engage when track spoofed.  
3. `EngageAttackOptions` omits or disables fire options when `TrackSpoofed`.  
4. Binder/projection unit test (no PlayMode required for AC 1–3).