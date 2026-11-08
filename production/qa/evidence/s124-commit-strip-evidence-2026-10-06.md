# S124 Combat-commit honesty — strip + bark evidence pack (S124-04)

**Date:** 2026-10-06
**Branch:** `cursor/s124-commit-strip-7fee`
**Sprint:** [Sprint 124](../../sprints/sprint-124-combat-commit.md) · QA plan: [S122–S127 rebaseline](../qa-plan-sprint-122-127-rebaseline-2026-09-30.md) (S124 row)
**Scope:** Must items S124-01..04 (headless). S124-05 / S124-06 deferred (see below).
**Doctrine:** ADR-010 §2–3, ADR-007, ADR-001 — presentation binds projections only.

> **This evidence pack does NOT close DRG-208.** It is input to the S127 evidence index only; DRG-208 stays open until the owner records the S127 Play Mode decision. Nothing here should be read as closing DRG-208 by inference.

## Delivered

| ID | Item | Artifact | Acceptance evidence |
|----|------|----------|---------------------|
| S124-01 | W2-C2-01 pre-commit constraint + cost strip | `src/ProjectAegis.Delegation.UnityAdapter/Presentation/CommitConstraintStripBinder.cs` | `CommitConstraintStripBinderTests` 12/12 — top-3 cap, fixed source order (attack option → engage preview → DRG-259 WRA salvo → DRG-258 abort tooltip), ordinal dedupe, deterministic re-bind, magazine cost + `MAG before -> after (short n)`, primary reason equals `EngageAttackOrderResolver` refusal code |
| S124-02 | W3-C2-02 strip binds projections only | `src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/CommitStripProjectionFenceTests.cs` | 7/7 — source scan forbids `SimulationSession`, `DelegationOrchestrator`, `DelegationBridge`, `ISimWorldSnapshot`, `IOrderSink`, `ApplyOrder`, `TryEnqueue`, `DecisionLog`, `Append`, `CatalogWriteGate`, `UnityEngine`, etc.; reflection limits public parameters to projection DTOs (+ `EngageContext`, `OrderLogEntry`); binders are stateless static classes. Mutation check: injecting `SimulationSession` into the bark source fails the fence (1 failed / 6 passed), reverted |
| S124-03 | W2-C2-04 refuse/drop bark + loggable reason | `src/ProjectAegis.Delegation.UnityAdapter/Presentation/CommitRefusalBarkBinder.cs` | `CommitRefusalBarkBinderTests` 12/12 — refused commit → bark + `COMMIT_REFUSED` `MessageLogLine` with the façade reason (blank → `ENQUEUE_REJECTED`); dropped commit (`ENGAGE_ABORT` / `POLICY_DENIAL`) → bark whose `LogLine` equals the `MessageLogProjection` row and contains the same reason code |
| S124-04 | W3-C2-01 evidence pack | this file | — |

## Commands (run from the worktree root, `export PATH="$HOME/.dotnet:$PATH"`)

| Command | Result |
|---------|--------|
| `dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj -m:1 --filter "FullyQualifiedName~CommitConstraintStripBinderTests\|FullyQualifiedName~CommitRefusalBarkBinderTests\|FullyQualifiedName~CommitStripProjectionFenceTests"` | 31/31 passed |
| same project, `--filter "FullyQualifiedName~Presentation.Commit\|FullyQualifiedName~WraSalvo\|FullyQualifiedName~WeaponAbortTooltip\|FullyQualifiedName~AttackOptionsPreview"` | 63/63 passed |
| same project, `--filter PlayModeSmokeHarnessTests` | 25/25 passed (floor ≥20) |
| same project, `--filter "FullyQualifiedName~ReplayGolden"` | 17/17 passed |
| same project, no filter | 886/886 passed |
| `dotnet build src/ProjectAegis.Delegation.UnityAdapter/ProjectAegis.Delegation.UnityAdapter.csproj -f netstandard2.1 -m:1 --no-incremental` | 0 warnings, 0 errors |
| `dotnet build src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj -m:1 --no-incremental` | 0 warnings, 0 errors |
| `dotnet format <UnityAdapter.Tests csproj> --verify-no-changes --include <5 new files>` | exit 0 |
| `grep -r "17144800277401907079" tests/ data/` | present in Baltic v2 goldens (unchanged) |

Full-solution build/test was not run on this VM (shared 4-core host; scoped builds only per sprint rules).

## Invariants

- `DelegationBridge.cs`: untouched (diff vs `origin/main` adds 5 new files only, no existing file edited).
- Baltic v2 replay goldens and hash `17144800277401907079`: untouched.
- `CatalogWriteGate` write paths: untouched.
- Files owned by concurrent tracks (`CombatEvents/`, `EngageExplainContract/`, `EngageExplainProjection`, `ContactCombatCard*`, `AttackOptionsPreviewBinder`) are consumed read-only.
- Refused-commit log lines are presentation-local (`SequenceId` 0); nothing is written to the order log.

## Deferred / not in this pack

- **S124-05 W2-DEL-04 initial Assign Agent** and **S124-06 W2-DEL-02 rebrief success path**: deferred. Both depend on the S123 play-entry path, which is still in flight.
- **Unity host wiring + Editor screenshots** (strip/bark UXML elements `commit-strip-constraints`, `commit-strip-cost`, `commit-bark-line` on the unit panel): not done. The likely host (`RightUnitPanelHost`) is shared with the `AttackOptionsPreviewBinder` / `ContactCombatCard*` follow-up track. The headless model is ready to bind.
- Should/Nice items (S124-07..12) are not started.
