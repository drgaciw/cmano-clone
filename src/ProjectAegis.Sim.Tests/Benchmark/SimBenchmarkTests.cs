namespace ProjectAegis.Sim.Tests.Benchmark;

using ProjectAegis.Sim.Benchmark;
using NUnit.Framework;

/// <summary>
/// Deterministic coverage for the INF-5.1 benchmark: derived metrics, per-entity budget math, and
/// artifact formatting. Throughput numbers themselves are machine-dependent and not asserted here;
/// the live <c>SimBenchmark.MeasureCoreTick</c> run is exercised by a smoke assertion only.
/// </summary>
public sealed class SimBenchmarkTests
{
    [Test]
    public void Result_derives_throughput_simseconds_and_realtime_multiple()
    {
        // 60 ticks (= 1 sim second) in 1 ms wall -> 1000x realtime.
        var r = new BenchmarkResult("core-tick", EntityCount: 0, Ticks: 60, Repetitions: 1, WallClockMs: 1.0);

        Assert.That(r.TotalTicks, Is.EqualTo(60));
        Assert.That(
            Math.Round(r.SimSeconds, 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(1.0, 6, MidpointRounding.AwayFromZero)));
        Assert.That(
            Math.Round(r.TicksPerSecond, 3, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(60_000.0, 3, MidpointRounding.AwayFromZero)));
        Assert.That(
            Math.Round(r.EffectiveRealtimeMultiple, 3, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(1000.0, 3, MidpointRounding.AwayFromZero)));
        Assert.That(
            Math.Round(r.NanosPerTick, 3, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(1_000_000.0 / 60.0, 3, MidpointRounding.AwayFromZero)));
    }

    [Test]
    public void TickWallBudget_matches_the_nfr_arithmetic()
    {
        // 1000x realtime -> one sim second (60 ticks) in 1 ms -> 16666.67 ns per tick.
        Assert.That(
            Math.Round(EntityScaleBudget.TickWallBudgetNanos(1000), 2, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(16_666.667, 2, MidpointRounding.AwayFromZero)));
        // 256x floor -> 65104.17 ns per tick.
        Assert.That(
            Math.Round(EntityScaleBudget.TickWallBudgetNanos(256), 2, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(65_104.167, 2, MidpointRounding.AwayFromZero)));
    }

    [Test]
    public void PerEntityNanos_is_budget_minus_fixed_over_entities()
    {
        // With ~0 fixed cost, 1000x @ 25k entities leaves the (famously tight) sub-nanosecond budget.
        var perEntity = EntityScaleBudget.PerEntityNanos(measuredFixedNanosPerTick: 0, entityCount: 25_000, targetRealtimeMultiple: 1000);
        Assert.That(
            Math.Round(perEntity, 4, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(16_666.667 / 25_000.0, 4, MidpointRounding.AwayFromZero)));
    }

    [Test]
    public void PerEntityNanos_goes_negative_when_fixed_cost_alone_blows_the_budget()
    {
        // Fixed per-tick cost already exceeds the wall budget -> no room for any per-entity work.
        var perEntity = EntityScaleBudget.PerEntityNanos(measuredFixedNanosPerTick: 20_000, entityCount: 25_000, targetRealtimeMultiple: 1000);
        Assert.That(perEntity < 0, Is.True);
    }

    [Test]
    public void Csv_has_header_and_one_row_per_result()
    {
        var rows = new[]
        {
            new BenchmarkResult("core-tick", 0, 60, 1, 1.0),
            new BenchmarkResult("core-tick", 0, 120, 2, 4.0),
        };

        var csv = BenchmarkReport.ToCsv(rows);
        var lines = csv.TrimEnd('\n').Split('\n');

        Assert.That(lines[0], Is.EqualTo(BenchmarkReport.CsvHeader));
        Assert.That(lines.Length, Is.EqualTo(3)); // header + 2 rows
        Assert.That(lines[1], Does.StartWith("core-tick,0,60,1,60,"));
    }

    [Test]
    public void Json_is_bracketed_array_with_expected_keys()
    {
        var json = BenchmarkReport.ToJson(new[] { new BenchmarkResult("core-tick", 0, 60, 1, 1.0) });

        Assert.That(json.TrimStart(), Does.StartWith("["));
        Assert.That(json, Does.Contain("\"mode\":\"core-tick\""));
        Assert.That(json, Does.Contain("\"entity_count\":0"));
        Assert.That(json, Does.Contain("\"effective_realtime_multiple\":1000"));
    }

    [Test]
    public void MeasureCoreTick_runs_and_reports_positive_throughput()
    {
        // Tiny run — asserts the harness executes and produces a sane row, not a specific speed.
        var result = SimBenchmark.MeasureCoreTick(seed: 42, ticks: 10_000, repetitions: 2, warmupTicks: 1_000);

        Assert.That(result.Mode, Is.EqualTo("core-tick"));
        Assert.That(result.EntityCount, Is.EqualTo(0));
        Assert.That(result.TotalTicks, Is.EqualTo(20_000));
        Assert.That(result.WallClockMs >= 0, Is.True);
        Assert.That(result.TicksPerSecond > 0, Is.True);
    }
}
