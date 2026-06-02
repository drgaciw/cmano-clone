# Epic: Policy–Engage Unification Slice

> **Status:** In progress (`MvpEngagementResolver` policy gate on `stack/data/basepd`)  
> **Priority:** P1 — replay parity for denials  
> **Created:** 2026-06-02  
> **Depends on:** Baltic engage MVP, EMCON slice (complete)  
> **GDD:** [policy-roe-emcon-wra.md](../../design/gdd/policy-roe-emcon-wra.md), [engagement-fire-control.md](../../design/gdd/engagement-fire-control.md)  
> **Deferred note:** `SIM-FUTURE` in baltic-headless-slice EPIC

## Goal

Single pipeline: **Intent → `IPolicyEvaluator` → DLZ → launch** inside `MvpEngagementResolver`; all ROE/WRA/EMCON denials appear in engagement order log + `WORLD_HASH`.

## Acceptance

1. Policy denial paths no longer bypass engage fingerprint.
2. Scenario tests: EmconOff / ROE restricted → stable abort codes in log.
3. `/replay-verify` PASS on existing Baltic golden + new policy-denial fixture.