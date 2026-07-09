# Gauntlet TDD fix log

## FIX-001 — Magazine depletion / catalog seed capacity / kill-chain clean findings

- **Commit:** `d417301` — `qa(gauntlet): fix magazine depletion + catalog seed capacity/kill-chain`
- **Tier found:** Preflight (Phase 0 baseline red — 32 failing tests blocked ladder)
- **Classification:** `sim-code` + `scenario-data` (catalog seed)
- **Red tests (representative):**
  - `ReplayGoldenBalticMagazineTests.Magazine_depletion_emits_MagazineEmpty_abort`
  - `PlatformWorkbookPhaseDWriteTests.E2E_sensor_export_edit_propose_approve_readback_via_write_service` (PLE-MAG-CAPACITY)
  - `KillChainRulePackTests.CrossSystem_orchestrator_Baltic_default_has_no_kill_chain_codes_on_clean_catalog`
  - `CatalogKillChainReportCommandTests.KillChain_catalog_kill_chain_report_empty_golden_on_clean_baltic`
- **Root cause:**
  1. Catalog magazine qty 120 on gun-76 mount capacity 1 blocked all workbook proposes.
  2. Catalog magazine totals overrode scenario `defaultMagazineRounds`, so `NO_AMMO` never fired.
  3. Missing-mobility speed checks emitted `KILL_CHAIN_SPEED_MISMATCH` warnings that broke clean Baltic goldens after engage rows were seeded.
- **Green fix:**
  - `CatalogSeedBootstrap`: gun magazine quantity clamped to 1; production DB synced.
  - `SimulationSession`: cap ledger rounds to scenario `DefaultMagazineRounds` when tighter than catalog.
  - `KillChainRules.EvaluateSpeedMismatch`: skip silently when mobility absent (still errors when both present and mismatched).
  - Import/kill-chain gate tests updated for pre-seeded engage rows.
- **Verify:** full suite 1609 passed / 0 failed; ReplayGolden 6/6; Phase0 smoke PASS.
- **Impact notes:** HIGH on catalog seed + kill-chain report surface area; no CRITICAL quarantines.
- **Parallelism:** sequential (shared seed/catalog blast radius).

No additional sim-code defects found during tier ladder execution (all 5 tiers green).
