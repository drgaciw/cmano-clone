# Sprint 127 — Play Mode signoff + gates

**Status:** Planned  
**Dates:** 2026-10-20 – 2026-10-24  
**Predecessor:** [S126 symbology](sprint-126-symbology-subset.md)  
**Linear:** [DRG-208](https://linear.app/drgamtd-workspace/issue/DRG-208) · [DRG-205](https://linear.app/drgamtd-workspace/issue/DRG-205) · [DRG-235](https://linear.app/drgamtd-workspace/issue/DRG-235) · [DRG-236](https://linear.app/drgamtd-workspace/issue/DRG-236)  
**Stage:** **Release** · **Not Launch**  
**Doctrine:** Owner signoff is human. Do not close 208 by inference from smoke.

## Sprint Goal

Owner Play Mode re-signoff with an evidence index. Replay/a11y gates without Phase N.

## Capacity

- 5 days · **4.0d Must** · Must ≈ 3–4d (208 is 1–2d human)

## Must Have

| ID | Task | Est.d | Notes |
|----|------|------:|-------|
| S127-01 | DRG-208 human Play Mode re-signoff | 1.0–2.0 | Requires 244 + entry (S123) + commit (S124) evidence |
| S127-02 | DRG-205 (+ 170 reuse) replay / a11y / usability gates | 1.0 | Do not reopen as greenfield |
| S127-03 | DRG-236 residual / DRG-235 defects | 1.0 | Close **open G2/G3 rows only** |

## Should Have

| ID | Task | Est.d | Notes |
|----|------|------:|-------|
| S127-04 | DRG-198 / 201 / 202 gauntlet slice | ≤1.0 | Do not starve 208 |

## Nice to Have

| ID | Task | Notes |
|----|------|-------|
| S127-05 | DRG-195 agentic provenance | — |
| S127-06 | SYM-04 expand / W2-SYM-02 | Only if S126 green + cap allows |

## Out of sprint

Launch gate. Phase N. Full screen-reader product. Closing 208 without owner.

## Definition of Done

- [ ] DRG-208 owner-accepted **or** explicitly blocked with missing evidence listed
- [ ] DRG-205 gate evidence attached
- [ ] Open G2/G3 defects closed or deferred with id
- [ ] QA sign-off: APPROVED or APPROVED WITH CONDITIONS
- [ ] `/smoke-check sprint` run
- [ ] No S1/S2 in delivered features
