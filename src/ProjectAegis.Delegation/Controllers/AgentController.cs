namespace ProjectAegis.Delegation.Controllers;

using ProjectAegis.Delegation.Attention;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Policy;
using ProjectAegis.Delegation.Roe;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Delegation.Trust;

public sealed class AgentController : IController
{
    private readonly List<Order> _issued = new();
    private double _nextDecisionSimTime;

    public AgentController(
        AgentId id,
        TraitVector traits,
        AutonomyLevel autonomy,
        SeededRng rng,
        IPolicy policy,
        double attentionBudget)
    {
        Id = id;
        Traits = traits;
        Autonomy = autonomy;
        Rng = rng;
        Policy = policy;
        AttentionBudget = attentionBudget;
        Experience = new AgentExperienceBlob();
    }

    public AgentId Id { get; }

    public TraitVector Traits { get; }

    public AutonomyLevel Autonomy { get; set; }

    public SeededRng Rng { get; }

    public IPolicy Policy { get; }

    public double AttentionBudget { get; }

    public AgentExperienceBlob Experience { get; }

    public bool IsHuman => false;

    public IReadOnlyList<Order> DrainIssuedOrders()
    {
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

        log.Append(new DecisionRecord(
            state.SimTime,
            Id,
            targetId,
            Autonomy,
            choice.Chosen.Kind,
            candidates,
            choice.Rationale,
            attention.Load,
            attention.Budget,
            choice.RngDraw));

        var gateResult = gate.Evaluate(Autonomy, order, playerApproved: false);
        if (gateResult.ExecuteNow)
        {
            _issued.Add(order);
        }
    }
}
