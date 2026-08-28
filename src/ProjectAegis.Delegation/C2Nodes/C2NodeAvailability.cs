namespace ProjectAegis.Delegation.C2Nodes;

/// <summary>Element capability state for package context. Distinct from link paint (DRG-214).</summary>
public enum C2NodeAvailability
{
    Available = 0,
    Unavailable = 1,
    LastKnown = 2,
}
