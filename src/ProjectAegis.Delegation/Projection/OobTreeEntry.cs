namespace ProjectAegis.Delegation.Projection;

/// <summary>Single unit row for the order of battle.</summary>
public sealed record OobTreeEntry(string UnitId, bool IsAlive)
{
    /// <summary>Configured player command ownership, or null when no mode is configured.</summary>
    public bool? IsPlayerCommandTarget { get; init; }
}