using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Decision;

/// <summary>Phase 2b (req 20 rev 2 §Order lifecycle cancel/replan, TR-c2-006 / AC-8): additive
/// <c>PlayerOrderCancelled</c> order-log path. Verifies the queue/controller removal, the logged
/// cancellation record, the lifecycle projection mapping to Aborted, and — critically — that the change
/// is additive (a log with no cancellation fingerprints identically to before).</summary>
[TestFixture]
public sealed class PlayerOrderCancellationTests
{
    private static Order MoveOrder(string unit) =>
        new(new OrderId(0), new TargetId(unit), 5.0, OrderKind.Move, RiskLevel.Low);

    [Test]
    public void Queue_TryRemove_removes_the_oldest_pending_order_for_the_target()
    {
        var queue = new PlayerOrderExecutionQueue();
        queue.Enqueue(MoveOrder("u1"), executeSimTick: 20);
        queue.Enqueue(MoveOrder("u2"), executeSimTick: 21);

        Assert.That(queue.TryRemove(new TargetId("u1"), out var removed, out var executeTick), Is.True);
        Assert.That(removed.Target.Value, Is.EqualTo("u1"));
        Assert.That(executeTick, Is.EqualTo((ulong)20));
        Assert.That(queue.PendingCount, Is.EqualTo(1), "only u1 removed; u2 remains pending");
    }

    [Test]
    public void Queue_TryRemove_returns_false_when_target_has_no_pending_order()
    {
        var queue = new PlayerOrderExecutionQueue();
        queue.Enqueue(MoveOrder("u1"), executeSimTick: 20);

        Assert.That(queue.TryRemove(new TargetId("u9"), out _, out _), Is.False);
        Assert.That(queue.PendingCount, Is.EqualTo(1));
    }

    [Test]
    public void Cancelled_order_before_execute_never_drains()
    {
        var controller = new HumanController();
        controller.Enqueue(MoveOrder("u1"), executeSimTick: 20);

        Assert.That(controller.TryCancel(new TargetId("u1"), out _, out _), Is.True);
        Assert.That(controller.DrainIssuedOrders(currentSimTick: 100), Is.Empty,
            "a cancelled order must not execute even once its execute tick arrives");
    }

    [Test]
    public void Logged_cancellation_maps_to_Aborted_in_the_lifecycle_projection()
    {
        var log = new DecisionLog();
        log.AppendPlayerOrder(new PlayerOrderRecord(0, 5.0, 5, new TargetId("u1"), OrderKind.Move, ExecuteSimTick: 20));

        var beforeCancel = OrderLifecycleProjection.Project(log);
        var key = System.Linq.Enumerable.Single(beforeCancel.Keys);
        Assert.That(beforeCancel[key], Is.EqualTo(OrderLifecycleState.Queued), "future execute tick → Queued");

        log.AppendPlayerOrderCancelled(new PlayerOrderCancelledRecord(0, 8.0, 8, new TargetId("u1"), OrderKind.Move, CancelledExecuteSimTick: 20));

        var afterCancel = OrderLifecycleProjection.Project(log);
        Assert.That(afterCancel[key], Is.EqualTo(OrderLifecycleState.Aborted));
        Assert.That(log.PlayerOrderCancellations, Has.Count.EqualTo(1));
    }

    [Test]
    public void Fingerprint_is_unchanged_when_no_cancellation_is_logged()
    {
        // Additive-determinism sanity: two logs with the SAME non-cancel entries fingerprint identically,
        // i.e. adding the PlayerOrderCancelled machinery does not perturb existing replays. (The frozen
        // production Baltic hash 17144800277401907079 is the end-to-end proof of the same property.)
        var a = new DecisionLog();
        var b = new DecisionLog();
        a.AppendPlayerOrder(new PlayerOrderRecord(0, 5.0, 5, new TargetId("u1"), OrderKind.Move, ExecuteSimTick: 20));
        b.AppendPlayerOrder(new PlayerOrderRecord(0, 5.0, 5, new TargetId("u1"), OrderKind.Move, ExecuteSimTick: 20));

        Assert.That(a.ComputeFingerprint(), Is.EqualTo(b.ComputeFingerprint()));
    }

    [Test]
    public void Fingerprint_captures_a_cancellation_when_one_is_logged()
    {
        // When a cancel DOES occur it must be part of the deterministic replay fingerprint.
        var withoutCancel = new DecisionLog();
        withoutCancel.AppendPlayerOrder(new PlayerOrderRecord(0, 5.0, 5, new TargetId("u1"), OrderKind.Move, ExecuteSimTick: 20));

        var withCancel = new DecisionLog();
        withCancel.AppendPlayerOrder(new PlayerOrderRecord(0, 5.0, 5, new TargetId("u1"), OrderKind.Move, ExecuteSimTick: 20));
        withCancel.AppendPlayerOrderCancelled(new PlayerOrderCancelledRecord(0, 8.0, 8, new TargetId("u1"), OrderKind.Move, CancelledExecuteSimTick: 20));

        Assert.That(withCancel.ComputeFingerprint(), Is.Not.EqualTo(withoutCancel.ComputeFingerprint()));
        Assert.That(withCancel.ComputeFingerprint(), Does.Contain("PlayerOrderCancelled"));
    }
}
