# 18 - Combat Domains and Damage

**Last Updated:** 2026-07-18  
**Status:** Draft — ready for design review  
**FR reverse-ref:** [FR-16](01-Project-Overview.md) — Multi-domain combat  
**CMO basis:** Manual §4.1.1–2, §9.2.1–7, §9.3; systems/damage §3.3.11  
**Related:** 14 Engagement, 15 Sensors, 16 Logistics, 13 Doctrine, 09–10 Near-future  
**Tracker:** [implementation-tracker-2026-07-04.md](../implementation-tracker-2026-07-04.md) row 18 — **Partial+**  
**Architecture:** [ADR-009](../../docs/architecture/adr-009-combat-domain-validators.md) — combat domain validators & deterministic damage order  
**GDD:** [combat-domains-damage.md](../../design/gdd/combat-domains-damage.md)

## Purpose

Specify **domain-specific combat** (air, naval surface, submarine, mine, land, EW interaction), **damage and repair**, **facility construction/destruction**, and **BDA** — as extensions of the unified engagement pipeline (doc 14).

Implements hub **[FR-16](01-Project-Overview.md)** (multi-domain combat).

## Vision

One resolver, many domain validators. Air-to-air, ASuW, ASW, mine warfare, and limited land fires share deterministic outcomes and logging, without duplicate “mini-games” that break replay parity.

## CMO Parity Requirements

| Domain | Manual | Aegis maturity (honest) |
|--------|--------|-------------------------|
| Air combat | §9.2.1 | **P0 spine Shipped** — domain enum + validators; **full BVR/WVR fidelity Phase N** |
| Naval combat | §9.2.2 | **P0 spine Shipped** — surface domain gate; **full radar-horizon / CIWS fidelity Phase N** |
| Submarine | §9.2.3 | **P0 spine Partial** — subsurface FC-track / identify gates; **full ASW depth / buoy patterns Phase N** |
| Mine warfare | §9.2.4 | **Partial** — `CombatDomain.Mine` + `MineAspectDomainValidator` + transit hazard hot-tick; **mine-laying/clearing missions Phase N** |
| Land combat | §9.2.5 | **Partial** — land aspect gate; limited facilities/IADS (doc 01) |
| EW | §9.2.6 | **P0** with doc 15 (sensor/jam path; not a separate combat mini-game) |
| Damage & repair | §9.2.7 | **P0 Partial+** — deterministic damage apply + platform HP/kill; **component repair Phase N** |
| Base build/destroy | §9.3 | **Partial** — facility damage / capacity hooks; full build events Phase N |

## Architecture: Domain Validators

Engagement pipeline (doc 14) calls **domain validator** after generic policy checks and before launch geometry/magazine — per [ADR-009](../../docs/architecture/adr-009-combat-domain-validators.md):

```
EngageIntent → IPolicyEvaluator → IDomainValidator.Validate(domain) → geometry/magazine → Launch
```

**Shipped spine domains** (`CombatDomain` enum): `Air`, `Surface`, `Subsurface`, `Land`, `Mine`, `Facility`.

Registry: `DomainValidatorRegistry` — stable iteration order by domain ordinal; gated by scenario `engage.combatDomainsEnabled`.

Each validator may add domain-namespace abort codes (e.g. `ASW_NO_SOLUTION` / `DomainNoSolution`, `MINE_ASPECT_BLOCK`, `LAND_ASPECT_BLOCK`, `FACILITY_ASPECT_BLOCK`).

**Honesty:** The **plug-in spine** (registry + per-domain validators + flag + deterministic damage order) is **Shipped**. Full CMO-fidelity air/naval/sub combat models (PK tables, radar horizon, buoy patterns, mining missions) remain **Phase N** — do not read historical P0 labels as full-fidelity claims.

## Air Combat

