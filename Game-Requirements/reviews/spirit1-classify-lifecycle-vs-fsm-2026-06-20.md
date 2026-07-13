# Spirit 1 — Classify/Identify: Scripted Lifecycle vs True FSM (G2 / R4)

**Date:** 2026-06-20  
**Authority:** [spirit1-vertical-slice-gap-analysis-2026-06-05.md](spirit1-vertical-slice-gap-analysis-2026-06-05.md) §2 G2, §5 item 4 (Recommendation R4)  
**Requirement:** Game-Requirements **req 15** — Sensor Detection & EW ([implementation-tracker-2026-06-04.md](../implementation-tracker-2026-06-04.md) row 15; [sensor-detection-ew GDD](../../design/gdd/sensor-detection-ew.md))  
**Remediation log:** [spirit1-gap-remediation-log-2026-06-20.md](spirit1-gap-remediation-log-2026-06-20.md)

## Purpose

Honest labeling of what the Vertical Slice MVP ships for "Contact Classify/Identify FSM" versus what req 15 defers to post-MVP. Closes gap **G2** (scripted lifecycle, not emergent state machine) without implying a functional defect.

---

## Tracker cross-reference

| Artifact | What it says | MVP vs post-MVP |
|----------|--------------|-----------------|
| [spirit1-vertical-slice-gap-analysis-2026-06-05.md](spirit1-vertical-slice-gap-analysis-2026-06-05.md) §1 #4 | Must-Ship "Contact Classify/Identify FSM" maps to DTO lifecycle + order-log transitions | **MVP — COVERED** (95 % completeness; G2 caveat) |
| [spirit1-vertical-slice-gap-analysis-2026-06-05.md](spirit1-vertical-slice-gap-analysis-2026-06-05.md) §2 G2 | No `*Fsm`/`*StateMachine` class; `ClassifyAfterTicks` / `IdentifyAfterTicks` drive promotions | **By design for MVP** — Low severity |
| [implementation-tracker-2026-06-04.md](../implementation-tracker-2026-06-04.md) req 15 | **Partial** (MVP slice **COVERED**); pending: **ECCM Phase 2**; full TR-sensor-004 workbook round-trip | MVP slice done; full req 15 corpus incomplete |
| [vertical-slice-mvp.md](../../production/milestones/vertical-slice-mvp.md) | "Contact Classify FSM" Must-Ship — **Complete** | Milestone scope only |
| [story-001-classify-identify-fsm.md](../../production/epics/sensor-classify-slice/story-001-classify-identify-fsm.md) | Epic story title uses "FSM"; acceptance is tick-threshold promotions | Naming is **product shorthand** for lifecycle transitions |

---

## MVP implementation (scripted lifecycle)

What ships today is a **deterministic, replay-stable scripted lifecycle**, not an emergent sensor-confidence state machine.

| Layer | Symbol / artifact | Role |
|-------|-------------------|------|
| Policy DTO (JSON) | `ScenarioPolicyJsonDto` → `ClassifyAfterTicks`, `IdentifyAfterTicks` | Scenario-authored tick thresholds (0 = disabled) |
| Sim record | `ScenarioContactLifecycle` | Loaded thresholds bound to scenario policy |
| Simulator | `PdDetectionContactSimulator` | Counts sustained detections; emits Detected → Classified → Identified promotions |
| State enum | `ContactLifecycleState` | Order-log and merge semantics (incl. Lost, datalink merge via `DatalinkSidePictureMerger`) |
| Order log | `ContactChange` / `FromContactChange` process | Auditable transitions for replay golden tests |
| Tests | `PdContactClassifyTests`, `ReplayGoldenBalticClassifyTests`, `BalticReplayHarnessContactTests` | Functional + golden coverage |

**MVP contract:** Given fixed scenario policy and seed, classification timing is **fully predictable** from tick counts — required for `/replay-verify` and Baltic golden hashes.

---

## Post-MVP intent (true FSM)

Req 15 and the implementation tracker defer **sensor-confidence / ECCM-driven** classification to **ECCM Phase 2** (and related datalink-delay modeling beyond bounded catalog lag). That work implies:

- Transitions driven by **EW duel outcomes**, jamming, confidence decay, and ECCM counters — not fixed `*AfterTicks` alone.
- Emergent states that cannot be fully authored in static scenario JSON.
- Traceability via a **named type** in the graph (today absent — gap G2).

---

## Future symbol placeholder (ECCM Phase 2)

| Field | Value |
|-------|--------|
| **Placeholder name** | `ContactSensorConfidenceFsm` |
| **Status** | **Not implemented** — reserved for post-MVP req 15 / ECCM Phase 2 |
| **Intended home** | `ProjectAegis.Sim.Sensors` (alongside `PdDetectionContactSimulator`) |
| **Replaces / wraps** | Tick-threshold promotions where ECCM and confidence rules apply; MVP `ClassifyAfterTicks` / `IdentifyAfterTicks` remain valid for Baltic baseline scenarios |
| **Traceability** | When implemented, register in GitNexus and link from req 15 tracker row and [sensor-detection-ew.md](../../design/gdd/sensor-detection-ew.md) |

Until `ContactSensorConfidenceFsm` exists, **do not** refer to MVP classify behavior as a full sensor-confidence FSM in architecture docs or requirement trace matrices — use **"scripted contact lifecycle"** or **"tick-threshold classify/identify promotions."**

---

## Naming guidance (immediate)

| Say this (MVP) | Avoid (implies post-MVP FSM) |
|----------------|------------------------------|
| Scripted contact lifecycle | Emergent classification FSM |
| Tick-threshold classify/identify | Sensor-confidence state machine |
| `ClassifyAfterTicks` / `IdentifyAfterTicks` policy fields | ECCM-driven transition table |
| G2 acknowledged, Low severity | Must-Ship functional gap |

Epic/story titles may retain "FSM" for historical traceability if this cross-ref is linked in the story header.
