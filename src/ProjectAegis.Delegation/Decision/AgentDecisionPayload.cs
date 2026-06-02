namespace ProjectAegis.Delegation.Decision;

using ProjectAegis.Delegation.Core;

/// <summary>ADR-003 / GDD order-log-replay: canonical <see cref="OrderLogEntryKind.AgentDecision"/> payload.</summary>
public sealed record AgentDecisionPayload(
    ulong SimTick,
    double SimTime,
    AgentId AgentId,
    TargetId TargetId,
    AutonomyLevel AutonomyLevel,
    OrderKind ChosenOrderKind,
    IReadOnlyList<ScoredIntent> ScoredIntents,
    string Rationale,
    double AttentionLoad,
    double AttentionBudget,
    double RngDraw)
{
    public static AgentDecisionPayload FromDecisionRecord(DecisionRecord record, ulong simTick) =>
        new(
            simTick != 0 ? simTick : record.SimTick,
            record.SimTime,
            record.AgentId,
            record.TargetId,
            record.AutonomyLevel,
            record.ChosenKind,
            record.Alternatives,
            record.Rationale,
            record.AttentionLoad,
            record.AttentionBudget,
            record.RngDraw);

    public DecisionRecord ToDecisionRecord() =>
        new(
            SimTime,
            AgentId,
            TargetId,
            AutonomyLevel,
            ChosenOrderKind,
            ScoredIntents,
            Rationale,
            AttentionLoad,
            AttentionBudget,
            RngDraw,
            SimTick);
}