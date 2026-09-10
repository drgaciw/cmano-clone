# Future Sprint Roadmap - Unity UX/UI Slice G

**Date:** 2026-09-08. **Stage:** Release. **Scope:** Bounded update for Slice G and its audit dependencies, not a full-project status reassessment.

This snapshot adds the owner-authorized Unity UX/UI slice to the [July 14 roadmap](future-sprint-roadmap-07142026.md). Prior numbered-sprint scope remains governed by that snapshot and later program-specific records. No new numbered sprint or commercial launch scope is assigned here. Release v1 / Spirit 1 stays closed.

## Delivery

| Work | Record | State at snapshot |
| --- | --- | --- |
| Unity UX/UI epic | [DRG-233](https://linear.app/drgamtd-workspace/issue/DRG-233) | Backlog; scope and acceptance created |
| G1 requirements/evidence baseline | [DRG-234](https://linear.app/drgamtd-workspace/issue/DRG-234) | Backlog; ready for baseline work |
| G2 integrated C2 acceptance | [DRG-235](https://linear.app/drgamtd-workspace/issue/DRG-235) | Backlog; depends on G1 |
| G3 Mission/Platform Editor acceptance | [DRG-236](https://linear.app/drgamtd-workspace/issue/DRG-236) | Backlog; depends on G1 |

[Repository plan](../superpowers/plans/2026-09-08-slice-g-unity-ux-ui.md) defines task criteria, evidence provenance and architecture boundaries. [Notion](https://app.notion.com/p/3d5f7cb4e4df812186c2db1a397b17d1) holds the scope/acceptance contract; Linear remains delivery authority.

## Dependency Order

1. Reconcile G1 and audit E traceability (DRG-187/188). Recover GitNexus under DRG-197 before any symbol edits requiring impact analysis.
2. Run G2 and G3 in parallel after G1, with isolated implementation worktrees and one local Editor coordinator.
3. Reuse existing A-C, accessibility and performance owners (DRG-170/176/177/205/26); route evidence-backed defects to them.
4. Feed G evidence into audit D amendments for requirements 11/20/21. Resolve pending requirement approvals explicitly.
5. Close the scoped UX/UI gate only after technical evidence and owner acceptance are separately recorded.

Audit D/E/F are finding categories in the September 2 audit, not confirmed delivery slice definitions. This corrects the earlier planning shorthand. G does not create replacement D/E/F epics.

## Baseline and Open Gates

- HEAD `37e6ba6d` plus existing uncommitted Slice C layout/metadata/evidence. Preserve that work.
- Fresh verification: build 0 warnings/errors, 3,216 solution tests, 24 proxy smoke and six canonical replay tests, all passing. SDK 8.0.424 is selected by the current roll-forward configuration.
- [Slice C evidence](../superpowers/reviews/2026-09-08-slice-c-playmode-acceptance.md) records technical Editor smoke PASS; owner acceptance remains pending. Positive live group/BDA flow and live authored coverage are not proven by that package.
- GitNexus refresh reported Ladybug WAL assertions and unavailable FTS; it was interrupted after approximately nine minutes without completion. DRG-197 remains the recovery owner; no fresh graph acceptance is claimed here.
- No new Unity visual or frame-time measurement was made for this planning update.

[Dashboard snapshot](dashboard-snapshots/2026-09-08-slice-g.md) records this same bounded update. The stable alias points here; historical roadmap content remains preserved in the dated snapshots.
