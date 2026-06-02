namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using NUnit.Framework;

[TestFixture]
public sealed class MessageLogPanelBinderIntegrationTests
{
    [Test]
    public void Harness_combat_messages_bind_to_panel_rows()
    {
        var result = BalticReplayHarness.Run(42, "baltic-patrol", ticks: 4);
        var panel = MessageLogPanelBinder.Bind(
            result.Messages.Where(m => m.Category is "KILL_CONFIRMED" or "MAGAZINE").ToArray());

        Assert.That(panel.Rows, Is.Not.Empty);
        Assert.That(panel.Rows.Any(r => r.Category == "KILL_CONFIRMED"), Is.True);
    }
}