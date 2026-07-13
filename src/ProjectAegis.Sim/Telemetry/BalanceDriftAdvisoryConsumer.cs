namespace ProjectAegis.Sim.Telemetry;

using ProjectAegis.Data.Telemetry;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Scenario;

/// <summary>
/// Advisory-only sim consumer for S22-06 balance drift telemetry. Records engagement outcomes into
/// <see cref="IBalanceTelemetrySink"/> when <c>enableBalanceDrift</c> is true; never mutates world state.
/// </summary>
public sealed class BalanceDriftAdvisoryConsumer
{
    private readonly IBalanceTelemetrySink _sink;
    private BalanceDriftReport _lastReport = BalanceDriftReport.EmptyDisabled;

    public BalanceDriftAdvisoryConsumer(ScenarioBalanceTelemetrySettings? settings = null)
    {
        settings ??= ScenarioBalanceTelemetrySettings.Disabled;
        Enabled = settings.EnableBalanceDrift;
        _sink = BalanceTelemetrySinkFactory.Create(
            new BalanceDriftFeatureFlags { EnableBalanceDrift = settings.EnableBalanceDrift },
            settings.Options);
        foreach (var trial in settings.BalanceTrials)
        {
            if (trial.ExpectedWinRate is { } expected)
            {
                _sink.RegisterExpectedWinRate(trial.EntityId, expected);
            }
        }
    }

    public bool Enabled { get; }

    public BalanceDriftReport LastReport => _lastReport;

    public IBalanceTelemetrySink Sink => _sink;

    public void RecordEngagementOutcome(string shooterUnitId, string? outcomeCode)
    {
        if (!Enabled || string.IsNullOrWhiteSpace(shooterUnitId))
        {
            return;
        }

        var won = outcomeCode is EngagementOutcomeCodes.Kill or EngagementOutcomeCodes.Hit;
        RecordOutcome(shooterUnitId, BalanceEntityKind.Platform, won);
    }

    public void RecordOutcome(string entityId, BalanceEntityKind entityKind, bool won)
    {
        if (!Enabled)
        {
            return;
        }

        _sink.RecordOutcome(entityId, entityKind, won);
        _lastReport = _sink.EvaluateDrift();
    }

    public BalanceDriftReport EvaluateDrift() => _sink.EvaluateDrift();
}