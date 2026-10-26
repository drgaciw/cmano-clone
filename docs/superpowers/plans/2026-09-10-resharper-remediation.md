# Project Aegis — ReSharper remediation plan

## Current status — refreshed 2026-09-22

**Original remediation: COMPLETE. Current working-tree regression gate: FAIL (five new warnings).**

Phases 0–6 below were completed on 2026-09-10 and landed through [PR #623](https://github.com/drgaciw/cmano-clone/pull/623), squash commit `c7810de42df72c8df13bffa02fca1680d3bb0bc4`. Their instructions, estimates, and baseline descriptions are retained as historical implementation context, not pending work. The original closeout recorded 1,363 fixed, 956 accepted exceptions, seven deferred integrations, and zero new/reintroduced/ambiguous findings.

The [September 22 current-state assessment](../reviews/2026-09-22-resharper-current-state.md) supersedes that checkpoint only for current-state claims. Pinned ReSharper CLI 2026.2.1 with SDK 8.0.400 successfully analyzed all supported frameworks after a clean build (zero warnings/errors). Its comparison reports:

| Current comparison measure | Count |
|---|---:|
| Original entries absent from this scan (`fixed` in gate output) | 1,373 |
| Matching accepted exceptions | 946 |
| Matching deferred integrations | 7 |
| Original open entries | 0 |
| New findings, outside the original inventory | 5 |
| Reintroduced / ambiguous | 0 / 0 |

This evidence describes HEAD `4190fb1b49cd15775b6b995b7c245d73828cd7a2` **plus uncommitted changes**, not an isolated clean commit. The stored ledger still records the original 1,363/956/7 dispositions. Ten accepted entries no longer match the scan; they are provenance-review candidates, not ten newly verified fixes.

### Active follow-up queue

1. **Narrow test cleanup:** three redundant null-forgiving operators and two unused imports across `ScenarioDataPathsUnityResolveTests` and `ScenarioPolicyRepositoryTests`. GitNexus upstream analysis reports LOW risk, zero direct dependents, and zero affected execution flows for both classes. Preserve assertions and existing working-tree edits; rerun impact on the exact methods before a later implementation.
2. **Exception provenance review:** nine positional properties in `MagazineLoadoutApplyState.cs` and one in `UnitDetailApplyState.cs`. Confirm current consumers and identity continuity before changing any ledger disposition. Keep original identities and audit history.
3. **Runtime capability integrations:** retain RSH-001–007 in the [owned follow-up register](../reviews/2026-09-10-resharper-followups.md). Current source/graph review does not establish completed production provider wiring. Advice's working fallback is distinct from its optional evidence provider. Close an integration only with concrete production composition and acceptance evidence.
4. **Verification-discovered checkout issue:** the full suite reports 3,351 passed and one failure because existing modified player settings enable `UNITY_MCP_READY`. Restore Editor-only MCP isolation in a separate Unity configuration task; do not weaken the assertion. Current smoke (26/26), locked replay (6/6), tooling (19/19), and build gates pass. See the assessment for exact commands and logs.

The refresh changes documentation only. It does not fix these warnings, implement capabilities, change public APIs, regenerate the baseline, demote rule families, or authorize protected bridge/catalog edits. The September 10 closeout and HTML report remain historical artifacts. Current verification and analysis limitations are recorded in the dated assessment.

## Historical implementation plan — completed 2026-09-10

## Recommendation and scope

Remediate in small, rule-specific changes, starting with potential behavior problems and establishing a reproducible baseline before bulk edits. Success means every finding has a reviewed disposition and new warnings are prevented; it does not require deleting intentional contracts or suppressing entire rule families to reach zero.

This plan reviews the existing SARIF and selected source examples. It does not validate every finding as a defect, rerun the analyzer, or change project code. Estimates below are planning estimates, not measured implementation times.

## Baseline evidence

- Report: `C:\Users\dgorn\cmano-clone\cmano-clone\artifacts\resharper\inspectcode.sarif`
- Report modified: 2026-09-10 11:56:33 UTC.
- SHA-256: `C6808CB407DC6DE4EDBB2BF9D94D0B917D3C0D0A0A02F987D581318F012C0B83`.
- Current checkout HEAD at review: `c6dc93b0b061eef6be3867237a9024afd8e4ec90`. The checkout has uncommitted changes; this is not proof of the exact source revision analyzed.
- 2,326 warning results, 42 rule IDs, 751 affected files. All 2,326 are unique by rule, path, line, and column; they should not be divided by the number of target frameworks. Different rules can still describe the same underlying issue.
- All reported locations are under `src/`. This report is not a complete Unity Assets analysis.
- Existing wrapper uses `--no-build`, defaults to WARNING, does not explicitly set solution-wide analysis, and checks tool exit status without gating on findings. It found warnings successfully; it is not yet a regression gate.

### Work inventory

| Workstream | Findings | Treatment |
|---|---:|---|
| Redundant syntax and expression simplifications | 1,246 | Narrow, reviewed transformations with compilation and tests |
| Nullability and precondition contracts | 416 | Verify runtime boundaries before changing guards or annotations |
| Unused members, variables, parameters, and collections | 522 | Prove absence of serialization, external, Unity, reflection, and record-contract use |
| Documentation, naming, and namespaces | 116 | Repair documentation; preserve required framework namespaces and public contracts |
| Behavior, conversion, numeric, culture, and duplication review | 26 | Investigate individually; add regression coverage where behavior changes |
| **Total** | **2,326** | Every result receives a disposition |

### Distribution by project

| Project suffix | Findings |
|---|---:|
| Delegation | 701 |
| Data | 415 |
| Delegation.UnityAdapter.Tests | 262 |
| Delegation.UnityAdapter | 241 |
| Sim | 189 |
| Data.Tests | 164 |
| MissionEditor.Cli | 110 |
| Delegation.Tests | 92 |
| MissionEditor.Cli.Tests | 64 |
| Sim.Tests | 60 |
| Data.Excel.Tests | 18 |
| Sim.Benchmark | 5 |
| Data.Excel | 4 |
| Delegation.Demo | 1 |

## Phase 0 — Reproduce and control the baseline

Estimated effort: 0.5–1 engineering day.

1. Preserve the original SARIF and hash before another run overwrites it. Record source revision, uncommitted patch state, tool version, SDK/MSBuild version, analysis settings, target frameworks, and command line.
2. Use an isolated branch or worktree for remediation. Carry in only the agreed source state; do not inadvertently remediate a different revision from the baseline.
3. Build the solution, then repeat the current inspection settings. Investigate differences from 2,326 before accepting a new baseline. Analyze all supported frameworks; a net8-only pass would miss netstandard compatibility issues.
4. Make solution-wide analysis and personal/global settings handling explicit for reproducibility. If enabling additional analysis changes results, record the extra findings separately from this original inventory.
5. Export a disposition ledger keyed by rule ID, normalized path, and SARIF context fingerprint, with location fallback. Include message, framework tags, owner, status, rationale, verification, and resolution revision. Handle fingerprint collisions; line numbers alone are unstable after edits.
6. Establish a small shared settings policy without mass demotions. Accepted exceptions must identify a specific contract or limitation, an owner, and a review condition.

Exit: a repeatable inspection and auditable original-to-current inventory.

## Phase 1 — Investigate behavior and integration findings

Estimated effort: 1–2 engineering days, excluding newly discovered feature work.

Review the 26 findings in this workstream first. These are candidates for investigation, not 26 confirmed bugs.

- **Seven suspicious conversions:** inspect `SliceAContactFrame.cs:68,77`, `CoordinationBridge.cs:33`, `AdviceBridge.cs:22`, and `StatusFrame.cs:98,118,138`. ReSharper reports no solution type implementing both snapshot and capability interfaces. A text search of Unity runtime scripts also found no occurrences of these capability names. Trace actual snapshot creation and consumers across the full repository, including Unity, before deciding whether this is an intentional extension point or missing integration. Exercise supplied and missing capability paths. Preserve fail-closed authority behavior. If integration is absent, create a separately scoped defect rather than deleting the checks.
- **Swarm constants:** `SwarmOffensiveEffect.cs:45–46` produces three constant/unreachable warnings, plus one of the ten float comparisons. `ScaleFactorPower = 1.0` and `MinLivingScale = 0.0` make branches redundant under current tuning. Verify intended tuning semantics and deterministic outputs before simplifying. Do not silently alter the tuning model or simulation results.
- **Unreachable switch arm:** `CoordinationCommandBridge.cs:141` maps Reattack to `engage` after Reattack is explicitly rejected. Remove the redundant arm only while preserving that rejection; add or retain a regression test proving Reattack cannot enqueue an order.
- **Ten float equality findings:** review exact replay/time identity, sentinel checks, and geometry comparisons separately. Do not replace all equalities with epsilon comparisons. Test numeric boundaries when semantics change.
- **Three culture findings:** all occur in `PlatformCatalogViewerTests.cs`. Make expected formatting explicit and verify under at least two cultures where a formatting assumption matters.
- **Two duplicated-statement findings:** extract only when shared behavior and ownership are clear; avoid incidental architectural changes.

Exit: each candidate is fixed with evidence, recorded as intentional, or linked to a concrete scoped follow-up. No unresolved behavior defect is hidden by suppressions.

## Phase 2 — Apply narrow cleanup batches

Estimated effort: 1–2 engineering days.

The 1,246 syntax findings comprise: redundant qualifiers 816; usings 161; explicit default arguments 141; type-pattern-to-null-check 47; casts 41; full-member `with` expressions 17; switch arms 9; explicit arrays 8; jumps 3; using-resource initialization 2; type arguments 1.

Start with qualifiers and usings, then review the remaining families individually. Begin with a 20–30-file pilot. Subsequent batches should cover one rule family in one project and remain reviewable; avoid solution-wide cleanup combining unrelated transformations.

Validate binding and overload resolution after qualifier/cast removal. Preserve evaluation order and side effects. Keep explicit defaults when they intentionally pin behavior against future API default changes. A full-member `with` rewrite needs attention to record cloning and any additional state.

Prioritize Delegation and Data for volume, while avoiding files concurrently being edited. Keep behavioral changes out of cleanup batches so regression causes are identifiable.

Exit: reviewed cleanup findings disappear from rescans, with tests and required protected-file exclusions maintained.

## Phase 3 — Reconcile nullability contracts

Estimated effort: 2–3 engineering days.

The 416 findings comprise: non-null coalescing 162; redundant null-forgiving operators 123; conditions constant under nullable contracts 94; conditional access on non-null values 23; return type can be non-null 3; constant null coalescing 2; parameters used only for preconditions 9.

Classify each affected API as internal trusted code or a runtime boundary. JSON, SQLite, workbook imports, Unity, reflection, and callers without nullable annotations can violate static assumptions. Correct inaccurate annotations first. Retain deliberate validation and fallback behavior with narrowly justified exceptions where needed. Never remove a guard solely because an annotation says null is impossible.

Add focused malformed-input and null-boundary tests for changed behavior. Recheck both frameworks and affected import/export paths.

Exit: annotations match runtime contracts and rejected-input behavior remains intentional.

## Phase 4 — Resolve unused-member findings

Estimated effort: 2–4 engineering days.

The 522 findings comprise: unread positional properties global/local 325/25; unused accessors global/local 81/45; unused variables 18; redundant assignments 10; unused local members 6; unused parameters 6; collections never updated/queried 2/2; unused return value 1; unaccessed variable 1.

Start with local variables and demonstrably unused private code, preserving initializer side effects. Public records and DTOs require broader contract review. For example, `SkillEnvelope.cs` contains 22 findings in evidence, authority, replay, and skill-envelope contracts. Lack of a C# read is insufficient evidence for removal: record equality, deconstruction, serialization, consumers outside this solution, and schema compatibility can depend on these members.

Review JSON/workbook contracts, Unity bindings, reflection, public API use, and fixtures before deleting members or setters. Keep intentional contract members and document the reason using the narrowest effective annotation or suppression. Group actual API removals separately and validate serialization round trips and consumer compatibility.

Exit: every retained member has a documented consumer or contract purpose; removed members have evidence supporting removal.

## Phase 5 — Documentation and naming

Estimated effort: 0.5–1 engineering day.

Address 80 invalid XML comments, 30 naming findings, five namespace findings, and one unresolved text reference.

Four namespace findings are `Polyfills/IsExternalInit.cs` in Data, Delegation, UnityAdapter, and Sim. These must retain `System.Runtime.CompilerServices`; add precise exceptions rather than moving them into project namespaces. Review the fifth namespace finding in `ScenarioDocumentEditorLiveValidationTests.cs` normally.

Correct broken XML references and parameter documentation. Preserve external field names and intentional test naming conventions. Public renames require consumer analysis and must not alter serialized names unintentionally.

Exit: documentation is accurate and naming exceptions explain real contracts.

## Phase 6 — Prevent regressions and close the ledger

Estimated effort: 0.5–1 engineering day.

Add a CI comparison that parses SARIF and fails on newly introduced unaccepted warnings or analyzer execution failure. A tool exit code of zero is not sufficient: the initial run succeeded with 2,326 warnings. Match findings by context-aware identity, not just total count, so fixing ten findings cannot conceal ten new ones.

Keep original, fixed, accepted-exception, deferred, and new counts distinct. Do not automatically regenerate the baseline after every run or lower whole rule severities to erase debt. Expire or remove obsolete exceptions as consumers and contracts change. Declare remediation complete only when there are no unresolved items in the agreed scope; deferred items remain visibly open.

## Validation and repository constraints

Before editing functions or classes, follow the repository's GitNexus impact-analysis requirement. For UnityAdapter, projection, and presentation changes, load the project Unity/C# architecture skill and its finish checklist during implementation.

- Preserve `DelegationBridge.cs` zero-touch protection: the report contains 16 findings there. Track them as constrained findings; do not include them in automated edits.
- Preserve CatalogWriteGate existing write paths: four findings occur in `CatalogWriteGate.cs`. Resolve only within its extend-only contract or document a scoped exception.
- Preserve Baltic v2 replay hash `17144800277401907079`, six replay golden tests, and Baltic v3 isolation.
- For each completed implementation batch, run and read the required repository checks: `dotnet build ProjectAegis.sln`; `dotnet test ProjectAegis.sln -v minimal`; and the UnityAdapter test project with `--filter PlayModeSmokeHarnessTests`.
- Required documented floors are at least 1,638 solution tests with zero failures and at least 20 PlayModeSmoke tests passing. Record actual starting counts and do not regress them. Build must have zero errors and warnings.
- Add targeted tests only where needed to verify behavioral or boundary changes. For pure cleanup, existing coverage plus compilation and inspection comparison is usually the relevant evidence.
- Run InspectCode after a successful fresh build and compare identities against the baseline. Separate analysis configuration changes from source-remediation batches.
- Do not replace replay goldens simply to make a remediation change pass. Investigate any changed result.

## Delivery status

**Delivered:** original phases 0–6 completed and merged through PR #623 on 2026-09-10. The original **8–14 engineering day** budget is historical, not an estimate of remaining work.

The active queue above separates five new test-code warnings, ten exception-provenance reviews, and seven already-owned runtime integrations. No new implementation estimate or delivery date is assigned by this documentation refresh. Future batches must preserve finding identities, reviewed dispositions, focused diffs, and verification/rescan evidence.
