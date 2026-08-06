# GAUNTLET-20260723-T3-IDROE

- **Scenario:** `gauntlet-t3-id-roe`
- **Tier:** 3
- **Class:** `oracle`
- **Determinism:** fingerprint stable for fixed seed (verified on t3-id-roe)
- **Description:** WeaponsTight ID-ROE: denials=112 score=-560 kills=0 all seeds (deterministic). Expects calibrated for shorter/looser envelope.
- **Remediation:** recalibrate `gauntlet.expect` numeric bounds for ladder tick budgets + seeds 42,7,123; keep fingerprint/shooter fail-closed gates.
