# Engagement & Fire Control

> **Status:** In Review  
> **Author:** design-system  
> **Last Updated:** 2026-06-08  
> **Last Verified:** 2026-06-08 @ `afd2e1a`  
> **Implements Pillar:** Simulation fidelity, Operator explainability  
> **Requirements:** [14-Engagement-And-Fire-Control.md](../../Game-Requirements/requirements/14-Engagement-And-Fire-Control.md)  
> **Depends on:** [policy-roe-emcon-wra.md](policy-roe-emcon-wra.md), [sensor-detection-ew.md](sensor-detection-ew.md), [logistics-magazines.md](logistics-magazines.md), [order-log-replay.md](order-log-replay.md)  
> **Depended on by:** [combat-domains-damage.md](combat-domains-damage.md), [command-and-control-ui.md](command-and-control-ui.md), [scoring-losses.md](scoring-losses.md)

> **Quick reference** — Layer: **Core** · Priority: **MVP** · System #6 · Key deps: Policy, Sensors, Logistics · Depended on by: Combat, C2 UI, Scoring

## Summary

One **engagement resolver** (`IEngagementResolver` / `MvpEngagementResolver`) handles player attack-menu commits, agent `OrderKind.Engage` intents, and mission auto-fire. Policy and comms gates run first; geometry, DLZ, and magazines second; every abort emits a stable glossary code in the order log. Wave 5 ships the headless attack menu (`EngageAttackOptions` → `DelegationBridge`); swarm **sector coordinator** remains **P1 deferred**.

## Overview

`ProjectAegis.Sim.Engage` owns the deterministic fire-control pipeline. `SimTickPipeline` invokes the resolver at **ADR-004 step 8** after detection and delegation intent collection. `ProjectAegis.Delegation` projects live engage context for the C2 attack menu and enqueues `EngageRequest` objects into the sim session—there is no bypass path for agents or humans.

**MVP on `main` @ `afd2e1a`:** policy denials (`RoeHoldFire`, `WraSalvo`, `EmconOff`, …), magazine consumption, DLZ personality gating, air-readiness abort, spoof-track abort, deterministic swarm slot deconfliction (`SwarmSalvoDeconfliction`), and Baltic replay golden engage fingerprint.

## Player Fantasy

You pick a weapon and see why it will or will not fire before you commit—and when an AI wingman holds fire, you get the same staff answer you would. The attack menu greys out impossible shots with the same abort code the resolver will log if you force the issue.

## Detailed Design

### Core Rules

1. **Single resolver** — All engage paths call `IEngagementResolver.Resolve(in EngageRequest)`; no parallel “UI-only” fire logic.
2. **Fixed gate order** — Abort evaluation order in `MvpEngagementResolver` is stable for determinism (see pipeline table).
3. **Intent sources** — Player (`PlayerOrder` / attack menu), agent (`OrderKind.Engage` via orchestrator), mission auto (`intentSource: mission` in log payload).
4. **Preview before commit** — `EngagePreviewProjection` mirrors resolver DLZ/envelope/track gates for the unit panel and attack menu.
5. **Order log contract** — Every resolve produces an `EngagementRecord` with `Launched` or `EngagementAbortReasonCodes.ToLogCode(abort)`.
6. **Magazine coupling** — Successful launch calls `MagazineLedger.TryConsumeSalvo`; empty magazine aborts before launch (`MagazineEmpty`).
7. **Swarm deconfliction (P0)** — `SwarmSalvoDeconfliction.Allocate` grants one shooter per target per tick, sorted by `(shooterId, targetId, weaponId)`.
8. **Swarm coordinator (P1 — deferred)** — Sector-based multi-shooter assignment across sub-swarms; not implemented; sequential slot order is the P0 stand-in.
9. **Comms guard** — `SimulationSession` blocks new engages when comms `Denied`, logging `FireAbortReason.CommsDenied` before the resolver runs.
10. **Combat outcomes** — Post-launch hit/intercept/kill rolls use seeded `CombatOutcomeResolver` (doc 18); abort reasons apply only pre-launch.

### Engagement Pipeline

