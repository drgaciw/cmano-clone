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
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Sim;

[TestFixture]
public sealed class CatalogPhaseBReadinessEngageTests
{
    [Test]
    public void Session_blocks_engage_when_catalog_mobility_has_zero_range()
    {
        var catalog = new InMemoryCatalogReader(
            [
                new CatalogSensorBinding("u1", "radar-1", 1.0, "baltic-fixture-radar1"),
            ],
            "phase-b-mobility-block-fixture",
            CatalogValidationDefaults.BalticPlatforms(),
            loadouts: [new CatalogLoadout("u1", "asuw-default", "ASUW Default", "asuw", IsDefault: true)],
            magazines:
            [
                new CatalogMagazineEntry("u1", "asuw-default", "vls-fwd", CatalogWeaponIds.MvpDefault, 2),
            ],
            mobility: [new CatalogMobility("u1", MaxSpeedKnots: 32, RangeNm: 0)]);
        var orchestrator = new DelegationOrchestrator(17);
        var engage = CatalogEngageEnvelope.Apply(
            ScenarioEngageDefaults.MvpFallback.ToEngageContext(roundsRemaining: 2),
            catalog);
        var session = SimulationSession.BindMvpEngagement(
            orchestrator,
            engage,
            defaultMagazineRounds: 2,
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

        Assert.That(session.Sim.LastEngagementResults, Is.Not.Empty);
        Assert.That(session.Sim.LastEngagementResults[0].Launched, Is.False);
        Assert.That(session.Sim.LastEngagementResults[0].AbortReason, Is.EqualTo(EngagementAbortReason.AirNotReady));
    }

    [Test]
    public void Session_blocks_engage_when_catalog_emcon_posture_is_off()
    {
        var catalog = new InMemoryCatalogReader(
            [
                new CatalogSensorBinding("u1", "radar-1", 1.0, "baltic-fixture-radar1"),
            ],
            "phase-b-emcon-block-fixture",
            CatalogValidationDefaults.BalticPlatforms(),
            loadouts: [new CatalogLoadout("u1", "asuw-default", "ASUW Default", "asuw", IsDefault: true)],
            magazines:
            [
                new CatalogMagazineEntry("u1", "asuw-default", "vls-fwd", CatalogWeaponIds.MvpDefault, 2),
            ],
            emcon: [new CatalogEmcon("u1", "free", "radar-1", "off")]);
        var orchestrator = new DelegationOrchestrator(19);
        var engage = CatalogEngageEnvelope.Apply(
            ScenarioEngageDefaults.MvpFallback.ToEngageContext(roundsRemaining: 2),
            catalog);
        var session = SimulationSession.BindMvpEngagement(
            orchestrator,
            engage,
            defaultMagazineRounds: 2,
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

        Assert.That(session.Sim.LastEngagementResults, Is.Not.Empty);
        Assert.That(session.Sim.LastEngagementResults[0].Launched, Is.False);
        Assert.That(session.Sim.LastEngagementResults[0].AbortReason, Is.EqualTo(EngagementAbortReason.EmconOff));
    }

    [Test]
    public void Session_allows_engage_with_Baltic_phase_b_fixture_rows()
    {
        var catalog = InMemoryCatalogReader.BalticPhaseBFixture();
        var orchestrator = new DelegationOrchestrator(23);
        var engage = CatalogEngageEnvelope.Apply(
            ScenarioEngageDefaults.MvpFallback.ToEngageContext(roundsRemaining: 0),
            catalog);
        var session = SimulationSession.BindMvpEngagement(
            orchestrator,
            engage,
            defaultMagazineRounds: 2,
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

        Assert.That(session.Sim.LastEngagementResults, Is.Not.Empty);
        Assert.That(session.Sim.LastEngagementResults[0].Launched, Is.True);
    }

    private sealed class EngageOnlyPolicy : IPolicy
    {
        public IReadOnlyList<ScoredIntent> GenerateCandidates(PerceivedState perceived, TraitVector traits) =>
            [new ScoredIntent(OrderKind.Engage, 1.0, RiskLevel.High)];
    }
}