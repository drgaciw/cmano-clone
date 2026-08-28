namespace ProjectAegis.Delegation.C2Nodes;

/// <summary>
/// One composable C2 element. May share a platform with other elements under a different
/// capability scope (organic sensor vs package track feed).
/// </summary>
public sealed record C2NodeElement(
    string ElementId,
    string PlatformUnitId,
    C2NodeRole Role,
    C2NodeAvailability Availability,
    C2NodeMembership Membership,
    string CapabilityScope,
    bool TaskOrgDetached,
    ulong LastSimTick,
    double LastSimTime,
    ulong? CorrelationSequenceId,
    IReadOnlyList<string> SourceRefs);