```
Intent (player | agent | mission)
  → [Delegation] Comms guard → SwarmSalvoDeconfliction slot accept
  → SimTickPipeline step 8: IEngagementResolver.Resolve
      → killed-target check
      → world context (IEngageWorldQuery)
      → speculative gate (scenario TRL / black-project)
      → IPolicyEvaluator (ROE / WRA / EMCON map)
      → air-ops readiness
      → spoof-track
      → radar EMCON active
      → fire-control track
      → magazine pre-check
      → combat-domain validator
      → hypersonic gate
      → envelope Contains(range)
      → DlzEngageGate.AllowsLaunch(personality)
      → TryConsumeSalvo
      → launch + CombatOutcomeResolver chain
  → EngagementRecord + MagazineChange (on launch) → order log
```

| Step | Gate | Fail reason (log code examples) |
|------|------|----------------------------------|
| 0 | Comms `Denied` | `CommsDenied` (delegation, pre-resolver) |
| 0 | Swarm slot lost | silent skip (no duplicate target allocation) |
| 1 | Target killed | `TargetDestroyed` |
| 2 | No world context | `NO_FIRE_CONTROL_TRACK` |
| 3 | Speculative scenario | `TECHNOLOGY_LEVEL_EXCEEDED`, `BLACK_PROJECT_REQUIRED` |
| 4 | Policy | `ROE_HOLD_FIRE`, `WRA_SALVO`, `EMCON_OFF`, `WEAPONS_TIGHT` |
| 5 | Air ops | `AIR_NOT_READY` |
| 6 | Cyber spoof | `CYBER_SPOOF_TRACK` |
| 7 | EMCON | `EMCON_OFF` |
| 8 | Track quality | `NO_FIRE_CONTROL_TRACK` |
| 9 | Magazine | `MAGAZINE_EMPTY` |
| 10 | Combat domain | `MOUNT_OFFLINE`, `DOMAIN_NO_SOLUTION` |
| 11 | Hypersonic defense | `DOMAIN_NO_SOLUTION` |
| 12 | Envelope | `OUT_OF_ENVELOPE` |
| 13 | DLZ + personality | `DLZ_OUT` |
| 14 | Salvo consume | `MAGAZINE_EMPTY` |

### Intent Sources

| Source | Entry path | Log tag |
|--------|------------|---------|
| Player | `DelegationBridge.TryEnqueueAttackOption` → `PlayerOrderRecord` → `OrderKind.Engage` | `PlayerOrder` |
| Agent | `AgentController` intent → `AutonomyGate` → `OrderKind.Engage` | `AgentIntent` + `policySnapshotId` |
| Mission auto | Mission runtime prosecution (doc 11) → same orchestrator queue | `intentSource: mission` |

### Attack Menu (Wave 5 — shipped headless)

| Component | Role |
|-----------|------|
| `EngagePreviewProjection` | Builds `EngagePreview` (DLZ label, `CanFire`, top abort code) |
| `EngageAttackOptions.Build` | `fire-single`, `fire-salvo`, `hold-fire` entries with `DisabledReason` |
| `EngageAttackOrderResolver.TryResolve` | Maps option id → `OrderKind.Engage` + salvo override or `Hold` |
| `DelegationBridge.GetAttackMenuOptions` | Live menu for `UnitDetailBridge` / C2 panel |
| `DelegationBridge.TryEnqueueAttackOption` | Commits player order; sets `Session.NextEngageSalvoOverride` |

Disabled menu entries surface the same glossary string the resolver would log (e.g. `DLZ_OUT`, `CYBER_SPOOF_TRACK`, `NO_FIRE_CONTROL_TRACK`).

### DLZ (MVP)

State per `(shooter, target, weapon)` from `DlzEvaluator.Evaluate(range, envelope)`:

| State | Condition (MVP bands) |
|-------|------------------------|
| `OutOfZone` | `range < min×0.9` or `range > max` |
| `Approaching` | `max×0.85 < range ≤ max` |
| `InZone` | otherwise inside envelope |
| `Unknown` | reserved when envelope data missing (catalog fallback) |

**Personality launch policy** (`DlzEngageGate.AllowsLaunch`):

| Personality | Fires when state is |
|-------------|---------------------|
| `Early` | `InZone` or `Approaching` |
| `Normal` | `InZone` only |
| `Late` | `InZone` and `range ≤ max×0.7` |

