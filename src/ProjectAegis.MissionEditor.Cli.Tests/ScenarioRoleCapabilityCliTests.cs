namespace ProjectAegis.MissionEditor.Cli.Tests;

using System.Text.Json;
using Cli;
using ProjectAegis.Data.Scenario.Authoring;
using Xunit;

/// <summary>
/// DRG-345 / proposed AME-6.11: <c>scenario_validate</c>, <c>scenario_export</c> and
/// <c>scenario_simulate_sample</c> report the same capability findings as the GUI projection
/// (both come from <see cref="MissionRoleCapabilityManifest"/>).
/// </summary>
public sealed class ScenarioRoleCapabilityCliTests
{
    [Fact]
    public void scenario_validate_reports_manifest_capability_findings_and_exits_1()
    {
        var path = ScenarioValidationFixturePaths.Require("role-capability-unsupported.json");
        var expected = MissionRoleCapabilityManifest.EvaluateFindings(ScenarioDocumentJsonLoader.LoadFromFile(path))
            .Select(f => $"{f.Severity}|{f.Code}|{f.MissionId}|{f.Message}")
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        using var writer = new StringWriter();
        Assert.Equal(1, ScenarioValidateCommand.Run(path, quiet: false, writer));

        using var json = JsonDocument.Parse(writer.ToString());
        Assert.False(json.RootElement.GetProperty("canExport").GetBoolean());
        var actual = json.RootElement.GetProperty("findings").EnumerateArray()
            .Where(f => MissionRoleCapabilityManifest.IsCapabilityCode(f.GetProperty("code").GetString()!))
            .Select(f => $"{f.GetProperty("severity").GetString()}|{f.GetProperty("code").GetString()}|{f.GetProperty("missionId").GetString()}|{f.GetProperty("message").GetString()}")
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(expected, actual);
        Assert.Equal(5, actual.Length);
    }

    [Fact]
    public void scenario_validate_supported_fixture_has_no_capability_findings()
    {
        var path = ScenarioValidationFixturePaths.Require("role-capability-supported.json");

        using var writer = new StringWriter();
        Assert.Equal(0, ScenarioValidateCommand.Run(path, quiet: false, writer));
        Assert.DoesNotContain("MISSION_ROLE_", writer.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void scenario_simulate_sample_rejects_unsupported_role_before_running()
    {
        var path = ScenarioValidationFixturePaths.Require("role-capability-unsupported.json");

        using var writer = new StringWriter();
        Assert.Equal(1, ScenarioSimulateSampleCommand.Run(path, ticks: 4, quiet: false, writer));
        var output = writer.ToString();
        Assert.Contains(MissionRoleCapabilityManifest.CodeRoleNotExecuted, output, StringComparison.Ordinal);
        Assert.DoesNotContain("sample-complete", output, StringComparison.Ordinal);
    }
}
