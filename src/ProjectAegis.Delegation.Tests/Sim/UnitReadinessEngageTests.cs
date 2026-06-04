using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Policy;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Tests.Helpers;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Sim;

[TestFixture]
public sealed class UnitReadinessEngageTests
{
    [Test]
    public void Session_applies_not_ready_to_engage_context_for_shooter()
    {
        var session = SimulationSession.CreateWithMvpEngagement(7);
        session.UnitReadiness = new UnitReadinessMap(
            new Dictionary<string, bool> { ["u1"] = false });

        var unit = new UnitTarget(new TargetId("u1"));
        var agent = session.Orchestrator.CreateAgent(
            new AgentId("a1"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous,
            policy: new EngageOnlyPolicy());
        session.Orchestrator.AssignAgentToTarget(agent, unit, EffectivePolicy.DefaultFree);
        session.Orchestrator.Register(unit);
        session.BeginExecution();

        for (var t = 0; t < 5; t++)
        {
            session.Tick(MvpObservedStates.EngageTick(t));
        }

        Assert.That(session.Sim.LastEngagementResults, Is.Not.Empty);
        Assert.That(session.Sim.LastEngagementResults[0].Launched, Is.False);
        Assert.That(session.Sim.LastEngagementResults[0].AbortReason, Is.EqualTo(EngagementAbortReason.AirNotReady));
    }

    private sealed class EngageOnlyPolicy : IPolicy
    {
        public IReadOnlyList<ScoredIntent> GenerateCandidates(PerceivedState perceived, TraitVector traits) =>
            [new ScoredIntent(OrderKind.Engage, 1.0, RiskLevel.High)];
    }
}