Preview UI shows `DLZ: {state} ({personality})`; resolver uses scenario `DlzPersonality` from `ScenarioEngageDefaults`.

### Swarm Fire Control

| Tier | Behavior | Status |
|------|----------|--------|
| **P0** | `SwarmSalvoDeconfliction` — first claimant per `targetId` after sort by `shooterId` | **Implemented** |
| **P1** | Swarm Coordinator — sector slots, fire distribution across 50+ shooters | **Deferred** (documented gap) |

## Formulas

### Envelope

```
inEnvelope = (range >= minRange) AND (range <= maxRange)
```

`WeaponEnvelope.Contains(range)` is the resolver gate; DLZ bands use separate multipliers on `maxRange`.

### DLZ state

```
if range < minRange × 0.9        → OutOfZone
else if range > maxRange         → OutOfZone
else if range > maxRange × 0.85  → Approaching
else                             → InZone
```

### DLZ launch permission

```
allowsLaunch = personalityPolicy(dlzState, range, envelope, personality)
```

Where `personalityPolicy` is the table in **DLZ (MVP)** above.

### Salvo consumption

```
roundsToFire = max(1, salvoSize)
launchOk = MagazineLedger.TryConsumeSalvo(shooterId, mountId, roundsToFire)
```

| Symbol | Meaning | Source |
|--------|---------|--------|
| `range` | Shooter–target slant range (m) | `EngageContext.RangeMeters` |
| `minRange`, `maxRange` | Weapon envelope | Catalog / `ScenarioEngageDefaults` |
| `salvoSize` | WRA salvo for mount | Policy + `EngageContext.SalvoSize` |
| `personality` | Early / Normal / Late | Scenario `engageDefaults.DlzPersonality` |

### Worked Example

**Setup:** SAM vs inbound contact; `WeaponEnvelope(1_000, 100_000)`; `range = 90_000 m`; `DlzPersonality.Early`; track valid; policy free; magazine ≥ salvo.

1. **Envelope:** `90_000 ∈ [1_000, 100_000]` → pass.
2. **DLZ state:** `90_000 > 85_000` (`max×0.85`) → `Approaching`.
3. **Personality:** `Early` allows `Approaching` → `allowsLaunch = true`.
4. **Consume:** `TryConsumeSalvo(shooter, mount, salvo)` → success.
5. **Outcome:** `EngageResult.Launch(engagementId)`; log code `Launched`.

**Contrast:** Same inputs with `DlzPersonality.Normal` → `Approaching` not allowed → abort `DLZ_OUT` (verified in `MvpEngagementResolverTests` and `MvpEngagementDlzPersonalityTests`).

## Edge Cases

| Scenario | Expected Behavior | Rationale |
|----------|-------------------|-----------|
| Policy pass, DLZ fail | Log `DLZ_OUT`; no launch; menu `fire-single` disabled with same code | Preview/resolver parity |
| Agent engage without fire-control track | Abort `NO_FIRE_CONTROL_TRACK`; `EngagementOrderLogContractTests` | Sensor gating |
| Radar EMCON off with otherwise valid track | Abort `EMCON_OFF` before geometry | Policy/emission order |
| Comms `Denied` during fight | `CommsDenied` policy denial; in-flight shots complete; new engages blocked | `baltic-patrol-comms` replay |
| Track spoofed (cyber) | Abort `CYBER_SPOOF_TRACK`; attack menu disables fire | `DelegationBridgeAttackOptionTests` |
| Target already in `KilledTargetRegistry` | Abort `TargetDestroyed` without magazine spend | Idempotent re-engage |
| Salvo request exceeds rounds | Menu disables `fire-salvo` with `NO_AMMO`; resolver aborts `MAGAZINE_EMPTY` | Logistics coupling |
| Two agents same target same tick | Lower `shooterId` wins slot; other order skipped silently | `SwarmSalvoDeconfliction` determinism |
| Air unit not ready (`AirNotReady`) | Abort before DLZ; readiness ties to logistics GDD | `MvpEngagementAirNotReadyTests` |
| Speculative weapon above scenario TRL | Abort `TECHNOLOGY_LEVEL_EXCEEDED` | Scenario speculative gate |
| Mid-flight target lost | Outcome phase applies miss/intercept per doc 18; no re-abort at launch | Post-launch domain |

