namespace ProjectAegis.Delegation.Orchestration;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Groups;
using ProjectAegis.Delegation.Policy;
using ProjectAegis.Delegation.Roe;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Sim.Policy;
using SimPolicy = ProjectAegis.Sim.Policy;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Delegation.Hindsight;
using ProjectAegis.Delegation.Trust;
using ProjectAegis.Sim.Scenario;

public sealed class DelegationOrchestrator
{
    private readonly List<ICommandableTarget> _targets = new();
    private readonly AutonomyGate _autonomyGate;
    private readonly SimPolicy.IPolicyEvaluator _policyEvaluator;
    private readonly PolicySnapshotRegistry _policySnapshots = new();
    private readonly OverrideService _overrideService = new();
    private readonly DetachRejoinService _detachRejoinService;
    private readonly List<TrustSignal> _trustSignals = new();
    private long _orderIdSequence = 1;

    public DelegationOrchestrator(int globalSeed, SimPolicy.IPolicyEvaluator? policyEvaluator = null)
        : this(globalSeed, policyEvaluator, hindsight: null)
    {
    }

    public DelegationOrchestrator(
        int globalSeed,
        SimPolicy.IPolicyEvaluator? policyEvaluator,
        HindsightOptions? hindsight)
    {
        GlobalSeed = globalSeed;
        DecisionLog = new DecisionLog();
        Hindsight = HindsightIntegration.TryCreate(hindsight);
        if (Hindsight is not null)
        {
            DecisionLog.HindsightHook = Hindsight.OrderLogHook;
        }

        _detachRejoinService = new DetachRejoinService(_overrideService);
        _policyEvaluator = policyEvaluator ?? new PolicyEvaluator(ResolvePolicyForUnit);
        _autonomyGate = new AutonomyGate(
            new RoePolicyAdapter(_policyEvaluator, _policySnapshots.CreateContext));
    }

    /// <summary>Optional Hindsight sidecar; null in CI/replay unless explicitly enabled.</summary>
    public HindsightIntegration? Hindsight { get; }

    public SimPolicy.IPolicyEvaluator PolicyEvaluator => _policyEvaluator;

    public EffectivePolicy ResolveEffectivePolicyForUnit(ulong unitId) => ResolvePolicyForUnit(unitId);

    public PolicySnapshotRegistry PolicySnapshots => _policySnapshots;

    public ScenarioPolicyProfile? ScenarioPolicy { get; set; }

    /// <summary>
    /// When true, all human ingress paths are blocked (req 03 AvA observer attach).
    /// Enforced at both the orchestrator boundary (TryTakeDirectControl/TryReleaseDirectControl)
    /// and the Unity bridge (DelegationBridge.TryEnqueueHumanOrder).
    /// </summary>
    public bool AttachReplayViewer { get; set; }

    public SimulationPhase Phase { get; private set; } = SimulationPhase.Planning;

    public int GlobalSeed { get; }

    public DecisionLog DecisionLog { get; }

    /// <summary>ADR-003 C1: unified order-log read surface.</summary>
    public IOrderLog OrderLog => DecisionLog;

    public IReadOnlyList<Order> ExecutedOrders { get; private set; } = Array.Empty<Order>();

    public IReadOnlyList<TrustSignal> TrustSignals => _trustSignals;

    public void Register(ICommandableTarget target) => _targets.Add(target);

