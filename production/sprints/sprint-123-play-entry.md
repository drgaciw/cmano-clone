# Sprint 123 — Play-entry / loop honesty (+ Slice A close)

**Status:** Planned  
**Dates:** 2026-09-22 – 2026-09-26  
**Predecessor:** [S122 tracker + G1](sprint-122-tracker-g1.md)  
**Next:** [S124 combat-commit](sprint-124-combat-commit.md)  
**Linear:** [DRG-243](https://linear.app/drgamtd-workspace/issue/DRG-243) · [DRG-246](https://linear.app/drgamtd-workspace/issue/DRG-246) · [DRG-182](https://linear.app/drgamtd-workspace/issue/DRG-182) · [DRG-183](https://linear.app/drgamtd-workspace/issue/DRG-183)  
**Stage:** **Release** · **Not Launch**  
**Doctrine:** ADR-010 — UI never owns world truth · Baltic hash unchanged

Architect cut: raw Must ~4.5d **UNREALISTIC**. Preferred Must **3.75d** (park DRG-251 + Reset).

## Sprint Goal

Load Baltic → real briefing → mode + side → Begin Execution → one tick. Slice A targetability closable. Does **not** close DRG-208.

## Capacity

- 5 days · **4.0d Must** · Must load **3.75d**

## Must Have

| ID | Task | Est.d | Deps | Acceptance Criteria |
|----|------|------:|------|---------------------|
| S123-01 | DRG-243 PLAY-ENTRY load → Planning | 0.5 | G1 gaps known | Package list + load; failed resolve non-mutating |
| S123-02 | W3-CORE-02 Baltic briefing fixture fields | 0.25 | — | Fixture fields present for briefing bind |
| S123-03 | W2-CORE-01 briefing **content** panel | 0.5 | 243, W3-CORE-02 | Player-visible briefing content (not PLAY-ENTRY chrome) |
| S123-04 | DRG-246 MODE-01 enum selector | 0.5 | surface label | Human / Mixed / AvA via façade |
| S123-05 | W2-MODE-01 play-side picker | 0.5 | 246 | Side pick after mode enum |
| S123-06 | W3-MODE-01 Begin Execution gated on mode+side | 0.25 | 246, W2-MODE-01 | Cannot Begin without both |
| S123-07 | W3-CORE-01 golden-path smoke | 0.5 | above | Load → briefing → mode+side → Begin → one tick |
| S123-08 | DRG-182 authority / ROE UI (shrunk) | 0.5 | — | Residual chrome; not a doctrine editor rewrite |
| S123-09 | DRG-183 Slice A acceptance harness | 0.5 | 180 | Harness green / DRG-178 ready In Review |

## Should Have

| ID | Task | Est.d |
|----|------|------:|
| S123-10 | DRG-251 loop-policy chrome | 0.25 |
| S123-11 | W3-CORE-03 Reset → Planning | 0.25 |
| S123-12 | W2-CORE-02′ ROE acknowledge (relate 182) | 0.25 |
| S123-13 | DRG-244 evidence index under 208 | 0.25 |
| S123-14 | DRG-239 TG/coverage fixture | 0.5 |
| S123-15 | W2-DEL-01 badges if surface free after 246 | 0.5 |

## Nice to Have

| ID | Task | Est.d |
|----|------|------:|
| S123-16 | DRG-181 sensor-to-shooter | 0.5 |
| S123-17 | DRG-245 / 249 MODE-02/03 | 0.25 |
| S123-18 | W2-MODE-02′ compression copy (soft-hold) | 0.25 |
| S123-19 | N-GATE-PLAY-01 if not done in S122 | 0.25 |

## Out of sprint

Closing DRG-208 by inference. Phase N spectator. Dual-side TEST SANDBOX (MODE-02) unless Nice lands.

## Risks

| Risk | Mitigation |
|------|------------|
| Surface collision with S124 commit strip | Surface card from S122.0-05 |
| Must slip over 4d | Drop 182 to Should before dropping smoke |

## Definition of Done

- [ ] All Must Have complete
- [ ] Golden-path smoke green
- [ ] Entry evidence linked for DRG-208 (208 stays open)
- [ ] Slice A harness green
- [ ] ADR-010; Baltic hash intact
- [ ] QA plan exists or warning accepted
