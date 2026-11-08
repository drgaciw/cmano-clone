namespace ProjectAegis.Delegation.Projection;

/// <summary>Projection-only facility capacity transition derived from engagement outcomes (ADR-009 stub).</summary>
public sealed record FacilityDamageChangeRecord(
    ulong SequenceId,
    double SimTime,
    ulong SimTick,
    string FacilityId,
    string TargetId,
    string PreviousState,
    string NewState);