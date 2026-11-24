# Sprint 5 C8 / H5 — Consolidated RED->GREEN TDD, Architecture Fence & Save/Resume Evidence

**Branch:** `sprint/s5-h6-save-resume` (stacked on `sprint/s4-h4-editors-g3` @ `618e5e46`, Track A @ `f7267cde`, Track B @ `ef2fd8fc`, 2026-11-23)
**Active Sprint 5 Story:** `DRG-349` (`[H6-SAVE-SLICE] Prove deterministic same-build save and resume before multiplayer`)

---

## 1. Track A — Verbatim RED Evidence (`DRG-349` Core Driver, Envelope, Journal & Full Catalog Identity)

- **Timestamp (UTC):** `2026-11-23T12:56:07Z`
- **Working Directory:** `/home/username01/cmano-clone`
- **Branch / Base:** `sprint/s5-h6-save-resume` @ `618e5e46`
- **RED Test Files Created / Modified:**
  - `src/ProjectAegis.Data.Tests/Snapshots/CatalogFullSnapshotHasherTests.cs`
  - `src/ProjectAegis.Delegation.Tests/Input/InputCommandInventoryTests.cs`
  - `src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/BalticReplayStepperTests.cs`
  - `src/ProjectAegis.Delegation.UnityAdapter.Tests/Save/SessionCommandFacadeInventoryTests.cs`
  - `src/ProjectAegis.Delegation.UnityAdapter.Tests/Save/SaveEnvelopeAndSlotStoreTests.cs`
  - `src/ProjectAegis.Delegation.UnityAdapter.Tests/Save/SaveResumeDriverTests.cs`
- **Command 1:** `date -u +"%Y-%m-%dT%H:%M:%SZ" && export PATH="$HOME/.dotnet:$PATH" && dotnet build ProjectAegis.sln --no-restore 2>&1`

