namespace ProjectAegis.Delegation.Projection;

/// <summary>One row in the sensor C2 contact list (derived from order log, not authoritative sim state).</summary>
public sealed record ContactPictureEntry(
    string ContactId,
    string TargetId,
    string ObserverId,
    string LifecycleState,
    ulong LastSimTick,
    double LastSimTime);