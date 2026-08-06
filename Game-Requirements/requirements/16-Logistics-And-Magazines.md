# 16 - Logistics, Magazines, and Platform Operations

**Last Updated:** 2026-07-26 (boat operations specified — §Boat Operations, LOG-09…11)  
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
| Boat operations (launch, recovery, embarked load) | §3.3.8 | **P1** |
| UNREP / at-sea replenishment | §3.3.8 | **P1** |
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

## Boat Operations

Embarked small craft — ship's boats, RHIBs, LCVP/LCU, LCAC — launched and recovered from a host
platform. Distinct from Air Operations above: the constraints, the failure modes, and the tactical
cost are not the same, and the model must not be a copy of the air FSM.

**P1** States:

```
Stowed | Prepping | Launching | Waterborne | Returning | Alongside | Recovering | Maintenance
```

### Launch and recovery constraints

- **P1** **Recovery limits are stricter than launch limits.** A craft may be put in the water in
  conditions in which it cannot be safely brought back aboard. This asymmetry is the domain's
  defining tactical trap and must be modelled, not smoothed away — launching into a rising sea
  state is a decision with a consequence.
- **P1** Host must reduce speed for davit operations; well-deck operations (LCAC/LCU) require
  ballasting down, which takes time and further constrains host speed and manoeuvre. Boat
  operations therefore **cost the host its tactical freedom** for the duration — this coupling is
  the point, not an implementation detail.
- **P1** Launch mechanism per host: `Davit | WellDeck | Ramp`, with distinct duration and
  simultaneity limits.
- **P2** Simultaneous operations bounded by davit count / well-deck capacity.

### Sea state dependency

**P1** Boat launch and recovery are gated by sea state.

