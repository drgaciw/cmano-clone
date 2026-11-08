# Fixes — gauntlet-20260713-1739

1. **GAUNTLET-NS21-001 (sim-code):** `GauntletOracleEvaluator.CollectTrueLaunchedShooters` — remove `StringSplitOptions.TrimEntries` for netstandard2.1; trim tokens manually. Verified: multi-TF build + GauntletOracleEvaluatorTests.
2. **Oracle envelopes (9 policies):** Recalibrated `gauntlet.expect` from max-variance batch at tier tick budgets. See `oracle-expect-recalibration.json`.
