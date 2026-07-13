# Bug Report

## Summary
**Title**: `TargetRegistry.Register` silently corrupts `TargetId` -> binding mapping and produces duplicate OOB/map-picture entries when two entities register the same string target key
**ID**: BUG-TARGETREG-0001
**Severity**: S2-Major
**Priority**: P2-Next Sprint
**Status**: Open
**Reported**: 2026-07-06
**Reporter**: gameplay-qa-agent (qa-r2-08-unity-adapter)

## Classification
- **Category**: UI / Integration / Presentation (sim-state-to-presentation mapping mismatch — duplicate unit rendering) with an underlying data-integrity defect in the bridge registry
- **System**: Unity/DOTS entity registration bridge — `ProjectAegis.Delegation.UnityAdapter.Bridge.TargetRegistry`, consumed by `OobTreeBridge`, `MapPictureBridge`, `UnitDetailBridge` (all via `TargetRegistry.CollectMemberIds()`) and by `OrderDispatcher`/`DelegationBridge.Tick` (via `TryGetBinding(TargetId, ...)`)
- **Frequency**: Always — deterministic, reproduces every time two different sim entities are registered under the same string target key
- **Regression**: Unknown — `TargetRegistry.Register` validates `EntityKey` uniqueness (throws `InvalidOperationException` for a duplicate entity) but has never validated `TargetId` uniqueness; this asymmetry appears to have existed since the registry was introduced and was never covered by a test that registers two entities with a colliding target key.

## Environment
- **Build**: branch `qa-r2-08-unity-adapter`, worktree HEAD at repo commit `e2e1342` (main tip) at loop start
- **Platform**: linux, .NET 8.0 SDK, `dotnet test` (no Unity Editor available in this sandbox — bug and fix are entirely in the plain .NET `ProjectAegis.Delegation.UnityAdapter` assembly)
- **Scene/Level**: N/A — pure bridge/registry bug, exercised via `ProjectAegis.Delegation.UnityAdapter.Tests`
- **Game State**: Two distinct Unity/ECS entities (different `EntityKey`s — e.g. two different DOTS entity indices) are registered against the delegation layer using the same string target key (e.g. a duplicate unit id/callsign from bad scenario data, a retried registration call, or a near-future spawn plan id that happens to collide with an already-registered unit id)

## Reproduction Steps
**Preconditions**: A fresh `TargetRegistry` (obtained from `DelegationBridge.Registry`).

1. `registry.RegisterUnit(new EntityKey(1), "u1")`
2. `registry.RegisterUnit(new EntityKey(2), "u1")` — a *different* entity, same target key string
3. Inspect `registry.TryGetBinding(new TargetId("u1"), out var binding)` and `registry.CollectMemberIds()`

**Expected Result**: The second registration should be rejected (fail fast), exactly as a duplicate `EntityKey` registration already is — the registry should never silently associate two different Unity/ECS entities with the same logical target id.

**Actual Result** (pre-fix): The second call succeeded silently. `_byTarget["u1"]` was overwritten to point at entity 2 (so all future order dispatch/`TryGetBinding` lookups for `"u1"` silently resolve to entity 2 only — orders intended for entity 1 are dropped without any error), and `_memberIds` gained a second `"u1"` entry. `OobTreeBridge.Build` / `MapPictureBridge.Build` / `UnitDetailBridge` all read `registry.CollectMemberIds()` and feed it straight into `OobTreeProjection.Project`, which does **no de-duplication** — so the C2 order-of-battle tree and map picture would render the same unit id twice for what are actually two distinct entities, a visible presentation defect, with the underlying `OrderDispatcher`/`TryGetBinding` corruption as the more serious latent defect (silently dropped orders for one of the two entities).

## Technical Context
- **Likely affected files**:
  - `src/ProjectAegis.Delegation.UnityAdapter/Bridge/TargetRegistry.cs` — private `Register(EntityKey, ICommandableTarget)` only checked `_byEntity.ContainsKey(entity)`, never checked `_byTarget.ContainsKey(target.Id)` before writing `_byTarget[target.Id] = binding` and appending to `_memberIds`.
