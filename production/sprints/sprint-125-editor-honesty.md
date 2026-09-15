# Sprint 125 — Editor honesty

**Status:** Planned  
**Dates:** 2026-10-06 – 2026-10-10  
**Predecessor:** [S124 combat-commit](sprint-124-combat-commit.md)  
**Next:** [S126 symbology](sprint-126-symbology-subset.md)  
**Linear:** [DRG-274](https://linear.app/drgamtd-workspace/issue/DRG-274) AUTH-01 · [DRG-236](https://linear.app/drgamtd-workspace/issue/DRG-236) G3 · [DRG-234](https://linear.app/drgamtd-workspace/issue/DRG-234)  
**Stage:** **Release** · **Not Launch**  
**Doctrine:** ADR-011 / 013–017 · Excel-primary · **no ADR-013 parser** · CatalogWriteGate extend-only

Must load **3.5d**. File AUTH-02 as DRG-236 child after S122.0 if cap allows.

## Sprint Goal

ME/PE honesty visible: G3 evidence, save≠export, Excel contract/quarantine/binding. No editor rewrite.

## Capacity

- 5 days · **4.0d Must** · Must load **3.5d**

## Must Have

| ID | Task | Est.d | Deps | Acceptance Criteria |
|----|------|------:|------|---------------------|
| S125-01 | AUTH-01′ / DRG-274 G3 ME evidence package | 0.5 | DRG-234 | Real Editor screenshots; success + recovery paths |
| S125-02 | AUTH-02 PE evidence (236 child) | 0.5 | DRG-234; file after S122.0 | Same bar for Platform Editor |
| S125-03 | W3-AUTH-01 evidence-first DoD pin | 0.25 | — | DoD freeze honored |
| S125-04 | AUTH-04 save vs export chrome | 0.25 | — | Save ≠ export demo |
| S125-05 | AUTH-03 Doc 11 / verb honesty | 0.5 | — | Verbs match write-gate |
| S125-06 | AUTH-08 workbook contract CI | 0.5 | — | Contract CI; silent-drop counts |
| S125-07 | AUTH-12 dbRef / snapshot binding | 0.5 | — | Binding visible |
| S125-08 | AUTH-13 + AUTH-09 quarantine UX + LatLon messaging | 0.5 | — | Quarantine + messaging; no parser |

## Should Have

| ID | Task | Est.d |
|----|------|------:|
| S125-09 | AUTH-05…07, AUTH-16 demo/Teleport/ADR-016/dirty | 1.0 |
| S125-10 | W2-AUTH-01, W2-AUTH-02 | 0.5 |
| S125-11 | AUTH-10 migrate persistence (drop first if slip) | 0.5 |
| S125-12 | N-GATE-ED-01 editor wiki gate | 0.25 |

## Nice to Have

W3-AUTH-03, W3-AUTH-02, AUTH-15, W2-AUTH-03…08 (04 derived-only).

## Out of sprint

AUTH-11, AUTH-14, `.scen` parser, WYSIWYG, Scenario Lab, Lua.

## Definition of Done

- [ ] AUTH-01/02 evidence attached to 236/274
- [ ] Save≠export demo
- [ ] Excel write-gate only
- [ ] W3-AUTH-01 freeze honored
- [ ] QA plan exists or warning accepted
