namespace ProjectAegis.Data.Telemetry;

/// <summary>
/// S29-10: catalog import/approve pipeline feature surface for balance drift advisories.
/// Default <c>enableBalanceDrift=false</c> preserves pre-story behavior (DBI-5 P0 note).
/// </summary>
public sealed class CatalogBalanceDriftPipelineSettings
{
    public static readonly CatalogBalanceDriftPipelineSettings Disabled = new();

    public CatalogBalanceDriftPipelineSettings(
        bool enableBalanceDrift = false,
        BalanceDriftOptions? options = null,
        IBalanceTelemetrySink? telemetrySink = null)
    {
        EnableBalanceDrift = enableBalanceDrift;
        Options = options;
        TelemetrySink = telemetrySink;
    }

    public bool EnableBalanceDrift { get; }

    public BalanceDriftOptions? Options { get; }

    /// <summary>Optional injected sink for tests; production path uses <see cref="BalanceTelemetrySinkFactory"/>.</summary>
    public IBalanceTelemetrySink? TelemetrySink { get; }
}