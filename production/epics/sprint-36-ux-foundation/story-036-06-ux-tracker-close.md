---
id: S36-06
status: Ready
type: Config
priority: must-have
graphite_branch: stack/s36/ux-foundation
estimate_days: 0.5
dependencies:
  - S36-01 accessibility sign-off
  - S36-02 interaction patterns polish
  - S36-03 difficulty curve validation
  - S36-04 art-bible review
  - S36-05 c2 frame notes
owner: team-ui
sprint: 36
req_trace: polish-scope-boundary UX close; Game-Requirements/implementation-tracker-2026-06-04.md ; Req 20 partial polish
governing_adrs: ADR-010
---

# Story 036-06 — UX Foundation Tracker Close + GDD Cross-ref Polish

> **Epic:** sprint-36-ux-foundation

## Summary

Close UX foundation items in tracker / gate notes. Polish cross-references across design/gdd/command-and-control-ui.md , game-concept.md , ux/ specs, and production docs. Ensure S36 stories are reflected in epics/index and related agentic logs. Doc + index updates only.

## Acceptance Criteria

- [ ] `Game-Requirements/implementation-tracker-2026-06-04.md` (or relevant UX rows) annotated for S36 UX foundation closure (no MVP-complete claims)
- [ ] Cross-refs in `design/gdd/command-and-control-ui.md` and `design/gdd/game-concept.md` updated for accessibility / patterns / art-bible / difficulty
- [ ] `production/epics/index.md` updated for sprint-36-ux-foundation (Stories column)
- [ ] Polish scope references consistent; no scope creep introduced
- [ ] Lean review complete on final doc set

## QA Test Cases

```
Manual check: Tracker + index hygiene
  Setup: Read implementation-tracker + production/epics/index.md + this EPIC + S36 stories
  Verify: UX foundation items (accessibility, patterns, curve, art-bible, C2 frame) marked appropriately; epic listed with 6 stories
  Pass condition: No dangling "missing" notes for S35 residuals; links in GDDs resolve

Manual check: Cross-ref sweep
  Setup: Grep for key design files in gdd/ + ux/
  Verify: Mutual references between command-and-control-ui, game-concept, accessibility, art-bible, interaction-patterns, difficulty-curve
  Pass condition: Consistent Polish Phase 1 scope language
```

## Test Evidence Path

- `Game-Requirements/implementation-tracker-2026-06-04.md`
- `production/epics/index.md`
- `design/gdd/command-and-control-ui.md`
- `design/gdd/game-concept.md`
- This EPIC.md and all S36-0x story files

## Out of Scope

- Closing any tracker row to full MVP
- New GDD content beyond polish cross-refs
- Code or data changes
