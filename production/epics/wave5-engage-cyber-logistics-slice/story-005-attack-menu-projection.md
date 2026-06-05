# Story 005 — Attack menu projection

**Epic:** wave5-engage-cyber-logistics-slice  
**Sprint:** 14  
**Priority:** Must Have  
**Status:** Complete  
**TR-ID:** req-14 §4.1.1

## Acceptance

1. `EngageAttackOptions.Build` returns ≥2 options for Baltic engage fixture.  
2. Disabled state when preview abort (spoof, readiness, comms).  
3. `EngageAttackOrderResolver` maps option id → engage order payload.  
4. `EngageAttackOptionsTests` extended; all green.