# Adversarial TDD Hardening — Wave 2 (docs 13–20)

**Date:** 2026-07-08  
**Branch:** `test/adversarial-wave2-tdd`  
**Mode:** Parallel audit (4 domains) → regression pins  
**Production code:** none

## Wave 2 claims attacked

| Doc | Claims |
|-----|--------|
| 13–14 | WeaponsTight reason, WRA boundary, swarm deconflict |
| 15–16 | Magazine atomicity, AIR_NOT_READY freeze, catalog zero seeder |
| 17–19 | PolicyDenial sequenceId for AAR |
| 18 | MountOffline + ASW DomainNoSolution on full resolver |

## Pins implemented (all green)

| Test | Assembly |
|------|----------|
| `WeaponsTight_denies_with_WeaponsTight_not_RoeHoldFire` (evaluator + resolver) | Sim.Tests |
| `Wra_max_salvo_exact_boundary` | Sim.Tests |
| `Wra_salvo_exact_max_allows_launch` | Sim.Tests |
| `Allocate_three_way_same_target_is_deterministic_under_reverse_input` | Sim.Tests |
| `Resolve_AIR_NOT_READY_does_not_mutate_magazine_ledger` | Sim.Tests |
| `TryConsumeSalvo_when_insufficient_rounds…` | Sim.Tests |
| `TrySeedInitialRounds_catalog_resolved_zero…` | Sim.Tests |
| `Resolve_MountOnline_false…` | Sim.Tests |
| `Resolve_subsurface_ContactIdentified_false…` | Sim.Tests |
| `PolicyDenial_sequenceId_matches_order_log_entry` | Delegation.Tests |

**Verify:** Sim filter **12 passed**; Delegation PolicyDenial **1 passed**.

## Backlog (not this PR)

- CommsOrderDelay sort vs FIFO doc honesty or code fix  
- Dual-surface WeaponsTight UA harness hard pin  
- ADR-010 presentation non-mutation e2e  
- EngagePreview ASW parity with resolver  
