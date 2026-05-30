# 14 - Engagement and Fire Control

**Last Updated:** May 29, 2026  
**Status:** Draft — ready for design review  
**CMO basis:** Manual §3.3.1–2, §3.3.9, §4.1.1, §9.1–2, §9.2.8–9 (DLZ)  
**Related:** 13 Doctrine/ROE/WRA, 15 Sensors, 18 Combat Domains, 04 Delegation, 17 Order Log

## Purpose

Specify **manual**, **assisted**, and **autonomous** engagement paths, **fire-control resolution**, **dynamic launch zone (DLZ)** handling, and **swarm-scale** salvo coordination — with deterministic outcomes and shared explainability when engagements abort.

## Vision

Firing a weapon is a deliberate, auditable decision. Human click, agent intent, and mission auto-engage must converge on **one engagement resolver** so replays, balance batches, and “why didn’t it shoot?” support never diverge.

## CMO Parity Requirements

| Capability | CMO | Aegis |
|------------|-----|-------|
| Engage targets auto | §3.3.1 | **P0** |
| Engage targets manual | §3.3.2 | **P0** |
| Attack options context menu | §4.1.1 | **P0** |
| Mount/weapon selection | §3.3.9 | **P0** |
| DLZ awareness | §9.2.9 | **P0** — preview + log |
| Weapon won’t fire diagnostics | §9.2.8 | **P0** — via doc 13 + this doc |

## Engagement Pipeline (Single Resolver)

All paths feed one ordered pipeline (fixed step order for determinism):

```
Intent (player | agent | mission auto)
  → Policy check (doc 13) → FireAbort? log & stop
  → Target validity (contact id, side, identification level)
  → Mount/weapon availability (magazine, cooldown, damage)
  → Geometry (range, altitude, aspect, DLZ / envelope)
  → Deconfliction (friendly fire, swarm slot, fire distribution)
  → Launch execution → Order log + message log
```

**P0:** No bypass path for agents; mission auto-engage uses same pipeline with `intentSource: mission`.

## Engagement Modes

### Manual (Human Mode)

- Player selects contact + weapon/mount + quantity.
- **P0** Preview: time-to-impact band, DLZ in/out, abort reasons before commit.
- **P0** Commit writes `OrderLogEntry.type = PlayerEngage`.

### Assisted / Semi-Autonomous / Full (Delegation)

- Agent produces `EngageIntent` ranked list; autonomy level gates execution (doc 04).
- **P0** Illegal intents never execute silently — always `FireAbortReason` + optional player prompt.
- **P0** `intentSource: agent` + `agentId` + `personality` + `policySnapshotId` on log.

### Mission Auto-Engage

- Patrol prosecution, strike TOT, SAM self-defense from mission parameters (doc 11).
- **P0** Mission auto-engage respects mission ROE/WRA overrides and unit snapshot.

## DLZ and Envelopes

- **P0** Pre-compute DLZ state per shooter–target–weapon triple: `inZone | approaching | outOfZone | unknown`.
- **P0** UI: DLZ indicator on contact hover and weapon panel (CMO parity).
- **P0** Log when launch aborted due to `FireAbortReason.DLZ_OUT` or `DLZ_CLOSING`.
- **P1** Agent personality affects **when** to fire inside zone (early vs late), not zone math.

## Swarm and Mass Engagement (2030s / Aegis unique)

- **P0** **Salvo deconfliction** for 50+ simultaneous shooters: deterministic target allocation by sorted `(shooterId, targetId, weaponId)`.
- **P0** Magazine burn-rate warnings at planning time (Validation Agent) and runtime (message log).
- **P1** **Swarm Coordinator** assigns fire sectors to sub-swarms to prevent duplicate engagements on same track.

## Functional Requirements

### Targeting

- **P0** Engage only valid contact ids; BDA and re-attack rules in doc 18.
- **P0** Identification threshold (e.g., hostile assumed vs confirmed) enforced by ROE.

### Weapon/Mount

- **P0** Respect magazine counts, reload time, mount damage, orientation limits.
- **P0** Support ripple, salvo, and single-shot per WRA.

### ASW / domain-specific (preview)

- **P1** Torpedo launch, buoy drop, depth charge paths hook same pipeline with domain validators (doc 18).

### Near-future weapons (doc 09)

- **P1** Hypersonic, directed energy, swarm munitions register domain-specific geometry validators; still use FireAbortReason namespace.

## FireAbortReason Catalog (initial)

| Code | Typical cause |
|------|----------------|
| `ROE_*` | Policy doc 13 |
| `WRA_*` | Salvo/range/category |
| `EMCON_*` | Sensor/illuminator required but off |
| `DLZ_OUT` | Outside envelope |
| `NO_AMMO` | Magazine empty |
| `MOUNT_OFFLINE` | Damage/cooldown |
| `TARGET_INVALID` | Stale contact, friendly |
| `DECONFLICT_*` | Friendly fire block, swarm slot |
| `MISSION_DISABLED` | Mission not prosecuting |

**P0:** Extend enum only with version bump; replays store code + version.

## Non-Functional Requirements

| Area | Target |
|------|--------|
| Determinism | Identical intents + state + seed → identical launch times and outcomes |
| Performance | 10k engagement checks/tick budgeted; DLZ cache invalidated on configurable events |
| Headless | Full pipeline without UI; CSV/JSON engage summary per run |

## MCP / Agentic Tools

| Tool | Description |
|------|-------------|
| `engage_preview` | DLZ + abort reasons for shooter/target/weapon |
| `engage_execute` | Authorized launch (editor test or sim) |
| `engage_list_intents` | Pending agent intents for side |

## Acceptance Criteria

1. Same manual engage twice with same seed → identical impact times in order log.
2. Agent **Aggressive** fires earlier in DLZ than **Cautious** with same snapshot; both legal.
3. Blocked shot always produces message log + `FireAbortReason` + policy reference.
4. 500-drone salvo run completes without duplicate target allocation on same tick.
5. Mission auto-engage on patrol respects prosecution geometry; shots outside zone denied with reason.
6. Headless batch exports engage count and abort histogram per side.

## Phased Delivery

| Phase | Scope |
|-------|--------|
| **MVP** | Pipeline, manual + mission auto, DLZ preview, core FireAbortReason, order log |
| **Phase 2** | Full agent intents, swarm deconfliction, ASW hooks |
| **Phase 3** | Near-future weapon validators, ML-assisted aim (research only) |

## Open Questions

1. Auto-engage granularity: per mount vs per unit central fire control?
2. Player override mid-salvo: cancel remaining rounds deterministically?
3. Tacview ACMI export of engage events (doc 17 P2)?

## Traceability

| Doc | Relationship |
|-----|----------------|
| 13 | Policy before geometry |
| 15 | Contacts and tracks |
| 17 | Order log entries |
| 11 | Mission auto-engage parameters |
| `cmo-manual-traceability.md` | §3.3.1–2, §9.2.8–9 |

---

**References:** CMO Manual §9.2.8–9; `docs/manual/index.html`
