namespace ProjectAegis.Delegation.Tests.Orchestration;

using NUnit.Framework;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Watch;

[TestFixture]
public sealed class ApprovalAutoPauseCompatibilityTests
{
    [Test]
    public void Legacy_session_does_not_pause_or_set_pause_reason_when_a_proposal_is_queued()
    {
        var session = new SimulationSession(42);
        session.Orchestrator.PendingApprovalQueue.Enqueue(new Order(
            new OrderId(1), new TargetId("u1"), 0, OrderKind.Engage, RiskLevel.High));
        Assert.That(session.IsSimPaused, Is.False);
        Assert.That(session.LastWatchPauseReason, Is.EqualTo(WatchPauseReason.None));
        Assert.That(session.Orchestrator.PendingApprovalQueue.Pending, Has.Count.EqualTo(1));
    }

    [Test]
    public void Externally_scheduled_session_pauses_and_requires_resolving_approval_to_resume()
    {
        var session = new SimulationSession(42) { ExternallyScheduledSteps = true };
        session.Orchestrator.PendingApprovalQueue.Enqueue(new Order(
            new OrderId(1), new TargetId("u1"), 0, OrderKind.Engage, RiskLevel.High));
        Assert.That(session.IsSimPaused, Is.True);
        Assert.That(session.LastWatchPauseReason, Is.EqualTo(WatchPauseReason.OrderProposal));
        Assert.That(session.TryResumeSim(), Is.False);
        Assert.That(session.Orchestrator.PendingApprovalQueue.TryReject(new OrderId(1)), Is.True);
        Assert.That(session.TryResumeSim(), Is.True);
        Assert.That(session.IsSimPaused, Is.False);
    }
}