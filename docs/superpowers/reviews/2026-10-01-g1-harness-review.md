# G1 and Slice A harness critical-path review — 2026-10-01

**Verdict:** PR #682 is a credible headless DRG-183 acceptance contribution with fresh exact-head Release verification passing; review and integration acceptance remain. PR #662 needs traceability corrections before DRG-234 acceptance. Neither establishes owner Play Mode acceptance or closes Slice A/G1.

## Revision and evidence scope

Read-only GitHub inspection confirmed current main and local `origin/main` at `1902dc1299678ea17f75014406fd9c616e5576bd`. [PR #682](https://github.com/drgaciw/cmano-clone/pull/682) is OPEN/CLEAN at `43b67ee0074f6bb4c152894a9534adcae256ace5`, with base main `1902dc12`. [PR #662](https://github.com/drgaciw/cmano-clone/pull/662) is OPEN/BEHIND at `4af71b63577bd6fc7cfd08054e9282d7269f309f`; its reported base is `954a1d24db5a2e0e29835cd642dc3e96688f39ea`. PR files were read through `git show` at these immutable heads. The initial preflight ran no tests; the subsequent PR-specific verification below did. The coordinator's main/planning-tree gates are distinct evidence and must retain their own revision and local delta.

GitNexus path-qualified context corroborated `TargetabilityAcceptProjection` in `src/ProjectAegis.Delegation/TargetabilityAccept/TargetabilityAcceptProjection.cs`, `KillChainContactStateProjection` in `src/ProjectAegis.Delegation/Projection/KillChainContactStateProjection.cs`, and `C2AuthorityProjector` in `src/ProjectAegis.Delegation/Skills/C2AuthorityProjector.cs`. The selected index is `C:\Users\dgorn\My Projects\cmano-clone\.claude\worktrees\project-requirements-review-f115ae`, indexed at main `1902dc12`. It does not index #682's new test files or this worktree's planning delta.

## DRG-183: acceptance coverage and limits

[DRG-183](https://linear.app/drgamtd-workspace/issue/DRG-183/build-slice-a-targetability-scenario-and-acceptance-harness) requires a valid path and an invalid **or approval-required** path, provenance/freshness/authority/ROE explanations, and replay-stable evidence without firing. The [three harness tests](https://github.com/drgaciw/cmano-clone/blob/43b67ee0074f6bb4c152894a9534adcae256ace5/src/ProjectAegis.Delegation.Tests/TargetabilityAccept/SliceATargetabilityAcceptanceHarnessTests.cs) cover that permitted alternative:

- `Valid_path_reaches_target_and_approval_path_is_withheld_without_firing` checks both Find→Fix→Track→Target chains, Permitted versus Withheld/`WEAPONS_RELEASE_REQUIRED`, and zero engagement, outcome, player-order, magazine and ordnance rows.
- `Explanations_name_provenance_freshness_authority_roe_and_cause` checks fresh/high provenance, WeaponsFree ROE, complete chains and explicit approval reasons.
- `Acceptance_fingerprint_matches_fixture_pin_and_is_replay_stable` repeats projection and compares the checked-in fixture pin.

The [harness](https://github.com/drgaciw/cmano-clone/blob/43b67ee0074f6bb4c152894a9534adcae256ace5/src/ProjectAegis.Delegation.Tests/TargetabilityAccept/SliceATargetabilityAcceptanceHarness.cs) constructs contact-change logs and fixture fire-control/shooter providers, then calls production projections. This proves deterministic composition over declared input, rather than simulated sensing or a live Unity interaction. Existing [projection tests](https://github.com/drgaciw/cmano-clone/blob/1902dc1299678ea17f75014406fd9c616e5576bd/src/ProjectAegis.Delegation.Tests/TargetabilityAccept/TargetabilityAcceptProjectionTests.cs) separately cover stale provenance, missing fire control, WeaponsTight, catalog misses, degraded comms and missing provenance. An additional invalid harness path would broaden coverage, but is not required by the issue's stated alternative.

#682 reports September 27 results: three harness tests, 14 TargetabilityAccept tests, ReplayGolden 6/6 and headless PlayModeSmoke 25/25. Its earlier full run reported 3427/3428 with a Unity plugin DLL export failure; the PR body does not claim a post-fix full count. GitHub shows successful September 27 checks. The fresh run below now supplies exact-head full-suite evidence while preserving this chronology. DRG-208 remains dependent on DRG-183 plus its separate owner acceptance; DRG-219 composition and DRG-213 C2-node evidence are related dependencies, not automatically accepted by this harness.

## Fresh PR-specific verification — 2026-10-01

Created isolated linked worktree `.worktrees/drg-183-review-20261001` at immutable `43b67ee0074f6bb4c152894a9534adcae256ace5`; no source, test or fixture edits. Used `C:\Users\dgorn\AppData\Local\Temp\aegis-sdk-8.0.400\dotnet.exe` (reported SDK **8.0.400**) with process `DOTNET_ROOT` set to that SDK directory and MSBuild `-m:1`.

Restore passed. Ran the exact-head `tools/copy-delegation-assemblies.ps1` to publish Release `netstandard2.1` assemblies into ignored Unity plugin paths. Its process-local `dotnet` wrapper selected the exact SDK, appended `-m:1` and threw on native failure. Before the script's recursive deletion, the absolute `.tmp-unity-plugin-publish` path was checked to be precisely inside this review worktree; the temporary directory was absent afterward. Only generated ignored build/plugin artifacts were added.

| Executed check | Fresh result |
|---|---|
| `build ProjectAegis.sln -c Release --no-restore -m:1 -v minimal` | PASS; **0 warnings, 0 errors** |
| `test ProjectAegis.sln -c Release --no-build --no-restore -m:1 -v minimal` | PASS; **3428 passed, 0 failed, 0 skipped** |
| Delegation tests / `SliceATargetabilityAcceptanceHarnessTests` filter | PASS; **3/3** |
| UnityAdapter tests / `ReplayGoldenSuiteTests` filter | PASS; **6/6** |
| UnityAdapter tests / `PlayModeSmokeHarnessTests` filter | PASS; **25/25**, headless proxy |

All filter runs used Release, `--no-build --no-restore -m:1 -v minimal`. Full-suite totals were Delegation 1111, UnityAdapter 820, Sim 584, Data 773, MissionEditor CLI 116 and Data.Excel 24. All outputs were read and native exits were zero. The earlier plugin export failure did not reproduce with the freshly generated plugins; this closes the missing exact-head full-suite evidence concern for this environment. It does not establish Editor loading or owner interaction acceptance.

Final `git status --porcelain` and tracked diff were empty in the PR worktree. Only the three harness files differ from main `1902dc12`. `DelegationBridge.cs`, CatalogWriteGate paths, scenario policies and regression goldens are unchanged against main. Hash search confirmed Baltic v2 `17144800277401907079`, including patrol world/checkpoint pins. No commit, merge or app update was performed.

## G1: changes required

The [#662 baseline](https://github.com/drgaciw/cmano-clone/blob/4af71b63577bd6fc7cfd08054e9282d7269f309f/docs/superpowers/reviews/2026-09-08-slice-g-baseline.md) correctly separates historical Editor, headless and owner evidence. However, grouped requirements and placeholders do not meet [DRG-234](https://linear.app/drgamtd-workspace/issue/DRG-234/slice-g1-reconcile-unity-requirements-and-evidence-baseline)'s per-criterion test, scenario, artifact, revision/local-delta, delivery-owner and acceptance-owner contract. Replace entries such as “DRG-170 In Review” in the test column and “G2/G3 owners” with concrete tests and named accountable owners; retain missing evidence explicitly.

At main `1902dc12`, [Doc 20](https://github.com/drgaciw/cmano-clone/blob/1902dc1299678ea17f75014406fd9c616e5576bd/Game-Requirements/requirements/20-Command-And-Control-UI.md) defines CMD-01–30, not CMD-31–43. Nevertheless, [CMD-31–37 closeout](https://github.com/drgaciw/cmano-clone/blob/1902dc1299678ea17f75014406fd9c616e5576bd/production/agentic/sprint-ui-maturity-cmd31-37-closeout-2026-08-01.md) and [Wave 2 closeout](https://github.com/drgaciw/cmano-clone/blob/1902dc1299678ea17f75014406fd9c616e5576bd/production/agentic/sprint-ui-maturity-wave2-closeout-2026-08-01.md) record concrete command, datalink, doctrine, overlay, live-edit, performance and roster deliveries. Split delivery evidence from missing canonical definitions. CMD-38/39 remain partial implementation drafts pending owner authorization to append; CMD-40–43 remain audit proposals. Do not label all nine IDs simply proposed/unapproved or infer approval from code.

## Integration recommendation

The immediate critical path is to review #682's final diff against main and record a reviewer disposition before using its verified headless evidence to unblock DRG-208. A different integration revision must retain its own gate evidence. The parallel G1 path should preserve #662 as a historical baseline and add individually owned reconciliation rows; rebase or refresh it before integration because GitHub reports BEHIND. Each new row should identify requirement definition maturity, delivery evidence, test layer/result and owner acceptance independently. A successful CI check or an implementation closeout can support technical delivery, but cannot authorize a requirement append or substitute for the owner exercising the product flow.

Preserve September 8/17 suite counts, MCP outage, GitNexus incompatibility and checkout lag as dated observations. This review's successful graph queries supersede the incompatibility as a present blanket blocker; current Editor availability was not probed. Refresh Doc 11's AME gaps against named package/schema/event/migration criteria and Doc 21 against ADR-011's Excel-primary, human-ApproveBatch contract. Preserve Slice C's pending owner decision, unproven positive live group/BDA flow and live authored coverage. G2/G3 planning can proceed; visual acceptance needs fresh scenario-bound Editor evidence. Cite ADR-010 §2–3, ADR-007 and ADR-001 for presentation boundaries. No runtime or requirement approval is granted here.
