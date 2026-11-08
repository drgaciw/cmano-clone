---
id: S36-03
status: Ready
type: Config
priority: must-have
graphite_branch: stack/s36/ux-foundation
estimate_days: 0.75
dependencies:
  - S35-03 UX foundation trio
  - S35-11 playtest session 7
owner: team-qa
sprint: 36
req_trace: polish-scope difficulty-curve; fun validation; design/difficulty-curve.md full sign-off
governing_adrs: ADR-010 (presentation of player-facing intent only)
---

# Story 036-03 — Difficulty Curve Validation + Playtest Tie-in

> **Epic:** sprint-36-ux-foundation

## Summary

Validate and close `design/difficulty-curve.md` (Band A/B/C for Baltic). Tie to existing playtest corpus (fun-hypothesis, human sessions). Confirm bands map correctly to NPE/standard/advanced fixtures without changing production pins. Doc sign-off only.

## Acceptance Criteria

- [ ] Band model, scenario mapping, and difficulty signals reviewed against `baltic-patrol*` fixtures and Platform Editor workflow
- [ ] Playtest notes (`production/playtests/`) referenced; gaps (e.g. NPE tutorial, FUEL, COMMS legend) noted as P1 doc items only
- [ ] Cross-links to `design/gdd/game-concept.md`, `production/polish-scope-boundary-2026-06-19.md`, interaction-patterns
- [ ] Evidence: validation record or sign-off note in doc
- [ ] No change to production hash or scenario content; no new fixtures

## QA Test Cases

```
Manual check: Difficulty band completeness
  Setup: Read design/difficulty-curve.md + playtest docs + polish-scope-boundary
  Verify: Band A (intro: baltic-patrol-classify, baltic-patrol), Band B (standard + editor), Band C (advanced comms isolates) all documented with signals and ratings
  Pass condition: "full sign-off" on difficulty-curve achieved; mappings traceable to ReplayGolden and human sessions

Manual check: Playtest tie-in
  Setup: Cross-reference fun-hypothesis-validation-2026-06-19.md and human/ thinkalouds
  Verify: Known gaps called out without promising implementation in S36
  Pass condition: Doc marked validated or "sign-off with notes"
```

## Test Evidence Path

- `design/difficulty-curve.md`
- `production/playtests/fun-hypothesis-validation-2026-06-19.md`
- `production/playtests/human/`
- `data/scenarios/baltic-patrol*.policy.json` (read-only reference)

## Out of Scope

- Balance retuning of sim formulas
- New scenario families or Band D
- In-game numeric difficulty labels (P1 future)
- Delegation UX difficulty
