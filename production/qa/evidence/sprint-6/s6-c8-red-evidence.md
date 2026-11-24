# Sprint 6 C8 / H5 — Consolidated RED->GREEN TDD Evidence

**Branch:** `sprint/s6-gauntlet-g2-gate` (stacked on `sprint/s5-h6-save-resume` @ `4b5b4ca1`, 2026-11-23)

---

## Milestone M1 (DRG-201, DRG-195, DRG-187)

### 1. Verbatim RED Evidence (Before Any Production or `docs/` Edits)

- **Timestamp (UTC):** `2026-11-23T15:20:35Z`
- **Working Directory:** `/home/username01/cmano-clone`
- **Branch / Base:** `sprint/s6-gauntlet-g2-gate` @ `4b5b4ca1`
- **RED Test Files Created (NUnit 5.0.0):**
  - `src/ProjectAegis.Data.Tests/Evidence/MissionThreadEvidenceLedgerTests.cs`
  - `src/ProjectAegis.Data.Tests/Evidence/RevisionBoundAcceptanceValidatorTests.cs`
  - `src/ProjectAegis.MissionEditor.Cli.Tests/GauntletOracleEvalBaselineLedgerTests.cs`
  - `src/ProjectAegis.Ai.Host.Tests/Evidence/EvidenceLedgerProjectorTests.cs`
  - `src/ProjectAegis.Data.Tests/Architecture/KillChainTraceabilityMatrixPinsTests.cs`
  - `src/ProjectAegis.Delegation.Tests/Decision/AgentDecisionProvenanceTests.cs`
  - `src/ProjectAegis.Delegation.Tests/Skills/DegradedModePolicyTests.cs`
  - `src/ProjectAegis.Delegation.UnityAdapter.Tests/CommandReview/DegradedModeAdviceVerificationTests.cs`
  - `src/ProjectAegis.Ai.Host.Tests/Evidence/EvidenceProjectorProvenanceAndDegradedTests.cs`
- **Command:** `date -u +"%Y-%m-%dT%H:%M:%SZ" && git branch --show-current && git rev-parse --short HEAD && export PATH="$HOME/.dotnet:$PATH" && dotnet build ProjectAegis.sln --no-restore 2>&1`

```text
2026-11-23T15:20:35Z
sprint/s6-gauntlet-g2-gate
4b5b4ca1
  ProjectAegis.Ai.Contracts -> /home/username01/cmano-clone/src/ProjectAegis.Ai.Contracts/bin/Debug/net11.0/ProjectAegis.Ai.Contracts.dll
  ProjectAegis.Ai.Contracts -> /home/username01/cmano-clone/src/ProjectAegis.Ai.Contracts/bin/Debug/netstandard2.1/ProjectAegis.Ai.Contracts.dll
  ProjectAegis.Data -> /home/username01/cmano-clone/src/ProjectAegis.Data/bin/Debug/net11.0/ProjectAegis.Data.dll
  ProjectAegis.Data -> /home/username01/cmano-clone/src/ProjectAegis.Data/bin/Debug/netstandard2.1/ProjectAegis.Data.dll
  ProjectAegis.Ai.Contracts.Tests -> /home/username01/cmano-clone/src/ProjectAegis.Ai.Contracts/test/bin/Debug/net11.0/ProjectAegis.Ai.Contracts.Tests.dll
  ProjectAegis.Sim -> /home/username01/cmano-clone/src/ProjectAegis.Sim/bin/Debug/netstandard2.1/ProjectAegis.Sim.dll
  ProjectAegis.Sim -> /home/username01/cmano-clone/src/ProjectAegis.Sim/bin/Debug/net11.0/ProjectAegis.Sim.dll
  ProjectAegis.Data.Excel -> /home/username01/cmano-clone/src/ProjectAegis.Data.Excel/bin/Debug/net11.0/ProjectAegis.Data.Excel.dll
  ProjectAegis.Delegation -> /home/username01/cmano-clone/src/ProjectAegis.Delegation/bin/Debug/net11.0/ProjectAegis.Delegation.dll
  ProjectAegis.Delegation -> /home/username01/cmano-clone/src/ProjectAegis.Delegation/bin/Debug/netstandard2.1/ProjectAegis.Delegation.dll
  ProjectAegis.Sim.Benchmark -> /home/username01/cmano-clone/src/ProjectAegis.Sim.Benchmark/bin/Debug/net11.0/ProjectAegis.Sim.Benchmark.dll
  ProjectAegis.Data.Excel.Tests -> /home/username01/cmano-clone/src/ProjectAegis.Data.Excel.Tests/bin/Debug/net11.0/ProjectAegis.Data.Excel.Tests.dll
  ProjectAegis.Ai.Host -> /home/username01/cmano-clone/src/ProjectAegis.Ai.Host/bin/Debug/net11.0/ProjectAegis.Ai.Host.dll
  ProjectAegis.Delegation.UnityAdapter -> /home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter/bin/Debug/net11.0/ProjectAegis.Delegation.UnityAdapter.dll
  ProjectAegis.Delegation.UnityAdapter -> /home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter/bin/Debug/netstandard2.1/ProjectAegis.Delegation.UnityAdapter.dll
  ProjectAegis.Sim.Tests -> /home/username01/cmano-clone/src/ProjectAegis.Sim.Tests/bin/Debug/net11.0/ProjectAegis.Sim.Tests.dll
  ProjectAegis.MissionEditor.Cli -> /home/username01/cmano-clone/src/ProjectAegis.MissionEditor.Cli/bin/Debug/net11.0/ProjectAegis.MissionEditor.Cli.dll
  ProjectAegis.Delegation.Demo -> /home/username01/cmano-clone/src/ProjectAegis.Delegation.Demo/bin/Debug/net11.0/ProjectAegis.Delegation.Demo.dll
  ProjectAegis.Delegation.Demo.Tests -> /home/username01/cmano-clone/src/ProjectAegis.Delegation.Demo.Tests/bin/Debug/net11.0/ProjectAegis.Delegation.Demo.Tests.dll
/home/username01/cmano-clone/src/ProjectAegis.MissionEditor.Cli.Tests/GauntletOracleEvalBaselineLedgerTests.cs(4,25): error CS0234: The type or namespace name 'Evidence' does not exist in the namespace 'ProjectAegis.Data' (are you missing an assembly reference?) [/home/username01/cmano-clone/src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj]
/home/username01/cmano-clone/src/ProjectAegis.Data.Tests/Evidence/MissionThreadEvidenceLedgerTests.cs(2,25): error CS0234: The type or namespace name 'Evidence' does not exist in the namespace 'ProjectAegis.Data' (are you missing an assembly reference?) [/home/username01/cmano-clone/src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj]
/home/username01/cmano-clone/src/ProjectAegis.Data.Tests/Evidence/RevisionBoundAcceptanceValidatorTests.cs(2,25): error CS0234: The type or namespace name 'Evidence' does not exist in the namespace 'ProjectAegis.Data' (are you missing an assembly reference?) [/home/username01/cmano-clone/src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj]
/home/username01/cmano-clone/src/ProjectAegis.Data.Tests/Evidence/RevisionBoundAcceptanceValidatorTests.cs(137,20): error CS0246: The type or namespace name 'RevisionBoundAcceptanceRecord' could not be found (are you missing a using directive or an assembly reference?) [/home/username01/cmano-clone/src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj]
/home/username01/cmano-clone/src/ProjectAegis.Data.Tests/Evidence/RevisionBoundAcceptanceValidatorTests.cs(165,20): error CS0246: The type or namespace name 'RevisionBoundAcceptanceRecord' could not be found (are you missing a using directive or an assembly reference?) [/home/username01/cmano-clone/src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj]
/home/username01/cmano-clone/src/ProjectAegis.Ai.Host.Tests/Evidence/EvidenceLedgerProjectorTests.cs(4,25): error CS0234: The type or namespace name 'Evidence' does not exist in the namespace 'ProjectAegis.Data' (are you missing an assembly reference?) [/home/username01/cmano-clone/src/ProjectAegis.Ai.Host.Tests/ProjectAegis.Ai.Host.Tests.csproj]
/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/CommandReview/DegradedModeAdviceVerificationTests.cs(36,17): error CS1739: The best overload for 'DecisionRecord' does not have a parameter named 'PolicySnapshotId' [/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj]
/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/CommandReview/DegradedModeAdviceVerificationTests.cs(57,47): error CS0117: 'AdviceSkillService' does not contain a definition for 'InvokeWithPolicy' [/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj]
/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/CommandReview/DegradedModeAdviceVerificationTests.cs(131,58): error CS1061: 'DecisionLogDeepLinkDetail' does not contain a definition for 'Personality' and no accessible extension method 'Personality' accepting a first argument of type 'DecisionLogDeepLinkDetail' could be found (are you missing a using directive or an assembly reference?) [/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj]
/home/username01/cmano-clone/src/ProjectAegis.Delegation.Tests/Decision/AgentDecisionProvenanceTests.cs(33,13): error CS1739: The best overload for 'DecisionRecord' does not have a parameter named 'PolicySnapshotId' [/home/username01/cmano-clone/src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj]
/home/username01/cmano-clone/src/ProjectAegis.Delegation.Tests/Decision/AgentDecisionProvenanceTests.cs(37,29): error CS1061: 'AgentDecisionPayload' does not contain a definition for 'PolicySnapshotId' and no accessible extension method 'PolicySnapshotId' accepting a first argument of type 'AgentDecisionPayload' could be found (are you missing a using directive or an assembly reference?) [/home/username01/cmano-clone/src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj]
/home/username01/cmano-clone/src/ProjectAegis.Delegation.Tests/Skills/DegradedModePolicyTests.cs(15,21): error CS0103: The name 'DegradedModePolicy' does not exist in the current context [/home/username01/cmano-clone/src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj]
/home/username01/cmano-clone/src/ProjectAegis.Delegation.Tests/Skills/DegradedModePolicyTests.cs(62,80): error CS0117: 'SkillEnvelopeValidator' does not contain a definition for 'ReasonPolicyUnavailable' [/home/username01/cmano-clone/src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj]
/home/username01/cmano-clone/src/ProjectAegis.Delegation.Tests/Skills/DegradedModePolicyTests.cs(70,106): error CS1739: The best overload for 'Validate' does not have a parameter named 'currentSimTick' [/home/username01/cmano-clone/src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj]
/home/username01/cmano-clone/src/ProjectAegis.Delegation.Tests/Skills/DegradedModePolicyTests.cs(112,25): error CS1061: 'C2AuthorityProjectionContext' does not contain a definition for 'PolicyUnavailable' and no accessible extension method 'PolicyUnavailable' accepting a first argument of type 'C2AuthorityProjectionContext' could be found (are you missing a using directive or an assembly reference?) [/home/username01/cmano-clone/src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj]
/home/username01/cmano-clone/src/ProjectAegis.Delegation.Tests/Skills/DegradedModePolicyTests.cs(117,89): error CS0117: 'C2AuthorityProjector' does not contain a definition for 'ReasonPolicyUnavailable' [/home/username01/cmano-clone/src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj]

Build FAILED.
    0 Warning(s)
    58 Error(s)
```

