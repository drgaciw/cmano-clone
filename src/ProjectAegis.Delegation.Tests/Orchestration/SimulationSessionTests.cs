using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Policy;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Orchestration;

[TestFixture]
public sealed class SimulationSessionTests
{
    [Test]
    public void Tick_logs_engagement_when_resolver_launches()
    {
        var resolver = new RecordingEngagementResolver(simulateLaunch: true);
        var session = new SimulationSession(99, resolver);
        var unit = new UnitTarget(new TargetId("u1"));
        var agent = session.Orchestrator.CreateAgent(
            new AgentId("a1"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous,
            policy: new EngageOnlyPolicy());
        session.Orchestrator.AssignAgentToTarget(
            agent,
            unit,
            EffectivePolicy.DefaultFree);
        session.Orchestrator.Register(unit);

        for (var t = 0; t < 5; t++)
        {
            session.Tick(new ObservedState(t, 2, 0, new Dictionary<TargetId, bool>()));
        }

        Assert.That(session.Orchestrator.DecisionLog.Engagements, Is.Not.Empty);
        Assert.That(resolver.Requests, Is.Not.Empty);
    }

    private sealed class EngageOnlyPolicy : IPolicy
    {
        public IReadOnlyList<ScoredIntent> GenerateCandidates(PerceivedState perceived, TraitVector traits)
        {
            _ = perceived;
            _ = traits;
            return [new ScoredIntent(OrderKind.Engage, 1.0, RiskLevel.High)];
        }
    }
}
