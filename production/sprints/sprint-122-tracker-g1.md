# Sprint 122 — Tracker honesty + Slice G1 baseline

**Status:** Planned (rev 2, 2026-09-15 — amended per backlog review; see Change log)  
**Dates:** 2026-09-15 – 2026-09-19  
**Predecessor:** [S122.0 cap hygiene](sprint-122-0-cap-hygiene.md)  
**Next:** [S123 play-entry](sprint-123-play-entry.md)  
**Linear:** [DRG-234](https://linear.app/drgamtd-workspace/issue/DRG-234) G1 · [DRG-242](https://linear.app/drgamtd-workspace/issue/DRG-242) · [DRG-241](https://linear.app/drgamtd-workspace/issue/DRG-241) · [DRG-188](https://linear.app/drgamtd-workspace/issue/DRG-188) · [DRG-197](https://linear.app/drgamtd-workspace/issue/DRG-197)  
**Epic:** [DRG-233](https://linear.app/drgamtd-workspace/issue/DRG-233) Slice G  
**Stage:** **Release** · **Not Launch**  
**Doctrine:** ADR-010 / 007 · no DelegationBridge hotpath · no CatalogWriteGate · no golden edits  
**Blocker for S123:** DRG-197 must produce a usable current-graph impact report (MCP, or the global CLI with `--repo <worktree>` recorded in this sprint's closeout) before any S123 Unity symbol edit. Linear carries `blocks` relations from DRG-197 to DRG-243/246/235/236.

## Sprint Goal

Linear tells the truth about Slice A/B/C. G1 evidence matrix exists so later sprints validate instead of rediscovering. The hub pin tests are re-pinned once so S122/S125 requirement amendments stop tripping them.

## Capacity

- Total days: 5 · Buffer 20% · **Available Must: 4.0d** · Should ≤2.0d  
- Must load: **4.0d** (rev 2: S122-01/02/05 done or removed on 09-15; review rows added)

## Already done (2026-09-15, before sprint start)

| Was | Result |
|-----|--------|
| S122-01 DRG-240 Slice B → In Review | **Done.** DRG-165…170 In Review, DRG-184 In Progress, evidence on DRG-240 |
| S122-02 DRG-237 Slice C → In Review | **Canceled (overtaken).** DRG-171…174/191/194 already Done; DRG-185 In Progress |
| S122-05 DRG-180 provenance UI | **Done** via #626. DRG-181 (#625) and DRG-182 (#627) also merged; they are not S123 build rows |
| Reconciliation | Notion Hub / S121 plan / Delivery archive / Combat UX callout and DRG-208 body now name S122.0–S127; DRG-208 has `blocked by` DRG-243/239/244/183 |

## Must Have

| ID | Task | Agent/Owner | Est.d | Deps | Acceptance Criteria |
|----|------|-------------|------:|------|---------------------|
| S122-03 | DRG-241 Surface / single-owner labels | tracker | 0.25 | — | Labels exist; applied to Unity-touching opens |
| S122-04 | DRG-242 + DRG-234 G1 checklist + baseline doc | docs | 1.5 | 241 helpful | `docs/superpowers/reviews/2026-09-*-slice-g-baseline.md`; evidence classes per row; linked from DRG-234 |
| S122-09 | DRG-197 GitNexus reindex (MCP Ladybug v42 vs engine v40) | tooling | 0.75 | before S123 | Fresh `impact` on `CommandReviewView`, `CombatMapView`, `ScenarioEditorShellHost` returns current consumers. If MCP still fails: CLI `--repo` fallback works and the path used is written into the closeout |
| S122-11 | **HUB-PIN** re-pin hub tests + FR-21 index row | docs | 0.5 | with first G1 amendment | `Wave4RtmIndexHonestyPinsTests` and `RequirementsHubContractTests` updated in the same PR as the first req 11/20/21 amendment; doc 01 Related Index gets the FR-21 row so draft 24 can move into `requirements/`. Done once, not per sprint |
| S122-12 | **FLOOR** single-source test floor | devops | 0.25 | — | One file (e.g. `production/test-floor.json`) read by `tools/verify-ci-local.ps1`, `tools/buildkite/dotnet-ci.sh` and cited by AGENTS.md; the 1232 / 1638 / 3062 / 3216 generations collapse to one measured value |
| S122-13 | **REVIEW** DRG-181/182 post-merge review carry-over | unity reviewer | 0.5 | #625/#627 merged | pr-finish checklist verdicts recorded on DRG-181/182; any defect filed to its A-slice owner, not rebuilt in S123 |
| S122-08 | DRG-188 Notion/Linear/GitHub/Graphite wiring | tracker | 0.25 | parallel | GitHub → Linear state automation confirmed (PR open → In Review, merge → Done) on one live PR |

## Should Have

| ID | Task | Est.d | Deps |
|----|------|------:|------|
| S122-06 | DRG-275 / N-FRESH-01′ wiki honesty stamp (include: Notion research pages older than the Linear waterline) | 0.5 | 234 |
| S122-07 | N-GATE-PLAY-01 Play Mode wiki gate pack | 0.25 | 275 |
| S122-14 | Design-page 0/8 decision: either fill one Notion design page per sprint theme (play entry, combat commit, editor, symbology) at 0.25d each, or drop the eight-section claim from the wiki template | 0.25 (decision) | owner |

## Nice to Have

| ID | Task | Est.d |
|----|------|------:|
| S122-10 | DRG-238 waterline closeout note | 0.25 |

## Out of sprint

MODE/DEL implementation (file defects to DRG-246/252). Treating DRG-165–175 as greenfield after In Review. Rebuilding DRG-180/181/182 chrome.

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| Agents re-implement Slice B/C chrome | M | H | In Review states + Surface labels |
| G1 expands into MODE/DEL | M | H | G1 is matrix only |
| DRG-197 still failing on 09-19 | M | H | CLI fallback is an accepted exit; record the path; S123 does not start Unity edits without it |
| Hub pins trip on the G1 amendment PR | H | M | S122-11 lands in the same PR |

## Definition of Done

- [ ] All Must Have complete
- [ ] G1 markdown linked from DRG-234
- [ ] DRG-197 impact report (MCP or CLI) attached to the issue
- [ ] Hub pin tests green after the first requirement amendment
- [ ] Test-floor file exists and CI scripts read it
- [ ] No owner-accepted without human
- [ ] No DelegationBridge / CatalogWriteGate / golden edits
- [ ] QA plan exists (from S122.0-07)

## Change log

- **2026-09-15 rev 2** (backlog review): S122-01/02/05 moved to "Already done"; DRG-197 promoted to Must with an explicit S123 blocker and CLI fallback; HUB-PIN, FLOOR, REVIEW and design-page decision rows added; DRG-188 AC tightened to a live automation check. Must load 3.75d → 4.0d.
- **2026-09-13 rev 1**: wave3 architect plan §S122.