    public AgentController CreateAgent(
        AgentId id,
        TraitVector traits,
        AutonomyLevel autonomy,
        double attentionBudget = 20,
        IPolicy? policy = null)
    {
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

    public AgentController CreateAgentFromPreset(
        AgentId id,
        PersonalityPreset preset,
        AutonomyLevel autonomy,
        IPolicy? policy = null)
    {
        var agent = CreateAgent(
            id,
            preset.Traits,
            autonomy,
            PersonalityCatalog.ResolveAttentionBudget(preset),
            policy);
        agent.SetPersonalitySlug(preset.Name);
        return agent;
    }

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
        agent.BindPolicySnapshot(snapshotId, policy, DecisionLog);
        Hindsight?.OrderLogHook.RegisterAgent(agent.Id, agent.PersonalitySlug);
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

    public void BeginExecution(double simTime = 0, ulong simTick = 0)
    {
        if (Phase == SimulationPhase.Planning)
        {
            DecisionLog.AppendModeChange(new ModeChangeRecord(
                0,
                simTime,
                simTick,
                nameof(SimulationPhase.Planning),
                nameof(SimulationPhase.Executing)));
        }

        Phase = SimulationPhase.Executing;
    }

    public bool TryTakeDirectControl(UnitTarget unit, double simTime)
    {
        if (AttachReplayViewer)
        {
            return false;
        }

        var previous = DescribeActiveController(unit.Slot);
        var parentGroup = FindParentGroup(unit.Id);

        if (parentGroup is not null)
        {
            _detachRejoinService.Detach(parentGroup, unit);
            DecisionLog.AppendGroupMemberDetach(new GroupMemberDetachRecord(0, simTime, parentGroup.Id, unit.Id));
        }
        else if (unit.Slot.Active is AgentController)
        {
            _overrideService.TakeDirectControl(unit, new HumanController());
        }
        else
        {
            unit.Slot.SetActive(new HumanController());
        }

        var agentId = unit.Slot.SuspendedAgent?.Id
            ?? (unit.Slot.Active is AgentController active ? active.Id : null);
        DecisionLog.AppendControllerChange(new ControllerChangeRecord(
            0, simTime, unit.Id, previous, "Human", agentId));
        return true;
    }

    public bool TryReleaseDirectControl(UnitTarget unit, double simTime)
    {
        if (AttachReplayViewer)
        {
            return false;
        }

        if (unit.Slot.Active is not HumanController)
        {
            return false;
        }

        if (unit.IsDetachedFromGroup && unit.DetachedFromGroupId is { } groupId &&
            FindTarget(groupId) is GroupTarget parentGroup)
        {
            _detachRejoinService.Rejoin(parentGroup, unit);
            DecisionLog.AppendGroupMemberRejoin(new GroupMemberRejoinRecord(0, simTime, parentGroup.Id, unit.Id));
        }
        else
        {
            _overrideService.ReleaseDirectControl(unit);
        }

        var resumed = unit.Slot.Active is AgentController agent ? agent.Id : (AgentId?)null;
        DecisionLog.AppendControllerChange(new ControllerChangeRecord(
            0, simTime, unit.Id, "Human", DescribeActiveController(unit.Slot), resumed));
        return true;
    }

    public GroupTarget? FindParentGroup(TargetId unitId)
    {
        foreach (var target in _targets)
        {
            if (target is GroupTarget group && group.Members.Contains(unitId))
            {
                return group;
            }
        }

        return null;
    }

    private ICommandableTarget? FindTarget(TargetId id)
    {
        foreach (var target in _targets)
        {
            if (target.Id == id)
            {
                return target;
            }
        }

        return null;
    }

    private static string DescribeActiveController(ControllerSlot slot) =>
        slot.Active switch
        {
            HumanController => "Human",
            AgentController => "Agent",
            null when slot.SuspendedAgent is not null => "AgentSuspended",
            _ => "None",
        };

    public void Tick(ObservedState state)
    {
        if (Phase == SimulationPhase.Planning)
        {
            ExecutedOrders = Array.Empty<Order>();
            return;
        }

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
        TraitVector traits)
    {
        var verdict = LoopPolicyGate.CanEditPersonality(ScenarioPolicy, Phase, agent.Autonomy);
        if (!verdict.Allowed)
        {
            return verdict;
        }

        agent.RebindTraits(traits);
        return LoopPolicyVerdict.Allow();
    }

    public IReadOnlyList<OrderLogEntry> GetLiveOrderLogView() =>
        PlayerInfoFilter.FilterLiveEntries(
            DecisionLog.ChronologicalEntries(),
            LoopPolicyGate.ResolvePlayerInfoModel(ScenarioPolicy));

    public IReadOnlyList<TrustSignal> FinalizeScenario(
        bool missionSucceeded = false,
        double objectivesMetRatio = 1.0)
    {
        _trustSignals.Clear();
        _trustSignals.AddRange(
            TrustSignalEmitter.EmitFromSession(DecisionLog, missionSucceeded, objectivesMetRatio));
        Hindsight?.OnScenarioFinalized(DecisionLog, _trustSignals, missionSucceeded, objectivesMetRatio);
        return _trustSignals;
    }
}
