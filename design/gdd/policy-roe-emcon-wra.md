# Policy, ROE, EMCON, and WRA

> **Status:** In Review  
> **Author:** design-system + requirements 13  
> **Last Updated:** 2026-05-29  
> **Last Verified:** 2026-05-29  
> **Implements Pillar:** Simulation fidelity, Agentic command  
> **Requirements:** `Game-Requirements/requirements/13-Doctrine-ROE-EMCON-WRA.md`  
> **GitNexus:** `IRoeFilter` upstream impact **HIGH** — coordinate migrations with `DelegationOrchestrator`

> **Quick reference** — Layer: **Foundation (Core)** · Priority: **MVP** · Key deps: Simulation Core, Order Log · Depended on by: Engagement, Sensors, Delegation, C2 UI

## Summary

Theater commanders and scenario designers set **rules of engagement**, **weapon release authority**, **emissions control**, and **behavioral doctrine** that apply equally to humans and AI agents. Policy is evaluated in a **fixed, deterministic order**, and every blocked weapon or sensor action produces an explainable **FireAbortReason** logged for replay and briefing.

## Overview

In Project Aegis, policy is not a hidden difficulty modifier. It is a visible **contract**: side defaults flow down through missions and formations to individual units, with overrides tracked by **provenance**. When the player delegates a unit to an agent, the game freezes an immutable **Policy Snapshot** for that assignment; agents may be cautious or aggressive only within legal options—the rules do not change per personality.

## Player Fantasy

You set the rules of war for your theater—when forces may illuminate, fire, jam, or withdraw—and those rules bind your AI staff officers the same way they bind you. When something does not shoot, you get a staff answer, not a mystery.

## Detailed Design

### Core Rules

1. **Inheritance resolution** runs once per unit per tick (cached) in this order: unit override → embarked host → mission → group → side → scenario default. “Inherit” leaves the parent value; explicit values override.
2. **Effective policy** is the merged result exposed to UI, MCP, agents, and engagement pipeline—never only the local tab.
3. **ROE** selects among `HoldFire`, `WeaponsTight`, `WeaponsFree` (extensible). Each maps to gates: observe, illuminate, designate, fire ballistic, fire guided, jam, etc.
4. **WRA** applies per mount class or weapon category: max salvo, min/max range band, allowed target categories (air, surface, sub, land facility).
5. **EMCON** sets each emitter class on a unit: `Off`, `Passive`, `Active` for radar, sonar, ESM, datalink, OECM (subset per platform DB).
6. **Doctrine** sets behavioral flags: ignore plotted course, engage opportunity targets outside mission, withdraw when damaged, bingo fuel return, nuclear release (scenario flag only).
7. **Policy Snapshot** is created when an `AgentController` is assigned to a target; contains full effective policy + `policySnapshotId` + `capturedAtSimTick`.
8. **Policy updates** during play (player, event, or approved agent proposal) emit `PolicyUpdate` order-log entries; replay includes them.
9. **Autonomy gate** (existing `AutonomyGate`) runs **after** policy legality: illegal orders are `Rejected` before Manual/Assisted/Autonomous branching.
10. **Agents** must not emit orders that fail policy; Assisted mode surfaces block reason before execution.

### States and Transitions — EMCON (per emitter class)

| State | Entry | Exit | Behavior |
|-------|-------|------|----------|
| Off | Player/designer/command | Passive or Active | No emission; passive sensors may still receive |
| Passive | From Off or Active | Off or Active | Listen-only where applicable (ESM, passive sonar) |
| Active | From Off or Passive | Off or Passive | Full transmit/illuminate per sensor type |

Doctrine and ROE do not use FSM at MVP—they are resolved structs. Withdraw triggers a **mission suggestion** (Ferry/Support), not an instant teleport.

### Interactions with Other Systems

| System | Direction | Interface |
|--------|-----------|-----------|
| Agent Delegation | Policy → Delegation | `PolicySnapshot` on assign; `AutonomyGate` after `IPolicyEvaluator` |
| Engagement & Fire Control | Policy → Engage | `CanEngage(mount, target) → FireAbortReason?` before DLZ |
| Sensor & Detection | Policy ↔ Sensors | EMCON drives emission; detection reads emitter state |
| Order Log & Replay | Policy → Log | `PolicyDenial`, `PolicyUpdate` entry types |
| Mission Editor | Editor → Policy | `PolicyTemplate` / overrides in scenario package |
| C2 UI | Policy → UI | Effective policy chain, top-3 abort reasons on weapon hover |

## Formulas

### Effective policy resolution

```
effective = merge(scenarioDefault, side, group, mission, embarkedHost, unitOverride)
```

Merge is field-wise; last non-inherit wins. Evaluation order of fields in code is fixed (enum order) for determinism.

### WRA salvo check

```
allowed = (salvoCountThisEngagement < wra.maxSalvo) AND (range in wra.rangeBand) AND (targetCategory in wra.allowedCategories)
```

