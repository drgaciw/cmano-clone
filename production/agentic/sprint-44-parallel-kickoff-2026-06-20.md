# Sprint 44 Parallel Kickoff — Structural Debt Refactor (B3)

**Date:** 2026-06-20 (planning artifact)  
**Sprint plan:** `production/sprints/sprint-44-structural-debt-refactor.md`  
**Prereq:** S41 ADR + scope gate + B1 locked  

> **⛔ PLANNING ONLY**

## Sprint Goal

B3: Decision + Telemetry refactor; Osint audit. Max 3 code tracks + replay gate.

## Wave plan

| Wave | Track | Est. | Agent env | Stories |
|------|-------|------|-----------|---------|
| W0 | Baseline + QA | 1.5d | Cloud | S44-01 |
| W1 | Decision refactor | 4d | **Local lead** | S44-02 |
| W2 | Telemetry refactor | 3.5d | Local/Cloud | S44-03 |
| W3 | Osint audit | 2d | Cloud | S44-04 |
| parallel | Replay gate | ongoing | Cloud | S44-05 |
| W4 | Closeout | 0.5d | Local | S44-06 |

## Track ownership

| Track | Stack prefix | Agent env |
|-------|--------------|-----------|
| Decision | `stack/sprint44/decision-refactor` | Local lead |
| Telemetry | `stack/sprint44/telemetry-refactor` | Local/Cloud |
| Osint | `stack/sprint44/osint-audit` | Cloud |
| Replay gate | `stack/sprint44/replay-gate` | Cloud |
| Closeout | `stack/sprint44/closeout` | Local |

## File ownership matrix

| Module cluster | Owner track | Rule |
|----------------|-------------|------|
| `**/Decision/**` | Decision | GitNexus `rename` only |
| `**/Telemetry/**` | Telemetry | Zero shared files with Decision |
| `**/Osint/**` | Osint | Audit-first |
| `tests/regression/*` | Replay gate | 6/6 after each merge |

---

*Planning only. Pair determinism on all sim-adjacent touches.*
