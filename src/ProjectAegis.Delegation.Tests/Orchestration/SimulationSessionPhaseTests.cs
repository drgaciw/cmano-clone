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
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Orchestration;

[TestFixture]
public sealed class SimulationSessionPhaseTests
{
    [Test]
    public void Tick_is_no_op_while_planning()
    {
        var session = new SimulationSession(1, new StubEngagementResolver());
        var unit = new UnitTarget(new TargetId("u1"));
        var agent = session.Orchestrator.CreateAgent(
            new AgentId("a1"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous,
            policy: new EngageOnlyPolicy());
        session.Orchestrator.AssignAgentToTarget(agent, unit, EffectivePolicy.DefaultFree);
        session.Orchestrator.Register(unit);

        Assert.That(session.Phase, Is.EqualTo(SimulationPhase.Planning));
        session.Tick(new ObservedState(0, 1, 0, new Dictionary<TargetId, bool>(), false));

        Assert.That(session.Orchestrator.DecisionLog.Records, Is.Empty);
        Assert.That(session.Sim.LastWorldHash, Is.EqualTo(0UL));
    }

    [Test]
    public void BeginExecution_allows_ticks_and_advances_sim()
    {
        var session = new SimulationSession(1, new StubEngagementResolver());
        var unit = new UnitTarget(new TargetId("u1"));
        var agent = session.Orchestrator.CreateAgent(
            new AgentId("a1"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous,
            policy: new EngageOnlyPolicy());
        session.Orchestrator.AssignAgentToTarget(agent, unit, EffectivePolicy.DefaultFree);
        session.Orchestrator.Register(unit);

        session.BeginExecution();
        session.Tick(new ObservedState(0, 1, 0, new Dictionary<TargetId, bool>(), false));

        Assert.That(session.Phase, Is.EqualTo(SimulationPhase.Executing));
        Assert.That(session.Orchestrator.DecisionLog.Records, Is.Not.Empty);
    }

    [Test]
    public void TryRebindAgentTraits_denied_when_planningOnly_and_executing()
    {
        var orchestrator = new DelegationOrchestrator(42);
        orchestrator.ScenarioPolicy = new ScenarioPolicyProfile(
            EffectivePolicy.DefaultFree,
            personalityEditPolicy: PersonalityEditPolicy.PlanningOnly);
        var agent = orchestrator.CreateAgent(
            new AgentId("a1"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous);
        orchestrator.BeginExecution();

        var verdict = orchestrator.TryRebindAgentTraits(
            agent,
            PersonalityCatalog.All[1].Traits);

        Assert.That(verdict.Allowed, Is.False);
        Assert.That(agent.Traits, Is.EqualTo(PersonalityCatalog.All[0].Traits));
    }

    /// <summary>
    /// Adversarial: TieredRebrief mid-exec denies FullAutonomous rebind via orchestrator path
    /// (not only pure LoopPolicyGate matrix).
    /// </summary>
    [Test]
    public void TryRebindAgentTraits_tieredRebrief_denies_full_autonomous_while_executing_without_mutation()
    {
        var orchestrator = new DelegationOrchestrator(42);
        orchestrator.ScenarioPolicy = new ScenarioPolicyProfile(
            EffectivePolicy.DefaultFree,
            personalityEditPolicy: PersonalityEditPolicy.TieredRebrief);

        var original = PersonalityCatalog.All[0].Traits;
        var replacement = PersonalityCatalog.All[1].Traits;
        Assume.That(replacement, Is.Not.EqualTo(original));

        var agent = orchestrator.CreateAgent(
            new AgentId("a1"),
            original,
            AutonomyLevel.FullAutonomous);
        orchestrator.BeginExecution();

        var verdict = orchestrator.TryRebindAgentTraits(agent, replacement);

        Assert.That(verdict.Allowed, Is.False);
        Assert.That(agent.Traits, Is.EqualTo(original));
    }

    /// <summary>
    /// Adversarial e2e: Manual autonomy never auto-executes Engage from agent tick path.
    /// </summary>
    [Test]
    public void Manual_agent_tick_never_executes_order_without_player_approval()
    {
        var orchestrator = new DelegationOrchestrator(globalSeed: 7);
        var unit = new UnitTarget(new TargetId("u1"));
        var agent = orchestrator.CreateAgent(
            new AgentId("a1"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.Manual,
            policy: new EngageOnlyPolicy());
        orchestrator.AssignAgentToTarget(agent, unit, EffectivePolicy.DefaultFree);
        orchestrator.Register(unit);
        orchestrator.BeginExecution();

        for (var t = 0; t < 20; t++)
        {
            orchestrator.Tick(new ObservedState(
                SimTime: t,
                ContactCount: 2,
                ActiveEngagementCount: 0,
                MemberAlive: new Dictionary<TargetId, bool>(),
                PrimaryHostileDestroyed: false));
        }

        Assert.That(orchestrator.ExecutedOrders, Is.Empty,
            "Manual must never ExecuteNow when playerApproved is false on agent path.");
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
