using System.Text.Json;
using Xunit;

namespace ProjectAegis.MissionEditor.Cli.Tests;

public sealed class McpToolsManifestTests
{
    private static readonly string[] RequiredCliVerbs =
    [
        "scenario_create",
        "mission_add_patrol",
        "mission_add_strike",
        "mission_update_patrol",
        "mission_update_strike",
        "mission_add_ferry",
        "mission_add_support",
        "mission_update_ferry",
        "mission_update_support",
        "scenario_export",
        "scenario_migrate_preview",
        "scenario_umpire_snapshot",
        "scenario_undo",
        "mission_delete",
        "mission_plan_suggest",
        "scenario_comms_status",
        "scenario_cyber_status",
        "scenario_near_future_spawn",
        "scenario_validate",
        "scenario_export_brief",
        "scenario_simulate_sample",
        "scenario_event_trace",  // S86-01: added for Program.cs parity (active headless verb via ScenarioDocumentEditor.ExplainEventTrace)
        "scenario_publish",
        "scenario_ai_scaffold",
        "osint_search",
        "osint_digest",
        "osint_list_staging_proposals",
        "osint_get_proposal_detail",
        "osint_submit_review_decision",
        "platform_export_xlsx",
        "platform_import_xlsx",
        "platform_diff_xlsx",
    ];

    [Fact]
    public void mcp_tools_json_lists_all_headless_verbs()
    {
        var path = ResolveManifestPath();
        using var doc = JsonDocument.Parse(File.ReadAllText(path));

        // S86-01 polish: assert manifest schema and structure
        Assert.Equal(2, doc.RootElement.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("Headless mission-editor MCP tool bindings (Unity-MCP host registers these commands).", doc.RootElement.GetProperty("description").GetString());

        var tools = doc.RootElement.GetProperty("tools").EnumerateArray().ToList();
        var names = tools
            .Select(t => t.GetProperty("name").GetString())
            .Where(n => n != null)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var verb in RequiredCliVerbs)
        {
            Assert.Contains(verb, names);
        }

        // Every listed tool must have inputSchema (verb polish hygiene)
        foreach (var tool in tools)
        {
            var name = tool.GetProperty("name").GetString();
            if (RequiredCliVerbs.Contains(name))
            {
                Assert.True(tool.TryGetProperty("inputSchema", out _), $"MCP tool '{name}' missing inputSchema in manifest.");
            }
        }
    }

    private static string ResolveManifestPath()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            var candidate = Path.Combine(dir, "tools", "mission-editor", "mcp-tools.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = Path.GetDirectoryName(dir) ?? dir;
        }

        dir = Directory.GetCurrentDirectory();
        for (var i = 0; i < 8; i++)
        {
            var candidate = Path.Combine(dir, "tools", "mission-editor", "mcp-tools.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = Path.GetDirectoryName(dir) ?? dir;
        }

        throw new FileNotFoundException("mcp-tools.json");
    }
}