### 2. GREEN Verification Summary (`2026-11-23T15:27:08Z`)

- **Production & Docs Files Created:**
  - `src/ProjectAegis.Data/Evidence/MissionThreadEvidenceModels.cs`
  - `src/ProjectAegis.Data/Evidence/RevisionBoundAcceptanceRecord.cs`
  - `src/ProjectAegis.Data/Evidence/AcceptanceEvidenceValidator.cs`
  - `src/ProjectAegis.Data/Evidence/EvidenceLedger.cs`
  - `src/ProjectAegis.Ai.Host/Evidence/EvidenceLedgerAdapter.cs`
  - `src/ProjectAegis.Delegation/Skills/DegradedModePolicy.cs`
  - `docs/architecture/kill-chain-traceability-matrix.md`
- **Production & Docs Files Modified:**
  - `src/ProjectAegis.MissionEditor.Cli/GauntletOracleEvalCommand.cs`
  - `src/ProjectAegis.MissionEditor.Cli/Program.cs`
  - `src/ProjectAegis.Delegation/Decision/DecisionRecord.cs`
  - `src/ProjectAegis.Delegation/Decision/AgentDecisionPayload.cs`
  - `src/ProjectAegis.Delegation/Controllers/AgentController.cs`
  - `src/ProjectAegis.Delegation/Projection/DecisionLogDeepLinkProjection.cs`
  - `src/ProjectAegis.Delegation.UnityAdapter/Presentation/DecisionLogDeepLinkBinder.cs`
  - `src/ProjectAegis.Ai.Host/Evidence/EvidenceProjector.cs`
  - `src/ProjectAegis.Delegation/Skills/SkillEnvelopeValidator.cs`
  - `src/ProjectAegis.Delegation/Skills/C2AuthorityTypes.cs`
  - `src/ProjectAegis.Delegation/Skills/C2AuthorityProjector.cs`
  - `src/ProjectAegis.Delegation.UnityAdapter/CommandReview/AdviceSkillService.cs`
  - `docs/architecture/requirements-traceability.md`
- **Build & Multi-TFM `netstandard2.1` Results:**
  - `dotnet build ProjectAegis.sln --no-restore` -> `Build succeeded. 0 Warning(s), 0 Error(s)`
  - `dotnet build src/ProjectAegis.Delegation.UnityAdapter/ProjectAegis.Delegation.UnityAdapter.csproj -c Release -f netstandard2.1 --no-restore` -> `Build succeeded. 0 Warning(s), 0 Error(s)`