```text
2026-11-23T12:56:07Z
  ProjectAegis.Ai.Contracts -> /home/username01/cmano-clone/src/ProjectAegis.Ai.Contracts/bin/Debug/netstandard2.1/ProjectAegis.Ai.Contracts.dll
  ProjectAegis.Data -> /home/username01/cmano-clone/src/ProjectAegis.Data/bin/Debug/netstandard2.1/ProjectAegis.Data.dll
  ProjectAegis.Ai.Contracts -> /home/username01/cmano-clone/src/ProjectAegis.Ai.Contracts/bin/Debug/net11.0/ProjectAegis.Ai.Contracts.dll
  ProjectAegis.Data -> /home/username01/cmano-clone/src/ProjectAegis.Data/bin/Debug/net11.0/ProjectAegis.Data.dll
  ProjectAegis.Ai.Contracts.Tests -> /home/username01/cmano-clone/src/ProjectAegis.Ai.Contracts/test/bin/Debug/net11.0/ProjectAegis.Ai.Contracts.Tests.dll
  ProjectAegis.Sim -> /home/username01/cmano-clone/src/ProjectAegis.Sim/bin/Debug/netstandard2.1/ProjectAegis.Sim.dll
  ProjectAegis.Data.Excel -> /home/username01/cmano-clone/src/ProjectAegis.Data.Excel/bin/Debug/net11.0/ProjectAegis.Data.Excel.dll
  ProjectAegis.Sim -> /home/username01/cmano-clone/src/ProjectAegis.Sim/bin/Debug/net11.0/ProjectAegis.Sim.dll
  ProjectAegis.Sim.Benchmark -> /home/username01/cmano-clone/src/ProjectAegis.Sim.Benchmark/bin/Debug/net11.0/ProjectAegis.Sim.Benchmark.dll
  ProjectAegis.Data.Excel.Tests -> /home/username01/cmano-clone/src/ProjectAegis.Data.Excel.Tests/bin/Debug/net11.0/ProjectAegis.Data.Excel.Tests.dll
  ProjectAegis.Delegation -> /home/username01/cmano-clone/src/ProjectAegis.Delegation/bin/Debug/net11.0/ProjectAegis.Delegation.dll
  ProjectAegis.Delegation -> /home/username01/cmano-clone/src/ProjectAegis.Delegation/bin/Debug/netstandard2.1/ProjectAegis.Delegation.dll
  ProjectAegis.Sim.Tests -> /home/username01/cmano-clone/src/ProjectAegis.Sim.Tests/bin/Debug/net11.0/ProjectAegis.Sim.Tests.dll
  ProjectAegis.Delegation.UnityAdapter -> /home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter/bin/Debug/net11.0/ProjectAegis.Delegation.UnityAdapter.dll
  ProjectAegis.Ai.Host -> /home/username01/cmano-clone/src/ProjectAegis.Ai.Host/bin/Debug/net11.0/ProjectAegis.Ai.Host.dll
  ProjectAegis.Ai.Host.Tests -> /home/username01/cmano-clone/src/ProjectAegis.Ai.Host.Tests/bin/Debug/net11.0/ProjectAegis.Ai.Host.Tests.dll
  ProjectAegis.MissionEditor.Cli -> /home/username01/cmano-clone/src/ProjectAegis.MissionEditor.Cli/bin/Debug/net11.0/ProjectAegis.MissionEditor.Cli.dll
  ProjectAegis.Delegation.Demo -> /home/username01/cmano-clone/src/ProjectAegis.Delegation.Demo/bin/Debug/net11.0/ProjectAegis.Delegation.Demo.dll
  ProjectAegis.MissionEditor.Cli.Tests -> /home/username01/cmano-clone/src/ProjectAegis.MissionEditor.Cli.Tests/bin/Debug/net11.0/ProjectAegis.MissionEditor.Cli.Tests.csproj
  ProjectAegis.Delegation.Demo.Tests -> /home/username01/cmano-clone/src/ProjectAegis.Delegation.Demo.Tests/bin/Debug/net11.0/ProjectAegis.Delegation.Demo.Tests.dll
/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/Save/SaveEnvelopeAndSlotStoreTests.cs(9,44): error CS0234: The type or namespace name 'Save' does not exist in the namespace 'ProjectAegis.Delegation.UnityAdapter' (are you missing an assembly reference?) [/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj]
/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/Save/SaveResumeDriverTests.cs(13,44): error CS0234: The type or namespace name 'Save' does not exist in the namespace 'ProjectAegis.Delegation.UnityAdapter' (are you missing an assembly reference?) [/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj]
/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/Save/SessionCommandFacadeInventoryTests.cs(17,44): error CS0234: The type or namespace name 'Save' does not exist in the namespace 'ProjectAegis.Delegation.UnityAdapter' (are you missing an assembly reference?) [/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj]
/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/Save/SaveEnvelopeAndSlotStoreTests.cs(213,20): error CS0246: The type or namespace name 'SaveEnvelope' could not be found (are you missing a using directive or an assembly reference?) [/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj]
/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/Save/SaveResumeDriverTests.cs(249,16): error CS0246: The type or namespace name 'SaveResumeSessionUnit' could not be found (are you missing a using directive or an assembly reference?) [/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj]
/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/Save/SaveResumeDriverTests.cs(245,21): error CS0246: The type or namespace name 'HeadlessSaveResumeHost' could not be found (are you missing a using directive or an assembly reference?) [/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj]
/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/Save/SessionCommandFacadeInventoryTests.cs(283,46): error CS0246: The type or namespace name 'ReplayableSimWorld' could not be found (are you missing a using directive or an assembly reference?) [/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj]
/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/Save/SessionCommandFacadeInventoryTests.cs(283,72): error CS0246: The type or namespace name 'SessionCommandFacade' could not be found (are you missing a using directive or an assembly reference?) [/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj]
/home/username01/cmano-clone/src/ProjectAegis.Data.Tests/Snapshots/CatalogFullSnapshotHasherTests.cs(18,36): error CS1061: 'SqliteCatalogReader' does not contain a definition for 'GetSortedWeapons' ...
/home/username01/cmano-clone/src/ProjectAegis.Data.Tests/Snapshots/CatalogFullSnapshotHasherTests.cs(30,13): error CS1739: The best overload for 'InMemoryCatalogReader' does not have a parameter named 'weapons' ...
/home/username01/cmano-clone/src/ProjectAegis.Data.Tests/Snapshots/CatalogFullSnapshotHasherTests.cs(35,32): error CS1061: 'InMemoryCatalogReader' does not contain a definition for 'GetSortedWeapons' ...
/home/username01/cmano-clone/src/ProjectAegis.Data.Tests/Snapshots/CatalogFullSnapshotHasherTests.cs(44,38): error CS1061: 'SqliteCatalogReader' does not contain a definition for 'GetSortedPlatforms' ...
/home/username01/cmano-clone/src/ProjectAegis.Data.Tests/Snapshots/CatalogFullSnapshotHasherTests.cs(60,34): error CS1061: 'InMemoryCatalogReader' does not contain a definition for 'GetSortedPlatforms' ...
/home/username01/cmano-clone/src/ProjectAegis.Data.Tests/Snapshots/CatalogFullSnapshotHasherTests.cs(71,27): error CS0103: The name 'CatalogFullSnapshotHasher' does not exist in the current context ...
/home/username01/cmano-clone/src/ProjectAegis.Delegation.Tests/Input/InputCommandInventoryTests.cs(81,35): error CS0117: 'InputCommandInventory' does not contain a definition for 'RebriefAgentId' ...
/home/username01/cmano-clone/src/ProjectAegis.Delegation.Tests/Input/InputCommandInventoryTests.cs(108,37): error CS0117: 'InputCommandJournalKind' does not contain a definition for 'AcknowledgeWatchAttention' ...

Build FAILED.
    0 Warning(s)
    46 Error(s)
```

