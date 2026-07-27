# 16 - Logistics, Magazines, and Platform Operations

**Last Updated:** 2026-07-27  
**Status:** Draft — ready for design review  
**FR reverse-ref:** [FR-14](01-Project-Overview.md) — Logistics, magazines, and parasite (air/boat) operations  
**CMO basis:** Manual §3.3.4–8, §3.3.6–8, §3.3.16 (withdraw/redeploy linkage), §4.5.4–5, §6.3.13, §7.2.1–2 (ferry/support)  
**Related:** 11 Mission Editor, 13 Doctrine, 14 Engagement, 18 Combat Domains, 06 Database Intelligence, 17 Order Log, 20 C2 UI  
**Tracker:** [implementation-tracker-2026-07-04.md](../implementation-tracker-2026-07-04.md) row 16 — **Partial**  
**GDD:** [logistics-magazines.md](../../design/gdd/logistics-magazines.md)

## Purpose

Define **fuel**, **magazines**, **parasite unit operations** (hosted aircraft and boats), **air and boat operations**, **readiness**, and **sustainment** rules that constrain missions and engagements — with **validation** at authoring time and **deterministic** consumption at runtime.

Implements hub **[FR-14](01-Project-Overview.md)** (logistics, magazines, and parasite operations).

## Vision

Wars are won by logistics as much as firepower. Swarm-heavy 2030s scenarios must fail believably when magazines empty or tankers are mispositioned. The Validation Agent (doc 11) and player see the same arithmetic.

## CMO Parity Requirements

| Capability | CMO | Aegis |
|------------|-----|-------|
| Magazines UI | §3.3.6 | **P0** |
| Air operations (ready, takeoff, recovery) | §3.3.7 | **P0** |
| Boat ops / UNREP | §3.3.8 | **P1** |
| Parasite host identification & map distribution | §3.3.7–8, unit profile | **P0** product intent |
| Throttle / altitude / fuel | §3.3.4, §4.5.4–5 | **P0** |
| Ferry / support missions | §7.2.1–2 | **P0** |
| Losses and expenditures | §6.3.13 | **P0** — feeds doc 17 |
| Withdraw / redeploy in port (doctrine) | §3.3.16 | **P0** — see doc 13 |

**Honesty overlay:** P0/P1 rows = product intent. **Shipped MVP spine:** magazine consume + fuel burn/bands + boolean readiness → `AIR_NOT_READY` + ferry CLI/validation hooks. **Not shipped:** full air-ops FSM, boat-ops launch windows, quick-turnaround cycle, parasite host chrome, UNREP, live magazines C2 panel, product MCP logistics tools.

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

## Parasite Unit Operations

**Parasite units** are hosted platforms (aircraft, boats, or other embarked units) that depend on a **host** (airbase, port, carrier, parent vessel, or installation). Hosted units share the host’s basing, magazines, fuel, and readying cycle until they launch. Requirements below are CMO-parity product intent; maturity is called out per **LOG-*** ID.

### Parasite unit identification

| ID | Requirement | Priority / maturity |
|----|-------------|---------------------|
| **LOG-09** | **Host capability flagging.** The unit profile UI must identify units capable of hosting others via a specific icon in the **bottom-right corner** of the unit profile. | **P0** — **Phase N** (C2 chrome) |
| **LOG-10** | **Deployment visualization.** On selection of a host unit (airport, port, or equivalent installation), the system must highlight the **geographical distribution** of hosted (parasite) units across that installation. | **P0** — **Phase N** (map + host selection) |

UI affordances for LOG-09/10 live primarily under [20](20-Command-And-Control-UI.md); simulation ownership of host/parasite membership remains in this doc.

### Naval vessel operations (boat ops)

| ID | Requirement | Priority / maturity |
|----|-------------|---------------------|
| **LOG-11** | **Access point.** Boat operations must be accessible via a dedicated **Boats** menu entry (keyboard shortcut **F7**) or a **Boats** button on the unit/host chrome. | **P1** — **Phase N** |
| **LOG-12** | **Transit / launch delay.** The system must enforce a **15-minute launch window** from the moment a launch order is initiated until the boat departs the port/dock (deterministic sim-time delay; not wall-clock). | **P1** — **Phase N** |
| **LOG-13** | **Automatic refuel and rearm.** Hosted ships at a port/dock must be **automatically refueled and rearmed** unless overruled by Doctrine settings (doc 13). | **P1** — **Phase N** |

**Related residual:** UNREP at sea remains **LOG-07** (Phase N). LOG-13 covers **in-port / docked** automation only.

### Aircraft operations

