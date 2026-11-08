# Bug Report

## Summary
**Title**: MvpEngagementResolver silently discards its own resolved ROE, letting the injected PolicyEvaluator's unrelated fallback override HoldFire/WeaponsTight
**ID**: BUG-roe-holdfire-silently-overridden
**Severity**: S2-Major
**Priority**: P2-Next Sprint
**Status**: Open (fix implemented and tested on branch `qa-loop-02-roe-engage`, pending review/merge)
**Reported**: 2026-07-06
**Reporter**: gameplay-qa-agent (automated TDD bug-hunt loop, qa-loop-02)

## Classification
- **Category**: Gameplay (Rules of Engagement / weapon release authorization)
- **System**: Engagement resolution — `ProjectAegis.Sim.Engage.MvpEngagementResolver` + `ProjectAegis.Sim.Policy.PolicyEvaluator`
- **Frequency**: Always, when `MvpEngagementResolver` is constructed with an explicit `IPolicyEvaluator` whose own internal fallback `resolvePolicy` delegate resolves a *different* `EffectivePolicy` than the resolver's own `resolvePolicy` callback for the same unit. Dormant (no observable effect) in the one production wiring path that exists today (`SimulationSession.BindMvpEngagement`), because there both delegates happen to route to the same underlying `DelegationOrchestrator.ResolvePolicyForUnit` method.
- **Regression**: No — this appears to be a latent design/logic defect present since `MvpEngagementResolver`'s ROE integration was written, not a recent regression against `main` (5eab203). No prior test caught it because no existing test exercised evaluator/resolver resolvePolicy divergence with a snapshot-less `PolicyContext`.

## Environment
- **Build**: branch `qa-loop-02-roe-engage`, based on `main` @ `5eab203`
- **Platform**: headless .NET 8 test layer (no Unity Editor in this sandbox); `dotnet test` / xUnit substitutes for PlayMode QA
- **Scene/Level**: N/A (unit-level `MvpEngagementResolver.Resolve` call, no scenario/mission context required)
- **Game State**: Any unit with a valid fire-control track and ammunition, whose commanded ROE (as resolved by whatever `resolvePolicy` delegate the caller passed into `MvpEngagementResolver`) is `HoldFire` or `WeaponsTight`

## Reproduction Steps
**Preconditions**: A `MvpEngagementResolver` constructed with an `IPolicyEvaluator` (`PolicyEvaluator`) whose own internal fallback resolves `EffectivePolicy.DefaultFree` (`WeaponsFree`), and a `resolvePolicy` callback passed to the *resolver itself* that resolves `HoldFire` for the shooting unit. A target in range with ammunition available.

1. Construct `var evaluator = new PolicyEvaluator();` (no override — internal fallback is `WeaponsFree`).
2. Construct `var resolver = new MvpEngagementResolver(world, magazines, evaluator, _ => new EffectivePolicy(RoeLevel.HoldFire));` — i.e. the resolver's own per-unit ROE resolution says HoldFire for this shooter.
3. Set up an in-envelope, in-DLZ `EngageContext` with a fire-control track and rounds available.
4. Call `resolver.Resolve(request)`.

**Expected Result**: The engagement is aborted with `EngagementAbortReason.RoeHoldFire` — the unit's own commanded ROE (HoldFire) must govern whether it is allowed to fire, regardless of what the injected `IPolicyEvaluator`'s own internal fallback would otherwise resolve.

**Actual Result** (before fix): `result.Launched == true`. The resolver computes the correct `effective = HoldFire` policy internally (line 77, discarded), but always constructs `PolicyContext` with `PolicySnapshotId: 0`. `PolicyEvaluator.Evaluate` treats `PolicySnapshotId == 0` as "no snapshot resolved yet — re-resolve via my own internal `resolvePolicy` delegate" (this sentinel convention is legitimately used elsewhere, by `RoePolicyAdapter.DefaultContext` and `PolicySnapshotRegistry.CreateContext`, for the *decision/order* path). Because the resolver always sent `0`, the evaluator always discarded the resolver's already-computed `effective` and substituted its own internal fallback (`WeaponsFree`), silently permitting the launch. A unit correctly commanded to Hold Fire could fire live ordnance.

## Technical Context
- **Likely affected files**:
  - `src/ProjectAegis.Sim/Engage/MvpEngagementResolver.cs` (root cause — always passed `PolicySnapshotId: 0`, discarding its own resolved `effective` policy)
  - `src/ProjectAegis.Sim/Policy/PolicyEvaluator.cs` (consumer of the sentinel — `ctx.PolicySnapshotId != 0 ? ctx.Effective : _resolvePolicy(ctx.UnitId)`)
  - `src/ProjectAegis.Sim/Policy/PolicyContext.cs` (defines the `PolicySnapshotId` sentinel field)
