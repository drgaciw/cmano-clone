---
id: S36-01
status: Ready
type: Config
priority: must-have
graphite_branch: stack/s36/ux-foundation
estimate_days: 0.5
dependencies:
  - S35-03 UX foundation trio merged
  - S35-07 C2 sign-off refresh
owner: team-ui
sprint: 36
req_trace: Gate r2 residual close; polish-scope UX foundations; design/accessibility-requirements.md (committed S35); references design/gdd/command-and-control-ui.md (C2 presentation accessibility per Polish Phase 1)
governing_adrs: ADR-010 (headless presentation UI); ADR-007
---

# Story 036-01 — Accessibility Requirements Sign-off

> **Epic:** sprint-36-ux-foundation

## Summary

Complete full sign-off on `design/accessibility-requirements.md` (Standard tier). Verify WCAG AA contrast, scaling, reduced motion, colorblind cues, staging diff prefixes are documented and cross-linked. Doc-only polish per lean review and Polish Phase 1 boundary. No implementation changes.

## Acceptance Criteria

- [ ] `design/accessibility-requirements.md` reviewed end-to-end; all sections 1–8 present and internally consistent
- [ ] Cross-links to `design/art/art-bible.md`, `design/ux/interaction-patterns.md`, `design/ux/c2-command-post.md` validated (no broken anchors)
- [ ] Evidence: sign-off recorded in story or linked `production/qa/accessibility-signoff-s36-*.md` (lean evidence)
- [ ] C2 surfaces (drawer, OOB, topbar, staging diff) + Platform Editor hosts listed as Standard tier
- [ ] No code changes; only doc updates if gaps found during review
- [ ] Lean review: CONCERNS resolved or APPROVED

## QA Test Cases

```
Manual check: Accessibility doc completeness and sign-off
  Setup: Read design/accessibility-requirements.md + linked GDDs
  Verify: Tier table, contrast tables (text, semantic, diff), scaling rules, motion stubs, focus 3:1, non-color cues present; cross-refs resolve
  Pass condition: Gate residual "accessibility tier verified" fully closed; Standard tier confirmed for Polish Phase 1 surfaces

Manual check: Crosslink integrity
  Setup: Open all referenced design/*.md
  Verify: No dangling references to accessibility section numbers
  Pass condition: All links functional in rendered view
```

## Test Evidence Path

- `design/accessibility-requirements.md`
- `design/art/art-bible.md`
- `design/ux/interaction-patterns.md`
- `production/qa/c2-manual-signoff-2026-06-02.md` (C2 surface coverage)
- (optional) `production/qa/accessibility-signoff-s36-*.md`

## Out of Scope

- Full WCAG AAA or screen-reader certification (post-Baltic)
- Input remapping implementation (stubs only in doc)
- Delegation / globe surfaces
- Any Unity USS or C# edits
