namespace ProjectAegis.Delegation.PlatformDegrade;

/// <summary>
/// DRG-227: headless own-unit platform degrade / damage-control posture for Combat UX Slice C.
/// Advisory only — never retasks, detaches, rejoins, enqueues orders, or writes catalog.
/// </summary>
public sealed record PlatformDegradeSnapshot(
    ulong SimTick,
    IReadOnlyList<PlatformDegradeUnitRow> Units,
    PlatformDegradeKind Kind,
    bool IsWeaponsReleaseAuthorization,
    bool IsFireOrder,
    bool IsAutomaticEngagement,
    string StatusLine)
{
    /// <summary>Empty sentinel when no unit facts are supplied.</summary>
    public static PlatformDegradeSnapshot Empty { get; } = new(
        SimTick: 0,
        Units: Array.Empty<PlatformDegradeUnitRow>(),
        Kind: PlatformDegradeKind.AdvisoryDamageControl,
        IsWeaponsReleaseAuthorization: false,
        IsFireOrder: false,
        IsAutomaticEngagement: false,
        StatusLine: "PDG: —");
}
