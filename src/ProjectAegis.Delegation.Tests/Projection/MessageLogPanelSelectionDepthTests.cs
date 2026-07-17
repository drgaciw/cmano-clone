using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>S105 A2 — sequenceId / unit focus selection depth beyond index-only.</summary>
public sealed class MessageLogPanelSelectionDepthTests
{
    private static MessageLogPanelState SamplePanel() => MessageLogPanelBinder.Bind(new[]
    {
        new MessageLogLine(10, 1.0, "CONTACT", "New track", "red-1"),
        new MessageLogLine(20, 2.0, "HIT", "Impact", "blue-2"),
        new MessageLogLine(30, 3.0, "COMMS", "Degrade", null),
        new MessageLogLine(40, 4.0, "MISSION", "Hold", "blue-2"),
    });

    [Test]
    public void SelectBySequenceId_returns_row_when_present()
    {
        var sel = MessageLogPanelSelection.SelectBySequenceId(SamplePanel(), 20);
        Assert.That(sel, Is.Not.Null);
        Assert.That(sel!.SequenceId, Is.EqualTo(20uL));
        Assert.That(sel.UnitId, Is.EqualTo("blue-2"));
        Assert.That(sel.RowIndex, Is.EqualTo(1));
    }

    [Test]
    public void SelectBySequenceId_returns_null_when_missing()
    {
        Assert.That(MessageLogPanelSelection.SelectBySequenceId(SamplePanel(), 999), Is.Null);
    }

    [Test]
    public void SelectByUnitId_returns_latest_matching_row()
    {
        // blue-2 appears at index 1 (HIT) and index 3 (MISSION) — latest wins for unit focus.
        var sel = MessageLogPanelSelection.SelectByUnitId(SamplePanel(), "blue-2");
        Assert.That(sel, Is.Not.Null);
        Assert.That(sel!.SequenceId, Is.EqualTo(40uL));
        Assert.That(sel.RowIndex, Is.EqualTo(3));
        Assert.That(sel.UnitId, Is.EqualTo("blue-2"));
    }

    [Test]
    public void SelectByUnitId_is_case_sensitive_on_unit_id()
    {
        Assert.That(MessageLogPanelSelection.SelectByUnitId(SamplePanel(), "BLUE-2"), Is.Null);
    }

    [Test]
    public void TryFindRowIndex_by_sequence_and_unit()
    {
        var panel = SamplePanel();
        Assert.That(MessageLogPanelSelection.TryFindRowIndexBySequenceId(panel, 30, out var bySeq), Is.True);
        Assert.That(bySeq, Is.EqualTo(2));
        Assert.That(MessageLogPanelSelection.TryFindRowIndexByUnitId(panel, "red-1", out var byUnit), Is.True);
        Assert.That(byUnit, Is.EqualTo(0));
        Assert.That(MessageLogPanelSelection.TryFindRowIndexBySequenceId(panel, 1, out _), Is.False);
    }
}
