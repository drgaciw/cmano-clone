# 16 - Logistics, Magazines, and Platform Operations

**Last Updated:** 2026-07-08  
**Status:** Draft — ready for design review  
**FR reverse-ref:** [FR-14](01-Project-Overview.md) — Logistics and magazines  
**CMO basis:** Manual §3.3.4–8, §3.3.6, §4.5.4–5, §6.3.13, §7.2.1–2 (ferry/support)  
**Related:** 11 Mission Editor, 14 Engagement, 18 Combat Domains, 06 Database Intelligence, 17 Order Log  
**Tracker:** [implementation-tracker-2026-07-04.md](../implementation-tracker-2026-07-04.md) row 16 — **Partial**  
**GDD:** [logistics-magazines.md](../../design/gdd/logistics-magazines.md)

## Purpose

Define **fuel**, **magazines**, **air and boat operations**, **readiness**, and **sustainment** rules that constrain missions and engagements — with **validation** at authoring time and **deterministic** consumption at runtime.

Implements hub **[FR-14](01-Project-Overview.md)** (logistics and magazines).

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

**Honesty overlay:** P0/P1 rows = product intent. **Shipped MVP spine:** magazine consume + fuel burn/bands + boolean readiness → `AIR_NOT_READY` + ferry CLI/validation hooks. **Not shipped:** full air-ops FSM, UNREP, live magazines C2 panel, product MCP logistics tools.

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

**Honesty:** Full air-ops FSM above is **product intent**. **MVP shipped:** per-unit `ReadyForLaunch` boolean via `UnitReadinessMap` → engage abort `AIR_NOT_READY`. Full deck-cycle / taxi/landing state machine = **Phase N**.

## Boat and Naval Operations

- **P1** UNREP for fuel and limited rearm → **Phase N** (not on `main` as runtime model)
- **P0** Docked / underway replenishment affects readiness flags → **Partial / Phase N** (boolean readiness only)
- **P0** Submarine battery/charge model (simplified) → **Phase N**

## Readiness

**P0** Readiness aggregates:

- Fuel, ammo, damage (doc 18), crew rest (P2), maintenance hours

**P0** Mission assignment blocked or advisory when readiness below threshold.

**Honesty:** Composite `readinessScore` formula is **Phase N**. MVP = boolean `ReadyForLaunch` + validation `AIR_NOT_READY` + engage gate.

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

## Major IDs (LOG-*)

| ID | Summary | Priority / maturity |
|----|---------|---------------------|
| **LOG-01** | Magazine ledger consume on launch (`TryConsume` / salvo) | **P0** — **Shipped** (`MagazineLedger` + `MvpEngagementResolver`) |
| **LOG-02** | Catalog → magazine seeder (platform loadout rounds) | **P0** — **Partial** (`CatalogMagazineLedgerSeeder`; fallback `defaultMagazineRounds`) |
| **LOG-03** | Fuel burn ledger + joker/bingo band transitions | **P0** — **Shipped** (`FuelLedger`, `FuelTimelineTracker`; opt-in burn model) |
| **LOG-04** | Unit readiness map → `AIR_NOT_READY` engage/validation abort | **P0** — **Shipped** (`UnitReadinessMap`, `UnitReadinessMapFactory`) |
| **LOG-05** | Order-log magazine / fuel change rows | **P0** — **Shipped** (`MagazineChangeRecord`, `FuelStateChangeRecord` / burn records) |
| **LOG-06** | Editor logistics validation (`AIR_NOT_READY`, reachability codes) | **P0** — **Partial+** (`LogisticsValidationRulesTests`, ferry/strike rules) |
| **LOG-07** | UNREP / at-sea re-arm | **P1** — **Phase N** |
| **LOG-08** | Full air-ops FSM + product MCP (`logistics_*` / `magazine_get`) | **P0 intent** — **Phase N / Gap** |

## Non-Functional Requirements

| Area | Target |
|------|--------|
| Determinism | Fuel/magazine deltas reproducible per tick |
| Performance | Aggregate logistics for groups; detail on demand |
| UI | 5k units: magazine summary LOD at map zoom (**Deferred** product LOD — same north-star as hub scale NFRs) |

## MCP Tools

| Tool | Description | Honesty |
|------|-------------|---------|
| `logistics_get_readiness` | Unit readiness breakdown | **Gap** — not shipped product MCP |
| `logistics_project_sorties` | Sorties until bingo for mission | **Gap** |
| `magazine_get` | Stores and counts | **Gap** — headless `MagazineLedger` only |

Ferry authoring uses Mission Editor CLI (`mission_add_ferry` / `mission_update_ferry`) — not the logistics MCP table above.

## Acceptance Criteria

