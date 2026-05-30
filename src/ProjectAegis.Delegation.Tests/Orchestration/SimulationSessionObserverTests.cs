using ProjectAegis.Delegation.Orchestration;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Orchestration;

[TestFixture]
public sealed class SimulationSessionObserverTests
{
    [Test]
    public void AttachReplayViewer_delegates_to_orchestrator()
    {
        var session = SimulationSession.CreateWithMvpEngagement(globalSeed: 7);
        session.AttachReplayViewer = true;

        Assert.That(session.Orchestrator.AttachReplayViewer, Is.True);
    }
}
