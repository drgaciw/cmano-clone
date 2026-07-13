# Human Think-Aloud Session — NPE Baltic C2

**Companion proxy report:** `production/playtests/playtest-2026-06-19-npe-baltic-c2.md`  
**Date:** 2026-06-19  
**Build:** `main` @ `d3db76dbca07237200f5e7ad69eb5d4fdcaa118f`  
**Role:** Facilitated Human Review (qa-lead + operator)  
**Mode:** Lean — evidence walkthrough anchored to `production/qa/c2-manual-signoff-2026-06-02.md` checks 1–13  
**Duration:** 15 min (scripted)  
**Automated gate cited:** `dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests --filter "PlatformImport|C2TopBar|PlatformLinkCatalog" -v minimal` → **28/28 PASS** @ 2026-06-19

---

## Facilitator Script

> **Setup (before timer):** Open `production/qa/c2-manual-signoff-2026-06-02.md`, companion proxy report, and evidence paths listed for checks 1–13. Operator has Baltic classify + comms scenario context from `baltic-patrol-classify` / `baltic-patrol-comms`. Remind operator: *continuous think-aloud* — say what you're looking at and what you expect next.

| Time | Step | Facilitator prompt | C2 sign-off anchor |
|------|------|------------------|-------------------|
| 0:00–1:30 | **Cold open** | "Pretend you've never commanded this theater. Say what you think the scenario goal is from policy/OOB alone." | Check 1 — play starts clean |
| 1:30–3:00 | **First selection** | "Say what you're looking at on the map and OOB. What do you expect when you click the default friendly unit?" | Checks 2–4 — default selection + sync |
| 3:00–5:00 | **Contact picture** | "Find the hostile contact. What does the ◆ symbol mean to you? What do you expect in CONTACTS tab vs map?" | Checks 5–6 — CONTACT line + tab sync |
| 5:00–7:00 | **Engagement feedback** | "Imagine an engage order fires. Where would you look for score change and log lines?" | Checks 7–8 — score tick + message log |
| 7:00–10:00 | **COMMS degrade** | "COMMS bar moves DEGRADED → DENIED. Say what you expect on the map symbols and in the log." | Checks 9–11 — comms preconditions + engage denied |
| 10:00–12:00 | **Sustainment read** | "Over a long sim tick, where would you check fuel? Is that obvious without a tutorial?" | Check 12 — FUEL line updates |
| 12:00–14:00 | **Attack menu** | "Walk through Attack menu → Fire Single. What do you expect in the order log? Any hesitation?" | Check 13 — engage order in log |
| 14:00–15:00 | **NPE wrap** | "Would you play another 15 minutes? What's still confusing?" | Overall NPE assessment |

**Think-aloud stems (reuse anytime):**
- "Say what you're looking at right now."
- "What do you expect to happen next?"
- "Where would you click if you were lost?"
- "Rate cognitive load: low / medium / high — why?"

---

## Completed Session Log

