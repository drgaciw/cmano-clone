using ProjectAegis.Data.Telemetry;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Telemetry;
using Xunit;

namespace ProjectAegis.Sim.Tests.Telemetry;

public sealed class BalanceDriftAdvisoryConsumerTests
{
    private static ScenarioBalanceTelemetrySettings FastEnabledSettings => new(
        enableBalanceDrift: true,
        options: new BalanceDriftOptions
        {
            MinimumSampleRuns = 100,
            WinRateDriftThreshold = 0.08,
            DefaultExpectedWinRate = 0.5,
        });

    [Fact]
    public void Balance_disabled_consumer_does_not_accumulate_or_emit_advisories()
    {
        var consumer = new BalanceDriftAdvisoryConsumer();
        for (var i = 0; i < 200; i++)
        {
            consumer.RecordOutcome("u1", BalanceEntityKind.Platform, won: true);
        }

        Assert.False(consumer.Enabled);
        Assert.False(consumer.LastReport.DriftDetectionEnabled);
        Assert.Empty(consumer.LastReport.Findings);
        Assert.Equal(BalanceTelemetryGoldenHashes.EmptyState, consumer.Sink.ComputeStateHash());
    }

    [Fact]
    public void Balance_enabled_consumer_emits_drift_advisory_beyond_eight_percent()
    {
        var consumer = new BalanceDriftAdvisoryConsumer(FastEnabledSettings);
        for (var i = 0; i < 100; i++)
        {
            consumer.RecordOutcome("u1", BalanceEntityKind.Platform, won: i < 70);
        }

        var finding = Assert.Single(consumer.LastReport.Findings);
        Assert.Equal("BALANCE_WIN_RATE_DRIFT", finding.Code);
        Assert.Equal(0.7, finding.ActualWinRate, precision: 5);
        Assert.Equal(0.2, finding.DriftDelta, precision: 5);
    }

    [Fact]
    public void Balance_enabled_consumer_does_not_flag_at_exactly_eight_percent_band()
    {
        var consumer = new BalanceDriftAdvisoryConsumer(FastEnabledSettings);
        for (var i = 0; i < 100; i++)
        {
            consumer.RecordOutcome("u1", BalanceEntityKind.Platform, won: i < 58);
        }

        Assert.Empty(consumer.LastReport.Findings);
    }

    [Fact]
    public void Balance_zero_trials_do_not_emit_flags()
    {
        var consumer = new BalanceDriftAdvisoryConsumer(FastEnabledSettings);
        Assert.Empty(consumer.EvaluateDrift().Findings);
    }

    [Fact]
    public void Balance_engagement_outcome_records_platform_win_from_kill_or_hit()
    {
        var consumer = new BalanceDriftAdvisoryConsumer(FastEnabledSettings);
        consumer.RecordEngagementOutcome("u1", EngagementOutcomeCodes.Kill);
        consumer.RecordEngagementOutcome("u1", EngagementOutcomeCodes.Hit);
        consumer.RecordEngagementOutcome("u1", EngagementOutcomeCodes.Miss);

        Assert.True(consumer.LastReport.DriftDetectionEnabled);
        Assert.Empty(consumer.LastReport.Findings);
        Assert.NotEqual(BalanceTelemetryGoldenHashes.EmptyState, consumer.Sink.ComputeStateHash());
    }
}