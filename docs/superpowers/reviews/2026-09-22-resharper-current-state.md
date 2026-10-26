# ReSharper current-state assessment — 2026-09-22

**Documentation refresh: complete. Current analyzer regression gate: FAIL — five new warnings.**

This assessment updates the [remediation plan](../plans/2026-09-10-resharper-remediation.md). It preserves the [September 10 delivery closeout](2026-09-10-resharper-closeout.md), HTML report, original SARIF, disposition ledger, and separate `CecNodeRegistration.IsSwarm` source exception. No product code, public API, rule severity, or ledger disposition is changed by this refresh.

## Evidence boundary and current results

- Source revision: `4190fb1b49cd15775b6b995b7c245d73828cd7a2`, with existing uncommitted source/tests/tooling changes. Invocation metadata captures working-tree status; this is not clean-commit certification.
- Actual tool: JetBrains InspectCode **2026.2.1**, SDK **8.0.400**, all frameworks, solution-wide analysis enabled, personal/global settings layers disabled.
- Fresh build: **zero warnings and zero errors**. Analyzer execution: **exit 0**. Comparison: **exit 1**, correctly rejecting five new findings.
- Original inventory: **2,326**. Current comparison: **1,373 absent / 946 accepted matches / 7 deferred / 0 open**, plus **5 new / 0 reintroduced / 0 ambiguous**.
- Historical September 10 delivery: **1,363 fixed / 956 accepted / 7 deferred**, merged in PR #623 at `c7810de4`. Its 3,226-test result remains a historical checkpoint; current verification is recorded below.

The gate's `fixed` counter means an original entry was not matched in the current report. It is not a fresh behavioral review. The ledger itself remains 1,363 fixed, 956 accepted, and seven deferred. Ten accepted findings disappeared from this scan and require provenance review before any reclassification.

## Five new findings — separately scoped cleanup

| Rule | File / scan line | Required follow-up |
|---|---|---|
| `RedundantSuppressNullableWarningExpression` | `src/ProjectAegis.Data.Tests/Scenario/ScenarioDataPathsUnityResolveTests.cs:28` | Remove only the redundant `resolved!` suppression after its null assertion. |
| Same | Same file, line 53 | Remove only the redundant `resolved!` suppression; retain current-directory restoration and assertions. |
| Same | `src/ProjectAegis.Sim.Tests/Scenario/ScenarioPolicyRepositoryTests.cs:27` | Remove only the redundant `profile!` suppression after its null assertion. |
| `RedundantUsingDirective` | Same file, line 1 | Remove the redundant `System` import. |
| Same | Same file, line 2 | Remove the redundant `System.IO` import. |

Both test classes were inspected with GitNexus `impact(direction: upstream, includeTests: true)`: **LOW**, **0 direct dependents**, **0 affected processes**. These are class-level planning results, not a waiver of exact-symbol impact before later edits. The Data test file is untracked and the Sim test file modified in the existing checkout; preserve and coordinate that work. No cleanup is performed here. Acceptance for the later batch is unchanged assertions, focused tests and required repository gates passing, and zero new/reintroduced/ambiguous findings on a fresh CLI comparison.

## Ten disappearing exceptions — provenance review required

All ten were `NotAccessedPositionalProperty.Global` accepted exceptions:

| Source file | Previously accepted entries absent now |
|---|---:|
| `src/ProjectAegis.Delegation/Projection/MagazineLoadoutApplyState.cs` | 9 |
| `src/ProjectAegis.Delegation/Projection/UnitDetailApplyState.cs` | 1 |

Review current property consumers, framework coverage, context fingerprints, and intervening consumer changes against original ledger entries. Neither file has a path-specific commit after the September 10 landing, so declaration changes alone do not explain the change. New consumers are a hypothesis to verify, not a conclusion. A later ledger update must retain original identity and audit history and record the exact consumer evidence and resolution revision. Do not automatically refresh the baseline or treat absence as permission to remove a public record component.

## Seven integrations remain deferred

The [owned RSH-001–007 register](2026-09-10-resharper-followups.md) remains authoritative for owner roles and acceptance conditions. All seven deferred identities still match the fresh report.

| Items | Current composition evidence | Closure requirement |
|---|---|---|
| RSH-001 shooter / RSH-002 authority | `SimplePlayModeSimHost` implements `ISimWorldSnapshot` and `IOrderSink`, without either optional capability. `DelegationBridgeHost.RunTick` calls `SliceAContactFrameBridge.Build(snapshot, Bridge, CatalogReader)` without an explicit shooter provider. | Authoritative production candidates and contact/actor/time-bound authority; eligibility must never grant weapons clearance. |
| RSH-003 coordination | `CoordinationScenarioFacts` is an authored facts implementation, but the host calls the two-argument `CoordinationBridge.Build`, leaving optional facts to snapshot capability lookup. | Wire the concrete authored facts at runtime composition; preserve unknown roles/coverage when absent. |
| RSH-004 advice | The snapshot capability probe remains. `AdviceRuntimeEvidenceBridge.Build` supplies an existing fallback. A working fallback does not establish the optional provider's integration. | Concrete provider with selected-contact identity and exact simulation-time validation; preserve the fallback. |
| RSH-005 unit / RSH-006 sensor / RSH-007 EW status | `StatusFrameBridge.Build` retains three optional snapshot probes. Supplied-capability implementations found in tests do not prove shipped composition. | Read-only production facts with identity/time validation; retain unknown health, emissions, and EW when evidence is absent. |