- **Related systems**:
  - `src/ProjectAegis.Delegation/Projection/OobTreeProjection.cs` — `Project` maps `memberIds` 1:1 into `OobTreeEntry` rows with no de-dup, so any duplicate `TargetId` in the registry's member list surfaces directly as a duplicate row in the OOB tree presentation.
  - `src/ProjectAegis.Delegation.UnityAdapter/Bridge/MapPictureBridge.cs`, `UnitDetailBridge.cs` — both consume `registry.CollectMemberIds()` and would inherit the same duplicate-entry defect.
  - `src/ProjectAegis.Delegation.UnityAdapter/Bridge/OrderDispatcher.cs` / `DelegationBridge.Tick` — rely on `TryGetBinding(TargetId, ...)`, which after the collision only resolves to whichever entity registered last, silently dropping dispatch for the other entity.
- **Possible root cause**: The uniqueness check added for `EntityKey` (`_byEntity`) was never mirrored for the derived `TargetId` key space (`_byTarget`), even though both dictionaries are populated together in the same `Register` method and both are used as primary lookup keys elsewhere in the bridge.

## Evidence
- **New failing tests (red)**:
  - `ProjectAegis.Delegation.UnityAdapter.Tests.Bridge.TargetRegistryTests.RegisterUnit_with_duplicate_target_key_throws_instead_of_corrupting_registry`
  - `ProjectAegis.Delegation.UnityAdapter.Tests.Bridge.TargetRegistryTests.RegisterGroup_with_duplicate_target_key_throws_instead_of_corrupting_registry`
  - `ProjectAegis.Delegation.UnityAdapter.Tests.Bridge.TargetRegistryTests.CollectMemberIds_never_contains_duplicate_target_ids_after_failed_duplicate_registration`
  - Path: `src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/TargetRegistryTests.cs`
  - Failure before fix (representative):
    ```
    Assert.That(caughtException, expression)
      Expected: <System.InvalidOperationException>
      But was:  null
    ```
- **Fix**: `TargetRegistry.Register` now throws `InvalidOperationException` when `_byTarget.ContainsKey(target.Id)` is already true, before mutating any registry state — mirroring the existing `EntityKey` duplicate check. This applies uniformly to both `RegisterUnit` and `RegisterGroup` (both route through the same private `Register` method).
- **Post-fix (green)**: all 3 new tests pass.
  - `dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests` → **264/264** passed (baseline 261/261 + 3 new tests), zero regressions.
  - `dotnet test src/ProjectAegis.Delegation.Tests` → **253/253** unchanged (this project does not reference `ProjectAegis.Delegation.UnityAdapter`).

## Related Issues
- Distinct from BUG-C2GRAPH-0001 (`production/qa/bugs/BUG-c2-graph-highlight-stale-selection.md`, qa-loop-08 round 1: stale graph-highlight state after C2 selection change). That bug was a presentation-controller state-invalidation gap; this bug is a registry data-integrity gap (missing uniqueness validation) that happens to also manifest as a presentation-layer symptom (duplicate OOB/map entries).

## Notes
- Impact analysis performed manually (GitNexus CLI not reachable inside this isolated worktree, per repo policy — no `.gitnexus/` directory present to run `node .gitnexus/run.cjs analyze` against). Grep-based caller search for `.RegisterUnit(` / `.RegisterGroup(` across the whole repo found exactly two production call sites: `src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticReplayHarness.cs` (all invocations use distinct, hardcoded or plan-derived string keys: `"u1"`, `"ucav-blue"`, `"ucav-red"`, `"hostile-1"`, and `plan.UnitId` from near-future spawn planning) and test files (all using distinct keys). No Unity-side (`unity/ProjectAegis/Assets/Scripts`) source calls `RegisterUnit`/`RegisterGroup` directly — only a README line documents the API. Risk assessed as LOW: the change is additive fail-fast validation on a path with no existing production caller that ever registers a colliding key; it can only convert a previously-silent data-corruption bug into an explicit, catchable exception. All existing suites remain green.
- Suggested follow-up: if a legitimate future use case needs idempotent re-registration under the same target key (e.g. Unity-side reconnect/respawn retry logic), that caller should catch `InvalidOperationException` and decide explicitly (skip vs. replace) rather than relying on the previous silent-overwrite behavior.
