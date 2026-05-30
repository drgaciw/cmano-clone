namespace ProjectAegis.Delegation.Orchestration;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Policy;
using ProjectAegis.Delegation.Roe;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Sim.Policy;
using SimPolicy = ProjectAegis.Sim.Policy;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Delegation.Trust;
using ProjectAegis.Sim.Scenario;

public sealed class DelegationOrchestrator
{
    private readonly List<ICommandableTarget> _targets = new();
    private readonly AutonomyGate _autonomyGate;
    private readonly PolicySnapshotRegistry _policySnapshots = new();
    private long _orderIdSequence = 1;

    public DelegationOrchestrator(int globalSeed, SimPolicy.IPolicyEvaluator? policyEvaluator = null)
    {
        GlobalSeed = globalSeed;
        DecisionLog = new DecisionLog();
        var evaluator = policyEvaluator ?? new PolicyEvaluator(ResolvePolicyForUnit);
        _autonomyGate = new AutonomyGate(
            new RoePolicyAdapter(evaluator, _policySnapshots.CreateContext));
    }

    public PolicySnapshotRegistry PolicySnapshots => _policySnapshots;

    public ScenarioPolicyProfile? ScenarioPolicy { get; set; }

    public int GlobalSeed { get; }

    public DecisionLog DecisionLog { get; }

    public IReadOnlyList<Order> ExecutedOrders { get; private set; } = Array.Empty<Order>();

    public IReadOnlyList<TrustSignal> TrustSignals { get; } = new List<TrustSignal>();

    public void Register(ICommandableTarget target) => _targets.Add(target);

    public AgentController CreateAgent(
        AgentId id,
        TraitVector traits,
        AutonomyLevel autonomy,
        double attentionBudget = 20,
        IPolicy? policy = null)
    {
        // Must be a deterministic, cross-process-stable hash: string.GetHashCode is
        // randomized per process, which would make each agent's RNG stream differ
        // run-to-run and break replay reproducibility (DET-001).
        var salt = DeterministicHash.OrdinalHash(id.Value);
        var rng = new SeededRng(GlobalSeed, salt);
        return new AgentController(
            id,
            traits,
            autonomy,
            rng,
            policy ?? new StubPatrolPolicy(),
            attentionBudget);
    }

    /// <summary>Assign agent to target and freeze policy snapshot (TR-policy-003).</summary>
    public void AssignAgentToTarget(
        AgentController agent,
        ICommandableTarget target,
        EffectivePolicy effectivePolicy,
        ulong capturedAtSimTick = 0) =>
        AssignAgentToTarget(agent, target, effectivePolicy, isFriendly: true, capturedAtSimTick);

    public void AssignAgentToTarget(
        AgentController agent,
        ICommandableTarget target,
        EffectivePolicy? effectivePolicy,
        bool isFriendly,
        ulong capturedAtSimTick = 0)
    {
        var policy = effectivePolicy
            ?? ResolveScenarioPolicyForTarget(target.Id, isFriendly);
        var snapshotId = _policySnapshots.Capture(target.Id, policy, capturedAtSimTick);
        agent.BindPolicySnapshot(snapshotId, policy);
        target.Slot.SetActive(agent);
    }

    public EffectivePolicy ResolveScenarioPolicyForTarget(TargetId targetId, bool isFriendly)
    {
        if (ScenarioPolicy != null)
        {
            return ScenarioPolicy.ResolveForUnit(targetId.Value, isFriendly);
        }

        return EffectivePolicy.DefaultFree;
    }

    private EffectivePolicy ResolvePolicyForUnit(ulong unitId)
    {
        foreach (var pair in _policySnapshots.Snapshots)
        {
            if (OrderActionMapper.TargetIdToUlong(pair.Key) == unitId)
            {
                return pair.Value.Effective;
            }
        }

        return EffectivePolicy.DefaultFree;
    }

    public void Tick(ObservedState state)
    {
        var executed = new List<Order>();

        foreach (var target in _targets)
        {
            if (target is GroupTarget group && group.PendingReplan)
            {
                group.ClearReplanPending();
            }

            var memberCount = target is GroupTarget g ? g.Members.Count : 1;

            switch (target.Slot.Active)
            {
                case AgentController agent:
                    agent.TryDecide(
                        target.Id,
                        state,
                        memberCount,
                        ref _orderIdSequence,
                        _autonomyGate,
                        DecisionLog);
                    executed.AddRange(agent.DrainIssuedOrders());
                    break;
                case HumanController human:
                    executed.AddRange(human.DrainIssuedOrders());
                    break;
            }
        }

        ExecutedOrders = executed;
    }

    public LoopPolicyVerdict TryRebindAgentTraits(
        AgentController agent,
        TraitVector traits,
        SimulationPhase phase)
    {
        var verdict = LoopPolicyGate.CanEditPersonality(ScenarioPolicy, phase, agent.Autonomy);
        if (!verdict.Allowed)
        {
            return verdict;
        }

        agent.RebindTraits(traits);
        return LoopPolicyVerdict.Allow();
    }
}
