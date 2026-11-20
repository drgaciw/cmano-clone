using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Telemetry;
using ProjectAegis.Data.WriteGate;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Telemetry;

public sealed class BalanceTelemetryAccumulatorTests
{
    private static BalanceDriftOptions FastOptions => new()
    {
        MinimumSampleRuns = 100,
        WinRateDriftThreshold = 0.08,
        DefaultExpectedWinRate = 0.5,
    };

    [Test]
    public void Disabled_sink_does_not_accumulate_or_flag()
    {
        var sink = BalanceTelemetrySinkFactory.Create();
        for (var i = 0; i < 600; i++)
        {
            sink.RecordOutcome("overpowered", BalanceEntityKind.Platform, won: true);
        }

        var report = sink.EvaluateDrift();
        Assert.That(report.DriftDetectionEnabled, Is.False);
        Assert.That(report.Findings, Is.Empty);
        Assert.That(sink.ComputeStateHash(), Is.EqualTo(BalanceTelemetryGoldenHashes.EmptyState));
    }

    [Test]
    public void Insufficient_samples_do_not_emit_flags()
    {
        var sink = new BalanceTelemetryAccumulator(FastOptions);
        for (var i = 0; i < 99; i++)
        {
            sink.RecordOutcome("alpha", BalanceEntityKind.Weapon, won: true);
        }

        Assert.That(sink.EvaluateDrift().Findings, Is.Empty);
    }

    [Test]
    public void Drift_flag_fires_when_delta_exceeds_eight_percent()
    {
        var sink = new BalanceTelemetryAccumulator(FastOptions);
        for (var i = 0; i < 100; i++)
        {
            sink.RecordOutcome("alpha", BalanceEntityKind.Platform, won: i < 70);
        }

        var finding = ProjectAegis.Data.Tests.AssertEx.Single(sink.EvaluateDrift().Findings);
        Assert.That(finding.Code, Is.EqualTo("BALANCE_WIN_RATE_DRIFT"));
        Assert.That(finding.ActualWinRate, Is.EqualTo(0.7).Within(0.00005));
        Assert.That(finding.DriftDelta, Is.EqualTo(0.2).Within(0.00005));
    }

    [Test]
    public void Drift_flag_does_not_fire_at_exactly_eight_percent_band()
    {
        var sink = new BalanceTelemetryAccumulator(FastOptions);
        for (var i = 0; i < 100; i++)
        {
            sink.RecordOutcome("beta", BalanceEntityKind.Platform, won: i < 58);
        }

        Assert.That(sink.EvaluateDrift().Findings, Is.Empty);
    }

    [Test]
    public void Negative_drift_below_baseline_is_flagged()
    {
        var sink = new BalanceTelemetryAccumulator(FastOptions);
        for (var i = 0; i < 100; i++)
        {
            sink.RecordOutcome("gamma", BalanceEntityKind.Weapon, won: i < 30);
        }

        var finding = ProjectAegis.Data.Tests.AssertEx.Single(sink.EvaluateDrift().Findings);
        Assert.That(finding.DriftDelta < 0, Is.True);
        Assert.That(finding.DriftDelta, Is.EqualTo(-0.2).Within(0.00005));
    }

    [Test]
    public void Per_entity_expected_win_rate_is_honored()
    {
        var sink = new BalanceTelemetryAccumulator(FastOptions);
        sink.RegisterExpectedWinRate("delta", 0.7);
        for (var i = 0; i < 100; i++)
        {
            sink.RecordOutcome("delta", BalanceEntityKind.Platform, won: i < 79);
        }

        var finding = ProjectAegis.Data.Tests.AssertEx.Single(sink.EvaluateDrift().Findings);
        Assert.That(finding.ExpectedWinRate, Is.EqualTo(0.7).Within(0.00005));
        Assert.That(finding.DriftDelta, Is.EqualTo(0.09).Within(0.00005));
    }

    [Test]
    public void Telemetry_does_not_mutate_write_gate_state()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-balance-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            using var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(42));
            // Dispose reader before File.Delete — SQLite holds the DB open on Windows otherwise.
            CatalogSensorBinding[] bindings;
            using (var reader = new SqliteCatalogReader(dbPath, "balance-telemetry-test"))
            {
                bindings = reader.GetSortedSensorBindings().ToArray();
            }

            var batchId = gate.ProposeSensorBatch(bindings.Take(1).ToArray(), "test", "balance-telemetry");

            var sink = new BalanceTelemetryAccumulator(FastOptions);
            for (var i = 0; i < 120; i++)
            {
                sink.RecordOutcome("u1", BalanceEntityKind.Platform, won: i % 2 == 0);
            }

            Assert.That(                typeof(BalanceTelemetryAccumulator).GetInterfaces(), Does.Not.Contain(                typeof(IWriteGate)));
            Assert.That(() => sink.EvaluateDrift(), Throws.Nothing);
            Assert.That(                gate.ProposeSensorBatch(bindings.Take(1).ToArray(), "test", "balance-telemetry"), Is.EqualTo(                batchId));
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            // Best-effort cleanup; Windows may briefly hold locks after dispose.
            for (var attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    if (File.Exists(dbPath))
                    {
                        File.Delete(dbPath);
                    }

                    break;
                }
                catch (IOException)
                {
                    if (attempt == 4)
                    {
                        // Swallow on final attempt — best-effort cleanup must not mask try-block failures.
                        break;
                    }

                    Thread.Sleep(50);
                }
            }
        }
    }
}