- **Full Test Suite (`dotnet test ProjectAegis.sln -v minimal --no-build`):**
  - `ProjectAegis.Ai.Contracts.Tests`: `18 / 18` passed
  - `ProjectAegis.Delegation.Demo.Tests`: `15 / 15` passed
  - `ProjectAegis.Sim.Tests`: `653 / 653` passed
  - `ProjectAegis.Ai.Host.Tests`: `91 / 91` passed
  - `ProjectAegis.Delegation.Tests`: `1,548 / 1,548` passed
  - `ProjectAegis.Data.Excel.Tests`: `24 / 24` passed
  - `ProjectAegis.Delegation.UnityAdapter.Tests`: `1,504 / 1,504` passed
  - `ProjectAegis.Data.Tests`: `985 / 985` passed
  - `ProjectAegis.MissionEditor.Cli.Tests`: `173 / 173` passed
  - **Solution Total:** **`5,011 / 5,011` passed** (`0` failed, `0` skipped; `> 4,987` baseline floor, `+24` M1 tests)
- **Standing Invariants & Architecture Fences:**
  - `dotnet test ProjectAegis.sln --no-build -v minimal --filter "FullyQualifiedName~ReplayGolden"`: `17/17` (`UnityAdapter.Tests`) + `4/4` (`Delegation.Tests`); `ReplayGoldenSuiteTests`: `6/6`
  - `grep -r "17144800277401907079" tests/ data/ | wc -l`: `26`
  - `PlayModeSmokeHarnessTests`: `27 / 27`
  - `UnitySimImportForbidTests|NamespaceSccBaselineTests|CsprojReferenceGraphPinTests`: `31 / 31`
  - Hard-No `git diff --stat` (`4b5b4ca1..HEAD` and working tree) across `DelegationBridge.cs`, `SimulationSession.cs`, `WriteGate/`, `MapPlaceholderPanelHost.cs`, `GlobeMapProductHost.cs`, `tests/regression/`, `DelegationSmoke.unity`, `assets/data/catalog/`: empty (`0` diff).

### 3. Iteration 2 Remediation RED -> GREEN

#### Verbatim RED Evidence (Before Production Fixes)

- **Timestamp (UTC):** `2026-11-23T15:37:02Z`
- **Command:** `export PATH="$HOME/.dotnet:$PATH" && dotnet test ProjectAegis.sln --filter "FullyQualifiedName~MissionThreadEvidenceLedgerTests|FullyQualifiedName~RevisionBoundAcceptanceValidatorTests|FullyQualifiedName~GauntletOracleEvalBaselineLedgerTests|FullyQualifiedName~DegradedModeAdviceVerificationTests|FullyQualifiedName~DegradedModePolicyTests" -v minimal --no-build 2>&1`

```text
2026-11-23T15:37:02Z
  Failed Baseline_context_out_and_ledger_out_emit_correlated_pending_acceptance_and_evidence [70 ms]
  Error Message:
     Assert.That(authorityEntry.SimTick, Is.EqualTo(0UL))
  Expected: 0
  But was:  1

  Stack Trace:
     at ProjectAegis.MissionEditor.Cli.Tests.GauntletOracleEvalBaselineLedgerTests.Baseline_context_out_and_ledger_out_emit_correlated_pending_acceptance_and_evidence() in /home/username01/cmano-clone/src/ProjectAegis.MissionEditor.Cli.Tests/GauntletOracleEvalBaselineLedgerTests.cs:line 84

  Failed Baseline_context_with_empty_policies_or_empty_policy_sha_fails_closed_with_exit_1 [< 1 ms]
  Error Message:
     Assert.That(exit1, Is.EqualTo(1))
  Expected: 1
  But was:  0

  Stack Trace:
     at ProjectAegis.MissionEditor.Cli.Tests.GauntletOracleEvalBaselineLedgerTests.Baseline_context_with_empty_policies_or_empty_policy_sha_fails_closed_with_exit_1() in /home/username01/cmano-clone/src/ProjectAegis.MissionEditor.Cli.Tests/GauntletOracleEvalBaselineLedgerTests.cs:line 193

  Failed Partial_pass_reports_actual_passed_and_failed_scenario_counts_in_ledger [3 ms]
  Error Message:
     Assert.That(ledger.AcceptanceRecords[0].Counts.ObservedPassed, Is.EqualTo(1))
  Expected: 1
  But was:  0

  Stack Trace:
     at ProjectAegis.MissionEditor.Cli.Tests.GauntletOracleEvalBaselineLedgerTests.Partial_pass_reports_actual_passed_and_failed_scenario_counts_in_ledger() in /home/username01/cmano-clone/src/ProjectAegis.MissionEditor.Cli.Tests/GauntletOracleEvalBaselineLedgerTests.cs:line 246

Failed!  - Failed:     3, Passed:     1, Skipped:     0, Total:     4, Duration: 92 ms - ProjectAegis.MissionEditor.Cli.Tests.dll (net11.0)
  Failed EvidenceLedgerBuilder_rejects_invalid_and_duplicate_entries_and_is_tie_break_invariant [48 ms]
  Error Message:
     Assert.That(caughtException, expression)
  Expected: <System.ArgumentException>
  But was:  null

  Stack Trace:
     at ProjectAegis.Data.Tests.Evidence.MissionThreadEvidenceLedgerTests.EvidenceLedgerBuilder_rejects_invalid_and_duplicate_entries_and_is_tie_break_invariant() in /home/username01/cmano-clone/src/ProjectAegis.Data.Tests/Evidence/MissionThreadEvidenceLedgerTests.cs:line 254

  Failed Gauntlet_fingerprint_extractor_parses_real_decision_log_wire_format_tokens [5 ms]
  Error Message:
     Assert.That(policyUpdate.SimTick, Is.EqualTo(0UL))
  Expected: 0
  But was:  1

  Stack Trace:
     at ProjectAegis.Data.Tests.Evidence.MissionThreadEvidenceLedgerTests.Gauntlet_fingerprint_extractor_parses_real_decision_log_wire_format_tokens() in /home/username01/cmano-clone/src/ProjectAegis.Data.Tests/Evidence/MissionThreadEvidenceLedgerTests.cs:line 182

  Failed Ver07_4_ci_green_never_auto_accepts_and_forged_owner_fields_fail_closed [1 ms]
  Error Message:
     Assert.That(missingInputsResult.IsSchemaValid, Is.False)
  Expected: False
  But was:  True

  Stack Trace:
     at ProjectAegis.Data.Tests.Evidence.RevisionBoundAcceptanceValidatorTests.Ver07_4_ci_green_never_auto_accepts_and_forged_owner_fields_fail_closed() in /home/username01/cmano-clone/src/ProjectAegis.Data.Tests/Evidence/RevisionBoundAcceptanceValidatorTests.cs:line 78

Failed!  - Failed:     3, Passed:     6, Skipped:     0, Total:     9, Duration: 76 ms - ProjectAegis.Data.Tests.dll (net11.0)
  Failed SkillEnvelopeValidator_ttl_boundary_near_ulong_max_does_not_wrap_around [15 ms]
  Error Message:
     Assert.That(proposeOk.Ok, Is.True)
  Expected: True
  But was:  False

  Stack Trace:
     at ProjectAegis.Delegation.Tests.Skills.DegradedModePolicyTests.SkillEnvelopeValidator_ttl_boundary_near_ulong_max_does_not_wrap_around() in /home/username01/cmano-clone/src/ProjectAegis.Delegation.Tests/Skills/DegradedModePolicyTests.cs:line 116

Failed!  - Failed:     1, Passed:     3, Skipped:     0, Total:     4, Duration: 41 ms - ProjectAegis.Delegation.Tests.dll (net11.0)
  Failed AdviceSkillService_InvokeWithPolicy_discloses_all_five_degraded_conditions_with_zero_hidden_fallback [31 ms]
  Error Message:
     Assert.That(withheld.Result, Is.Null)
  Expected: null
  But was:  <AdviceSkillResult { Skill = SkillEnvelope { Lane = Read, SkillId = c2.advice.resource.recommend, InvocationId = c2.advice.resource.recommend:4:4:, ... }, Advice = AdviceFrame { ..., Availability = Available, ..., Authority = C2AuthorityProjection { ..., Targeting = C2TargetingAuthority { Disposition = Withheld, ReasonCode = RoeHoldFire, ... } }, Fallback = hold, ... } }>

  Stack Trace:
     at ProjectAegis.Delegation.UnityAdapter.Tests.CommandReview.DegradedModeAdviceVerificationTests.AdviceSkillService_InvokeWithPolicy_discloses_all_five_degraded_conditions_with_zero_hidden_fallback() in /home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/CommandReview/DegradedModeAdviceVerificationTests.cs:line 98

  Failed Real_baltic_replay_fingerprint_extracts_clean_typed_records_without_index_shifting [8 ms]
  Error Message:
     Assert.That(recommendations.All(r => Enum.TryParse<OrderKind>(r.Recommendation!.ChosenKind, out _)), Is.True)
  Expected: True
  But was:  False

  Stack Trace:
     at ProjectAegis.Delegation.UnityAdapter.Tests.CommandReview.DegradedModeAdviceVerificationTests.Real_baltic_replay_fingerprint_extracts_clean_typed_records_without_index_shifting() in /home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/CommandReview/DegradedModeAdviceVerificationTests.cs:line 147

Failed!  - Failed:     2, Passed:     2, Skipped:     0, Total:     4, Duration: 249 ms - ProjectAegis.Delegation.UnityAdapter.Tests.dll (net11.0)
```

