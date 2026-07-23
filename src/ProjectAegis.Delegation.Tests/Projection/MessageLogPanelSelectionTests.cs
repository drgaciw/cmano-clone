using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class MessageLogPanelSelectionTests
{
    [Test]
    public void Binder_preserves_sequenceId_and_unitId_for_selection()
    {
        var lines = new[]
        {
            new MessageLogLine(42, 1.0, "KILL_CONFIRMED", "Hostile destroyed", "blue-1"),
            new MessageLogLine(99, 2.0, "POLICY_DENIAL", "ROE tight", null),
        };

        var panel = MessageLogPanelBinder.Bind(lines);

        Assert.That(panel.Rows[0].SequenceId, Is.EqualTo(42uL));
        Assert.That(panel.Rows[0].UnitId, Is.EqualTo("blue-1"));
        Assert.That(panel.Rows[1].SequenceId, Is.EqualTo(99uL));
        Assert.That(panel.Rows[1].UnitId, Is.Null);
    }

    [Test]
    public void SelectRow_returns_sequenceId_and_unit_for_valid_index()
    {
        var panel = MessageLogPanelBinder.Bind(new[]
        {
            new MessageLogLine(7, 0.5, "CONTACT", "Track", "red-9"),
        });

        var selection = MessageLogPanelSelection.SelectRow(panel, 0);

        Assert.That(selection, Is.Not.Null);
        Assert.That(selection!.SequenceId, Is.EqualTo(7uL));
        Assert.That(selection.UnitId, Is.EqualTo("red-9"));
        Assert.That(selection.RowIndex, Is.EqualTo(0));
    }

    [Test]
    public void SelectRow_returns_null_for_out_of_range()
    {
        var panel = MessageLogPanelBinder.Bind(Array.Empty<MessageLogLine>());
        Assert.That(MessageLogPanelSelection.SelectRow(panel, 0), Is.Null);
        Assert.That(MessageLogPanelSelection.SelectRow(panel, -1), Is.Null);
    }
}
