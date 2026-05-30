# 18 - Combat Domains and Damage

**Last Updated:** May 29, 2026  
**Status:** Draft — ready for design review  
**CMO basis:** Manual §4.1.1–2, §9.2.1–7, §9.3; systems/damage §3.3.11  
**Related:** 14 Engagement, 15 Sensors, 16 Logistics, 13 Doctrine, 09–10 Near-future

## Purpose

Specify **domain-specific combat** (air, naval surface, submarine, mine, land, EW interaction), **damage and repair**, **facility construction/destruction**, and **BDA** — as extensions of the unified engagement pipeline (doc 14).

## Vision

One resolver, many domain validators. Air-to-air, ASuW, ASW, mine warfare, and limited land fires share deterministic outcomes and logging, without duplicate “mini-games” that break replay parity.

## CMO Parity Requirements

| Domain | Manual | Aegis |
|--------|--------|-------|
| Air combat | §9.2.1 | **P0** |
| Naval combat | §9.2.2 | **P0** |
| Submarine | §9.2.3 | **P0** |
| Mine warfare | §9.2.4 | **P1** |
| Land combat | §9.2.5 | **P1** (limited per doc 01) |
| EW | §9.2.6 | **P0** with doc 15 |
| Damage & repair | §9.2.7 | **P0** |
| Base build/destroy | §9.3 | **P1** |

## Architecture: Domain Validators

Engagement pipeline (doc 14) calls **domain validator** after generic checks:

```
EngageIntent → ... → DomainValidator(domain).Validate() → Launch
```

**P0** Domains: `Air`, `Surface`, `Subsurface`, `Land`, `Mine`, `Facility`.

Each validator adds `FireAbortReason` codes in a domain namespace (e.g., `ASW_NO_SOLUTION`).

## Air Combat

- **P0** Beyond-visual-range and within-visual-range missiles; gun optional
- **P0** Altitude, aspect, speed affect PK (from DB tables, not ad hoc)
- **P0** Intercept geometry uses same DLZ machinery (doc 14)
- **P1** Loyal wingman / drone escort rules (doc 09)

## Naval Surface Combat

- **P0** Anti-ship missiles, guns, CIWS
- **P0** Radar horizon and seeker activation logged
- **P1** Swarm saturation: multiple hits same tick ordered deterministically

## Submarine / ASW

- **P0** Torpedo launch through engage pipeline
- **P0** Contact classification requirements for sub-surface fire
- **P1** Buoy patterns, MAD, towed array (sensor doc 15)
- **P0** ASW context menu actions (manual §4.1.2) map to intents

## Mine Warfare

- **P1** Mining mission lays mines; mine-clearing mission
- **P0** Mine danger areas on map layer
- **P1** Deterministic mine hit on transit (seed-based placement)

## Land Combat (Limited)

Per doc 01 scope: **strategic facilities and IADS**, not battalion tactics.

- **P1** Fixed SAM, buildings, runways as targets
- **P0** Strike damage affects airbase capacity (doc 16)

## Damage Model

**P0** Component-level or platform-level damage (MVP: platform HP + critical flags).

| Level | Effect |
|-------|--------|
| Light | Sensor degradation |
| Moderate | Mount offline, speed cap |
| Heavy | Mission abort, withdraw suggestion (doc 13) |
| Destroyed | Unit removed; log to losses (doc 17) |

**P0** Damage application order: sorted by `engagementId`.

**P1** Damage control / repair over time at base or tender.

## Facilities (Construction / Destruction)

- **P1** Airbase, naval base, IADS build/destroy via events or strikes (§9.3)
- **P0** Runway crater reduces sortie rate; repair event restores

## Battle Damage Assessment

- **P0** Target status: unknown / damaged / destroyed on contact
- **P0** Re-attack recommendations in Assisted mode
- **P1** Agent **Opportunistic** favors BDA before shifting fires

## Near-Future (docs 09–10)

- **P1** Hypersonic: shortened DLZ, unique abort reasons
- **P1** Directed energy: magazine-less but thermal cap and weather
- **P2** Speculative weapons gated by scenario module

## Non-Functional Requirements

| Area | Target |
|------|--------|
| Determinism | PK rolls from seeded sub-RNG per engagement id |
| Performance | Domain validators skipped when no weapon in domain |
| Fidelity | Parameters traceable to DB source id (doc 06) |

## Acceptance Criteria

1. Air and surface engagements in one scenario share order log schema.
2. Destroyed unit cannot fire; mount offline produces `FireAbortReason.MOUNT_OFFLINE`.
3. ASW shot without valid subsurface contact denied with domain code.
4. Runway strike reduces max sorties until repair event.
5. Two replays: identical damage timeline for same engagements.
6. Mining mission places mines at deterministic coordinates from seed.

## Phased Delivery

| Phase | Scope |
|-------|--------|
| **MVP** | Air, surface, basic subsurface, damage, facilities as targets |
| **Phase 2** | Mines, ASW depth, base destroy/repair |
| **Phase 3** | Near-future weapon domains, full land IADS |

## Open Questions

1. Platform vs component damage for v1?
2. Crew abandon ship rules?
3. Nuclear damage — scenario gated only?

## Traceability

| Doc | Relationship |
|-----|----------------|
| 14 | Pipeline |
| 15 | Tracks for fire |
| 16 | Readiness after damage |
| 17 | Engagement and loss entries |
| `cmo-manual-traceability.md` | Ch 9 |

---

**References:** CMO Manual §9.2–9.3; `docs/manual/index.html`
