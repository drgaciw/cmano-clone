# Sprint 118 — Gauntlet residual quality

**Dates:** 2026-08-12  
**Predecessor:** S117 merged ([PR #483](https://github.com/drgaciw/cmano-clone/pull/483) @ `952f459`)  
**Linear:** [DRG-153](https://linear.app/drgamtd-workspace/issue/DRG-153)  
**Children:** [DRG-62](https://linear.app/drgamtd-workspace/issue/DRG-62) · [DRG-64](https://linear.app/drgamtd-workspace/issue/DRG-64) · [DRG-65](https://linear.app/drgamtd-workspace/issue/DRG-65)  
**Stage:** **Release** · **Not Launch**

## Goal

Close the three leftover gauntlet quality bugs from the PR #365 review: coverage-map `stressAxes` drift can fail, infer no longer treats stock `salvoSize>=2` as weapons pressure, and EW moderate is documented as unproven (Won't-retune without a 3-seed measurement).

## Must Have

| ID | AC |
|----|-----|
| S118-01 / DRG-64 | `_infer_stress_axes` keys weapons on magazine starve (`rounds<=2`), not `salvoSize`. Stock `gauntlet-t3-escort-strike` is `weapons:off`. |
| S118-02 / DRG-62 | Every coverage-map cell has `stressAxes` matching `infer_cell`. Bootstrap test compares `stressAxes`. `--rebuild-stress-axes` exists. |
| S118-03 / DRG-65 | README documents unproven non-`off` gate meaning. `ew: moderate` is **not** claimed proven. jamStrength **not** retuned without measurement. |

## Non-goals

Promote `swarm_*` axes · Phase N · DelegationBridge · CatalogWriteGate · S119 hygiene · S120/S121 C2

## Verify

```bash
python3 tools/qa-gauntlet/forge_scorecard.py --rebuild-stress-axes
pytest tools/qa-gauntlet/test_forge_scorecard.py tools/qa-gauntlet/test_stress_axes.py -q
```
