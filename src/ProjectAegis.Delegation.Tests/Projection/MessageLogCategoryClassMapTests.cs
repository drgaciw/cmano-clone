using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class MessageLogCategoryClassMapTests
{
    [TestCase("KILL_CONFIRMED", "message-log-row--kill")]
    [TestCase("HIT", "message-log-row--kill")]
    [TestCase("MAGAZINE", "message-log-row--magazine")]
    [TestCase("COMMS", "message-log-row--comms")]
    [TestCase("CONTACT", "message-log-row--contact")]
    [TestCase("CONTACT_CHANGE", "message-log-row--contact")]
    [TestCase("MISSION", "message-log-row--mission")]
    [TestCase("MISSION_TRANSITION", "message-log-row--mission")]
    [TestCase("POLICY_DENIAL", "message-log-row--policy")]
    [TestCase("WEAPON_LAUNCH", "message-log-row--weapon")]
    [TestCase("UNKNOWN_CAT", null)]
    [TestCase(null, null)]
    [TestCase("", null)]
    public void CssClassFor_maps_known_categories(string? category, string? expected)
    {
        Assert.That(MessageLogCategoryClassMap.CssClassFor(category), Is.EqualTo(expected));
    }

    [Test]
    public void Binder_embeds_category_css_class_on_rows()
    {
        var panel = MessageLogPanelBinder.Bind(new[]
        {
            new MessageLogLine(1, 0.0, "KILL_CONFIRMED", "Destroyed", "u1"),
            new MessageLogLine(2, 0.1, "COMMS", "Link lost", null),
            new MessageLogLine(3, 0.2, "CUSTOM", "Other", null),
        });

        Assert.That(panel.Rows[0].CategoryCssClass, Is.EqualTo("message-log-row--kill"));
        Assert.That(panel.Rows[1].CategoryCssClass, Is.EqualTo("message-log-row--comms"));
        Assert.That(panel.Rows[2].CategoryCssClass, Is.Null);
    }
}
