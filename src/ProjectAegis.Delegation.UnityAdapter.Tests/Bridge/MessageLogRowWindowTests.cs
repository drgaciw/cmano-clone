namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using NUnit.Framework;
using Projection;

/// <summary>UAT D38: the HUD row cap must not evict comms state transitions.</summary>
[TestFixture]
public sealed class MessageLogRowWindowTests
{
    private static MessageLogLine Line(ulong seq, string category, string text) =>
        new(seq, seq, category, text);

    [Test]
    public void Select_keeps_early_comms_rows_when_later_chatter_exceeds_cap()
    {
        var lines = new List<MessageLogLine>
        {
            Line(1, "COMMS", "Comms net-1: Nominal → Degraded (scheduled)"),
            Line(2, "COMMS", "Comms net-1: Degraded → Denied (scheduled)"),
        };
        for (ulong i = 3; i < 20; i++)
        {
            lines.Add(Line(i, "AGENT_DECISION", "decision " + i));
        }

        var shown = MessageLogRowWindow.Select(lines, 12);

        Assert.That(shown, Has.Count.EqualTo(12));
        Assert.That(shown.Count(l => l.Category == "COMMS"), Is.EqualTo(2),
            "COMMS rows evicted by tail window: " + string.Join(" | ", shown.Select(l => l.Category + "#" + l.SequenceId)));
        Assert.That(shown.Select(l => l.SequenceId), Is.Ordered);
        Assert.That(shown[^1].SequenceId, Is.EqualTo(19UL));
    }

    [Test]
    public void Select_under_cap_or_unlimited_returns_all()
    {
        var lines = new[] { Line(1, "COMMS", "a"), Line(2, "HIT", "b") };
        Assert.That(MessageLogRowWindow.Select(lines, 12), Has.Count.EqualTo(2));
        Assert.That(MessageLogRowWindow.Select(lines, 0), Has.Count.EqualTo(2));
    }

    [Test]
    public void Select_comms_alone_over_cap_keeps_newest_comms()
    {
        var lines = Enumerable.Range(1, 5).Select(i => Line((ulong)i, "COMMS", "c" + i)).ToArray();
        var shown = MessageLogRowWindow.Select(lines, 3);
        Assert.That(shown.Select(l => l.SequenceId), Is.EqualTo(new ulong[] { 3, 4, 5 }));
    }

    [Test]
    public void Baltic_patrol_comms_scenario_comms_rows_survive_host_row_cap()
    {
        var result = ProjectAegis.Delegation.UnityAdapter.Baltic.BalticReplayHarness.Run(7, "baltic-patrol-comms", ticks: 30, mvpEngagement: true);
        var shown = MessageLogRowWindow.Select(result.Messages, 12);
        Assert.That(shown.Count(l => l.Category == "COMMS"), Is.GreaterThanOrEqualTo(1));
        Assert.That(shown.First(l => l.Category == "COMMS").Text, Does.StartWith("Comms ").And.Contain("→"));
    }

    [Test]
    public void Select_with_policy_filters_Log_false_before_taking_trailing_maxRows_window()
    {
        var policy = MessageCategoryPolicy.LoadDefault()
            .WithRule("FUEL", new MessageCategoryRule(Log: false, Pause: false, MapBalloon: false, DropToRealTime: false));

        var lines = new List<MessageLogLine>
        {
            Line(1, "DAMAGE", "Damage u1: 100% -> 80% (Hit)"),
            Line(2, "CONTACT", "Contact c1 Detected -> Classified"),
        };
        for (ulong i = 3; i <= 14; i++)
        {
            lines.Add(Line(i, "FUEL", $"Fuel u1 tick {i}"));
        }

        var shown = MessageLogRowWindow.Select(lines, 12, policy);

        Assert.That(shown, Has.Count.EqualTo(2));
        Assert.That(shown.Select(l => l.SequenceId), Is.EqualTo(new[] { 1UL, 2UL }));
    }
}
