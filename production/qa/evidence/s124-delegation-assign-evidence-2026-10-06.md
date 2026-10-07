# S124 Delegation Evidence — Assign Agent + Rebrief success (S124-05 / S124-06)

**Sprint:** [S124 combat-commit honesty](../../sprints/sprint-124-combat-commit.md) · **QA plan:** [qa-plan-sprint-122-127-rebaseline-2026-09-30.md](../qa-plan-sprint-122-127-rebaseline-2026-09-30.md) (S124 "commit + delegation" row)
**Branch:** `cursor/s124-delegation-assign-7fee` · **Stacked on:** `cursor/s123-play-entry-7fee` @ `19b7627af8078b3aabfe462731f3469d705f96bf` (merge `910f8f48`)
**Rows:** S124-05 W2-DEL-04 initial Assign Agent · S124-06 W2-DEL-02 rebrief success path

> **Scope.** Headless evidence only. QA-plan acceptance for this half of the S124 row: "Initial Assign Agent and
> successful rebrief are exercised; failed command preserves state." Editor evidence (Unity host binding of the
> panel model) is **not** part of this branch. The commit strip / refuse bark (S124-01..04) is a separate branch;
> this work is surface-disjoint from the strip host. Does not touch or close DRG-208.

## Commits

| SHA | Content |
|-----|---------|
| `b77bf330` | Orchestrator façades `TryAssignAgentController`, `TryRebriefAgent`, gate `LoopPolicyGate.CanRebriefAgent` + 15 tests |
| `1cbe81bb` | `PlayEntrySession.CommandedTargets`, `PlayDelegation/` command façade + panel model + 21 tests (incl. golden path) |
| _commit carrying this file_ | This evidence doc |

## Story interpretation

- **Deny-only DEL-02.** Before this work, `LoopPolicyGate.CanEditPersonality` under `TieredRebrief` denied Semi-Autonomous+
  hot edits with "Rebrief Agent required at Semi-Autonomous or higher.", but no Rebrief Agent action existed
  ([phase-gate spec](../../../docs/superpowers/specs/2026-05-30-phase-gate-loop-policy-design.md) §3: "deny (rebrief deferred)").
  S124-06 adds that action. `TryRebindAgentTraits` (hot edit) is unchanged and still denies.
- **Rebrief gate** (`LoopPolicyGate.CanRebriefAgent`): `Anytime` allow · `TieredRebrief` allow (the sanctioned path) ·
  `PlanningOnly` allow in Planning, deny after Begin ("Personality locked after Begin Execution."). Rebrief sim-time cost
  is still a future policy field ([loop decisions spec](../../../docs/superpowers/specs/2026-05-30-core-gameplay-loop-decisions-design.md) §5).
- **Assign Agent timing.** Available after Begin only: S123 applies the mode's controllers at Begin via
  `SimulationModeConfigurator`, which overwrites every slot, so a Planning assignment would be silently discarded.
  Planning requests fail with `NOT_EXECUTING`.
- **Commanded targets.** Captured at Begin as the targets left human-controlled by the mode (Mixed → the chosen side,
  Human → friendly, AvA → none). Only those can be assigned/rebriefed; they stay commanded after handover to an agent.

## Code under test

