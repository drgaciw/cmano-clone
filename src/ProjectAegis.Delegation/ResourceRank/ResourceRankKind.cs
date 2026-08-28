namespace ProjectAegis.Delegation.ResourceRank;

/// <summary>
/// DRG-217: advisory classification for resource-ranking output.
/// Only <see cref="AdvisoryRanking"/> is emitted — never authorization or fire orders.
/// </summary>
public enum ResourceRankKind
{
    /// <summary>Headless ranked shooter/weapon list for UI review — not weapons release.</summary>
    AdvisoryRanking = 0,
}
