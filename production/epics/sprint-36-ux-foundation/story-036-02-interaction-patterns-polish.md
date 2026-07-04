---
id: S36-02
status: Ready
type: UI
priority: must-have
graphite_branch: stack/s36/ux-foundation
estimate_days: 1
dependencies:
  - S35-03 UX foundation trio
  - S36-01 accessibility sign-off
owner: team-ui
sprint: 36
req_trace: polish-scope UX; interaction-patterns.md polish; design/ux/c2-command-post.md + Platform Editor patterns
governing_adrs: ADR-010; ADR-007 (C2 map); ADR-011
---

# Story 036-02 — Interaction Patterns Polish + Crosslinks

> **Epic:** sprint-36-ux-foundation

## Summary

Polish `design/ux/interaction-patterns.md` for completeness on implemented C2 + Platform Editor screens. Add any missing cross-references to accessibility, art-bible, C2 GDD, difficulty curve. Ensure no "designed in-code only" surfaces remain for Phase 1 paths. Doc-only; lean review.

## Acceptance Criteria

- [ ] Pattern index (P-C2-01..P-PE-03) reviewed; gaps filled for C2 top bar, planning chrome, message log, staging workflow
- [ ] Cross-links added/verified to `design/accessibility-requirements.md`, `design/art/art-bible.md`, `design/gdd/command-and-control-ui.md`, `design/difficulty-curve.md`
- [ ] Evidence: updated patterns doc + lean review record
- [ ] Patterns reference actual hosts (`C2LeftDrawerPanelHost`, `PlatformImportPanelHost`, etc.) and sign-off anchors (checks 1–18, Platform tests)
- [ ] No new patterns invented for out-of-scope (Cesium, delegation badges)

## QA Test Cases

```
Manual check: Interaction patterns cover implemented screens
  Setup: Read design/ux/interaction-patterns.md + referenced UX/GDD files
  Verify: Every Polish Phase 1 C2/Platform surface (drawer tabs, selection sync, COMMS, import staging, diff feedback, validation errors) has explicit pattern entry with behavior + data binding table
  Pass condition: No implemented screen lacks a pattern reference (lean: sample of checks 5–6, 14, 17)

Manual check: Cross-ref audit
  Setup: Grep links + open files
  Verify: accessibility, art-bible, c2-command-post, game-concept, difficulty-curve referenced from patterns
  Pass condition: Links resolve; context sections updated where needed
```

## Test Evidence Path

- `design/ux/interaction-patterns.md`
- `design/accessibility-requirements.md`
- `design/art/art-bible.md`
- `design/gdd/command-and-control-ui.md`
- `design/ux/c2-command-post.md`

## Out of Scope

- New interaction behaviors or code
- Patterns for deferred features (globe Phase B, badges)
- Full usability study (playtest tie-in only)
