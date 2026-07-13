namespace ProjectAegis.Sim.Scenario;

using ProjectAegis.Data.Telemetry;

/// <summary>Scenario-level balance drift telemetry flags (DBI-5 advisory; default off).</summary>
public sealed class ScenarioBalanceTelemetrySettings
{
    public ScenarioBalanceTelemetrySettings(
        bool enableBalanceDrift = false,
        BalanceDriftOptions? options = null,
        IReadOnlyList<ScenarioBalanceTrial>? balanceTrials = null)
    {
        EnableBalanceDrift = enableBalanceDrift;
        Options = options;
        BalanceTrials = balanceTrials ?? Array.Empty<ScenarioBalanceTrial>();
    }

    /// <summary>When false (default), sim consumer is a no-op and no drift advisories are emitted.</summary>
    public bool EnableBalanceDrift { get; }

    public BalanceDriftOptions? Options { get; }

    public IReadOnlyList<ScenarioBalanceTrial> BalanceTrials { get; }

    public static ScenarioBalanceTelemetrySettings Disabled { get; } = new();
}