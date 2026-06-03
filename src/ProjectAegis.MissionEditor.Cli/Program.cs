using ProjectAegis.MissionEditor.Cli;

if (args.Length == 0)
{
    PrintUsage();
    return 1;
}

var command = args[0];
switch (command)
{
    case "scenario_validate":
        return RunScenarioValidate(args.Skip(1).ToArray());
    case "scenario_export_brief":
        return RunExportBrief(args.Skip(1).ToArray());
    case "scenario_simulate_sample":
        return RunSimulateSample(args.Skip(1).ToArray());
    default:
        Console.Error.WriteLine($"Unknown command: {command}");
        PrintUsage();
        return 1;
}

static int RunScenarioValidate(string[] args)
{
    string? path = null;
    for (var i = 0; i < args.Length; i++)
    {
        if (args[i] == "--path" && i + 1 < args.Length)
        {
            path = args[++i];
        }
    }

    if (string.IsNullOrWhiteSpace(path))
    {
        Console.Error.WriteLine("scenario_validate requires --path <scenario.json>");
        return 1;
    }

    return ScenarioValidateCommand.Run(path, quiet: false, Console.Out);
}

static int RunExportBrief(string[] args)
{
    string? path = null;
    string? outPath = null;
    for (var i = 0; i < args.Length; i++)
    {
        if (args[i] == "--path" && i + 1 < args.Length)
        {
            path = args[++i];
        }
        else if (args[i] == "--out" && i + 1 < args.Length)
        {
            outPath = args[++i];
        }
    }

    if (string.IsNullOrWhiteSpace(path))
    {
        Console.Error.WriteLine("scenario_export_brief requires --path <scenario.json>");
        return 1;
    }

    var exit = ScenarioValidateCommand.Run(path, quiet: true, Console.Out);
    if (exit != 0)
    {
        Console.Error.WriteLine("Export blocked: validation failed.");
        return exit;
    }

    outPath ??= Path.ChangeExtension(path, ".brief.md");
    File.WriteAllText(outPath, $"# Scenario brief\n\nSource: `{path}`\n\n_Validation passed; briefing content is P1._\n");
    Console.WriteLine($"BRIEF_WRITTEN={outPath}");
    return 0;
}

static int RunSimulateSample(string[] args)
{
    string? path = null;
    var ticks = 32;
    for (var i = 0; i < args.Length; i++)
    {
        if (args[i] == "--path" && i + 1 < args.Length)
        {
            path = args[++i];
        }
        else if (args[i] == "--ticks" && i + 1 < args.Length && int.TryParse(args[++i], out var t))
        {
            ticks = Math.Max(1, t);
        }
    }

    if (string.IsNullOrWhiteSpace(path))
    {
        Console.Error.WriteLine("scenario_simulate_sample requires --path <scenario.json>");
        return 1;
    }

    return ScenarioSimulateSampleCommand.Run(path, ticks, quiet: false, Console.Out);
}

static void PrintUsage()
{
    Console.WriteLine("Project Aegis — Mission Editor headless MCP tools");
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_validate --path <scenario.json>");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_export_brief --path <scenario.json> [--out brief.md]");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_simulate_sample --path <scenario.json> [--ticks N]");
}