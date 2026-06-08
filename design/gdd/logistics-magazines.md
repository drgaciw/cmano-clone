# Logistics & Magazines

> **Status:** In Review (Sprint 19 refresh — S19-06; was Approved 2026-06-01)  
> **Author:** design-system  
> **Last Updated:** 2026-06-08  
> **Implements Pillar:** Sustainment fidelity, Deterministic consumption  
> **Requirements:** [16-Logistics-And-Magazines.md](../../Game-Requirements/requirements/16-Logistics-And-Magazines.md)  
> **Architecture:** [architecture.md](../../docs/architecture/architecture.md), [ADR-003](../../docs/architecture/adr-003-order-log-contract.md), [ADR-004](../../docs/architecture/adr-004-tick-pipeline-order.md), [ADR-006](../../docs/architecture/adr-006-data-layer-boundary.md)  
> **Depends on:** [simulation-core-time.md](simulation-core-time.md), [engagement-fire-control.md](engagement-fire-control.md)  
> **Depended on by:** [agentic-mission-editor.md](agentic-mission-editor.md) (validation), [scoring-losses.md](scoring-losses.md) (expenditures)  
> **Implementation baseline:** `main` @ `afd2e1a`

> **Quick reference** — Layer: **Core** · Priority: **MVP** · System #7

## Summary

Logistics governs **fuel**, **magazines**, **readiness**, and **sortie generation** so engagements and missions fail for sustainment reasons—not arbitrary timers. Runtime consumption is **deterministic** and logged; authoring validation reuses the same reachability formulas as the sim (doc 11 Validation Engine, ADR-006).

**MVP on `main`:** `MagazineLedger` + `MvpEngagementResolver` consume rounds and abort `MagazineEmpty`; `MagazineChange` and `FuelStateChange` rows land in the order log; `FuelLedger` / `FuelTimelineTracker` burn fuel when a scenario opts into the burn model; `UnitReadinessMap` gates launch via `ReadyForLaunch` → `AIR_NOT_READY`.

**Known gaps (honest):** UNREP and at-sea re-arm (**P1**); full Platform DB magazine → mount → weapon chains (**P1** — MVP uses scenario `engage.defaultMagazineRounds`); editor `STRIKE_UNREACHABLE` reachability is **partial** (combat-radius and fuel-budget checks ship; full flight-plan / tanker parity **P1**); composite `readinessScore` formula deferred — MVP uses per-unit `ReadyForLaunch` boolean.

## Overview

| Concern | Owner | Notes |
|---------|-------|-------|
| Magazine consume | `MagazineLedger`, `MvpEngagementResolver` | `TryConsume` / `TryConsumeSalvo`; abort `MagazineEmpty` (log code `NO_AMMO`) |
| Magazine order log | `SimulationSession`, `DecisionLog` | `MagazineChange` row on successful launch only |
| Fuel burn | `FuelLedger`, `FuelTimelineTracker` | Opt-in via `ScenarioLogisticsSettings.UsesFuelBurnModel` |
| Fuel order log | `DelegationBridge`, `DecisionLog` | `FuelStateChange` on NOMINAL → JOKER → BINGO band crossings |
| Readiness gate | `UnitReadinessMap`, `PrimeEngageWorld` | `ReadyForLaunch == false` → `AIR_NOT_READY` before magazine check |
| Editor validation | `ScenarioValidationEngine`, `ReachabilityCalculator` | `STRIKE_UNREACHABLE`, `STRIKE_UNREACHABLE_FUEL`, `AIR_NOT_READY` |
| C2 display | `FuelStateProjection`, `UnitDetailProjection` | Unit detail `FUEL:` line (Sprint 8) |
| Scenario data | `data/scenarios/*.policy.json` | `engage`, `logistics`, `unitReadiness` blocks |

## Player Fantasy

You win or lose campaigns because magazines empty, tankers are late, and decks cannot generate sorties—not because the UI hid ammo counts. When a strike package is impossible, you hear it at planning time (`scenario_validate`) and see it in the message log at execution time (`MagazineEmpty`, `AIR_NOT_READY`, fuel band warnings).

## Detailed Rules

### Magazines (P0)