- **Shipped spine** Domain `Air` + aspect envelope gate (`AirAspectDomainValidator` / air readiness abort path)
- **Phase N** Full BVR/WVR missile fidelity; gun optional; altitude/aspect/speed PK tables from DB
- **Phase N** Intercept geometry beyond shared DLZ machinery (doc 14) as dedicated air-combat depth
- **P1 / Phase N** Loyal wingman / drone escort rules (doc 09)

## Naval Surface Combat

- **Shipped spine** Domain `Surface` + surface aspect validator
- **Phase N** Anti-ship missiles, guns, CIWS as full domain fidelity (not separate mini-game)
- **Phase N** Radar horizon and seeker activation logging depth
- **P1** Swarm saturation: multiple hits same tick ordered deterministically (engage-side salvo deconfliction is doc 14 / already shipped)

## Submarine / ASW

- **Partial (spine)** Torpedo / subsurface path through engage pipeline with `CombatDomain.Subsurface`
- **Partial** Contact classification / fire-control track requirements for sub-surface fire (`CombatDomainValidator` subsurface gates)
- **Phase N** Buoy patterns, MAD, towed array depth (sensor doc 15 + this domain)
- **Phase N** Full CMO ASW context-menu action fidelity (manual §4.1.2)

## Mine Warfare

**Honesty fix (P0/P1 inconsistency closed):**

| Capability | Status |
|------------|--------|
| `CombatDomain.Mine` enum + `MineAspectDomainValidator` + `MINE_ASPECT_BLOCK` / `MineAspectBlock` | **Partial / Shipped spine** (gate when `combatDomainsEnabled`) |
| Mine transit hazard zone + seeded placement (`MineTransitHazardHotTickApplier`, `baltic-patrol-mine-transit-hazard`) | **Partial** |
| Mine danger areas on map layer (full C2 presentation) | **Phase N** |
| Mining mission lays mines; mine-clearing mission | **Phase N** (tracker residual: mine-laying/clearing) |
| Full deterministic mine-warfare campaign loop | **Phase N** |

Do **not** claim mining missions as P0 while only the enum/aspect gate + transit hazard exist.

## Land Combat (Limited)

Per doc 01 scope: **strategic facilities and IADS**, not battalion tactics.

- **Partial** Fixed SAM / land aspect gate (`LandAspectDomainValidator`)
- **Partial** Strike / facility damage projections; runway/sortie capacity linkage (doc 16) where wired
- **Phase N** Full land IADS / battalion-scale combat

## Damage Model

**Shipped (Partial+):** Platform-level damage / kill via engagement outcomes; deterministic apply order; BDA contact lifecycle.

| Level | Effect (design) | Maturity |
|-------|-----------------|----------|
| Light | Sensor degradation | Partial / Phase N depth |
| Moderate | Mount offline, speed cap | Partial |
| Heavy | Mission abort, withdraw suggestion (doc 13) | Partial (`CatalogDamageWithdrawEngageGate`) |
| Destroyed | Unit removed; log to losses (doc 17) | **Shipped** (`KilledTargetRegistry` + BDA Lost) |

**Shipped:** Damage application order sorted by `engagementId` then `sequenceId` — `DeterministicDamageApplyBatch`.

**Phase N:** Full component-level damage control / repair over time at base or tender.

## Facilities (Construction / Destruction)

- **Partial** Facility domain validator + facility damage / capacity hot-tick projections
- **Phase N** Airbase / naval base / IADS full build/destroy event library (§9.3)
- **Partial** Runway / capacity degradation hooks where present; full repair-event loop Phase N

## Battle Damage Assessment

- **Shipped (Partial+)** Target status promotion via `BdaContactLifecycleRegistry` / `BdaContactLifecycleHotTickApplier` / `OrderLogBdaProjection` (`damageLevel` → contact status / Lost)
- **Partial** Re-attack recommendations in Assisted mode
- **Phase N** Agent **Opportunistic** BDA-before-fires personality depth

## Near-Future (docs 09–10)

