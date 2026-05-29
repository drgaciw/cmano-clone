namespace ProjectAegis.Delegation.Decision;

using ProjectAegis.Delegation.Core;

public sealed record DecisionRecord(
    double SimTime,
    AgentId AgentId,
    TargetId TargetId,
    AutonomyLevel AutonomyLevel,
    OrderKind ChosenKind,
    IReadOnlyList<ScoredIntent> Alternatives,
    string Rationale,
    double AttentionLoad,
    double AttentionBudget,
    double RngDraw);
