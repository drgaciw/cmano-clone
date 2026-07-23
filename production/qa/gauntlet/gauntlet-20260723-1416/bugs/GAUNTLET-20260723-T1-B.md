# GAUNTLET-20260723-T1-B

- **Scenario:** `gauntlet-t1-patrol-b`
- **Tier:** 1
- **Class:** `oracle`
- **Determinism:** fingerprint stable for fixed seed (verified on t3-id-roe)
- **Description:** minKills=3 fails seeds 7 (2 kills) and 123 (1 kill) at ticks=6; seed=42 has 4. Multi-seed variance after denser ORBAT.
- **Remediation:** recalibrate `gauntlet.expect` numeric bounds for ladder tick budgets + seeds 42,7,123; keep fingerprint/shooter fail-closed gates.
