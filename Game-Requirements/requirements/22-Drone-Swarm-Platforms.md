# 22 - Drone Swarm Platforms

**Last Updated:** 2026-08-09  
**Status:** Draft — landed from Notion proposal (owner triage 2026-08-09; Linear DRG-84/85); **CEC addendum 2026-08-09 (SWARM-31)**  
**FR reverse-ref:** **FR-20** ([01](01-Project-Overview.md)) — Drone / UAS swarm as first-class platform type  
**CMO basis:** No full CMO swarm-platform analogue; draws on air ops, engagement, sensors, C2 patterns; CEC modelled after real USN/USMC/allied Cooperative Engagement Capability (sensor-netting + integrated fire control)  
**Related:** [14](14-Engagement-And-Fire-Control.md) (salvo/swarm *slot* deconfliction is different), [15](15-Sensor-Detection-And-EW.md), [16](16-Logistics-And-Magazines.md), [17](17-Replay-AAR-And-Order-Log.md), [18](18-Combat-Domains.md), [19](19-Cyber-And-Comms.md), [20](20-Command-And-Control-UI.md), [21](21-Platform-Editor.md), [12](12-Terms-Glossary.md), [09](09-Near-Future-Technologies.md), [10](10-Speculative-Systems.md)  
**Linear:** Milestone **H8 — Drone Swarm Platforms** · Epic [DRG-83](https://linear.app/drgamtd-workspace/issue/DRG-83) · Phase A children DRG-86…91 (**Done**); Phase B umbrella [DRG-92](https://linear.app/drgamtd-workspace/issue/DRG-92)  
**Notion draft (superseded for content):** [SWARM-01…30 proposal](https://app.notion.com/p/3b7f7cb4e4df8104b63be13bc99f2358)  
**Research:** Notion [Drone Swarm Platforms — Research (2026-08-09)](https://app.notion.com/p/3b7f7cb4e4df81428f39e8423877957b)

## Purpose

Specify **drone / UAS swarms as first-class platforms**: one selectable unit with aggregate integrity (`droneCount` / `maxDrones`), intent-level orders, optional host/link (Phase B), **CEC-capable US/NATO sensor-netting and remote fire-control participation (SWARM-31)**, deterministic aggregate engagement SoT, and performance caps — without per-drone micro-control or per-member physics as authority.

Implements hub **FR-20**. Distinct from doc **14** *swarm slot ordering* / `SwarmSalvoDeconfliction` (missile salvo deconfliction among shooters) — vocabulary collision only.

## Vision

Players and agents task a **cloud**, not N micro-aircraft. Integrity is the fun currency: attrition thins DPS and ISR; hard-counter area AA shreds; soft kill and host logistics deepen play later. **US and NATO platforms** that field Cooperative Engagement Capability (CEC) can fold swarm sensors into a composite air picture and engage using remote-quality tracks when the mesh is up. Replay stays stable because combat SoT is **aggregate**, not boid physics.

## Owner triage (binding — 2026-08-09)

| Decision | Outcome |
|----------|---------|
| Charter | **In scope now** under air/naval interaction (Phase A core platform type) |
| Corpus | **This document** + amendment pointers on 14/15/16/18/20/21 (and hub 01) |
| UI Phase A | Reuse unit panel / Air Ops surfaces; dedicated Swarm Ops panel fields = Phase B (SWARM-14) |
| Catalog Phase A | **Abstract generic only** (1–2 presets); national exemplars Phase B+ |
| vs REQ-09/10 ([DRG-47](https://linear.app/drgamtd-workspace/issue/DRG-47)) | **Core air domain** owns SWARM-01…26 and **SWARM-31 (CEC)**; SWARM-27…30 sit near near-future/speculative |
| CEC (addendum 2026-08-09) | **In scope Phase B** for US/NATO catalog platforms; abstract Phase A generic remains non-CEC; not the same as SWARM-29 multi-static ISR mesh |

## Requirement index

| ID | Requirement | Priority | Phase |
|----|-------------|----------|-------|
| SWARM-01 | Swarm as first-class platform type | P0 | A |
| SWARM-02 | Aggregate integrity state (`droneCount`) | P0 | A |
| SWARM-03 | Intent orders: Move, Attack, Hold | P0 | A |
| SWARM-04 | Combat DPS/ISR scale with living drones | P0 | A |
| SWARM-05 | Single selection / OOB node | P0 | A |
| SWARM-06 | Headless logged swarm intents | P0 | A |
| SWARM-07 | Deterministic aggregate engagement SoT | P0 | A |
| SWARM-08 | At least one hard-counter interaction | P0 | A |
| SWARM-09 | Map integrity + density readout | P0 | A |
| SWARM-10 | Modes: Hold, Assault, Screen, Scatter, Rejoin | P1 | B |
| SWARM-11 | Host / mothership link | P1 | B |
| SWARM-12 | LinkState connected/degraded/lost | P1 | B |
| SWARM-13 | Regen near host/base (logistics-gated) | P1 | B |
| SWARM-14 | C2 mode + integrity panel fields | P1 | B |
| SWARM-15 | Doctrine / WRA for expend and auto-engage | P1 | B |
| SWARM-16 | Formations: cloud, wall, spear, orbit | P2 | C |
| SWARM-17 | Multi-axis auto-split assault | P2 | C |
| SWARM-18 | EMP / jam soft-kill effects | P2 | C |
| SWARM-19 | Expend / kamikaze pulse (authorized) | P2 | C |
| SWARM-20 | Mission types for swarm tasking | P2 | C |
| SWARM-21 | Catalog + Platform Editor authoring | P1 | A (schema+preset) / B (PE chrome) |
| SWARM-22 | Scenario editor place/configure swarm | P1 | B |
| SWARM-23 | Agent delegation compatibility | P1 | B |
| SWARM-24 | Replay: orders + integrity deltas | P0 | A |
| SWARM-25 | Performance / render LOD caps | P0 | A |
| SWARM-26 | Contact classification for swarms | P1 | B |
| **SWARM-31** | **CEC for US/NATO platforms (sensor-net + remote engage)** | **P1** | **B** |
| SWARM-27 | Split / merge swarm platforms | P3 | N |
| SWARM-28 | Per-member full physics SoT | P3 | N / Won't MVP |
| SWARM-29 | True multi-static ISR mesh | P3 | N |
| SWARM-30 | Full MUM-T package with manned flight leads | P3 | N |

---

# Phase A — Must ship for MVP

## SWARM-01 — Swarm is a first-class platform type **[P0]**

**Requirement.** The catalog and sim shall support a platform type (or type flag) representing a **drone/UAS swarm** that can be instantiated as a side-owned unit alongside ships, aircraft, and facilities.

**Why.** Without a platform type, swarms are either fake groups of N aircraft (micro hell, wrong logistics) or pure VFX.

**Acceptance.** A scenario can spawn at least one swarm unit; it appears in OOB and accepts selection.

## SWARM-02 — Aggregate integrity **[P0]**

**Requirement.** A swarm unit shall expose `droneCount` / `maxDrones` (or equivalent pool HP that maps 1:1 to count). Destruction of the platform occurs when integrity reaches zero.

**Acceptance.** Damage events reduce count; UI/projections show remaining integrity; at 0 the unit is destroyed/removed per normal unit death rules.

## SWARM-03 — Intent orders: Move, Attack, Hold **[P0]**

**Requirement.** The unit shall accept at least Move (plot/course or mission point), Attack (engage target/auto), and Hold (loiter/station) without requiring selection of individual drones.

**Acceptance.** Headless commands exist; C2 can issue them; swarm centroid follows / engages accordingly.

## SWARM-04 — Effects scale with living drones **[P0]**

**Requirement.** Offensive output and sensor effectiveness shall scale with living drone count (monotonic; exact curve is a tuning knob).

**Acceptance.** Full swarm detects/engages more effectively than a half-depleted swarm under identical geometry; golden or unit tests lock the curve.

## SWARM-05 — Single selection node **[P0]**

**Requirement.** Selection, map symbol, and OOB entry treat the swarm as **one unit**. Per-drone selection is out of scope for Phase A.

**Acceptance.** Clicking the swarm selects one unit id; multi-select behaves like any other unit.

## SWARM-06 — Headless logged intents **[P0]**

**Requirement.** Per [ADR-010](../../docs/architecture/adr-010-headless-first-command-driven-ui.md) patterns, swarm orders shall be headless commands that produce order-log rows (or equivalent deterministic intents), not UI-only gestures.

**Acceptance.** CLI/MCP or existing command path can move/attack a swarm; replay consumes the same intents.

## SWARM-07 — Deterministic aggregate engagement SoT **[P0]**

**Requirement.** Detection, fire, and damage resolution shall use **aggregate swarm state**, not non-deterministic per-drone physics, as the source of truth.

**Acceptance.** Same scenario + seed → same integrity timeline; cosmetic member layout may be seeded but must not drive outcomes.

## SWARM-08 — Hard counter interaction **[P0]**

**Requirement.** At least one weapon/system class (e.g. area AA / flak / CIWS profile) shall be demonstrably effective at shredding swarm integrity faster than point-fire against a conventional airframe of similar cost band.

**Acceptance.** Documented test scenario under `production/qa/` where swarm dies to area AA while surviving longer against single-target fire of equal nominal DPS.

## SWARM-09 — Map integrity + density readout **[P0]**

**Requirement.** Map and/or unit panel shall show remaining integrity (count or bar) and a density/affiliation-safe symbol distinct from a single light aircraft where feasible.

**Acceptance.** Player can read swarm health without opening a deep inspector; color alone is not the only channel ([CMD-12](20-Command-And-Control-UI.md) alignment).

## SWARM-24 — Replay integrity **[P0]**

**Requirement.** Replay/fixtures shall reconstruct swarm orders and integrity-affecting events. Member cosmetics need not be bit-identical if seeded consistently.

**Acceptance.** Golden replay or world-hash path includes swarm integrity deltas.

## SWARM-25 — Performance caps **[P0]**

**Requirement.** Logical drone count and rendered member count shall be independently capped; Phase A combat must remain aggregate so large logical counts do not imply O(n) engagement work per pulse.

**Acceptance.** Profiled scenario with design-max swarms meets project frame/pulse budgets (state numbers when landed in NFR tables).

---

# Phase B — Modes, host, C2, authoring, CEC

## SWARM-10 — Operational modes **[P1]**

**Requirement.** Swarm shall support modes: Hold, Assault, Screen, Scatter, Rejoin. Mode changes are orders (logged).

**Acceptance.** Mode is visible in projection; behavior differs in at least movement posture and engagement aggressiveness.

## SWARM-11 — Host / mothership **[P1]**

**Requirement.** A swarm may designate a host unit (ship/vehicle/aircraft/base). Screen mode prefers orbiting the host. Host loss triggers doctrine last-order. When the host is a **CEC-capable US/NATO** platform (SWARM-31), host↔swarm geometry and host health may **influence** CEC mesh join eligibility, but host C2 `linkState` and CEC mesh membership remain **separate channels** (see SWARM-12 / SWARM-31).

**Acceptance.** Scenario can bind `hostId`; Screen keeps swarm near host; host death produces a defined autonomous reaction.

## SWARM-12 — LinkState **[P1]**

**Requirement.** Swarm shall track `linkState` ∈ {connected, degraded, lost} driven by range, jam, and host/comms rules consistent with project comms degradation themes (doc 19). **`linkState` is the command/order channel only** (player/agent intent delivery). It is **not** CEC mesh membership — that lives on a separate state (SWARM-31 `cecMeshState` / equivalent). Generic tactical datalink lag remains doc 15 SEN-05.

**Acceptance.** Lost link blocks or delays new player orders per doctrine; panel shows cause ([CMD-17](20-Command-And-Control-UI.md) pattern: unknown-with-reason, not blank). Losing CEC mesh eligibility alone **must not** force `linkState=lost` or sever ordinary C2 orders.

## SWARM-13 — Regeneration **[P1]**

**Requirement.** Near a valid host/base with stores, swarm may regenerate drones over time up to `maxDrones`, subject to logistics rules (doc 16).

**Acceptance.** Without stores, regen does not occur; with stores, count climbs on a documented rate.

## SWARM-14 — C2 panel fields **[P1]**

**Requirement.** Selected swarm unit panel shows: mode, integrity (count/max), energy/endurance if modelled, host, `linkState` (C2), **`cecMeshState` / composite-track contribution when applicable (SWARM-31)**, and primary sensor/weapon summary.

**Acceptance.** Fields update live; missing telemetry uses explicit unknown reasons when link lost.

## SWARM-15 — Doctrine / WRA **[P1]**

**Requirement.** Doctrine shall control auto-engage posture and whether expend (if present) is authorized. WRA classification for swarm munitions/target classes shall be expressible (doc 13). CEC remote-engage (shooter uses another node's track) shall respect the same ROE/WRA gates as organic fire (doc 14).

**Acceptance.** Hold-fire prevents assault shots; authorized expend is the only path to SWARM-19 when that ships.

## SWARM-21 — Catalog + Platform Editor **[P1]**

**Requirement.** Authors can define swarm catalog entries: `maxDrones`, sensors, weapons/munition class, speed/endurance bands, default mode, host constraints, and (Phase B+) **`cecCapable` / CEC mesh membership flags for US and NATO national exemplars** (doc 21).

**Acceptance.**
- **Phase A (mandatory):** catalog schema fields + ≥1 abstract generic swarm preset loadable by Data / scenario refs (DRG-86). Without this, SWARM-01 cannot instantiate. Generic preset is **not** CEC-capable.
- **Phase B:** Platform Editor / PDA full chrome round-trip for swarm rows (or documented PE gap filed); US/NATO exemplars may set CEC flags per SWARM-31. Full PE authoring may lag schema.

## SWARM-22 — Scenario editor **[P1]**

**Requirement.** Scenario editor can place swarms, set initial count ≤ max, assign side/host/mission (doc 11).

**Acceptance.** Round-trip save/load preserves swarm fields; validation catches count > max and missing catalog refs.

## SWARM-23 — Agent delegation **[P1]**

**Requirement.** Delegated agents may issue the same intent orders as humans to swarm units; per-axis or unit-level delegation badges apply as for other units (doc 04).

**Acceptance.** Agent can task swarm assault/screen without UI; order log attributes actor correctly.

## SWARM-26 — Contact classification **[P1]**

**Requirement.** Hostile swarms as contacts expose classification that can distinguish swarm/UAS cloud from single airframe when sensors allow, with confidence ([CMD-29](20-Command-And-Control-UI.md) alignment).

**Acceptance.** Contact panel/projection can show swarm-class label when identified; misclassification possible at low quality.

## SWARM-31 — Cooperative Engagement Capability (CEC) for US and NATO platforms **[P1]**

**Definition (normative vocabulary).** **Cooperative Engagement Capability (CEC)** is a real-time **sensor-netting** and **integrated fire-control** system used primarily by the **U.S. Navy, Marine Corps, and allied (NATO) forces**. It links geographically dispersed ships, aircraft, and land units into a single, highly accurate **composite track picture** of the airspace, allowing **one platform to engage a target using data from another's radar** (or other contributing sensors). Glossary: [12](12-Terms-Glossary.md).

**Requirement.** Project Aegis shall model CEC as a **first-class mesh capability on US and NATO catalog platforms** that interact with drone/UAS swarm units:

1. **Catalog / affiliation gate.** US and NATO national platforms (ships, aircraft, land AA / sensor nodes, and CEC-flagged swarm or UAS nodes when fielded) may declare `cecCapable` (or equivalent). Phase A abstract generic swarm presets are **not** CEC-capable. Non-US/non-NATO platforms do not receive CEC mesh membership by default (may use generic tactical datalink only — doc 15 SEN-05).
2. **Separate mesh health from C2 link.** Implementations **shall** expose a distinct CEC mesh membership / health state (e.g. `cecMeshState` ∈ {inMesh, degraded, outOfMesh} or equivalent). It is layered **on top of** `linkState` and generic datalink (docs 15/19) — **not a replacement**. A CEC-only outage revokes remote-engage / composite contribution eligibility; it **must not** by itself block ordinary player/agent orders on the swarm (SWARM-12).
3. **Composite track picture.** When two or more CEC-capable units on the same side have mesh connectivity (range, EMCON, jam, and CEC mesh health per docs 15/19), the side air picture shall merge organic detections into a **shared composite track** suitable for fire-control quality when doctrine allows — not merely a delayed contact copy.
4. **Remote engage (engage-on-remote-data).** A CEC-capable shooter may resolve engagement against a track whose primary sensor contribution is from another CEC mesh member (including a CEC-capable swarm's aggregate ISR), subject to ROE/WRA/EMCON/DLZ (docs 13–14). Aggregate swarm SoT (SWARM-07) is unchanged: the swarm remains one unit; CEC does not invent per-drone fire-control bodies.
5. **Swarm participation.** A swarm unit may:
   - **Contribute** scaled aggregate sensor quality (SWARM-04) into the CEC composite picture when flagged CEC-capable and `cecMeshState` is live (mesh up), independent of whether `linkState` is connected for orders;
   - **Be engaged** as a composite track by remote CEC shooters (integrity damage still via authorized aggregate path);
   - **Lose mesh membership** when CEC mesh health drops (range/jam/EMCON/doctrine) or integrity reaches zero — composite contributions drop deterministically **without** automatically setting `linkState=lost`.
6. **Determinism & replay.** Mesh join/leave, composite-track quality, and remote-engage authorizations shall be pure functions of scenario seed, unit state, and ordered events (order log / integrity timeline). Same scenario + seed → same CEC outcomes.
7. **Distinct from** generic side datalink share lag (SEN-05), host-only `linkState` without fire-control quality (SWARM-12 alone), and true multi-static ISR geometry (SWARM-29 Phase N).

**Why.** CEC is how modern US/NATO forces fight air defense and UAS threats: one node illuminates, another kills. Modelling it for swarm warfare is required for credible US/NATO scenarios without faking “perfect shared awareness” for every side.

**Acceptance.**
- [ ] Catalog: ≥1 US or NATO surface/air platform row exposes CEC capability; generic `uas-swarm-generic` remains non-CEC.
- [ ] Headless fixture: two CEC nodes (e.g. ship + CEC swarm or ship + aircraft) form a composite track; a third CEC shooter can engage using remote data under identical geometry where organic-only would fail or degrade.
- [ ] Jam / range / CEC mesh-health loss removes remote-engage eligibility with explicit abort or quality reason (not silent blank); ordinary `linkState` C2 may remain connected.
- [ ] Unit model keeps `linkState` and `cecMeshState` (or equivalent) as independent fields in tests/fixtures.
- [ ] Replay/hash: CEC mesh events participate in golden path or documented swarm/CEC fixture (extends SWARM-24 themes).
- [ ] Evidence under `production/qa/` when implementation lands (Phase B child of DRG-92).

**Phase.** **B** (after Phase A host/link/sensor foundations). Surface at kickoff must be split (catalog flags vs sensor mesh vs engage remote track) so parallel agents do not collide.

---

# Phase C — Depth

## SWARM-16 — Formations **[P2]**

Cloud, wall/screen, spear, orbit as soft constraints on member layout and engagement geometry.

## SWARM-17 — Multi-axis auto-split **[P2]**

Assault may split logical mass across approach axes against a single high-value target when doctrine allows.

## SWARM-18 — EMP / jam soft-kill **[P2]**

EMP scatters/freezes mode switches temporarily; jam degrades linkState and order latency (and CEC mesh membership when SWARM-31 is live).

## SWARM-19 — Expend pulse **[P2]**

Authorized order spends N drones for a burst strike; irreversible; logged.

## SWARM-20 — Mission integration **[P2]**

Patrol / support / strike mission types can assign swarms with swarm-appropriate default modes.

---

# Phase N / Won't for MVP

## SWARM-27 — Split / merge **[P3]**

Split one swarm platform into two; merge compatible swarms. High UX and identity cost — defer.

## SWARM-28 — Per-member physics SoT **[P3 / Won't MVP]**

Individual drone bodies as engagement authority. Conflicts with SWARM-07; only if product fantasy demands it and replay strategy is redesigned.

## SWARM-29 — Multi-static ISR mesh **[P3]**

True distributed sensing geometry beyond scaled aggregate quality. **Not a substitute for CEC (SWARM-31):** CEC is real-time composite FC-quality tracks among US/NATO nodes; multi-static ISR is speculative geometry depth.

## SWARM-30 — Full MUM-T packages **[P3]**

Manned flight leads with organic swarms as a package type — after host + air ops maturity. CEC-capable manned leads may host CEC-flagged organic swarms when both ship.

---

# Amendments to existing requirements

Pointers only — full rewrites live in those docs when implementation waves touch them.

| Existing | Amendment |
|----------|-----------|
| Entity / unit model (Data / sim unit) | Swarm platform flag, integrity fields, hostId, linkState (C2), mode; Phase B+ `cecCapable` + independent `cecMeshState` on US/NATO rows |
| [18](18-Combat-Domains.md) | Air / UAS-swarm aspect; integrity-pool damage model |
| [15](15-Sensor-Detection-And-EW.md) | Aggregate ISR scaling; contact class for swarms; **CEC composite track picture (SWARM-31)** |
| [14](14-Engagement-And-Fire-Control.md) | Area-AA vs swarm integrity; note distinct from salvo swarm-slot deconfliction; **CEC remote engage (SWARM-31)** |
| [16](16-Logistics-And-Magazines.md) | Regen stores; host magazine coupling |
| [19](19-Cyber-And-Comms.md) | LinkState for swarm C2 (orders only); **CEC mesh health as a separate channel** |
| [20](20-Command-And-Control-UI.md) | Panel fields (SWARM-14); map density; no per-drone micro; CMD-24 reuse Phase A; CEC membership readout |
| [17](17-Replay-AAR-And-Order-Log.md) | Integrity deltas in golden paths; CEC mesh join/leave events when implemented |
| [21](21-Platform-Editor.md) | Authoring schema for swarm entries + CEC flags for US/NATO |
| [12](12-Terms-Glossary.md) | **CEC** term |
| [01](01-Project-Overview.md) | FR-20 + charter: swarms in scope for air/naval interaction |

---

# Suggested starter tuning (non-normative)

| Knob | Starter |
|------|---------|
| maxDrones | 40 logical |
| renderMembers | **12** (normative Phase A cap = `SwarmPerformanceCaps.RenderMaxMembersPerSwarm`; do not use 24 in fixtures) |
| dpsPerDrone | tune vs soft targets |
| regen | 1 drone / 1.5s near host if stores |
| scatter cooldown | 8s |
| armorClass | light-air |
| cecCapable (Phase A generic) | false |
| CEC mesh join range | scenario/doctrine tunable; jam drops membership before organic radar dies |

---

# Acceptance criteria — Phase A done when

1. Swarm platform spawns, selects, moves, attacks as one unit  
2. Losing drones weakens sensors/weapons measurably  
3. At least one hard counter shreds integrity faster than point fire  
4. Orders are headless + logged; replay/hash stable  
5. Map/panel show integrity without per-drone UI  
6. Performance caps documented and met in a stress scenario  

Phase A **does not** require CEC (SWARM-31) — that is Phase B.

## Delivery mapping (Linear H8)

| Wave | Issues | Scope |
|------|--------|-------|
| 0 | DRG-84 | Owner triage — **Done** |
| 1 | DRG-85 | Land this corpus — **Done** (#412) |
| 2 | DRG-86 → 87 | Catalog/schema; SwarmController MVP — **Done** (#413–414) |
| 3 | DRG-88 ∥ 89 ∥ 90 | Engage / sensors / C2 projection — **Done** (#415–417) |
| 4 | DRG-91 | Replay + caps — **Done** (#418) |
| HOLD | DRG-92 | Phase B umbrella — **split children before dispatch**; include **SWARM-31 CEC** surface when Phase B opens |

## Open questions

Resolved in owner triage 2026-08-09. Implementation open items live on Linear (Phase B DRG-92 children). CEC addendum open product choices (implementation wave):

1. Exact catalog field name (`cecCapable` vs mesh tier enum).  
2. Whether allied non-NATO partners (e.g. Japan/Aus) get opt-in CEC via scenario doctrine or only hard US/NATO tags.  
3. Composite-track quality ladder (organic / datalink / CEC) vs binary remote-engage gate.

## Traceability

| Doc | Relationship |
|-----|----------------|
| Hub **FR-20** ([01](01-Project-Overview.md)) | This document |
| 12 | Glossary: CEC |
| 14 | Engagement scaling / hard counter; **CEC remote engage**; **not** salvo slot deconfliction |
| 15 | Aggregate ISR; **CEC composite track** |
| 16 | Regen logistics |
| 17 | Replay integrity deltas; CEC mesh events |
| 18 | Domain / damage for integrity pools |
| 19 | LinkState; CEC mesh health |
| 20 | Selection, map, panel (Phase A reuse; Phase B Swarm Ops + CEC readout) |
| 21 | Catalog authoring + CEC flags |
| 09–10 | Speculative SWARM-27…30 only (not SWARM-31) |
| ADR-010 | Headless commands |
| ADR-003 | Order log |
| ADR-018 | Side-picture / datalink baseline (CEC builds on, does not replace) |

---

**Implementation grade:** Phase A code **landed** (H8 waves 0–4 / PRs #412–#418). SWARM-31 CEC = Phase B requirements only until DRG-92 children ship.  
**Design Status:** Draft — Phase A accepted; CEC addendum 2026-08-09.  
**References:** Notion research 2026-08-09; real-world CEC (USN/USMC/allied sensor-netting + integrated FC); ADR-010; docs 14/15/18/19/20.
