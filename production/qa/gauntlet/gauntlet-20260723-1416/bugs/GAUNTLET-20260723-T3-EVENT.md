# GAUNTLET-20260723-T3-EVENT

- **Scenario:** `gauntlet-t3-event-chain`
- **Tier:** 3
- **Class:** `oracle`
- **Determinism:** fingerprint stable for fixed seed (verified on t3-id-roe)
- **Description:** score 600 > maxScore 500 at seed=42 ticks=16; denser ORBAT yields higher kill scores.
- **Remediation:** recalibrate `gauntlet.expect` numeric bounds for ladder tick budgets + seeds 42,7,123; keep fingerprint/shooter fail-closed gates.
