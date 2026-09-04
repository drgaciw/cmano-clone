using ProjectAegis.Sim.Policy;
using Xunit;

namespace ProjectAegis.Sim.Tests.Policy;

public sealed class CumulativeSalvoPolicyTests
{
    [Fact]
    public void Multiple_single_orders_against_same_target_blocked_once_cumulative_MaxSalvo_reached()
    {
        // MaxSalvo = 2
        var policy = new EffectivePolicy(RoeLevel.WeaponsFree, MaxSalvo: 2);
        var evaluator = new PolicyEvaluator(_ => policy);
        const ulong targetId = 42;
        const ulong mountId = 1;
        const ulong tick = 100;

        var ctx = new PolicyContext(UnitId: 1, PolicySnapshotId: 0, SimTick: tick, Effective: policy, SalvoSize: 1);
        var request = new ActionRequest(ActionKind.FireGuided, targetId, mountId);

        // Order 1: 0 previously fired + 1 requested <= 2 -> Allowed
        var verdict1 = evaluator.Evaluate(ctx, request);
        Assert.True(verdict1.Allowed);
        evaluator.RegisterFired(targetId, count: 1, simTick: tick);

        // Order 2: 1 previously fired + 1 requested <= 2 -> Allowed
        var verdict2 = evaluator.Evaluate(ctx, request);
        Assert.True(verdict2.Allowed);
        evaluator.RegisterFired(targetId, count: 1, simTick: tick);

        // Order 3: 2 previously fired + 1 requested = 3 > 2 (MaxSalvo reached) -> Denied
        var verdict3 = evaluator.Evaluate(ctx, request);
        Assert.False(verdict3.Allowed);
        Assert.Equal(FireAbortReason.WraSalvo, verdict3.Reason);
    }

    [Fact]
    public void Cumulative_budget_tracked_per_target_track_independently()
    {
        var policy = new EffectivePolicy(RoeLevel.WeaponsFree, MaxSalvo: 2);
        var evaluator = new PolicyEvaluator(_ => policy);
        const ulong targetA = 101;
        const ulong targetB = 102;
        const ulong tick = 10;

        var ctx = new PolicyContext(UnitId: 1, PolicySnapshotId: 0, SimTick: tick, Effective: policy, SalvoSize: 1);

        // Fire 2 at Target A (exhausts Target A budget)
        evaluator.RegisterFired(targetA, count: 1, simTick: tick);
        evaluator.RegisterFired(targetA, count: 1, simTick: tick);

        // Target A is now blocked
        var verdictA = evaluator.Evaluate(ctx, new ActionRequest(ActionKind.FireGuided, targetA, 0));
        Assert.False(verdictA.Allowed);
        Assert.Equal(FireAbortReason.WraSalvo, verdictA.Reason);

        // Target B is fresh (0 fired) -> Allowed
        var verdictB = evaluator.Evaluate(ctx, new ActionRequest(ActionKind.FireGuided, targetB, 0));
        Assert.True(verdictB.Allowed);
    }

    [Fact]
    public void Instantaneous_burst_cap_denies_before_cumulative_check()
    {
        var policy = new EffectivePolicy(RoeLevel.WeaponsFree, MaxSalvo: 2);
        var evaluator = new PolicyEvaluator(_ => policy);
        const ulong targetId = 200;

        // Instantaneous SalvoSize = 3 > MaxSalvo 2
        var ctx = new PolicyContext(UnitId: 1, PolicySnapshotId: 0, SimTick: 0, Effective: policy, SalvoSize: 3);
        var verdict = evaluator.Evaluate(ctx, new ActionRequest(ActionKind.FireGuided, targetId, 0));

        Assert.False(verdict.Allowed);
        Assert.Equal(FireAbortReason.WraSalvo, verdict.Reason);
    }

    [Fact]
    public void Partial_salvo_denied_if_sum_exceeds_max_salvo()
    {
        var policy = new EffectivePolicy(RoeLevel.WeaponsFree, MaxSalvo: 3);
        var evaluator = new PolicyEvaluator(_ => policy);
        const ulong targetId = 300;

        evaluator.RegisterFired(targetId, count: 2, simTick: 50);

        // 2 fired + 2 requested = 4 > 3 -> Denied
        var ctxExceed = new PolicyContext(UnitId: 1, PolicySnapshotId: 0, SimTick: 50, Effective: policy, SalvoSize: 2);
        var verdictExceed = evaluator.Evaluate(ctxExceed, new ActionRequest(ActionKind.FireGuided, targetId, 0));
        Assert.False(verdictExceed.Allowed);
        Assert.Equal(FireAbortReason.WraSalvo, verdictExceed.Reason);

        // 2 fired + 1 requested = 3 <= 3 -> Allowed
        var ctxFit = new PolicyContext(UnitId: 1, PolicySnapshotId: 0, SimTick: 50, Effective: policy, SalvoSize: 1);
        var verdictFit = evaluator.Evaluate(ctxFit, new ActionRequest(ActionKind.FireGuided, targetId, 0));
        Assert.True(verdictFit.Allowed);
    }