| ID | Requirement | Priority / maturity |
|----|-------------|---------------------|
| **LOG-14** | **Access point.** Aircraft operations must be accessible via a dedicated **Aircraft** menu entry (keyboard shortcut **F6**) or an **Aircraft** button on the unit/host chrome. | **P0** — **Phase N** |
| **LOG-15** | **Loadout availability check.** Loadouts that require magazine ammunition **not available** at the host must be rendered in *italics* and **disabled** (not selectable for rearm/ready). | **P0** — **Phase N** |
| **LOG-16** | **Mass assignment.** Users must be able to select **multiple aircraft** (shift-click or control-click) for **batch** rearming / readying. | **P0** — **Phase N** |
| **LOG-17** | **Quick turnaround.** An **Enable Quick** feature must expedite rearming/refueling, constrained by a **4-hour limit** and a **2-sortie maximum** per aircraft per quick-turn window (data-driven knobs; defaults match CMO-parity values). | **P0** — **Phase N** |
| **LOG-18** | **Ready-time composition.** Readying times must include **refueling**, **re-arming**, and **general maintenance**. Any sortie that is initiated triggers a **full, non-stoppable readying cycle** upon return (player cannot cancel mid-cycle; sim continues deterministically). | **P0** — **Phase N** |

### Mission integration

| ID | Requirement | Priority / maturity |
|----|-------------|---------------------|
| **LOG-19** | **Flight size override via mission.** To bypass default flight-size limits (e.g. 6 aircraft), users must be able to assign a large group (e.g. 24 aircraft) to a mission and **launch the entire group simultaneously**. Default flight-size caps remain for unassigned / ad-hoc launches. | **P0** — **Phase N** (depends on mission runtime + air-ops; doc 11) |
| **LOG-20** | **Withdraw and Redeploy doctrine (port behavior).** Doctrine must provide a toggleable **Withdraw and Redeploy** setting that governs unit behavior while in port/dock (see doc 13 ROE-06 / withdraw rules). Hosted units respect the effective doctrine after inheritance. | **P0** — **Partial** on damage-withdraw gates; **full port toggle Phase N** |

### Editor / scenario design controls

| ID | Requirement | Priority / maturity |
|----|-------------|---------------------|
| **LOG-21** | **Unlimited Magazines at Air/Naval Bases.** Scenario Features must include a global flag: *Unlimited Magazines at Air/Naval Bases*. When active, ammunition constraints at air and naval bases are disabled and stock-out limits for loadouts are hidden (editor + play). When inactive, LOG-15 and magazine ledger rules apply normally. | **P0** — **Phase N** (scenario `features` + runtime; doc 11) |

**Feature flag contract:** store as a typed boolean under scenario `features` (doc 11 AME-2.3); locked in play mode (AME-1.1); diffable and schema-validated.

## Air Operations

**P0** States:

```
OnGround | Taxiing | TakingOff | Airborne | Landing | Maintenance
```

- Ready a/c count, sortie generation rate, deck cycle for carriers
- **P0** Support mission: tanker, AEW, EW orbit (doc 11)
- **P0** Airbase capacity and runway damage (links doc 18 facilities)
- Parasite access, loadouts, quick turnaround, and ready-time rules: **LOG-14**–**LOG-18**

**P0** Flight plan preview in editor: ETA, refuel segments, bingo (doc 11).

**Honesty:** Full air-ops FSM above is **product intent**. **MVP shipped:** per-unit `ReadyForLaunch` boolean via `UnitReadinessMap` → engage abort `AIR_NOT_READY`. Full deck-cycle / taxi/landing state machine, F6 Aircraft panel, and quick-turn cycle = **Phase N**.

## Boat and Naval Operations

