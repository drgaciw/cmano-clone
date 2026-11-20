namespace ProjectAegis.MissionEditor.Cli.Tests;

using System.Text.Json;
using Cli;
using Data.Scenario.Authoring;
using NUnit.Framework;

/// <summary>
/// DRG-345 / proposed AME-6.11: <c>scenario_validate</c>, <c>scenario_export</c> and
/// <c>scenario_simulate_sample</c> report the same capability findings as the GUI projection
/// (both come from <see cref="MissionRoleCapabilityManifest"/>).
/// </summary>
[TestFixture]
public sealed class ScenarioRoleCapabilityCliTests
{
    [Test]
    public void scenario_validate_reports_manifest_capability_findings_and_exits_1()
    {
        var path = ScenarioValidationFixturePaths.Require("role-capability-unsupported.json");
        var expected = MissionRoleCapabilityManifest.EvaluateFindings(ScenarioDocumentJsonLoader.LoadFromFile(path))
            .Select(f => $"{f.Severity}|{f.Code}|{f.MissionId}|{f.Message}")
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        using var writer = new StringWriter();
        Assert.That(ScenarioValidateCommand.Run(path, quiet: false, writer), Is.EqualTo(1));

        using var json = JsonDocument.Parse(writer.ToString());
        Assert.That(json.RootElement.GetProperty("canExport").GetBoolean(), Is.False);
        var actual = json.RootElement.GetProperty("findings").EnumerateArray()
            .Where(f => MissionRoleCapabilityManifest.IsCapabilityCode(f.GetProperty("code").GetString()!))
            .Select(f => $"{f.GetProperty("severity").GetString()}|{f.GetProperty("code").GetString()}|{f.GetProperty("missionId").GetString()}|{f.GetProperty("message").GetString()}")
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.That(actual, Is.EqualTo(expected));
        Assert.That(actual.Length, Is.EqualTo(5));
    }

    [Test]
    public void scenario_validate_supported_fixture_has_no_capability_findings()
    {
        var path = ScenarioValidationFixturePaths.Require("role-capability-supported.json");

        using var writer = new StringWriter();
        Assert.That(ScenarioValidateCommand.Run(path, quiet: false, writer), Is.EqualTo(0));
        Assert.That(writer.ToString(), Does.Not.Contain("MISSION_ROLE_"));
    }

    [Test]
    public void scenario_simulate_sample_rejects_unsupported_role_before_running()
    {
        var path = ScenarioValidationFixturePaths.Require("role-capability-unsupported.json");

        using var writer = new StringWriter();
        Assert.That(ScenarioSimulateSampleCommand.Run(path, ticks: 4, quiet: false, writer), Is.EqualTo(1));
        var output = writer.ToString();
        Assert.That(output, Does.Contain(MissionRoleCapabilityManifest.CodeRoleNotExecuted));
        Assert.That(output, Does.Not.Contain("sample-complete"));
    }
}