    [Fact]
    public void Engagement_window_expiration_restores_cumulative_budget()
    {
        var policy = new EffectivePolicy(RoeLevel.WeaponsFree, MaxSalvo: 2);
        var evaluator = new PolicyEvaluator(_ => policy);
        const ulong targetId = 400;
        const ulong windowTicks = 100;

        // Register 2 fired at tick 1000 with 100-tick window (active through tick 1099, expired at tick 1100)
        evaluator.RegisterFired(targetId, count: 2, simTick: 1000, windowTicks: windowTicks);

        // At tick 1050 (inside window): blocked
        var ctxInside = new PolicyContext(UnitId: 1, PolicySnapshotId: 0, SimTick: 1050, Effective: policy, SalvoSize: 1);
        var verdictInside = evaluator.Evaluate(ctxInside, new ActionRequest(ActionKind.FireGuided, targetId, 0));
        Assert.False(verdictInside.Allowed);
        Assert.Equal(FireAbortReason.WraSalvo, verdictInside.Reason);

        // At tick 1100 (window expired): allowed
        var ctxExpired = new PolicyContext(UnitId: 1, PolicySnapshotId: 0, SimTick: 1100, Effective: policy, SalvoSize: 1);
        var verdictExpired = evaluator.Evaluate(ctxExpired, new ActionRequest(ActionKind.FireGuided, targetId, 0));
        Assert.True(verdictExpired.Allowed);
    }

    [Fact]
    public void Non_fire_actions_bypass_cumulative_salvo_check()
    {
        var policy = new EffectivePolicy(RoeLevel.WeaponsFree, MaxSalvo: 1);
        var evaluator = new PolicyEvaluator(_ => policy);
        const ulong targetId = 500;

        evaluator.RegisterFired(targetId, count: 10, simTick: 0);

        var ctx = new PolicyContext(UnitId: 1, PolicySnapshotId: 0, SimTick: 0, Effective: policy, SalvoSize: 1);

        // Observe and Illuminate should still be allowed
        var observeVerdict = evaluator.Evaluate(ctx, new ActionRequest(ActionKind.Observe, targetId, 0));
        Assert.True(observeVerdict.Allowed);

        var illuminateVerdict = evaluator.Evaluate(ctx, new ActionRequest(ActionKind.Illuminate, targetId, 0));
        Assert.True(illuminateVerdict.Allowed);

        // Fire should be blocked
        var fireVerdict = evaluator.Evaluate(ctx, new ActionRequest(ActionKind.FireBallistic, targetId, 0));
        Assert.False(fireVerdict.Allowed);
        Assert.Equal(FireAbortReason.WraSalvo, fireVerdict.Reason);
    }

    [Fact]
    public void Injected_ledger_can_be_shared_and_manipulated()
    {
        var ledger = new EngagementSalvoLedger(defaultWindowTicks: 300);
        var policy = new EffectivePolicy(RoeLevel.WeaponsFree, MaxSalvo: 2);
        var evaluator1 = new PolicyEvaluator(_ => policy, ledger);
        var evaluator2 = new PolicyEvaluator(_ => policy, ledger);
        const ulong targetId = 600;

        // Register fired via evaluator1
        evaluator1.RegisterFired(targetId, count: 2, simTick: 0);

        // Evaluator2 sees the same cumulative state
        var ctx = new PolicyContext(UnitId: 2, PolicySnapshotId: 0, SimTick: 0, Effective: policy, SalvoSize: 1);
        var verdict = evaluator2.Evaluate(ctx, new ActionRequest(ActionKind.FireGuided, targetId, 0));
        Assert.False(verdict.Allowed);

        // Clear target on ledger unblocks
        ledger.ClearTarget(targetId);
        var verdictAfterClear = evaluator2.Evaluate(ctx, new ActionRequest(ActionKind.FireGuided, targetId, 0));
        Assert.True(verdictAfterClear.Allowed);
    }

    [Fact]
    public void Ledger_prune_and_total_fired_tracking()
    {
        var ledger = new EngagementSalvoLedger(defaultWindowTicks: 50);
        const ulong targetId = 700;

        ledger.RegisterFired(targetId, count: 2, simTick: 10, windowTicks: 20); // active 10..29, expires at 30
        ledger.RegisterFired(targetId, count: 1, simTick: 50, windowTicks: 50); // active 50..99, expires at 100

        Assert.Equal(3, ledger.GetTotalFired(targetId));
        Assert.Equal(2, ledger.GetActiveSalvoCount(targetId, currentSimTick: 20));
        Assert.Equal(0, ledger.GetActiveSalvoCount(targetId, currentSimTick: 40));
        Assert.Equal(1, ledger.GetActiveSalvoCount(targetId, currentSimTick: 60));
        Assert.Equal(0, ledger.GetActiveSalvoCount(targetId, currentSimTick: 110));

        ledger.PruneExpired(currentSimTick: 40);
        // Only the second entry (at tick 50, expires at 100) remains in ledger
        Assert.Equal(1, ledger.GetTotalFired(targetId));
    }
}
