using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Baltic;

static void PrintUsage()
{
    Console.WriteLine("Project Aegis — Baltic replay harness");
    Console.WriteLine("Usage:");
    Console.WriteLine("  Single: dotnet run --project src/ProjectAegis.Delegation.Demo -- [--seed N] [--scenario ID] [--ticks N] [--no-engage] [--csv]");
    Console.WriteLine("  Batch:  dotnet run --project src/ProjectAegis.Delegation.Demo -- --batch [--scenarios a,b] [--seeds 42,7] [--ticks N] [--csv-out path.csv]");
    Console.WriteLine("          --batch --all-scenarios  (every policy under data/scenarios)");
}

static int RunSingle(int seed, string scenario, int ticks, bool engage, bool printCsv)
{
    var result = BalticReplayHarness.Run(seed, scenario, ticks, mvpEngagement: engage);
    Console.WriteLine($"SEED={result.Seed} SCENARIO={result.ScenarioPolicyId} TICKS={result.Ticks} ENGAGEMENTS={result.EngagementCount}");
    Console.WriteLine($"FINGERPRINT={result.Fingerprint}");
    Console.WriteLine($"FINGERPRINT_SHA256={result.FingerprintSha256}");
    Console.WriteLine($"DETECTION_WORLD_HASH={result.DetectionWorldHash}");
    Console.WriteLine($"WORLD_HASH={result.WorldHash}");
    if (printCsv)
    {
        Console.WriteLine(result.ScoringCsvRow);
    }

    foreach (var checkpoint in result.Checkpoints)
    {
        Console.WriteLine(
            $"REPLAY_CHECKPOINT={checkpoint.SimTick}:{checkpoint.WorldHash}:{checkpoint.LastSequenceId}");
    }

    foreach (var message in result.Messages.Where(m =>
                 m.Category is "KILL_CONFIRMED" or "INTERCEPT_SUCCESS" or "HIT" or "MISS" or "COMMS"))
    {
        Console.WriteLine($"MESSAGE={message.Category}|{message.Text}");
    }

    return 0;
}

static int RunBatch(
    IReadOnlyList<string> scenarios,
    IReadOnlyList<int> seeds,
    int ticks,
    bool engage,
    string? csvOut)
{
    var rows = BalticBatchRunner.Run(new BalticBatchRunner.BatchRequest(scenarios, seeds, ticks, engage));
    var csv = BalticBatchRunner.ExportCsv(rows);
    if (csvOut != null)
    {
        File.WriteAllText(csvOut, csv);
        Console.WriteLine($"Wrote {rows.Count} rows to {csvOut}");
    }
    else
    {
        Console.Write(csv);
    }

    return 0;
}

var seed = 42;
var scenario = "baltic-patrol";
var ticks = 4;
var engage = true;
var printCsv = false;
var batch = false;
var allScenarios = false;
var scenarios = new List<string> { "baltic-patrol", "baltic-patrol-comms", "baltic-patrol-classify" };
var seeds = new List<int> { 42 };
string? csvOut = null;

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--help" or "-h":
            PrintUsage();
            return 0;
        case "--seed" when i + 1 < args.Length:
            seed = int.Parse(args[++i], System.Globalization.CultureInfo.InvariantCulture);
            break;
        case "--scenario" when i + 1 < args.Length:
            scenario = args[++i];
            break;
        case "--ticks" when i + 1 < args.Length:
            ticks = int.Parse(args[++i], System.Globalization.CultureInfo.InvariantCulture);
            break;
        case "--no-engage":
            engage = false;
            break;
        case "--csv":
            printCsv = true;
            break;
        case "--batch":
            batch = true;
            break;
        case "--all-scenarios":
            allScenarios = true;
            break;
        case "--scenarios" when i + 1 < args.Length:
            scenarios = args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            break;
        case "--seeds" when i + 1 < args.Length:
            seeds = args[++i]
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => int.Parse(s, System.Globalization.CultureInfo.InvariantCulture))
                .ToList();
            break;
        case "--csv-out" when i + 1 < args.Length:
            csvOut = args[++i];
            break;
        default:
            Console.Error.WriteLine($"Unknown argument: {args[i]}");
            PrintUsage();
            return 2;
    }
}

try
{
    if (batch)
    {
        var runScenarios = allScenarios
            ? BalticBatchRunner.DiscoverScenarioIds()
            : scenarios;
        return RunBatch(runScenarios, seeds, ticks, engage, csvOut);
    }

    return RunSingle(seed, scenario, ticks, engage, printCsv);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"ERROR: {ex.Message}");
    return 1;
}