Aegis has **no weather or sea-state model** (`src/` has no `Weather` / `SeaState` type; "sea state"
appears only in doc 15's sensor context). A full weather system is **not** a prerequisite.

**MVP path:** a **scenario-level static sea-state scalar** (Douglas 0–9) in scenario metadata,
deterministic by construction, with per-craft `maxLaunchSeaState` and `maxRecoverySeaState` from the
catalog (doc 06). This makes boat operations specifiable and testable without simulating weather.
Dynamic sea state remains **Phase N**.

### Embarked load

- **P1** Craft carry **personnel** (boarding teams, troops) and/or **cargo**, with capacity limits
  from the catalog. This is the boat analogue of aircraft loadout — the question is *who and what is
  embarked*, not which stores are mounted, so the magazine model does **not** apply.
- **P2** Boarding party composition affects VBSS outcomes (doc 14 / doc 18 as those mature).

### Readiness and failure

- **P1** Per-craft `ReadyForLaunch` boolean → `BOAT_NOT_READY` abort, parallel to `AIR_NOT_READY`
  (`LogisticsAbortReason`).
- **P1** **Stranded craft:** a waterborne craft whose host has departed, whose recovery sea-state
  limit is exceeded, or whose host has lost the relevant facility to damage (doc 18). A stranded
  craft is a *persistent tactical liability with a crew aboard* — not a written-off asset. Order log
  records the transition (doc 17).
- **P2** Craft endurance and fuel tracked independently of the host.

### Determinism

- **P1** All launch/recovery durations deterministic per tick; no wall-clock reads.
- **P1** Simultaneous launch/recovery ordering **stable and explicitly sorted** (craft id ordinal) —
  unordered enumeration over craft is the defect class already fixed once in scenario policy loading
  (DRG-54); do not reintroduce it here.

**Honesty:** All of the above is **product intent**. **Nothing is shipped** — `src/` contains no
boat, craft, or amphibious type, and the prior version of this section specified no boat operations
at all. This section defines the domain so the C2 Boat Operations window (doc 20, proposed
**CMD-25**) has something to derive from; it does not claim implementation.

## Naval Operations (host platform)

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
| Boat mission assigned to host with no embarked craft | Export scenario | Blocker |
| Scenario sea state exceeds craft `maxRecoverySeaState` | Export | Blocker — would strand the crew |
| Scenario sea state exceeds `maxLaunchSeaState` only | Export | Advisory — launchable, recovery at risk |
| Embarked load exceeds craft capacity | Export | Blocker |
| Simultaneous boat ops exceed davit / well-deck capacity | Quick run | Advisory |

## Agent integration

- Agents respect bingo and WRA; **Cautious** returns strike assets earlier
- **Swarm Coordinator** tracks expendable inventory across sub-swarms
- **P0** Agent cannot order takeoff if readiness fails — `LogisticsAbortReason` enum (parallel to FireAbortReason)
- **P1** Agent cannot order boat launch when sea state exceeds the recovery limit — `BOAT_NOT_READY`; **Cautious** personalities should refuse at the *launch* margin, not the recovery margin

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
| **LOG-09** | Boat launch/recovery FSM + `BOAT_NOT_READY` gate | **P1** — **Phase N** |
| **LOG-10** | Sea-state gate for boat ops (scenario-level scalar MVP; dynamic weather Phase N) | **P1** — **Phase N** |
| **LOG-11** | Embarked load (personnel / cargo) capacity + stranded-craft state | **P2** — **Phase N** |

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
7. Boat launches and recovers within a deterministic tick budget; two runs at fixed seed produce identical transition ticks. **(Phase N — LOG-09.)**
8. Launch is refused with `BOAT_NOT_READY` when scenario sea state exceeds the craft recovery limit, and the refusal names the limit that was exceeded. **(Phase N — LOG-09/LOG-10.)**
9. A craft whose host departs while waterborne enters `Stranded` with an order-log row, and is not silently destroyed or teleported. **(Phase N — LOG-11.)**

## Phased Delivery

| Phase | Scope |
|-------|--------|
| **MVP (spine shipped)** | Magazines consume, fuel bingo/joker, readiness boolean / `AIR_NOT_READY`, ferry/support linkage (CLI + validation), order-log change rows |
| **Phase 2** | Catalog live magazines polish, reachability/tanker parity, C2 magazine panel, swarm burn metrics |
| **Phase 3 / Phase N** | UNREP, full air-ops FSM, **boat-ops FSM + sea-state gate + embarked load (LOG-09…11)**, crew rest, detailed maintenance, product MCP logistics tools |

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
| Boat-ops FSM (launch/recovery/stranded) | — | **Phase N** | No boat / craft / amphibious type exists in `src/` |
| Sea-state gate for boat ops | — | **Phase N** | No `Weather` / `SeaState` type in `src/`; scalar MVP proposed (LOG-10) |
| MCP logistics / magazine tools | — | **Gap** | Spec tools not product MCP verbs |

**Honesty note:** Design Status remains **Draft** (Template B). Tracker **Partial** is correct: magazine + fuel + readiness spine shipped; UNREP, full air-ops FSM, live magazines UI, and MCP logistics remain open.

## Open Questions

1. Unified vs per-munition magazine modeling for VLS?
2. Instant rearm in editor test only?
3. Cargo mission logistics (doc 11 P1) — same doc or separate?
4. **Are embarked craft first-class units or sub-entities of the host?** This is the load-bearing architectural question for LOG-09. First-class units get their own ORBAT entry, sensors, and order log; sub-entities are cheaper but cannot be independently tasked or tracked once waterborne. Aircraft precedent does not settle it, because boats spend their sortie inside the host's own tactical picture rather than transiting away from it.
5. Is VBSS / boarding a **mission type** (doc 11) or a boat-operations action? Proposed: mission type, so it inherits validation and the mission timeline — but that makes doc 11 a dependency of LOG-11.
6. Is the scenario-level sea-state scalar (LOG-10) sufficient for v1, or does counter-piracy / littoral content require dynamic weather sooner than Phase N?

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
