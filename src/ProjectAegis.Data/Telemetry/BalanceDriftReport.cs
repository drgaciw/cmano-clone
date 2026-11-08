namespace ProjectAegis.Data.Telemetry;

/// <summary>Advisory drift evaluation snapshot (no write-gate side effects).</summary>
public sealed record BalanceDriftReport(
    bool DriftDetectionEnabled,
    IReadOnlyList<BalanceDriftFinding> Findings,
    string StateHash)
{
    public static readonly BalanceDriftReport EmptyDisabled = new(
        DriftDetectionEnabled: false,
        Findings: Array.Empty<BalanceDriftFinding>(),
        StateHash: BalanceTelemetryGoldenHashes.EmptyState);
}