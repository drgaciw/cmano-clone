# Sprint 122.0 — Cap hygiene (pre-flight)

**Status:** Planned  
**Dates:** 2026-09-15 – 2026-09-16 (day-0 of S122 week)  
**Predecessor:** S121 overlays residual-scope  
**Next:** [S122 tracker + G1](sprint-122-tracker-g1.md)  
**Index:** [S122–S127](sprint-122-127-index.md)  
**Stage:** **Release** · **Not Launch**  
**Source:** wave3 architect plan §S122.0

> Restore Linear headroom. Remainder stays out of Must until filed. Do not create issues until archive smoke (or explicit waiver).

## Sprint Goal

Bulk-archive Done issues (or waive), freeze the wave-1 remainder queue, and prove a create-smoke **or** commit that S122–S125 Must use existing DRGs only.

## Capacity

- Calendar: 2 days (hygiene gate, not a full 5-day Must week)
- Available: **0.75d Must** · 0.5d Should

## Must Have

| ID | Task | Est.d | AC |
|----|------|------:|-----|
| S122.0-01 | **W2-HYG-01** bulk-archive Done in Linear UI (`Ctrl+Shift+E`) | 0.25 | DRG-206 (and other Done) have `archivedAt` set; disappear from default Done list |
| S122.0-02 | **W2-HYG-02** remainder filing checklist | 0.25 | Ordered queue: N-ORD-01 → AUTH-02…16 → SYM-02…08; no AC rewrites |
| S122.0-03 | **W3-HYG-03** archive smoke **or** waiver | 0.25 | One test `save_issue` succeeds **or** written waiver: “S122–S125 Must use existing DRGs only” |

## Should Have

| ID | Task | Est.d |
|----|------|------:|
| S122.0-04 | **W3-HYG-01** tag Highs into S122–S127 (labels/cycles — human) | 0.25 |
| S122.0-05 | **W3-HYG-02** Surface collision card for S122–S124 | 0.25 |

## Nice to Have

| ID | Task | Est.d |
|----|------|------:|
| S122.0-06 | **W2-HYG-04** headroom log | 0.25 |

## Carryover

None — this is the program gate.

## Risks

| Risk | Mitigation |
|------|------------|
| Done mistaken for Archive (Free 250 still binds) | Verify `archivedAt` on DRG-206 before filing remainder |
| Filing W2/W3 ids mid-cap | Waiver: parent comments on existing DRGs |

## Definition of Done

- [ ] Archive smoke succeeds **or** explicit waiver recorded on this page
- [ ] Remainder checklist exists (do not file if cap still binds)
- [ ] S122 tracker work may start
- [ ] No Phase N issues created
- [ ] QA plan: not required for this hygiene gate
