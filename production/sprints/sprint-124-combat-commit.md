# Sprint 124 — Combat-commit honesty

**Status:** Planned  
**Dates:** 2026-09-29 – 2026-10-03  
**Predecessor:** [S123 play-entry](sprint-123-play-entry.md)  
**Next:** [S125 editor honesty](sprint-125-editor-honesty.md)  
**Linear:** [DRG-258](https://linear.app/drgamtd-workspace/issue/DRG-258) optional · [DRG-268](https://linear.app/drgamtd-workspace/issue/DRG-268)  
**Stage:** **Release** · **Not Launch**  
**Doctrine:** ADR-010 presentation-only · no DRG-170/205 reopen · no Cesium / APP-6 LOD

Architect preferred Must **2.75d** (no optional DRG-258).

## Sprint Goal

At fire commit, player sees top constraints + cost and a refuse bark with a loggable reason. Not a glass-cockpit Phase N.

## Capacity

- 5 days · **4.0d Must** · preferred Must **2.75d**

## Must Have

| ID | Task | Est.d | Deps | Acceptance Criteria |
|----|------|------:|------|---------------------|
| S124-01 | W2-C2-01 pre-commit constraint + cost strip | 1.0 | EngagePreview / FireAbort / WRA | Top constraints + magazine cost at commit |
| S124-02 | W3-C2-02 strip binds projections only | 0.25 | S124-01 | No new sim queries; ADR-010 fence |
| S124-03 | W2-C2-04 refuse/drop bark + log reason | 0.25 | MessageLog | Visible bark; reason loggable |
| S124-04 | W3-C2-01 vertical-slice evidence pack | 0.25 | strip+bark | Evidence attached; does not close DRG-208 |
| S124-05 | W2-DEL-04 initial Assign Agent | 0.5 | S123 path | Surface-disjoint from strip host |
| S124-06 | W2-DEL-02 rebrief success path | 0.5 | S124-05 | Success path beyond deny-only DEL-02 |

## Should Have

| ID | Task | Est.d | Notes |
|----|------|------:|-------|
| S124-07 | W2-C2-05 filter lanes | 0.5 | After strip+bark green |
| S124-08 | DRG-268 / C2-13 deep-link refuse row | 0.25 | Stretch |
| S124-09 | W2-DEL-01 badges | 0.5 | If not in S123 |
| S124-10 | DRG-258 abort tooltip | 0.5 | Only if 237/240 Done and 182 slipped |

## Nice to Have

| ID | Task | Est.d |
|----|------|------:|
| S124-11 | W2-C2-02 suitability notches | 0.25 |
| S124-12 | DRG-266 **or** DRG-262 (one only) | 0.5 |

## Out of sprint

W2-C2-03 until ID/abort projection confirmed. DRG-170/205. Scrub/AAR. Cesium/APP-6 LOD.

## Definition of Done

- [ ] Strip + bark + fence + evidence green
- [ ] Filter lanes shipped **or** deferred with reason
- [ ] Baltic hash unchanged
- [ ] DRG-208 not closed by inference
- [ ] QA plan exists or warning accepted
