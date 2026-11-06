namespace ProjectAegis.Delegation.ThreatAssessment;

using ProjectAegis.Sim.Engage;

/// <summary>Range and envelope facts backing a weapon recommendation.</summary>
public sealed record ThreatRangeAssessment(
    double RangeMeters,
    double EnvelopeMinMeters,
    double EnvelopeMaxMeters,
    DlzState DlzState,
    string DlzLabel,
    bool InEnvelope);
