# Sprint 42 Parallel Kickoff — Release Kickoff (B1 W1 + B2 Start)

**Date:** 2026-06-20 (planning artifact)  
**Trunk:** `main` @ (post-S41 + **scope gate APPROVED**)  
**Sprint plan:** `production/sprints/sprint-42-release-kickoff-content-art-bible-w1.md`  
**QA plan:** `production/qa/qa-plan-sprint-42-*.md` (TBD)  

> **⛔ DO NOT DISPATCH** until `production/gate-checks/scope-expansion-decision-*.md` is signed. Out-of-boundary until gate.

**Authority:** Post-gate boundary doc (replaces/lifts polish-scope-boundary). Roadmap §9 (S42 = B1 wave 1 + B2 §1–4).

## Sprint Goal (recap)

First Track B sprint: content wave 1 (Catalog/Platform/Scenario) + art bible sections 1–4 + expanded QA baseline.

## Wave plan

| Wave | Stories | Track | Est. | Agent env | Notes |
|------|---------|-------|------|-----------|-------|
| Day-1 | S42-01 | Baseline + gate matrix | 1d | Cloud | New boundary cited |
| W0 | S42-02 | QA plan | 1d | Cloud | Blocks waves |
| W1 | S42-03 | Content Catalog/Platform | 3d | **Local lead** | Single Catalog cluster |
| W2 | S42-04 | Content Scenario | 2.5d | Cloud | Replay-gated |
| W3 | S42-05 | Art bible 1–4 | 2d | Cloud | `design/art/art-bible.md` |
| W4 | S42-06 | Closeout | 0.5d | **Local** | Smoke |

## Track ownership

| Track | Owner | Stories | Stack prefix | Agent env |
|-------|-------|---------|--------------|-----------|
| Content Catalog/Platform | team-data | S42-03 | `stack/sprint42/content-catalog-platform` | Local lead |
| Content Scenario | team-simulation | S42-04 | `stack/sprint42/content-scenario` | Cloud |
| Art bible 1–4 | art-director | S42-05 | `stack/sprint42/art-bible-1-4` | Cloud |
| Baseline + QA | c-sharp-devops + team-qa | S42-01, S42-02 | `stack/sprint42/baseline-qa` | Cloud |
| Closeout | c-sharp-devops-engineer | S42-06 | `stack/sprint42/closeout` | Local |

## File ownership matrix

| File / path | Owner track |
|-------------|-------------|
| `data/scenarios/*` | Content Scenario |
| `src/**/Catalog*`, `Platform*` projections | Content Catalog/Platform |
| `design/art/art-bible.md` | Art bible 1–4 |
| `production/sprint-status.yaml` | Closeout |

## Hard gates

Replay 6/6; proxy 18/18+; `impact()` on all symbol edits; post-gate boundary cite.

## Worktree bootstrap

`production/agentic/s39-s48-worktree-manifest.md` §S42.

---

*Planning only — requires scope-expansion decision.*
