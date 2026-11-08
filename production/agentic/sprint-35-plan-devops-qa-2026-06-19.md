# Sprint 35 Plan — DevOps + QA/UX Track

**Date:** 2026-06-19  
**Agent:** parallel dispatch (c-sharp-devops-engineer + team-qa + team-ui)

## DevOps stories

| ID | Title | Priority | Est | Acceptance |
|----|-------|----------|-----|------------|
| S35-01 | Full-sln re-baseline | must-have | 1d | 1193/1193; ReplayGolden 6/6; GitNexus; smoke index |
| S35-14 | Closeout hygiene | must-have | 0.5d | `smoke-sprint-35-closeout-*.md`; carry-forward log |
| S35-13 | Stage advance → Polish | should-have | 0.25d | User-confirmed gate CONCERNS; `stage.txt` update |
| S35-15 | CI/local gate refresh | nice-to-have | 0.5d | 6th deferral OK; `verify-ci-local.ps1` disposition |

## QA / UX stories

| ID | Title | Priority | Est | Acceptance |
|----|-------|----------|-----|------------|
| S35-02 | QA plan (blocks impl) | must-have | 1d | `production/qa/qa-plan-sprint-35-2026-06-19.md` |
| S35-03 | UX foundation trio | must-have | 2d | `design/accessibility-requirements.md`, `design/ux/interaction-patterns.md`, `design/difficulty-curve.md` |
| S35-07 | C2 sign-off 18/18 | must-have | 1d | `sprint-35-c2-signoff-*.md`; 61/61 + 58/58 filters |
| S35-11 | Playtest session 7 | should-have | 1d | New file under `production/playtests/` |

## Wave order

1. S35-01 → S35-02 (blocking)  
2. S35-03 parallel S35-04 (unity plan)  
3. S35-07 after S35-06  
4. S35-14 closeout; S35-13 optional last