- **P1** UNREP for fuel and limited rearm → **Phase N** (not on `main` as runtime model) — **LOG-07**
- **P1** Parasite boat ops (F7, 15-minute launch window, auto refuel/rearm in port) → **LOG-11**–**LOG-13** — **Phase N**
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
| **LOG-09** | Host-capable unit profile icon (bottom-right) | **P0** — **Phase N** |
| **LOG-10** | Host selection highlights parasite distribution on map | **P0** — **Phase N** |
| **LOG-11** | Boat ops access (F7 / Boats button) | **P1** — **Phase N** |
| **LOG-12** | Boat launch 15-minute sim-time window | **P1** — **Phase N** |
| **LOG-13** | Auto refuel/rearm of docked hosted ships (doctrine-overridable) | **P1** — **Phase N** |
| **LOG-14** | Aircraft ops access (F6 / Aircraft button) | **P0** — **Phase N** |
| **LOG-15** | Unavailable loadouts italic + disabled | **P0** — **Phase N** |
| **LOG-16** | Multi-select batch rearm/ready aircraft | **P0** — **Phase N** |
| **LOG-17** | Quick turnaround (4 h / 2 sorties max) | **P0** — **Phase N** |
| **LOG-18** | Ready-time = refuel + rearm + maint; non-stoppable post-sortie cycle | **P0** — **Phase N** |
| **LOG-19** | Mission-assigned large flight simultaneous launch | **P0** — **Phase N** |
| **LOG-20** | Withdraw and Redeploy doctrine for port behavior | **P0** — **Partial** / Phase N full toggle |
| **LOG-21** | Scenario feature: Unlimited Magazines at Air/Naval Bases | **P0** — **Phase N** |

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
7. Host-capable unit shows host icon on unit profile; selecting the host highlights parasite distribution (**LOG-09**, **LOG-10**). **(Phase N.)**
8. Boat launch order does not depart until 15 sim-minutes after initiation (**LOG-12**). **(Phase N.)**
9. Docked hosted ships auto-refuel/rearm unless doctrine disables (**LOG-13**). **(Phase N.)**
10. Aircraft loadout requiring missing magazine ammo is italic + disabled (**LOG-15**). **(Phase N.)**
11. Multi-select aircraft batch rearm/ready completes for all selected ready-eligible units (**LOG-16**). **(Phase N.)**
12. Quick turnaround rejects a third sortie or any sortie after the 4-hour window (**LOG-17**). **(Phase N.)**
13. Post-sortie readying cycle cannot be cancelled and includes refuel + rearm + maintenance (**LOG-18**). **(Phase N.)**
14. Mission with 24 assigned aircraft launches the full group together, bypassing default flight-size cap (**LOG-19**). **(Phase N.)**
15. Scenario feature *Unlimited Magazines at Air/Naval Bases* disables base ammo constraints and hides stock-out loadout limits (**LOG-21**). **(Phase N.)**

## Phased Delivery

| Phase | Scope |
|-------|--------|
| **MVP (spine shipped)** | Magazines consume, fuel bingo/joker, readiness boolean / `AIR_NOT_READY`, ferry/support linkage (CLI + validation), order-log change rows |
| **Phase 2** | Catalog live magazines polish, reachability/tanker parity, C2 magazine panel, swarm burn metrics |
| **Phase 3 / Phase N** | UNREP, full air-ops FSM, parasite host chrome (LOG-09–10), F6/F7 air/boat ops (LOG-11–18), mission flight-size override (LOG-19), port withdraw/redeploy toggle (LOG-20), unlimited-base-magazines feature (LOG-21), crew rest, detailed maintenance, product MCP logistics tools |

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
| Parasite host / air-boat ops (LOG-09–21) | — | **Phase N** | Spec only; no runtime host membership model on `main` |
| MCP logistics / magazine tools | — | **Gap** | Spec tools not product MCP verbs |

**Honesty note:** Design Status remains **Draft** (Template B). Tracker **Partial** is correct: magazine + fuel + readiness spine shipped; UNREP, full air-ops FSM, parasite air/boat ops (LOG-09–21), live magazines UI, and MCP logistics remain open.

## Open Questions

1. Unified vs per-munition magazine modeling for VLS?
2. Instant rearm in editor test only?
3. Cargo mission logistics (doc 11 P1) — same doc or separate?
4. Default flight-size cap value(s) per platform class vs single global default (LOG-19)?
5. Quick-turn 4 h / 2 sorties: scenario-overridable knobs only, or also doctrine-level?

## Traceability

| Doc | Relationship |
|-----|----------------|
| Hub **FR-14** ([01](01-Project-Overview.md)) | Logistics, magazines, parasite ops — this doc |
| 11 | Mission validation, flight preview, ferry CLI, scenario `features` (LOG-21), mission flight-size launch (LOG-19) |
| 13 | Withdraw and Redeploy doctrine / port behavior (LOG-20); auto refuel/rearm override (LOG-13) |
| 14 | Magazine on fire; readiness before launch |
| 17 | Expenditures / order-log change rows |
| 18 | Base damage / readiness after damage |
| 20 | Host icon, F6/F7 Aircraft/Boats panels, multi-select, loadout italics (LOG-09–16) |
| `cmo-manual-traceability.md` | §3.3.4–8, §3.3.16, §6.3.13 |

---

**Implementation grade:** Partial — see [implementation-tracker-2026-07-04.md](../implementation-tracker-2026-07-04.md) row 16.  
Design Status remains **Draft** (Template B). Charter re-honesty: Wave 2 2026-07-08.

**References:** CMO Manual §3.3.6–8; `docs/manual/index.html`
