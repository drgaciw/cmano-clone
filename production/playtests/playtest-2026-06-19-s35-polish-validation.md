# Playtest Report — S35 Polish Validation (Session 7)

## Session Info
- **Date**: 2026-06-19
- **Build**: `main` @ `8de98b150da515b205358106852eb75376ddba5f`
- **Duration**: ~25 min equivalent (S35-06 presentation proxy + UX foundation cross-audit)
- **Tester**: QA lean proxy synthesis (headless Linux; no live Editor)
- **Platform**: Linux headless + Unity adapter tests (S35-07 C2 sign-off refresh)
- **Input Method**: N/A (automated UXML/spec proxy + behavioral harness)
- **Session Type**: Post-UX-foundation validation — S35-06 tooltip/onboarding wave across NPE, mid-game, and difficulty axes
- **Story**: S35-11 (`production/epics/sprint-35-polish-foundation/story-035-11-playtest-session-7.md`)
- **Design anchors**: `design/ux/onboarding-baltic.md`, `design/ux/interaction-patterns.md`, `design/difficulty-curve.md`, `design/accessibility-requirements.md`

## Test Focus
Whether Sprint 35 polish **closes presentation gaps** identified in playtest sessions 1–6:
1. **NPE** — Baltic onboarding copy (mission goal, first-actions table, message-log hint).
2. **Mid-game** — Platform catalog datalink lag helper (`LatencyMsNominal` → share-lag ticks) for curator mental model.
3. **Difficulty** — COMMS degrade legend (text + color, not color-only) and difficulty-band vocabulary alignment.

**Delta vs sessions 1–6:** Sessions 1–3 flagged missing onboarding spec, COMMS legend, and difficulty-curve doc. S35-03 authored UX foundation; S35-06 implemented presentation copy. Session 7 validates that chain — not a repeat of baseline C2/catalog harness passes.

## First Impressions (First 5 minutes)
- **Understood the goal?** **Yes (proxy)** — `MessageLogPanel.uxml` mission stub + `onboarding-baltic.md` §Mission Goal give explicit patrol/classify/Begin Execution path; closes session 1 partial-inference gap.
- **Understood the controls?** **Partially** — selection/classify/comms behavior unchanged (85/85 checks 1–13); new copy is presentation-only.
- **Emotional response**: **Engaged (inferred)** — analytical players gain mission framing without full tutorial system; **Residual confusion (low)** — FUEL line and readiness recovery still un-telegraphed.
- **Notes**: `C2CommsOnboardingTests` **4/4 PASS**; S35-07 C2 sign-off **18/18 PASS WITH NOTES** @ `8de98b1`.

## Gameplay Flow

### What worked well
- **NPE mission hint** — `MISSION: Patrol Baltic — classify hostile ◆ contacts, then Begin Execution.` in message log header (`message-log-onboarding-hint`).
- **Onboarding spec stub** — `design/ux/onboarding-baltic.md` ties mission goal, first-actions table, COMMS primer, and catalog lag note to Player Fantasy.
- **COMMS inline legend** — top bar declares `NOMINAL = full picture`, `DEGRADED = stale contacts, reduced symbol opacity`, `DENIED = no new engagements, map dimmed` with semantic USS classes (not color-only per `accessibility-requirements.md`).
- **Catalog lag helper** — `platform-catalog-comms-lag-helper` explains `LatencyMsNominal (catalog) drives share-lag ticks in sim (S34-07)`; aligns with `interaction-patterns.md` §8 sim-impact table.
- **Difficulty vocabulary** — `design/difficulty-curve.md` Band A–C mapping now authoritative; session 7 can reference bands without facilitator improvisation.
- **Behavioral regression** — checks 9–11 comms degrade/deny path unchanged (`BalticReplayHarnessCommsTests`, `MapPanelBinderTests`).

### Pain points
- **No full guided tutorial** — Severity: Low (explicitly out of S35-06 scope); Medium for non-sim NPE audience.
- **In-fight lag attribution overlay** — Severity: Medium — catalog helper helps curators in editor; player during `baltic-patrol-datalink-catalog-latency` still lacks HUD overlay (session 3 gap partially closed).
- **Headless-only legibility** — Severity: Low for merge gate; Medium for true visual density of inline legend on 1080p.

### Confusion points
- FUEL line still non-obvious for NPE (carried from session 1; not in S35-06 scope).
- Readiness recovery (`AIR_NOT_READY`) still log-only — `difficulty-curve.md` Loop 4 P1 backlog open.
- Three catalog sections (damage, comms, link) still potentially overwhelming for curators despite helper text.

