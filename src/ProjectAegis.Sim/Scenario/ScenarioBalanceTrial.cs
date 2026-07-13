namespace ProjectAegis.Sim.Scenario;

using ProjectAegis.Data.Telemetry;

/// <summary>Optional per-entity expected win-rate override for balance drift fixtures.</summary>
public sealed record ScenarioBalanceTrial(
    string EntityId,
    BalanceEntityKind EntityKind,
    double? ExpectedWinRate = null);