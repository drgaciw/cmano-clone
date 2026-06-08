# Sprint 19 — GDD Refresh Wave Design Review (S19-06)

**Date:** 2026-06-08  
**Sprint:** 19  
**Task:** S19-06 — GDD refresh wave (3 systems)  
**Baseline:** `main` @ `afd2e1a`  
**Tests:** `dotnet test ProjectAegis.sln` → **403/403 PASS**; PlayMode **7/7**

## Scope

| GDD | System # | Req docs | Prior status | New status |
|-----|----------|----------|--------------|------------|
| [simulation-core-time.md](../../design/gdd/simulation-core-time.md) | 1 | 03, 08 | In Review (stale 2026-05-29) | **In Review** (S19-06 refresh) |
| [logistics-magazines.md](../../design/gdd/logistics-magazines.md) | 7 | 16 | Approved 2026-06-01 | **In Review** (S19-06 refresh) |
| [engagement-fire-control.md](../../design/gdd/engagement-fire-control.md) | 6 | 14 | In Review (thin stub) | **In Review** (S19-06 expanded) |

## Verdict Summary

| GDD | Verdict | Blocking? |
|-----|---------|-----------|
| simulation-core-time | **APPROVED WITH CONDITIONS** | No — headless MVP gate met |
| logistics-magazines | **APPROVED WITH CONDITIONS** | No — sustainment MVP gate met |
| engagement-fire-control | **APPROVED WITH CONDITIONS** | No — unified resolver + attack menu met |

**Wave verdict:** **APPROVED WITH CONDITIONS** — all three GDDs are **design-review ready** for programmer hand-off on headless MVP scope. P1 gaps are documented and non-blocking for Sprint 19 merge.

---

## Checklist (per QA plan S19-06)

| Check | sim-core | logistics | engage |
|-------|----------|-----------|--------|
| Status → In Review or Approved | ✓ | ✓ | ✓ |
| AC complete, testable, no TBD | ✓ (9 AC) | ✓ (6 AC) | ✓ (6 AC) |
| Formulas + worked example / symbol table | ✓ | ✓ | ✓ |
| Edge Cases ≥ 3 rows | ✓ (6) | ✓ (10) | ✓ (10) |
| TR-IDs mapped to req docs | ✓ 001–005 | ✓ 001–004 | ✓ 001–003 |
| GitNexus notes where cited | ✓ | ✓ | ✓ |
| Implementation Mapping table | ✓ | ✓ | ✓ |

---

## simulation-core-time.md

**Reviewer:** agent (lean design-review, S19-06)  
**Completeness:** 8/8 sections

### AC spot-verify

| AC | Claim | Evidence | Result |
|----|-------|----------|--------|
| AC-1 | Same seed → identical world hash + fingerprint | `ReplayGoldenSuiteTests`, 6 golden files | **Met** |
| AC-2 | Fixed timestep advance | `SimTickRunnerTests`, `SimTickPipelineTests` | **Met** |
| AC-3 | Domain RNG determinism | `DeterministicDetectionLoopTests` | **Met** |
| AC-4 | Headless without Unity | `BalticReplayHarnessTests`, `BalticBatchRunnerTests` | **Met** |
| AC-5 | Planning pause no-op | `SimulationSessionPhaseTests` | **Met** |
| AC-6 | HeadlessBatch compression | `SimTickRunnerTests` | **Met** |
| AC-7 | Checkpoint store | `ReplayGoldenBalticCheckpointTests` | **Met** |
| AC-8 | Delegation order determinism | `OrchestratorTests` | **Met** |
| AC-9 | Sim assembly headless + PlayMode | `ProjectAegis.Sim.Tests`, PlayMode 7/7 | **Met** |

### Conditions (non-blocking)

1. **1000× throughput profile gate** — deferred P1 (doc 03).
2. **Full DOTS ECS bridge** — deferred P1 (ADR-005).
3. **Tick 3600+ long-horizon golden** — deferred P1; pinned 3–6 tick goldens sufficient for CI today.

---

## logistics-magazines.md

**Reviewer:** agent (lean design-review, S19-06)  
**Completeness:** 8/8 sections