- **Related systems**: `ProjectAegis.Delegation.Roe.RoePolicyAdapter` and `ProjectAegis.Delegation.Orchestration.PolicySnapshotRegistry` both legitimately construct `PolicyContext` with `PolicySnapshotId: 0` for the *order/decision* path when no snapshot has been captured yet — this is correct there, and confirms the sentinel's intended semantics. `MvpEngagementResolver` is the only caller that had already resolved a trustworthy `EffectivePolicy` of its own but still passed `0`, misusing the sentinel.
- **Possible root cause**: The `MvpEngagementResolver` constructor accepts both an `IPolicyEvaluator` and a separate `Func<ulong, EffectivePolicy> resolvePolicy`. The resolver correctly calls its own `resolvePolicy` to compute `effective`, but then failed to signal to the evaluator (via a non-zero `PolicySnapshotId`) that this value was already resolved and authoritative, so the evaluator's snapshot-less fallback path silently took precedence instead.
- **Why it hasn't surfaced in production**: The only production callsite, `SimulationSession.BindMvpEngagement` (`src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs:52-60`), passes `orchestrator.PolicyEvaluator` and `orchestrator.ResolveEffectivePolicyForUnit` — both of which route to the same `DelegationOrchestrator.ResolvePolicyForUnit` method for the same unit, so the discarded value and the substituted value happen to agree today. The bug is a live footgun for any future/alternate wiring (e.g. a test harness or a future mode that constructs the evaluator and resolver with different policy sources), and a real defect in the API contract between `MvpEngagementResolver` and `IPolicyEvaluator`.

## Evidence
- **New regression test**: `ProjectAegis.Sim.Tests.Engage.MvpEngagementResolverTests.Resolver_resolve_policy_denies_hold_fire_even_when_evaluator_default_allows` (`src/ProjectAegis.Sim.Tests/Engage/MvpEngagementResolverTests.cs`).
  - Confirmed **RED** without the fix: `Assert.False(result.Launched)` failed with `Expected: False / Actual: True` (xUnit run against `MvpEngagementResolver.cs` reverted via `git stash`).
  - Confirmed **GREEN** with the fix applied.
- **Logs**: xUnit console output captured during this QA loop (see commit body / session transcript); not persisted as a separate log file.
- **Visual**: N/A (headless .NET layer, no Unity PlayMode available in this sandbox)

## Related Issues
- None filed previously — `production/qa/bugs/` was empty prior to this report (per `production/qa/bug-triage-2026-06-02.md`: "No open bugs on file").

## Notes
The fix introduces `MvpEngagementResolver.ResolvedPolicySnapshotMarker` (`const ulong = 1`), an opaque non-zero `PolicySnapshotId` used only for policy contexts the resolver builds from its own already-resolved `effective` policy. It is never registered in, or looked up from, `PolicySnapshotRegistry`; its sole purpose is the sentinel signal described above. This does not change behavior for `RoePolicyAdapter`/`PolicySnapshotRegistry` snapshot-less contexts (they still legitimately pass `0`), and does not affect `PassthroughPolicyEvaluator` (the only other `IPolicyEvaluator` implementation), which ignores `PolicySnapshotId` entirely.

Impact analysis (manual — GitNexus not reachable in this worktree; no `.gitnexus/run.cjs` present, and reachability wasn't re-verified via `npx gitnexus`): only two `IPolicyEvaluator` implementations exist in the codebase (`PolicyEvaluator`, `PassthroughPolicyEvaluator`); only `PolicyEvaluator` reads `PolicySnapshotId`. Only one production callsite constructs `MvpEngagementResolver` with a live `IPolicyEvaluator` (`SimulationSession.BindMvpEngagement`), and it is behaviorally unaffected (see above). All other callsites are unit tests. Full regression run after the fix: `ProjectAegis.Sim.Tests` 282/282 (281 baseline + 1 new), `ProjectAegis.Delegation.Tests` 251/251, `ProjectAegis.Delegation.UnityAdapter.Tests` 260/260, `ProjectAegis.Data.Tests` 476/476 — all match baseline with zero regressions. Risk assessed as LOW: the change only alters behavior for a currently-nonexistent production wiring pattern (divergent evaluator/resolvePolicy pair) and brings `MvpEngagementResolver` into line with the sentinel contract already honored by `RoePolicyAdapter` and `PolicySnapshotRegistry`.