Later integration acceptance must exercise production composition, missing/supplied capability paths, mismatched identities, stale/future facts, and withheld authority. Follow ADR-010 §2–3, ADR-007, and ADR-001. `DelegationBridge.cs` remains zero-touch; existing `CatalogWriteGate` write paths and Baltic v2 goldens remain protected. No integration or product API change is authorized by this assessment.

## GitNexus evidence and limitations

The repository was selected by absolute path to avoid identically named indexes. Initial incremental indexing failed because the File full-text index was inconsistent. `analyze --repair-fts --index-only` succeeded, followed by `analyze --force --index-only`: **40,296 nodes, 82,300 edges, 954 clusters, 300 flows**, indexed at HEAD `4190fb1b`. Index-only mode excluded agent/skill file injection.

Queries and symbol context covered the Slice A composition path, `SimplePlayModeSimHost`, `SliceAContactFrameBridge`, `CoordinationBridge`, and `StatusFrameBridge`. Fresh post-rebuild context and direct source inspection supersede results from the partially rebuilt database. The graph confirmed the host's two implemented interfaces; source inspection corroborated the actual provider arguments and fallback behavior.

The rebuild warned that C# namespace scanning was truncated at a directory/depth/readability limit. Interface dispatch can also hide concrete consumers; an empty graph process list is not proof that a runtime path is unused. Treat graph reach as bounded evidence and corroborate production composition and contract retention in source.

## Current verification

All required runs completed and their outputs were read. Test execution used sequential project scheduling (`-m:1`) to avoid cross-project CPU contention without disabling tests or relaxing timing assertions.

| Gate | Current result | Evidence |
|---|---|---|
| `dotnet build ProjectAegis.sln` | PASS — 0 warnings, 0 errors | [build.log](../../../artifacts/resharper/refresh-verification-2026-09-22/build.log) |
| `dotnet test ProjectAegis.sln -v minimal -m:1` | **FAIL — 3,351 passed, 1 failed, 0 skipped; 3,352 total** | [tests.log](../../../artifacts/resharper/refresh-verification-2026-09-22/tests.log) |
| PlayModeSmokeHarnessTests | PASS — 26/26 | [smoke.log](../../../artifacts/resharper/refresh-verification-2026-09-22/smoke.log) |
| ReplayGoldenSuiteTests | PASS — 6/6 | [replay.log](../../../artifacts/resharper/refresh-verification-2026-09-22/replay.log) |
| ReSharper tooling unit tests | PASS — 19/19 | [tooling.log](../../../artifacts/resharper/refresh-verification-2026-09-22/tooling.log) |
| Pinned CLI comparison | **FAIL — 5 new warnings**; analyzer itself exited 0 | [comparison](../../../artifacts/resharper/plan-refresh-2026-09-22.sarif.comparison.json) |

The sole solution-test failure is `Player_scripting_defines_do_not_enable_unity_mcp_ready`: the pre-existing modified `unity/ProjectAegis/ProjectSettings/ProjectSettings.asset` contains `UNITY_MCP_READY` in player platform defines (including Standalone). This is a separate Editor-only MCP isolation problem, not an analyzer finding or a change introduced by the documentation refresh. Record a follow-up to restore Editor-only isolation through the supported Unity configuration workflow and rerun the failing test/full suite. Do not edit Unity YAML or relax the assertion as part of this refresh.

The Baltic v2 hash `17144800277401907079` remains present in 18 files under `tests/` and `data/`; the locked replay suite passed. No golden, protected source, Unity settings, or existing working-tree change was modified by this task. The plan diff passes `git diff --check`. Current state is therefore **not release-green**, despite completion of this documentation update.

## Reproduction and evidence links

From the repository root, select the installed SDK 8.0.400 and CLI 2026.2.1 explicitly, then run:

```powershell
./tools/resharper/inspect.ps1 -OutputPath artifacts/resharper/plan-refresh-2026-09-22.sarif -DotnetPath <sdk-8.0.400/dotnet.exe> -ToolPath <cli-2026.2.1/inspectcode.exe> -Check
```

Preserve this dated report; use a distinct output name for subsequent runs. Do not run inspection concurrently with builds/tests in this worktree.

- [Fresh SARIF](../../../artifacts/resharper/plan-refresh-2026-09-22.sarif), [invocation metadata](../../../artifacts/resharper/plan-refresh-2026-09-22.sarif.run.json), [comparison](../../../artifacts/resharper/plan-refresh-2026-09-22.sarif.comparison.json), [build/inspection log](../../../artifacts/resharper/plan-refresh-2026-09-22.log).
- [GitNexus initial failure](../../../artifacts/resharper/gitnexus-plan-refresh-2026-09-22.log), [FTS repair](../../../artifacts/resharper/gitnexus-plan-repair-2026-09-22.log), [successful rebuild](../../../artifacts/resharper/gitnexus-plan-rebuild-2026-09-22.log).
- [Original ledger](../../../tools/resharper/ledger.json) and [CLI policy](../../../tools/resharper/README.md).

Raw analysis/test logs live in ignored local artifacts and are not guaranteed available in another clone. The dated assessment preserves the key results without changing the original evidence. Documentation acceptance means truthful accounting and actionable follow-ups, not a passing current analyzer gate.
