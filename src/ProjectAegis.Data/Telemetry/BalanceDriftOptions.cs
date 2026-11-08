namespace ProjectAegis.Data.Telemetry;

/// <summary>Tuneable balance drift thresholds (DBI-5.3).</summary>
public sealed class BalanceDriftOptions
{
    /// <summary>Win-rate delta above which an advisory flag is raised (default ±8%).</summary>
    public double WinRateDriftThreshold { get; init; } = 0.08;

    /// <summary>Minimum agent-vs-agent runs before drift evaluation (default 500).</summary>
    public int MinimumSampleRuns { get; init; } = 500;

    /// <summary>Baseline expected win rate when no per-entity override is registered.</summary>
    public double DefaultExpectedWinRate { get; init; } = 0.5;
}