- **Phase N / P1** Hypersonic: shortened DLZ, unique abort reasons
- **Phase N / P1** Directed energy: magazine-less but thermal cap and weather
- **Phase N / P2** Speculative weapons gated by scenario module

### Major IDs (DOM-*)

| ID | Summary | Priority / maturity |
|----|---------|---------------------|
| **DOM-01** | Domain validator plug-in after policy, before launch ([ADR-009](../../docs/architecture/adr-009-combat-domain-validators.md)) | **P0** — **Shipped** (`IDomainValidator`, `DomainValidatorRegistry`, `combatDomainsEnabled`) |
| **DOM-02** | Stable domain enum + per-domain aspect validators (Air/Surface/Subsurface/Land/Mine/Facility) | **P0** — **Shipped spine** (`CombatDomain`, `*AspectDomainValidator`, `CombatDomainValidator`) |
| **DOM-03** | Deterministic damage apply order (`engagementId`, `sequenceId`) | **P0** — **Shipped** (`DeterministicDamageApplyBatch`) |
| **DOM-04** | BDA contact lifecycle (kill → Lost / message-log projection) | **P0** — **Partial+** (`BdaContactLifecycleRegistry`, `BdaContactLifecycleHotTickApplier`, `OrderLogBdaProjection`) |
| **DOM-05** | Platform kill / damage-withdraw gates | **P0** — **Partial+** (`KilledTargetRegistry`, `CatalogDamageWithdrawEngageGate`) |
| **DOM-06** | Mine domain gate + transit hazard | **Partial** — enum/gate + `MineTransitHazardHotTickApplier`; **missions Phase N** |
| **DOM-07** | Full air / naval / sub combat fidelity (PK tables, horizon, ASW depth) | **Phase N** |
| **DOM-08** | Mine-laying / mine-clearing missions + map danger layer | **Phase N** |
| **DOM-09** | Facility build/destroy event library + repair loop | **Phase N / Partial** hot-tick only |
| **DOM-10** | Near-future domain validators (hypersonic, DEW) | **Phase N** |

## Non-Functional Requirements

| Area | Target |
|------|--------|
| Determinism | PK rolls from seeded sub-RNG per engagement id; damage apply order fixed |
| Performance | Domain validators skipped when no weapon in domain / flag off |
| Fidelity | Parameters traceable to DB source id (doc 06) when full tables land (Phase N) |

## Acceptance Criteria

Evidence policy: check only with named types/tests/fixtures; unbuilt fidelity stays Phase N.

1. Air and surface engagements in one scenario share order log schema — **met** via unified engage path + ADR-009 registry.
2. Destroyed unit cannot fire; mount offline produces abort / withdraw path — **partial+** (`KilledTargetRegistry`, damage-withdraw gate).
3. ASW shot without valid subsurface contact denied with domain code — **partial** (subsurface FC/identify gates when domains enabled).
4. Runway strike reduces max sorties until repair event — **Phase N residual** (facility capacity hooks Partial only).
5. Two replays: identical damage timeline for same engagements — **met** (`DeterministicDamageApplyBatchTests`; combat-domains goldens).
6. Mining mission places mines at deterministic coordinates from seed — **Phase N** (transit hazard seeded placement is **Partial**, not mining missions).

## Phased Delivery

| Phase | Scope |
|-------|--------|
| **MVP / Shipped spine** | Domain validator plug-in ([ADR-009](../../docs/architecture/adr-009-combat-domain-validators.md)), `CombatDomain` enum, aspect validators, deterministic damage order, BDA registry hooks, platform kill |
| **Partial residual** | Mine transit hazard; facility damage projection; land aspect; subsurface classify gates; hot-tick HP/damageLevel |
| **Phase N** | Full air/naval/sub fidelity combat; mine-laying/clearing missions; full facility build/destroy; component repair; near-future weapon domains |

**Shipped (not Phase N debt):** validator registry + plug-in spine, `DeterministicDamageApplyBatch`, BDA lifecycle registry, domain abort codes under `combatDomainsEnabled`.

