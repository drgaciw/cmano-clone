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

        _ = scenarioPolicyId;
        var bridge = new DelegationBridge(seed, mvpEngagement: mvpEngagement);
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

        var harness = new HeadlessSnapshot(contactCount: 2);
        for (var t = 0; t < ticks; t++)
        {
            harness.Advance(1.0);
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
        private double _simTime;

        public HeadlessSnapshot(int contactCount) => ContactCount = contactCount;

        public double SimTime => _simTime;

        public int ContactCount { get; }

        public int ActiveEngagementCount => 0;

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