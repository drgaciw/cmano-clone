# Logistics & Magazines

> **Status:** Approved  
> **Last Updated:** 2026-06-01  
> **Requirements:** [16-Logistics-And-Magazines.md](../../Game-Requirements/requirements/16-Logistics-And-Magazines.md)  
> **Depends on:** [simulation-core-time.md](simulation-core-time.md), [engagement-fire-control.md](engagement-fire-control.md)  
> **Depended on by:** [agentic-mission-editor.md](agentic-mission-editor.md) (validation), system 17 Scoring

> **Quick reference** — Layer: **Core** · Priority: **MVP** · System #7

## Overview

Logistics governs **fuel**, **magazines**, **readiness**, and **sortie generation** so engagements and missions fail for sustainment reasons—not arbitrary timers. Runtime consumption is **deterministic** and logged; authoring validation reuses the same formulas as the sim (doc 11 Validation Engine).

## Player Fantasy

You win or lose campaigns because magazines empty, tankers are late, and decks cannot generate sorties—not because the UI hid ammo counts. When a strike package is impossible, you hear it at planning time and see it in the message log at execution time.

## Detailed Rules

### Magazines (P0)

1. Platform DB (doc 06) defines magazine → mount → weapon chains with count, reload time, and compatible stores.
2. **Consumption** occurs in the engagement pipeline (doc 14) via `MagazineLedger.TryConsume` — already MVP in `MvpEngagementResolver`.
3. Each launch emits a `MagazineChange` order-log row: `{shooterId, mountId, delta, reason: fire|reload|transfer}`.
4. UI shows magazine %, ready count, reload progress (doc 20).

### Fuel & endurance (P0)

1. Fuel types per domain (aviation JP, naval F76, etc.) from DB.
2. Burn rate = `baseBurn * throttleFactor * altitudeFactor` (MVP: throttle only; altitude P1).
3. States: `Normal`, `Joker`, `Bingo` — warnings in message log at thresholds from scenario data.
4. Ferry missions (doc 11) move aircraft between bases; validation checks reachability + return fuel.

### Air operations (P0)

States: `OnGround | Taxiing | TakingOff | Airborne | Landing | Maintenance`. Ready aircraft count and sortie rate constrain mission assignment. Carrier deck cycle is data-driven (turnaround seconds per aircraft type).

### Readiness (P0)

`readinessScore = min(fuelNorm, ammoNorm, damageNorm, maintenanceNorm)` in `[0,1]`. Mission assign blocked when `< readinessMin` (scenario tunable); advisory when `< readinessWarn`.

### Integration

| Consumer | Uses logistics for |
|----------|-------------------|
| Engagement | Magazine empty → `MagazineEmpty` abort |
| Mission editor | Strike fuel/magazine validation |
| Order log | MagazineChange, FuelStateChange |
| Scoring | Expenditures (doc 17) |

## Formulas

| Symbol | Meaning | Range |
|--------|---------|-------|
| `M0` | Magazine capacity (rounds) | > 0 |
| `M(t)` | Rounds after tick t | [0, M0] |
| `F(t)` | Fuel remaining (kg) | [0, F0] |
| `burn` | `baseBurnRate * throttle` | throttle ∈ [0,1] |
| `R` | Readiness | [0,1] |

**Magazine after fire:** `M' = M - 1` if launch allowed; else unchanged.

**Fuel per tick:** `F' = F - burn * deltaSeconds`.

**Example:** M0=48, 2 fires → M=46; F0=10_000 kg, burn=2 kg/s, Δt=60 → F=9_880.

## Edge Cases

| Case | Behavior |
|------|----------|
| Salvo > rounds left | Fire up to `M` rounds; remainder `MagazineEmpty` per mount |
| Reload in progress | Mount not `ready`; engage abort `MountNotReady` |
| Simultaneous fires same mount | Deterministic order by `shooterId` then `mountId` |
| UNREP disabled in scenario | Re-arm at sea blocked; log advisory only |
| Zero fuel | Unit `Bingo`; movement orders auto-suggest RTB (delegation policy P1) |

## Dependencies

- **Requires:** Simulation Core (tick), Platform DB (doc 06), Engagement (magazine consume), Order Log (entries).
- **Required by:** Mission Editor validation, Scoring & Losses, C2 UI magazines panel.

## Tuning Knobs

| Knob | Safe range | Effect |
|------|------------|--------|
| `readinessMin` | 0.2–0.8 | Mission block threshold |
| `jokerFuelFrac` | 0.15–0.35 | Warning threshold |
| `bingoFuelFrac` | 0.05–0.15 | RTB pressure |
| `defaultReloadSeconds` | 30–600 | Mount turnaround |
| `burnRateMultiplier` | 0.5–2.0 | Scenario difficulty |

Data path: `assets/data/logistics_*.json` (future); MVP uses scenario engage block + `MagazineLedger` defaults.

## Acceptance Criteria

1. **AC-1:** Given magazine count 2, two legal engages consume to 0; third engage logs `MagazineEmpty` and does not launch.
2. **AC-2:** `MagazineChange` appears in `DecisionLog.ChronologicalEntries()` with deterministic ordering.
3. **AC-3:** Fuel decreases per tick with fixed seed; replay fingerprint unchanged when logistics disabled (feature flag off).
4. **AC-4:** Mission validation rejects strike package when `fuelRequired > fuelAvailable` (editor, doc 11) with code `STRIKE_UNREACHABLE_FUEL`.
5. **AC-5:** Readiness `< readinessMin` blocks mission assign in editor (advisory or hard per scenario strictness).