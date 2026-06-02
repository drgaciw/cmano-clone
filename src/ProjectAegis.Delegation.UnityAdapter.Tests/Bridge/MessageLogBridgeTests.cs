namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using ProjectAegis.Delegation.UnityAdapter.Baltic;
using NUnit.Framework;

[TestFixture]
public sealed class MessageLogBridgeTests
{
    [Test]
    public void Harness_messages_include_kill_and_magazine_categories()
    {
        var result = BalticReplayHarness.Run(42, "baltic-patrol", ticks: 4);
        Assert.That(result.Messages.Any(m => m.Category == "KILL_CONFIRMED"), Is.True);
        Assert.That(result.Messages.Any(m => m.Category == "MAGAZINE"), Is.True);
    }

    [Test]
    public void Classify_scenario_messages_include_contact_category()
    {
        var result = BalticReplayHarness.Run(42, "baltic-patrol-classify", ticks: 6);
        Assert.That(result.Messages.Any(m => m.Category == "CONTACT"), Is.True);
    }
}