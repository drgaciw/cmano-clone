# QA Plan — QA Gauntlet Forge (ad-hoc feature)

**Date:** 2026-07-23  
**Scope:** `/qa-gauntlet-forge` companion + scorecard/corpus (PR #337 branch)  
**Review mode:** lean  
**Stage:** Release (not Launch)  
**Smoke:** S101 closeout PASS (`production/qa/smoke-sprint-101-closeout-2026-07-17.md`); forge-specific automated scorecard tests added this cycle

## Story classification

| Story | Type | Automated | Manual | Blocker? |
|-------|------|-----------|--------|----------|
| FORGE-01 skill + program.md | Integration | Docs review | Spot-check phase hooks | No |
| FORGE-02 corpus bootstrap | Config/Data | coverage consistency test | — | Fixed |
| FORGE-03 forge_scorecard.py | Logic | **pytest required** | — | Fixed |
| FORGE-04 qa-gauntlet wiring + expect-regen | Integration | — | Next full gauntlet run | No |

## Entry criteria

1. S101 smoke PASS (suite floor) — met  
2. Forge scorecard unit tests green — required this cycle  
3. No false promote of already-indexed ladder policies — required  

## Exit criteria

- P1–P4 defects fixed and verified  
- `python3 -m pytest tools/qa-gauntlet/test_forge_scorecard.py` PASS  
- Tier 1–5 scorecard on `gauntlet-20260723-1416` → promote=0 for indexed policies  

## Out of scope

Full 5-tier gauntlet ladder re-run; S104 C2 host stories; Launch advance.
