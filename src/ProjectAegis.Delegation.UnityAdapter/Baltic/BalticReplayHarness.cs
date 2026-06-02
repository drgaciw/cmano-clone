namespace ProjectAegis.Delegation.UnityAdapter.Baltic;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Policy;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;

/// <summary>Headless Baltic slice runner for CLI and replay-verify gate.</summary>
public static class BalticReplayHarness
{
    public sealed record Result(int Seed, string ScenarioPolicyId, int Ticks, string Fingerprint, int EngagementCount);

    public static Result Run(int seed, string scenarioPolicyId, int ticks, bool mvpEngagement = true)
    {
        if (ticks < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(ticks), "ticks must be >= 1");
        }

        ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
        var profile = ScenarioPolicyRepository.TryGet(scenarioPolicyId);
        var contactSim = profile?.ContactSeeds.Count > 0
            ? new ScenarioContactSimulator(profile.ContactSeeds, profile.UnitRadarEmcon)
            : null;

        var bridge = new DelegationBridge(seed, mvpEngagement: mvpEngagement, scenarioPolicyId: scenarioPolicyId);
        if (mvpEngagement && bridge.Session == null)
        {
            throw new InvalidOperationException("MVP engage session was not created.");
        }

        var unit = new UnitTarget(new TargetId("u1"));
        var agent = bridge.Orchestrator.CreateAgent(
            new AgentId("a1"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous,
            policy: new EngageOnlyPolicy());
        bridge.Orchestrator.AssignAgentToTarget(agent, unit, EffectivePolicy.DefaultFree);
        bridge.Orchestrator.Register(unit);
        bridge.BeginExecution();

        var harness = new HeadlessSnapshot(contactSim, profile?.UnitRadarEmcon, fallbackContactCount: 2, fallbackHasTrack: true);
        for (var t = 0; t < ticks; t++)
        {
            harness.Advance(1.0);
            var simTick = (ulong)Math.Max(0, (long)harness.SimTime);
            if (contactSim != null)
            {
                foreach (var transition in contactSim.Tick(simTick, harness.SimTime))
                {
                    bridge.Orchestrator.DecisionLog.AppendContactTransition(transition);
                }
            }

            bridge.Tick(harness, harness);
        }

        return new Result(
            seed,
            scenarioPolicyId,
            ticks,
            bridge.Orchestrator.DecisionLog.ComputeFingerprint(),
            bridge.Orchestrator.DecisionLog.Engagements.Count);
    }

    private sealed class HeadlessSnapshot : ISimWorldSnapshot, IOrderSink
    {
        private readonly ScenarioContactSimulator? _contacts;
        private readonly IReadOnlyDictionary<string, EmconState>? _unitRadarEmcon;
        private readonly int _fallbackContactCount;
        private readonly bool _fallbackHasTrack;
        private double _simTime;

        public HeadlessSnapshot(
            ScenarioContactSimulator? contacts,
            IReadOnlyDictionary<string, EmconState>? unitRadarEmcon,
            int fallbackContactCount,
            bool fallbackHasTrack)
        {
            _contacts = contacts;
            _unitRadarEmcon = unitRadarEmcon;
            _fallbackContactCount = fallbackContactCount;
            _fallbackHasTrack = fallbackHasTrack;
        }

        public double SimTime => _simTime;

        public int ContactCount =>
            _contacts != null ? _contacts.ActiveCount : _fallbackContactCount;

        public int ActiveEngagementCount => 0;

        public TargetId? PrimaryHostileContactId
        {
            get
            {
                if (_contacts?.PrimaryTargetId is { } id)
                {
                    return new TargetId(id);
                }

                return ContactCount > 0 ? new TargetId("hostile-1") : null;
            }
        }

        public bool HasFireControlTrackOnPrimaryContact
        {
            get
            {
                if (_contacts != null && _contacts.ActiveCount > 0)
                {
                    return _contacts.PrimaryHasFireControlTrack;
                }

                return ContactCount > 0 && _fallbackHasTrack;
            }
        }

        public bool ObserverRadarEmconActive =>
            ScenarioEmconResolver.ResolveRadar("u1", _unitRadarEmcon) == EmconState.Active;

        public void Advance(double delta) => _simTime += delta;

        public bool IsMemberAlive(TargetId memberId) => true;

        public void ApplyOrder(EntityKey entity, in Order order) { }
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