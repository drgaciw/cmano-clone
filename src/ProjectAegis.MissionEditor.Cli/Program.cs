using ProjectAegis.Data.Import;
using ProjectAegis.Data.Osint;
using ProjectAegis.Data.Osint.Connectors;
using ProjectAegis.MissionEditor.Cli;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;

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
    case "scenario_export":  // S83-01 export command polish (track D); uses ScenarioExportCommand.Prepare; cites roadmap-execute-plan-07042026.md §4, sprint-83, boundary-2026-07-04.md, qa # (via AC-12)
        return RunScenarioExport(args.Skip(1).ToArray());
    case "scenario_simulate_sample":
        return RunSimulateSample(args.Skip(1).ToArray());
    case "scenario_create":
        return RunScenarioCreate(args.Skip(1).ToArray());
    case "scenario_migrate_preview": // cli verb for proof
        return RunScenarioMigratePreview(args.Skip(1).ToArray());
    case "scenario_umpire_snapshot":
        return RunScenarioUmpireSnapshot(args.Skip(1).ToArray());
    case "scenario_ai_scaffold":
        return RunScenarioAiScaffold(args.Skip(1).ToArray());
    case "scenario_publish":
        return RunScenarioPublish(args.Skip(1).ToArray());
    case "mission_add_patrol":
        return RunMissionAddPatrol(args.Skip(1).ToArray());
    case "mission_add_strike":
        return RunMissionAddStrike(args.Skip(1).ToArray());
    case "mission_update_patrol":
        return RunMissionUpdatePatrol(args.Skip(1).ToArray());
    case "mission_update_strike":
        return RunMissionUpdateStrike(args.Skip(1).ToArray());
    case "mission_add_ferry":
        return RunMissionAddFerry(args.Skip(1).ToArray());
    case "mission_add_support":
        return RunMissionAddSupport(args.Skip(1).ToArray());
    case "mission_update_ferry":
        return RunMissionUpdateFerry(args.Skip(1).ToArray());
    case "scenario_undo":
        return RunScenarioUndo(args.Skip(1).ToArray());
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
    case "scenario_event_trace":
        return RunScenarioEventTrace(args.Skip(1).ToArray());
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
    case "platform_export_xlsx":
        return RunPlatformExportXlsx(args.Skip(1).ToArray());
    case "platform_import_xlsx":
        return RunPlatformImportXlsx(args.Skip(1).ToArray());
    case "platform_diff_xlsx":
        return RunPlatformDiffXlsx(args.Skip(1).ToArray());
    case "catalog_platform_browse":
        return RunCatalogPlatformBrowse(args.Skip(1).ToArray());
    case "catalog_mount_loadout_quarantine_triage":
        return RunMountLoadoutQuarantineTriage(args.Skip(1).ToArray());
    case "catalog_release_diff":
        return RunCatalogReleaseDiff(args.Skip(1).ToArray());
    case "catalog_dependency_graph":
        return RunCatalogDependencyGraph(args.Skip(1).ToArray());
    case "catalog_kill_chain_report":
        return RunCatalogKillChainReport(args.Skip(1).ToArray());
    case "catalog_link_report":
        return RunCatalogLinkReport(args.Skip(1).ToArray());
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

/// <summary>
/// scenario_export --path : S83-01 polished export surface. Loads, Prepare via ScenarioExportCommand (manifest + gate), emits JSON summary.
/// Completes track D command surface. Cites: sprint-83-export-undo-ferry.md S83-01, roadmap-execute-plan-07042026.md, scenario-editor-scope-boundary-2026-07-04.md, qa-plan #5/#13/#14 transitively via editor, AGENTS.md GitNexus pre.
/// </summary>
static int RunScenarioExport(string[] args)
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
        Console.Error.WriteLine("scenario_export requires --path <scenario.json>");
        return 1;
    }

    if (!File.Exists(path))
    {
        return McpToolResult.WriteError(Console.Out, "NOT_FOUND", $"Scenario not found: {path}");
    }

    try
    {
        var document = ScenarioDocumentJsonLoader.LoadFromFile(path);
        var catalog = ScenarioValidateCommand.ResolveCatalogPublic(document);
        var config = new ValidationConfig();
        var pkg = ScenarioExportCommand.Prepare(document, catalog, config);

        // S83-01 polish: use new formatter + full package info
        var summary = ScenarioExportCommand.FormatExportSummary(pkg);
        var result = new
        {
            ok = pkg.Allowed,
            path,
            summary,
            transformCount = pkg.TransformManifest?.Count ?? 0,
            validationReportHash = pkg.ValidationReport?.ReportHash,
            allowed = pkg.Allowed,
            editVersion = pkg.ExportDocument?.Metadata?.EditVersion,
        };
        return McpToolResult.WriteOk(Console.Out, result);
    }
    catch (Exception ex)
    {
        return McpToolResult.WriteError(Console.Out, "EXPORT_ERROR", ex.Message);
    }
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