#### GREEN Verification Summary (After Minimal Production Fixes)

- **Timestamp (UTC):** `2026-11-23T15:39:56Z`
- **Production Files Fixed:**
  - `src/ProjectAegis.Data/Evidence/EvidenceLedger.cs` (`GauntletFingerprintEvidenceExtractor.Extract` canonical `DecisionLog.ComputeFingerprint()` wire-format parser, `EvidenceLedgerBuilder.Build` validation + duplicate-ID rejection + deterministic tie-breaking, `EvidenceLedgerSerializer.TryParseBaselineContext` fail-closed on invalid/empty policy entries)
  - `src/ProjectAegis.Data/Evidence/AcceptanceEvidenceValidator.cs` (require `AcceptedInputs != null` when `OwnerAcceptance.Status == Accepted`)
  - `src/ProjectAegis.MissionEditor.Cli/GauntletOracleEvalCommand.cs` (fail closed when `evaluatedPolicies.Count > 0` and `baseline.Policies` is empty or missing catalog file; compute actual `ObservedPassed` and `ObservedFailed` scenario counts in `FloorVsObservedCounts`; disambiguate multi-row `EvidenceId` prefixes)
  - `src/ProjectAegis.Delegation.UnityAdapter/CommandReview/AdviceSkillService.cs` (`InvokeWithPolicy` fails closed with `Result: null` whenever `assessment.IsDegraded` is `true`)
  - `src/ProjectAegis.Delegation/Skills/SkillEnvelopeValidator.cs` (overflow-safe `IsExpired` TTL subtraction)
- **Build & Multi-TFM `netstandard2.1` Results:**
  - `dotnet build ProjectAegis.sln --no-restore` -> `Build succeeded. 0 Warning(s), 0 Error(s)`
  - `dotnet build src/ProjectAegis.Delegation.UnityAdapter/ProjectAegis.Delegation.UnityAdapter.csproj -c Release -f netstandard2.1 --no-restore` -> `Build succeeded. 0 Warning(s), 0 Error(s)`
- **Full Test Suite (`dotnet test ProjectAegis.sln -v minimal --no-build`):**
  - `ProjectAegis.Ai.Contracts.Tests`: `18 / 18` passed
  - `ProjectAegis.Sim.Tests`: `653 / 653` passed
  - `ProjectAegis.Delegation.Demo.Tests`: `15 / 15` passed
  - `ProjectAegis.Ai.Host.Tests`: `91 / 91` passed
  - `ProjectAegis.Delegation.Tests`: `1,549 / 1,549` passed
  - `ProjectAegis.Data.Excel.Tests`: `24 / 24` passed
  - `ProjectAegis.Data.Tests`: `986 / 986` passed
  - `ProjectAegis.Delegation.UnityAdapter.Tests`: `1,505 / 1,505` passed
  - `ProjectAegis.MissionEditor.Cli.Tests`: `175 / 175` passed
  - **Solution Total:** **`5,016 / 5,016` passed** (`0` failed, `0` skipped; `> 5,011` Iteration 1 total)
- **Standing Invariants & Architecture Fences:**
  - `dotnet test ProjectAegis.sln --no-build -v minimal --filter "FullyQualifiedName~ReplayGolden"`: `17/17` (`UnityAdapter.Tests`) + `4/4` (`Delegation.Tests`); `ReplayGoldenSuiteTests`: `6/6`
  - `grep -r "17144800277401907079" tests/ data/ | wc -l`: `26`
  - `PlayModeSmokeHarnessTests`: `27 / 27`
  - `UnitySimImportForbidTests|NamespaceSccBaselineTests|CsprojReferenceGraphPinTests`: `31 / 31`
  - Hard-No `git diff --stat` (`4b5b4ca1..HEAD` and working tree) across `DelegationBridge.cs`, `SimulationSession.cs`, `WriteGate/`, `MapPlaceholderPanelHost.cs`, `GlobeMapProductHost.cs`, `tests/regression/`, `DelegationSmoke.unity`, `assets/data/catalog/`: empty (`0` diff).


---

## Milestone M2 (DRG-193, DRG-203, DRG-204)

### 1. Verbatim RED Evidence (Before Any Production or `docs/` Edits)

- **Timestamp (UTC):** `2026-11-23T15:55:59Z`
- **Working Directory:** `/home/username01/cmano-clone`
- **Branch / Base:** `sprint/s6-gauntlet-g2-gate` @ `4b5b4ca1`
- **RED Test Files Created (NUnit 5.0.0):**
  - `src/ProjectAegis.Delegation.Tests/Ifc/IfcMissionThreadVerificationTests.cs` (`DRG-193`)
  - `src/ProjectAegis.Delegation.Tests/Resilience/AgenticResilienceAndSaboteurTests.cs` (`DRG-203`)
  - `src/ProjectAegis.Data.Tests/Architecture/BaselineRegressionSelectionPolicyTests.cs` (`DRG-204`)
