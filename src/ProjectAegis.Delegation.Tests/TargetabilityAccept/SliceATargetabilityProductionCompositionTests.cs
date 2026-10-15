using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Policy;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Delegation.TargetabilityAccept;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Sim.Policy;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.TargetabilityAccept;

/// <summary>
/// DRG-350: the Slice A targetability fixture driven through the production headless composition
/// (<see cref="SimulationSession.BindMvpEngagementForScenario"/> over a scenario-bound orchestrator).
/// Silence is asserted on the orchestrator's authoritative log, not on a hand-built one.
/// </summary>
[TestFixture]
public sealed class SliceATargetabilityProductionCompositionTests
{
    [Test]
    public void Production_session_reaches_target_with_zero_fire_or_ordnance_in_authoritative_log()
    {
        var evidence = SliceATargetabilityAcceptanceHarness.Run(SliceATargetabilityComposition.ProductionSession).Evidence;

        Assert.That(evidence.Paths, Has.Count.EqualTo(2));
        Assert.That(evidence.EngagementCount, Is.Zero);
        Assert.That(evidence.EngagementOutcomeCount, Is.Zero);
        Assert.That(evidence.PlayerOrderCount, Is.Zero);
        Assert.That(evidence.MagazineChangeCount, Is.Zero);
        Assert.That(evidence.OrdnanceStateChangeCount, Is.Zero);
        Assert.That(evidence.ContactChangeCount, Is.EqualTo(6));
        Assert.That(evidence.AcceptanceFingerprint, Does.Not.Contain("17144800277401907079"));

        foreach (var path in evidence.Paths)
        {
            Assert.That(path.Phase, Is.EqualTo(KillChainPhase.Target), path.PathId);
            Assert.That(path.PhasesVisited, Is.EqualTo("Find>Fix>Track>Target"), path.PathId);
            Assert.That(path.TechnicallyTargetable, Is.True, path.PathId);
            Assert.That(path.ContactChangeCount, Is.EqualTo(3), path.PathId);
            Assert.That(path.EngagementCount, Is.Zero, path.PathId);
            Assert.That(path.EngagementOutcomeCount, Is.Zero, path.PathId);
            Assert.That(path.PlayerOrderCount, Is.Zero, path.PathId);
            Assert.That(path.MagazineChangeCount, Is.Zero, path.PathId);
            Assert.That(path.OrdnanceStateChangeCount, Is.Zero, path.PathId);
        }
    }

    [Test]
    public void Production_session_verdicts_match_fixture_log_verdicts()
    {
        var fixture = SliceATargetabilityAcceptanceHarness.Run(SliceATargetabilityComposition.FixtureLog).Evidence;
        var production = SliceATargetabilityAcceptanceHarness.Run(SliceATargetabilityComposition.ProductionSession).Evidence;

        Assert.That(production.Paths, Has.Count.EqualTo(fixture.Paths.Count));
        for (var i = 0; i < fixture.Paths.Count; i++)
        {
            var f = fixture.Paths[i];
            var p = production.Paths[i];
            Assert.That(p.PathId, Is.EqualTo(f.PathId));
            Assert.That(p.Phase, Is.EqualTo(f.Phase), f.PathId);
            Assert.That(p.Loss, Is.EqualTo(f.Loss), f.PathId);
            Assert.That(p.Disposition, Is.EqualTo(f.Disposition), f.PathId);
            Assert.That(p.WithheldCauseCode, Is.EqualTo(f.WithheldCauseCode), f.PathId);
            Assert.That(p.AuthorityDisposition, Is.EqualTo(f.AuthorityDisposition), f.PathId);
            Assert.That(p.PendingApproval, Is.EqualTo(f.PendingApproval), f.PathId);
            Assert.That(p.EngageVerbDisposition, Is.EqualTo(f.EngageVerbDisposition), f.PathId);
            Assert.That(p.EngageVerbReason, Is.EqualTo(f.EngageVerbReason), f.PathId);
            Assert.That(p.Roe, Is.EqualTo(f.Roe), f.PathId);
            Assert.That(p.Freshness, Is.EqualTo(f.Freshness), f.PathId);
            Assert.That(p.SourceRef, Is.EqualTo(f.SourceRef), f.PathId);
            Assert.That(p.ChainComplete, Is.EqualTo(f.ChainComplete), f.PathId);
        }

        Assert.That(production.Paths[0].Disposition, Is.EqualTo(TargetabilityAcceptDisposition.Permitted));
        Assert.That(production.Paths[1].Disposition, Is.EqualTo(TargetabilityAcceptDisposition.Withheld));
        Assert.That(production.Paths[1].WithheldCauseCode, Is.EqualTo(TargetabilityAcceptCauseCodes.WeaponsReleaseRequired));
    }

    [Test]
    public void Production_session_is_replay_stable()
    {
        var first = SliceATargetabilityAcceptanceHarness.Run(SliceATargetabilityComposition.ProductionSession).Evidence;
        var second = SliceATargetabilityAcceptanceHarness.Run(SliceATargetabilityComposition.ProductionSession).Evidence;

        Assert.That(second.AcceptanceFingerprint, Is.EqualTo(first.AcceptanceFingerprint));
    }

    [Test]
    public void Same_production_path_records_fire_when_shooter_is_an_engaging_agent()
    {
        var log = SliceATargetabilityAcceptanceHarness.DriveProductionSession("valid-target", WireEngageAgent);

        Assert.That(log.ContactChanges, Has.Count.EqualTo(3));
        Assert.That(log.Engagements, Is.Not.Empty);
    }

    private static void WireEngageAgent(SimulationSession session, UnitTarget shooter)
    {
        var agent = session.Orchestrator.CreateAgent(
            new AgentId("drg-350-engage"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous,
            policy: new EngageOnlyPolicy());
        session.Orchestrator.AssignAgentToTarget(agent, shooter, EffectivePolicy.DefaultFree);
        session.Orchestrator.Register(shooter);
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
