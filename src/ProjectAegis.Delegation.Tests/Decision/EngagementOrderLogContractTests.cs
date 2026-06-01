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
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Decision;

[TestFixture]
public sealed class EngagementOrderLogContractTests
{
    [Test]
    public void Mvp_session_logs_stable_abort_codes_in_fingerprint()
    {
        var session = SimulationSession.BindMvpEngagementForScenario(
            new DelegationOrchestrator(7),
            "restricted-engagement");
        WireEngageAgent(session);
        session.BeginExecution();
        session.Tick(new ObservedState(0, 2, 0, new Dictionary<TargetId, bool>()));

        var entry = session.Orchestrator.DecisionLog.Engagements[0];
        Assert.That(entry.AbortReasonCode, Is.EqualTo(nameof(EngagementAbortReason.OutOfEnvelope))
            .Or.EqualTo(nameof(EngagementAbortReason.DlzOut)));
        Assert.That(entry.AbortReason, Is.EqualTo(entry.AbortReasonCode));

        var fingerprint = session.Orchestrator.DecisionLog.ComputeFingerprint();
        Assert.That(fingerprint, Does.Contain("Engagement|"));
        Assert.That(fingerprint, Does.Contain(entry.AbortReasonCode));
    }

    [Test]
    public void Observed_state_without_track_aborts_NoFireControlTrack()
    {
        var session = SimulationSession.BindMvpEngagement(
            new DelegationOrchestrator(5),
            new EngageContext(50_000, new WeaponEnvelope(1_000, 100_000), RoundsRemaining: 2, HasFireControlTrack: true),
            defaultMagazineRounds: 2);
        WireEngageAgent(session);
        session.BeginExecution();
        session.Tick(new ObservedState(0, 1, 0, new Dictionary<TargetId, bool>(), HasFireControlTrack: false));

        Assert.That(session.Orchestrator.DecisionLog.Engagements[0].AbortReasonCode,
            Is.EqualTo(nameof(EngagementAbortReason.NoFireControlTrack)));
    }

    [Test]
    public void Launched_engagement_uses_Launched_code()
    {
        var session = SimulationSession.BindMvpEngagementForScenario(
            new DelegationOrchestrator(7),
            "baltic-patrol");
        WireEngageAgent(session);
        session.BeginExecution();

        for (var t = 0; t < 3; t++)
        {
            session.Tick(new ObservedState(t, 2, 0, new Dictionary<TargetId, bool>()));
        }

        Assert.That(
            session.Orchestrator.DecisionLog.Engagements.Any(e =>
                e.Launched && e.AbortReasonCode == EngagementAbortReasonCodes.Launched),
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