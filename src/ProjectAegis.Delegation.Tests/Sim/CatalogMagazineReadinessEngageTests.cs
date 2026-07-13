using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Policy;
using ProjectAegis.Delegation.Roe;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Tests.Helpers;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Sim;

[TestFixture]
public sealed class CatalogMagazineReadinessEngageTests
{
    [Test]
    public void Session_seeds_magazine_from_catalog_not_scenario_fallback()
    {
        var catalog = InMemoryCatalogReader.BalticMagazineFixture(magazineQuantity: 2);
        var orchestrator = new DelegationOrchestrator(11);
        var engage = CatalogEngageEnvelope.Apply(
            ScenarioEngageDefaults.MvpFallback.ToEngageContext(roundsRemaining: 0),
            catalog);
        var session = SimulationSession.BindMvpEngagement(
            orchestrator,
            engage,
            defaultMagazineRounds: 99,
            catalogReader: catalog);

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

        var shooterId = OrderActionMapper.TargetIdToUlong(new TargetId("u1"));
        Assert.That(session.Magazines!.GetRounds(shooterId, 0), Is.EqualTo(1));
        Assert.That(session.Sim.LastEngagementResults, Is.Not.Empty);
        Assert.That(session.Sim.LastEngagementResults[0].Launched, Is.True);
    }

    private sealed class EngageOnlyPolicy : IPolicy
    {
        public IReadOnlyList<ScoredIntent> GenerateCandidates(PerceivedState perceived, TraitVector traits) =>
            [new ScoredIntent(OrderKind.Engage, 1.0, RiskLevel.High)];
    }
}