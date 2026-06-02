using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Engage;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class MessageLogProjectionTests
{
    [Test]
    public void Kill_and_intercept_map_to_distinct_message_categories()
    {
        var log = new DecisionLog();
        log.AppendEngagementOutcome(new EngagementOutcomeRecord(
            1, 1, 1, new TargetId("u1"), new TargetId("hostile-1"), 1,
            EngagementOutcomeCodes.Kill, 0.1));
        log.AppendEngagementOutcome(new EngagementOutcomeRecord(
            2, 2, 2, new TargetId("u1"), new TargetId("hostile-2"), 2,
            EngagementOutcomeCodes.Intercept, 0.2));

        var messages = MessageLogProjection.Project(log);

        Assert.That(messages[0].Category, Is.EqualTo("KILL_CONFIRMED"));
        Assert.That(messages[0].Text, Does.Contain("destroyed"));
        Assert.That(messages[1].Category, Is.EqualTo("INTERCEPT_SUCCESS"));
        Assert.That(messages[1].Text, Does.Contain("remains operational"));
    }
}