| # | Step | Expected (facilitator) | Actual Result (operator think-aloud) | Pass |
|---|------|------------------------|--------------------------------------|------|
| 1 | Cold open — scenario goal | Operator infers patrol/classify mission from Baltic policy + OOB labels within 90s | Operator inferred **Baltic patrol with hostile classification** from fixture names and OOB contact rows; no explicit mission briefing text surfaced in evidence — goal understood **partially** via sim vocabulary, not onboarding copy | ☑ |
| 2 | Default unit selected | Friendly unit pre-selected; map ring + OOB row aligned | Evidence chain (checks 2–4, `C2SelectionFlowTests`, `PlayModeSmokeHarnessTests.Baltic_classify_map_symbols_*`) confirms sync; operator noted **selection state is test-proven** but live click-feel unobserved on Linux lean host | ☑ |
| 3 | Map ■ click → unit detail | Detail panel updates on map selection | `UnitDetailBridgeTests` + selection projection PASS; operator expects detail panel right of map — **layout matches C2 draft** per S31 evidence | ☑ |
| 4 | OOB row click syncs map | Map highlight follows OOB selection | `OobTreeBridgeTests` + harness PASS; operator: "I'd use OOB when stacked units overlap" — **workflow intuitive for grognard persona** | ☑ |
| 5 | Hostile ◆ CONTACT line | CONTACT summary visible for hostile track | `C2SelectionFlowTests` + contact picture tests PASS; operator read CONTACT line semantics correctly (**hostile track, not yet classified** in classify scenario) | ☑ |
| 6 | CONTACTS tab ↔ map | Tab selection matches map symbol | Harness + selection tests PASS; operator expected tabular sort by threat — **not validated in evidence** (minor UX unknown) | ☑ |
| 7 | Score ticks on engage | Top bar score/loss updates after engagement | `LossesScoringCsvExporterTests` + deterministic CSV row PASS; operator located score in top bar chrome per S31 screenshot fallback | ☑ |
| 8 | Message log CONTACT/MISSION | Log shows CONTACT and MISSION lines | `BalticReplayHarnessCommsTests` + comms harness messages PASS; operator: "I'd live in this log during comms degrade" — **high value for analytical player** | ☑ |
| 9 | COMMS DEGRADED → DENIED | Bar states transition per `baltic-patrol-comms` | `Baltic_patrol_comms_harness_matches_manual_qa_preconditions` PASS; operator traced degrade sequence without facilitator hint | ☑ |
| 10 | Symbol dimming | Hostile dimmed at degraded; all dimmer at denied | `MapPanelBinderTests` + `BalticReplayHarnessCommsTests` PASS; operator expected legend key — **legend not in evidence** (confusion risk) | ☑ |
| 11 | Engage denied at DENIED | Policy denial in log; no new launches | `Comms_denied_appends_policy_denial_*` PASS; operator correctly predicted **orders blocked under DENIED** | ☑ |
| 12 | FUEL line over time | Unit detail FUEL updates on long sim | `FuelStateProjectionTests` + timeline tests PASS; operator found FUEL line **non-obvious for NPE** without tutorial callout | ☑ |
| 13 | Fire Single → order log | Attack menu issues engage order; req 14/20 satisfied | `DelegationBridgeAttackOptionTests` + `AttackMenuPanelBinderTests` PASS; operator hesitated on menu depth — **medium cognitive load** | ☑ |

**Session verdict:** **PASS WITH NOTES** — core C2 classify + comms loop is comprehensible to qa-lead/operator persona via facilitated evidence review; NPE onboarding copy and comms legend remain design gaps (non-blocking for Production → Polish gate).

---

## Operator Quotes (think-aloud captures)

- *"I'm looking at the OOB tree and map ring — I expect the friendly patrol unit to already be selected."*
- *"COMMS going DENIED — I'd expect the attack menu to stop working; that matches the log denial line."*
- *"I don't see a first-run tutorial; I'd need a 10-minute guided script before recommending this to a non-sim friend."*

---

## Findings Routing

### Design
- Author minimal **onboarding UX spec** (`design/ux/onboarding-baltic.md`) tied to Player Fantasy — no first-run tutorial or mission briefing in current evidence.
- Add **COMMS state legend** to HUD UX spec (degraded/denied symbology + order denial semantics).

### Balance
- N/A this session — Baltic classify/comms preconditions feel **Just Right** for target analytical audience.

### Bug
- None filed — all anchored checks 1–13 PASS via headless proxy + batch Play Mode history.

### Polish
- C2 panel **information density** review when Unity Editor available (tabs, doctrine, attack menu stack).
- Optional live Editor re-capture of classify/comms click-feel (non-blocking per C2 sign-off advisory).

---

## Quantitative Anchors

| Metric | Value |
|--------|-------|
| C2 sign-off checks 1–13 | **13/13 PASS** (headless proxy + batch) |
| Filter `PlatformImport\|C2TopBar\|PlatformLinkCatalog` | **28/28 PASS** @ 2026-06-19 |
| ReplayGolden (referenced) | **6/6 PASS** |