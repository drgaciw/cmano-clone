# Epic: Contact Stale / Lost Slice

> **Status:** Complete  
> **Created:** 2026-06-02

## Goal

Emit `ContactChange` **Detected → Lost** when a contact misses updates for `staleThresholdTicks` (sensor GDD MVP).

## Acceptance

1. Scenario `contactLifecycle.staleThresholdTicks` in JSON.
2. Pd path: after detect, missed ticks → Lost row in order log.
3. Regression test with threshold 1.