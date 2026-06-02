using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Orchestration;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Decision;

[TestFixture]
public sealed class IOrderLogContractTests
{
    [Test]
    public void Orchestrator_OrderLog_surfaces_decision_log_entries()
    {
        var orchestrator = new DelegationOrchestrator(1);
        IOrderLog log = orchestrator.OrderLog;

        Assert.That(log, Is.SameAs(orchestrator.DecisionLog));
        Assert.That(log.ChronologicalEntries(), Is.Empty);
        Assert.That(log.ComputeFingerprint(), Is.EqualTo(string.Empty));
    }
}