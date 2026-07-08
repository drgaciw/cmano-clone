using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class MessageLogPanelBinderTests
{
    [Test]
    public void Bind_prefixes_category_on_each_row()
    {
        var lines = new[]
        {
            new MessageLogLine(1, 1.0, "KILL_CONFIRMED", "Hostile destroyed", "u1"),
            new MessageLogLine(2, 2.0, "MAGAZINE", "Magazine -1", "u1"),
        };

        var panel = MessageLogPanelBinder.Bind(lines);

        Assert.That(panel.Rows, Has.Count.EqualTo(2));
        Assert.That(panel.Rows[0].DisplayLine, Does.StartWith("[KILL_CONFIRMED]"));
        Assert.That(panel.Rows[1].Category, Is.EqualTo("MAGAZINE"));
    }

    [Test]
    public void Bind_single_arg_overload_leaves_LifecycleState_null()
    {
        var lines = new[]
        {
            new MessageLogLine(1, 1.0, "PLAYER_ORDER", "Player ordered Move for u1", "u1"),
        };

        var panel = MessageLogPanelBinder.Bind(lines);

        Assert.That(panel.Rows[0].LifecycleState, Is.Null);
    }

    [Test]
    public void Bind_with_lifecycle_map_surfaces_state_on_PLAYER_ORDER_rows()
    {
        // req 20 AC-8: a queued order shows its lifecycle state in the message log row.
        var lines = new[]
        {
            new MessageLogLine(1, 1.0, "PLAYER_ORDER", "Player ordered Move for u1", "u1"),
            new MessageLogLine(2, 2.0, "MAGAZINE", "Magazine -1", "u1"),
        };
        var lifecycle = new Dictionary<OrderLifecycleProjection.OrderKey, OrderLifecycleState>
        {
            [new OrderLifecycleProjection.OrderKey("u1", 1)] = OrderLifecycleState.Queued,
        };

        var panel = MessageLogPanelBinder.Bind(lines, lifecycle);

        Assert.That(panel.Rows[0].LifecycleState, Is.EqualTo(OrderLifecycleState.Queued));
        Assert.That(panel.Rows[1].LifecycleState, Is.Null, "non-PLAYER_ORDER rows are unaffected");
    }
}