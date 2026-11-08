using ProjectAegis.Delegation.Demo;
using ProjectAegis.Delegation.UnityAdapter.Baltic;

static void PrintUsage()
{
    Console.WriteLine("Project Aegis — Baltic replay harness");
    Console.WriteLine("Usage:");
    Console.WriteLine("  Single: dotnet run --project src/ProjectAegis.Delegation.Demo -- [--seed N] [--scenario ID] [--ticks N] [--no-engage] [--csv]");
    Console.WriteLine("  Batch:  dotnet run --project src/ProjectAegis.Delegation.Demo -- --batch [--scenarios a,b] [--seeds 42,7] [--ticks N] [--csv-out path.csv]");
    Console.WriteLine("          --batch --all-scenarios  (every policy under data/scenarios)");
    Console.WriteLine("          --batch --experiment path.experiment.json [--csv-out path.csv]");
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

static int Dispatch(DemoInvocation invocation)
{
    switch (invocation.Mode)
    {
        case DemoMode.ExperimentBatch:
            var definition = ExperimentDefinitionLoader.LoadFile(invocation.ExperimentPath!);
            return ExperimentSeedGridBatch.Execute(definition, invocation.CsvOut);
        case DemoMode.Batch:
            var runScenarios = invocation.AllScenarios
                ? BalticBatchRunner.DiscoverScenarioIds()
                : invocation.Scenarios;
            return RunBatch(runScenarios, invocation.Seeds, invocation.Ticks, invocation.Engage, invocation.CsvOut);
        case DemoMode.Single:
            return RunSingle(invocation.Seed, invocation.Scenario, invocation.Ticks, invocation.Engage, invocation.PrintCsv);
        default:
            Console.Error.WriteLine("Unknown demo mode.");
            PrintUsage();
            return 2;
    }
}

try
{
    switch (DemoCommandLine.Parse(args))
    {
        case { Kind: DemoParseKind.Help }:
            PrintUsage();
            return 0;
        case { Kind: DemoParseKind.Error, Error: var error }:
            Console.Error.WriteLine(error);
            PrintUsage();
            return 2;
        case { Kind: DemoParseKind.Ok, Invocation: { } invocation }:
            return Dispatch(invocation);
        default:
            Console.Error.WriteLine("Could not parse arguments.");
            PrintUsage();
            return 2;
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"ERROR: {ex.Message}");
    return 1;
}