---

## 2. Track A — GREEN Verification Evidence (`DRG-349` Core Driver, Envelope, Journal & Full Catalog Identity — Commit `f7267cde`)

- **Timestamp (UTC):** `2026-11-23T13:05:28Z`
- **Production Files Modified / Created:**
  - `src/ProjectAegis.Data/Catalog/ICatalogReader.cs` (added default interface methods `GetSortedWeapons()`, `GetSortedWeaponsCore()`, `GetSortedPlatforms()`, `GetSortedPlatformsCore()`)
  - `src/ProjectAegis.Data/Catalog/SqliteCatalogReader.cs` (implemented `GetSortedWeapons()` over 6-column `weapon_catalog` and `GetSortedPlatforms()` enriched with `_platformDomains`)
  - `src/ProjectAegis.Data/Catalog/InMemoryCatalogReader.cs` (added optional `weapons` ctor parameter, `GetSortedWeapons()`, `GetSortedPlatforms()`, and weapon envelope lookup)
  - `src/ProjectAegis.Data/Catalog/CatalogStagingOverlayReader.cs` (forwarded `GetSortedWeapons()` and `GetSortedPlatforms()`)
  - `src/ProjectAegis.Data/Snapshots/CatalogFullSnapshotHasher.cs` (deterministic order-independent 13-domain SHA-256 + `IsValidNonEmptySha256Hex`, `AreCompatibleHashes`, `TryValidateBoundSnapshot`; `CatalogSnapshotHasher.cs` preserved untouched)
  - `src/ProjectAegis.Delegation/Input/InputCommandInventory.cs` (kept `0..14` unchanged; added `RebriefAgent = 15`..`SetAgentAutonomy = 21` for 22 total registered commands)
  - `src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticReplayStepper.cs` (extracted setup + `StepOneObservedTick()` + `BuildResult()` from `BalticReplayHarness.RunCore`)
  - `src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticReplayHarness.cs` (`RunCore` delegates 1:1 to `BalticReplayStepper`)
  - `src/ProjectAegis.Delegation.UnityAdapter/Save/SaveEnvelope.cs` (4-block versioned envelope DTOs, `SaveFailureCode`, `SaveIdentityContext`)
  - `src/ProjectAegis.Delegation.UnityAdapter/Save/SaveEnvelopeSerializer.cs` (canonical `Utf8JsonWriter` serialization, sequential `JournalDigest`, `EnvelopeHashSha256`, strict `JsonDocument` parser rejecting truncated/unknown-version/missing-clock/duplicate/network/oversized/tampered payloads)
  - `src/ProjectAegis.Delegation.UnityAdapter/Save/SessionInputJournal.cs` (ordered monotonic journal + sequential SHA-256 digest)
  - `src/ProjectAegis.Delegation.UnityAdapter/Save/SessionCommandFacade.cs` (`[SessionCommand]` 22-command façade + `AuthorityShadowState` unjournaled-mutation detector)
  - `src/ProjectAegis.Delegation.UnityAdapter/Save/SaveSlotStore.cs` (`manual-1..3` + `autosave` atomic write via `FileOptions.WriteThrough` + `Flush(true)` + `File.Move`/`File.Replace`, non-destructive load)
  - `src/ProjectAegis.Delegation.UnityAdapter/Save/ReplayableSimWorld.cs` (live `ISimWorldSnapshot` + side-effect-applying `IOrderSink` using `PlayModeKinematicMover`, posture maps, craft ops counters, and `AppliedOrders`)
  - `src/ProjectAegis.Delegation.UnityAdapter/Save/SaveResumeDriver.cs` (9-step pre-validation -> isolated candidate tick-0 replay -> checksum verification -> `WatchAttentionQueue.Clear()` + `Enqueue()` & `PauseSim()` -> atomic swap -> projection publish, with `BalticV3SideRegistry` state guard)
