using System.Linq;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>Track T3 (req 20 §Alerting and Interruption, TR-c2-007): severity consumer over
/// <see cref="MessageLogLine"/> using the Phase 0 <see cref="AlertSeverityMap"/> contract.</summary>
[TestFixture]
public sealed class AlertProjectionTests
{
    [Test]
    public void Project_tags_each_line_with_its_severity()
    {
        var lines = new[]
        {
            new MessageLogLine(1, 10.0, "KILL_CONFIRMED", "Hostile destroyed", "u1"),
            new MessageLogLine(2, 11.0, "CONTACT", "New contact", "u2"),
            new MessageLogLine(3, 12.0, "FUEL", "Fuel burn", "u3"),
        };

        var items = AlertProjection.Project(lines);

        Assert.That(items, Has.Count.EqualTo(3));
        Assert.That(items[0].Severity, Is.EqualTo(AlertSeverity.Critical));
        Assert.That(items[1].Severity, Is.EqualTo(AlertSeverity.Notable));
        Assert.That(items[2].Severity, Is.EqualTo(AlertSeverity.Routine));
    }

    [Test]
    public void Project_preserves_line_fields()
    {
        var line = new MessageLogLine(7, 42.5, "POLICY_DENIAL", "Fire denied", "u9");

        var item = AlertProjection.Project(new[] { line })[0];

        Assert.That(item.SequenceId, Is.EqualTo(line.SequenceId));
        Assert.That(item.SimTime, Is.EqualTo(line.SimTime));
        Assert.That(item.Category, Is.EqualTo(line.Category));
        Assert.That(item.Text, Is.EqualTo(line.Text));
        Assert.That(item.UnitId, Is.EqualTo(line.UnitId));
    }

    [Test]
    public void ProjectSeverity_filters_to_requested_tier_only()
    {
        var lines = new[]
        {
            new MessageLogLine(1, 1.0, "KILL_CONFIRMED", "a", "u1"),
            new MessageLogLine(2, 2.0, "MODE", "b", "u2"),
            new MessageLogLine(3, 3.0, "POLICY_DENIAL", "c", "u3"),
            new MessageLogLine(4, 4.0, "FUEL", "d", "u4"),
        };

        var critical = AlertProjection.ProjectSeverity(lines, AlertSeverity.Critical);

        Assert.That(critical, Has.Count.EqualTo(2));
        Assert.That(critical[0].Category, Is.EqualTo("KILL_CONFIRMED"));
        Assert.That(critical[1].Category, Is.EqualTo("POLICY_DENIAL"));
    }

    [Test]
    public void Project_of_empty_list_is_empty()
    {
        Assert.That(AlertProjection.Project(System.Array.Empty<MessageLogLine>()), Is.Empty);
    }

    [Test]
    public void Project_preserves_input_order_even_when_not_sorted_by_sim_time_or_sequence()
    {
        // The projection must not reorder/sort — callers (e.g. ToastStackModel, message log rendering)
        // depend on Project returning items in exactly the order MessageLogProjection produced them.
        var lines = new[]
        {
            new MessageLogLine(9, 90.0, "FUEL", "later sequence, listed first", "u9"),
            new MessageLogLine(2, 5.0, "KILL_CONFIRMED", "earlier sequence, listed second", "u2"),
            new MessageLogLine(5, 40.0, "CONTACT", "middle", "u5"),
        };

        var items = AlertProjection.Project(lines);

        Assert.That(items.Select(i => i.SequenceId), Is.EqualTo(new ulong[] { 9, 2, 5 }));
    }

    [Test]
    public void ProjectSeverity_of_empty_list_is_empty()
    {
        Assert.That(
            AlertProjection.ProjectSeverity(System.Array.Empty<MessageLogLine>(), AlertSeverity.Critical),
            Is.Empty);
    }
}
