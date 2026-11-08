# Bug Report

## Summary
**Title**: Redundant "Take Direct Control" call on an already-detached unit silently discards queued player orders
**ID**: BUG-double-take-control-drops-queued-orders
**Severity**: S2-Major
**Priority**: P2-Next Sprint
**Status**: Fixed (regression test added, fix landed same commit)
**Reported**: 2026-07-05
**Reporter**: gameplay-qa-agent (qa-loop-03-delegation)

## Classification
- **Category**: Gameplay (C2 delegation / override authority)
- **System**: `ProjectAegis.Delegation` — group detach/rejoin + direct-control override
- **Frequency**: Sometimes (10-50%) — requires a double-invocation of take-direct-control on a unit already under human control (double-click, re-issued hotkey, retried network/UI call, or any caller code path that doesn't itself track detached state)
- **Regression**: No — appears to be present since `DetachRejoinService`/`TryTakeDirectControl` were introduced; not previously covered by any test

## Environment
- **Build**: worktree `qa-loop-03-delegation`, main HEAD as of 2026-07-05 (.NET simulation layer, no Unity Editor involved — reproduced purely via `dotnet test`)
- **Platform**: N/A (pure C# sim/delegation logic, exercised headlessly)
- **Scene/Level**: N/A — reproduced with a synthetic `DelegationOrchestrator` + one `GroupTarget`/`UnitTarget` pair
- **Game State**: A friendly unit that is a member of an agent-controlled group

## Reproduction Steps
**Preconditions**: A `UnitTarget` is a member of a `GroupTarget` whose `Slot.Active` is an `AgentController`. Both are registered with a `DelegationOrchestrator`.

1. Player takes direct control of the unit: `orchestrator.TryTakeDirectControl(unit, simTime)`. This calls `DetachRejoinService.Detach`, which (a) installs a new `HumanController` on `unit.Slot`, and (b) removes the unit from `group.Members` (`GroupTarget.RemoveMember`).
2. Player queues one or more orders on the unit while it is delegated to comms delay (e.g. `req 19` `CommsOrderDelay` holding an order a few ticks in `PlayerOrderExecutionQueue`): `human.Enqueue(order, executeSimTick)`.
3. Player invokes take-direct-control again on the *same, already-detached* unit — e.g. a double-clicked "Take Direct Control" UI action, a retried call after a dropped network round-trip, or simply re-selecting the unit and hitting the hotkey again while unaware it is already detached: `orchestrator.TryTakeDirectControl(unit, simTime2)`.

**Expected Result**: The second call is a harmless no-op (the unit is already under direct human control); any orders already queued for the unit remain queued and will still execute at their scheduled `executeSimTick`.

**Actual Result** (before fix): `TryTakeDirectControl` calls `FindParentGroup(unit.Id)`, which now returns `null` because step 1 already removed the unit from `group.Members`. Falling through the `else if (unit.Slot.Active is AgentController)` branch (also false, since `Active` is already a `HumanController`), execution reaches the final `else` branch: `unit.Slot.SetActive(new HumanController())`. This unconditionally replaces the existing `HumanController` instance — including its internal `PlayerOrderExecutionQueue` and every order enqueued in step 2 — with a brand-new, empty one. The queued order(s) vanish with no error, no log entry, and no player-visible feedback; the unit simply never executes the order it was given.

## Technical Context
- **Likely affected files**:
  - `src/ProjectAegis.Delegation/Orchestration/DelegationOrchestrator.cs` (`TryTakeDirectControl`, lines ~180-209 prior to fix) — root cause
  - `src/ProjectAegis.Delegation/Groups/DetachRejoinService.cs` (`Detach`) — removes group membership, which is what makes the second call take the wrong branch
  - `src/ProjectAegis.Delegation/Orchestration/OverrideService.cs` (`TakeDirectControl`) — also unconditionally calls `SetActive`, but is only reached in the "not currently under human control" cases; not itself the trigger here
  - `src/ProjectAegis.Delegation/Controllers/HumanController.cs` / `Decision/PlayerOrderExecutionQueue.cs` — hold the state that gets discarded
  - `src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs` (`TryTakeDirectControl`, line 244) — thin passthrough to the orchestrator method with no guard of its own, so any UI/input path (double-click, repeated hotkey, retried RPC) reaches the bug unmodified
- **Related systems**: req 19 comms-delay order queueing; group detach/rejoin (delegation authority overrides)
- **Root cause**: `TryTakeDirectControl` determined "is this unit already grouped" by checking live group membership (`FindParentGroup`), but `Detach` had already mutated that membership on the first call. The method had no direct check of "is the unit already under a `HumanController`" and therefore treated a redundant take-control request the same as a first-time one, unconditionally installing a fresh controller.

## Evidence
- **New failing-then-passing test**: `ProjectAegis.Delegation.Tests.Orchestration.OrchestratorOverrideTests.TryTakeDirectControl_calledAgainOnAlreadyDetachedUnit_preservesQueuedPlayerOrders`
  (`src/ProjectAegis.Delegation.Tests/Orchestration/OrchestratorOverrideTests.cs`)
  - Before fix: **Failed** — `Assert.That(unit.Slot.Active, Is.SameAs(human))` failed because a new `HumanController` instance had replaced the original one (`human.PendingOrderCount` would also have reported the loss on the follow-up assertion).
  - After fix: **Passed**.
- **Full suite before/after**: `ProjectAegis.Delegation.Tests` 251/251 (baseline) -> 252/252 (251 pre-existing + 1 new, all green). `ProjectAegis.Delegation.UnityAdapter.Tests` 260/260 -> 260/260 (no change, no regressions — sole caller `DelegationBridge.TryTakeDirectControl` is an unconditional passthrough so the new guard is exercised transparently).

## Related Issues
- Adjacent design note: `docs/superpowers/specs/2026-05-30-agent-delegation-decisions-design.md` documents `TryTakeDirectControl`/`DetachRejoinService`/`DelegationBridge.TryTakeDirectControl` as the override+detach code path but does not call out idempotency requirements for repeated take-control calls.

## Notes
Fix implemented as a minimal guard at the top of `DelegationOrchestrator.TryTakeDirectControl`: if `unit.Slot.Active is HumanController`, return `true` immediately (already under direct control, no-op) before the `FindParentGroup`/`AgentController` branching runs. This preserves all previously-tested behavior (first-time detach from a group, first-time take-control of an ungrouped unit, release/rejoin) and only changes the previously-untested double-take-control edge case, so blast radius is limited to this one call path (verified via manual grep — GitNexus MCP tools and `.gitnexus/run.cjs` are not present/reachable in this isolated worktree, so impact was assessed by grepping all callers of `DelegationOrchestrator.TryTakeDirectControl` instead of automated impact analysis).
