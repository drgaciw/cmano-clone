# Mission Editor UAT — multi-variant (2026-07-19)

**Orchestration:** 3 parallel agents (CRUD types · board ops · automated Mission tests) + TDD fix for NL plan gaps.

**Surface:** headless Mission Editor CLI (`mission_*` verbs). Unity Mission Board chrome remains deferred; C2 MISSIONS tab is runtime projection only.

## Variations

| Base | Role |
|------|------|
| `scenario_create` + orbat seed | Empty mission board |
| `golden_clean.json` | Existing patrol-1 |
| `doctrine-inheritance.json` | Patrol + strike fixtures |
| `strike-package.scenario.json` (TL-0) | Multi-unit strike |
| `ferry-redeploy.scenario.json` (TL-2) | Board ops only (validate fails TL) |

## CRUD results (52 steps)

All **mission_add/update** for Patrol, Strike, Ferry, Support + **list/delete** + negatives:

| Check | Result |
|-------|--------|
| Full CRUD × 3 bases | **52/52 PASS** |
| 2-waypoint patrol | exit 1 `INVALID_ZONE` (correct) |
| Stale editVersion | exit 3 `CONFLICT` (correct) |

Log: `production/qa/uat-mission-crud-variants-2026-07-19.log`

## Board ops results

| Op | A create | B golden | C strike-pkg | D ferry-redeploy |
|----|----------|----------|--------------|------------------|
| mission_list + filters | PASS | PASS | PASS | PASS |
| all 4 templates add | PASS | PASS | PASS | PASS |
| mission_clone | PASS | PASS | PASS | PASS |
| plan_suggest (pre-fix) | ferry intent gap | same | same | same |
| scenario_validate post empty templates | FAIL expected (`MISSION_NO_UNITS`) | same | same | + TL_* |

Log: `production/qa/uat-mission-board-variants-2026-07-19.log`

## Automated tests

| Suite | Passed | Failed |
|-------|-------:|-------:|
| Cli.Tests ~Mission | 108 | 0 |
| Data.Tests ~Mission | 39 | 0 |
| Post-fix plan_suggest tests | included in Mission filter | 0 |

## Bugs fixed (TDD)

| Issue | Fix |
|-------|-----|
| `mission_plan_suggest` ignored ferry/redeploy/tanker/support/aew (fallback only) | Keyword suggestions for `mission_add_ferry`, `mission_add_support`, templates `tpl-ferry-empty` / `tpl-support-tanker`; token-safe `ew` matching |
| `mission_update_support` response omitted `supportRole` | Echo `supportRole` in WriteOk payload |

Tests: `Run_ferry_tanker_support_intent_suggests_ferry_and_support_tools`, `Run_aew_support_intent_suggests_aew_role`

## Known residuals (not regressions)

1. Empty templates fail **validate** until units/targets assigned — by design.
2. TL-2 example `ferry-redeploy` fails catalog branch validation — fixture/catalog gap.
3. `--side` filter empty when ORBAT lacks listed unit ids (sideId resolution).
4. Unity Mission Board Editor window still deferred (product scope).

## Operator smoke

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -c Release -- \
  mission_plan_suggest --intent "ferry redeploy tanker support"
# expect mission_add_ferry + mission_add_support, role Tanker

dotnet run --project src/ProjectAegis.MissionEditor.Cli -c Release -- \
  mission_list --path assets/data/scenarios/validation/doctrine-inheritance.json
```
