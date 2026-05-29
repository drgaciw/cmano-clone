namespace ProjectAegis.Delegation.Orchestration;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Policy;
using ProjectAegis.Delegation.Roe;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Delegation.Trust;

public sealed class DelegationOrchestrator
{
    private readonly List<ICommandableTarget> _targets = new();
    private readonly AutonomyGate _autonomyGate = new(new PassthroughRoeFilter());
    private long _orderIdSequence = 1;

    public DelegationOrchestrator(int globalSeed)
    {
        GlobalSeed = globalSeed;
        DecisionLog = new DecisionLog();
    }

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
        var salt = id.Value.GetHashCode(StringComparison.Ordinal);
        var rng = new SeededRng(GlobalSeed, salt);
        return new AgentController(
            id,
            traits,
            autonomy,
            rng,
            policy ?? new StubPatrolPolicy(),
            attentionBudget);
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
}
