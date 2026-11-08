namespace ProjectAegis.Data.Telemetry;

/// <summary>
/// Hook for agent-vs-agent sim outcomes (DBI-5). Advisory only — never mutates catalog or bypasses
/// <see cref="WriteGate.IWriteGate"/>.
/// </summary>
public interface IBalanceTelemetrySink
{
    void RecordOutcome(string entityId, BalanceEntityKind entityKind, bool won);

    void RegisterExpectedWinRate(string entityId, double expectedWinRate);

    BalanceDriftReport EvaluateDrift();

    string ComputeStateHash();
}