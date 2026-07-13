using System.Globalization;
using ProjectAegis.Sim.Benchmark;

// Headless sim throughput benchmark (INF-5.1). Managed timing only — no Unity player loop.
//
//   dotnet run -c Release --project src/ProjectAegis.Sim.Benchmark -- \
//     [--ticks N] [--reps N] [--warmup N] [--seed N] [--out path] [--format csv|json] [--budget]
//
// Measures core-tick throughput and (with --budget) prints the per-entity compute budget the
// 25k@1000x NFR implies. See docs/reports/sim-entity-scale-benchmark-2026-07-08.md.

static string? Arg(string[] a, string name)
{
    var i = Array.IndexOf(a, name);
    return i >= 0 && i + 1 < a.Length ? a[i + 1] : null;
}

static bool Flag(string[] a, string name) => Array.IndexOf(a, name) >= 0;

var inv = CultureInfo.InvariantCulture;
long ticks = long.Parse(Arg(args, "--ticks") ?? "5000000", inv);
int reps = int.Parse(Arg(args, "--reps") ?? "3", inv);
long warmup = long.Parse(Arg(args, "--warmup") ?? "200000", inv);
ulong seed = ulong.Parse(Arg(args, "--seed") ?? "42", inv);
string format = (Arg(args, "--format") ?? "csv").ToLowerInvariant();
string? outPath = Arg(args, "--out");

Console.Error.WriteLine(
    $"[benchmark] core-tick: ticks={ticks} reps={reps} warmup={warmup} seed={seed} (entity workload=0; see gap report)");

var result = SimBenchmark.MeasureCoreTick(seed, ticks, reps, warmup);
var rows = new[] { result };

string artifact = format == "json" ? BenchmarkReport.ToJson(rows) : BenchmarkReport.ToCsv(rows);
if (outPath != null)
{
    File.WriteAllText(outPath, artifact);
    Console.Error.WriteLine($"[benchmark] wrote {format} artifact -> {outPath}");
}

Console.Write(artifact);

Console.Error.WriteLine(
    $"[benchmark] {result.TicksPerSecond:N0} ticks/s -> {result.EffectiveRealtimeMultiple:N0}x realtime " +
    $"({result.NanosPerTick:0.###} ns/tick fixed cost).");

if (Flag(args, "--budget"))
{
    int[] entityCounts = { 1_000, 5_000, 10_000, 25_000 };
    double[] targets = { 256, 1000 };
    Console.Error.WriteLine();
    Console.Error.WriteLine("[budget] single-threaded per-entity ns budget = (tick wall budget - measured fixed cost) / entities");
    foreach (var mult in targets)
    {
        var tickBudget = EntityScaleBudget.TickWallBudgetNanos(mult);
        Console.Error.WriteLine($"[budget] target {mult:N0}x realtime -> {tickBudget:0.###} ns wall / tick:");
        foreach (var n in entityCounts)
        {
            var perEntity = EntityScaleBudget.PerEntityNanos(result.NanosPerTick, n, mult);
            Console.Error.WriteLine($"[budget]   {n,6:N0} entities -> {perEntity,10:0.###} ns/entity/tick (1 core)");
        }
    }
}

return 0;
