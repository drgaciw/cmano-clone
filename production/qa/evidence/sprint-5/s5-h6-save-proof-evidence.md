# Sprint 5 H6 Save/Resume Proof Evidence (`DRG-349`)

- **Story:** `DRG-349` (`[H6-SAVE-SLICE] Prove deterministic same-build save and resume before multiplayer`)
- **Branch:** `sprint/s5-h6-save-resume` (stacked on `sprint/s4-h4-editors-g3` @ `618e5e46`, Track A @ `f7267cde` (`f7267cde0705fa58fd375ad151f32d214b893c77`), Track B @ `ef2fd8fc` (`ef2fd8fcbf1d509ecf5626caf1dead2177154e79`))
- **Contract & Architecture:** `docs/architecture/adr-029-h6-save-resume.md` (Option B, Waiver W0), `docs/engineering/h6-save-resume-contract.md`
- **Timestamp (UTC):** `2026-11-23T14:29:44Z`

---

## 1. Differential Proof Matrix (`F10` — `N+M` Continuous vs `N` Save/Restore + `M` Resumed)

All `14` differential and rollback cases in `src/ProjectAegis.Delegation.UnityAdapter.Tests/Save/SaveResumeDifferentialProofTests.cs` (verified via `SaveDifferentialAssert`) compare uninterrupted `N+M` execution against save at `N` + isolated tick-0 restore + `M` resumed ticks across:
- Clocks & hashes (`ObservedTick`, `PipelineTick`, `LastSimWorldHash`, `DetectionWorldHash`, `CombinedWorldHash`)
- `SimulationSession` (`TimeAccelerationFactor`, `IsSimPaused`, `LastWatchPauseReason`, `NextEngageSalvoOverride`, `Magazines.Snapshot()`, `KilledTargets`, `AirOps.Snapshot()`, `BoatOps.Snapshot()`, `WatchQueue.Cards`)
- `DecisionLog` (`OrderLogReplayFingerprint.ComputeSha256Hex`, `ComputeFingerprint()`, chronological entry-by-entry `SequenceId`/`Kind`/`SimTime`/`Payload` parity)
- `ReplayCheckpointStore` boundaries (`SimTick`, `WorldHash`, `LogFingerprint`, `LastSequenceId`)
- `ReplayableSimWorld` / `IOrderSink` side effects (`AppliedOrders`, `EmconPostures`, `SensorPostures`, `EwPostures`, `CraftOpsCounts`, `ComputeSideEffectsDigestSha256()`)
- `PendingApprovalQueue` & controller/agent state (`Active` vs `SuspendedAgent`, `Traits`, `Autonomy`, `AttentionBudget`, `PersonalitySlug`, `IsActionsPaused`, `PolicySnapshotId`, `EffectivePolicy`)

