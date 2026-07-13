namespace ProjectAegis.Sim.Benchmark;

using System.Diagnostics;
using System.Globalization;
using System.Text;
using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Time;

/// <summary>
/// One measured benchmark row (INF-5.1: records entity count, ticks executed, wall-clock).
/// </summary>
/// <param name="Mode">Workload label (e.g. <c>core-tick</c>).</param>
/// <param name="EntityCount">
/// Per-tick entity workload actually processed. <b>0 for <c>core-tick</c></b> — the managed sim
/// currently does no per-entity work per tick (see the entity-scale gap report). Recorded honestly
/// rather than as an aspirational figure.
/// </param>
/// <param name="Ticks">Ticks executed per repetition.</param>
/// <param name="Repetitions">Number of measured repetitions.</param>
/// <param name="WallClockMs">Total measured wall-clock across all repetitions, in milliseconds.</param>
public sealed record BenchmarkResult(
    string Mode,
    int EntityCount,
    long Ticks,
    int Repetitions,
    double WallClockMs)
{
    /// <summary>Fixed simulation tick rate (1/60 s dt — see <see cref="SimClock"/>).</summary>
    public const double TicksPerSimSecond = 60.0;

    public long TotalTicks => Ticks * Repetitions;

    public double TicksPerSecond => WallClockMs <= 0 ? 0 : TotalTicks / (WallClockMs / 1000.0);

    /// <summary>Simulated seconds represented by the executed ticks.</summary>
    public double SimSeconds => TotalTicks / TicksPerSimSecond;

    /// <summary>Wall-clock nanoseconds per single tick — the figure the entity budget derives from.</summary>
    public double NanosPerTick => TotalTicks <= 0 ? 0 : WallClockMs * 1_000_000.0 / TotalTicks;

    /// <summary>
    /// Effective speed vs. real time: simulated-seconds / wall-seconds. The 25k NFR asks for
    /// <c>1000×+</c> here <i>with the per-entity workload attached</i> — which does not exist yet.
    /// </summary>
    public double EffectiveRealtimeMultiple =>
        WallClockMs <= 0 ? 0 : SimSeconds / (WallClockMs / 1000.0);
}

/// <summary>Headless sim throughput benchmark (INF-5.1). Pure managed timing — no Unity player loop.</summary>
public static class SimBenchmark
{
    /// <summary>
    /// Measures raw tick-advance throughput of the ADR-004 <see cref="SimTickPipeline"/> with no
    /// pending engagements. This is the sim's per-tick <i>fixed</i> cost (clock advance + world-hash
    /// recompute) and establishes the time-advance ceiling the per-entity budget is carved out of.
    /// </summary>
    public static BenchmarkResult MeasureCoreTick(ulong seed, long ticks, int repetitions, long warmupTicks)
    {
        if (ticks <= 0) throw new ArgumentOutOfRangeException(nameof(ticks));
        if (repetitions <= 0) throw new ArgumentOutOfRangeException(nameof(repetitions));

        // Warm up the JIT and caches; not counted.
        RunCoreTicks(seed, Math.Max(0, warmupTicks));

        var sw = Stopwatch.StartNew();
        for (var r = 0; r < repetitions; r++)
        {
            RunCoreTicks(seed, ticks);
        }

        sw.Stop();
        return new BenchmarkResult("core-tick", EntityCount: 0, ticks, repetitions, sw.Elapsed.TotalMilliseconds);
    }

    private static void RunCoreTicks(ulong seed, long ticks)
    {
        var pipeline = new SimTickPipeline(new SimSeed(seed), new StubEngagementResolver());
        for (long t = 0; t < ticks; t++)
        {
            pipeline.TickOnce(TimeCompressionMode.HeadlessBatch);
        }
    }
}

/// <summary>
/// Derives the per-entity compute budget implied by an NFR target from a measured tick cost.
/// This is what turns the (honest) core-tick number into an actionable engineering constraint.
/// </summary>
public static class EntityScaleBudget
{
    /// <summary>
    /// Wall-clock budget available to a single tick to hit <paramref name="targetRealtimeMultiple"/>,
    /// in nanoseconds. (One sim second is <see cref="BenchmarkResult.TicksPerSimSecond"/> ticks; at
    /// multiple M, one sim second must complete in 1/M wall seconds.)
    /// </summary>
    public static double TickWallBudgetNanos(double targetRealtimeMultiple, double ticksPerSimSecond = BenchmarkResult.TicksPerSimSecond)
    {
        if (targetRealtimeMultiple <= 0) throw new ArgumentOutOfRangeException(nameof(targetRealtimeMultiple));
        var wallSecondsPerSimSecond = 1.0 / targetRealtimeMultiple;
        var wallSecondsPerTick = wallSecondsPerSimSecond / ticksPerSimSecond;
        return wallSecondsPerTick * 1_000_000_000.0;
    }

    /// <summary>
    /// Per-entity nanosecond budget after subtracting the measured fixed per-tick cost. Negative
    /// means the target is already unreachable from fixed cost alone; ~0 means no room for per-entity
    /// work; the value is the single-threaded budget (divide by effective parallelism for a fleet).
    /// </summary>
    public static double PerEntityNanos(
        double measuredFixedNanosPerTick,
        int entityCount,
        double targetRealtimeMultiple,
        double ticksPerSimSecond = BenchmarkResult.TicksPerSimSecond)
    {
        if (entityCount <= 0) throw new ArgumentOutOfRangeException(nameof(entityCount));
        var budget = TickWallBudgetNanos(targetRealtimeMultiple, ticksPerSimSecond);
        return (budget - measuredFixedNanosPerTick) / entityCount;
    }
}
