namespace ProjectAegis.Delegation.UnityAdapter.Tests.Gameplay;

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

[TestFixture]
public sealed class ExternalStepSessionTests
{
    [TestCase(false, 8UL)]
    [TestCase(true, 1UL)]
    public void External_scheduler_owns_acceleration_only_when_opted_in(bool external, ulong expectedTicks)
    {
        var session = new SimulationSession(42) { ExternallyScheduledSteps = external };
        session.BeginExecution();
        session.SetTimeAccelerationFactor(8);
        session.Tick(State());
        Assert.That(session.Sim.Clock.SimTick, Is.EqualTo(expectedTicks));
    }

    [Test]
    public void Live_range_outside_envelope_denies_fire_without_spending_rounds()
    {
        var session = CreateArmedSession();
        session.ResolveEngagementRangeMeters = shooter => shooter == "u1" ? 200_000 : 5_000;
        session.Tick(State());
        Assert.That(session.Orchestrator.DecisionLog.Engagements, Has.Count.EqualTo(1));
        Assert.That(session.Orchestrator.DecisionLog.Engagements[0].Launched, Is.False);
        Assert.That(session.Orchestrator.DecisionLog.MagazineChanges, Is.Empty);
    }

    [Test]
    public void Live_range_can_make_previously_outside_context_engageable()
    {
        var session = CreateArmedSession(rangeMeters: 200_000);
        session.ResolveEngagementRangeMeters = _ => 5_000;
        session.Tick(State());
        Assert.That(session.Orchestrator.DecisionLog.Engagements.Any(e => e.Launched), Is.True);
    }

    [TestCase(-1)]
    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    public void Invalid_live_range_fails_closed(double range)
    {
        var session = CreateArmedSession();
        session.ResolveEngagementRangeMeters = _ => range;
        session.Tick(State());
        Assert.That(session.Orchestrator.DecisionLog.Engagements.Any(e => e.Launched), Is.False);
        Assert.That(session.Orchestrator.DecisionLog.MagazineChanges, Is.Empty);
    }

    [Test]
    public void No_live_range_callback_preserves_legacy_default_context()
    {
        var session = CreateArmedSession();
        Assert.That(session.ExternallyScheduledSteps, Is.False);
        Assert.That(session.ResolveEngagementRangeMeters, Is.Null);
        session.Tick(State());
        Assert.That(session.Orchestrator.DecisionLog.Engagements.Any(e => e.Launched), Is.True);
    }

    [Test]
    public void Shooter_specific_sensor_context_does_not_inherit_global_fire_control_permission()
    {
        var session = CreateArmedSession();
        var requestedShooter = string.Empty;
        session.ResolveEngagementContext = (shooter, context) =>
        {
            requestedShooter = shooter;
            return context with { HasFireControlTrack = false };
        };
        session.Tick(State());
        Assert.That(requestedShooter, Is.EqualTo("u1"));
        Assert.That(session.Orchestrator.DecisionLog.Engagements.Any(e => e.Launched), Is.False);
        Assert.That(session.Orchestrator.DecisionLog.MagazineChanges, Is.Empty);
    }

    [Test]
    public void Shooter_specific_context_range_is_revalidated()
    {
        var session = CreateArmedSession();
        session.ResolveEngagementContext = (_, context) => context with { RangeMeters = double.NaN };
        session.Tick(State());
        Assert.That(session.Orchestrator.DecisionLog.Engagements.Any(e => e.Launched), Is.False);
        Assert.That(session.Orchestrator.DecisionLog.MagazineChanges, Is.Empty);
    }

    [Test]
    public void Shooter_specific_context_cannot_replenish_authoritative_magazines()
    {
        var session = CreateArmedSession();
        session.ResolveEngagementContext = (_, context) => context with { RoundsRemaining = 999, PkBase = 0 };
        for (var time = 0; time < 10; time++)
        {
            session.Tick(State() with { SimTime = time });
        }

        Assert.That(session.Orchestrator.DecisionLog.Engagements.Count(e => e.Launched), Is.EqualTo(4));
        Assert.That(session.Orchestrator.DecisionLog.MagazineChanges.Sum(change => change.Delta), Is.EqualTo(-4));
    }

    private static SimulationSession CreateArmedSession(double rangeMeters = 5_000)
    {
        var session = SimulationSession.CreateWithMvpEngagement(
            42,
            new EngageContext(rangeMeters, new WeaponEnvelope(0, 100_000), 4, true),
            defaultMagazineRounds: 4);
        var unit = new UnitTarget(new TargetId("u1"));
        var agent = session.Orchestrator.CreateAgent(
            new AgentId("clock-test"), PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous, policy: new EngagePolicy());
        session.Orchestrator.AssignAgentToTarget(agent, unit, EffectivePolicy.DefaultFree);
        session.Orchestrator.Register(unit);
        session.BeginExecution();
        return session;
    }

    private static ObservedState State() => new(
        0, 1, 0, new Dictionary<TargetId, bool> { [new("u1")] = true },
        HasFireControlTrack: true, PrimaryHostileContactId: new TargetId("hostile-1"));

    private sealed class EngagePolicy : IPolicy
    {
        public IReadOnlyList<ScoredIntent> GenerateCandidates(PerceivedState perceived, TraitVector traits) =>
            new[] { new ScoredIntent(OrderKind.Engage, 1, RiskLevel.High) };
    }
}
