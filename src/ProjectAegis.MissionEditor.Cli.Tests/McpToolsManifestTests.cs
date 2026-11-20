using System.Text.Json;
using NUnit.Framework;

namespace ProjectAegis.MissionEditor.Cli.Tests;

[TestFixture]
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
        "orbat_upsert_unit",
        "orbat_move_unit",
        "orbat_clone_unit",
        "reference_point_upsert",
        "scenario_export",
        "scenario_migrate_preview",
        "scenario_umpire_snapshot",
        "scenario_undo",
        "mission_delete",
        "event_add",
        "event_update",
        "event_delete",
        "side_list",
        "side_upsert",
        "side_delete",
        "timeline_list",
        "timeline_upsert",
        "timeline_delete",
        "mission_list",
        "mission_clone",
        "mission_add_from_template",
        "mission_plan_suggest",
        "scenario_comms_status",
        "scenario_cyber_status",
        "scenario_near_future_spawn",
        "scenario_validate",
        "scenario_diff_summary",
        "scenario_export_brief",
        "scenario_simulate_sample",
        "scenario_event_trace",  // S86-01: added for Program.cs parity (active headless verb via ScenarioDocumentEditor.ExplainEventTrace)
        "scenario_publish",
        "scenario_ai_scaffold",
        "aar_explain",
        "osint_search",
        "osint_digest",
        "osint_list_staging_proposals",
        "osint_get_proposal_detail",
        "osint_submit_review_decision",
        "platform_export_xlsx",
        "platform_import_xlsx",
        "platform_diff_xlsx",
        "platform_design_propose",
    ];

    [Test]
    public void mcp_tools_json_lists_all_headless_verbs()
    {
        var path = ResolveManifestPath();
        using var doc = JsonDocument.Parse(File.ReadAllText(path));

        // S86-01 polish: assert manifest schema and structure
        Assert.That(doc.RootElement.GetProperty("schemaVersion").GetInt32(), Is.EqualTo(2));
        Assert.That(doc.RootElement.GetProperty("description").GetString(), Is.EqualTo("Headless mission-editor MCP tool bindings (Unity-MCP host registers these commands)."));

        var tools = doc.RootElement.GetProperty("tools").EnumerateArray().ToList();
        var names = tools
            .Select(t => t.GetProperty("name").GetString())
            .Where(n => n != null)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var verb in RequiredCliVerbs)
        {
            Assert.That(names, Does.Contain(verb));
        }

        // Every listed tool must have inputSchema (verb polish hygiene)
        foreach (var tool in tools)
        {
            var name = tool.GetProperty("name").GetString();
            if (RequiredCliVerbs.Contains(name))
            {
                Assert.That(tool.TryGetProperty("inputSchema", out _), Is.True, $"MCP tool '{name}' missing inputSchema in manifest.");
            }
        }

        // DRG-747: mission_plan_suggest exposes opt-in provider fake args in the schema.
        var suggest = tools.Single(t => t.GetProperty("name").GetString() == "mission_plan_suggest");
        var props = suggest.GetProperty("inputSchema").GetProperty("properties");
        Assert.That(props.TryGetProperty("provider", out var providerProp), Is.True);
        Assert.That(providerProp.GetProperty("enum")[0].GetString(), Is.EqualTo("fake"));
        Assert.That(props.TryGetProperty("fixture", out _), Is.True);
        Assert.That(props.TryGetProperty("script", out _), Is.True);
        Assert.That(props.TryGetProperty("expectedEditVersion", out _), Is.True);
        Assert.That(props.TryGetProperty("currentEditVersion", out _), Is.True);
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