- **Build & `netstandard2.1` Publish Results (`2026-11-23T13:04:21Z`):**
  ```text
  Build succeeded. (ProjectAegis.sln)
      0 Warning(s)
      0 Error(s)
  Build succeeded. (ProjectAegis.Data -c Release -f netstandard2.1)
      0 Warning(s)
      0 Error(s)
  Build succeeded. (ProjectAegis.Delegation -c Release -f netstandard2.1)
      0 Warning(s)
      0 Error(s)
  ProjectAegis.Delegation.UnityAdapter -> .../bin/Release/netstandard2.1/publish/ (0 Warning(s), 0 Error(s))
  ```
- **Targeted & Full Solution Test Results (`2026-11-23T13:03:53Z`–`2026-11-23T13:05:28Z`):**
  ```text
  Architecture fences (UnitySimImportForbidTests|NamespaceSccBaselineTests|CsprojReferenceGraphPinTests): Passed 31/31
  Data.Tests Track A filter (CatalogFullSnapshotHasherTests|CatalogSnapshot|DbSnapshotStoreTests): Passed 9/9
  Delegation.Tests Track A filter (InputCommandInventoryTests|WatchAttentionQueueTests): Passed 8/8
  UnityAdapter.Tests Track A filter (ReplayGolden|PlayModeSmokeHarnessTests|Save|BalticReplayStepper): Passed 68/68
    - ReplayGolden: 17/17 (v2 WORLD_HASH=17144800277401907079 preserved)
    - PlayModeSmokeHarnessTests: 27/27

  Full Solution (dotnet test ProjectAegis.sln -v minimal --no-build):
    Passed! - Failed: 0, Passed:   18, Skipped: 0, Total:   18 - ProjectAegis.Ai.Contracts.Tests.dll (net11.0)
    Passed! - Failed: 0, Passed:   15, Skipped: 0, Total:   15 - ProjectAegis.Delegation.Demo.Tests.dll (net11.0)
    Passed! - Failed: 0, Passed:   88, Skipped: 0, Total:   88 - ProjectAegis.Ai.Host.Tests.dll (net11.0)
    Passed! - Failed: 0, Passed:  653, Skipped: 0, Total:  653 - ProjectAegis.Sim.Tests.dll (net11.0)
    Passed! - Failed: 0, Passed: 1543, Skipped: 0, Total: 1543 - ProjectAegis.Delegation.Tests.dll (net11.0)
    Passed! - Failed: 0, Passed:   24, Skipped: 0, Total:   24 - ProjectAegis.Data.Excel.Tests.dll (net11.0)
    Passed! - Failed: 0, Passed: 1483, Skipped: 0, Total: 1483 - ProjectAegis.Delegation.UnityAdapter.Tests.dll (net11.0)
    Passed! - Failed: 0, Passed:  974, Skipped: 0, Total:  974 - ProjectAegis.Data.Tests.dll (net11.0)
    Passed! - Failed: 0, Passed:  171, Skipped: 0, Total:  171 - ProjectAegis.MissionEditor.Cli.Tests.dll (net11.0)
    Total: 4,969 / 4,969 passed (0 failed, 0 skipped)
  ```
- **Invariants Verified:**
  - `grep -rn "17144800277401907079" tests/ data/ | wc -l`: `26`
  - `git diff 618e5e46` across `DelegationBridge.cs`, `SimulationSession.cs`, `CatalogSnapshotHasher.cs`, `WriteGate/`, `DelegationBridgeHost.cs`, `MapPlaceholderPanelHost.cs`, `GlobeMapProductHost.cs`, `tests/regression/`, `DelegationSmoke.unity`, `assets/data/catalog/`, and `*.csproj`: empty (`0` diff).

---

## Iteration 2 RED -> GREEN (`DRG-349` Track A — `ReplayableSimWorld.ApplyOrder` Mutual Recursion Fix & `SessionCommandFacade` `combinedWorldHashProvider`)

### 1. Verbatim RED Evidence (2026-11-23T13:48:02Z / 2026-11-23T13:48:46Z)

- **RED Test Added First:** `Restore_and_resume_with_non_engage_orders_move_hold_set_emcon_matches_continuous_run` in `src/ProjectAegis.Delegation.UnityAdapter.Tests/Save/SaveResumeDriverTests.cs` (dispatching `OrderKind.Move`, `OrderKind.Hold` via `TryEnqueueAttackOption("hold-fire")`, and `OrderKind.SetEmcon` with `ReplayableSimWorld` bound to `BalticReplayStepper.Snapshot`).
- **Command 1 (Runtime `StackOverflowException` reproduction):**
  `date -u +"%Y-%m-%dT%H:%M:%SZ" && export PATH="$HOME/.dotnet:$PATH" && timeout 30s dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "FullyQualifiedName~Restore_and_resume_with_non_engage_orders_move_hold_set_emcon_matches_continuous_run" -v minimal 2>&1`