1. Launch decrements correct magazine; reload completes at deterministic tick. *(Reload complete = residual; fire consume **Shipped**.)*
2. Strike without tanker triggers validation advisory in Baltic tutorial scenario.
3. Carrier deck cycle limits sorties per hour in test scenario. **(Phase N — not full air-ops FSM.)**
4. Ferry mission moves squadron; fuel at destination matches projection within tolerance. *(Ferry CLI/validation **Partial+**; full runtime ferry fuel projection residual.)*
5. Losses/expenditures report (doc 17) matches magazine consumption totals. *(Expenditures wire Partial.)*
6. Agent aborts takeoff when readiness &lt; threshold with logged reason. **(`AIR_NOT_READY` Shipped on engage + validation.)**

## Phased Delivery

| Phase | Scope |
|-------|--------|
| **MVP (spine shipped)** | Magazines consume, fuel bingo/joker, readiness boolean / `AIR_NOT_READY`, ferry/support linkage (CLI + validation), order-log change rows |
| **Phase 2** | Catalog live magazines polish, reachability/tanker parity, C2 magazine panel, swarm burn metrics |
| **Phase 3 / Phase N** | UNREP, full air-ops FSM, crew rest, detailed maintenance, product MCP logistics tools |

## Implementation Mapping (headless)

| Area | Path / type | Status | Evidence |
|------|-------------|--------|----------|
| Magazine ledger | `MagazineLedger` (`ProjectAegis.Sim` · `Engage/`) | **Shipped** | `MagazineSalvoTests`; engage `MagazineEmpty` / `NO_AMMO`; `baltic-patrol-magazine`; `ReplayGoldenBalticMagazineTests` |
| Catalog magazine seeder | `CatalogMagazineLedgerSeeder`, `CatalogMagazineResolver` | **Partial** | `CatalogMagazineLedgerSeederTests`, `CatalogMagazineResolverTests`; fallback rounds when catalog unresolved |
| Fuel ledger / timeline | `FuelLedger` (`Sim` · `Logistics/`), `FuelTimelineTracker` (`Delegation` · `Logistics/`) | **Shipped** | `FuelLedgerTests`, `FuelTimelineTrackerTests`; `BalticReplayHarnessFuelTests`; opt-in `UsesFuelBurnModel` |
| Fuel / magazine order log | `MagazineChangeRecord`, `FuelStateChangeRecord`, `FuelBurnRecord` | **Shipped** | `MagazineChangeOrderLogTests`, `FuelStateChangeOrderLogTests`, `FuelBurnOrderLogTests` |
| Readiness map / AIR_NOT_READY | `UnitReadinessMap`, `UnitReadinessMapFactory`, engage air-ready gate | **Shipped** | `MvpEngagementAirNotReadyTests`, `UnitReadinessEngageTests`, `BalticReplayHarnessReadinessPolicyTests`; `baltic-patrol-readiness`; validation `AIR_NOT_READY` |
| Editor validation | `ScenarioValidationEngine` logistics rules | **Partial+** | `LogisticsValidationRulesTests` (`AIR_NOT_READY`); ferry/strike reachability codes Partial |
| Fuel C2 projection | `FuelStateProjection` | **Partial** | `FuelStateProjectionTests`; unit detail `FUEL:` line path |
| UNREP / re-arm at sea | — | **Phase N** | Tracker residual: UNREP; live magazines UI |
| Air-ops FSM (taxi/takeoff/land/maint) | — | **Phase N** | Boolean readiness only on `main` |
| MCP logistics / magazine tools | — | **Gap** | Spec tools not product MCP verbs |

**Honesty note:** Design Status remains **Draft** (Template B). Tracker **Partial** is correct: magazine + fuel + readiness spine shipped; UNREP, full air-ops FSM, live magazines UI, and MCP logistics remain open.

## Open Questions

1. Unified vs per-munition magazine modeling for VLS?
2. Instant rearm in editor test only?
3. Cargo mission logistics (doc 11 P1) — same doc or separate?

## Traceability

| Doc | Relationship |
|-----|----------------|
| Hub **FR-14** ([01](01-Project-Overview.md)) | Logistics and magazines — this doc |
| 11 | Mission validation, flight preview, ferry CLI |
| 14 | Magazine on fire; readiness before launch |
| 17 | Expenditures / order-log change rows |
| 18 | Base damage / readiness after damage |
| `cmo-manual-traceability.md` | §3.3.4–8, §6.3.13 |

---

**Implementation grade:** Partial — see [implementation-tracker-2026-07-04.md](../implementation-tracker-2026-07-04.md) row 16.  
Design Status remains **Draft** (Template B). Charter re-honesty: Wave 2 2026-07-08.

**References:** CMO Manual §3.3.6–8; `docs/manual/index.html`
