# Sprint 123 — Play-entry / loop honesty (+ Slice A close + interim owner walk)

**Status:** Planned (rev 2, 2026-09-15 — thinned per backlog review; see Change log)  
**Dates:** 2026-09-22 – 2026-09-26  
**Predecessor:** [S122 tracker + G1](sprint-122-tracker-g1.md)  
**Next:** [S124 combat-commit](sprint-124-combat-commit.md)  
**Linear:** [DRG-243](https://linear.app/drgamtd-workspace/issue/DRG-243) · [DRG-246](https://linear.app/drgamtd-workspace/issue/DRG-246) · [DRG-183](https://linear.app/drgamtd-workspace/issue/DRG-183) · [DRG-244](https://linear.app/drgamtd-workspace/issue/DRG-244) · [DRG-208](https://linear.app/drgamtd-workspace/issue/DRG-208) (interim walk, owner)  
**Stage:** **Release** · **Not Launch**  
**Doctrine:** ADR-010 — UI never owns world truth · Baltic hash unchanged  
**Blocker:** [DRG-197](https://linear.app/drgamtd-workspace/issue/DRG-197) GitNexus reindex must be usable (or the CLI `--repo` fallback recorded in the S122 closeout) before any Unity symbol edit in this sprint. Linear carries this as a `blocks` relation on DRG-243 and DRG-246.

Architect cut (rev 1): raw Must ~4.5d **UNREALISTIC**. Rev 2 Must **3.5d** agent + **0.5d owner** (interim walk).

## Sprint Goal

Load Baltic → real briefing → mode + side → Begin Execution → one tick. Slice A targetability closable. Owner walks the golden path at end of sprint as an **interim** DRG-208 checkpoint. Does **not** close DRG-208.

## Capacity

- 5 days · **4.0d Must** · Must load **3.5d** agent + 0.5d owner (day 5)

## Must Have

| ID | Task | Est.d | Deps | Acceptance Criteria |
|----|------|------:|------|---------------------|
| S123-01 | DRG-243 PLAY-ENTRY load → Planning | 0.5 | G1 gaps known; DRG-197 | Package list + load; failed resolve non-mutating |
| S123-02 | W3-CORE-02 Baltic briefing fixture fields | 0.25 | — | Fixture fields present for briefing bind |
| S123-03 | W2-CORE-01 briefing **content** panel | 0.5 | 243, W3-CORE-02 | Player-visible briefing content (not PLAY-ENTRY chrome) |
| S123-04 | DRG-246 MODE-01 enum selector | 0.5 | surface label; DRG-197 | Human / Mixed / AvA via façade |
| S123-05 | W2-MODE-01 play-side picker | 0.5 | 246 | Side pick after mode enum |
| S123-06 | W3-MODE-01 Begin Execution gated on mode+side | 0.25 | 246, W2-MODE-01 | Cannot Begin without both |
| S123-07 | W3-CORE-01 golden-path smoke | 0.5 | above | Load → briefing → mode+side → Begin → one tick |
| S123-08 | DRG-183 Slice A acceptance harness | 0.5 | 180 (Done #626), 181/182 merged in S122 | Harness green / DRG-178 ready In Review |
| S123-09 | DRG-244 evidence index under 208 | 0.25 | S123-07 | Index lists entry-path evidence with commit SHAs; feeds the walk |
| S123-10 | **DRG-208 interim owner walk** (day 5, owner-only, not agent-dispatchable) | 0.5 owner | S123-07, S123-09 | Owner comments on DRG-208: golden path pass/fail at the S123 tip. Also records the design decision on `BUG-scoring-penalises-roe-correct-refusals`. Final re-signoff remains S127. |

## Should Have

| ID | Task | Est.d |
|----|------|------:|
| S123-11 | W3-CORE-03 Reset → Planning | 0.25 |
| S123-12 | W2-CORE-02′ ROE acknowledge (relate 182) | 0.25 |
| S123-13 | DRG-239 TG/coverage fixture | 0.5 |

## Nice to Have

| ID | Task | Est.d |
|----|------|------:|
| S123-14 | DRG-245 / 249 MODE-02/03 | 0.25 |
| S123-15 | W2-MODE-02′ compression copy (soft-hold) | 0.25 |
| S123-16 | N-GATE-PLAY-01 if not done in S122 | 0.25 |

## Out of sprint

- DRG-181 sensor-to-shooter chrome (#625) and DRG-182 authority/ROE chrome (#627): **reviewed and merged in S122**, not rebuilt here. If either is still open on 09-22, it is an S122 carry-over reviewed first, not an S123 build row.
- DRG-251 loop-policy chrome → S124 Should or later.
- W2-DEL-01 delegation badges (DRG-252) → S124-09 already holds it.
- Closing DRG-208 by inference. Phase N spectator. Dual-side TEST SANDBOX (MODE-02) unless Nice lands.

## Risks

| Risk | Mitigation |
|------|------------|
| DRG-197 reindex still failing on 09-22 | Use global GitNexus CLI with `--repo <worktree>`; record which path produced the impact report in the PR |
| Surface collision with S124 commit strip | Surface card from S122.0-05 |
| Owner unavailable day 5 | Walk slides to S124 day 1; S124 Must does not start on the same host until it is done |
| Must slip over 3.5d | Drop S123-13 then S123-12 before dropping smoke or the walk |

## Definition of Done

- [ ] All Must Have complete
- [ ] Golden-path smoke green
- [ ] DRG-244 index links entry evidence at the sprint tip SHA
- [ ] Owner interim walk recorded on DRG-208 (208 stays open)
- [ ] Slice A harness green
- [ ] ADR-010; Baltic hash intact
- [ ] QA plan exists (run `/qa-plan sprint` in S122.0; "warning accepted" is no longer a pass)

## Change log

- **2026-09-15 rev 2** (backlog review, [artifact](https://claude.ai/artifact/QnFNFwjdKPQSiiFYqHteAb)): removed DRG-181/182 (in flight, S122 review), DRG-251 and W2-DEL-01 (moved out); promoted DRG-244 to Must; added DRG-208 interim owner walk as S123-10; named DRG-197 as the sprint blocker with the CLI fallback; QA plan made a hard DoD item. Must 3.75d → 3.5d agent.
- **2026-09-13 rev 1**: wave3 architect plan §S123.