| # | Test Method | Scenario / Seed | `N` / `M` | Coverage | Result |
|---|---|---|---|---|---|
| 1 | `Differential_interruption_1_before_pending_approval_and_after_approve_reject` | `baltic-patrol` (`42`) | `3 / 4` | Interruption 1: save before pending approval (`PendingApprovalQueue`) + approve in `M`, and reject+approve before save | Passed (`76 ms`) |
| 2 | `Differential_interruption_2_delayed_comms_order_in_flight` | `baltic-patrol-comms` (`42`) | `3 / 4` | Interruption 2: `CommsState.Degraded` delayed `Move` order in flight at save (`N=3`), dispatched at tick `4` in `M` | Passed (`49 ms`) |
| 3 | `Differential_interruption_3_active_engagement_and_magazine_depletion` | `baltic-patrol` (`42`) | `2 / 4` | Interruption 3: mid-engagement save with `MagazineLedger` depletion & `EngagementRecord` | Passed (`38 ms`) |
| 4 | `Differential_time_acceleration_2x_4x_8x_observed_vs_pipeline_tick` | `baltic-patrol` (`77`) | `6 / 4` | Accelerated clocks `2x` -> `4x` -> `8x` (`ObservedTick=10`, `PipelineTick=41`) | Passed (`35 ms`) |
| 5 | `Differential_same_tick_multiple_commands_preserve_sequence_order` | `baltic-v3-patrol` (`42`) | `4 / 4` | 5 commands issued at same `ObservedTick == 2` (`SequenceOrder == 0..4`) | Passed (`5 ms`) |
| 6 | `Differential_agent_rebind_rebrief_assign_and_doctrine_override` | `baltic-v3-patrol` (`42`) | `5 / 5` | `TryAssignAgentController` (`ucav-blue`), `TryRebriefAgent`, `TryRebindAgentTraits`, `DoctrineOverrideCommand.TryApply` | Passed (`229 ms`) |
| 7 | `Differential_direct_control_handoff_and_pause_resume_agent_actions` | `baltic-patrol` (`42`) | `3 / 5` | `TryPauseAgentActions` -> `TryTakeDirectControl` -> save/restore -> `TryReleaseDirectControl` -> `TryResumeAgentActions` | Passed (`44 ms`) |
| 8 | `Differential_pending_NextEngageSalvoOverride_survives_save_and_controls_next_salvo` | `baltic-patrol-salvo` (`42`) | `2 / 3` | Pending `NextEngageSalvoOverride == 1` via `TryEnqueueAttackOption("fire-single")` overrides scenario `salvoSize: 2` post-restore | Passed (`39 ms`) |
| 9 | `Differential_craft_ops_launch_aircraft_launch_boat_and_recover_boat` | `baltic-patrol` (`42`) | `6 / 6` | `launch_aircraft`, `launch_boat`, and post-restore `recover_boat` (`AirOps`, `BoatOps`, `CraftOpsCounts == 3`) | Passed (`81 ms`) |
| 10 | `Differential_contact_override_and_play_delegation_personality_autonomy` | `baltic-v3-patrol` (`42`) | `5 / 5` | `TrySetAgentAutonomy`, `TrySetAgentPersonality`, `TryReclassifyContact`, post-restore `TryDropContact` | Passed (`17 ms`) |
| 11 | `Differential_watch_auto_pause_gate_ack_dismiss_clearance_and_second_gen_save` | `baltic-v3-patrol` (`42`) | `3 / 3 + 2` | `WatchAutoPauseGate` (`TryResumeSim(false)` blocked until re-ack/dismiss on both Gen 1 and Gen 2), `IsAcknowledged=false`/`IsDismissed=false` on load, 2nd-gen save/restore | Passed (`27 ms`) |
| 12 | `Differential_omitted_state_only_or_unknown_journal_command_fails_and_rolls_back` | `baltic-v3-patrol` / `baltic-patrol` (`42`) | `4` / `2` | Unknown command (`ReplayCommandFailed`), omitted state-only row preserving digest (`IntegrityMismatch`), forged omission of `SetTimeAccelerationFactor` / `TryApprovePendingOrder` (`ReplayMismatch`), zero host mutation | Passed (`45 ms`) |
| 13 | `Rollback_corrupt_truncated_incompatible_envelope_and_slot_store_leave_active_session_and_slots_unchanged` | `gauntlet-t1-patrol-a` (`42`) | `4` | Truncated JSON, wrong `schemaVersion`, `"lockstep": true`, tampered hash, build/catalog/scenario/policy/seed mismatch, missing slot — active host, `BalticV3SideRegistry`, `manual-1` slot bytes, and `baltic_patrol.db` SHA-256 unchanged | Passed (`17 ms`) |
| 14 | `Differential_real_sqlite_catalog_gauntlet_orbat_parity` | `gauntlet-t1-patrol-a` (`42`) | `6 / 4` | Real SQLite `assets/data/catalog/baltic_patrol.db` (`layerVersion: "harness-catalog"`), multi-unit `Gauntlet.Units`, `SaveSlotStore` `manual-2`/`autosave`, DB SHA-256 invariant | Passed (`41 ms`) |

---

## 2. Restore Latency Benchmark (`F11` — `~30s` Ceiling Compliance)

- **Test Method:** `Restore_latency_benchmark_across_tick_depths_and_accelerations_stays_under_30s_ceiling`
- **Command:** `export PATH="$HOME/.dotnet:$PATH" && dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --no-build --filter "FullyQualifiedName~SaveResumeDifferentialProofTests" --logger "console;verbosity=normal" 2>&1`
- **Setup:** `baltic-patrol` (`seed: 42`), representative journal commands (`SetTimeAccelerationFactor`, `TryTakeDirectControl`, `TryEnqueueHumanOrder(Move)`, `TryReleaseDirectControl`, `TryApplyDoctrineOverride`), 1 warmup restore + `3` measured `Stopwatch` iterations per `(T, accel)` cell covering full `SaveResumeDriver.TryRestore(host, saveBytes)`.
- **Track A Defect Caught & Fixed by Benchmark:** Initial run at `T=360` failed with `Save payload size 5107498 exceeds limit 4194304` because `checkpointRows` stores raw `DecisionLog.ComputeFingerprint()` (`h6-save-resume-contract.md` L337), causing envelope JSON to grow quadratically (`5.11 MB` at `T=360`, `14.24 MB` at `T=600`). Raised `SaveEnvelopeBounds.MaxPayloadBytes` (`4 MiB -> 64 MiB`) and `MaxRawFingerprintLength` (`512 KiB -> 2 MiB`) in `src/ProjectAegis.Delegation.UnityAdapter/Save/SaveEnvelopeSerializer.cs` (`0` edits to `DelegationBridge.cs`, `SimulationSession.cs`, or any Hard-No file).

