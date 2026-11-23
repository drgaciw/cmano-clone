using NUnit.Framework;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Projection;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class PendingApprovalInterruptCoordinatorTests
{
    private static PendingApprovalEntry MakePending(long orderId, string targetId, RiskLevel risk = RiskLevel.High) =>
        new(new Order(new OrderId(orderId), new TargetId(targetId), 0.0, OrderKind.Engage, risk));

    [Test]
    public void Evaluate_issues_at_most_one_1x_drop_per_approval_order_and_never_in_replay_or_observer()
    {
        var coordinator = new PendingApprovalInterruptCoordinator();
        var orderId = new OrderId(101);
        var pending = new[] { MakePending(101, "hostile-1") };
        var windows = new Dictionary<OrderId, double?> { [orderId] = 45.0 };

        // First tick with ord-101 at 30x -> must request 1x drop once
        var batch1 = coordinator.Evaluate(pending, currentSimTime: 15.0, dlzExpiryByOrder: windows, isReplayViewer: false, isObserver: false);
        Assert.That(batch1.ShouldDropToRealTime, Is.True);
        Assert.That(batch1.NewlyInterruptedOrderIds, Is.EqualTo(new[] { orderId }));
        Assert.That(batch1.Cards.Count, Is.EqualTo(1));
        Assert.That(batch1.Cards[0].RemainingSimSeconds, Is.EqualTo(30.0).Within(0.001));
        Assert.That(batch1.Cards[0].CountdownLine, Does.Contain("30.0s"));

        // Second tick with same orderId -> must NOT re-issue 1x drop
        var batch2 = coordinator.Evaluate(pending, currentSimTime: 16.0, dlzExpiryByOrder: windows, isReplayViewer: false, isObserver: false);
        Assert.That(batch2.ShouldDropToRealTime, Is.False);
        Assert.That(batch2.NewlyInterruptedOrderIds, Is.Empty);
        Assert.That(batch2.Cards[0].RemainingSimSeconds, Is.EqualTo(29.0).Within(0.001));

        // While paused (currentSimTime unchanged at 16.0), countdown halts cleanly
        var batchPaused = coordinator.Evaluate(pending, currentSimTime: 16.0, dlzExpiryByOrder: windows, isReplayViewer: false, isObserver: false);
        Assert.That(batchPaused.Cards[0].RemainingSimSeconds, Is.EqualTo(29.0).Within(0.001));

        // New coordinator in replay mode must never request 1x drop
        var replayCoord = new PendingApprovalInterruptCoordinator();
        var replayBatch = replayCoord.Evaluate(pending, currentSimTime: 15.0, dlzExpiryByOrder: windows, isReplayViewer: true, isObserver: false);
        Assert.That(replayBatch.ShouldDropToRealTime, Is.False);
    }

    [Test]
    public void Evaluate_marks_missing_authoritative_dlz_data_as_unknown_and_never_fabricates_timer()
    {
        var coordinator = new PendingApprovalInterruptCoordinator();
        var pending = new[] { MakePending(102, "hostile-2") };

        var batch = coordinator.Evaluate(pending, currentSimTime: 20.0, dlzExpiryByOrder: null, isReplayViewer: false, isObserver: false);

        Assert.That(batch.Cards.Count, Is.EqualTo(1));
        Assert.That(batch.Cards[0].IsDlzUnknown, Is.True);
        Assert.That(batch.Cards[0].RemainingSimSeconds, Is.Null);
        Assert.That(batch.Cards[0].CountdownLine, Does.Contain("UNKNOWN"));
    }

    [Test]
    public void ValidateDecision_blocks_replay_observer_stale_and_expired_approvals_with_explicit_reason()
    {
        var coordinator = new PendingApprovalInterruptCoordinator();
        var orderId = new OrderId(103);
        var pending = new[] { MakePending(103, "hostile-3") };

        var replay = coordinator.ValidateDecision(orderId, pending, currentSimTime: 10.0, expiresAtSimTime: 20.0, isReplayViewer: true, isObserver: false);
        Assert.That(replay.Allowed, Is.False);
        Assert.That(replay.ReasonCode, Is.EqualTo("REPLAY_READ_ONLY"));

        var observer = coordinator.ValidateDecision(orderId, pending, currentSimTime: 10.0, expiresAtSimTime: 20.0, isReplayViewer: false, isObserver: true);
        Assert.That(observer.Allowed, Is.False);
        Assert.That(observer.ReasonCode, Is.EqualTo("OBSERVER_READ_ONLY"));

        var stale = coordinator.ValidateDecision(new OrderId(999), pending, currentSimTime: 10.0, expiresAtSimTime: 20.0, isReplayViewer: false, isObserver: false);
        Assert.That(stale.Allowed, Is.False);
        Assert.That(stale.ReasonCode, Is.EqualTo("APPROVAL_STALE"));

        var expired = coordinator.ValidateDecision(orderId, pending, currentSimTime: 25.0, expiresAtSimTime: 20.0, isReplayViewer: false, isObserver: false);
        Assert.That(expired.Allowed, Is.False);
        Assert.That(expired.ReasonCode, Is.EqualTo("DLZ_EXPIRED"));

        var valid = coordinator.ValidateDecision(orderId, pending, currentSimTime: 15.0, expiresAtSimTime: 20.0, isReplayViewer: false, isObserver: false);
        Assert.That(valid.Allowed, Is.True);
        Assert.That(valid.ReasonCode, Is.EqualTo("OK"));
    }
}
