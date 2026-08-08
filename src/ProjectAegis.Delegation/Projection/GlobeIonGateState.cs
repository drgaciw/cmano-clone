namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Honest ion / package presence gate for globe tile streaming (Wave 4 Lane C).
/// Never stores token values — only presence flags and a bindable status line.
/// </summary>
public sealed record GlobeIonGateState(
    bool HasTokenConfigured,
    bool PackageAvailable,
    bool StreamingActive,
    string StatusLine)
{
    /// <summary>Inactive defaults (no token, package unknown/absent).</summary>
    public static GlobeIonGateState InactiveNoToken { get; } =
        GlobeIonGateProjection.Project(hasToken: false, packageAvailable: false);
}
