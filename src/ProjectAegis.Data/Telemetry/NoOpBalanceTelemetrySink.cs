namespace ProjectAegis.Data.Telemetry;

/// <summary>P0 no-op hook when <c>enableBalanceDrift</c> is false (DBI-5 P0 note).</summary>
public sealed class NoOpBalanceTelemetrySink : IBalanceTelemetrySink
{
    public static readonly NoOpBalanceTelemetrySink Instance = new();

    private NoOpBalanceTelemetrySink()
    {
    }

    public void RecordOutcome(string entityId, BalanceEntityKind entityKind, bool won)
    {
    }

    public void RegisterExpectedWinRate(string entityId, double expectedWinRate)
    {
    }

    public BalanceDriftReport EvaluateDrift() => BalanceDriftReport.EmptyDisabled;

    public string ComputeStateHash() => BalanceTelemetryGoldenHashes.EmptyState;
}