```text
2026-11-23T13:48:02Z
  Determining projects to restore...
  All projects are up-to-date for restore.
  ProjectAegis.Data -> /home/username01/cmano-clone/src/ProjectAegis.Data/bin/Debug/net11.0/ProjectAegis.Data.dll
  ProjectAegis.Ai.Contracts -> /home/username01/cmano-clone/src/ProjectAegis.Ai.Contracts/bin/Debug/net11.0/ProjectAegis.Ai.Contracts.dll
  ProjectAegis.Sim -> /home/username01/cmano-clone/src/ProjectAegis.Sim/bin/Debug/net11.0/ProjectAegis.Sim.dll
  ProjectAegis.Delegation -> /home/username01/cmano-clone/src/ProjectAegis.Delegation/bin/Debug/net11.0/ProjectAegis.Delegation.dll
  ProjectAegis.Delegation.UnityAdapter -> /home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter/bin/Debug/net11.0/ProjectAegis.Delegation.UnityAdapter.dll
  ProjectAegis.Delegation.UnityAdapter.Tests -> /home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/bin/Debug/net11.0/ProjectAegis.Delegation.UnityAdapter.Tests.dll
Test run for /home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/bin/Debug/net11.0/ProjectAegis.Delegation.UnityAdapter.Tests.dll (.NETCoreApp,Version=v11.0)
A total of 1 test files matched the specified pattern.
The active test run was aborted. Reason: Test host process crashed : Process path: /home/username01/.dotnet/dotnet
Stack overflow.
   at ProjectAegis.Delegation.UnityAdapter.Bridge.PlayModeKinematicMover.PlotCourseAhead(System.String)
Repeated 37417 times:
--------------------------------
   at ProjectAegis.Delegation.UnityAdapter.Save.ReplayableSimWorld.ApplyOrder(ProjectAegis.Delegation.UnityAdapter.Bridge.EntityKey, ProjectAegis.Delegation.Core.Order ByRef)
   at ProjectAegis.Delegation.UnityAdapter.Save.ReplayableSimWorld.ProjectAegis.Delegation.UnityAdapter.Bridge.IOrderSink.ApplyOrder(ProjectAegis.Delegation.UnityAdapter.Bridge.EntityKey, ProjectAegis.Delegation.Core.Order ByRef)
   at ProjectAegis.Delegation.UnityAdapter.Baltic.BalticReplayStepper+BalticHeadlessSnapshot.ApplyOrder(ProjectAegis.Delegation.UnityAdapter.Bridge.EntityKey, ProjectAegis.Delegation.Core.Order ByRef)
   at ProjectAegis.Delegation.UnityAdapter.Baltic.BalticReplayStepper+BalticHeadlessSnapshot.ProjectAegis.Delegation.UnityAdapter.Bridge.IOrderSink.ApplyOrder(ProjectAegis.Delegation.UnityAdapter.Bridge.EntityKey, ProjectAegis.Delegation.Core.Order ByRef)
--------------------------------
   at ProjectAegis.Delegation.UnityAdapter.Bridge.OrderDispatcher.Dispatch(System.Collections.Generic.IReadOnlyList`1<ProjectAegis.Delegation.Core.Order>, ProjectAegis.Delegation.UnityAdapter.Bridge.TargetRegistry, ProjectAegis.Delegation.UnityAdapter.Bridge.IOrderSink)
   at ProjectAegis.Delegation.UnityAdapter.Bridge.DelegationBridge.Tick(ProjectAegis.Delegation.UnityAdapter.Bridge.ISimWorldSnapshot, ProjectAegis.Delegation.UnityAdapter.Bridge.IOrderSink)
   at ProjectAegis.Delegation.UnityAdapter.Baltic.BalticReplayStepper.StepOneObservedTick()
   at ProjectAegis.Delegation.UnityAdapter.Tests.Save.SaveResumeDriverTests.Restore_and_resume_with_non_engage_orders_move_hold_set_emcon_matches_continuous_run()
Test Run Aborted.
```

- **Command 2 (Compile RED for `additionalInnerSink`, `forwardOrdersToInnerSink`, and `combinedWorldHashProvider` tests):**
  `date -u +"%Y-%m-%dT%H:%M:%SZ" && export PATH="$HOME/.dotnet:$PATH" && dotnet build src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --no-restore 2>&1`

```text
2026-11-23T13:48:46Z
/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/Save/SaveResumeDriverTests.cs(378,13): error CS1739: The best overload for 'BindInnerSnapshot' does not have a parameter named 'additionalInnerSink' [/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj]
/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/Save/SaveResumeDriverTests.cs(391,13): error CS1739: The best overload for 'BindInnerSnapshot' does not have a parameter named 'forwardOrdersToInnerSink' [/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj]
/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/Save/SaveResumeDriverTests.cs(419,17): error CS1739: The best overload for 'SessionCommandFacade' does not have a parameter named 'combinedWorldHashProvider' [/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj]

