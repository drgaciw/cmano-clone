using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Policy;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Delegation.Tests.Helpers;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Glossary;
using ProjectAegis.Sim.Policy;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Orchestration;

[TestFixture]
public sealed class SimulationSessionMvpTests
{
    [Test]
    public void Mvp_resolver_launches_when_defaults_prime_world()
    {
        var session = SimulationSession.CreateWithMvpEngagement(7);
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

        Assert.That(session.Orchestrator.DecisionLog.Engagements.Any(e => e.Launched), Is.True);
    }

    [Test]
    public void BindMvpEngagementForScenario_uses_restricted_engagement_out_of_envelope()
    {
        var orchestrator = new DelegationOrchestrator(42);
        var session = SimulationSession.BindMvpEngagementForScenario(
            orchestrator,
            "restricted-engagement");
        var unit = new UnitTarget(new TargetId("u1"));
        var agent = session.Orchestrator.CreateAgent(
            new AgentId("a1"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous,
            policy: new EngageOnlyPolicy());
        session.Orchestrator.AssignAgentToTarget(agent, unit, EffectivePolicy.DefaultFree);
        session.Orchestrator.Register(unit);
        session.BeginExecution();

        session.Tick(MvpObservedStates.EngageTick(0));

        var aborted = session.Orchestrator.DecisionLog.Engagements
            .Where(e => !e.Launched)
            .ToList();
        Assert.That(aborted, Is.Not.Empty);
        Assert.That(aborted[0].AbortReason, Is.EqualTo(AbortReasonCatalog.Engage.OUT_OF_ENVELOPE)
            .Or.EqualTo(AbortReasonCatalog.Engage.DLZ_OUT));
    }

    [Test]
    public void Mvp_resolver_logs_abort_when_out_of_envelope()
    {
        var session = SimulationSession.CreateWithMvpEngagement(
            42,
            new EngageContext(200_000, new WeaponEnvelope(1_000, 100_000), 2, true));
        Assert.That(session.EngageWorld, Is.Not.Null);
        Assert.That(session.Magazines, Is.Not.Null);

        var unit = new UnitTarget(new TargetId("u1"));
        var agent = session.Orchestrator.CreateAgent(
            new AgentId("a1"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous,
            policy: new EngageOnlyPolicy());
        session.Orchestrator.AssignAgentToTarget(agent, unit, EffectivePolicy.DefaultFree);
        session.Orchestrator.Register(unit);
        session.BeginExecution();

        for (var t = 0; t < 3; t++)
        {
            session.Tick(MvpObservedStates.EngageTick(t));
        }

        var aborted = session.Orchestrator.DecisionLog.Engagements
            .Where(e => !e.Launched && e.AbortReason != null)
            .ToList();
        Assert.That(aborted, Is.Not.Empty);
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
