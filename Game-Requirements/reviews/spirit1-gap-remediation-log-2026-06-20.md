# Spirit 1 Gap Remediation Log

**Date:** 2026-06-20  
**Authority:** [spirit1-vertical-slice-gap-analysis-2026-06-05.md](spirit1-vertical-slice-gap-analysis-2026-06-05.md) §5 Recommendations  
**Scope:** Docs-only remediation track (parallel-safe with G1/G5 code/tooling tracks)  
**Milestone alias:** [vertical-slice-mvp-spirit1-alias-2026-06-20.md](../../production/milestones/vertical-slice-mvp-spirit1-alias-2026-06-20.md)

## Summary

| Rec | Gap | Title | Status | Deliverable |
|-----|-----|-------|--------|-------------|
| R3 | — | Treat `SimulationSession` as frozen hub | **COMPLETE** | [adr-simulation-session-frozen-hub-spirit1-2026-06-20.md](../../docs/architecture/adr-simulation-session-frozen-hub-spirit1-2026-06-20.md) |
| R4 / G2 | G2 | Label the FSM honestly | **COMPLETE** | [spirit1-classify-lifecycle-vs-fsm-2026-06-20.md](spirit1-classify-lifecycle-vs-fsm-2026-06-20.md) |
| R5 | — | Re-issue Spirit 1 planning artifacts | **COMPLETE** | [vertical-slice-mvp-spirit1-alias-2026-06-20.md](../../production/milestones/vertical-slice-mvp-spirit1-alias-2026-06-20.md) + gap analysis header note |

Recommendations R1 (graph rebuild / G5), R2 (Unity traceability / G1) are **out of scope** for this docs-only track.

---

## R3 — SimulationSession frozen hub

**Status:** **COMPLETE** (2026-06-20)

**Action:** Accepted ADR addendum documenting CRITICAL blast radius (68 symbols), mandatory GitNexus impact-before-edit gate, and optional future interface-extraction note. No product code changed.

**Artifact:** [docs/architecture/adr-simulation-session-frozen-hub-spirit1-2026-06-20.md](../../docs/architecture/adr-simulation-session-frozen-hub-spirit1-2026-06-20.md)

**Evidence cited:** Gap analysis §4 (`impact(SimulationSession, upstream)` = CRITICAL, 68 symbols); §5 item 3.

---

## G2 / R4 — Label FSM honestly

**Status:** **COMPLETE** (2026-06-20)

**Action:** Tracker cross-ref doc maps req 15 MVP scripted lifecycle (`ClassifyAfterTicks` / `IdentifyAfterTicks`, `PdDetectionContactSimulator`) vs post-MVP true FSM. Reserved placeholder symbol **`ContactSensorConfidenceFsm`** for ECCM Phase 2 traceability.

**Artifact:** [Game-Requirements/reviews/spirit1-classify-lifecycle-vs-fsm-2026-06-20.md](spirit1-classify-lifecycle-vs-fsm-2026-06-20.md)

**Evidence cited:** Gap analysis §2 G2, §1 #4, §5 item 4; implementation tracker req 15.

---

## R5 — Re-issue Spirit 1 planning artifacts

**Status:** **COMPLETE** (2026-06-20)

**Action:** Canonical name-mapping doc ("Spirit 1" → Baltic Vertical Slice MVP / Phase 1) with pointer to [vertical-slice-mvp.md](../../production/milestones/vertical-slice-mvp.md). One-paragraph backward-compat header note added to original gap analysis.

**Artifacts:**

- [production/milestones/vertical-slice-mvp-spirit1-alias-2026-06-20.md](../../production/milestones/vertical-slice-mvp-spirit1-alias-2026-06-20.md)
- Updated header on [spirit1-vertical-slice-gap-analysis-2026-06-05.md](spirit1-vertical-slice-gap-analysis-2026-06-05.md)

**Evidence cited:** Gap analysis §0 premise corrections, §5 item 5.

---

## Out of scope (other tracks)

| Rec | Gap | Owner track |
|-----|-----|-------------|
| R1 | G5 | Tooling — `npx gitnexus analyze --force` |
| R2 | G1 | Code — Unity adapter seam for `SensorC2PanelHost` graph linkage |