## Implementation Mapping (headless)

| Area | Path / type | Status | Evidence |
|------|-------------|--------|----------|
| Domain validator registry | `DomainValidatorRegistry`, `IDomainValidator` (`ProjectAegis.Sim` · `Engage/`) | **Shipped** | [ADR-009](../../docs/architecture/adr-009-combat-domain-validators.md); `DomainValidatorRegistryTests`; `combatDomainsEnabled` flag on `MvpEngagementResolver` |
| Domain gates (legacy + aspect) | `CombatDomainValidator`, `*AspectDomainValidator`, `CombatDomain` | **Shipped spine** | `CombatDomainValidatorTests`; air/surface/sub/land/mine/facility validators; production Baltic may set `combatDomainsEnabled=true` |
| Deterministic damage order | `DeterministicDamageApplyBatch` | **Shipped** | `DeterministicDamageApplyBatchTests` (shuffled → sorted apply) |
| BDA registry / lifecycle | `BdaContactLifecycleRegistry`, `BdaContactLifecycleHotTickApplier`, `OrderLogBdaProjection`, `BdaContactDamageStates` | **Partial+** | `BdaContactLifecycleHotTickApplierTests`; damageLevel → Lost; order-log BDA projection |
| Kill / damage-withdraw | `KilledTargetRegistry`, `CatalogDamageWithdrawEngageGate` | **Partial+** | Engage kill path; withdraw on damage threshold |
| Mine hazard | `MineAspectDomainValidator`, `MineTransitHazardHotTickApplier` | **Partial** | `baltic-patrol-mine-transit-hazard`; **mine warfare missions = Phase N** |
| Combat-domains fixtures | `combat-domains-smoke`, `baltic-patrol-combat-domains`; **v3 theater (S75/S79)**: `baltic-v3-patrol`, `baltic-v3-mission-roe-band-c` (`combatDomainsEnabled=true`) | **Shipped (test/isolation)** | `CombatDomainsSmokePolicyTests`, `BalticCombatDomainsPolicyTests`; `BalticReplayHarnessV3UcavTests` (v3 patrol with domains on); hash invariant preserved |
| Full air/naval/sub fidelity | — | **Phase N** | Not claimed Shipped; PK tables / horizon / full ASW depth deferred |
| Mine-laying / clearing missions | — | **Phase N** | Tracker residual row 18 |

**Honesty note:** Design Status remains **Draft** (Template B). Tracker **Partial+** reflects shipped validator/damage/BDA spine + Baltic theater content — not full multi-domain CMO combat parity.

## Open Questions

1. Platform vs component damage for v1? → **Resolved for MVP:** platform-level; component model Phase N.
2. Crew abandon ship rules? → **Open / Phase N.**
3. Nuclear damage — scenario gated only? → **Proposed:** scenario-gated only.

## Traceability

| Doc | Relationship |
|-----|----------------|
| Hub **FR-16** ([01](01-Project-Overview.md)) | Multi-domain combat — this doc |
| 14 | Pipeline hosts domain validators post-policy |
| 15 | Tracks / classification for fire domain gates |
| 16 | Readiness after damage; magazine linkage |
| 17 | Engagement, damage, and loss entries |
| 09–10 | Near-future / speculative domain extensions |
| [ADR-009](../../docs/architecture/adr-009-combat-domain-validators.md) | Accepted architecture for validators + damage order |
| `cmo-manual-traceability.md` | Ch 9 |
| Tracker row 18 | Partial+ — S75/S79 v3 theater exercises combat-domains spine; mine-laying/clearing is next stack task |

---

**Implementation grade:** Partial+ — see [implementation-tracker-2026-07-04.md](../implementation-tracker-2026-07-04.md) row 18.  
Design Status remains **Draft** (Template B). Charter re-honesty: Wave 2 2026-07-08.
