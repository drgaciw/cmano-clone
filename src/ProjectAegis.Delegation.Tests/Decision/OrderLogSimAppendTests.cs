using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Policy;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Decision;

[TestFixture]
public sealed class OrderLogSimAppendTests
{
    [Test]
    public void IOrderLog_Append_engagement_and_mission_rows()
    {
        IOrderLog log = new DecisionLog();

        log.Append(OrderLogEntryFactories.FromMissionTransition(
            new MissionTransitionRecord(0, 0, 0, "start-exec", "Execution")));
        log.Append(OrderLogEntryFactories.FromEventFired(
            new EventFiredRecord(0, 1, 1, "contact-window", "contact_window_open")));
        log.Append(OrderLogEntryFactories.FromEngagement(
            new EngagementRecord(0, 2, 2, new TargetId("u1"), 1, true, "Launched")));

        var fingerprint = log.ComputeFingerprint();
        Assert.That(fingerprint, Does.Contain("MissionTransition|"));
        Assert.That(fingerprint, Does.Contain("EventFired|"));
        Assert.That(fingerprint, Does.Contain("Engagement|"));
    }
}