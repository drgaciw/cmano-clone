namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Pure projection for Cesium ion visual gate (Wave 4 Lane C).
/// Token presence only — never accepts or returns secret values.
/// Optional <see cref="GlobeTileStreamingConfig"/> is recorded for apply-state, not required for gate math.
/// </summary>
public static class GlobeIonGateProjection
{
    public const string StatusActive = "GLOBE TILES · ACTIVE";
    public const string StatusNoIonToken = "GLOBE TILES · INACTIVE · NO_ION_TOKEN";
    public const string StatusPackageMissing = "GLOBE TILES · INACTIVE · PACKAGE_MISSING";

    /// <summary>
    /// Project gate state from presence flags.
    /// <paramref name="hasToken"/> is true when a non-empty token is configured (Inspector or env) — value never stored.
    /// <paramref name="packageAvailable"/> is true when com.cesium.unity / CESIUM_FOR_UNITY is active.
    /// <paramref name="config"/> is optional streaming contract (ignored for StreamingActive math; used by apply-state).
    /// </summary>
    public static GlobeIonGateState Project(
        bool hasToken,
        bool packageAvailable,
        GlobeTileStreamingConfig? config = null)
    {
        // config is intentionally unused for gate math — presence-only contract.
        // Callers may pass it so apply-state can bind SSE / asset id alongside status.
        _ = config;

        var streamingActive = hasToken && packageAvailable;
        var status = ResolveStatusLine(hasToken, packageAvailable, streamingActive);
        return new GlobeIonGateState(
            HasTokenConfigured: hasToken,
            PackageAvailable: packageAvailable,
            StreamingActive: streamingActive,
            StatusLine: status);
    }

    private static string ResolveStatusLine(bool hasToken, bool packageAvailable, bool streamingActive)
    {
        if (streamingActive)
        {
            return StatusActive;
        }

        // Prefer package-missing when the runtime cannot host Cesium types at all.
        if (!packageAvailable)
        {
            return StatusPackageMissing;
        }

        // Package present but no ion token → honest inactive (visual gate closed).
        if (!hasToken)
        {
            return StatusNoIonToken;
        }

        // Unreachable when StreamingActive = hasToken && packageAvailable, kept for exhaustiveness.
        return StatusNoIonToken;
    }
}
