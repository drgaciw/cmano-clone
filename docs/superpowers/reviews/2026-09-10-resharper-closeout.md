# ReSharper remediation delivery closeout

Date: 2026-09-10
Branch: `codex/resharper-remediation-2026-09-10`
Base: `c6dc93b0b061eef6be3867237a9024afd8e4ec90`
Status: **PASS — all local delivery gates complete**

Implementation follows the [2026-09-10 remediation plan](../plans/2026-09-10-resharper-remediation.md), found in the Codex task “Assess product for code improvement.” The [ledger](../../../tools/resharper/ledger.json) contains every original finding and its proof; the [verification manifest](2026-09-10-resharper-verification.json) records evidence hashes. Detailed logs and reviewed batch patches remain under `artifacts/resharper/remediation-2026-09-10/` in this worktree.

## Outcome

The original controlled inventory is fully dispositioned:

| Status | Count |
|---|---:|
| Fixed | 1,363 |
| Accepted exception | 956 |
| Deferred capability | 7 |
| Open | 0 |
| **Original total** | **2,326** |

`final.sarif.comparison.json` passes with **0 new, 0 reintroduced, and 0 ambiguous** findings. One remediation-created finding, `CecNodeRegistration.IsSwarm`, is recorded separately in `ledger.sourceExceptions`; it is outside the original 2,326. The public positional component remains for construction, deconstruction, equality, and `ToString`; only its unread private runtime copy was removed.

The final [product-source review](2026-09-10-resharper-source-review.md) and [architecture checklist](2026-09-10-resharper-architecture-review.md) are **PASS**. No concrete behavior, contract, presentation-boundary, or protected-invariant regression was found.

## Behavior and contract changes

- Rollback now captures a deep undo snapshot, restores the full captured document, and reports an unknown snapshot ID without mutation. Focused tests cover successful and missing-ID paths.
- `AdjudicationWorkspace.ComputeDiff` still rejects both null snapshots and now reports the correct `before` or `after` `ArgumentNullException.ParamName`; the test-first red failure and two green focused tests are recorded.
- Hindsight request construction preserves successful request content/order and now disposes the request if content creation throws.
- Cast cleanup preserves signed-to-`uint` normalization; nullable patterns preserve polarity; tuple arrays retain nullable target types.
- Removed replay/timeline checks do not suppress mutation calls or change arguments. Switch-arm reductions preserve every current result, including the distinct `WEAPON_LAUNCH` case.
- Explicit defaults remain where they pin security/authoring roles, input fallback, tick cadence, simulation modes, and golden/fixture policy. Seven reviewed redundant defaults were removed.
- Non-null return annotations match producers. Eleven caller null-forgiving operators were removed after the fresh scan; the first `UnitDetailBridge.BuildSelected` overload was strengthened separately with exact LOW/2 impact. The shared nullable `EnrichAttackMenu`/`BuildPrimary` path remains nullable.
- Unused cleanup retains side-effecting calls and public contracts. Culture-sensitive assertions use invariant/cross-culture proof. Documentation repairs preserve runtime bindings; one test namespace now matches its directory, with all six tests still discovered.

## Analyzer control and CI tooling

- Original SARIF SHA-256: `C6808CB407DC6DE4EDBB2BF9D94D0B917D3C0D0A0A02F987D581318F012C0B83`.
- A controlled InspectCode 2026.2.1 reproduction matched all 2,326 rule/path/location/message entries across supported frameworks.
- The SARIF gate compares identity and multiplicity, validates framework tags and per-entry audit fields, rejects malformed/failed analyzer reports, and fails ambiguous mixed-disposition groups. It does not count-match or regenerate the baseline automatically.
- Tooling evidence: **19/19 tests pass** (`tooling-final-tests.log`). The Windows workflow pins the SDK/tool, builds before inspection, supplies explicit settings/frameworks, uses isolated output, and calls the gate in check mode.
- Rule-specific source batches used reviewed identities and mandatory pre-edit GitNexus impacts. HIGH/CRITICAL symbols received focused or replay evidence. No final `detect_changes` claim is made: that check is required before a commit, and no commit is requested; the available index represents main rather than this isolated worktree.

## Identity migrations

Three explicit provenance cases prevent line-ending or adjacent-edit churn from hiding findings:

