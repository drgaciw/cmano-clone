using ProjectAegis.Data.Import;
using ProjectAegis.Data.Osint;
using ProjectAegis.Data.Osint.Connectors;
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
    case "mission_add_support":
        return RunMissionAddSupport(args.Skip(1).ToArray());
    case "mission_add_ferry":
        return RunMissionAddFerry(args.Skip(1).ToArray());
    case "reference_point_set":
        return RunReferencePointSet(args.Skip(1).ToArray());
    case "scenario_load":
        return RunScenarioLoad(args.Skip(1).ToArray());
    case "scenario_save":
        return RunScenarioSave(args.Skip(1).ToArray());
    case "event_add":
        return RunEventAdd(args.Skip(1).ToArray());
    case "event_validate":
        return RunEventValidate(args.Skip(1).ToArray());
    case "mission_plan_suggest":
        return RunMissionPlanSuggest(args.Skip(1).ToArray());
    case "scenario_comms_status":
        return RunScenarioCommsStatus(args.Skip(1).ToArray());
    case "scenario_cyber_status":
        return RunScenarioCyberStatus(args.Skip(1).ToArray());
    case "scenario_near_future_spawn":
        return RunScenarioNearFutureSpawn(args.Skip(1).ToArray());
    case "catalog_intelligence_run":
        return RunCatalogIntelligence(args.Skip(1).ToArray());
    case "catalog_entity_map":
        return CatalogEntityMapCommand.Run(Console.Out);
    case "catalog_write_propose":
        return RunCatalogWritePropose(args.Skip(1).ToArray());
    case "catalog_write_approve":
        return RunCatalogWriteApprove(args.Skip(1).ToArray());
    case "catalog_import_markdown":
        return RunCatalogImportMarkdown(args.Skip(1).ToArray());
    case "osint_staging_review":
        return RunOsintStagingReview(args.Skip(1).ToArray());
    case "osint_search":
        return RunOsintSearch(args.Skip(1).ToArray());
    // S21: add cases for osint_digest, osint_list_staging_proposals etc as needed (delegate to runner/OsintStagingReviewCommand)
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

static int RunMissionAddSupport(string[] args)
{
    var path = CliArgParser.GetFlag(args, "--path");
    var editVersion = CliArgParser.GetIntFlag(args, "--edit-version", -1);
    var missionId = CliArgParser.GetFlag(args, "--id");
    var role = CliArgParser.GetFlag(args, "--role") ?? "Tanker";
    var units = CliArgParser.GetRepeated(args, "--unit");
    if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(missionId) ||
        units.Count == 0 || editVersion < 0)
    {
        Console.Error.WriteLine("mission_add_support requires --path --edit-version --id --unit U [--wp lat,lon]+ [--role Tanker|AEW|EW]");
        return 1;
    }

    try
    {
        var waypoints = CliArgParser.ParseWaypoints(CliArgParser.GetRepeated(args, "--wp"));
        return MissionAddSupportCommand.Run(path, editVersion, missionId, units, role, waypoints, Console.Out);
    }
    catch (FormatException ex)
    {
        return McpToolResult.WriteError(Console.Out, "INVALID_ZONE", ex.Message);
    }
}

static int RunMissionAddFerry(string[] args)
{
    var path = CliArgParser.GetFlag(args, "--path");
    var editVersion = CliArgParser.GetIntFlag(args, "--edit-version", -1);
    var missionId = CliArgParser.GetFlag(args, "--id");
    var destination = CliArgParser.GetFlag(args, "--destination");
    var units = CliArgParser.GetRepeated(args, "--unit");
    if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(missionId) ||
        string.IsNullOrWhiteSpace(destination) || units.Count == 0 || editVersion < 0)
    {
        Console.Error.WriteLine("mission_add_ferry requires --path --edit-version --id --unit U --destination <baseId>");
        return 1;
    }

    return MissionAddFerryCommand.Run(path, editVersion, missionId, units, destination, Console.Out);
}

static int RunReferencePointSet(string[] args)
{
    var path = CliArgParser.GetFlag(args, "--path");
    var editVersion = CliArgParser.GetIntFlag(args, "--edit-version", -1);
    var pointId = CliArgParser.GetFlag(args, "--id");
    var geometryType = CliArgParser.GetFlag(args, "--type") ?? "point";
    var radiusRaw = CliArgParser.GetFlag(args, "--radius-nm");
    double? radius = double.TryParse(radiusRaw, out var r) ? r : null;
    if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(pointId) || editVersion < 0)
    {
        Console.Error.WriteLine("reference_point_set requires --path --edit-version --id --wp lat,lon");
        return 1;
    }

    try
    {
        var waypoints = CliArgParser.ParseWaypoints(CliArgParser.GetRepeated(args, "--wp"));
        if (waypoints.Count == 0)
        {
            Console.Error.WriteLine("reference_point_set requires --path --edit-version --id --wp lat,lon");
            return 1;
        }

        return ReferencePointSetCommand.Run(path, editVersion, pointId, geometryType, waypoints, radius, Console.Out);
    }
    catch (FormatException ex)
    {
        return McpToolResult.WriteError(Console.Out, "INVALID_ZONE", ex.Message);
    }
}