### Moments of delight
- Session 1–3 open items (onboarding spec, COMMS legend, difficulty doc) now have **traceable design + UXML evidence** — playtest corpus gains closure signal beyond harness PASS counts.

## Axis Coverage

| Axis | Session 1–6 baseline | S35 polish delta (session 7) | Verdict |
|------|----------------------|--------------------------------|---------|
| **NPE** | Goal inferred from fixtures; no briefing copy | Message-log hint + `onboarding-baltic.md` first-actions | **Improved** — presentation gap closed; tutorial system deferred |
| **Mid-game** | Catalog round-trip proven; lag cause-effect facilitator-only | Platform catalog lag helper under COMMS section | **Improved** — curator path clearer; in-fight overlay still open |
| **Difficulty** | Operator predicted deny; legend/tooltip top request | Inline COMMS legend + `difficulty-curve.md` bands/loops | **Improved** — P0 legend delivered; P1 denial one-liner + lag overlay open |

## Bugs Encountered

| # | Description | Severity | Reproducible |
|---|-------------|----------|--------------|
| — | None filed | — | — |

## Feature-Specific Feedback

### NPE onboarding copy (S35-06)
- **Understood purpose?** Yes (proxy — UXML + spec cross-check)
- **Found engaging?** Unknown — needs facilitated think-aloud on copy readability
- **Suggestions**: Optional dismissible coach marks in future Polish; keep message-log hint visible through first Begin Execution.

### COMMS degrade legend (S35-06)
- **Understood purpose?** Yes (proxy — text labels satisfy accessibility non-color-only rule)
- **Found engaging?** Unknown — legend row density on narrow viewports unvalidated in Editor
- **Suggestions**: Cross-link legend to `interaction-patterns.md` P-C2-05 map opacity table in facilitator script.

### Datalink lag helper (S35-06 / S34-07)
- **Understood purpose?** Partially — helper text in catalog; not on C2 map during lag fixture
- **Found engaging?** Unknown for in-fight players; Yes (inferred) for catalog curators
- **Suggestions**: P1 lag-source attribution overlay per `difficulty-curve.md` §6 backlog.

## Quantitative Data

| Gate | Result |
|------|--------|
| `C2CommsOnboarding` (S35-06 presentation) | **4/4 PASS** |
| Checks 1–13 filter | **85/85 PASS** |
| Checks 14–18 filter | **58/58 PASS** |
| `ReplayGoldenSuiteTests` | **6/6 PASS** |
| Production Baltic hash | `17144800277401907079` (unchanged) |
| `DelegationBridge.cs` diff | **ZERO touch** |

## Overall Assessment
- **Would play again?** **Yes (proxy)** — S35 polish reduces first-session friction for target analytical audience
- **Difficulty**: **Just Right** for Band A classify path; Band C isolates unchanged (engineering-strong, communication-improved)
- **Pacing**: **Good** — copy additions do not alter RTwP tick model
- **Session length preference**: **Good** — onboarding stub supports 10–15 min NPE path per `difficulty-curve.md`

## Top 3 Priorities from this session
1. Run **facilitated think-aloud** on S35 polish items referencing `onboarding-baltic.md` + `interaction-patterns.md` (companion human session).
2. **P1 backlog**: HUD order-denial one-liner and in-fight lag-source overlay (post-S35-06 residual gaps).
3. Optional **live Editor re-capture** for comms legend legibility at 1080p (non-blocking per S35-07 advisory).

## Findings Triage

| Finding | Severity | Route | Blocking? |
|---------|----------|-------|-----------|
| S35-06 closes P0 COMMS legend + NPE mission hint | — | **Closed** | No |
| `onboarding-baltic.md` + `difficulty-curve.md` close design-doc gaps | — | **Closed** | No |
| Catalog lag helper improves curator mental model | Low | **Closed (partial)** | No |
| FUEL line non-obvious for NPE | Medium | Design — future onboarding pass | No |
| In-fight lag attribution overlay absent | Medium | Polish P1 — `difficulty-curve.md` §6 | No |
| Readiness recovery copy absent | Medium | Design — quick-design strings | No |
| Delegation badges / trust signals thin | Medium | **S36+** per polish-scope-boundary | No |
| Live Editor click-feel unobserved | Low | Advisory re-capture | No |

## Action Routing
- **Design changes:** P1 denial one-liner + readiness recovery copy; optional coach marks
- **Balance:** N/A — presentation-only delta
- **Bugs:** None
- **Polish:** Editor PNG re-capture for legend row density; lag overlay (S36+ candidate)

**Session mode:** Lean proxy synthesis post-S35-06 — **adds signal beyond sessions 1–6** on polish presentation closure; companion human session recommended for copy legibility.