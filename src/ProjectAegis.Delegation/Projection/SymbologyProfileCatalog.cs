namespace ProjectAegis.Delegation.Projection;

/// <summary>Selectable symbology profiles and their display names (S126 SYM-02 scaffold).</summary>
public static class SymbologyProfileCatalog
{
    /// <summary>Reason surfaced when the civilian profile is requested before its scope decision.</summary>
    public const string CivilianPendingReason =
        "Civilian-friendly profile pending scope decision (DRG-232 SYM-CIV-01); not selectable.";

    /// <summary>Profiles a player may select, in display order.</summary>
    public static IReadOnlyList<SymbologyProfile> Selectable { get; } =
    [
        SymbologyProfile.Legacy,
        SymbologyProfile.MilitaryTactical,
    ];

    /// <summary>True when the profile can be selected and rendered.</summary>
    public static bool IsSelectable(SymbologyProfile profile) =>
        profile is SymbologyProfile.Legacy or SymbologyProfile.MilitaryTactical;

    /// <summary>Player-facing profile name.</summary>
    public static string DisplayName(SymbologyProfile profile) => profile switch
    {
        SymbologyProfile.Legacy => "Legacy APP-6 placeholder",
        SymbologyProfile.MilitaryTactical => "Military Tactical (subset)",
        SymbologyProfile.CivilianFriendly => "Civilian-friendly (pending)",
        _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, null),
    };
}
