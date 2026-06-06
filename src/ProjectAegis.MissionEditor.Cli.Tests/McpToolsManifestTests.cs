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
        "mission_delete",
        "mission_plan_suggest",
        "scenario_comms_status",
        "scenario_cyber_status",
        "scenario_near_future_spawn",
        "scenario_validate",
        "scenario_export_brief",
        "scenario_simulate_sample",
        "osint_search",
        "osint_digest",
        "osint_list_staging_proposals",
        "osint_get_proposal_detail",
        "osint_submit_review_decision",
    ];

    [Fact]
    public void mcp_tools_json_lists_all_headless_verbs()
    {
        var path = ResolveManifestPath();
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var names = doc.RootElement
            .GetProperty("tools")
            .EnumerateArray()
            .Select(t => t.GetProperty("name").GetString())
            .Where(n => n != null)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var verb in RequiredCliVerbs)
        {
            Assert.Contains(verb, names);
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