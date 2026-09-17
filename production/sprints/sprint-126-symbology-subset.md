# Sprint 126 — Symbology subset (presentation-only)

**Status:** Planned  
**Dates:** 2026-10-13 – 2026-10-17  
**Predecessor:** [S125 editor honesty](sprint-125-editor-honesty.md)  
**Next:** [S127 Play Mode signoff](sprint-127-play-mode-signoff.md)  
**Linear:** [DRG-231](https://linear.app/drgamtd-workspace/issue/DRG-231) SYM-MIL-01 · [DRG-232](https://linear.app/drgamtd-workspace/issue/DRG-232) SYM-CIV-01  
**Stage:** **Release** · **Not Launch**  
**Doctrine:** ADR-010 · presentation-only · **no sim/order/replay hash mutation** · not APP-6 certification

**Prerequisite:** Wave-1 **SYM-02** scaffold Done **or** explicit waiver. File SYM-02… after S122.0 if cap allows. W2-SYM-05 HOLD.

## Sprint Goal

Dual-profile naval **thin** slice + canonical keys + non-certification legend. Not a full atlas.

## Capacity

- 5 days · **4.0d Must** · Must load **3.0d**

## Must Have

| ID | Task | Est.d | Deps | Acceptance Criteria |
|----|------|------:|------|---------------------|
| S126-01 | W3-SYM-03 freeze HOLD W2-SYM-05 + no certification | 0.25 | — | Affiliation expand stays HOLD |
| S126-02 | N-GATE-SYM-01 wiki gate (MIL-only if CIV HOLD) | 0.25 | — | Wiki gate pack |
| S126-03 | W2-SYM-01 canonical key registry | 0.5 | — | Hash-safe keys |
| S126-04 | SYM-03 headless profile switch | 0.5 | SYM-02 or waiver | Profile switch without world mutation |
| S126-05 | W3-SYM-01 ≥3 dual-profile naval types | 1.0 | S126-03/04 | Thin naval slice |
| S126-06 | W2-SYM-03 subset / not-certified disclaimer | 0.25 | — | Visible disclaimer |
| S126-07 | W3-SYM-02 day-5 evidence pack | 0.25 | above | Evidence filed; DRG-208 unchanged |

## Should Have

| ID | Task | Est.d |
|----|------|------:|
| S126-08 | W2-SYM-06 swarm integrity both profiles | 0.25 |
| S126-09 | W2-SYM-04 dual-axis manifest if SYM-02 incomplete | 0.5 |

## Nice to Have

W2-SYM-02 mono a11y → prefer S127+.

## Out of sprint

W2-SYM-05 HOLD; SYM-01/06 HOLD; CMD-13 LOD; SWARM-27…30; full SYM-04 atlas.

## Definition of Done

- [ ] ≥3 dual types
- [ ] Toggle without hash change
- [ ] Disclaimer visible
- [ ] Evidence filed
- [ ] DRG-208 unchanged
- [ ] QA plan exists or warning accepted
