# Milestone Alias — "Spirit 1" → Baltic Vertical Slice MVP (Phase 1)

**Date:** 2026-06-20  
**Authority:** [spirit1-vertical-slice-gap-analysis-2026-06-05.md](../../Game-Requirements/reviews/spirit1-vertical-slice-gap-analysis-2026-06-05.md) §0, §5 item 5 (Recommendation R5)  
**Remediation log:** [spirit1-gap-remediation-log-2026-06-20.md](../../Game-Requirements/reviews/spirit1-gap-remediation-log-2026-06-20.md)

## Canonical mapping

| Informal / legacy name | Canonical milestone | Canonical doc |
|------------------------|---------------------|-----------------|
| **Spirit 1** | **Baltic-Style Vertical Slice (MVP)** | [vertical-slice-mvp.md](vertical-slice-mvp.md) |
| Spirit 1 / Phase 1 | **Vertical Slice MVP** / **Phase 1** | Same |
| "Spirit 1 gap analysis" | Spirit 1 **review alias** for the MVP gap report | [spirit1-vertical-slice-gap-analysis-2026-06-05.md](../../Game-Requirements/reviews/spirit1-vertical-slice-gap-analysis-2026-06-05.md) |

**Rule:** New planning artifacts (sprints, epics, gates, ADRs, production milestones) MUST use the **canonical name** in titles and `production/milestones/` paths. The string **"Spirit 1"** is retained only as a **backward-compatible alias** in review filenames and cross-links — not as a milestone ID in `production/sprint-status.yaml` or epic registry.

## Why this alias exists

The Spirit 1 gap analysis (2026-06-05) found **no milestone, epic, or sprint** registered under the name "Spirit 1" in this repository. "Spirit" appears elsewhere only as the B-2A Spirit platform entry or figurative prose. The review was scoped to the **Baltic Vertical Slice MVP** milestone; this document makes that mapping explicit so future agents and contributors do not search for a phantom milestone.

## Vocabulary to use

| Use | Do not use (for milestone scope) |
|-----|----------------------------------|
| Baltic Vertical Slice MVP | Spirit 1 milestone |
| Vertical Slice / Phase 1 | Spirit 1 epic |
| Sprint N (e.g. Sprint 11 gate) | Spirit 1 sprint |
| [vertical-slice-mvp.md](vertical-slice-mvp.md) | `production/milestones/spirit-1.md` (does not exist) |

## Related artifacts (still valid under alias)

These retain "spirit1" in filenames for link stability; content refers to the canonical MVP:

- [spirit1-vertical-slice-gap-analysis-2026-06-05.md](../../Game-Requirements/reviews/spirit1-vertical-slice-gap-analysis-2026-06-05.md) — primary gap report
- [spirit1-classify-lifecycle-vs-fsm-2026-06-20.md](../../Game-Requirements/reviews/spirit1-classify-lifecycle-vs-fsm-2026-06-20.md) — G2/R4 lifecycle labeling
- [spirit1-gap-remediation-log-2026-06-20.md](../../Game-Requirements/reviews/spirit1-gap-remediation-log-2026-06-20.md) — remediation tracker
- [adr-simulation-session-frozen-hub-spirit1-2026-06-20.md](../../docs/architecture/adr-simulation-session-frozen-hub-spirit1-2026-06-20.md) — R3 frozen hub policy

## Pointer — source of truth

All Spirit 1 / MVP scope, success criteria, feature lists, and gate status live in:

**[production/milestones/vertical-slice-mvp.md](vertical-slice-mvp.md)**

When in doubt, cite that file — not "Spirit 1" alone.
