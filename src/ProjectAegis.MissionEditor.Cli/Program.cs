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
    case "scenario_create":
        return RunScenarioCreate(args.Skip(1).ToArray());
    case "mission_add_patrol":
        return RunMissionAddPatrol(args.Skip(1).ToArray());
    case "mission_add_strike":
        return RunMissionAddStrike(args.Skip(1).ToArray());
    case "mission_update_patrol":
        return RunMissionUpdatePatrol(args.Skip(1).ToArray());
    case "mission_update_strike":
        return RunMissionUpdateStrike(args.Skip(1).ToArray());
    case "mission_delete":
        return RunMissionDelete(args.Skip(1).ToArray());
    case "mission_plan_suggest":
        return RunMissionPlanSuggest(args.Skip(1).ToArray());
    case "scenario_comms_status":
        return RunScenarioCommsStatus(args.Skip(1).ToArray());
    case "scenario_cyber_status":
        return RunScenarioCyberStatus(args.Skip(1).ToArray());
    case "scenario_near_future_spawn":
        return RunScenarioNearFutureSpawn(args.Skip(1).ToArray());
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

static int RunScenarioCreate(string[] args)
{
    var outPath = CliArgParser.GetFlag(args, "--out");
    if (string.IsNullOrWhiteSpace(outPath))
    {
        Console.Error.WriteLine("scenario_create requires --out <scenario.json>");
        return 1;
    }

    return ScenarioCreateCommand.Run(
        outPath,
        CliArgParser.GetFlag(args, "--db-ref"),
        CliArgParser.GetFlag(args, "--policy-id"),
        CliArgParser.GetULongFlag(args, "--seed"),
        Console.Out);
}

static int RunMissionAddPatrol(string[] args)
{
    var path = CliArgParser.GetFlag(args, "--path");
    var missionId = CliArgParser.GetFlag(args, "--id");
    var editVersion = CliArgParser.GetIntFlag(args, "--edit-version", -1);
    if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(missionId) || editVersion < 0)
    {
        Console.Error.WriteLine("mission_add_patrol requires --path --edit-version --id [--unit U]+ [--wp lat,lon]+");
        return 1;
    }

    try
    {
        var zone = CliArgParser.ParseWaypoints(CliArgParser.GetRepeated(args, "--wp"));
        return MissionAddPatrolCommand.Run(
            path,
            editVersion,
            missionId,
            CliArgParser.GetRepeated(args, "--unit"),
            zone,
            Console.Out);
    }
    catch (FormatException ex)
    {
        return McpToolResult.WriteError(Console.Out, "INVALID_ZONE", ex.Message);
    }
}

static int RunMissionAddStrike(string[] args)
{
    var path = CliArgParser.GetFlag(args, "--path");
    var missionId = CliArgParser.GetFlag(args, "--id");
    var editVersion = CliArgParser.GetIntFlag(args, "--edit-version", -1);
    if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(missionId) || editVersion < 0)
    {
        Console.Error.WriteLine("mission_add_strike requires --path --edit-version --id --unit U [--target T]+");
        return 1;
    }

    return MissionAddStrikeCommand.Run(
        path,
        editVersion,
        missionId,
        CliArgParser.GetRepeated(args, "--unit"),
        CliArgParser.GetRepeated(args, "--target"),
        Console.Out);
}

static int RunMissionUpdatePatrol(string[] args)
{
    var path = CliArgParser.GetFlag(args, "--path");
    var missionId = CliArgParser.GetFlag(args, "--id");
    var editVersion = CliArgParser.GetIntFlag(args, "--edit-version", -1);
    if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(missionId) || editVersion < 0)
    {
        Console.Error.WriteLine("mission_update_patrol requires --path --edit-version --id [--unit U]+ [--wp lat,lon]+");
        return 1;
    }

    try
    {
        var zone = CliArgParser.ParseWaypoints(CliArgParser.GetRepeated(args, "--wp"));
        return MissionUpdatePatrolCommand.Run(
            path,
            editVersion,
            missionId,
            CliArgParser.GetRepeated(args, "--unit"),
            zone.Count > 0 ? zone : null,
            Console.Out);
    }
    catch (FormatException ex)
    {
        return McpToolResult.WriteError(Console.Out, "INVALID_ZONE", ex.Message);
    }
}

