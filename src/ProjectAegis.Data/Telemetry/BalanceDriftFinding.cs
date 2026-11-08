namespace ProjectAegis.Data.Telemetry;

/// <summary>Advisory-only balance drift finding; never blocks catalog commit (DBI-5.2).</summary>
public sealed record BalanceDriftFinding(
    string Code,
    string EntityId,
    BalanceEntityKind EntityKind,
    int SampleRuns,
    double ExpectedWinRate,
    double ActualWinRate,
    double DriftDelta,
    string Message);