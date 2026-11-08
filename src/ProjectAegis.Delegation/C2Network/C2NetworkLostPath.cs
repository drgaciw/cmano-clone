namespace ProjectAegis.Delegation.C2Network;

/// <summary>Modeled path that no longer carries live datalink updates.</summary>
public sealed record C2NetworkLostPath(
    string FromUnitId,
    string ToUnitId,
    string LinkType,
    ulong LastKnownSimTick);
