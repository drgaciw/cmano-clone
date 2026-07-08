# Adversarial TDD Hardening Pass — 2026-07-08

**Branch:** `test/adversarial-tdd-harden`  
**Mode:** Parallel adversarial audit (4 domains) → TDD regression pins  
**Code lead train:** unaffected (tests only; zero DelegationBridge production edits)

## Audit domains

| Domain | Agent focus |
|--------|-------------|
| Delegation / loop | Manual gate, ROE vs approval, TieredRebrief rebind |
| Modes / determinism | AttachReplayViewer, dual-side inventory, hash pin |
| Write-gate / OSINT | Propose≠live, silent mutate, 0.65 boundary |
| UA / docs / CLI | Policy-engage dual-surface, hub FR map, validate exit codes |

## Pins implemented (green)

| Test | Assembly | Hardens |
|------|----------|---------|
| `player_approval_cannot_override_roe_reject_even_on_manual` | Delegation.Tests | ROE &gt; approval |
| `TryTakeDirectControl_returns_false_when_AttachReplayViewer_enabled` | Delegation.Tests | Observer take-control |
| `TryReleaseDirectControl_returns_false_when_AttachReplayViewer_enabled` | Delegation.Tests | Observer release-control |
| `Manual_agent_tick_never_executes_order_without_player_approval` | Delegation.Tests | Manual never auto-fires e2e |
| `TryRebindAgentTraits_tieredRebrief_denies_full_autonomous_while_executing_without_mutation` | Delegation.Tests | Orchestrator rebind path |
| `ProposeSensorBatch_without_ApproveBatch_not_readable_as_live_catalog` | Data.Tests | Staging ≠ live |
| `hub_lists_FR_01_through_FR_19_and_related_index_targets_exist` | Data.Tests | OV-SC-G5 corpus honesty |
| `scenario_validate_missing_file_returns_exit_2_json_error` | Cli.Tests | CLI exit-2 contract |

**Verify (targeted):** Delegation filter **6 passed**; Data filter **2 passed**; CLI filter **1 passed**.

## Backlog (not implemented this pass)

1. Dual-surface ROE WeaponsTight hard pin (no soft Or) — high value, UA harness  
2. Dual-side shippable policy inventory (all `*.policy.json` false except sandbox)  
3. Named production hash corpus pin for OV-SC-G2  
4. Silent overwrite change_log previous_value assert  
5. OSINT exact 0.65 digest E2E  
6. CLI malformed JSON stable exit (may need production try/catch)

## Invariants

- No `src/**/*.cs` production changes  
- No golden / hash edits  
- DelegationBridge hotpath untouched  