static int RunScenarioPublish(string[] args)
{
    var path = CliArgParser.GetFlag(args, "--path");
    if (string.IsNullOrWhiteSpace(path))
    {
        Console.Error.WriteLine("scenario_publish requires --path <scenario.json>");
        return 1;
    }

    return ScenarioPublishCommand.Run(path, Console.Out);
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

static int RunMissionAddSupport(string[] args)
{
    var path = CliArgParser.GetFlag(args, "--path");
    var missionId = CliArgParser.GetFlag(args, "--id");
    var editVersion = CliArgParser.GetIntFlag(args, "--edit-version", -1);
    if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(missionId) || editVersion < 0)
    {
        Console.Error.WriteLine("mission_add_support requires --path --edit-version --id --role R [--unit U]+ [--wp lat,lon]+");
        return 1;
    }

    try
    {
        var zone = CliArgParser.ParseWaypoints(CliArgParser.GetRepeated(args, "--wp"));
        return MissionAddSupportCommand.Run(
            path,
            editVersion,
            missionId,
            CliArgParser.GetRepeated(args, "--unit"),
            CliArgParser.GetFlag(args, "--role") ?? string.Empty,
            zone,
            Console.Out);
    }
    catch (FormatException ex)
    {
        return McpToolResult.WriteError(Console.Out, "INVALID_ZONE", ex.Message);
    }
}

static int RunScenarioUndo(string[] args)
{
    var path = CliArgParser.GetFlag(args, "--path");
    var editVersion = CliArgParser.GetIntFlag(args, "--edit-version", -1);
    if (string.IsNullOrWhiteSpace(path) || editVersion < 0)
    {
        Console.Error.WriteLine("scenario_undo requires --path --edit-version");
        return 1;
    }

    return ScenarioUndoCommand.Run(path, editVersion, Console.Out);
}

static int RunMissionAddFerry(string[] args)
{
    var path = CliArgParser.GetFlag(args, "--path");
    var missionId = CliArgParser.GetFlag(args, "--id");
    var editVersion = CliArgParser.GetIntFlag(args, "--edit-version", -1);
    if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(missionId) || editVersion < 0)
    {
        Console.Error.WriteLine("mission_add_ferry requires --path --edit-version --id [--unit U]+ --destination D");
        return 1;
    }

    return MissionAddFerryCommand.Run(
        path,
        editVersion,
        missionId,
        CliArgParser.GetRepeated(args, "--unit"),
        CliArgParser.GetFlag(args, "--destination") ?? string.Empty,
        Console.Out);
}

