# Fixes — gauntlet-20260720-2000

## GAUNTLET-ORACLE-T5-ROE-001 (oracle)

- **Symptom:** Tier-5 `gauntlet-t5-roe-change` oracle red at ticks=40: denials 80 > max 28; score -200/-300 < min 50 (all seeds 42,7,123).
- **Class:** oracle (expect envelope), not sim-code. Fingerprint inject evidence still present; numerics match pre-e0f59a6 ladder baseline.
- **Root cause:** CI recalibration e0f59a6 rewrote `gauntlet.expect` for ticks=10 smoke, violating S95 tier-tick discipline (ladder T5@40 is authority).
- **TDD:**
  - RED: dual-profile evaluator tests (`profile: ci` vs default ladder).
  - GREEN: `GauntletOracleEvaluator` + CLI `--profile ladder|ci`; restore ladder expect; add `expectCi`; CI workflow uses `--profile ci`.
- **Verify:** T5 allPassed; CI dry-run allPassed; x10 ladder 10/10; suite **1782/0f** (was 1779, +3 tests); hindsight SYN-T12 + MD-001 PASS.
- **Files:**
  - `src/ProjectAegis.Data/Catalog/GauntletOracleEvaluator.cs`
  - `src/ProjectAegis.Data.Tests/Catalog/GauntletOracleEvaluatorTests.cs`
  - `src/ProjectAegis.MissionEditor.Cli/GauntletOracleEvalCommand.cs`
  - `src/ProjectAegis.MissionEditor.Cli/Program.cs`
  - `data/scenarios/gauntlet-t5-roe-change.policy.json`
  - `.github/workflows/gauntlet-oracle.yml`
