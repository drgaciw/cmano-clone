# Human Think-Aloud Session — S35 Polish Validation

**Companion proxy report:** `production/playtests/playtest-2026-06-19-s35-polish-validation.md`  
**Date:** 2026-06-19  
**Build:** `main` @ `8de98b150da515b205358106852eb75376ddba5f`  
**Role:** Facilitated Human Review (qa-lead + operator)  
**Mode:** Lean — evidence walkthrough on S35-06 presentation delta + S35-03 UX foundation docs  
**Duration:** 20 min (scripted)  
**Automated gate cited:** `dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests --filter "C2CommsOnboarding" -v minimal` → **4/4 PASS**; checks 1–13 **85/85**, checks 14–18 **58/58** @ 2026-06-19

---

## Facilitator Script

> **Setup (before timer):** Open companion proxy report, `design/ux/onboarding-baltic.md`, `design/ux/interaction-patterns.md` (P-C2-05, §8 staging diff / sim impact), `design/difficulty-curve.md` (Bands A–C, Loops 2–3), and UXML evidence paths from `C2CommsOnboardingTests`. Operator has context from sessions 1–3 think-aloud gaps (missing legend, onboarding copy, difficulty doc). Remind operator: *continuous think-aloud* — evaluate whether S35 polish would have changed your session 1–3 answers.

| Time | Step | Facilitator prompt | Design / evidence anchor |
|------|------|------------------|-------------------------|
| 0:00–2:00 | **NPE cold open** | "Read `onboarding-baltic.md` §Mission Goal aloud. Would this have helped in session 1 cold open?" | `onboarding-baltic.md` §Mission Goal |
| 2:00–4:00 | **Message-log hint** | "Inspect `MessageLogPanel.uxml` mission stub. Say what you'd do in the first 5 minutes." | `message-log-onboarding-hint`; §First Actions table |
| 4:00–7:00 | **COMMS legend** | "Top bar legend row: NOMINAL / DEGRADED / DENIED. Does this fix session 1 check 10 confusion?" | `C2TopBarPanel.uxml`; P-C2-05 in `interaction-patterns.md` |
| 7:00–10:00 | **COMMS degrade walk** | "COMMS DEGRADED → DENIED — map opacity + ghost duplicates. Match legend text to what you expect on map." | `difficulty-curve.md` Loop 2; checks 9–11 |
| 10:00–13:00 | **Mid-game catalog helper** | "Open platform catalog COMMS section helper. Connect `LatencyMsNominal` to sim lag without facilitator." | `PlatformCatalogPanel.uxml`; `interaction-patterns.md` §8 sim-impact |
| 13:00–16:00 | **Difficulty bands** | "Using `difficulty-curve.md`, rank classify / comms / catalog-latency fixtures. Easier than session 3?" | Bands A–C mapping |
| 16:00–18:00 | **Residual gaps** | "What's still missing after S35 polish? FUEL, readiness, delegation badges?" | Session 1–3 findings routing |
| 18:00–20:00 | **Polish wrap** | "Would S35 copy change your session 1 verdict? Play another 15 minutes?" | Overall S35 assessment |

**Think-aloud stems (reuse anytime):**
- "Say what you're reading in the onboarding spec right now."
- "Does the legend text match what you expected from color alone?"
- "Where would you look for lag cause — catalog, map, or log?"
- "Rate cognitive load: lower / same / higher vs session 1 — why?"

---

## Completed Session Log