### AC spot-verify

| AC | Claim | Evidence | Result |
|----|-------|----------|--------|
| AC-1 | Magazine empty on third engage | `MvpEngagementResolverTests`, `MagazineChangeOrderLogTests`, `ReplayGoldenBalticMagazineTests` | **Met** |
| AC-2 | MagazineChange in chronological log | `MagazineChangeOrderLogTests`, replay goldens | **Met** |
| AC-3 | Deterministic fuel burn | `FuelLedgerTests`, `FuelTimelineTrackerTests`, `BalticReplayHarnessFuelTests` | **Met** |
| AC-4 | Editor strike reachability | `ScenarioValidationEngineTests`, `ReachabilityCalculatorTests` | **Partial** — flight-plan parity P1 |
| AC-5 | Editor AIR_NOT_READY | `LogisticsValidationRulesTests` | **Met** |
| AC-6 | Runtime AIR_NOT_READY | `MvpEngagementAirNotReadyTests`, `BalticReplayHarnessReadinessPolicyTests`, readiness golden | **Met** |

### Conditions (non-blocking)

1. **Catalog magazine chains** — MVP uses scenario `defaultMagazineRounds`; P1.
2. **UNREP / at-sea re-arm** — P1.
3. **Composite readinessScore** — MVP uses `ReadyForLaunch` boolean; P1.

---

## engagement-fire-control.md

**Reviewer:** agent (lean design-review, S19-06)  
**Completeness:** 8/8 sections

### AC spot-verify

| AC | Claim | Evidence | Result |
|----|-------|----------|--------|
| AC-1 | Unified resolver | `MvpEngagementResolverTests`, `ReplayGoldenBalticEngageTests` | **Met** |
| AC-2 | Abort logging + fingerprint | `EngagementOrderLogContractTests`, `AbortReasonManifestTests` | **Met** |
| AC-3 | DLZ preview parity | `MvpEngagementDlzPersonalityTests`, `EngageAttackOptionsTests` | **Met** |
| AC-4 | Attack menu commit | `DelegationBridgeAttackOptionTests`, `EngageAttackOrderResolverTests` | **Met** |
| AC-5 | Policy + magazine denials | `MvpEngagementResolverTests`, `MagazineChangeOrderLogTests` | **Met** |
| AC-6 | Swarm slot order P0 | `SwarmSalvoDeconflictionTests` | **Met** |

### Conditions (non-blocking)

1. **TR-engage-003 sector coordinator** — P1 gap; P0 deconfliction documented.
2. **TR-engage-002 DLZ map indicator** — preview ships; map symbology P1.
3. **Multi-mount picker** — MVP implicit mount `0`; UX P1.

---

## GitNexus blast-radius notes (cited in GDDs)

| Symbol | Risk | Sprint 19 touch |
|--------|------|-----------------|
| `SimulationSession` | HIGH | Readiness priming, engage phase, fuel drain |
| `SimTickPipeline` | HIGH | ADR-004 ordering |
| `DelegationBridge` | CRITICAL | Attack menu — no edits in S19-06 |
| `MvpEngagementResolver` | MEDIUM | Documented only |

---

## systems-index updates

| System # | Old status | New status |
|----------|------------|------------|
| 1 Simulation Core & Time | In Review | **In Review** (S19-06 refresh — design-review ready) |
| 6 Engagement & Fire Control | Partial (Baltic engage) | **In Review** (Wave 5 attack menu + resolver expanded) |
| 7 Logistics & Magazines | Partial (MagazineChange; fuel P0 open) | **In Review** (fuel + readiness shipped; catalog chains P1) |

---

## Sign-off

| Role | Verdict | Date |
|------|---------|------|
| Design (agent lean review) | APPROVED WITH CONDITIONS | 2026-06-08 |
| QA lead (automated AC cross-check) | PASS WITH NOTES | 2026-06-08 |
| Human design lead | Pending | — |

**Notes:** Human design-lead sign-off optional for doc-only sprint task; programmer hand-off permitted on headless MVP scope per PI-006 proxy precedent.