1. Platform DB (doc 06) defines magazine → mount → weapon chains with count, reload time, and compatible stores. **MVP gap:** chains are not yet loaded from catalog; capacity comes from scenario policy `engage.defaultMagazineRounds` and harness defaults (**P1** catalog binding).
2. **Consumption** occurs in the engagement pipeline (doc 14, ADR-004 step 8) via `MagazineLedger.TryConsume` / `TryConsumeSalvo` inside `MvpEngagementResolver.Resolve`.
3. Each successful launch emits a `MagazineChange` order-log row: `{shooterId, mountId, delta, reason: fire|reload|transfer}`. MVP implements `fire` only; reload/transfer **P1**.
4. UI shows magazine %, ready count, reload progress (doc 20). **Partial:** order-log and engage abort paths ship; dedicated magazines panel **P1**.

### Fuel & endurance (P0)

1. Fuel types per domain (aviation JP, naval F76, etc.) from DB. **MVP:** single capacity + burn rate per scenario `logistics` block.
2. Burn rate = `baseBurn * throttleFactor` (MVP: constant `burnRateKgPerSecond`; throttle and altitude factors **P1**).
3. States: `NOMINAL`, `JOKER`, `BINGO` — `FuelStateChange` order-log row and message-log `FUEL` category on band crossing.
4. Ferry missions (doc 11) validated for reachability; `FERRY_UNREACHABLE` / `FERRY_UNREACHABLE_FUEL` codes ship in validation engine.

### Air operations (P0)

States: `OnGround | Taxiing | TakingOff | Airborne | Landing | Maintenance`. **MVP:** launch readiness is a boolean `ReadyForLaunch` per unit in scenario metadata/policy JSON—not the full state machine. Ready aircraft count and carrier deck cycle are data-driven targets (**P1** full air-ops FSM).

### Readiness (P0)

**Target formula (doc 16):** `readinessScore = min(fuelNorm, ammoNorm, damageNorm, maintenanceNorm)` in `[0,1]`. Mission assign blocked when `< readinessMin`; advisory when `< readinessWarn`.

**MVP implementation:** per-unit `ReadyForLaunch` boolean in `scenario.Metadata.UnitReadiness` / policy `unitReadiness`. `UnitReadinessMap.IsReadyForLaunch` defaults missing units to ready. `SimulationSession.PrimeEngageWorld` sets `EngageContext.AirOperationsReady` from the map before each engage. Editor `AirReadyLaunchRule` emits `AIR_NOT_READY` on strike missions with `ReadyForLaunch == false`. Composite score and `readinessMin`/`readinessWarn` knobs **P1**.

### Integration

| Consumer | Uses logistics for |
|----------|-------------------|
| Engagement | Magazine empty → `MagazineEmpty` (`NO_AMMO`); not ready → `AIR_NOT_READY` |
| Mission editor | Strike fuel/magazine/readiness validation (`ScenarioValidationEngine`) |
| Order log | `MagazineChange`, `FuelStateChange` |
| C2 UI | `FuelStateProjection` → unit detail `FUEL:` line |
| Scoring | Expenditures (doc 17) — magazine deltas feed loss accounting **P1** full wire |

## Formulas

| Symbol | Meaning | Range |
|--------|---------|-------|
| `M0` | Magazine capacity (rounds) | > 0 |
| `M(t)` | Rounds after tick t | [0, M0] |
| `F(t)` | Fuel remaining (kg) | [0, F0] |
| `burn` | `baseBurnRate * throttle` | throttle ∈ [0,1] (throttle **P1**) |
| `R` | Readiness score (target) | [0,1] |
| `ready` | `ReadyForLaunch` (MVP) | bool |

**Magazine after fire:** `M' = M - salvoSize` if `TryConsumeSalvo` succeeds; else unchanged and abort `MagazineEmpty`.

**Fuel per tick:** `F' = F - burnRateKgPerSecond * deltaSeconds` (clamped at 0).

**Fuel band:** `state = NOMINAL` if `F/F0 > jokerFrac`; `JOKER` if `F/F0 > bingoFrac`; else `BINGO`.

### Worked example

- **Magazines:** `M0 = 2`, two single-round engages → `M = 0`; third engage aborts `MagazineEmpty`, no third `MagazineChange` row.
- **Fuel:** `F0 = 10_000 kg`, `burn = 80 kg/s`, `Δt = 1 s` × 2 ticks → `F = 9_840 kg` (`FuelLedgerTests`).
- **Readiness:** `unitReadiness.u1.readyForLaunch = false` in `baltic-patrol-readiness.policy.json` → every engage attempt logs `AIR_NOT_READY`.

