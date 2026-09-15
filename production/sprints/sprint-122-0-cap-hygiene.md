# Sprint 122.0 — Cap hygiene (pre-flight)

**Status:** Planned (rev 2, 2026-09-15 — amended per backlog review; see Change log)  
**Dates:** 2026-09-15 – 2026-09-16 (day-0 of S122 week)  
**Predecessor:** S121 overlays residual-scope  
**Next:** [S122 tracker + G1](sprint-122-tracker-g1.md)  
**Index:** [S122–S127](sprint-122-127-index.md)  
**Stage:** **Release** · **Not Launch**  
**Source:** wave3 architect plan §S122.0 · [backlog review 2026-09-15](https://claude.ai/artifact/QnFNFwjdKPQSiiFYqHteAb)

> Restore Linear headroom. Remainder stays out of Must until filed. Do not create issues until archive smoke (or explicit waiver).

## Sprint Goal

Bulk-archive Done issues (or waive), freeze the wave-1 remainder queue, put the program into Linear cycles, and prove a create-smoke **or** commit that S122–S125 Must use existing DRGs only.

## Capacity

- Calendar: 2 days (hygiene gate, not a full 5-day Must week)
- Available: **2.0d Must** · 0.75d Should (rev 2: hygiene rows added from the review; still a gate, not a feature day)

## Must Have

| ID | Task | Owner | Est.d | AC |
|----|------|-------|------:|-----|
| S122.0-01 | **W2-HYG-01** bulk-archive Done in Linear UI (`Ctrl+Shift+E`) | human | 0.25 | DRG-206 (and other Done) have `archivedAt` set; both projects together under the 250 Free cap (278 unarchived on 09-15, ~190 Done) |
| S122.0-02 | **W2-HYG-02** remainder filing checklist | agent | 0.25 | Ordered queue: N-ORD-01 → AUTH-02…16 → SYM-02…08; no AC rewrites. Default is checklists on DRG-236 / DRG-231 / DRG-232, not new issues |
| S122.0-03 | **W3-HYG-03** archive smoke **or** waiver | agent | 0.25 | One test `save_issue` succeeds **or** written waiver: "S122–S125 Must use existing DRGs only" |
| S122.0-04 | **W3-HYG-01′** create six weekly cycles (09-15→09-19 … 10-20→10-24) and assign each sprint file's Must rows | human (Linear UI; MCP has no cycle tool) | 0.25 | Every S122–S127 Must row's DRG sits in its cycle; nothing Must-tagged is left cycle-less. Labels are not a substitute |
| S122.0-05 | **W3-HYG-02** Surface collision card for S122–S124, **including a docs-surface row** | agent | 0.25 | Card names one owner per Unity host *and* per `docs/engineering/*.md` path; `docs/engineering/README.md` listed as a shared hotspot with one index owner per wave |
| S122.0-06 | **HYG-BUG** bug-ledger verify pass + guard | agent | 0.5 | Per `docs/superpowers/plans/2026-09-01-open-bug-backlog-remediation.md` §1–2: the 14 `Open` reports whose fix is on `main` flipped to `Verified Fixed` (Closed needs human), `bug-ledger-check.sh` wired into the bash CI parity script. `BUG-scoring-penalises-roe-correct-refusals` stays open for the S123 owner walk |
| S122.0-07 | **QA-PLAN** run `/qa-plan sprint` for S122 and S123 | agent | 0.25 | `production/qa/qa-plan-sprint-122-*.md` and `-123-*.md` exist; S122/S123 DoD no longer say "warning accepted" |

## Should Have

| ID | Task | Est.d |
|----|------|------:|
| S122.0-08 | **W2-HYG-04** headroom log | 0.25 |
| S122.0-09 | Docs-PR merge order: land #628–#642 serially in stack order (or collapse to one stack); retitle #637 (it adds `identity-classification-projection.md`, not the HUD guide) | 0.5 |

## Nice to Have

| ID | Task | Est.d |
|----|------|------:|
| S122.0-10 | Notion research pages older than the Linear waterline: stamp closed-history (H8 tracking + workflow pages done 09-15; sweep the rest) | 0.25 |

## Carryover

None — this is the program gate.

## Risks

| Risk | Mitigation |
|------|------------|
| Done mistaken for Archive (Free 250 still binds) | Verify `archivedAt` on DRG-206 before filing remainder |
| Filing W2/W3 ids mid-cap | Waiver: parent comments on existing DRGs |
| Cycles skipped "because labels" | S122.0-04 AC requires cycles; the sprint files carry day estimates Linear never sees otherwise |
| Docs PRs merged in parallel | README index conflicts thirteen PRs; serial merge only (S122.0-09) |

## Definition of Done

- [ ] Archive smoke succeeds **or** explicit waiver recorded on this page
- [ ] Remainder checklist exists (do not file if cap still binds)
- [ ] Six cycles exist and S122–S127 Must rows are assigned
- [ ] Collision card has host **and** docs rows
- [ ] Bug-ledger statuses flipped; guard script runs in CI parity
- [ ] QA plans for S122 and S123 exist
- [ ] S122 tracker work may start
- [ ] No Phase N issues created

## Change log

- **2026-09-15 rev 2** (backlog review): cycles made a Must with an AC (was "labels/cycles — human" Should); docs-surface row added to the collision card; bug-ledger verify pass + guard and `/qa-plan` added as Must; docs-PR serial-merge and Notion stale-page sweep added. Capacity restated 0.75d → 2.0d Must.
- **2026-09-13 rev 1**: wave3 architect plan §S122.0.