## Dependencies

| System | Direction | Nature |
|--------|-----------|--------|
| Simulation Core & Time | Depends on | `SimTickPipeline` step 8; `simTick` on `EngageRequest` |
| Policy, ROE, EMCON, WRA | Depends on | `IPolicyEvaluator` before geometry; maps to `EngagementAbortReason` |
| Sensor & Contact Model | Depends on | Fire-control track, range, spoof flags in `EngageContext` |
| Logistics & Magazines | Depends on | `MagazineLedger` consume; `AirOperationsReady` |
| Order Log & Replay | Depended on by | `EngagementRecord`, `PlayerOrder`, `PolicyDenial` rows |
| Agent Delegation | Mutual | Orchestrator queues engages; session primes `IEngageWorldQuery` |
| Command & Control UI | Depended on by | Attack menu, engage preview, DLZ label |
| Combat Domains & Damage | Depended on by | `CombatDomainValidator`, outcome resolver |
| Cyber & Comms Degradation | Depends on | Comms guard + spoof-track abort |
| Scoring & Losses | Depended on by | Kill/outcome lines in replay fingerprint |

**Upstream GDD contracts:** Policy inheritance and `FireAbortReason` namespace in [policy-roe-emcon-wra.md](policy-roe-emcon-wra.md); magazine ledger in [logistics-magazines.md](logistics-magazines.md).

## Tuning Knobs

| Knob | MVP Default | Safe Range | Increase Effect |
|------|-------------|------------|-----------------|
| `engageDefaults.DlzPersonality` | `Normal` | Early / Normal / Late | Earlier shots inside envelope band |
| `engageDefaults.DefaultMagazineRounds` | scenario fallback | 1–64 | More shots before `MAGAZINE_EMPTY` in MVP fixtures |
| `envelope.MinRangeMeters` | catalog | 0–50 km | Larger no-fire inner zone |
| `envelope.MaxRangeMeters` | catalog | 1–500 km | Longer max engagement range |
| `ctx.SalvoSize` / WRA max salvo | 1–2 | 1–8 | Larger ripple per commit |
| `ctx.PkBase` / `PkKill` | 0.85 / 1.0 | 0–1 | Higher kill rate (balance) |
| `speculative.technologyLevelCap` | scenario | 0–5 | Blocks near-future weapons when exceeded |
| DLZ approaching band | `max×0.85` | 0.7–0.95 | Wider `Approaching` window |

Data path: `ScenarioEngageDefaults` in scenario JSON; weapon envelopes from `ICatalogReader` / `CatalogEngageEnvelope`.

## Acceptance Criteria

1. **AC-1 (unified resolver):** Given identical seed and `EngageRequest` queue, agent-driven and manually queued engages produce the same `Sim.LastEngagementResults` sequence.  
   **Evidence:** `MvpEngagementResolverTests`; `EngagementOrderLogContractTests`; `ReplayGoldenBalticEngageTests` (`replay-golden-baltic-engage-2026-06-02.txt`).

2. **AC-2 (abort logging):** Every failed resolve appends an `EngagementRecord` whose `AbortReasonCode` equals `EngagementAbortReasonCodes.ToLogCode(abort)` and appears in `DecisionLog.ComputeFingerprint()`.  
   **Evidence:** `EngagementOrderLogContractTests.Mvp_session_logs_stable_abort_codes_in_fingerprint`; glossary manifest round-trip in `AbortReasonManifestTests`.

3. **AC-3 (DLZ preview parity):** When `EngagePreviewProjection` reports `CanFire = false` with code `DLZ_OUT`, resolver aborts `DlzOut` for the same `EngageContext` and personality.  
   **Evidence:** `MvpEngagementDlzPersonalityTests` (Early launches in Approaching); `EngageAttackOptionsTests` (disabled reason propagation).

