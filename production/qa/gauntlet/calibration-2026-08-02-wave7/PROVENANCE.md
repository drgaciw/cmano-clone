# PROVENANCE — calibration-2026-08-02-wave7

| Field | Value |
|-------|-------|
| **Program** | Wave 7 track W7-a / W7-b |
| **Baseline HEAD** | `a2c4c49` — feat(sim+qa): Winchester hard engage gate + saboteur mutant 09 (#381) |
| **Catalog** | `tools/qa-gauntlet/mutants/catalog.yaml` (00 control + 01–09 defects) |
| **Driver** | `tools/qa-gauntlet/saboteur.py` |
| **Ladder subset** | tiers 1/3/5, seeds as driver defaults, roving 0 |
| **ReplayGolden filter** | UnityAdapter ReplayGolden suite |
| **Kill rate** | **9/9** defects (`caughtDefects=9`, `survivedDefects=0`) |
| **Control** | `00-noop-comment` SURVIVED (expected) |
| **Invalid mutants** | 0 |
| **Env** | `VSTEST_CONNECTION_TIMEOUT=300`, .NET 8.0.423, Linux worktree |
| **Date (UTC)** | 2026-08-02 |

## Notes

- Mutant **09** caught by goldens/tiers/token_coverage (+ ReplayGolden on UnityAdapter).
- Mutant **08** caught by goldens/tiers/token_coverage/victory_roe (ReplayGolden stayed green — ladder oracles load-bearing).
- T5 alone passed under mutant **08** while the aggregate run still failed — residual discriminative note for W7-d triage (do not weaken goldens).
