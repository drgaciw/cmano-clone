# Gauntlet next steps — tiers 3–5 catalog red + oracle CLI

**Date:** 2026-07-13  
**Scope:** Recommended P0–P2 from post–t1/t2 catalog work.

## Delivered

### P0 — Catalog red on tiers 3–5 (+ joint smoke)

Every `gauntlet-t3*` … `t5*` policy and `gauntlet-joint-orbat-smoke`:

| Side | Platforms |
|---|---|
| Blue joint | Visby (surface), Gripen (air), Gotland (sub) |
| Red | Sovremenny (+ Steregushchiy when multi-target detection) |

Detection `targetId`s use catalog red only — **no** `hostile-1` / `hostile-far` / `u1`.

Side-correct combat via existing `BalticV3SideRegistry` registration.

### P1 — Direction tests

- `BalticReplayHarnessGauntletTier35CatalogTests` — red units, no synthetic detection, blue-on-red launches.
- Joint ORBAT test asserts `CATALOG_UNIT:em-sovremenny…` and no `|hostile-1|`.

### P2 — Shipped oracle CLI

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- gauntlet_oracle_eval \
  --policy-dir <tier-dir> --csv <results.csv> --out oracle-eval.json
```

Calls `GauntletOracleEvaluator.EvaluateFromPolicyAndCsv` only. Skill Phase C documents the CLI.

### P3 (lite / deferred)

Air/sub remain **detection-role** on joint ORBAT; **surface Visby** is primary shooter against catalog red. Full air/sub engage shooters remain a follow-up.

## Batch evidence (seeds 42,7,123)

| Tier | Ticks | Rows | blue-on-red | red-on-blue | red-on-red |
|---|---|---|---|---|---|
| 3–5 combined | 16/24/40 | 36 | 83 | 20 | **0** |

C#/CLI oracle: **12/12** t3–t5 scenarios Passed after expect recalibration.

## Files

- Policies: `data/scenarios/gauntlet-t{3,4,5}-*.policy.json`, `gauntlet-joint-orbat-smoke.policy.json`
- Tests: `BalticReplayHarnessGauntletTier35CatalogTests.cs`, CLI `GauntletOracleEvalCommandTests.cs`
- CLI: `GauntletOracleEvalCommand.cs`
- Skill: `.claude/skills/qa-gauntlet/SKILL.md` Phase C CLI gate
