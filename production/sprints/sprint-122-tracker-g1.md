# Sprint 122 — Tracker honesty + Slice G1 baseline

**Status:** Planned  
**Dates:** 2026-09-15 – 2026-09-19  
**Predecessor:** [S122.0 cap hygiene](sprint-122-0-cap-hygiene.md)  
**Next:** [S123 play-entry](sprint-123-play-entry.md)  
**Linear:** [DRG-234](https://linear.app/drgamtd-workspace/issue/DRG-234) G1 · [DRG-240](https://linear.app/drgamtd-workspace/issue/DRG-240) · [DRG-237](https://linear.app/drgamtd-workspace/issue/DRG-237) · [DRG-180](https://linear.app/drgamtd-workspace/issue/DRG-180)  
**Epic:** [DRG-233](https://linear.app/drgamtd-workspace/issue/DRG-233) Slice G  
**Stage:** **Release** · **Not Launch**  
**Doctrine:** ADR-010 / 007 · no DelegationBridge hotpath · no CatalogWriteGate · no golden edits

## Sprint Goal

Linear tells the truth about Slice B/C. G1 evidence matrix exists so later sprints validate instead of rediscovering.

## Capacity

- Total days: 5 · Buffer 20% · **Available Must: 4.0d** · Should ≤2.0d  
- Must load: **3.75d**

## Must Have

| ID | Task | Agent/Owner | Est.d | Deps | Acceptance Criteria |
|----|------|-------------|------:|------|---------------------|
| S122-01 | DRG-240 Slice B → In Review (165–170) | tracker | 0.25 | S122.0 | Comments with evidence links; state In Review not Backlog |
| S122-02 | DRG-237 Slice C → In Review (+ 185 note) | tracker | 0.25 | S122.0 | Same; DRG-185 In Progress or In Review |
| S122-03 | DRG-241 Surface / single-owner labels | tracker | 0.25 | — | Labels exist; applied to Unity-touching opens |
| S122-04 | DRG-242 + DRG-234 G1 checklist + baseline doc | docs | 1.5 | 241 helpful | `docs/superpowers/reviews/2026-09-*-slice-g-baseline.md`; evidence classes per row |
| S122-05 | DRG-180 continue provenance UI | unity | 1.5 | Slice A | In Progress work continues; does not close Slice A |

## Should Have

| ID | Task | Est.d | Deps |
|----|------|------:|------|
| S122-06 | DRG-275 / N-FRESH-01′ wiki honesty stamp | 0.5 | 234 |
| S122-07 | N-GATE-PLAY-01 Play Mode wiki gate pack | 0.25 | 275 |
| S122-08 | DRG-188 Notion/Linear/GitHub/Graphite wiring | 0.5 | parallel |
| S122-09 | DRG-197 GitNexus reindex (MCP Ladybug v42 vs engine v40) | 0.5–1 | before heavy Unity |

## Nice to Have

| ID | Task | Est.d |
|----|------|------:|
| S122-10 | DRG-238 waterline closeout note | 0.25 |

## Out of sprint

MODE/DEL implementation (file defects to DRG-246/252). Treating DRG-165–175 as greenfield after In Review.

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| Agents re-implement Slice B/C chrome | M | H | In Review comments + Surface labels |
| G1 expands into MODE/DEL | M | H | G1 is matrix only |

## Definition of Done

- [ ] All Must Have complete
- [ ] DRG-240/237 Done or blocked with live state table
- [ ] G1 markdown linked from DRG-234
- [ ] No owner-accepted without human
- [ ] No DelegationBridge / CatalogWriteGate / golden edits
- [ ] QA plan: **missing** — run `/qa-plan sprint` before S123 implementation
