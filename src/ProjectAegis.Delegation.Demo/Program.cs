using ProjectAegis.Delegation.UnityAdapter.Baltic;

static void PrintUsage()
{
    Console.WriteLine("Project Aegis — Baltic replay harness");
    Console.WriteLine("Usage: dotnet run --project src/ProjectAegis.Delegation.Demo -- [--seed N] [--scenario ID] [--ticks N] [--no-engage]");
}

var seed = 42;
var scenario = "baltic-patrol";
var ticks = 4;
var engage = true;

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
        default:
            Console.Error.WriteLine($"Unknown argument: {args[i]}");
            PrintUsage();
            return 2;
    }
}

try
{
    var result = BalticReplayHarness.Run(seed, scenario, ticks, mvpEngagement: engage);
    Console.WriteLine($"SEED={result.Seed} SCENARIO={result.ScenarioPolicyId} TICKS={result.Ticks} ENGAGEMENTS={result.EngagementCount}");
    Console.WriteLine($"FINGERPRINT={result.Fingerprint}");
    Console.WriteLine($"DETECTION_WORLD_HASH={result.DetectionWorldHash}");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"ERROR: {ex.Message}");
    return 1;
}