| File | Role |
|------|------|
| `src/ProjectAegis.Delegation/Orchestration/LoopPolicyGate.cs` | `CanRebriefAgent` (new, additive) |
| `src/ProjectAegis.Delegation/Orchestration/DelegationOrchestrator.cs` | `TryAssignAgentController` → `AssignAgentToTarget` + `ControllerChangeRecord`; refuses `pending-human-orders` while a human queue is nonempty. `TryRebriefAgent` → traits + slug + `PersonalityCatalog.ResolveAttentionBudget` + `PolicyUpdateRecord` field `personality.rebrief` |
| `src/ProjectAegis.Delegation.UnityAdapter/PlayEntry/PlayEntrySession.cs` | `CommandedTargets` set at Begin, cleared on load / reset |
| `src/ProjectAegis.Delegation.UnityAdapter/PlayEntry/PlayEntryContracts.cs` | `PlayCommandedTarget` record |
| `src/ProjectAegis.Delegation.UnityAdapter/PlayDelegation/PlayDelegationCommands.cs` | `TryAssignAgent` / `TryRebriefAgent` command façade returning `PlayEntryResult` |
| `src/ProjectAegis.Delegation.UnityAdapter/PlayDelegation/PlayDelegationErrorCodes.cs` | `NOT_EXECUTING`, `TARGET_NOT_COMMANDED`, `UNKNOWN_PRESET`, `ASSIGN_DENIED`, `NO_AGENT`, `REBRIEF_DENIED` |
| `src/ProjectAegis.Delegation.UnityAdapter/PlayDelegation/AssignAgentPanelPresentation.cs` | Read-only panel rows: controller kind, agent id, preset, autonomy, `CanAssign`, `CanRebrief`, block reason |

Loggable reasons: assign → `CONTROLLER` message-log line `Controller <id>: Human → Agent`; rebrief success →
`POLICY_UPDATE` line `Policy personality.rebrief: <old> → <new>` plus the command result message; denial → the gate
`DenialReason` in the `REBRIEF_DENIED` / `ASSIGN_DENIED` result message (no log write, no mutation).

## Tests

| Row | Acceptance | Tests |
|-----|------------|-------|
| S124-06 gate | Rebrief matrix | `LoopPolicyGateTests.CanRebriefAgent_matrix` (×5), `CanRebriefAgent_defaults_to_allow_without_scenario_policy` |
| S124-05 core | Assign via façade, logged; denials non-mutating | `AgentAssignRebriefTests`: `TryAssignAgentController_replaces_human_and_logs_controller_change`, `_assigns_to_group_targets`, `_denies_when_agent_already_active_without_mutation`, `_denies_unregistered_target_and_replay_viewer`, `_denies_while_unit_is_under_direct_control_with_suspended_agent` |
| S124-06 core | Success beyond deny-only; denial preserved | `AgentAssignRebriefTests`: `TryRebriefAgent_succeeds_under_tieredRebrief_at_semi_autonomous_where_rebind_is_denied`, `_denied_under_planningOnly_while_executing_without_mutation_or_log`, `_denies_same_preset_as_no_change`, `_denied_while_replay_viewer_attached`; existing `SimulationSessionPhaseTests.TryRebindAgentTraits_*` unchanged and green |
| S124-05 adapter | Assign from S123 path; failed command preserves state | `PlayDelegationCommandsTests`: `Begin_captures_*` (×2), `Agent_vs_agent_has_no_commanded_targets_and_assign_is_refused`, `Reset_clears_commanded_targets`, `Assign_agent_after_begin_installs_preset_agent_and_logs_controller_change`, `Assign_agent_in_planning_is_refused_because_begin_configures_controllers`, `Assign_without_package_is_refused`, `Assign_to_opposing_target_when_commanding_friendly_is_refused_without_mutation`, `Assign_with_unknown_preset_is_refused_without_mutation`, `Second_assign_is_denied_by_the_orchestrator_and_preserves_the_first_agent` |
| S124-06 adapter | Rebrief success reflected + reason; denial path | `PlayDelegationCommandsTests`: `Rebrief_success_under_tiered_rebrief_updates_agent_and_logs_reason`, `Rebrief_denied_under_planning_only_reports_reason_and_preserves_state`, `Rebrief_of_human_controlled_target_is_refused` |
| Panel model | Projection only | `AssignAgentPanelPresentationTests` (×5) incl. `Projection_does_not_mutate_controller_state_or_log` |
| Integration | S123 golden path → assign → rebrief → one tick | `PlayDelegationGoldenPathTests`: `Golden_path_begin_assign_rebrief_one_tick` (friendly agent reaches the decision pipeline), `Golden_path_with_assign_and_rebrief_is_deterministic` (order-log SHA-256 equal across two runs), `Golden_path_leaves_baltic_replay_hash_invariant_untouched` (`BalticReplayHarness.Run(42, "baltic-patrol", 4).WorldHash == 17144800277401907079`) |

