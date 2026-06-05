namespace ProjectAegis.Delegation.Controllers;

using ProjectAegis.Delegation.Attention;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Policy;
using ProjectAegis.Delegation.Roe;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Delegation.Trust;

public sealed class AgentController : IController
{
    private readonly List<Order> _issued = new();
    private double _nextDecisionSimTime;
    private TraitVector _traits;

    public AgentController(
        AgentId id,
        TraitVector traits,
        AutonomyLevel autonomy,
        SeededRng rng,
        IPolicy policy,
        double attentionBudget)
    {
        Id = id;
        _traits = traits;
        Autonomy = autonomy;
        Rng = rng;
        Policy = policy;
        AttentionBudget = attentionBudget;
        Experience = new AgentExperienceBlob();
    }

    public AgentId Id { get; }

    public TraitVector Traits => _traits;

    public AutonomyLevel Autonomy { get; set; }

    public SeededRng Rng { get; }

    public IPolicy Policy { get; }

    public double AttentionBudget { get; }

    public AgentExperienceBlob Experience { get; }

    /// <summary>Personality preset name when created via <see cref="DelegationOrchestrator.CreateAgentFromPreset"/>.</summary>
    public string? PersonalitySlug { get; private set; }

    public ulong PolicySnapshotId { get; private set; }

    internal void SetPersonalitySlug(string slug) => PersonalitySlug = slug;

    public EffectivePolicy EffectivePolicy { get; private set; } = EffectivePolicy.DefaultFree;

    public bool IsHuman => false;

    public void BindPolicySnapshot(ulong policySnapshotId, EffectivePolicy effective, DecisionLog? log = null, double simTime = 0, ulong simTick = 0)
    {
        if (log != null && policySnapshotId != PolicySnapshotId)
        {
            log.AppendPolicyUpdate(new PolicyUpdateRecord(
                0,
                simTime,
                simTick,
                policySnapshotId,
                "roe",
                EffectivePolicy.Roe.ToString(),
                effective.Roe.ToString()));
        }

        PolicySnapshotId = policySnapshotId;
        EffectivePolicy = effective;
    }

    public void RebindTraits(TraitVector traits) => _traits = traits;

    public IReadOnlyList<Order> DrainIssuedOrders(ulong currentSimTick)
    {
        _ = currentSimTick;
        if (_issued.Count == 0)
        {
            return Array.Empty<Order>();
        }

        var copy = _issued.ToArray();
        _issued.Clear();
        return copy;
    }

    public void TryDecide(
        TargetId targetId,
        ObservedState state,
        int memberCount,
        ref long orderIdSequence,
        AutonomyGate gate,
        DecisionLog log)
    {
        if (state.SimTime < _nextDecisionSimTime)
        {
            return;
        }

        var attention = AttentionCalculator.Evaluate(AttentionBudget, memberCount, state);
        var delay = attention.Degradation.SlowerReactions
            ? Math.Max(1.0, Traits.ReactionDelay * 5.0)
            : Traits.ReactionDelay;
        _nextDecisionSimTime = state.SimTime + delay;

        var perceived = PerceivedStateFactory.FromFull(state, Traits.SituationalAwareness);
        var candidates = Policy.GenerateCandidates(perceived, Traits);
        var choice = DecisionPipeline.Choose(candidates, Traits, attention, Rng);

        var order = new Order(
            new OrderId(orderIdSequence++),
            targetId,
            state.SimTime,
            choice.Chosen.Kind,
            DefaultRiskClassifier.Classify(choice.Chosen.Kind));

        var simTick = (ulong)Math.Max(0, (long)state.SimTime);
        log.Append(OrderLogEntry.FromDecisionRecord(
            new DecisionRecord(
                state.SimTime,
                Id,
                targetId,
                Autonomy,
                choice.Chosen.Kind,
                candidates,
                choice.Rationale,
                attention.Load,
                attention.Budget,
                choice.RngDraw,
                simTick),
            simTick));

        var gateResult = gate.Evaluate(Autonomy, order, playerApproved: false);
        if (gateResult.Rejected && gateResult.PolicyDenialReason != FireAbortReason.None)
        {
            log.Append(OrderLogEntryFactories.FromPolicyDenial(new PolicyDenialRecord(
                SequenceId: 0,
                state.SimTime,
                SimTick: (ulong)Math.Max(0, (long)state.SimTime),
                Id,
                targetId,
                PolicySnapshotId,
                gateResult.PolicyDenialReason,
                order.Kind)));
        }

        if (gateResult.ExecuteNow)
        {
            _issued.Add(order);
        }
    }
}
