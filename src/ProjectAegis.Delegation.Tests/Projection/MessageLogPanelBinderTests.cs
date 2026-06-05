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
}