static int RunMissionUpdateStrike(string[] args)
{
    var path = CliArgParser.GetFlag(args, "--path");
    var missionId = CliArgParser.GetFlag(args, "--id");
    var editVersion = CliArgParser.GetIntFlag(args, "--edit-version", -1);
    if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(missionId) || editVersion < 0)
    {
        Console.Error.WriteLine("mission_update_strike requires --path --edit-version --id [--unit U]+ [--target T]+");
        return 1;
    }

    return MissionUpdateStrikeCommand.Run(
        path,
        editVersion,
        missionId,
        CliArgParser.GetRepeated(args, "--unit"),
        CliArgParser.GetRepeated(args, "--target"),
        Console.Out);
}

static int RunMissionPlanSuggest(string[] args)
{
    var intent = CliArgParser.GetFlag(args, "--intent");
    if (string.IsNullOrWhiteSpace(intent))
    {
        Console.Error.WriteLine("mission_plan_suggest requires --intent <text>");
        return 1;
    }

    return MissionPlanSuggestCommand.Run(intent, Console.Out);
}

static int RunScenarioCommsStatus(string[] args)
{
    var policyId = CliArgParser.GetFlag(args, "--policy");
    return ScenarioCommsStatusCommand.Run(policyId, Console.Out);
}

static int RunScenarioCyberStatus(string[] args)
{
    var policyId = CliArgParser.GetFlag(args, "--policy");
    return ScenarioCyberStatusCommand.Run(policyId, Console.Out);
}

static int RunScenarioNearFutureSpawn(string[] args)
{
    var path = CliArgParser.GetFlag(args, "--path");
    if (string.IsNullOrWhiteSpace(path))
    {
        Console.Error.WriteLine("scenario_near_future_spawn requires --path <scenario.json>");
        return 1;
    }

    return ScenarioNearFutureSpawnCommand.Run(path, Console.Out);
}

static int RunMissionDelete(string[] args)
{
    var path = CliArgParser.GetFlag(args, "--path");
    var missionId = CliArgParser.GetFlag(args, "--id");
    var editVersion = CliArgParser.GetIntFlag(args, "--edit-version", -1);
    if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(missionId) || editVersion < 0)
    {
        Console.Error.WriteLine("mission_delete requires --path --edit-version --id");
        return 1;
    }

    return MissionDeleteCommand.Run(path, editVersion, missionId, Console.Out);
}

static void PrintUsage()
{
    Console.WriteLine("Project Aegis — Mission Editor headless MCP tools");
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_create --out <scenario.json> [--db-ref R] [--policy-id P] [--seed N]");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- mission_add_patrol --path <scenario.json> --edit-version N --id <id> --unit U [--wp lat,lon]+");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- mission_add_strike --path <scenario.json> --edit-version N --id <id> --unit U --target T");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- mission_update_patrol --path <scenario.json> --edit-version N --id <id> [--unit U]+ [--wp lat,lon]+");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- mission_update_strike --path <scenario.json> --edit-version N --id <id> [--unit U]+ [--target T]+");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- mission_delete --path <scenario.json> --edit-version N --id <id>");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_validate --path <scenario.json>");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_export_brief --path <scenario.json> [--out brief.md]");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_simulate_sample --path <scenario.json> [--ticks N]");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- mission_plan_suggest --intent \"patrol and strike baltic\"");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_comms_status --policy baltic-patrol-comms");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_cyber_status --policy baltic-patrol-comms");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_near_future_spawn --path <scenario.json>");
}