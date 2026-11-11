# Parallel story wave — verification and handoff

Date: 2026-11-01. Base: `b5853f6d66d6ed73f43bd691105f68f6aa57fcc4`.

User requested parallel local agents, subagents, and Fly.io Sprites. Three independent implementations were delivered in isolated worktrees, with independent per-track review and a combined review checkout. Existing uncommitted gameplay changes in the main checkout were excluded. No commit, push, PR submission, merge, or owner acceptance occurred; Linear issues remain In Progress.

## Delivered surfaces

| Story | Result | Worker worktree | Focused proof |
|---|---|---|---|
| [DRG-254](https://linear.app/drgamtd-workspace/issue/DRG-254) | Immutable per-agent chosen-intent, denial and explicitly attributed intervention metrics over `IReadOnlyOrderLog`; unknown attribution preserved | `.worktrees/orch-drg-254-20261101` | 11 tests; runtime RED then GREEN; independent spec/quality/architecture PASS |
| [DRG-410](https://linear.app/drgamtd-workspace/issue/DRG-410) | Headless allocation preview using supplied policy/engagement facts and shared shooter/mount ammunition budgets | `.worktrees/orch-drg-410-20261101` | 20 tests; two validation-mutation RED checks; restored GREEN; independent spec/quality/architecture PASS |
| [DRG-257](https://linear.app/drgamtd-workspace/issue/DRG-257) | Validated experiment JSON/schema/sample, existing batch-runner wrapper, stable real fingerprints/CSV, standalone CLI and usage guide | `.worktrees/orch-drg-257-20261101` | 30 adapter tests and nine real CLI process tests; empty-path regression RED/GREEN; independent spec/architecture PASS, quality approved with optional suggestions |

DRG-410 follows the CMANO-stamped preview-only scope. Command submission, UI integration and replaying committed allocations remain outside this delivery. DRG-257 is the authorized seed-only infrastructure stub; optional doctrine/ROE IDs are opaque metadata and do not alter runtime policy.

Worker branches use Linear's generated names. The combined uncommitted result is in `.worktrees/orch-review-20261101`, detached at the pinned base, with eighteen new product/test/document files. All existing tracked and staged diffs in that checkout are empty. Files were copied through explicit per-story allowlists; no protected hub, existing policy, golden, project or solution file was edited. New standalone CLI/test projects are outside the solution and were verified separately.

## Required gates — RUN and READ

Exact SDK: **8.0.400**, installed at `%TEMP%/aegis-orchestration-baseline-20261101/dotnet-8.0.400/dotnet.exe`, selected through process-local environment only.

| Gate | Clean baseline | Combined final result |
|---|---:|---:|
| `dotnet build ProjectAegis.sln` | 0 warnings / 0 errors | **0 warnings / 0 errors** |
| `dotnet test ProjectAegis.sln -v minimal` | 4058 passed / 0 failed / 0 skipped | **4119 passed / 0 failed / 0 skipped** |
| `PlayModeSmokeHarnessTests` | 26/26 | **26/26** |
| `ReplayGoldenSuiteTests` | 6/6 | **6/6** |
| Standalone experiment CLI test project build | N/A | **0 warnings / 0 errors** |
| Standalone experiment CLI process tests | N/A | **9/9** |
| `netstandard2.1` plugin publish/copy | PASS | **PASS** |
| Baltic v2 hash | Preserved | **`17144800277401907079` preserved** |

Final project counts: Sim 653; Delegation 1346; Data.Excel 24; MissionEditor.Cli 119; UnityAdapter 1142; Data 835. The increase is exactly 61 solution tests: 11 + 20 + 30. The nine CLI tests are additional and outside the solution total.

Commands ran from the combined review worktree:

```powershell
dotnet build ProjectAegis.sln -v minimal
./tools/copy-delegation-assemblies.ps1
dotnet build tools/experiment-runner/tests/ProjectAegis.ExperimentRunner.Tests.csproj -v minimal
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter ReplayGoldenSuiteTests
dotnet test tools/experiment-runner/tests/ProjectAegis.ExperimentRunner.Tests.csproj --no-build -v minimal
rg -n '17144800277401907079' tests data
git diff --exit-code
git diff --cached --exit-code
```

The plugin bootstrap's resolved `.tmp-unity-plugin-publish` deletion target was verified inside each exact worktree and checked against reparse points before invocation. Only ignored generated DLLs were published; no binaries belong to the source patches. A fresh-checkout missing-plugin test failure was retained in the baseline logs, recovered through this documented bootstrap, and followed by a fully green baseline rerun.

## Review dispositions and limits

- DRG-410's first review found a masked invalid-fact test: unrelated missing legs could satisfy its exception assertion. The fix uses complete matching facts and checks the exception parameter. Removing each intended validation guard now produces the expected test failure; both production files were restored byte for byte before the final GREEN run. Re-review closed the finding.
- DRG-257's numeric lexical restriction is now explicit in the usage guide: JSON Schema may accept mathematical integers such as `1.0`; the parser requires integer token spelling such as `1`. This is a documentation clarification, with no runtime policy change.
- DRG-257's reviewer suggested rejecting identical input/output paths. This remains optional: the CLI explicitly replaces the requested output file and has no separate definition-file preservation contract. Use distinct definition and CSV paths. No new preservation requirement was silently treated as delivered.
- The experiment wrapper delegates unchanged CSV formatting. The later scoped instruction excluded tests that merely mirror the existing exporter; real output, fingerprints, ordering, duplicate seeds and culture stability were tested.
- All work is headless. Proxy smoke is not live Editor evidence or owner acceptance. No new Unity scene, prefab, layout or host was edited.
- GitNexus reported the root index 138 commits behind. Read-anchor impact was disclosed: `DecisionLog` CRITICAL, 327 direct dependents/eight grouped flows; it remained read-only. Existing symbols were not edited; new-symbol lookup misses were recorded as unknown, never safe-zero evidence. No commit occurred, so pre-commit `detect_changes` remains a gate for any later commit.
- Hindsight retention failed with PostgreSQL shared-memory exhaustion. Local briefs, reports, patches and this handoff preserve the session state; no infrastructure cleanup was attempted.

## unity-csharp-architect — PR finish (UCA-M4)

**Checklist:** `production/agentic/skills/unity-csharp-architect/checklists/pr-finish.md`  
**Skill:** `production/agentic/skills/unity-csharp-architect/SKILL.md`  
**ADRs:** ADR-010 §§2–3, ADR-007, ADR-001; ADR-019 for read-only AAR logs.

**Verdict:** PASS

**Evidence:**
- Presentation/analysis reads immutable facts or `IReadOnlyOrderLog`; no log/world mutations or live authority access.
- Command path: N/A for allocation preview and AAR analysis. Experiment execution uses the existing headless batch runner, with no new player-command or hotpath integration.
- Assemblies: existing Delegation and UnityAdapter APIs; additive standalone CLI → UnityAdapter and standalone tests → CLI. No UnityEngine dependency, asmdef change, cycle, protected bridge/session edit or SQLite write path.
- MonoBehaviour/DI/frame allocation/Editor: N/A; no host or scene changes, only offline projections and experiment execution.
- Tests: combined required gates above; meaningful feature tests and independent reviews. Live Play Mode not claimed.
- Plugins: compatible `netstandard2.1` publish/copy succeeded; generated DLLs excluded from source patches.

## Orchestration state and follow-on gates

The authoritative [dispatch plan](../../docs/superpowers/plans/2026-11-01-parallel-story-dispatch.md) links Notion design and Linear delivery records. DRG-408's missing blocked-by DRG-412 relation and contradictory dependency sentence were repaired and independently read back. Existing DRG-412/417 worktrees were preserved; no competing implementation was started. Alert integration, unstamped DRG-712 UI work, authority-hub changes and gated save implementation remain held for their own prerequisites.

Initial Fly.io dispatch was blocked on authentication. Follow-through: CLI login succeeded for `drg-amtd`; dedicated Sprite `aegis-linux-gates-20261101` was created. The trial permits three total Sprites; the existing `my-first-sprite` and `open-crm-cpq` environments were preserved, and a second new Sprite could not be created. No billing or deletion action was taken.

Two agents verified independent remote checkouts on the dedicated Sprite: `sprite_linux_gates` used `/home/sprite/aegis`, and `sprite_cli_verify` used `/home/sprite/aegis-cli`. Both received hash-verified pinned source plus the eighteen-file reviewed patch. Exact SDK 8.0.400 and native ICU were installed on Ubuntu 26.04; the full-gate lane also hydrated tracked public Git LFS assets from a verified object cache. No local authentication files or remote model credentials were copied. Cloud cap five and combined effective cap four–six remained in force.

### Remote Linux results — PASS

| Check | Result |
|---|---:|
| Solution build | 0 warnings / 0 errors |
| Full solution | 4119 passed / 0 failed / 0 skipped |
| Proxy smoke | 26/26 |
| Canonical replay | 6/6 |
| Broader replay filter | 17/17 |
| Standalone CLI build/process tests | 0 warnings/errors; 9/9 |
| Focused new feature tests | Experiments 30/30; AAR/allocation 31/31 |
| Actual CLI sample | Seeds 7,8,7; repeated-seed fingerprints equal |
| Optional policy-label metadata | Original, absent and replacement labels produce byte-identical CSV |
| Protected content | Hash preserved; bridge/write-gate match HEAD; empty tracked/staged content diff |

Both remote execution jobs completed with exit code zero. The focused CLI lane additionally verified all 6789 tracked files byte-for-byte against the pinned index (symlink payloads included); its initial LFS/stat status artifacts did not indicate content changes. Source changes remain exactly the eighteen reviewed additions. The Sprite is retained for future work; no destroy operation occurred. Local reports and pulled evidence are in `%TEMP%/aegis-orchestration-20261101/sprite-linux-gates-report.md`, `sprite-cli-verify-report.md`, and their associated artifact/log directories.

Evidence: `%TEMP%/aegis-orchestration-20261101/` contains per-story briefs/reports/patches/reviews, `combined-review-package.diff`, and `combined-*.log`; `%TEMP%/aegis-orchestration-baseline-20261101/` contains baseline logs. Final whole-wave review is recorded as `final-wave-review.md` in the first evidence directory.
