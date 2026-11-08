namespace ProjectAegis.Sim.Engage;

/// <summary>Sim-side damage outcome row for deterministic batch ordering (ADR-009).</summary>
public readonly record struct EngagementDamageOutcome(ulong EngagementId, ulong SequenceId, string OutcomeCode);