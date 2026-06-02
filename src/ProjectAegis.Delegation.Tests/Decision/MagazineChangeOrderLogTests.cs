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
using ProjectAegis.Sim.Policy;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Decision;

[TestFixture]
public sealed class MagazineChangeOrderLogTests
{
    [Test]
    public void Two_launches_emit_two_magazine_change_rows_with_delta_minus_one()
    {
        var session = SimulationSession.BindMvpEngagement(
            new DelegationOrchestrator(42),
            new EngageContext(50_000, new WeaponEnvelope(1_000, 100_000), RoundsRemaining: 2, HasFireControlTrack: true, PkKill: 0),
            defaultMagazineRounds: 2);
        WireEngageAgent(session);
        session.BeginExecution();

        for (var t = 0; t < 3; t++)
        {
            session.Tick(MvpObservedStates.EngageTick(t));
        }

        var changes = session.Orchestrator.DecisionLog.MagazineChanges;
        Assert.That(changes, Has.Count.EqualTo(2));
        Assert.That(changes.All(c => c.Delta == -1 && c.ReasonCode == MagazineChangeReasonCodes.Fire), Is.True);

        var entries = session.Orchestrator.DecisionLog.ChronologicalEntries();
        Assert.That(entries.Count(e => e.Kind == OrderLogEntryKind.MagazineChange), Is.EqualTo(2));

        var fingerprint = session.Orchestrator.DecisionLog.ComputeFingerprint();
        Assert.That(fingerprint, Does.Contain("MagazineChange|"));
        Assert.That(fingerprint, Does.Contain("|fire"));
    }

    [Test]
    public void Third_engage_after_magazine_empty_has_no_magazine_change_row()
    {
        var session = SimulationSession.BindMvpEngagement(
            new DelegationOrchestrator(7),
            new EngageContext(50_000, new WeaponEnvelope(1_000, 100_000), RoundsRemaining: 2, HasFireControlTrack: true, PkKill: 0),
            defaultMagazineRounds: 2);
        WireEngageAgent(session);
        session.BeginExecution();

        for (var t = 0; t < 4; t++)
        {
            session.Tick(MvpObservedStates.EngageTick(t));
        }

        Assert.That(session.Orchestrator.DecisionLog.MagazineChanges, Has.Count.EqualTo(2));
        Assert.That(
            session.Orchestrator.DecisionLog.Engagements.Any(e =>
                !e.Launched && e.AbortReasonCode == nameof(EngagementAbortReason.MagazineEmpty)),
            Is.True);
    }

    private static void WireEngageAgent(SimulationSession session)
    {
        var unit = new UnitTarget(new TargetId("u1"));
        var agent = session.Orchestrator.CreateAgent(
            new AgentId("a1"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous,
            policy: new EngageOnlyPolicy());
        session.Orchestrator.AssignAgentToTarget(agent, unit, EffectivePolicy.DefaultFree);
        session.Orchestrator.Register(unit);
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