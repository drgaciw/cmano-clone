# Story 003 — Live readiness from policy JSON

**Epic:** wave5-engage-cyber-logistics-slice  
**Sprint:** 13  
**Priority:** Must Have  
**Status:** Complete  
**TR-ID:** req-16 P0 readiness

## Acceptance

1. `baltic-patrol-readiness.policy.json` loads `unitReadiness` without harness override.  
2. `BalticReplayHarness.Run(..., "baltic-patrol-readiness")` applies map from profile.  
3. `ScenarioPolicySpoofReadinessJsonTests` covers readiness dictionary.  
4. Tracker row 16 cites policy path + test names.