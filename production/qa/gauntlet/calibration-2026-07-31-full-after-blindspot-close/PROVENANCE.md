# Live saboteur — full catalog after blind-spot close (2026-07-31)

| Field | Value |
|-------|--------|
| Commit | `54900c7` (pre-this-artifact commit on branch) |
| Tool | `python3 tools/qa-gauntlet/saboteur.py` full catalog |
| Kill rate | **7/7** |
| Exit | **0** |
| Defects | all 7 caught (01–07); control 00 survived |

## Delta vs prior

| Artifact | Kill rate |
|----------|-----------|
| role-refresh recompute | 4/6 |
| live UnityAdapter ReplayGolden (#374) | 5/6 |
| **this run** (ladder scenarios for 03/05/06) | **7/7** |

## Scenario pins added

- `gauntlet-t2/t3-strike-salvo-boundary` — `salvoSize == maxSalvo` (mutant 03)
- `gauntlet-t3-logistics-contact-lifecycle` — Classified/Identified (mutant 05 ladder)
- `gauntlet-t3-emcon-engage-block` — real `emcon` Passive → EMCON_OFF (mutant 06)

Mutant 06 role: `expected-miss` → `defect`. `EMCON_OFF` is `requiredRunWide`.