Build FAILED.
    0 Warning(s)
    3 Error(s)
```

### 2. Iteration 2 GREEN Fix & Verification (`2026-11-23T13:49:19Z`)

- **Production Fixes Applied:**
  - `src/ProjectAegis.Delegation.UnityAdapter/Save/ReplayableSimWorld.cs`: separated snapshot binding from optional order forwarding (`IOrderSink? additionalInnerSink = null, bool forwardOrdersToInnerSink = false` on `BindInnerSnapshot`, defaulting to `false` so `BalticHeadlessSnapshot -> ReplayableSimWorld` executes once without recursing back) and added `_isApplyingOrder` re-entrancy + `ReferenceEquals(sinkToForward, this)` guards in `ApplyOrder`.
  - `src/ProjectAegis.Delegation.UnityAdapter/Save/SessionCommandFacade.cs`: added optional `Func<ulong>? combinedWorldHashProvider = null` constructor parameter queried by `TryCaptureEnvelope` when `combinedWorldHash == 0UL` before falling back to `lastWorldHash`.
  - `src/ProjectAegis.Delegation.UnityAdapter/Save/SaveResumeDriver.cs`: wired `combinedWorldHashProvider: () => candidateStepper.CombinedWorldHash` in `TryRestoreEnvelope`.
- **Save Suite Result (`--filter "FullyQualifiedName~Save"`):**
  `Passed! - Failed: 0, Passed: 25, Skipped: 0, Total: 25, Duration: 466 ms - ProjectAegis.Delegation.UnityAdapter.Tests.dll (net11.0)`

---

## 3. Track B — Verbatim RED Evidence (DRG-349 Differential Proof Matrix & Latency)

- **Timestamp (UTC):** `2026-11-23T14:10:11Z`
- **Working Directory:** `/home/username01/cmano-clone`
- **Branch / Base:** `sprint/s5-h6-save-resume` @ `f7267cde`
- **RED Test Files Created:**
  - `src/ProjectAegis.Delegation.UnityAdapter.Tests/Save/SaveDifferentialAssert.cs` (RED stub throwing `NotImplementedException`)
  - `src/ProjectAegis.Delegation.UnityAdapter.Tests/Save/SaveResumeDifferentialProofTests.cs` (14 differential/rollback proof cases + 1 restore latency benchmark matrix across `T = 60, 180, 360, 600` at `1x` & `4x`)
- **Command:** `date -u +"%Y-%m-%dT%H:%M:%SZ" && export PATH="$HOME/.dotnet:$PATH" && dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "FullyQualifiedName~SaveResumeDifferentialProofTests" -v minimal 2>&1`

```text
2026-11-23T14:10:11Z
  Determining projects to restore...
  All projects are up-to-date for restore.
  ProjectAegis.Data -> /home/username01/cmano-clone/src/ProjectAegis.Data/bin/Debug/net11.0/ProjectAegis.Data.dll
  ProjectAegis.Ai.Contracts -> /home/username01/cmano-clone/src/ProjectAegis.Ai.Contracts/bin/Debug/net11.0/ProjectAegis.Ai.Contracts.dll
  ProjectAegis.Sim -> /home/username01/cmano-clone/src/ProjectAegis.Sim/bin/Debug/net11.0/ProjectAegis.Sim.dll
  ProjectAegis.Delegation -> /home/username01/cmano-clone/src/ProjectAegis.Delegation/bin/Debug/net11.0/ProjectAegis.Delegation.dll
  ProjectAegis.Delegation.UnityAdapter -> /home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter/bin/Debug/net11.0/ProjectAegis.Delegation.UnityAdapter.dll
  ProjectAegis.Delegation.UnityAdapter.Tests -> /home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/bin/Debug/net11.0/ProjectAegis.Delegation.UnityAdapter.Tests.dll
Test run for /home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/bin/Debug/net11.0/ProjectAegis.Delegation.UnityAdapter.Tests.dll (.NETCoreApp,Version=v11.0)
A total of 1 test files matched the specified pattern.
  Failed Differential_agent_rebind_rebrief_assign_and_doctrine_override [175 ms]
  Error Message:
     Assert.That(cont.Facade.TryApplyDoctrineOverride(new TargetId("u1"), "WeaponsFree", 2.0), Is.True)
  Expected: True
  But was:  False

  Failed Differential_contact_override_and_play_delegation_personality_autonomy [6 ms]
  Error Message:
     Contact 'c1' is not active.
