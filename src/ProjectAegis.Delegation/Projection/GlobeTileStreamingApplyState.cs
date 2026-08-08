namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Headless apply path for globe tile-streaming UI status (Wave 4 Lane C).
/// Toolkit hosts bind <see cref="GlobeTileStreamingPresentation.StatusLine"/> without re-formatting.
/// </summary>
public static class GlobeTileStreamingApplyState
{
    /// <summary>
    /// Apply gate + optional config into presentation fields.
    /// Status line is always the gate status (ACTIVE / NO_ION_TOKEN / PACKAGE_MISSING).
    /// </summary>
    public static GlobeTileStreamingPresentation Apply(
        GlobeIonGateState? gate,
        GlobeTileStreamingConfig? config = null)
    {
        var resolvedGate = gate ?? GlobeIonGateState.InactiveNoToken;
        var resolvedConfig = config ?? GlobeTileStreamingConfig.Default;

        return new GlobeTileStreamingPresentation(
            StatusLine: resolvedGate.StatusLine,
            StreamingActive: resolvedGate.StreamingActive,
            HasTokenConfigured: resolvedGate.HasTokenConfigured,
            PackageAvailable: resolvedGate.PackageAvailable,
            IonAssetId: resolvedConfig.IonAssetId,
            MaximumScreenSpaceError: resolvedConfig.MaximumScreenSpaceError,
            PreloadAncestors: resolvedConfig.PreloadAncestors,
            PreloadSiblings: resolvedConfig.PreloadSiblings,
            EnableFrustumCulling: resolvedConfig.EnableFrustumCulling,
            BalticBbox: resolvedConfig.BalticBbox,
            ConfigSummary: FormatConfigSummary(resolvedConfig));
    }

    /// <summary>
    /// Project gate from presence flags then apply — product path for headless / Toolkit smoke.
    /// </summary>
    public static GlobeTileStreamingPresentation ProjectAndApply(
        bool hasToken,
        bool packageAvailable,
        GlobeTileStreamingConfig? config = null)
    {
        var gate = GlobeIonGateProjection.Project(hasToken, packageAvailable, config);
        return Apply(gate, config);
    }

    private static string FormatConfigSummary(GlobeTileStreamingConfig config)
    {
        return $"asset={config.IonAssetId} sse={config.MaximumScreenSpaceError:0.##}";
    }
}

/// <summary>Applied tile-streaming presentation fields for Toolkit / Cesium hosts.</summary>
public sealed record GlobeTileStreamingPresentation(
    string StatusLine,
    bool StreamingActive,
    bool HasTokenConfigured,
    bool PackageAvailable,
    int IonAssetId,
    double MaximumScreenSpaceError,
    bool PreloadAncestors,
    bool PreloadSiblings,
    bool EnableFrustumCulling,
    BalticBbox? BalticBbox,
    string ConfigSummary)
{
    public static GlobeTileStreamingPresentation Empty { get; } =
        GlobeTileStreamingApplyState.Apply(GlobeIonGateState.InactiveNoToken, GlobeTileStreamingConfig.Default);
}
