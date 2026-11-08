---
id: S35-06
status: Complete
type: UI
priority: must-have
graphite_branch: stack/sprint35/c2-polish
estimate_days: 2
dependencies:
  - S35-04 profiler baseline merged
owner: team-unity
sprint: 35
completed: 2026-06-19
req_trace: Req 20 TR-c2-001; fun-hypothesis onboarding/comms gaps
governing_adrs: ADR-007; design/ux/c2-command-post.md; design/art/art-bible.md
---

# Story 035-06 — C2 Onboarding and Comms Tooltips

> **Epic:** sprint-35-c2-platform-polish

## Summary

Close fun-hypothesis UX gaps: minimal Baltic NPE onboarding copy, COMMS degrade/denied legend, catalog-derived datalink lag helper text. **Presentation-only** — no delegation badges, trust UX, or sim behavior.

## Acceptance Criteria

- [x] Comms DEGRADED/DENIED states have tooltip or inline legend (not color-only)
- [x] Datalink lag helper links `LatencyMsNominal` → share-lag behavior (S34-07 vocabulary)
- [x] Minimal mission goal + first-action hint on C2 entry (UXML copy or `design/ux/onboarding-baltic.md` stub)
- [x] C2 checks 1–13 headless proxy **≥61/61** PASS
- [x] `grep DelegationBadge|TrustSignal|HYPERSONIC_ALERT` in `unity/ProjectAegis/Assets/` — zero **new** refs
- [x] ZERO touch `DelegationBridge.cs`
- [x] `ReplayGoldenSuiteTests` — **6/6** PASS

## QA Test Cases

```
Manual check: Comms degrade legibility
  Setup: Baltic comms scenario; C2 sensor strip
  Verify: Player can identify DEGRADED vs DENIED without prior tribal knowledge
  Pass condition: Playtest gap from human think-aloud closed or documented PASS WITH NOTES

Test: Headless C2 checks 1-13 regression
  Given: S35-06 USS/UXML changes
  When: dotnet test --filter "PlayModeSmoke|C2Selection|OobTree|LossesScoring|BalticReplay|FuelState|AttackMenu"
  Then: ≥61/61 PASS
```

## Test Evidence Path

- `unity/ProjectAegis/Assets/UI/` (C2 UXML/USS)
- `design/ux/onboarding-baltic.md` (optional stub)
- `production/playtests/fun-hypothesis-validation-2026-06-19.md` (revision note)

## Out of Scope

- Delegation badges + trust signals (S36+ handoff item 7)
- Full tutorial system
- Platform Editor Phase C–H copy (unless shared token only)