## Edge Cases

| Case | Behavior |
|------|----------|
| Salvo > rounds left | `TryConsumeSalvo` fails; abort `MagazineEmpty` (no partial salvo in MVP) |
| Reload in progress | Mount not `ready`; engage abort `MountNotReady` (**P1** — reload timer not wired) |
| Simultaneous fires same mount | Deterministic order by `shooterId` then `mountId` (delegation engage queue + salvo deconflict) |
| UNREP disabled in scenario | Re-arm at sea blocked; log advisory only (**P1** — UNREP not implemented) |
| Zero fuel | Unit `BINGO`; `FuelStateChange` logged; RTB auto-suggest **P1** |
| Scenario without burn model | No `FuelTimelineTracker`; fingerprint has no `FuelStateChange` rows (`baltic-patrol` short run) |
| Missing `unitReadiness` entry | Treated as ready (`UnitReadinessMap` default) |
| Editor strike beyond combat radius | `STRIKE_UNREACHABLE` with `excess_nm` data |
| Editor strike inside radius, over fuel budget | `STRIKE_UNREACHABLE_FUEL` with `excess_nm` data |
| Catalog magazine chains absent | Harness uses `engage.defaultMagazineRounds` from policy JSON |

## Dependencies

| System | Direction | Contract |
|--------|-----------|----------|
| Simulation Core | Requires | Fixed tick; engage at pipeline step 8 |
| Platform DB (doc 06) | Requires | Magazine chains, fuel curves (**P1** live catalog bind) |
| Engagement (doc 14) | Bidirectional | Resolver consumes magazines; readiness primed in session |
| Order Log (doc 17) | Downstream | `MagazineChange`, `FuelStateChange` fingerprint lines |
| Mission Editor (doc 11) | Downstream | `ScenarioValidationEngine`, `ReachabilityCalculator` |
| C2 UI (doc 20) | Downstream | `FuelStateProjection`, magazines panel **P1** |
| Scoring & Losses (doc 17) | Downstream | Expenditure roll-up from order log |

## Tuning Knobs

| Knob | Safe range | Effect | MVP config path |
|------|------------|--------|-----------------|
| `defaultMagazineRounds` | 1–96 | Initial rounds per shooter+mount | `engage.defaultMagazineRounds` in `*.policy.json` |
| `readinessMin` | 0.2–0.8 | Mission block threshold | **P1** — MVP uses `ReadyForLaunch` bool |
| `readinessWarn` | 0.3–0.9 | Advisory threshold | **P1** |
| `jokerFuelFrac` | 0.15–0.35 | JOKER band threshold | `logistics.jokerFuelFraction` |
| `bingoFuelFrac` | 0.05–0.15 | BINGO band threshold | `logistics.bingoFuelFraction` |
| `burnRateKgPerSecond` | 10–500 | Fuel drain rate | `logistics.burnRateKgPerSecond` |
| `fuelCapacityKg` | 1_000–50_000 | Starting fuel | `logistics.fuelCapacityKg` |
| `usesFuelBurnModel` | bool | Enable `FuelTimelineTracker` | `logistics.usesFuelBurnModel` |
| `defaultReloadSeconds` | 30–600 | Mount turnaround | **P1** |
| `burnRateMultiplier` | 0.5–2.0 | Scenario difficulty scalar | **P1** |

Data paths: `data/scenarios/*.policy.json` (runtime MVP); `assets/data/logistics_*.json` (**future** catalog export).

## Acceptance Criteria