- **Command:** `export PATH="$HOME/.dotnet:$PATH" && date -u +"%Y-%m-%dT%H:%M:%SZ" && git branch --show-current && git rev-parse --short HEAD && dotnet build ProjectAegis.sln --no-restore 2>&1`

```text
2026-11-23T15:55:59Z
sprint/s6-gauntlet-g2-gate
4b5b4ca1
  ProjectAegis.Data -> /home/username01/cmano-clone/src/ProjectAegis.Data/bin/Debug/netstandard2.1/ProjectAegis.Data.dll
  ProjectAegis.Ai.Contracts -> /home/username01/cmano-clone/src/ProjectAegis.Ai.Contracts/bin/Debug/netstandard2.1/ProjectAegis.Ai.Contracts.dll
  ProjectAegis.Data -> /home/username01/cmano-clone/src/ProjectAegis.Data/bin/Debug/net11.0/ProjectAegis.Data.dll
  ProjectAegis.Ai.Contracts -> /home/username01/cmano-clone/src/ProjectAegis.Ai.Contracts/bin/Debug/net11.0/ProjectAegis.Ai.Contracts.dll
  ProjectAegis.Sim -> /home/username01/cmano-clone/src/ProjectAegis.Sim/bin/Debug/net11.0/ProjectAegis.Sim.dll
  ProjectAegis.Data.Excel -> /home/username01/cmano-clone/src/ProjectAegis.Data.Excel/bin/Debug/net11.0/ProjectAegis.Data.Excel.dll
  ProjectAegis.Sim -> /home/username01/cmano-clone/src/ProjectAegis.Sim/bin/Debug/netstandard2.1/ProjectAegis.Sim.dll
  ProjectAegis.Delegation -> /home/username01/cmano-clone/src/ProjectAegis.Delegation/bin/Debug/net11.0/ProjectAegis.Delegation.dll
  ProjectAegis.Sim.Benchmark -> /home/username01/cmano-clone/src/ProjectAegis.Sim.Benchmark/bin/Debug/net11.0/ProjectAegis.Sim.Benchmark.dll
  ProjectAegis.Data.Excel.Tests -> /home/username01/cmano-clone/src/ProjectAegis.Data.Excel.Tests/bin/Debug/net11.0/ProjectAegis.Data.Excel.Tests.dll
  ProjectAegis.Delegation -> /home/username01/cmano-clone/src/ProjectAegis.Delegation/bin/Debug/netstandard2.1/ProjectAegis.Delegation.dll
  ProjectAegis.Ai.Contracts.Tests -> /home/username01/cmano-clone/src/ProjectAegis.Ai.Contracts/test/bin/Debug/net11.0/ProjectAegis.Ai.Contracts.Tests.dll
  ProjectAegis.Ai.Host -> /home/username01/cmano-clone/src/ProjectAegis.Ai.Host/bin/Debug/net11.0/ProjectAegis.Ai.Host.dll
  ProjectAegis.Delegation.UnityAdapter -> /home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter/bin/Debug/net11.0/ProjectAegis.Delegation.UnityAdapter.dll
  ProjectAegis.Sim.Tests -> /home/username01/cmano-clone/src/ProjectAegis.Sim.Tests/bin/Debug/net11.0/ProjectAegis.Sim.Tests.dll
  ProjectAegis.Delegation.UnityAdapter -> /home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter/bin/Debug/netstandard2.1/ProjectAegis.Delegation.UnityAdapter.dll
  ProjectAegis.Delegation.Demo -> /home/username01/cmano-clone/src/ProjectAegis.Delegation.Demo/bin/Debug/net11.0/ProjectAegis.Delegation.Demo.dll
  ProjectAegis.Ai.Host.Tests -> /home/username01/cmano-clone/src/ProjectAegis.Ai.Host.Tests/bin/Debug/net11.0/ProjectAegis.Ai.Host.Tests.dll
  ProjectAegis.Delegation.UnityAdapter.Tests -> /home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/bin/Debug/net11.0/ProjectAegis.Delegation.UnityAdapter.Tests.dll
  ProjectAegis.MissionEditor.Cli -> /home/username01/cmano-clone/src/ProjectAegis.MissionEditor.Cli/bin/Debug/net11.0/ProjectAegis.MissionEditor.Cli.dll
  ProjectAegis.Delegation.Demo.Tests -> /home/username01/cmano-clone/src/ProjectAegis.Delegation.Demo.Tests/bin/Debug/net11.0/ProjectAegis.Delegation.Demo.Tests.dll
  ProjectAegis.MissionEditor.Cli.Tests -> /home/username01/cmano-clone/src/ProjectAegis.MissionEditor.Cli.Tests/bin/Debug/net11.0/ProjectAegis.MissionEditor.Cli.Tests.dll
/home/username01/cmano-clone/src/ProjectAegis.Delegation.Tests/Ifc/IfcMissionThreadVerificationTests.cs(8,31): error CS0234: The type or namespace name 'Ifc' does not exist in the namespace 'ProjectAegis.Delegation' (are you missing an assembly reference?) [/home/username01/cmano-clone/src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj]
/home/username01/cmano-clone/src/ProjectAegis.Delegation.Tests/Resilience/AgenticResilienceAndSaboteurTests.cs(9,31): error CS0234: The type or namespace name 'Resilience' does not exist in the namespace 'ProjectAegis.Delegation' (are you missing an assembly reference?) [/home/username01/cmano-clone/src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj]
/home/username01/cmano-clone/src/ProjectAegis.Delegation.Tests/Ifc/IfcMissionThreadVerificationTests.cs(348,9): error CS0246: The type or namespace name 'IfcCaseKind' could not be found (are you missing a using directive or an assembly reference?) [/home/username01/cmano-clone/src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj]
/home/username01/cmano-clone/src/ProjectAegis.Delegation.Tests/Ifc/IfcMissionThreadVerificationTests.cs(345,20): error CS0246: The type or namespace name 'IfcContactEvaluationInput' could not be found (are you missing a using directive or an assembly reference?) [/home/username01/cmano-clone/src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj]
/home/username01/cmano-clone/src/ProjectAegis.Data.Tests/Architecture/BaselineRegressionSelectionPolicyTests.cs(3,25): error CS0234: The type or namespace name 'Verification' does not exist in the namespace 'ProjectAegis.Data' (are you missing an assembly reference?) [/home/username01/cmano-clone/src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj]
/home/username01/cmano-clone/src/ProjectAegis.Data.Tests/Architecture/BaselineRegressionSelectionPolicyTests.cs(14,20): error CS0246: The type or namespace name 'GitNexusIndexState' could not be found (are you missing a using directive or an assembly reference?) [/home/username01/cmano-clone/src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj]
/home/username01/cmano-clone/src/ProjectAegis.Data.Tests/Architecture/BaselineRegressionSelectionPolicyTests.cs(112,9): error CS0246: The type or namespace name 'TestRunTier' could not be found (are you missing a using directive or an assembly reference?) [/home/username01/cmano-clone/src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj]
/home/username01/cmano-clone/src/ProjectAegis.Data.Tests/Architecture/BaselineRegressionSelectionPolicyTests.cs(148,9): error CS0246: The type or namespace name 'GitNexusIndexHealthStatus' could not be found (are you missing a using directive or an assembly reference?) [/home/username01/cmano-clone/src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj]
/home/username01/cmano-clone/src/ProjectAegis.Data.Tests/Architecture/BaselineRegressionSelectionPolicyTests.cs(155,9): error CS0246: The type or namespace name 'GitNexusRiskLevel' could not be found (are you missing a using directive or an assembly reference?) [/home/username01/cmano-clone/src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj]
... (42 additional CS0103 errors on missing Verification symbols in BaselineRegressionSelectionPolicyTests.cs) ...

Build FAILED.
    0 Warning(s)
    51 Error(s)
```