| # | Step | Expected (facilitator) | Actual Result (operator think-aloud) | Pass |
|---|------|------------------------|--------------------------------------|------|
| 1 | NPE mission goal readability | Operator states patrol/classify/Begin Execution goal within 60s from spec | Operator read mission goal aloud and paraphrased **classify before engage** — **clearer than session 1 fixture inference** | ☑ |
| 2 | First-actions table | Operator can sequence OOB → map → CONTACTS → top bar → Begin Execution | Operator followed §First Actions without facilitator correction — **actionable NPE script** | ☑ |
| 3 | Message-log onboarding hint | Operator cites mission stub as in-game anchor | Operator: "I'd see this in the log header on entry — fixes the 'no briefing' gap from session 1" | ☑ |
| 4 | COMMS legend text labels | Operator distinguishes NOMINAL / DEGRADED / DENIED without color-only reliance | Operator matched legend lines to session 1 checks 9–11 behavior; noted **text satisfies accessibility** per `accessibility-requirements.md` | ☑ |
| 5 | Legend ↔ map symbology | Operator links DEGRADED text to 55% hostile + ghost duplicate | Cross-read P-C2-05 + `difficulty-curve.md` Loop 2; operator predicted dimming **without facilitator hint** — session 1 gap closed | ☑ |
| 6 | DENIED order block | Operator expects engage denial with legend context | Operator: "DENIED line says no new engagements — I'd check attack menu before blaming a bug" | ☑ |
| 7 | Catalog lag helper (mid-game) | Operator connects `LatencyMsNominal` → share-lag ticks from helper alone | Operator connected catalog edit to sim lag **without facilitator** — improvement vs session 3 step 6; still wants **in-fight HUD overlay** | ☑ |
| 8 | Difficulty band authority | Operator ranks fixtures using `difficulty-curve.md` bands | Operator produced band-aligned tier list (classify=Band A, catalog-latency=Band C) — **session 3 step 10 fail reversed** | ☑ |
| 9 | FUEL line NPE | Operator notes FUEL still subtle | Operator: "Onboarding table doesn't call out FUEL — still medium confusion for new players" | ☑ |
| 10 | Readiness recovery | Operator identifies readiness copy gap | Operator: "Log denial yes; fix path still missing — same as session 3" | ☑ |
| 11 | Delegation / trust UX | Operator notes pillar gap unchanged | Operator confirmed **delegation badges still absent** — S36+ scope; non-blocking | ☑ |

**Session verdict:** **PASS WITH NOTES** — S35-06 presentation polish materially improves NPE goal clarity, COMMS legend comprehension, and curator lag mental model vs sessions 1–3; residual P1 items (FUEL callout, readiness recovery, in-fight lag overlay, delegation UX) remain Polish follow-up.

---

## Operator Quotes (think-aloud captures)

- *"Reading onboarding-baltic — I'd have known classify-before-engage in session 1 without guessing from fixture names."*
- *"NOMINAL / DEGRADED / DENIED as text — I don't have to decode amber/red alone; that's what session 1 was missing."*
- *"Catalog lag helper — now I get LatencyMsNominal → ticks; I'd still want that on the map during the latency fixture."*
- *"Difficulty-curve bands — I can rank scenarios without improvising tiers like last time."*

---

## Findings Routing

### Design
- Add **FUEL line callout** to `onboarding-baltic.md` §First Actions (step 7) or unit-detail tooltip — carried from session 1.
- Author **readiness recovery one-liner** for `AIR_NOT_READY` denials (`difficulty-curve.md` Loop 4 P1).
- Keep **delegation badges / trust signals** on S36+ backlog — out of S35 polish scope.

### Balance
- N/A — presentation-only delta; Band A–C ratings unchanged and **Just Right** for analytical audience.

### Bug
- None filed — `C2CommsOnboardingTests` **4/4 PASS**; behavioral checks 9–11 unchanged.

### Polish
- **P1:** In-fight lag-source attribution overlay (catalog vs policy) — helper text insufficient for Band C in-scenario players.
- **P1:** HUD order-denial one-liner beyond log row (Loop 2 recover path).
- Optional live Editor re-capture for comms legend row density at 1080p (S35-07 advisory).

---

## Quantitative Anchors

| Metric | Value |
|--------|-------|
| `C2CommsOnboarding` | **4/4 PASS** |
| Checks 1–13 filter | **85/85 PASS** |
| Checks 14–18 filter | **58/58 PASS** |
| ReplayGolden (referenced) | **6/6 PASS** |
| S35-06 UXML surfaces validated | Top bar legend, catalog lag helper, message-log hint |
| Design docs referenced in script | `onboarding-baltic.md`, `interaction-patterns.md`, `difficulty-curve.md` |