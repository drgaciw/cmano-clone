namespace ProjectAegis.Data.Telemetry;

/// <summary>Feature flags for post-P0 balance drift detection (DBI-5).</summary>
public sealed class BalanceDriftFeatureFlags
{
    /// <summary>When false (default), telemetry sink is a no-op and no drift flags are emitted.</summary>
    public bool EnableBalanceDrift { get; init; }
}