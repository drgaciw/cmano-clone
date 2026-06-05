# Sprint 18 — C2 manual sign-off runbook (S18-01)

**Date:** 2026-06-04  
**Checklist:** `production/qa/c2-manual-signoff-2026-06-02.md` (13 checks)  
**Build under test:** `main` @ `f7e6fa6` (PR #69; update SHA in checklist header when starting)

## Prerequisites

1. Unity **6000.3.14f1** — see `unity/ProjectAegis/PLAYMODE-SMOKE.md`
2. Headless pre-check **PASS** — `production/qa/smoke-2026-06-04.md`
3. Scenarios: `baltic-patrol-classify`, `baltic-patrol-comms`

## Headless proxy (already green)

| Check | Proxy |
|-------|--------|
| 2–12 (except 1, 13 partial) | `production/qa/c2-automated-proxy-2026-06-02.md` |
| 13 Attack menu | `DelegationBridgeAttackOptionTests`, `UnitDetailBridgeTests`, PlayMode if wired |

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
- Update `production/sprint-status.yaml` → `s18-c2-signoff: done` when PASS