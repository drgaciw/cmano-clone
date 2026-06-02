# C2 manual QA sign-off — 2026-06-02

**Build:** `main` @ `2a08518` (Sprints 7–9 + Unity gitignore)  
**Scenarios:** `baltic-patrol-classify` (selection), `baltic-patrol-comms` (COMMS + map stale)  
**Reference:** `unity/ProjectAegis/PLAYMODE-SMOKE.md`  
**Headless pre-check:** `production/qa/headless-smoke-evidence-2026-06-02.md` (PASS 243 tests)  
**Automated proxy:** `production/qa/c2-automated-proxy-2026-06-02.md` (checks 2–12 partially covered headless)

| # | Check | Pass | Tester | Notes |
|---|--------|------|--------|-------|
| 1 | Play starts without console errors | ☐ | | |
| 2 | Default unit selected (OOB + map ring) | ☐ | | |
| 3 | Map ■ click updates unit detail | ☐ | | |
| 4 | OOB row click syncs map | ☐ | | |
| 5 | Hostile ◆ shows CONTACT line | ☐ | | |
| 6 | CONTACTS tab click matches map | ☐ | | |
| 7 | Top bar score ticks on engage | ☐ | | |
| 8 | Message log shows CONTACT/MISSION lines | ☐ | | |
| 9 | `baltic-patrol-comms`: COMMS bar DEGRADED → DENIED | ☐ | | |
| 10 | Hostile ◆ dimmed (degraded), all symbols dimmer (denied) | ☐ | | |
| 11 | Engage denied in log after DENIED (no new launches) | ☐ | | |
| 12 | Unit detail FUEL line updates over long sim time | ☐ | | |

**Verdict:** ☐ PASS ☐ FAIL  
**Blockers:**