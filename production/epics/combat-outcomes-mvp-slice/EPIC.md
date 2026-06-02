# Epic: Combat Outcomes MVP Slice

> **Status:** Not started  
> **Priority:** P2 — player-visible fight resolution  
> **Created:** 2026-06-02  
> **Depends on:** Engage launch on log, platform-db-basepd (recommended)  
> **GDD:** Combat domains (system #8 — GDD not started)

## Goal

After `EngagementRecord.Launched`, deterministic **hit/miss/kill** using `SeededRng` domains; outcome rows in order log; `WORLD_HASH` includes outcome sub-hash.

## Acceptance

1. No `System.Random` or `GetHashCode` in outcome path.
2. Baltic scenario: at least one intercept outcome row in replay export.
3. Magazine + outcome both reflected in fingerprint tests.