1. **AC-1 (TR-logistics-001):** Given magazine count 2, two legal engages consume to 0; third engage logs `MagazineEmpty` (`NO_AMMO`) and does not launch. **Evidence:** `src/ProjectAegis.Sim.Tests/Engage/MvpEngagementResolverTests.cs` (`In_zone_with_ammo_launches_and_decrements_magazine`), `src/ProjectAegis.Delegation.Tests/Decision/MagazineChangeOrderLogTests.cs` (`Third_engage_after_magazine_empty_has_no_magazine_change_row`), `src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/ReplayGoldenBalticMagazineTests.cs`, `src/ProjectAegis.Sim.Tests/Engage/MagazineSalvoTests.cs`.
2. **AC-2 (TR-logistics-002):** `MagazineChange` appears in `DecisionLog.ChronologicalEntries()` with deterministic ordering and fingerprint lines. **Evidence:** `MagazineChangeOrderLogTests.cs` (`Two_launches_emit_two_magazine_change_rows_with_delta_minus_one`), `ReplayGoldenBalticMagazineTests.cs`, `src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/ReplayGoldenBalticSalvoTests.cs`.
3. **AC-3 (TR-logistics-003):** Fuel decreases per tick with fixed parameters; repeat headless runs produce identical fingerprint; scenarios without burn model emit no `FuelStateChange` rows. **Evidence:** `src/ProjectAegis.Sim.Tests/Logistics/FuelLedgerTests.cs`, `src/ProjectAegis.Delegation.Tests/Logistics/FuelTimelineTrackerTests.cs`, `src/ProjectAegis.Delegation.Tests/Decision/FuelStateChangeOrderLogTests.cs`, `src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/BalticReplayHarnessFuelTests.cs`, `data/scenarios/baltic-patrol-comms.policy.json`.
4. **AC-4 (TR-logistics-004):** Mission validation rejects strike package when target is beyond combat radius (`STRIKE_UNREACHABLE`) or inside radius but over fuel budget (`STRIKE_UNREACHABLE_FUEL`). **Evidence:** `src/ProjectAegis.Data.Tests/Validation/ScenarioValidationEngineTests.cs`, `src/ProjectAegis.Data.Tests/Validation/ReachabilityCalculatorTests.cs`, `src/ProjectAegis.Data.Tests/Validation/ValidationGoldenTests.cs`, `src/ProjectAegis.MissionEditor.Cli.Tests/ScenarioValidateCliTests.cs`. **Partial:** full flight-plan / tanker-segment parity with runtime sim **P1** (ADR-006 Accepted; editor uses `ReachabilityCalculator` not live `FuelLedger`).
5. **AC-5:** Strike mission with `ReadyForLaunch == false` emits `AIR_NOT_READY` in editor validation. **Evidence:** `src/ProjectAegis.Data.Tests/Validation/LogisticsValidationRulesTests.cs` (`Strike_with_not_ready_unit_emits_AIR_NOT_READY`). **Note:** MVP uses boolean `ReadyForLaunch`, not composite `readinessScore < readinessMin` (**P1**).
6. **AC-6:** Runtime engage with `ReadyForLaunch == false` aborts `AIR_NOT_READY` before magazine consume; policy JSON drives harness without explicit map injection. **Evidence:** `src/ProjectAegis.Sim.Tests/Engage/MvpEngagementAirNotReadyTests.cs`, `src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/BalticReplayHarnessReadinessPolicyTests.cs`, `tests/regression/replay-golden-baltic-readiness-2026-06-04.txt`, `data/scenarios/baltic-patrol-readiness.policy.json`.

## Implementation Mapping