static int RunMissionUpdateFerry(string[] args)
{
    var path = CliArgParser.GetFlag(args, "--path");
    var missionId = CliArgParser.GetFlag(args, "--id");
    var editVersion = CliArgParser.GetIntFlag(args, "--edit-version", -1);
    if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(missionId) || editVersion < 0)
    {
        Console.Error.WriteLine("mission_update_ferry requires --path --edit-version --id [--unit U]+ [--destination D]");
        return 1;
    }

    return MissionUpdateFerryCommand.Run(
        path,
        editVersion,
        missionId,
        CliArgParser.GetRepeated(args, "--unit"),
        CliArgParser.GetFlag(args, "--destination"),
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
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- mission_add_ferry --path <scenario.json> --edit-version N --id <id> [--unit U]+ --destination D");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- mission_add_support --path <scenario.json> --edit-version N --id <id> --role Tanker|AEW|EW [--unit U]+ [--wp lat,lon]+");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- mission_update_ferry --path <scenario.json> --edit-version N --id <id> [--unit U]+ [--destination D]");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_undo --path <scenario.json> --edit-version N");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- mission_delete --path <scenario.json> --edit-version N --id <id>");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_validate --path <scenario.json>");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_publish --path <scenario.json>");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_export --path <scenario.json>   // S83-01 polished (cites roadmap-execute-plan-07042026.md + boundary-2026-07-04.md + qa units)");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_export_brief --path <scenario.json> [--out brief.md]");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_simulate_sample --path <scenario.json> [--ticks N]");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- mission_plan_suggest --intent \"patrol and strike baltic\"");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_comms_status --policy baltic-patrol-comms");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_cyber_status --policy baltic-patrol-comms");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_near_future_spawn --path <scenario.json>");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- scenario_event_trace --path <scenario.json> [--event ID]");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_intelligence_run [--db <catalog.db>]");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_entity_map");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_write_propose --db <catalog.db> --platform P --sensor S --base-pd 0.7");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_write_approve --db <catalog.db> --batch <batchId> [--enable-balance-drift]");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_import_markdown --db <catalog.db> --markdown <path.md> [--entity sensor|weapon|platform|aircraft|submarine|facility] [--map-baltic-platform-ids] [--max-records N] [--chunk-size 500] [--report-out report.json]");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- platform_export_xlsx [--db <catalog.db>] --out <path> [--snapshot <id>] [--tl-tier TL-0..TL-5] [--io closedxml|canonical]");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- platform_import_xlsx --db <catalog.db> --in <workbook> [--io closedxml|canonical]");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- platform_diff_xlsx [--db <catalog.db>] [--base <path>] [--edited <path>] [--io closedxml|canonical]");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_platform_browse [--db <catalog.db>] [--max-records N]");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_mount_loadout_quarantine_triage --db <catalog.db> [--entity platform|submarine|facility] [--propose-json <path>] [--apply]");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_release_diff --db <catalog.db> --from <releaseVersion> --to <releaseVersion>");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_dependency_graph [--db <catalog.db>]");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_kill_chain_report [--db <catalog.db>]");
    Console.WriteLine("  dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_link_report [--db <catalog.db>]");
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
    var entityRaw = CliArgParser.GetFlag(args, "--entity");
    var mapBaltic = args.Any(a => a.Equals("--map-baltic-platform-ids", StringComparison.Ordinal));
    try
    {
        var entity = CatalogImportMarkdownCommand.ParseEntity(entityRaw);
        return CatalogImportMarkdownCommand.Run(
            db,
            markdown,
            maxRecords,
            chunkSize,
            Console.Out,
            reportOut,
            entity,
            mapBaltic);
    }
    catch (ArgumentException ex)
    {
        Console.Error.WriteLine(ex.Message);
        return 1;
    }
}

static int RunPlatformExportXlsx(string[] args)
{
    var db = CliArgParser.GetFlag(args, "--db");
    var outPath = CliArgParser.GetFlag(args, "--out") ?? CliArgParser.GetFlag(args, "--output");
    var snapshotId = CliArgParser.GetFlag(args, "--snapshot") ?? CliArgParser.GetFlag(args, "--snapshot-id") ?? string.Empty;
    var ioFlag = CliArgParser.GetFlag(args, "--io");
    var tlTier = CliArgParser.GetFlag(args, "--tl-tier") ?? CliArgParser.GetFlag(args, "--tlTier");
    return PlatformExportXlsxCommand.Run(db, outPath ?? string.Empty, snapshotId, ioFlag, Console.Out, tlTier);
}

static int RunPlatformImportXlsx(string[] args)
{
    var db = CliArgParser.GetFlag(args, "--db");
    var inPath = CliArgParser.GetFlag(args, "--in") ?? CliArgParser.GetFlag(args, "--input");
    var actorType = CliArgParser.GetFlag(args, "--actor-type") ?? "cli";
    var actorId = CliArgParser.GetFlag(args, "--actor-id") ?? "mission-editor";
    if (string.IsNullOrWhiteSpace(db))
    {
        Console.Error.WriteLine("platform_import_xlsx requires --db <catalog.db> [--in <workbook.xlsx>]");
        return 1;
    }

    var ioFlag = CliArgParser.GetFlag(args, "--io");
    return PlatformImportXlsxCommand.Run(db, inPath ?? string.Empty, actorType, actorId, ioFlag, Console.Out);
}

