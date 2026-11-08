namespace ProjectAegis.Data.Telemetry;

/// <summary>Creates the appropriate telemetry sink for the current feature flags.</summary>
public static class BalanceTelemetrySinkFactory
{
    public static IBalanceTelemetrySink Create(
        BalanceDriftFeatureFlags? featureFlags = null,
        BalanceDriftOptions? options = null)
    {
        var flags = featureFlags ?? new BalanceDriftFeatureFlags();
        return flags.EnableBalanceDrift
            ? new BalanceTelemetryAccumulator(options)
            : NoOpBalanceTelemetrySink.Instance;
    }
}