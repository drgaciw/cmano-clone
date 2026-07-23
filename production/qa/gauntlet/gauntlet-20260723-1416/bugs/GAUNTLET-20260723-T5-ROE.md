# GAUNTLET-20260723-T5-ROE

- **Scenario:** `gauntlet-t5-roe-change`
- **Tier:** 5
- **Class:** `oracle`
- **Determinism:** fingerprint stable for fixed seed (verified on t3-id-roe)
- **Description:** ticks=40 denser ORBAT: denials=360 score=-1700; expects from ticks=10 CI calibration (maxDenials=180 minScore=-430). Fingerprint inject gates still valid.
- **Remediation:** recalibrate `gauntlet.expect` numeric bounds for ladder tick budgets + seeds 42,7,123; keep fingerprint/shooter fail-closed gates.