- Initial CRLF-to-LF reproduction differences are mapped one-to-one in `original-to-reproduced.json`; rule/path/location/message identity remains controlled.
- RSH-002 capability check: adjacent `TargetId(shooter!)` cleanup shifted the unchanged finding from `A8BB5754…55DB` to `3BE444A4…FC4F`. The original fingerprint and deferred audit fields remain, with both identities recorded in history.
- CLI Metadata guard: removing the preceding `ExportDocument?` qualifier shifted the required `Metadata?` finding from original ledger fingerprint `296A863C…EC6` occurrence 2 to single active SARIF fingerprint `AE4E1755…6894`. Accepted status and original occurrence metadata remain preserved.

No general matching algorithm was relaxed for either manual migration.

## Verification completed

The observed starting suite was 3,216 tests; the final suite has 3,226, including ten added regression/contract cases. Both Debug and Release builds have zero warnings and errors. Reproduction commands and SDK/tool selection are documented in [tools/resharper/README.md](../../../tools/resharper/README.md).

- Debug batch endpoint: **3,226 tests passed / 0 failed**, **24/24 PlayModeSmoke**, **17/17 ReplayGolden filter** including the locked six-test suite.
- Local Release CI (`final-ci-local.log`): restore PASS; catalog import **67/67**; tracked `*.db3` check PASS; Release build **0 warnings / 0 errors**; full suite **3,226/3,226**; ReplayGoldenSuite **6/6**; PlayModeSmoke **24/24**.
- Unity plugin: Release `netstandard2.1` publish completed; **14 DLLs** copied and verified by SHA-256 (`final-plugin-copy.json`, `final-plugin-check.log`). No `net8.0` output was copied into Unity plugins.
- `DelegationBridge.cs`, `CatalogWriteGate.cs`, `tests/regression`, and Unity assets/asmdefs have zero product diff. Baltic v2 hash `17144800277401907079` remains unchanged.
- `git diff --check` is clean; no proprietary CMO database is tracked.
- Main-checkout preservation: `main-preservation-check.json` reports `mainTrackedDiffUnchanged: true`; normalized digest remains `bf56a937ce6c236c5f1b3d0affcb129688e5a8bdb0f1adbe291046a9eee06621`.
- Final invariant manifest: `final-invariants.json` confirms the protected paths, **26** locked-hash reference lines, and **606** changed source files. The reviewed source patch SHA-256 is `763741437AF7E5791A5FAAB9CA4DF7451C6880389BF95A2C5F096267A9833F54`.

## Deferred capability integrations

The [seven owned follow-ups](2026-09-10-resharper-followups.md) retain `SuspiciousTypeConversion.Global` checks that remain deliberately visible and are planned as separately scoped runtime-composition work:

- `SliceAContactFrame`: sensor-to-shooter source and Slice A authority source.
- `CoordinationBridge`: coordination facts provider.
- `AdviceBridge`: advice evidence provider.
- `StatusFrame`: electronic-warfare, sensor, and unit status providers.

Each entry has a maintainer role and the same review condition: resolve only when a concrete production provider is wired and runtime-composition acceptance is verified. These are not fixed, suppressed globally, or omitted from the ledger.

## Final analyzer gate

The final pinned InspectCode wrapper run completed successfully (`final.sarif`; validation session 42815, exit 0). Its identity comparison passes with **1,363 fixed / 956 accepted / 7 deferred / 0 open**, and **0 new / 0 reintroduced / 0 ambiguous** findings. Every original finding has a reviewed disposition; the seven deferred capability integrations remain explicitly visible.

No commit, push, pull request, or remote CI run has been performed.

## unity-csharp-architect — PR finish (UCA-M4)

**Checklist:** [pr-finish.md](../../../production/agentic/skills/unity-csharp-architect/checklists/pr-finish.md)
**Skill:** [unity-csharp-architect](../../../production/agentic/skills/unity-csharp-architect/SKILL.md)
**ADRs:** ADR-010 §2–3, ADR-007, ADR-001, ADR-006, ADR-011
**Verdict:** PASS

**Evidence:** Existing snapshot/projection and command seams are preserved; no MonoBehaviour, Editor, assembly edge, or protected bridge change. Full Debug/Release tests and smoke pass. Release `netstandard2.1` plugin refresh is verified. The linked architecture review covers every applicable checklist row and explains N/A items. This local review block is ready for a future PR body; no PR was requested.