static int RunScenarioLoad(string[] args)
{
    var source = CliArgParser.GetFlag(args, "--path");
    var dest = CliArgParser.GetFlag(args, "--out");
    if (string.IsNullOrWhiteSpace(source))
    {
        Console.Error.WriteLine("scenario_load requires --path <file.json|.aegis-scenario> [--out dest.json]");
        return 1;
    }

    // Package loads must not overwrite the zip with stable JSON; require an explicit --out.
    if (source.EndsWith(".aegis-scenario", StringComparison.OrdinalIgnoreCase) &&
        string.IsNullOrWhiteSpace(dest))
    {
        Console.Error.WriteLine("scenario_load of .aegis-scenario package requires --out <dest.json>");
        return 1;
    }

    return ScenarioLoadCommand.Run(source, dest, Console.Out);
}

static int RunScenarioSave(string[] args)
{
    var path = CliArgParser.GetFlag(args, "--path");
    var editVersion = CliArgParser.GetIntFlag(args, "--edit-version", -1);
    if (string.IsNullOrWhiteSpace(path) || editVersion < 0)
    {
        Console.Error.WriteLine("scenario_save requires --path --edit-version N");
        return 1;
    }

    return ScenarioSaveCommand.Run(path, editVersion, Console.Out);
}

static int RunEventAdd(string[] args)
{
    var path = CliArgParser.GetFlag(args, "--path");
    var editVersion = CliArgParser.GetIntFlag(args, "--edit-version", -1);
    var eventId = CliArgParser.GetFlag(args, "--id");
    var priority = CliArgParser.GetIntFlag(args, "--priority", 100);
    var triggerType = CliArgParser.GetFlag(args, "--trigger") ?? "Time";
    var atTick = CliArgParser.GetIntFlag(args, "--at-tick", -1);
    int? tick = atTick >= 0 ? atTick : null;
    if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(eventId) || editVersion < 0)
    {
        Console.Error.WriteLine("event_add requires --path --edit-version --id --trigger Time [--at-tick N]");
        return 1;
    }

    return EventAddCommand.Run(path, editVersion, eventId, priority, triggerType, tick, Console.Out);
}

static int RunEventValidate(string[] args)
{
    var path = CliArgParser.GetFlag(args, "--path");
    var eventId = CliArgParser.GetFlag(args, "--id");
    if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(eventId))
    {
        Console.Error.WriteLine("event_validate requires --path --id");
        return 1;
    }

    return EventValidateCommand.Run(path, eventId, Console.Out);
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
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- mission_add_support --path <scenario.json> --edit-version N --id <id> --unit U [--wp lat,lon]+ [--role Tanker|AEW|EW]");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- mission_add_ferry --path <scenario.json> --edit-version N --id <id> --unit U --destination <baseId>");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- reference_point_set --path <scenario.json> --edit-version N --id <id> --wp lat,lon");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_load --path <file.json|.aegis-scenario> [--out dest.json]  # --out required for packages");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_save --path <scenario.json> --edit-version N");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- event_add --path <scenario.json> --edit-version N --id <id> --trigger Time [--at-tick N]");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- event_validate --path <scenario.json> --id <id>");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_validate --path <scenario.json>");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_export_brief --path <scenario.json> [--out brief.md]");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_simulate_sample --path <scenario.json> [--ticks N]");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- mission_plan_suggest --intent \"patrol and strike baltic\"");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_comms_status --policy baltic-patrol-comms");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_cyber_status --policy baltic-patrol-comms");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_near_future_spawn --path <scenario.json>");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_intelligence_run [--db <catalog.db>]");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_entity_map");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_write_propose --db <catalog.db> --platform P --sensor S --base-pd 0.7");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_write_approve --db <catalog.db> --batch <batchId>");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_import_markdown --db <catalog.db> --markdown <sensor.md> [--max-records N] [--chunk-size 500] [--report-out report.json]");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- osint_staging_review --db <catalog.db> [--approve <batchId>]");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- osint_search [--db <fixture.json>]  # S21 MCP search_osint");
    // S21: osint_digest, osint_list_staging_proposals, osint_get_proposal_detail, osint_submit_review_decision
}

