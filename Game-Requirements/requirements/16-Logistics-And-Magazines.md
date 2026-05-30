# 16 - Logistics, Magazines, and Platform Operations

**Last Updated:** May 29, 2026  
**Status:** Draft — ready for design review  
**CMO basis:** Manual §3.3.4–8, §3.3.6, §4.5.4–5, §6.3.13, §7.2.1–2 (ferry/support)  
**Related:** 11 Mission Editor, 14 Engagement, 18 Combat Domains, 06 Database Intelligence, 17 Order Log

## Purpose

Define **fuel**, **magazines**, **air and boat operations**, **readiness**, and **sustainment** rules that constrain missions and engagements — with **validation** at authoring time and **deterministic** consumption at runtime.

## Vision

Wars are won by logistics as much as firepower. Swarm-heavy 2030s scenarios must fail believably when magazines empty or tankers are mispositioned. The Validation Agent (doc 11) and player see the same arithmetic.

## CMO Parity Requirements

| Capability | CMO | Aegis |
|------------|-----|-------|
| Magazines UI | §3.3.6 | **P0** |
| Air operations (ready, takeoff, recovery) | §3.3.7 | **P0** |
| Boat ops / UNREP | §3.3.8 | **P1** |
| Throttle / altitude / fuel | §3.3.4, §4.5.4–5 | **P0** |
| Ferry / support missions | §7.2.1–2 | **P0** |
| Losses and expenditures | §6.3.13 | **P0** — feeds doc 17 |

## Magazines and Munitions

**P0** Magazine → mount → weapon chain from DB (doc 06).

- Count, reload time, compatible stores, depth (e.g., VLS cells)
- **Consumption** on launch via engagement pipeline (doc 14)
- **P0** `MagazineChange` order log: fire, reload complete, transfer from reserve

**P0** UI: magazine %, weapon ready count, reload progress.

**P1** Re-arm at base / UNREP with time delay and scenario feature flag.

### Swarm / near-future (doc 09)

- **P0** Mass expendable munitions: burn-rate metric per mission
- **P1** Forward arming and reload trucks (land) with capacity limits

## Fuel and Endurance

**P0** Fuel types per platform domain (aviation JP, naval, etc.).

- Burn rate vs throttle, altitude, speed (simplified curves MVP)
- **Bingo / joker** fuel states with warnings in message log
- **P0** Ferry missions move aircraft between bases (doc 11)

**P0** Validation Agent: strike package cannot reach target and return without tanker plan (advisory or blocker per scenario strictness).

## Air Operations

**P0** States:

```
OnGround | Taxiing | TakingOff | Airborne | Landing | Maintenance
```

- Ready a/c count, sortie generation rate, deck cycle for carriers
- **P0** Support mission: tanker, AEW, EW orbit (doc 11)
- **P0** Airbase capacity and runway damage (links doc 18 facilities)

**P0** Flight plan preview in editor: ETA, refuel segments, bingo (doc 11).

## Boat and Naval Operations

- **P1** UNREP for fuel and limited rearm
- **P0** Docked / underway replenishment affects readiness flags
- **P0** Submarine battery/charge model (simplified)

## Readiness

**P0** Readiness aggregates:

- Fuel, ammo, damage (doc 18), crew rest (P2), maintenance hours

**P0** Mission assignment blocked or advisory when readiness below threshold.

## Functional Requirements — Validation

| Check | When | Severity |
|-------|------|----------|
| Empty magazine on assigned striker | Export scenario | Blocker |
| No tanker on long strike | Export | Advisory (configurable) |
| Airbase capacity exceeded | Export | Blocker |
| Ferry without destination | Export | Blocker |
| Projected sorties &gt; physical capacity | Quick run | Advisory |

## Agent integration

- Agents respect bingo and WRA; **Cautious** returns strike assets earlier
- **Swarm Coordinator** tracks expendable inventory across sub-swarms
- **P0** Agent cannot order takeoff if readiness fails — `LogisticsAbortReason` enum (parallel to FireAbortReason)

## Non-Functional Requirements

| Area | Target |
|------|--------|
| Determinism | Fuel/magazine deltas reproducible per tick |
| Performance | Aggregate logistics for groups; detail on demand |
| UI | 5k units: magazine summary LOD at map zoom |

## MCP Tools

| Tool | Description |
|----------|-------------|
| `logistics_get_readiness` | Unit readiness breakdown |
| `logistics_project_sorties` | Sorties until bingo for mission |
| `magazine_get` | Stores and counts |

## Acceptance Criteria

1. Launch decrements correct magazine; reload completes at deterministic tick.
2. Strike without tanker triggers validation advisory in Baltic tutorial scenario.
3. Carrier deck cycle limits sorties per hour in test scenario.
4. Ferry mission moves squadron; fuel at destination matches projection within tolerance.
5. Losses/expenditures report (doc 17) matches magazine consumption totals.
6. Agent aborts takeoff when readiness &lt; threshold with logged reason.

## Phased Delivery

| Phase | Scope |
|-------|--------|
| **MVP** | Magazines, fuel bingo, air ready/launch, ferry/support linkage |
| **Phase 2** | UNREP, rearm, runway damage, swarm burn metrics |
| **Phase 3** | Crew rest, detailed maintenance |

## Open Questions

1. Unified vs per-munition magazine modeling for VLS?
2. Instant rearm in editor test only?
3. Cargo mission logistics (doc 11 P1) — same doc or separate?

## Traceability

| Doc | Relationship |
|-----|----------------|
| 11 | Mission validation, flight preview |
| 14 | Magazine on fire |
| 17 | Expenditures |
| 18 | Base damage |
| `cmo-manual-traceability.md` | §3.3.4–8, §6.3.13 |

---

**References:** CMO Manual §3.3.6–8; `docs/manual/index.html`
