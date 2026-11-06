using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Policy;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.Roe;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Delegation.Skills;
using ProjectAegis.Delegation.TargetabilityAccept;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
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
            Assert.That(path.ChainComplete, Is.False, path.PathId);
            Assert.That(path.Disposition, Is.EqualTo(TargetabilityAcceptDisposition.Withheld), path.PathId);
            Assert.That(path.WithheldCauseCode, Is.EqualTo(TargetabilityAcceptCauseCodes.NoEligibleShooter), path.PathId);
            Assert.That(path.AuthorityDisposition, Is.EqualTo(nameof(C2AuthorityDisposition.Permitted)), path.PathId);
            Assert.That(path.ContactChangeCount, Is.EqualTo(3), path.PathId);
            Assert.That(path.EngagementCount, Is.Zero, path.PathId);
            Assert.That(path.EngagementOutcomeCount, Is.Zero, path.PathId);
            Assert.That(path.PlayerOrderCount, Is.Zero, path.PathId);
            Assert.That(path.MagazineChangeCount, Is.Zero, path.PathId);
            Assert.That(path.OrdnanceStateChangeCount, Is.Zero, path.PathId);
        }
    }

    [Test]
    public void Production_session_kill_chain_follows_log_and_authority_ignores_fixture_block()
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
            Assert.That(p.Roe, Is.EqualTo(f.Roe), f.PathId);
            Assert.That(p.Freshness, Is.EqualTo(f.Freshness), f.PathId);
            Assert.That(p.SourceRef, Is.EqualTo(f.SourceRef), f.PathId);
            Assert.That(p.ChainComplete, Is.False, f.PathId);
            Assert.That(p.Disposition, Is.EqualTo(TargetabilityAcceptDisposition.Withheld), f.PathId);
            Assert.That(p.WithheldCauseCode, Is.EqualTo(TargetabilityAcceptCauseCodes.NoEligibleShooter), f.PathId);
            Assert.That(p.AuthorityDisposition, Is.EqualTo(nameof(C2AuthorityDisposition.Permitted)), f.PathId);
            Assert.That(p.PendingApproval, Is.Empty, f.PathId);
        }

        Assert.That(fixture.Paths[0].Disposition, Is.EqualTo(TargetabilityAcceptDisposition.Permitted));
        Assert.That(fixture.Paths[0].ChainComplete, Is.True);
        Assert.That(fixture.Paths[1].Disposition, Is.EqualTo(TargetabilityAcceptDisposition.Withheld));
        Assert.That(fixture.Paths[1].WithheldCauseCode, Is.EqualTo(TargetabilityAcceptCauseCodes.WeaponsReleaseRequired));
        Assert.That(fixture.Paths[1].AuthorityDisposition, Is.EqualTo(nameof(C2AuthorityDisposition.ApprovalRequired)));
        Assert.That(production.Paths[1].WithheldCauseCode, Is.Not.EqualTo(TargetabilityAcceptCauseCodes.WeaponsReleaseRequired));
        Assert.That(production.Paths[1].AuthorityDisposition, Is.Not.EqualTo(nameof(C2AuthorityDisposition.ApprovalRequired)));
    }

    [Test]
    public void Production_verdict_follows_session_magazine_fire_control_and_policy_seams()
    {
        var unseeded = SliceATargetabilityAcceptanceHarness.Run(SliceATargetabilityComposition.ProductionSession).Evidence;
        Assert.That(unseeded.Paths[0].ChainComplete, Is.False);
        Assert.That(unseeded.Paths[0].WithheldCauseCode, Is.EqualTo(TargetabilityAcceptCauseCodes.NoEligibleShooter));
        Assert.That(unseeded.Paths[1].WithheldCauseCode, Is.EqualTo(TargetabilityAcceptCauseCodes.NoEligibleShooter));

        var seeded = SliceATargetabilityAcceptanceHarness.RunProduction(SeedOneRound).Evidence;
        Assert.That(seeded.Paths[0].ChainComplete, Is.True);
        Assert.That(seeded.Paths[0].Disposition, Is.EqualTo(TargetabilityAcceptDisposition.Permitted));
        Assert.That(seeded.Paths[0].WithheldCauseCode, Is.EqualTo(TargetabilityAcceptCauseCodes.None));
        Assert.That(seeded.Paths[0].MagazineChangeCount, Is.Zero);
        Assert.That(seeded.Paths[1].Disposition, Is.EqualTo(TargetabilityAcceptDisposition.Permitted));
        Assert.That(seeded.Paths[1].AuthorityDisposition, Is.EqualTo(nameof(C2AuthorityDisposition.Permitted)));
        Assert.That(seeded.Paths[1].PendingApproval, Is.Empty);
        Assert.That(seeded.Paths[1].WithheldCauseCode, Is.EqualTo(TargetabilityAcceptCauseCodes.None));

        var held = SliceATargetabilityAcceptanceHarness.RunProduction(SeedOneRoundAndHoldFire).Evidence;
        Assert.That(held.Paths[0].ChainComplete, Is.True);
        Assert.That(held.Paths[0].Roe, Is.EqualTo(nameof(RoeLevel.HoldFire)));
        Assert.That(held.Paths[0].Disposition, Is.EqualTo(TargetabilityAcceptDisposition.Withheld));
        Assert.That(held.Paths[0].WithheldCauseCode, Is.EqualTo(TargetabilityAcceptCauseCodes.RoeHoldFire));
        Assert.That(held.Paths[0].AuthorityDisposition, Is.EqualTo(nameof(C2AuthorityDisposition.Withheld)));
        Assert.That(held.Paths[1].WithheldCauseCode, Is.EqualTo(TargetabilityAcceptCauseCodes.RoeHoldFire));

        var noTrack = SliceATargetabilityAcceptanceHarness.RunProduction(SeedOneRoundAndDropFireControl).Evidence;
        Assert.That(noTrack.Paths[0].TechnicallyTargetable, Is.False);
        Assert.That(noTrack.Paths[0].Phase, Is.EqualTo(KillChainPhase.Track));
        Assert.That(noTrack.Paths[0].Disposition, Is.EqualTo(TargetabilityAcceptDisposition.Withheld));
        Assert.That(noTrack.Paths[0].WithheldCauseCode, Is.EqualTo(TargetabilityAcceptCauseCodes.NoFireControl));
        Assert.That(noTrack.Paths[1].Phase, Is.EqualTo(KillChainPhase.Track));
        Assert.That(noTrack.Paths[1].WithheldCauseCode, Is.EqualTo(TargetabilityAcceptCauseCodes.NoFireControl));
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

    [Test]
    public void HasFireControlTrack_respects_observed_tick_loss_for_human_and_primed_contexts()
    {
        var profile = ScenarioPolicyRepository.TryGet("baltic-patrol")!;
        var orchestrator = new DelegationOrchestrator(42) { ScenarioPolicy = profile };
        var session = SimulationSession.BindMvpEngagementForScenario(
            orchestrator,
            "baltic-patrol",
            Data.Catalog.InMemoryCatalogReader.BalticPatrolFixture());
        var shooterId = new TargetId("friendly-1");
        var hostileId = new TargetId("hostile-1");
        var shooter = new UnitTarget(shooterId);
        SeedOneRound(session, shooter);
        session.BeginExecution();
        orchestrator.DecisionLog.AppendContactChange(
            new ContactChangeRecord(0, 1.0, 1, "obs-1", "contact-1", hostileId.Value, "Detected", "Hostile"));
        var alive = new Dictionary<TargetId, bool> { [shooterId] = true };

        Assert.That(
            session.Tick(new ObservedState(1, 1, 0, alive, HasFireControlTrack: false, PrimaryHostileContactId: hostileId)),
            Is.True);
        Assert.That(
            session.HasFireControlTrack("contact-1", hostileId.Value, shooterId.Value),
            Is.False,
            "Human non-engaging tick with HasFireControlTrack=false must not fall back to scenario template true.");

        session.EngageWorld!.Set(
            new EngageRequest(
                OrderActionMapper.TargetIdToUlong(shooterId),
                OrderActionMapper.TargetIdToUlong(hostileId),
                MountId: 0,
                SimTick: 1),
            session.DefaultEngageContext!.Value with { HasFireControlTrack = true });
        Assert.That(
            session.Tick(new ObservedState(2, 1, 0, alive, HasFireControlTrack: true, PrimaryHostileContactId: hostileId)),
            Is.True);
        Assert.That(session.HasFireControlTrack("contact-1", hostileId.Value, shooterId.Value), Is.True);

        Assert.That(
            session.Tick(new ObservedState(3, 1, 0, alive, HasFireControlTrack: false, PrimaryHostileContactId: hostileId)),
            Is.True);
        Assert.That(
            session.HasFireControlTrack("contact-1", hostileId.Value, shooterId.Value),
            Is.False,
            "Subsequent non-engaging tick losing fire-control track must override earlier primed context.");
    }

    private static void SeedOneRound(SimulationSession session, UnitTarget shooter)
    {
        shooter.Slot.SetActive(new HumanController());
        session.Orchestrator.Register(shooter);
        session.Magazines!.SetRounds(OrderActionMapper.TargetIdToUlong(shooter.Id), mountId: 0, rounds: 1);
    }

    private static void SeedOneRoundAndHoldFire(SimulationSession session, UnitTarget shooter)
    {
        SeedOneRound(session, shooter);
        session.Orchestrator.ApplyRoeToUnits(
            [shooter.Id.Value],
            new EffectivePolicy(RoeLevel.HoldFire),
            isFriendly: true,
            simTime: 0,
            simTick: 0);
    }

    private static void SeedOneRoundAndDropFireControl(SimulationSession session, UnitTarget shooter)
    {
        SeedOneRound(session, shooter);
        var template = session.DefaultEngageContext!.Value with { HasFireControlTrack = false };
        session.EngageWorld!.Set(
            new EngageRequest(
                OrderActionMapper.TargetIdToUlong(shooter.Id),
                OrderActionMapper.TargetIdToUlong(new TargetId("hostile-1")),
                MountId: 0,
                SimTick: 0),
            template);
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