The TieredRebrief / PlanningOnly adapter tests set `Bridge.Orchestrator.ScenarioPolicy` on the test fixture, because no
shipped scenario policy uses those modes (Baltic `baltic-patrol-catalog` defaults to `Anytime`).

## Commands and results (agent run, 2026-10-07, branch tip `1cbe81bb`)

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj -m:1 \
  --filter "FullyQualifiedName~AgentAssignRebriefTests|FullyQualifiedName~LoopPolicyGateTests|FullyQualifiedName~SimulationSessionPhaseTests"
# Passed! Failed: 0, Passed: 30, Total: 30

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj -m:1 \
  --filter "FullyQualifiedName~ProjectAegis.Delegation.UnityAdapter.Tests.PlayDelegation"
# Passed! Failed: 0, Passed: 22, Total: 22

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj -m:1 \
  --filter "FullyQualifiedName~ProjectAegis.Delegation.UnityAdapter.Tests.PlayEntry"
# Passed! Failed: 0, Passed: 54, Total: 54   (includes S123 null/missing metadata SCHEMA_ERROR)

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj -m:1 \
  --filter PlayModeSmokeHarnessTests
# Passed! Failed: 0, Passed: 25, Total: 25

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj -m:1 -v minimal
# Passed! Failed: 0, Passed: 927, Total: 927

dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj -m:1 -v minimal
# Failed: 1, Passed: 1125, Total: 1126 — the one failure is environmental, see below

dotnet build src/ProjectAegis.Delegation.UnityAdapter/ProjectAegis.Delegation.UnityAdapter.csproj -m:1 -f netstandard2.1
# 0 Warning(s), 0 Error(s)
```

ReplayGolden filters: `ProjectAegis.Delegation.Tests` 4/4, `ProjectAegis.Delegation.UnityAdapter.Tests` 17/17.
Environmental failure: `Unity_plugin_Delegation_dll_exports_Epic_A_types_used_by_Runtime_hosts` requires
`unity/ProjectAegis/Assets/Plugins/ProjectAegis/ProjectAegis.Delegation.dll`, which is gitignored
(`.gitignore:33`) and absent from both this worktree and the main checkout until `tools/copy-delegation-assemblies.ps1` runs.
Full-solution gates are run serially by the orchestrator and are not claimed here.

## Invariants

- `DelegationBridge.cs`, `SimulationSession.cs`, `src/ProjectAegis.Data/` (incl. `CatalogWriteGate`), `tests/` goldens: zero diff vs `71f57b98`.
- Baltic v2 hash `17144800277401907079` still pinned in `tests/regression/replay-golden-baltic-*.txt` and reproduced by the integration test.
- New order-log rows (`ControllerChange`, `PolicyUpdate personality.rebrief`) are written only when a player issues the command; no existing replay path issues them.

## Known gaps

- **Unity host / Editor evidence.** No UXML / MonoBehaviour binds `AssignAgentPanelPresentation` or `PlayDelegationCommands` yet; the QA plan's Editor evidence for this row is outstanding.
- **Rebrief sim-time cost.** Not implemented (spec marks it a future scenario-policy field).
- **Unassign / hand back to human.** Out of scope; existing `TryTakeDirectControl` / `TryReleaseDirectControl` cover takeover of an agent-controlled unit.
- **Autonomy change on rebrief.** Rebrief swaps the personality preset only; autonomy stays as assigned (`CanEditAutonomy` is always-allow and unchanged).
- **Hindsight personality re-registration.** Follow-up (Codex P2). `AssignAgentToTarget` registers the slug on `HindsightOrderLogHook`; `TryRebriefAgent` changes `PersonalitySlug` without calling `RegisterAgent` again, so later decisions stay on the previous personality bank when Hindsight is enabled. The hook's map is private, so this was left as a follow-up rather than an untested sidecar change.
