# Story 004 — Readiness engage abort in order log

**Epic:** wave5-engage-cyber-logistics-slice  
**Sprint:** 13  
**Priority:** Must Have  
**Status:** Complete  
**TR-ID:** req-16 / req-17

## Acceptance

1. Engage attempt when `ReadyForLaunch == false` → `AIR_NOT_READY` in fingerprint.  
2. `BalticReplayHarnessReadinessPolicyTests` PASS.  
3. Message log or order log row visible in projection test (category consistent with logistics family).