static int RunCatalogImportMarkdown(string[] args)
{
    var db = CliArgParser.GetFlag(args, "--db");
    var markdown = CliArgParser.GetFlag(args, "--markdown");
    if (string.IsNullOrWhiteSpace(db) || string.IsNullOrWhiteSpace(markdown))
    {
        Console.Error.WriteLine("catalog_import_markdown requires --db --markdown");
        return 1;
    }

    var maxRecordsRaw = CliArgParser.GetFlag(args, "--max-records");
    int? maxRecords = int.TryParse(maxRecordsRaw, out var max) ? max : null;
    var chunkSize = CliArgParser.GetIntFlag(args, "--chunk-size", CmoMarkdownImportProposer.DefaultChunkSize);
    var reportOut = CliArgParser.GetFlag(args, "--report-out");
    return CatalogImportMarkdownCommand.Run(db, markdown, maxRecords, chunkSize, Console.Out, reportOut);
}

static int RunOsintStagingReview(string[] args)
{
    var db = CliArgParser.GetFlag(args, "--db");
    var approve = CliArgParser.GetFlag(args, "--approve");
    if (string.IsNullOrWhiteSpace(db))
    {
        Console.Error.WriteLine("osint_staging_review requires --db <catalog.db> [--approve <batchId>]");
        return 1;
    }
    return OsintStagingReviewCommand.Run(db, approve, Console.Out);
}

static int RunOsintSearch(string[] args)
{
    // S20-01 / S21: osint_search using real fixture (data/osint_facts.json) + FileOsintConnector + runner.
    // Prefer the committed real fixture for CLI/MCP fallback; --db <path> overrides only if exists.
    // Graceful: missing fixture -> File returns empty (deterministic). All connectors implement IOsintConnector.
    var overrideDb = CliArgParser.GetFlag(args, "--db");
    string fixturePath = Path.Combine("data", "osint_facts.json");
    IOsintConnector conn;
    if (!string.IsNullOrWhiteSpace(overrideDb) && File.Exists(overrideDb))
    {
        conn = new FileOsintConnector(overrideDb);
    }
    else
    {
        conn = new FileOsintConnector(fixturePath); // real fixture (or empty if absent)
    }
    var runner = new OsintDigestRunner(0.65);
    var (proposals, logOnly) = runner.Run(conn.Fetch());
    return McpToolResult.WriteOk(Console.Out, new
    {
        ok = true,
        proposals = proposals.Select(p => new { p.CanonicalId, p.SourceUrl, p.RelevanceScore, p.Snippet }).ToArray(),
        logOnlyCount = logOnly.Length
    });
}

// Similar minimal for other S21 MCP tools (osint_digest, list_staging, detail, submit) can delegate to existing runner/OsintStagingReviewCommand
// For brevity in S21, osint_list_staging_proposals etc reuse OsintStagingReviewCommand.Run pattern.

static int RunCatalogIntelligence(string[] args)
{
    var db = CliArgParser.GetFlag(args, "--db");
    return CatalogIntelligenceRunCommand.Run(db, Console.Out);
}

static int RunCatalogWritePropose(string[] args)
{
    var db = CliArgParser.GetFlag(args, "--db");
    var platform = CliArgParser.GetFlag(args, "--platform");
    var sensor = CliArgParser.GetFlag(args, "--sensor");
    var basePd = CliArgParser.GetDoubleFlag(args, "--base-pd", -1);
    if (string.IsNullOrWhiteSpace(db) || string.IsNullOrWhiteSpace(platform) ||
        string.IsNullOrWhiteSpace(sensor) || basePd < 0)
    {
        Console.Error.WriteLine("catalog_write_propose requires --db --platform --sensor --base-pd");
        return 1;
    }

    return CatalogWriteProposeCommand.Run(db, platform, sensor, basePd, Console.Out);
}

static int RunCatalogWriteApprove(string[] args)
{
    var db = CliArgParser.GetFlag(args, "--db");
    var batch = CliArgParser.GetFlag(args, "--batch");
    if (string.IsNullOrWhiteSpace(db) || string.IsNullOrWhiteSpace(batch))
    {
        Console.Error.WriteLine("catalog_write_approve requires --db --batch");
        return 1;
    }

    var snapshotId = CliArgParser.GetFlag(args, "--snapshot-id");
    var releaseVersion = CliArgParser.GetFlag(args, "--release-version");
    return CatalogWriteApproveCommand.Run(db, batch, Console.Out, snapshotId, releaseVersion);
}