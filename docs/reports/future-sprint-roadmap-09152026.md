# Future Sprint Roadmap - S122.0–S127 Program

**Date:** 2026-09-15. **Stage:** Release (not Launch). **Scope:** Adopts the numbered S122.0–S127 program (PR #636) as the current roadmap and records the 2026-09-15 backlog reconciliation across Notion, Linear and the sprint files.

This snapshot supersedes the [September 8 Slice G snapshot](future-sprint-roadmap-09082026.md) as the canonical roadmap. Slice G (DRG-233) is folded into the program rather than tracked as a separate bounded update: G1 is S122, G3 is S125, G2 residuals and DRG-208 final signoff are S127. Release v1 / Spirit 1 stays closed; Phase N and Launch remain parked.

## Program

Canonical sprint records: [`production/sprints/sprint-122-127-index.md`](../../production/sprints/sprint-122-127-index.md). Machine status: [`production/sprint-status-s122-s127.yaml`](../../production/sprint-status-s122-s127.yaml).

| Sprint | Dates | Theme | Anchor issues | Must |
| --- | --- | --- | --- | --- |
| S122.0 | 09-15 → 09-16 | Linear cap hygiene, remainder filing, cycles | DRG-206 archive smoke | 2.0d|
| S122 | 09-15 → 09-19 | Tracker honesty + Slice G1 baseline | DRG-234 · 242 · 275 · 188 · 197 | 4.0d |
| S123 | 09-22 → 09-26 | Play-entry / loop honesty + interim owner walk | DRG-243 · 246 · 183 · 244 · 208 (interim) | 3.5d + 0.5d owner |
| S124 | 09-29 → 10-03 | Combat-commit honesty | DRG-268 · (258) | 2.75d |
| S125 | 10-06 → 10-10 | Editor honesty, Slice G3 | DRG-274 · 236 | 3.5d |
| S126 | 10-13 → 10-17 | Symbology thin naval slice | DRG-231 · 232 | 3.0d |
| S127 | 10-20 → 10-24 | Owner Play Mode re-signoff + gates | DRG-208 · 205 · 235 · 236 | 3–4d |

Capacity per sprint file: 5 calendar days, 20% buffer, 4.0d Must ceiling.

## Reconciliation recorded 2026-09-15

Source: [backlog review](https://claude.ai/artifact/QnFNFwjdKPQSiiFYqHteAb) (rev 3).

- Notion Hub "Next", the Delivery archive intro, the S121 plan page and the Combat UX callout now name S122.0–S127 as the current program. The "no S122" guard from the S121 era is withdrawn; DRG-208 is an acceptance gate, not a start gate.
- Linear: Slice B stories DRG-165…170 moved to In Review with evidence on DRG-240 (Done); DRG-237 canceled as overtaken (Slice C UI Done 09-15); epics DRG-184/185 In Progress; DRG-197 raised to High. Blocking relations: DRG-243/239/244/183 → DRG-208; DRG-197 → DRG-235/236/243/246; DRG-234 → DRG-235/236.
- Notion H8 Drone Swarm tracking and workflow pages stamped closed history (DRG-83 Done 2026-08-09).
- S123 thinned (PR #653): DRG-181/182 removed as build rows (merged #625/#627), DRG-251 and badges dropped, DRG-244 promoted, interim DRG-208 walk added as S123-10.

## Open gates and human-only items

- DRG-208 owner walk: interim at S123 day 5, final at S127. Not agent-dispatchable.
- Linear cycles do not exist and Done issues are not archived; both are S122.0 owner actions the MCP cannot perform.
- No QA plans exist for S122–S127; `/qa-plan sprint` is required before S123 implementation.
- DRG-197 GitNexus reindex is the blocker for every Unity symbol edit from S123 on; fallback is the global CLI with `--repo` against the worktree.
- SYM-CIV-01 scope question (DRG-232) must be answered by S125 exit or S126 runs MIL-only.
- Fourteen docs PRs (#628–#642) all edit `docs/engineering/README.md` and must merge serially in stack order.
- Test floor is cited as 1232 / 1638 / 3062 / 3216 across documents; one source file is owed in S122.

Prior numbered-sprint history remains in the dated snapshots listed on the [stable alias](future-sprint-roadpmap.md).