| Ticks (Observed) | Accel | PipelineTick | Payload Bytes | Min (ms) | Median (ms) | Max (ms) | Max (s) | Ceiling (<30.0s) |
|---|---|---|---|---|---|---|---|---|
| 60 | 1x | 60 | 151720 | 16.25 | 20.45 | 26.97 | 0.0270 | PASS |
| 180 | 1x | 180 | 1282313 | 136.81 | 154.13 | 155.36 | 0.1554 | PASS |
| 360 | 1x | 360 | 5107498 | 215.93 | 264.39 | 277.54 | 0.2775 | PASS |
| 600 | 1x | 600 | 14239773 | 511.65 | 544.39 | 545.70 | 0.5457 | PASS |
| 60 | 4x | 240 | 152219 | 11.67 | 14.25 | 17.57 | 0.0176 | PASS |
| 180 | 4x | 720 | 1282814 | 59.37 | 61.76 | 69.67 | 0.0697 | PASS |
| 360 | 4x | 1440 | 5107985 | 182.66 | 190.18 | 225.44 | 0.2254 | PASS |
| 600 | 4x | 2400 | 14240259 | 490.29 | 495.24 | 512.65 | 0.5127 | PASS |

**Peak measured restore latency:** `0.5457s` (`545.70 ms`) at `T = 600` (`1x`, `PipelineTick = 600`, `14.24 MB` envelope; `4x` peak `0.5127s` / `512.65 ms`) — **55x faster** than the `30.0s` ceiling.

---

## 3. Plugin Assembly Sync & Full Headless Gates (`F12`, `F13`)

- **Plugin Sync (`bash tools/copy-delegation-assemblies.sh`):**
  - Published `ProjectAegis.Delegation.UnityAdapter.csproj -c Release -f netstandard2.1` (`0` errors, `0` warnings).
  - Synced `15` DLLs + `libe_sqlite3.so` to `unity/ProjectAegis/Assets/Plugins/ProjectAegis/` (`git status --short unity/ProjectAegis/Assets/Plugins/ProjectAegis/` clean).
- **Headless Build & Tests (`export PATH="$HOME/.dotnet:$PATH"`):**
  - `dotnet build ProjectAegis.sln --no-restore -v minimal` -> **0 Warning(s), 0 Error(s)**
  - `dotnet test ProjectAegis.sln -v minimal --no-build` -> **4,987 / 4,987 Passed** (`0` Failed, `0` Skipped; `>= 4,938` floor):
    - `ProjectAegis.Ai.Contracts.Tests`: `18 / 18`
    - `ProjectAegis.Delegation.Demo.Tests`: `15 / 15`
    - `ProjectAegis.Sim.Tests`: `653 / 653`
    - `ProjectAegis.Ai.Host.Tests`: `88 / 88`
    - `ProjectAegis.Delegation.Tests`: `1,543 / 1,543`
    - `ProjectAegis.Data.Excel.Tests`: `24 / 24`
    - `ProjectAegis.Delegation.UnityAdapter.Tests`: `1,501 / 1,501`
    - `ProjectAegis.Data.Tests`: `974 / 974`
    - `ProjectAegis.MissionEditor.Cli.Tests`: `171 / 171`
  - `ReplayGolden` -> **17 / 17 Passed**
  - `PlayModeSmokeHarnessTests` -> **27 / 27 Passed** (`>= 27/27`)
  - Architecture Fences (`UnitySimImportForbidTests|NamespaceSccBaselineTests|CsprojReferenceGraphPinTests`) -> **31 / 31 Passed**
  - `grep -rn "17144800277401907079" tests/ data/ | wc -l` -> **`26`** (Baltic v2 golden hash preserved)
  - `git diff 618e5e46` across `DelegationBridge.cs`, `SimulationSession.cs`, `WriteGate/`, `MapPlaceholderPanelHost.cs`, `GlobeMapProductHost.cs`, `tests/regression/`, `DelegationSmoke.unity`, `assets/data/catalog/` -> **0 lines**

---

## 4. Live Unity `:8081` PlayMode Verification (`F13`)

- **Endpoint:** `http://127.0.0.1:8081` (`gamedev-mcp-server 9.6.2.0`, Unity `6000.3.27f2`)
- **Asset Refresh (`rql-mutate`: `editor | call RefreshAssets()`):** `refreshed`, waited until `editor | select isCompiling isUpdating` -> `false|false`.
- **PlayMode Execution (`tests-run`, `testMode: "PlayMode"`, `testAssembly: "ProjectAegis.Unity.Tests"`):**
  ```json
  {
    "Status": "Passed",
    "TotalTests": 61,
    "PassedTests": 61,
    "FailedTests": 0,
    "SkippedTests": 0,
    "Duration": "00:00:05.3880070"
  }
  ```
- **Post-Run RQL Checks:**
  - `testResults | group-by status` -> `Passed|61`
  - `testResults | where status != "Passed"` -> `0 results`
  - `console | where logType == "Error" or logType == "Exception"` -> `0 results`
  - `editor | select isPlaying isPaused isCompiling isUpdating` -> `false|false|false|false` (clean Edit Mode)
  - `scenes | select name path isDirty` -> `""|""|false`