### 2. GREEN Verification Summary (`2026-11-23T16:01:34Z`)

- **Production & Docs Files Created:**
  - `src/ProjectAegis.Delegation/Ifc/IfcMissionThreadVerifier.cs` (`DRG-193` — `ProjectAegis.Delegation.Ifc`)
  - `src/ProjectAegis.Delegation/Resilience/AgenticResilienceVerifier.cs` (`DRG-203` — `ProjectAegis.Delegation.Resilience`)
  - `src/ProjectAegis.Delegation/Resilience/SaboteurCalibrationLedgerRecorder.cs` (`DRG-203` — `ProjectAegis.Delegation.Resilience`)
  - `src/ProjectAegis.Data/Verification/BaselineRegressionSelectionPolicy.cs` (`DRG-204` — `ProjectAegis.Data.Verification`)
  - `docs/engineering/baseline-regression-and-test-selection-policy.md` (`DRG-204` — `ARCH-NFR-3`, `VER-06`, `DRG-323`)
- **Docs Files Modified:**
  - `docs/engineering/README.md` (indexed `baseline-regression-and-test-selection-policy.md` under `## Operations`)
  - `docs/architecture/requirements-traceability.md` (added cross-link note for `DRG-204` / `ARCH-NFR-3` / `VER-06`)
- **Build & Multi-TFM `netstandard2.1` Results:**
  - `dotnet build ProjectAegis.sln --no-restore` -> `Build succeeded. 0 Warning(s), 0 Error(s)`
  - `dotnet build src/ProjectAegis.Delegation.UnityAdapter/ProjectAegis.Delegation.UnityAdapter.csproj -c Release -f netstandard2.1 --no-restore` -> `Build succeeded. 0 Warning(s), 0 Error(s)`
- **Full Test Suite (`dotnet test ProjectAegis.sln -v minimal --no-build`):**
  - `ProjectAegis.Ai.Contracts.Tests`: `18 / 18` passed
  - `ProjectAegis.Delegation.Demo.Tests`: `15 / 15` passed
  - `ProjectAegis.Sim.Tests`: `653 / 653` passed
  - `ProjectAegis.Ai.Host.Tests`: `91 / 91` passed
  - `ProjectAegis.Delegation.Tests`: `1,555 / 1,555` passed (`+6` M2 tests; `KCX-01` 100-contact `EvaluateBatch`: `median=0.0644 ms, p95=0.1012 ms < 0.5 ms`)
  - `ProjectAegis.Data.Excel.Tests`: `24 / 24` passed
  - `ProjectAegis.Delegation.UnityAdapter.Tests`: `1,505 / 1,505` passed
  - `ProjectAegis.Data.Tests`: `1,018 / 1,018` passed (`+32` M2 tests)
  - `ProjectAegis.MissionEditor.Cli.Tests`: `175 / 175` passed
  - **Solution Total:** **`5,054 / 5,054` passed** (`0` failed, `0` skipped; `+38` over M1 `5,016` baseline, `> 4,987` Sprint 6 floor)
- **Standing Invariants, Architecture Fences & Python Suites:**
  - `dotnet test ProjectAegis.sln --no-build -v minimal --filter "FullyQualifiedName~ReplayGolden"`: `17/17` (`UnityAdapter.Tests`) + `4/4` (`Delegation.Tests`); `ReplayGoldenSuiteTests`: `6/6`
  - `PlayModeSmokeHarnessTests`: `27 / 27`
  - `UnitySimImportForbidTests|NamespaceSccBaselineTests|CsprojReferenceGraphPinTests`: `31 / 31` (`39/39` including `KillChainTraceabilityMatrixPinsTests` + `Wave4RtmIndexHonestyPinsTests`)
  - `grep -r "17144800277401907079" tests/ data/ | wc -l`: `26`
  - `python3 -m pytest tools/qa-gauntlet/test_saboteur.py -v`: `13 passed`
  - `python3 -B -m unittest discover -s tools -p test_gitnexus_safe_reindex.py -v`: `17 tests OK`
  - Hard-No `git diff --stat 4b5b4ca1` across `DelegationBridge.cs`, `SimulationSession.cs`, `WriteGate/`, `MapPlaceholderPanelHost.cs`, `GlobeMapProductHost.cs`, `tests/regression/`, `DelegationSmoke.unity`, `assets/data/catalog/`, plus `dotnet-ci.sh`, `verify-ci-local.ps1`, `production/test-floor.json`, `mutants/catalog.yaml`, `*.csproj`: empty (`0` diff).

### 3. Iteration 2 Remediation RED Evidence (2026-11-23T16:13:36Z)

- **Timestamp (UTC):** `2026-11-23T16:13:36Z`
- **Working Directory:** `/home/username01/cmano-clone`
- **RED Test Files Updated (Before ANY Production `.cs` or `docs/` Edits):**
  - `src/ProjectAegis.Delegation.Tests/Ifc/IfcMissionThreadVerificationTests.cs` (`DRG-193` — non-coercion of `CaseKind` + `VerifyCaseInvariants`, `Normal => Permitted`, empty-batch fail-closed, order-invariant `ContentDigestSha256` & canonical JSON, duplicate `ContactId` rejection)
  - `src/ProjectAegis.Delegation.Tests/Resilience/AgenticResilienceAndSaboteurTests.cs` (`DRG-203` — real per-condition fault injection via `VerifyPlayerOverridePreservationUnderFault` + structured `PlayerOverrideEvidence` on `AgenticResilienceVerifier.VerifyScenario`, negative override evidence test, fail-closed `SaboteurCalibrationLedgerRecorder` missing/wrong-typed summary fields + `killRate` mismatch)
  - `src/ProjectAegis.Data.Tests/Architecture/BaselineRegressionSelectionPolicyTests.cs` (`DRG-204` — `docs/` maps to both `ProjectAegis.Data.Tests` and `ProjectAegis.Delegation.Tests`, leading `./` normalization, negative integer rejection in `TryParseTestFloorJson` and `EvaluateBaselineGate`)
