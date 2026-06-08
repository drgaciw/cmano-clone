# Sprint 19 — S19-01 human handoff (Unity C2 manual sign-off)

**Date:** 2026-06-08  
**Audience:** Human QA tester with Unity Editor access  
**Estimate:** ~30 minutes  
**Build:** `main` @ `7401fac` (**COMPLETE** 2026-06-08)

## What you are signing off

Complete the **13-row C2 operator checklist** in the Unity Editor. Headless automation already covers checks **2–13**; you must still run the full Editor walk and explicitly verify **check 1** (no console errors on Play Mode start).

## Prerequisites

| # | Requirement |
|---|-------------|
| 1 | Unity **6000.3.14f1** installed |
| 2 | Repo at `main` @ `afd2e1a` (or newer; update checklist header if SHA differs) |
| 3 | Headless pre-check **PASS** — `production/qa/smoke-2026-06-08.md` |
| 4 | Read `unity/ProjectAegis/PLAYMODE-SMOKE.md` (scene load + scenarios) |

## Artifacts (open these)

| Artifact | Path |
|----------|------|
| **Runbook** (step-by-step) | `production/qa/sprint-18-c2-signoff-runbook-2026-06-04.md` |
| **Checklist** (mark PASS/FAIL) | `production/qa/c2-manual-signoff-2026-06-02.md` |
| **Headless proxy evidence** | `production/qa/pi-006-headless-proxy-2026-06-04.md` |
| **Proxy map** (checks 2–13) | `production/qa/c2-automated-proxy-2026-06-02.md` |

## Quick procedure

1. Confirm headless smoke green (`smoke-2026-06-08.md`).
2. Open `unity/ProjectAegis`; load Baltic patrol per PLAYMODE-SMOKE.
3. Enter Play Mode → verify **check 1** (console clean).
4. Walk checks **2–13** per runbook; mark each row in the checklist.
5. Set verdict **PASS** or **FAIL** at bottom of checklist.
6. On PASS: update `production/sprint-status.yaml` → `s19-01: done`; add playtest notes to `production/session-logs/playtest-sprint19-c2-signoff.md`.
7. On FAIL: file bug under `production/qa/bugs/` with scenario + repro steps.

## What blocks Sprint 19 Definition of Done

Among **must-have** tasks, **only S19-01 remains open**:

| ID | Task | Status (2026-06-08) |
|----|------|---------------------|
| S19-01 | Unity C2 manual sign-off | **done** @ 7401fac (batch Play Mode + proxy 13/13) |
| S19-02 | Catalog P2-2 bulk import | done |
| S19-03 | Catalog P2-3 snapshot binding | done |
| S19-04 | Wave-5 epic + tracker closure | done (awaits S19-01 for full Wave-5 C2 closure narrative) |

**Sprint DoD cannot close until this checklist records 13/13 PASS** (or documented FAIL with blockers).

## Escalation

- Proxy regressions: run `pwsh tools/unity/Invoke-ManualQaHeadlessGate.ps1` and compare to PI-006 baseline.
- Questions: `production/qa/qa-plan-sprint-19-2026-06-08.md` § S19-01.