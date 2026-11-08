# SWARM-B7 / DRG-99 — Doctrine/WRA for swarm auto-engage

**Date:** 2026-08-09  
**Surface:** `src/ProjectAegis.Sim/Policy/**`, `src/ProjectAegis.Sim.Tests/Policy/**`  
**Requirement:** SWARM-15

## ACs

| AC | Evidence |
|----|----------|
| HoldFire blocks auto-engage | `HoldFire_denies_auto_engage_with_RoeHoldFire` |
| AutoEngageAuthorized=false denies | `WeaponsFree_auto_engage_denied_when_not_authorized` |
| Expend unauthorized denied | `Expend_without_authorization_denied` |
| WRA salvo applies to auto-engage | `Wra_max_salvo_gates_auto_engage` |
| Backward compat manual fire | `Default_policy_still_allows_manual_FireGuided` |

## Types

- `EffectivePolicy.AutoEngageAuthorized` / `ExpendAuthorized`
- `FireAbortReason.AutoEngageDenied` / `ExpendUnauthorized`
- `ActionRequest.IsAutoEngage` / `IsExpend`
