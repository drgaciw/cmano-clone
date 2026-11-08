---
id: S36-04
status: Ready
type: Config
priority: must-have
graphite_branch: stack/s36/ux-foundation
estimate_days: 1
dependencies:
  - S35-03 UX foundation trio
  - S36-01 accessibility sign-off
owner: team-ui
sprint: 36
req_trace: Gate art-bible lean sign-off; polish-scope-boundary UX; design/art/art-bible.md
governing_adrs: ADR-007; ADR-010; ADR-011
---

# Story 036-04 — Art-Bible Lean Review and Sign-off

> **Epic:** sprint-36-ux-foundation

## Summary

Advance `design/art/art-bible.md` from Draft to lean sign-off (AD-ART-BIBLE) for C2 + Platform Editor surfaces only. Review visual identity, mood, color tokens (incl. colorblind), USS tokens, layout hierarchy, NATO cues. Doc-only; defer full asset production.

## Acceptance Criteria

- [ ] Art bible sections 1– (visual identity, mood, palette, layout, typography, tokens, semantic diff) reviewed for Phase 1 completeness
- [ ] Colorblind safety, planning vs executing states, evidence-grade clarity rules validated against accessibility-requirements
- [ ] Cross-links to game-concept, c2-command-post, interaction-patterns, accessibility
- [ ] Lean sign-off recorded (APPROVED or CONCERNS with acceptance); status updated
- [ ] No asset creation or code; references only to `unity/ProjectAegis/Assets/UI/`

## QA Test Cases

```
Manual check: Art bible review for Polish Phase 1
  Setup: Read design/art/art-bible.md + art/color references + linked UX/GDDs
  Verify: One-line rule, principles, mood tables (planning/executing), palette with WCAG notes, token usage for C2/PE, layout hierarchy, evidence clarity present and consistent
  Pass condition: Lean AD-ART-BIBLE sign-off achieved or explicit "APPROVED WITH NOTES" for Baltic slice

Manual check: Token + accessibility alignment
  Setup: Compare art-bible palette against accessibility contrast tables
  Verify: Hex values and ratios match; colorblind notes present
  Pass condition: No contradictions
```

## Test Evidence Path

- `design/art/art-bible.md`
- `design/accessibility-requirements.md`
- `design/ux/interaction-patterns.md`
- `unity/ProjectAegis/Assets/UI/` (reference only, no changes)

## Out of Scope

- Full art production pipeline, VFX, world/character assets
- Cesium visual language
- Post-Baltic AAA polish
- Any binary asset edits