| Concept | Code path | Test path |
|---------|-----------|-----------|
| Magazine ledger | `src/ProjectAegis.Sim/Engage/MagazineLedger.cs` | `src/ProjectAegis.Sim.Tests/Engage/MagazineSalvoTests.cs`, `MvpEngagementResolverTests.cs` |
| Engage consume + abort | `src/ProjectAegis.Sim/Engage/MvpEngagementResolver.cs` | `MvpEngagementResolverTests.cs`, `MvpEngagementAirNotReadyTests.cs` |
| Magazine order log | `src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs`, `src/ProjectAegis.Delegation/Decision/DecisionLog.cs` | `src/ProjectAegis.Delegation.Tests/Decision/MagazineChangeOrderLogTests.cs` |
| Magazine records | `src/ProjectAegis.Delegation/Decision/MagazineChangeRecord.cs`, `MagazineChangeReasonCodes.cs` | `MagazineChangeOrderLogTests.cs` |
| Fuel ledger | `src/ProjectAegis.Sim/Logistics/FuelLedger.cs` | `src/ProjectAegis.Sim.Tests/Logistics/FuelLedgerTests.cs` |
| Fuel timeline + bands | `src/ProjectAegis.Delegation/Logistics/FuelTimelineTracker.cs` | `src/ProjectAegis.Delegation.Tests/Logistics/FuelTimelineTrackerTests.cs` |
| Fuel order log | `src/ProjectAegis.Delegation/Decision/FuelStateChangeRecord.cs`, `DelegationBridge.cs` | `FuelStateChangeOrderLogTests.cs`, `BalticReplayHarnessFuelTests.cs` |
| Fuel C2 projection | `src/ProjectAegis.Delegation/Projection/FuelStateProjection.cs` | `src/ProjectAegis.Delegation.Tests/Projection/FuelStateProjectionTests.cs` |
| Readiness map | `src/ProjectAegis.Delegation/Sim/UnitReadinessMap.cs` | `BalticReplayHarnessReadinessPolicyTests.cs` |
| Readiness factory | `src/ProjectAegis.Data/Scenario/UnitReadinessMapFactory.cs` | `src/ProjectAegis.Data.Tests/Validation/LogisticsValidationRulesTests.cs` |
| Editor validation | `src/ProjectAegis.Data/Validation/Rules/ValidationRules.cs`, `ReachabilityCalculator` | `ScenarioValidationEngineTests.cs`, `LogisticsValidationRulesTests.cs` |
| Headless harness | `src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticReplayHarness.cs` | `ReplayGoldenBalticMagazineTests.cs`, `BalticReplayHarnessFuelTests.cs`, `BalticReplayHarnessReadinessPolicyTests.cs` |
| Replay goldens | `src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/ReplayGoldenRegressionCatalog.cs` | `tests/regression/replay-golden-baltic-readiness-2026-06-04.txt` |
| Abort glossary | `data/glossary/abort_reason_manifest.json`, `AbortReasonCatalog.Generated.cs` | `src/ProjectAegis.Sim.Tests/Glossary/AbortReasonManifestTests.cs` |

## TR IDs

| ID | Requirement | Implementation evidence | Status |
|----|-------------|-------------------------|--------|
| TR-logistics-001 | Magazine ledger with `MagazineEmpty` abort in engage pipeline | `MagazineLedger`, `MvpEngagementResolver` | **Partial** — consume + abort covered; catalog magazine chains **P1** |
| TR-logistics-002 | `MagazineChange` rows in chronological order log | `SimulationSession`, `DecisionLog.AppendMagazineChange` | **Covered** — `MagazineChangeOrderLogTests`, replay goldens |
| TR-logistics-003 | Deterministic fuel burn per tick | `FuelLedger`, `FuelTimelineTracker` | **Partial** — constant burn + band transitions; throttle/altitude **P1** |
| TR-logistics-004 | Mission editor strike fuel validation aligned with sim | `ReachabilityCalculator`, `ScenarioValidationEngine` | **Partial** — `STRIKE_UNREACHABLE` / `STRIKE_UNREACHABLE_FUEL` ship; full flight-plan parity **P1** |

## GitNexus (2026-06-08)

| Symbol | Risk | Action before edit |
|--------|------|-------------------|
| `SimulationSession` | **HIGH** | Readiness priming in `PrimeEngageWorld` sets `AirOperationsReady` from `UnitReadiness`; magazine rows appended post-launch. Run `npx gitnexus impact --repo cmano-clone -d upstream SimulationSession` and replay goldens after changes. |
| `DelegationBridge` | **HIGH** | Owns `FuelTimelineTracker` drain per tick; run PlayMode 7/7 after fuel/logistics edits. |
| `MvpEngagementResolver` | **MEDIUM** | Magazine consume + `AIR_NOT_READY` gate; coordinate with engage GDD. |
| `MagazineLedger` | **LOW** | Pure sim state; test via `ProjectAegis.Sim.Tests`. |

## Open Questions

1. **Catalog magazine chains:** When does `engage.defaultMagazineRounds` yield to live Platform DB mount counts? **Deferred P1** (tracker row 16).
2. **Composite readiness score:** Replace `ReadyForLaunch` bool with `readinessScore` and `readinessMin`/`readinessWarn` scenario knobs? **Deferred P1**.
3. **UNREP / at-sea re-arm:** Scenario feature flag + time delay model? **Deferred P1** (doc 16 § Boat ops).
4. **Editor ↔ sim fuel parity:** Should `ReachabilityCalculator` call `FuelLedger` directly or share a extracted sustainment library? **ADR-006** alignment study.
5. **Magazines C2 panel:** Dedicated UI vs unit-detail lines only? **Deferred P1** (doc 20).