- **Command & Verbatim RED Output:**
  `export PATH="$HOME/.dotnet:$PATH" && date -u +"%Y-%m-%dT%H:%M:%SZ" && dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj --filter "IfcMissionThreadVerificationTests|AgenticResilienceAndSaboteurTests" -v minimal 2>&1`
  `dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter "BaselineRegressionSelectionPolicyTests" -v minimal 2>&1`

```text
2026-11-23T16:13:36Z
  Determining projects to restore...
  All projects are up-to-date for restore.
  ProjectAegis.Data -> /home/username01/cmano-clone/src/ProjectAegis.Data/bin/Debug/net11.0/ProjectAegis.Data.dll
  ProjectAegis.Sim -> /home/username01/cmano-clone/src/ProjectAegis.Sim/bin/Debug/net11.0/ProjectAegis.Sim.dll
  ProjectAegis.Delegation -> /home/username01/cmano-clone/src/ProjectAegis.Delegation/bin/Debug/net11.0/ProjectAegis.Delegation.dll
/home/username01/cmano-clone/src/ProjectAegis.Delegation.Tests/Resilience/AgenticResilienceAndSaboteurTests.cs(403,20): error CS0246: The type or namespace name 'PlayerOverrideEvidence' could not be found (are you missing a using directive or an assembly reference?) [/home/username01/cmano-clone/src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj]
---
  Determining projects to restore...
  All projects are up-to-date for restore.
  ProjectAegis.Data -> /home/username01/cmano-clone/src/ProjectAegis.Data/bin/Debug/net11.0/ProjectAegis.Data.dll
  ProjectAegis.Data.Excel -> /home/username01/cmano-clone/src/ProjectAegis.Data.Excel/bin/Debug/net11.0/ProjectAegis.Data.Excel.dll
  ProjectAegis.Data.Tests -> /home/username01/cmano-clone/src/ProjectAegis.Data.Tests/bin/Debug/net11.0/ProjectAegis.Data.Tests.dll
Test run for /home/username01/cmano-clone/src/ProjectAegis.Data.Tests/bin/Debug/net11.0/ProjectAegis.Data.Tests.dll (.NETCoreApp,Version=v11.0)
A total of 1 test files matched the specified pattern.
  Failed FastPr_docs_change_selects_both_data_and_delegation_tests [17 ms]
  Error Message:
     Assert.That(plan.SelectedTestProjects, Is.EquivalentTo(new[]
        {
            "ProjectAegis.Data.Tests",
            "ProjectAegis.Delegation.Tests",
        }))
  Expected: equivalent to < "ProjectAegis.Data.Tests", "ProjectAegis.Delegation.Tests" >
  But was:  < "ProjectAegis.Data.Tests" >
  Missing (1): < "ProjectAegis.Delegation.Tests" >

  Stack Trace:
     at ProjectAegis.Data.Tests.Architecture.BaselineRegressionSelectionPolicyTests.FastPr_docs_change_selects_both_data_and_delegation_tests() in /home/username01/cmano-clone/src/ProjectAegis.Data.Tests/Architecture/BaselineRegressionSelectionPolicyTests.cs:line 276

  Failed FastPr_normalizes_leading_dot_slash_without_false_fallback [9 ms]
  Error Message:
     Assert.That(plan.RunFullSolution, Is.False)
  Expected: False
  But was:  True

  Stack Trace:
     at ProjectAegis.Data.Tests.Architecture.BaselineRegressionSelectionPolicyTests.FastPr_normalizes_leading_dot_slash_without_false_fallback() in /home/username01/cmano-clone/src/ProjectAegis.Data.Tests/Architecture/BaselineRegressionSelectionPolicyTests.cs:line 293

  Failed Test_floor_and_baseline_gate_reject_negative_integer_fields [< 1 ms]
  Error Message:
     Assert.That(BaselineRegressionPolicy.TryParseTestFloorJson(negativeFloorJson, out _, out _), Is.False)
  Expected: False
  But was:  True

  Stack Trace:
     at ProjectAegis.Data.Tests.Architecture.BaselineRegressionSelectionPolicyTests.Test_floor_and_baseline_gate_reject_negative_integer_fields() in /home/username01/cmano-clone/src/ProjectAegis.Data.Tests/Architecture/BaselineRegressionSelectionPolicyTests.cs:line 312

Failed!  - Failed:     3, Passed:    32, Skipped:     0, Total:    35, Duration: 60 ms - ProjectAegis.Data.Tests.dll (net11.0)
```

### 4. Iteration 2 GREEN Verification Summary (2026-11-23T16:20:19Z)

- **Timestamp (UTC):** `2026-11-23T16:20:19Z`
- **Production & Docs Files Remediated:**
  - `src/ProjectAegis.Delegation/Ifc/IfcMissionThreadVerifier.cs` (`DRG-193` — removed `CaseKind.LostNode` / `CaseKind.DegradedLink` coercion in `EvaluateBatch` and `BuildEvidenceLedger`; reject duplicate `ContactId` via `ArgumentException`; canonical-sort evaluations by `(ContactId, TargetId, CaseKind, SimTick)` in `BuildEvidenceLedger` for order-invariant SHA-256 digests; fail closed on empty batches; strengthened `VerifyCaseInvariants` incl. `Normal => Permitted`)
  - `src/ProjectAegis.Delegation/Resilience/AgenticResilienceVerifier.cs` (`DRG-203` — replaced bare `bool playerOverridePreserved = true` with structured `PlayerOverrideEvidence` checked inside `VerifyScenario`)
  - `src/ProjectAegis.Delegation/Resilience/SaboteurCalibrationLedgerRecorder.cs` (`DRG-203` — fail closed with `JsonException` on missing/wrong-typed `summary` fields; validate `summary.killRate == $"{caughtDefects}/{caughtDefects + survivedDefects}"`)
  - `src/ProjectAegis.Data/Verification/BaselineRegressionSelectionPolicy.cs` (`DRG-204` — mapped `docs/` to both `ProjectAegis.Data.Tests` and `ProjectAegis.Delegation.Tests`; normalized leading `./`; rejected `< 0` ints in `TryParseTestFloorJson`, `ValidateFloorMonotonicity`, and `EvaluateBaselineGate`; documented `TestSelectionRequest.IsWorkingTreeClean` and `ImpactedSymbols` as advisory metadata)
  - `docs/engineering/baseline-regression-and-test-selection-policy.md` (`DRG-204` — updated §3 `docs/` row to include `ProjectAegis.Delegation.Tests` (`EditorVerbCatalogTests`))
