using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Policy;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Tests.Helpers;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Catalog;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Sim;

[TestFixture]
public sealed class CatalogDamageHotTickEngageTests
{
    private static InMemoryCatalogReader BalticWithDamage() =>
        new(
            InMemoryCatalogReader.BalticPatrolFixture().GetSortedSensorBindings(),
            "baltic+damage-hot-tick",
            CatalogValidationDefaults.BalticPlatforms(),
            damage: [new CatalogPlatformDamage("u1", MaxHp: 100, WithdrawThresholdPct: 25)]);

    private static ScenarioPolicyProfile HotTickProfile() =>
        new(
            EffectivePolicy.DefaultFree,
            engageDefaults: new ScenarioEngageDefaults(
                45_000,
                5_000,
                120_000,
                defaultMagazineRounds: 4,
                hasFireControlTrack: true,
                combatDomainsEnabled: true),
            catalogWithdrawTargets:
            [
                new ScenarioCatalogWithdrawTarget("u1", 26),
            ]);

    [Test]
    public void Session_hot_tick_apply_logs_platform_damage_and_refreshes_withdraw_trials()
    {
        var catalog = BalticWithDamage();
        var orchestrator = new DelegationOrchestrator(17) { ScenarioPolicy = HotTickProfile() };
        var engage = CatalogEngageEnvelope.Apply(
            orchestrator.ScenarioPolicy!.ResolveEngageContext(),
            catalog);
        var session = SimulationSession.BindMvpEngagement(
            orchestrator,
            engage,
            defaultMagazineRounds: 4,
            catalogReader: catalog);

        Assert.That(session.CatalogDamageHotTickTracker, Is.Not.Null);

        var unit = new UnitTarget(new TargetId("u1"));
        var agent = orchestrator.CreateAgent(
            new AgentId("a1"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous,
            policy: new EngageOnlyPolicy());
        orchestrator.AssignAgentToTarget(agent, unit, EffectivePolicy.DefaultFree);
        orchestrator.Register(unit);
        session.BeginExecution();

        session.Tick(MvpObservedStates.EngageTick(0));
        session.Tick(MvpObservedStates.EngageTick(1));

        Assert.That(orchestrator.DecisionLog.PlatformDamageChanges, Is.Not.Empty);
        Assert.That(
            orchestrator.DecisionLog.PlatformDamageChanges.All(
                c => c.ReasonCode == PlatformDamageChangeReasonCodes.AmbientTick),
            Is.True);
        Assert.That(session.CatalogWithdrawTrials, Has.Count.EqualTo(1));
        Assert.That(session.CatalogWithdrawTrials[0].WithdrawRecommended, Is.True);
        Assert.That(orchestrator.DecisionLog.ComputeFingerprint(), Does.Contain("PlatformDamageChange|"));
    }

    [Test]
    public void Session_without_combat_domains_does_not_create_hot_tick_tracker()
    {
        var catalog = BalticWithDamage();
        var orchestrator = new DelegationOrchestrator(17)
        {
            ScenarioPolicy = new ScenarioPolicyProfile(
                EffectivePolicy.DefaultFree,
                catalogWithdrawTargets: [new ScenarioCatalogWithdrawTarget("u1", 20)]),
        };
        var engage = ScenarioEngageDefaults.MvpFallback.ToEngageContext(roundsRemaining: 4);
        var session = SimulationSession.BindMvpEngagement(orchestrator, engage, catalogReader: catalog);

        Assert.That(session.CatalogDamageHotTickTracker, Is.Null);
    }

    private sealed class EngageOnlyPolicy : IPolicy
    {
        public IReadOnlyList<ScoredIntent> GenerateCandidates(PerceivedState perceived, TraitVector traits) =>
            [new ScoredIntent(OrderKind.Engage, 1.0, RiskLevel.High)];
    }
}