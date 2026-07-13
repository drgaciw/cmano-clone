using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Telemetry;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Tests.Telemetry;

public sealed class BalanceTelemetryAccumulatorTests
{
    private static BalanceDriftOptions FastOptions => new()
    {
        MinimumSampleRuns = 100,
        WinRateDriftThreshold = 0.08,
        DefaultExpectedWinRate = 0.5,
    };

    [Fact]
    public void Disabled_sink_does_not_accumulate_or_flag()
    {
        var sink = BalanceTelemetrySinkFactory.Create();
        for (var i = 0; i < 600; i++)
        {
            sink.RecordOutcome("overpowered", BalanceEntityKind.Platform, won: true);
        }

        var report = sink.EvaluateDrift();
        Assert.False(report.DriftDetectionEnabled);
        Assert.Empty(report.Findings);
        Assert.Equal(BalanceTelemetryGoldenHashes.EmptyState, sink.ComputeStateHash());
    }

    [Fact]
    public void Insufficient_samples_do_not_emit_flags()
    {
        var sink = new BalanceTelemetryAccumulator(FastOptions);
        for (var i = 0; i < 99; i++)
        {
            sink.RecordOutcome("alpha", BalanceEntityKind.Weapon, won: true);
        }

        Assert.Empty(sink.EvaluateDrift().Findings);
    }

    [Fact]
    public void Drift_flag_fires_when_delta_exceeds_eight_percent()
    {
        var sink = new BalanceTelemetryAccumulator(FastOptions);
        for (var i = 0; i < 100; i++)
        {
            sink.RecordOutcome("alpha", BalanceEntityKind.Platform, won: i < 70);
        }

        var finding = Assert.Single(sink.EvaluateDrift().Findings);
        Assert.Equal("BALANCE_WIN_RATE_DRIFT", finding.Code);
        Assert.Equal(0.7, finding.ActualWinRate, precision: 5);
        Assert.Equal(0.2, finding.DriftDelta, precision: 5);
    }

    [Fact]
    public void Drift_flag_does_not_fire_at_exactly_eight_percent_band()
    {
        var sink = new BalanceTelemetryAccumulator(FastOptions);
        for (var i = 0; i < 100; i++)
        {
            sink.RecordOutcome("beta", BalanceEntityKind.Platform, won: i < 58);
        }

        Assert.Empty(sink.EvaluateDrift().Findings);
    }

    [Fact]
    public void Negative_drift_below_baseline_is_flagged()
    {
        var sink = new BalanceTelemetryAccumulator(FastOptions);
        for (var i = 0; i < 100; i++)
        {
            sink.RecordOutcome("gamma", BalanceEntityKind.Weapon, won: i < 30);
        }

        var finding = Assert.Single(sink.EvaluateDrift().Findings);
        Assert.True(finding.DriftDelta < 0);
        Assert.Equal(-0.2, finding.DriftDelta, precision: 5);
    }

    [Fact]
    public void Per_entity_expected_win_rate_is_honored()
    {
        var sink = new BalanceTelemetryAccumulator(FastOptions);
        sink.RegisterExpectedWinRate("delta", 0.7);
        for (var i = 0; i < 100; i++)
        {
            sink.RecordOutcome("delta", BalanceEntityKind.Platform, won: i < 79);
        }

        var finding = Assert.Single(sink.EvaluateDrift().Findings);
        Assert.Equal(0.7, finding.ExpectedWinRate, precision: 5);
        Assert.Equal(0.09, finding.DriftDelta, precision: 5);
    }

    [Fact]
    public void Telemetry_does_not_mutate_write_gate_state()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-balance-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            using var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(42));
            var bindings = new SqliteCatalogReader(dbPath, "balance-telemetry-test").GetSortedSensorBindings();
            var batchId = gate.ProposeSensorBatch(bindings.Take(1).ToArray(), "test", "balance-telemetry");

            var sink = new BalanceTelemetryAccumulator(FastOptions);
            for (var i = 0; i < 120; i++)
            {
                sink.RecordOutcome("u1", BalanceEntityKind.Platform, won: i % 2 == 0);
            }

            Assert.DoesNotContain(
                typeof(IWriteGate),
                typeof(BalanceTelemetryAccumulator).GetInterfaces());
            Assert.Null(Record.Exception(() => sink.EvaluateDrift()));
            Assert.Equal(
                batchId,
                gate.ProposeSensorBatch(bindings.Take(1).ToArray(), "test", "balance-telemetry"));
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }
}