- **Formatting & Builds (`0` errors, `0` warnings):**
  - `dotnet format ProjectAegis.sln --verify-no-changes --no-restore --include <7 M2 C# files>`: exit `0`
  - `dotnet build ProjectAegis.sln --no-restore`: `Build succeeded. 0 Warning(s), 0 Error(s)`
  - `dotnet build src/ProjectAegis.Delegation.UnityAdapter/ProjectAegis.Delegation.UnityAdapter.csproj -c Release -f netstandard2.1 --no-restore`: `Build succeeded. 0 Warning(s), 0 Error(s)`
- **Full Solution Test Suite (`dotnet test ProjectAegis.sln -v minimal --no-build`):**
  - `ProjectAegis.Ai.Contracts.Tests`: `18 / 18` passed
  - `ProjectAegis.Delegation.Demo.Tests`: `15 / 15` passed
  - `ProjectAegis.Ai.Host.Tests`: `91 / 91` passed
  - `ProjectAegis.Sim.Tests`: `653 / 653` passed
  - `ProjectAegis.Delegation.Tests`: `1,565 / 1,565` passed (`+10` over Iter 1 `1,555`; `KCX-01` 100-contact `EvaluateBatch`: `median=0.0677 ms, p95=0.0708 ms, max=0.1181 ms < 0.5 ms`)
  - `ProjectAegis.Data.Excel.Tests`: `24 / 24` passed
  - `ProjectAegis.Data.Tests`: `1,021 / 1,021` passed (`+3` over Iter 1 `1,018`; `BaselineRegressionSelectionPolicyTests`: `35 / 35`)
  - `ProjectAegis.Delegation.UnityAdapter.Tests`: `1,505 / 1,505` passed
  - `ProjectAegis.MissionEditor.Cli.Tests`: `175 / 175` passed
  - **Solution Total:** **`5,067 / 5,067` passed** (`0` failed, `0` skipped; `+51` over M1 `5,016` baseline, `> 4,987` Sprint 6 floor)
- **Standing Invariants, Architecture Fences & Python Suites:**
  - `dotnet test ProjectAegis.sln --no-build -v minimal --filter "FullyQualifiedName~ReplayGolden"`: `17/17` (`UnityAdapter.Tests`) + `4/4` (`Delegation.Tests`); `ReplayGoldenSuiteTests`: `6/6`
  - `PlayModeSmokeHarnessTests`: `27 / 27`
  - `UnitySimImportForbidTests|NamespaceSccBaselineTests|CsprojReferenceGraphPinTests|KillChainTraceabilityMatrixPinsTests|Wave4RtmIndexHonestyPinsTests`: `39 / 39` (`31/31` fences + `8` KCM/RTM pins)
  - `grep -r "17144800277401907079" tests/ data/ | wc -l`: `26`
  - `python3 -m pytest tools/qa-gauntlet/test_saboteur.py -v`: `13 passed`
  - `python3 -B -m unittest discover -s tools -p test_gitnexus_safe_reindex.py -v`: `17 tests OK`
  - Hard-No `git diff --stat 4b5b4ca1` across `DelegationBridge.cs`, `SimulationSession.cs`, `WriteGate/`, `MapPlaceholderPanelHost.cs`, `GlobeMapProductHost.cs`, `tests/regression/`, `DelegationSmoke.unity`, `assets/data/catalog/`, `dotnet-ci.sh`, `verify-ci-local.ps1`, `production/test-floor.json`, `mutants/catalog.yaml`, `*.csproj`: empty (`0` diff); `/home/username01/cmano-clone/.agents` absent.


---

## M3 Track A RED Evidence (`DRG-202`, `DRG-176`, `DRG-205` / `ACC-11` / `VER-04` / `VER-06`)

Command:
```bash
dotnet build src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --no-restore
dotnet build src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --no-restore
```

Verbatim failing RED output before adding `ThreeLayerCombatOracle.cs`, `CombatPictureDeclutterBudget.cs`, `CombatReleaseGateEvaluator.cs`, and `C2CommsLegendTooltipBinder`:
```text
src/ProjectAegis.Data.Tests/Evidence/ThreeLayerCombatOracleTests.cs(32,28): error CS0103: The name 'OracleEvidenceClass' does not exist in the current context
src/ProjectAegis.Data.Tests/Evidence/ThreeLayerCombatOracleTests.cs(41,23): error CS0103: The name 'ThreeLayerCombatOracleEvaluator' does not exist in the current context
src/ProjectAegis.Data.Tests/Evidence/ThreeLayerCombatOracleTests.cs(54,23): error CS0246: The type or namespace name 'SimTruthOracleFact' could not be found
src/ProjectAegis.Data.Tests/Evidence/ThreeLayerCombatOracleTests.cs(64,30): error CS0246: The type or namespace name 'PresentationProjectionOracleFact' could not be found
src/ProjectAegis.Data.Tests/Evidence/ThreeLayerCombatOracleTests.cs(73,28): error CS0246: The type or namespace name 'RenderedAccessibilityOracleFact' could not be found
src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/CombatPictureDeclutterBudgetTests.cs(107,22): error CS0103: The name 'CombatPictureDeclutterEvaluator' does not exist in the current context
src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/CombatReleaseGateEvaluatorTests.cs(37,21): error CS0103: The name 'CombatReleaseGateEvaluator' does not exist in the current context
src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/CombatReleaseGateEvaluatorTests.cs(52,68): error CS0246: The type or namespace name 'Acc11AccessibilityInput' could not be found
src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/CombatReleaseGateEvaluatorTests.cs(56,37): error CS0103: The name 'C2CommsLegendTooltipBinder' does not exist in the current context
src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/CombatReleaseGateEvaluatorTests.cs(84,71): error CS0246: The type or namespace name 'Ver06CiGateInput' could not be found
    0 Warning(s)
    25 Error(s)
```


---

## Track B Headless RED Evidence (`DRG-177` / `DRG-235` `CombatTutorialUsabilityRunnerTests`)

Command:
```bash
dotnet build src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --no-restore
```

Verbatim failing RED output before adding `CombatTutorialUsabilityRunner.cs`:
```text
src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/CombatTutorialUsabilityRunnerTests.cs(12,27): error CS0103: The name 'CombatTutorialUsabilityRunner' does not exist in the current context
src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/CombatTutorialUsabilityRunnerTests.cs(35,30): error CS0246: The type or namespace name 'CombatTutorialStepOutcome' could not be found
    0 Warning(s)
    7 Error(s)
```


---

## Track B Unity PlayMode RED Evidence (`Sprint6SliceG2PlayModeTests`)

Live Unity `:8081` PlayMode run (`tests-run` `ProjectAegis.Unity.Tests.Sprint6SliceG2PlayModeTests`) captured initial RED failure before switching synthetic `ClickEvent` to UI Toolkit keyboard `NavigationSubmitEvent`:
```text
Status: Failed, TotalTests: 5, PassedTests: 4, FailedTests: 1
ProjectAegis.Unity.Tests.Sprint6SliceG2PlayModeTests.SliceG2_DegradedCommsAcceptanceFlow_PointerAndKeyboard_AllSevenStages
  Expected: Executing
  But was:  Planning
```
