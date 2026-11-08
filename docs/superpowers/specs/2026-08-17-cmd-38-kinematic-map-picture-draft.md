# CMD-38 — Kinematic Map Picture (DRAFT)

**Status:** Implemented partial (Track B 2026-08-17) — **pending owner approval to land in REQ-20**  
**Do not append** to `Game-Requirements/requirements/20-Command-And-Control-UI.md` until explicitly approved.  
**Date:** 2026-08-17  
**Parent audit:** `docs/superpowers/reviews/2026-08-17-playmode-visual-audit.md`  
**Review mode:** lean (director gates skipped)

---

## Elevator intent

Make the C2 map picture **move like CMANO**: platform icons **slide** between authoritative geographic (or course/speed-derived) positions each tick, and **plotted courses / waypoints** appear as polylines on the map canvas — not as cinematic VFX.

Today, Play Mode shows static ■/◆ from `MapPictureProjection` hash placement because `ISimWorldSnapshot` carries no lat/lon/course/speed. Players who issue Plot Course see a log line, not a sailing unit. CMD-38 closes that product gap without reopening art-bible §7 particles.

---

## Relationship to existing requirements

| ID | Relationship |
|----|----------------|
| **CMD-06** | Phase A placeholder (hash / normalized xy) remains valid until kinematics wire lands; CMD-38 is the **live motion** AC on top of / replacing hash as the *live* layout. Product globe (Phase N / Cesium) stays separate. |
| **CMD-30.7** | Route and mission-area overlays — CMD-38 **consumes** plotted course geometry and draws it; CMD-30.7 owns the overlay taxonomy and declutter rules. |
| **CMD-30.5** | Engagement geometry (illumination / targeting vectors) is **orthogonal** — implement under existing CMD-30.5; not invented here. |
| **ADR-007** | Phase A = hash until sim publishes coordinates; Phase B = world-anchored symbols. CMD-38 is the **presentation contract** for consuming published kinematics (Phase B wire + cosmetic lerp). |
| **ADR-010** | UI remains a **presentation client**: interpolating icon positions between ticks is presentation-only state; sim authority stays on the snapshot / order log. |

---

## Acceptance criteria (draft)

1. **Authoritative pose on the wire.** Each tick (or checkpoint cadence), friendly units and known contacts used for the map picture expose either:
   - WGS84 **lat/lon** (preferred for ADR-007 Phase B), or
   - **course (deg) + speed** plus a last-known position sufficient to advance the picture deterministically for display.
2. **Live layout abandons hash for units with pose.** When pose is present, `MapPictureProjection` (or successor DTO) places symbols from that pose; hash placement remains only for entities without pose (explicit “unknown position” styling).
3. **Icon sliding.** Between sim ticks, the Unity map host may **lerp** symbol screen positions for cosmetic smoothness. Lerp is presentation-only, deterministic given tick poses + wall-clock frame times do not affect order-log / replay hash.
4. **Course polylines.** When a unit has an active plotted course / waypoint list (from player order or mission route), the map draws a **polyline** (UI Toolkit overlay path consistent with `MapCanvasOverlayRenderer`) from current pose through waypoints. Clearing the course removes the polyline.
5. **Plot Course feedback.** Issuing Plot Course updates the polyline within one projection refresh; the message log may still show `PLAYER_ORDER` — map change is mandatory for AC pass.
6. **Destroyed / dead.** Destroyed units stop sliding; glyph/state follows existing map symbol rules (CMD-06 / APP-6).
7. **Headless parity.** Projection DTOs remain testable without Unity; Play Mode and headless smoke can assert polyline vertex counts / pose fields without Game View.

---

## Non-goals

- **Particles, explosions, missile trails as VFX Graph / ParticleSystem** — rejected by art-bible §7 for Baltic v1; reopen only via explicit Track C product decision.
- **Cinematic camera chase or 3D ballistic arcs** — out of scope; 2D icon + line picture only.
- **Making Swarm / other isolated kinematics systems the C2 authority** without snapshot publication.
- **DelegationBridge hotpath edits** — forbidden through Release v1; publish pose via snapshot / projection seams only.
- **Changing Baltic v2 replay golden** `17144800277401907079`.

---

## Dependencies

| Dependency | Notes |
|------------|--------|
| Snapshot / ECS publish of lat/lon **or** course/speed | Blocking for live layout; today absent from `ISimWorldSnapshot` |
| Course / waypoint data from orders or mission runtime | Needed for CMD-30.7 polyline content |
| Map canvas overlay path (UI Toolkit) | Prefer same stack as rings/edges (`MapCanvasOverlayRenderer`) |
| ADR-007 Phase B readiness | Cesium optional for globe; normalized canvas can consume lat/lon projected to xy first (MVP) |

---

## MVP vs later

| Slice | Scope |
|-------|--------|
| **MVP** | Publish lat/lon (or course/speed + last pose) for smoke ORBAT + Baltic session members; stop hash for those ids; lerp icons; draw simple waypoint polylines for Plot Course; headless projection tests |
| **Later** | Full Cesium world-anchor; mission-area fills; multi-unit formation routes; contact estimated tracks with epistemic styling (CMD-29); scrub UI tying into RPL-08 |

---

## Implementation tracks (informational)

CMD-38 lands under **Track B** in the Play Mode visual audit. Track A (richer log) can proceed without CMD-38. Track C (VFX) remains rejected.

---

## Approval gate

- [ ] Owner approves this draft intent and ACs  
- [ ] Owner authorizes append of a numbered **CMD-38** block to REQ-20  
- [x] Implementation from draft exception (Track B, 2026-08-17) — REQ-20 not appended

*Pending owner approval to land in REQ-20. Headless pose + Play Mode stub motion landed; Cesium world-anchor and full Baltic ECS publish remain later.*
