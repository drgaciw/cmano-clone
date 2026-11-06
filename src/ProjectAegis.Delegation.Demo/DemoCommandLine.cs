namespace ProjectAegis.Delegation.Demo;

/// <summary>Which demo entry the parsed arguments selected.</summary>
public enum DemoMode
{
    /// <summary>One harness run.</summary>
    Single = 0,

    /// <summary>Existing <c>--batch</c> scenario and seed lists.</summary>
    Batch = 1,

    /// <summary><c>--batch --experiment</c> seed-grid run.</summary>
    ExperimentBatch = 2,
}

/// <summary>Outcome of parsing demo arguments.</summary>
public enum DemoParseKind
{
    /// <summary>The caller should print usage and exit 0.</summary>
    Help = 0,

    /// <summary>Arguments describe a run.</summary>
    Ok = 1,

    /// <summary>The caller should print <see cref="DemoParseResult.Error"/> and exit 2.</summary>
    Error = 2,
}

/// <summary>Parsed <c>ProjectAegis.Delegation.Demo</c> arguments.</summary>
public sealed record DemoInvocation(
    DemoMode Mode,
    int Seed,
    string Scenario,
    int Ticks,
    bool Engage,
    bool PrintCsv,
    IReadOnlyList<string> Scenarios,
    IReadOnlyList<int> Seeds,
    bool AllScenarios,
    string? CsvOut,
    string? ExperimentPath);

/// <summary>Help, a successful parse, or a usage error.</summary>
public sealed class DemoParseResult
{
    private DemoParseResult(DemoParseKind kind, DemoInvocation? invocation, string? error)
    {
        Kind = kind;
        Invocation = invocation;
        Error = error;
    }

    /// <summary>Which outcome this result carries.</summary>
    public DemoParseKind Kind { get; }

    /// <summary>Present when <see cref="Kind"/> is <see cref="DemoParseKind.Ok"/>.</summary>
    public DemoInvocation? Invocation { get; }

    /// <summary>Present when <see cref="Kind"/> is <see cref="DemoParseKind.Error"/>.</summary>
    public string? Error { get; }

    /// <summary>Usage requested.</summary>
    public static DemoParseResult Help() => new(DemoParseKind.Help, null, null);

    /// <summary>Arguments parsed.</summary>
    public static DemoParseResult Ok(DemoInvocation invocation) => new(DemoParseKind.Ok, invocation, null);

    /// <summary>Usage error.</summary>
    public static DemoParseResult Fail(string message) => new(DemoParseKind.Error, null, message);
}

/// <summary>Parses demo command-line arguments, including <c>--batch --experiment</c>.</summary>
public static class DemoCommandLine
{
    /// <summary>Parses <paramref name="args"/> using the same defaults as the historical demo entry point.</summary>
    public static DemoParseResult Parse(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);
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
        string? experimentPath = null;
        var scenariosSet = false;
        var seedsSet = false;
        var ticksSet = false;
        var engageSet = false;
        var seedSet = false;
        var scenarioSet = false;
        var csvSet = false;

        for (var i = 0; i < args.Count; i++)
        {
            switch (args[i])
            {
                case "--help" or "-h":
                    return DemoParseResult.Help();
                case "--seed" when i + 1 < args.Count:
                    seed = int.Parse(args[++i], System.Globalization.CultureInfo.InvariantCulture);
                    seedSet = true;
                    break;
                case "--scenario" when i + 1 < args.Count:
                    scenario = args[++i];
                    scenarioSet = true;
                    break;
                case "--ticks" when i + 1 < args.Count:
                    ticks = int.Parse(args[++i], System.Globalization.CultureInfo.InvariantCulture);
                    ticksSet = true;
                    break;
                case "--no-engage":
                    engage = false;
                    engageSet = true;
                    break;
                case "--csv":
                    printCsv = true;
                    csvSet = true;
                    break;
                case "--batch":
                    batch = true;
                    break;
                case "--all-scenarios":
                    allScenarios = true;
                    break;
                case "--scenarios" when i + 1 < args.Count:
                    scenarios = args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                    scenariosSet = true;
                    break;
                case "--seeds" when i + 1 < args.Count:
                    seeds = args[++i]
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(s => int.Parse(s, System.Globalization.CultureInfo.InvariantCulture))
                        .ToList();
                    seedsSet = true;
                    break;
                case "--csv-out" when i + 1 < args.Count:
                    csvOut = args[++i];
                    break;
                case "--experiment" when i + 1 < args.Count:
                    experimentPath = args[++i];
                    break;
                default:
                    return DemoParseResult.Fail("Unknown argument: " + args[i]);
            }
        }

        if (experimentPath != null)
        {
            if (!batch)
            {
                return DemoParseResult.Fail("--experiment requires --batch.");
            }

            if (scenariosSet || seedsSet || allScenarios || ticksSet || engageSet || seedSet || scenarioSet || csvSet)
            {
                return DemoParseResult.Fail(
                    "--experiment defines scenario, seeds, ticks, and engagement. Do not combine it with --scenarios, --seeds, --all-scenarios, --ticks, --no-engage, --seed, --scenario, or --csv.");
            }

            return DemoParseResult.Ok(new DemoInvocation(
                DemoMode.ExperimentBatch,
                seed,
                scenario,
                ticks,
                engage,
                printCsv,
                scenarios,
                seeds,
                allScenarios,
                csvOut,
                experimentPath));
        }

        return DemoParseResult.Ok(new DemoInvocation(
            batch ? DemoMode.Batch : DemoMode.Single,
            seed,
            scenario,
            ticks,
            engage,
            printCsv,
            scenarios,
            seeds,
            allScenarios,
            csvOut,
            experimentPath));
    }
}
