namespace ProjectAegis.Sim.Tests.Benchmark;

using ProjectAegis.Sim.Benchmark;
using Xunit;

/// <summary>
/// Deterministic coverage for the INF-5.1 benchmark: derived metrics, per-entity budget math, and
/// artifact formatting. Throughput numbers themselves are machine-dependent and not asserted here;
/// the live <c>SimBenchmark.MeasureCoreTick</c> run is exercised by a smoke assertion only.
/// </summary>
public sealed class SimBenchmarkTests
{
    [Fact]
    public void Result_derives_throughput_simseconds_and_realtime_multiple()
    {
        // 60 ticks (= 1 sim second) in 1 ms wall -> 1000x realtime.
        var r = new BenchmarkResult("core-tick", EntityCount: 0, Ticks: 60, Repetitions: 1, WallClockMs: 1.0);

        Assert.Equal(60, r.TotalTicks);
        Assert.Equal(1.0, r.SimSeconds, 6);
        Assert.Equal(60_000.0, r.TicksPerSecond, 3);
        Assert.Equal(1000.0, r.EffectiveRealtimeMultiple, 3);
        Assert.Equal(1_000_000.0 / 60.0, r.NanosPerTick, 3);
    }

    [Fact]
    public void TickWallBudget_matches_the_nfr_arithmetic()
    {
        // 1000x realtime -> one sim second (60 ticks) in 1 ms -> 16666.67 ns per tick.
        Assert.Equal(16_666.667, EntityScaleBudget.TickWallBudgetNanos(1000), 2);
        // 256x floor -> 65104.17 ns per tick.
        Assert.Equal(65_104.167, EntityScaleBudget.TickWallBudgetNanos(256), 2);
    }

    [Fact]
    public void PerEntityNanos_is_budget_minus_fixed_over_entities()
    {
        // With ~0 fixed cost, 1000x @ 25k entities leaves the (famously tight) sub-nanosecond budget.
        var perEntity = EntityScaleBudget.PerEntityNanos(measuredFixedNanosPerTick: 0, entityCount: 25_000, targetRealtimeMultiple: 1000);
        Assert.Equal(16_666.667 / 25_000.0, perEntity, 4);
    }

    [Fact]
    public void PerEntityNanos_goes_negative_when_fixed_cost_alone_blows_the_budget()
    {
        // Fixed per-tick cost already exceeds the wall budget -> no room for any per-entity work.
        var perEntity = EntityScaleBudget.PerEntityNanos(measuredFixedNanosPerTick: 20_000, entityCount: 25_000, targetRealtimeMultiple: 1000);
        Assert.True(perEntity < 0);
    }

    [Fact]
    public void Csv_has_header_and_one_row_per_result()
    {
        var rows = new[]
        {
            new BenchmarkResult("core-tick", 0, 60, 1, 1.0),
            new BenchmarkResult("core-tick", 0, 120, 2, 4.0),
        };

        var csv = BenchmarkReport.ToCsv(rows);
        var lines = csv.TrimEnd('\n').Split('\n');

        Assert.Equal(BenchmarkReport.CsvHeader, lines[0]);
        Assert.Equal(3, lines.Length); // header + 2 rows
        Assert.StartsWith("core-tick,0,60,1,60,", lines[1]);
    }

    [Fact]
    public void Json_is_bracketed_array_with_expected_keys()
    {
        var json = BenchmarkReport.ToJson(new[] { new BenchmarkResult("core-tick", 0, 60, 1, 1.0) });

        Assert.StartsWith("[", json.TrimStart());
        Assert.Contains("\"mode\":\"core-tick\"", json);
        Assert.Contains("\"entity_count\":0", json);
        Assert.Contains("\"effective_realtime_multiple\":1000", json);
    }

    [Fact]
    public void MeasureCoreTick_runs_and_reports_positive_throughput()
    {
        // Tiny run — asserts the harness executes and produces a sane row, not a specific speed.
        var result = SimBenchmark.MeasureCoreTick(seed: 42, ticks: 10_000, repetitions: 2, warmupTicks: 1_000);

        Assert.Equal("core-tick", result.Mode);
        Assert.Equal(0, result.EntityCount);
        Assert.Equal(20_000, result.TotalTicks);
        Assert.True(result.WallClockMs >= 0);
        Assert.True(result.TicksPerSecond > 0);
    }
}
