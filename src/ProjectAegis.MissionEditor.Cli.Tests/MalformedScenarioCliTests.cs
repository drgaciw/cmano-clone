namespace ProjectAegis.MissionEditor.Cli.Tests;

using System.Text.Json;
using Cli;
using NUnit.Framework;

/// <summary>
/// UAT CLI-1: malformed scenario JSON must yield the standard error envelope and exit 1,
/// not an unhandled exception (process exit 134).
/// </summary>
[TestFixture]
public sealed class MalformedScenarioCliTests
{
    private string _path = "";

    [SetUp]
    public void SetUp()
    {
        _path = Path.Combine(Path.GetTempPath(), $"aegis-bad-{Guid.NewGuid():N}.json");
        File.WriteAllText(_path, "{bad json");
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_path))
        {
            File.Delete(_path);
        }
    }

    [Test]
    public void scenario_validate_malformed_json_returns_exit_1_error_envelope()
    {
        using var writer = new StringWriter();
        Assert.That(ScenarioValidateCommand.Run(_path, quiet: false, writer), Is.EqualTo(1));
        AssertEnvelope(writer.ToString());
    }

    [Test]
    public void scenario_simulate_sample_malformed_json_returns_exit_1_error_envelope()
    {
        using var writer = new StringWriter();
        Assert.That(ScenarioSimulateSampleCommand.Run(_path, ticks: 4, quiet: false, writer), Is.EqualTo(1));
        AssertEnvelope(writer.ToString());
    }

    private static void AssertEnvelope(string text)
    {
        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement;
        Assert.That(root.GetProperty("ok").GetBoolean(), Is.False);
        Assert.That(root.GetProperty("code").GetString(), Is.EqualTo("LOAD_FAILED"));
        Assert.That(root.GetProperty("message").GetString(), Is.Not.Empty);
    }
}
