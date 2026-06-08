# Sprint 19 — C2 manual sign-off runbook (S19-01 carryover)

**Date:** 2026-06-08 (handoff); runbook authored 2026-06-04  
**Context:** Sprint 18 S18-01 carried forward as **S19-01**  
**Handoff:** `production/qa/sprint-19-c2-signoff-handoff-2026-06-08.md`  
**Checklist:** `production/qa/c2-manual-signoff-2026-06-02.md` (13 checks)  
**Build under test:** `main` post-rebase (Sprint 19 handoff; update SHA in checklist header when starting Editor run)

## Prerequisites

1. Unity **6000.3.14f1** — see `unity/ProjectAegis/PLAYMODE-SMOKE.md`
2. Headless pre-check **PASS** — `production/qa/smoke-2026-06-08.md`
3. Scenarios: `baltic-patrol-classify`, `baltic-patrol-comms`

## Headless proxy (already green)

| Check | Proxy |
|-------|--------|
| 2–12 (except 1, 13 partial) | `production/qa/c2-automated-proxy-2026-06-02.md` |
| 13 Attack menu | `DelegationBridgeAttackOptionTests`, `UnitDetailBridgeTests`, PlayMode if wired |

**PI-006 closure:** `production/qa/pi-006-headless-proxy-2026-06-04.md`

## Editor steps (human)

1. Open `unity/ProjectAegis`, load Baltic patrol scene per PLAYMODE-SMOKE.
2. Enter Play Mode; confirm no console errors (check **1**).
3. Walk checks **2–12** per checklist table.
4. **Check 13:** Select friendly unit with hostile track → open attack options → choose **Fire Single** → confirm engage/order log entry (no launch if policy denies).
5. Mark PASS/FAIL per row; set verdict at bottom of checklist file.
6. If FAIL, file bug under `production/qa/bugs/` with scenario + repro steps.

## Completion criteria

- All 13 rows checked with tester name/date
- Verdict **PASS** or **FAIL** recorded in checklist
- Update `production/sprint-status.yaml` → `s19-01: done` when PASS
- Playtest notes to `production/session-logs/playtest-sprint19-c2-signoff.md` (per QA plan)

## Execution note (2026-06-08 handoff)
Headless/proxy evidence refreshed (`smoke-2026-06-08.md`, 403/403 tests). Checklist updated with test mappings for 2–13 + explicit notes for Editor-only rows. Full visual sign-off (clicks, dimming, tab sync in scene) still requires local Editor run by human per prerequisites.
