# Playtest Report — New Player Experience (Baltic C2)

## Session Info
- **Date**: 2026-06-19
- **Build**: `main` @ `d3db76dbca07237200f5e7ad69eb5d4fdcaa118f`
- **Duration**: ~15 min equivalent (headless proxy batches + checklist audit)
- **Tester**: QA lean proxy synthesis (headless Linux; no live Editor)
- **Platform**: Linux headless + Unity batchmode proxy (S19/S25/S34 evidence chain)
- **Input Method**: N/A (automated PlayMode + dotnet harness)
- **Session Type**: First-time theater-commander path (Baltic patrol classify + comms preconditions)

## Test Focus
Whether a new player can understand the **theater commander fantasy** and core C2 loop within the first interaction cycle: load scenario → read contacts/OOB → select unit → see detail panels → (planning) begin execution.

## First Impressions (First 5 minutes)
- **Understood the goal?** **Partially** — scenario policy and OOB/contact sync are test-proven; no human readability study of tutorial/onboarding copy.
- **Understood the controls?** **Partially** — selection/classify/comms flows pass headless proxy (`Baltic_classify_selection_flow_syncs_map_oob_and_contact_summary`, comms batch PASS).
- **Emotional response**: **Engaged (inferred)** — deterministic replay + rich contact FSM suggests depth; **Confused (risk)** — no guided onboarding UI spec beyond C2 drafts.
- **Notes**: S19 PlayMode batch (`Invoke-C2PlayModeSignoffBatch.ps1` comms + classify) PASS @ `7401fac`; S25 tri-batch proxy PASS @ `bd225ae`. Player fantasy from `design/gdd/game-concept.md` is design-complete but not playtest-validated with live humans.

## Gameplay Flow

### What worked well
- Baltic classify scenario: hostile symbols, OOB tree, map placeholder, and contact summary stay synchronized (headless).
- COMMS degrade scenario: degraded/denied comms preconditions match manual QA expectations (S25-11).
- C2 checklist 1–13 remain PASS via sustained headless proxy (61/61 @ S31 baseline, re-confirmed S34).

### Pain points
- **No first-run tutorial or mission briefing UI** — Severity: Medium (onboarding gap for NPE).
- **Headless-only validation** — Severity: Low for merge gate; Medium for true NPE confidence.

### Confusion points
- Without live Editor, information density of C2 panels (tabs, doctrine, attack menu) is unvalidated for cognitive load.
- "Begin Execution" transition is test-proven (`C2TopBar` 5/5) but not observed with a naive player.

### Moments of delight
- Deterministic replay golden suite (6/6) supports "evidence-based command" pillar — strong for analytical players.

## Bugs Encountered

| # | Description | Severity | Reproducible |
|---|-------------|----------|--------------|
| — | None filed this session | — | — |

## Feature-Specific Feedback

### C2 Selection / Contacts (classify)
- **Understood purpose?** Partially (proxy only)
- **Found engaging?** Unknown — needs human session
- **Suggestions**: Add NPE smoke script: 10-minute guided Baltic classify walkthrough with explicit success criteria.

### COMMS degrade
- **Understood purpose?** Partially
- **Found engaging?** Unknown
- **Suggestions**: Document comms state legend in HUD UX spec.

## Quantitative Data (if available)
- **Headless proxy tests (checks 1–13 path)**: 61/61 PASS (historical baseline)
- **PlayMode batch scenarios**: comms PASS, classify PASS (S19); + doctrine PASS (S25 proxy)

## Overall Assessment
- **Would play again?** **Maybe** (proxy — sim depth suggests yes for target audience)
- **Difficulty**: **Just Right** for harness fixtures; **Unknown** for human NPE
- **Pacing**: **Good** in RTwP planning model (design intent)
- **Session length preference**: **Good** for analytical sessions

## Top 3 Priorities from this session
1. Run one **live human NPE session** on Baltic classify with think-aloud notes (15 min).
2. Author minimal **onboarding UX spec** (`design/ux/onboarding-baltic.md`) tied to Player Fantasy.
3. Keep headless tri-batch as regression gate; do not treat it as substitute for NPE sign-off.

## Action Routing
- **Design changes:** Onboarding / tutorial flow — consider `/ux-design onboarding-baltic`
- **Balance:** N/A this session
- **Bugs:** None
- **Polish:** C2 panel density review when Editor available

**Session mode:** Lean proxy synthesis — satisfies gate structure; **human NPE session still recommended**.