Assert.That(cont.Facade.TryReclassifyContact("c1", "Friendly", 2.0, out var cRes), Is.True)
  Expected: True
  But was:  False

  Failed Differential_craft_ops_launch_aircraft_launch_boat_and_recover_boat [92 ms]
  Error Message:
   System.NotImplementedException : RED stub: AssertFullParity not yet implemented (Craft ops launch_aircraft/launch_boat/recover_boat N=6, M=6).

  Failed Differential_direct_control_handoff_and_pause_resume_agent_actions [42 ms]
  Error Message:
   System.NotImplementedException : RED stub: AssertFullParity not yet implemented (Direct control handoff + pause/resume agent actions).

  Failed Differential_interruption_1_before_pending_approval_and_after_approve_reject [36 ms]
  Error Message:
   System.NotImplementedException : RED stub: AssertPendingApprovalsEqual not yet implemented (Subcase A at save tick N=3).

  Failed Differential_interruption_2_delayed_comms_order_in_flight [46 ms]
  Error Message:
   System.NotImplementedException : RED stub: AssertFullParity not yet implemented (Delayed comms order in flight N=3, M=4).

  Failed Differential_interruption_3_active_engagement_and_magazine_depletion [41 ms]
  Error Message:
   System.NotImplementedException : RED stub: AssertFullParity not yet implemented (Active engagement + magazine depletion N=2, M=4).

  Failed Differential_omitted_state_only_or_unknown_journal_command_fails_and_rolls_back [7 ms]
  Error Message:
   System.NotImplementedException : RED stub: AssertHostUnchanged not yet implemented (unknown cmd).

  Failed Differential_pending_NextEngageSalvoOverride_survives_save_and_controls_next_salvo [42 ms]
  Error Message:
   System.NotImplementedException : RED stub: AssertFullParity not yet implemented (Pending NextEngageSalvoOverride fire-single N=2, M=3).

  Failed Differential_real_sqlite_catalog_gauntlet_orbat_parity [67 ms]
  Error Message:
   System.NotImplementedException : RED stub: AssertFullParity not yet implemented (Real SQLite gauntlet-t1-patrol-a ORBAT N=6, M=4).

  Failed Differential_same_tick_multiple_commands_preserve_sequence_order [1 ms]
  Error Message:
     Assert.That(cont.Facade.TryApplyDoctrineOverride(new TargetId("u1"), "WeaponsFree", 2.0), Is.True)
  Expected: True
  But was:  False

  Failed Differential_time_acceleration_2x_4x_8x_observed_vs_pipeline_tick [33 ms]
  Error Message:
   System.NotImplementedException : RED stub: AssertFullParity not yet implemented (Time acceleration 2x/4x/8x N=6, M=4).

  Failed Differential_watch_auto_pause_gate_ack_dismiss_clearance_and_second_gen_save [6 ms]
  Error Message:
   System.NotImplementedException : RED stub: AssertFullParity not yet implemented (WatchAutoPauseGate 1st-gen restore + re-ack/dismiss + M=3).

  Failed Restore_latency_benchmark_across_tick_depths_and_accelerations_stays_under_30s_ceiling [23 ms]
  Error Message:
     Assert.That(unit.Facade.TryApplyDoctrineOverride(new TargetId("u1"), "WeaponsFree", 2.0), Is.True)
  Expected: True
  But was:  False

  Failed Rollback_corrupt_truncated_incompatible_envelope_and_slot_store_leave_active_session_and_slots_unchanged [25 ms]
  Error Message:
   System.NotImplementedException : RED stub: AssertHostUnchanged not yet implemented (truncated).

