namespace ProjectAegis.Delegation.Projection;

/// <summary>One row in the facility capacity picture (derived from order log, not authoritative sim state).</summary>
public sealed record FacilityPictureEntry(
    string FacilityId,
    string TargetId,
    string CapacityState,
    ulong LastSimTick,
    double LastSimTime);