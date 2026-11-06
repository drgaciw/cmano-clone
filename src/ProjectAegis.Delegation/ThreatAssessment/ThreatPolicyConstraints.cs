namespace ProjectAegis.Delegation.ThreatAssessment;

using ProjectAegis.Sim.Policy;

/// <summary>ROE/WRA policy facts constraining a weapon recommendation.</summary>
public sealed record ThreatPolicyConstraints(
    RoeLevel RoeLevel,
    int MaxSalvo,
    bool AutoEngageAuthorized,
    bool ExpendAuthorized,
    bool PolicyAllowsFire,
    string? PolicyAbortCode);