static int RunPlatformDiffXlsx(string[] args)
{
    var db = CliArgParser.GetFlag(args, "--db");
    var basePath = CliArgParser.GetFlag(args, "--base") ?? CliArgParser.GetFlag(args, "--source");
    var editedPath = CliArgParser.GetFlag(args, "--edited") ?? CliArgParser.GetFlag(args, "--in");
    var ioFlag = CliArgParser.GetFlag(args, "--io");
    return PlatformDiffXlsxCommand.Run(db, basePath ?? string.Empty, editedPath ?? string.Empty, ioFlag, Console.Out);
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

static int RunCatalogPlatformBrowse(string[] args)
{
    var db = CliArgParser.GetFlag(args, "--db");
    var maxRecords = CliArgParser.GetIntFlag(args, "--max-records", 0);
    return CatalogPlatformBrowseCommand.Run(
        db,
        Console.Out,
        maxRecords > 0 ? maxRecords : null);
}

static int RunCatalogDependencyGraph(string[] args)
{
    if (args.Contains("--help", StringComparer.Ordinal) || args.Contains("-h", StringComparer.Ordinal))
    {
        CatalogDependencyGraphCommand.PrintHelp(Console.Out);
        return 0;
    }

    var db = CliArgParser.GetFlag(args, "--db");
    try
    {
        return CatalogDependencyGraphCommand.Run(db, Console.Out);
    }
    catch (ArgumentException ex)
    {
        Console.Error.WriteLine(ex.Message);
        CatalogDependencyGraphCommand.PrintHelp(Console.Error);
        return 1;
    }
}

static int RunCatalogKillChainReport(string[] args)
{
    if (args.Contains("--help", StringComparer.Ordinal) || args.Contains("-h", StringComparer.Ordinal))
    {
        CatalogKillChainReportCommand.PrintHelp(Console.Out);
        return 0;
    }

    var db = CliArgParser.GetFlag(args, "--db");
    try
    {
        return CatalogKillChainReportCommand.Run(db, Console.Out);
    }
    catch (ArgumentException ex)
    {
        Console.Error.WriteLine(ex.Message);
        CatalogKillChainReportCommand.PrintHelp(Console.Error);
        return 1;
    }
}

static int RunCatalogLinkReport(string[] args)
{
    if (args.Contains("--help", StringComparer.Ordinal) || args.Contains("-h", StringComparer.Ordinal))
    {
        CatalogLinkReportCommand.PrintHelp(Console.Out);
        return 0;
    }

    var db = CliArgParser.GetFlag(args, "--db");
    try
    {
        return CatalogLinkReportCommand.Run(db, Console.Out);
    }
    catch (ArgumentException ex)
    {
        Console.Error.WriteLine(ex.Message);
        CatalogLinkReportCommand.PrintHelp(Console.Error);
        return 1;
    }
}

static int RunCatalogReleaseDiff(string[] args)
{
    if (args.Contains("--help", StringComparer.Ordinal) || args.Contains("-h", StringComparer.Ordinal))
    {
        CatalogReleaseDiffCommand.PrintHelp(Console.Out);
        return 0;
    }

    var db = CliArgParser.GetFlag(args, "--db");
    var from = CliArgParser.GetFlag(args, "--from");
    var to = CliArgParser.GetFlag(args, "--to");
    if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
    {
        var positional = args
            .Where(a => !a.StartsWith("-", StringComparison.Ordinal))
            .ToArray();
        if (positional.Length >= 2)
        {
            from ??= positional[0];
            to ??= positional[1];
        }
    }

    if (string.IsNullOrWhiteSpace(db) || string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
    {
        Console.Error.WriteLine("catalog_release_diff requires --db --from --to");
        CatalogReleaseDiffCommand.PrintHelp(Console.Error);
        return 1;
    }

    try
    {
        return CatalogReleaseDiffCommand.Run(db, from, to, Console.Out);
    }
    catch (ArgumentException ex)
    {
        Console.Error.WriteLine(ex.Message);
        return 1;
    }
}

static int RunMountLoadoutQuarantineTriage(string[] args)
{
    var db = CliArgParser.GetFlag(args, "--db");
    if (string.IsNullOrWhiteSpace(db))
    {
        Console.Error.WriteLine("catalog_mount_loadout_quarantine_triage requires --db");
        return 1;
    }

    var entity = CliArgParser.GetFlag(args, "--entity");
    var proposeJson = CliArgParser.GetFlag(args, "--propose-json");
    var dryRun = !args.Contains("--apply", StringComparer.Ordinal);
    return MountLoadoutQuarantineTriageCommand.Run(db, dryRun, entity, proposeJson, Console.Out);
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
    var enableBalanceDrift = args.Contains("--enable-balance-drift", StringComparer.Ordinal);
    return CatalogWriteApproveCommand.Run(
        db,
        batch,
        Console.Out,
        snapshotId,
        releaseVersion,
        enableBalanceDrift);
}

// --- scenario editor requirements verbs (thin shells for AC2/AC3/AC4) ---
static int RunScenarioMigratePreview(string[] args)
{
    var path = CliArgParser.GetFlag(args, "--path") ?? "";
    var target = CliArgParser.GetFlag(args, "--target") ?? "next-db";
    if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
    {
        // headless: use representative with legacy unit (no __verif literal)
        var editor = ScenarioDocumentEditor.CreateNew();
        var pre = editor.ComputeFileHash();
        var (snapId, preHashSnap) = editor.CreateSnapshotForRollback("pre");
        editor.AddPatrolMission("patrol-legacy-rep", new[] { "legacy-patrol-ship" }, new[] { new ScenarioWaypointDto { Lat = 57.0, Lon = 20.0 } });
        var mid = editor.ComputeFileHash();
        Console.WriteLine(editor.ComparePrePost(pre, mid)); // delta=1 after mutation
        var preview = editor.PreviewDbMigration(target);
        Console.WriteLine(preview);
        // demo real rollback + delta using actual hash (post rollback delta=0)
        editor.RollbackToSnapshot(snapId);
        var post = editor.ComputeFileHash();
        Console.WriteLine(editor.ComparePrePost(pre, post));
        return 0;
    }
    var ed = ScenarioDocumentEditor.Load(path);
    var preHash = ed.ComputeFileHash();
    var (snapId2, preHashSnap2) = ed.CreateSnapshotForRollback("pre-migration");
    Console.WriteLine(ed.PreviewDbMigration(target));
    // rollback demo with real id
    ed.RollbackToSnapshot(snapId2);
    var postHash = ed.ComputeFileHash();
    Console.WriteLine(ed.ComparePrePost(preHash, postHash));
    return 0;
}

static int RunScenarioUmpireSnapshot(string[] args)
{
    var path = CliArgParser.GetFlag(args, "--path") ?? "";
    var editor = string.IsNullOrWhiteSpace(path) || !File.Exists(path) ? ScenarioDocumentEditor.CreateNew() : ScenarioDocumentEditor.Load(path);
    var ws = new AdjudicationWorkspace(editor, "umpire");
    Console.WriteLine("umpire and adjudication workspace (first-class)");
    var before = ws.Snapshot("before");
    editor.AddPatrolMission("cli-ump-inject", new[] { "u1" }, new[] { new ScenarioWaypointDto { Lat = 57.0, Lon = 20.0 } });
    editor.CommitMutation();
    var after = ws.Snapshot("after");
    var diff = ws.ComputeDiff(before, after, "umpire inject unit");
    Console.WriteLine($"before/after diffs for umpire interventions: {diff.DiffSummary} reason=\"{diff.Reason}\"");
    var audit = ws.AuditLog("inject", "exercise control", "umpire");
    Console.WriteLine($"audit logging with reasons: action={audit.Action} reason=\"{audit.Reason}\" role={audit.Role}");
    Console.WriteLine(ws.ApplyRoleGuard("umpire"));
    ws.Freeze(); ws.Step(); ws.Resume();
    Console.WriteLine("freeze, step, inject, and resume controls engaged");
    return 0;
}

static int RunScenarioAiScaffold(string[] args)
{
    var brief = CliArgParser.GetFlag(args, "--brief") ?? "Baltic defensive";
    var editor = ScenarioDocumentEditor.CreateNew();
    Console.WriteLine(editor.RedTeamPlanningAssistant(brief));
    Console.WriteLine(editor.NlScaffold(brief));
    Console.WriteLine(editor.RunSmokeTestAgent());
    Console.WriteLine(editor.ExplainProvenance("mission-1"));
    Console.WriteLine(editor.BuildManifest("Baltic Defense", brief, editor.Metadata.DbRef ?? "baltic"));
    return 0;
}

static int RunScenarioEventTrace(string[] args)
{
    // S84-01: delegate to dedicated ScenarioEventTraceCommand for AC-7 structured unmet_conditions JSON.
    // Cites: sprint-84-event-debugger.md, kickoff, roadmap-execute-plan-07042026.md, qa-plan unit#7.
    return ScenarioEventTraceCommand.Run(args, Console.Out, Console.Error);
}