4. **AC-4 (attack menu commit):** `DelegationBridge.TryEnqueueAttackOption("fire-single")` appends `PlayerOrder` with `OrderKind.Engage`; spoofed track disables fire with `CYBER_SPOOF_TRACK`.  
   **Evidence:** `DelegationBridgeAttackOptionTests`; `EngageAttackOrderResolverTests`.

5. **AC-5 (policy + magazine denials):** ROE hold-fire, WRA salvo cap, and empty magazine each abort without launch and without magazine delta.  
   **Evidence:** `MvpEngagementResolverTests` (`RoeHoldFire`, `WraSalvo`, `DlzOut`, `EmconOff`); `MagazineChangeOrderLogTests`.

6. **AC-6 (swarm slot order — P0):** Duplicate target claims in one tick resolve deterministically by sorted `shooterId`.  
   **Evidence:** `SwarmSalvoDeconflictionTests`. **P1 coordinator:** explicitly deferred; no AC until sector allocator ships.

## Implementation Mapping

| Concept | Type / Location | Notes |
|---------|-----------------|-------|
| Resolver contract | `ProjectAegis.Sim.Engage.IEngagementResolver` | Single entry `Resolve` |
| MVP resolver | `ProjectAegis.Sim.Engage.MvpEngagementResolver` | Full gate chain |
| Tick wiring | `ProjectAegis.Sim.Core.SimTickPipeline` | Step 8; mixes engage into `LastWorldHash` |
| Engage request | `ProjectAegis.Sim.Engage.EngageRequest` | `shooterId`, `targetId`, `mountId`, `simTick` |
| World query | `ProjectAegis.Sim.Engage.IEngageWorldQuery` | `DictionaryEngageWorldQuery` in tests |
| DLZ | `DlzEvaluator`, `DlzEngageGate` | State + personality launch |
| Swarm P0 | `ProjectAegis.Sim.Engage.SwarmSalvoDeconfliction` | Pre-resolver in `SimulationSession` |
| Session bridge | `ProjectAegis.Delegation.Orchestration.SimulationSession` | Comms guard, enqueue, log results |
| Attack menu | `ProjectAegis.Delegation.Projection.EngageAttackOptions` | Static `Build` |
| Order mapping | `ProjectAegis.Delegation.Projection.EngageAttackOrderResolver` | Option id → `OrderKind` |
| Preview | `ProjectAegis.Delegation.Projection.EngagePreviewProjection` | Headless DLZ label |
| Unity bridge | `ProjectAegis.Delegation.UnityAdapter.Bridge.DelegationBridge` | `GetAttackMenuOptions`, `TryEnqueueAttackOption` |
| Abort glossary | `EngagementAbortReasonCodes` + `AbortReasonManifest` | Stable replay strings |
| Test harness | `BalticReplayHarness` | `replay-golden-baltic-engage` fingerprint |

## TR IDs

| ID | Requirement | Implementation Status |
|----|-------------|----------------------|
| TR-engage-001 | Unified resolver for player, agent, mission-auto | **Covered** — `MvpEngagementResolver` + `SimTickPipeline` |
| TR-engage-002 | DLZ state machine + order-log logging | **Partial** — evaluator + preview + abort codes; UI map indicator P1 |
| TR-engage-003 | Swarm slot order / coordinator | **P1 gap** — P0 `SwarmSalvoDeconfliction` only; sector coordinator deferred |

## GitNexus

| Symbol | Blast radius | Action |
|--------|--------------|--------|
| `DelegationBridge` | **CRITICAL** | Run `gitnexus impact` before any sim/bridge edit; attack menu and `PlayerOrder` path route here |
| `EngageAttackOptions` | **LOW** | Projection-only; safe to extend menu labels/options |
| `OrderKind.Engage` | **Verify** | Enum value exists today; **verify before extending** `OrderKind` with new engage variants—prefer payload fields over new enum members |

Recommended pre-merge command:

```
npx gitnexus impact --repo cmano-clone -d upstream DelegationBridge
```

## Open Questions

1. Mount/weapon picker UX (doc 14 §3.3.9): single implicit mount `0` in MVP—when to expose multi-mount selection?
2. DLZ `Unknown` state: catalog missing envelope—block fire or use scenario fallback envelope?
3. P1 Swarm Coordinator: ADR amendment needed for sector graph input?