Failed!  - Failed:    15, Passed:     0, Skipped:     0, Total:    15, Duration: 651 ms - ProjectAegis.Delegation.UnityAdapter.Tests.dll (net11.0)
```

---

## 4. Track B — Verbatim GREEN Verification Evidence (`DRG-349` Differential Proof Matrix & Latency — Commit `ef2fd8fc`)

- **Timestamp (UTC):** `2026-11-23T14:27:23Z` (post-Iteration 2 fix: `CreateScratchTempDir()` switched to `Path.GetTempPath()` + Gen 2 `TryResumeSim(explicitOverride: false, out _)` assertion in Test 11)
- **Track B Files Created / Updated:**
  - `src/ProjectAegis.Delegation.UnityAdapter.Tests/Save/SaveDifferentialAssert.cs` (deep-state differential parity assertions across clocks, hashes, `SimulationSession`, `DecisionLog` chronological sequence/content/fingerprints, `ReplayCheckpointStore`, `ReplayableSimWorld`/`IOrderSink` side effects, `PendingApprovalQueue`, and controller/agent states)
  - `src/ProjectAegis.Delegation.UnityAdapter.Tests/Save/SaveResumeDifferentialProofTests.cs` (`14` differential & rollback proof cases + `1` deterministic wall-clock restore latency benchmark across `T = 60, 180, 360, 600` at `1x` & `4x`)
  - `src/ProjectAegis.Delegation.UnityAdapter/Save/SaveEnvelopeSerializer.cs` (raised `SaveEnvelopeBounds.MaxPayloadBytes` from `4 MiB` to `64 MiB` and `MaxRawFingerprintLength` from `512 KiB` to `2 MiB` after the `F11` benchmark uncovered that `T = 360` raw `LogFingerprint` checkpoint rows already reach `5.11 MB` and `T = 600` reaches `14.24 MB`, while `0` edits were made to `DelegationBridge.cs`, `SimulationSession.cs`, or any Hard-No file)
- **Command:** `date -u +"%Y-%m-%dT%H:%M:%SZ" && export PATH="$HOME/.dotnet:$PATH" && dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --no-build --filter "FullyQualifiedName~SaveResumeDifferentialProofTests" --logger "console;verbosity=normal" 2>&1`

```text
2026-11-23T14:27:23Z
Test run for /home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/bin/Debug/net11.0/ProjectAegis.Delegation.UnityAdapter.Tests.dll (.NETCoreApp,Version=v11.0)
A total of 1 test files matched the specified pattern.
NUnit Adapter 6.3.1.0: Test execution started
Running selected tests in /home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/bin/Debug/net11.0/ProjectAegis.Delegation.UnityAdapter.Tests.dll
   NUnit3TestExecutor discovered 15 of 15 NUnit test cases using Current Discovery mode, Non-Explicit run
  Passed Differential_agent_rebind_rebrief_assign_and_doctrine_override [229 ms]
  Passed Differential_contact_override_and_play_delegation_personality_autonomy [17 ms]
  Passed Differential_craft_ops_launch_aircraft_launch_boat_and_recover_boat [81 ms]
  Passed Differential_direct_control_handoff_and_pause_resume_agent_actions [44 ms]
  Passed Differential_interruption_1_before_pending_approval_and_after_approve_reject [76 ms]
  Passed Differential_interruption_2_delayed_comms_order_in_flight [49 ms]
  Passed Differential_interruption_3_active_engagement_and_magazine_depletion [38 ms]
  Passed Differential_omitted_state_only_or_unknown_journal_command_fails_and_rolls_back [45 ms]
  Passed Differential_pending_NextEngageSalvoOverride_survives_save_and_controls_next_salvo [39 ms]
  Passed Differential_real_sqlite_catalog_gauntlet_orbat_parity [41 ms]
  Passed Differential_same_tick_multiple_commands_preserve_sequence_order [5 ms]
  Passed Differential_time_acceleration_2x_4x_8x_observed_vs_pipeline_tick [35 ms]
  Passed Differential_watch_auto_pause_gate_ack_dismiss_clearance_and_second_gen_save [27 ms]
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


  Passed Restore_latency_benchmark_across_tick_depths_and_accelerations_stays_under_30s_ceiling [7 s]
  Passed Rollback_corrupt_truncated_incompatible_envelope_and_slot_store_leave_active_session_and_slots_unchanged [17 ms]

Test Run Successful.
Total tests: 15
     Passed: 15
 Total time: 8.4737 Seconds
```

- **Post-Track-B (`ef2fd8fc`) Full-Suite & Gate Cross-Reference (`2026-11-23T14:44:26Z` M3 re-verified):**
  - `dotnet build ProjectAegis.sln --no-restore` -> `0 Warning(s), 0 Error(s)`
  - `dotnet test ProjectAegis.sln -v minimal --no-build` -> **`4,987 / 4,987` passed** (`0` failed, `0` skipped; `>= 4,938` floor): `Ai.Contracts.Tests` `18`, `Delegation.Demo.Tests` `15`, `Sim.Tests` `653`, `Ai.Host.Tests` `88`, `Delegation.Tests` `1,543`, `Data.Excel.Tests` `24`, `Delegation.UnityAdapter.Tests` `1,501` (including `Save` `40/40`, `ReplayGolden` `17/17`, `PlayModeSmokeHarnessTests` `27/27`), `Data.Tests` `974` (including `31/31` architecture fences), `MissionEditor.Cli.Tests` `171` (see `production/qa/evidence/sprint-5/s5-h6-save-proof-evidence.md` §3–4 for full matrix + `61/61` live Unity `:8081` PlayMode verification).


