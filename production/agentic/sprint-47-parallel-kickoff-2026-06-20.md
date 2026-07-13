# Sprint 47 Parallel Kickoff — Release Dry Run (B6 Prep)

**Date:** 2026-06-20 (planning artifact)  
**Sprint plan:** `production/sprints/sprint-47-release-dry-run.md`  
**Prereq:** B5 complete  

> **⛔ PLANNING ONLY**

## Sprint Goal

Full gate dry run: tests, smoke, gate-check draft, CI preflight, evidence bundle, Go/No-Go checklist.

## Wave plan

| Wave | Track | Est. | Agent env | Stories |
|------|-------|------|-----------|---------|
| W1 | Test + smoke | 1.5d | Cloud | S47-01 |
| W2 | Gate-check draft | 1.5d | **Local** | S47-02 |
| W3 | CI preflight | 1.5d | Cloud | S47-03 |
| W4 | Evidence consolidation | 1.5d | **Local** | S47-04 |
| W5 | Go/No-Go + closeout | 1d | Local | S47-05 |

## Track ownership

| Track | Stack prefix | Agent env |
|-------|--------------|-----------|
| Test + smoke | `stack/sprint47/test-smoke` | Cloud |
| Gate-check | `stack/sprint47/gate-check` | **Local** |
| CI preflight | `stack/sprint47/ci-preflight` | Cloud |
| Evidence | `stack/sprint47/evidence` | **Local** |
| Closeout | `stack/sprint47/closeout` | Local |

## File ownership matrix

| Path | Owner |
|------|-------|
| `production/gate-checks/release-dry-run-*` | Gate-check |
| `.buildkite/**` | CI preflight (read-only unless fix) |
| `production/qa/evidence/` (bundle) | Evidence |

---

*Planning only. Surfaces blockers for S48.*