| Variable | Type | Range | Source |
|----------|------|-------|--------|
| salvoCountThisEngagement | int | 0–N | runtime engage tracker |
| wra.maxSalvo | int | 1–100 | policy snapshot |
| range | float | 0–∞ m | geometry |
| targetCategory | enum | — | contact classification |

### Policy evaluation order (per tick, per action)

```
1. Resolve effective policy (cached per unitId)
2. ROE gate
3. WRA gate (if engage)
4. EMCON gate (if illuminate/jam)
5. Doctrine gate (withdraw, opportunity engage)
6. AutonomyGate (Manual/Assisted/Autonomous)
```

## Edge Cases

| Scenario | Expected Behavior | Rationale |
|----------|-------------------|-----------|
| Side weapons tight, mission weapons free | Mission wins for assigned units | Specificity |
| Hold fire + agent Aggressive | No shots; log ROE_HOLD_FIRE | Personality ≠ rules |
| Radar passive + active illuminate order | Deny; EMCON_RADAR_PASSIVE | EMCON |
| Third missile same engagement, max salvo 2 | Deny; WRA_MAX_SALVO | WRA |
| Player changes ROE mid-fight | PolicyUpdate logged; new eval next tick | Auditable |
| Event changes posture to hostile | ROE gates update; log SidePostureChange | Scenario |
| Embarked helo inherits ship EMCON unless overridden | Host chain in inheritance | Naval ops |
| Assisted + illegal agent intent | Show reason; no execution | Doc 04 |
| PassthroughRoeFilter in tests | Allow all; production uses real evaluator | Test seam |
| Re-delegate same unit | New snapshotId; old log entries keep old id | Attribution |
| Nuclear fire without scenario flag | Deny; DOCTRINE_NUCLEAR_DISABLED | Scope |
| Strike mission + hold fire ROE | Validation blocker at export | Doc 11 |

## Dependencies

| System | Direction | Nature |
|--------|-----------|--------|
| Simulation Core & Time | Depends on | Fixed tick, sim time for snapshot |
| Order Log & Replay | Depends on | Log entry types |
| Engagement & Fire Control | Depended on by | Pre-engage checks |
| Sensor & Contact Model | Depended on by | EMCON emissions |
| Agent Delegation | Mutual | Snapshot + gate ordering |
| Command & Control UI | Depended on by | Doctrine tabs, explain |
| Scenario & Mission Editor | Depended on by | Authoring source of scenario policy ids + doctrine inheritance |

**Upstream GDD gaps:** Simulation Core and Order Log GDDs provisional—contracts defined here and in `docs/architecture/architecture.md`.

## Tuning Knobs

| Parameter | MVP Default | Safe Range | Increase Effect |
|-----------|-------------|------------|-----------------|
| `wra.defaultMaxSalvo` | 2 | 1–8 | More missiles per engagement |
| `policy.cacheTicks` | 1 | 1–4 | Staleness vs CPU |
| `roe.weaponsTightIlluminate` | false | bool | More restrictive tight ROE |
| `withdraw.damageThreshold` | 0.4 | 0.2–0.8 | Earlier withdraw suggestions |
| `emcon.scheduleEnabled` | false | bool | Phase 2 mission EMCON |

## Visual/Audio Requirements

| Event | Visual | Audio | Priority |
|-------|--------|-------|----------|
| Policy denial | Weapon greyed + tooltip | Short deny tone | P0 |
| ROE change | Side panel flash | — | P1 |
| EMCON change | Emitter icon on unit | — | P0 |

## Acceptance Criteria

1. Mission-level weapons free overrides side weapons tight for assigned strikers only.
2. Cautious agent under hold fire produces zero engage orders; log contains `ROE_HOLD_FIRE`.
3. Passive radar blocks illuminate with `EMCON_RADAR_PASSIVE` in message and order log.
4. Fourth shot in same engagement blocked when `maxSalvo = 2`.
5. Two headless runs, same seed and snapshots → identical denial sequence (order log hash).
6. `policy_explain_fire` MCP returns field reference matching UI tooltip.

## Technical Notes (Implementation)

| Component | Location today | Target |
|-----------|----------------|--------|
| `IRoeFilter` | `ProjectAegis.Delegation.Roe` | Wrap or replace with `IPolicyEvaluator` |
| `AutonomyGate` | `Orchestration` | Unchanged ordering; feed after policy |
| `PassthroughRoeFilter` | Tests / default orchestrator | Keep for tests only in prod path |
| `PolicySnapshot` | New | `ProjectAegis.Sim.Policy` or Delegation until Sim exists |

Run before merge: `npx gitnexus impact --repo cmano-clone -d upstream IRoeFilter`

## TR IDs (for architecture traceability)

| ID | Requirement |
|----|-------------|
| TR-policy-001 | Inheritance resolution order fixed and cached |
| TR-policy-002 | Policy Snapshot on agent assign |
| TR-policy-003 | FireAbortReason on all denials |
| TR-policy-004 | PolicyUpdate in order log |
| TR-policy-005 | WRA before engagement geometry |
| TR-policy-006 | EMCON feeds sensor emission model |
