namespace ProjectAegis.MissionEditor.Cli.Tests;

using System.Text.Json;
using Cli;
using NUnit.Framework;

/// <summary>
/// ME-W3 Track W3-c: CLI contract for <c>scenario_diff_summary</c> → <c>{ ok, summary }</c>.
/// </summary>
[TestFixture]
public sealed class ScenarioDiffSummaryCliTests
{
    [Test]
    public void scenario_diff_summary_identical_files_returns_no_semantic_changes()
    {
        var path = WriteTempScenario("""
            {
              "metadata": { "tlBranch": "TL-0", "dbRef": "baltic_patrol", "seed": 1, "editVersion": 1 },
              "missions": [ { "id": "patrol-1", "type": "Patrol", "assignedUnitIds": ["u1"] } ],
              "sides": [ { "id": "blue", "name": "Blue" } ],
              "orbat": { "units": [ { "id": "u1", "sideId": "blue", "platformId": "p1", "lat": 57.0, "lon": 20.0 } ] }
            }
            """);
        try
        {
            using var writer = new StringWriter();
            var exit = ScenarioDiffSummaryCommand.Run(path, path, writer);
            Assert.That(exit, Is.EqualTo(0));

            using var doc = JsonDocument.Parse(writer.ToString());
            Assert.That(doc.RootElement.GetProperty("ok").GetBoolean(), Is.True);
            Assert.That(doc.RootElement.GetProperty("summary").GetString(), Is.EqualTo("no semantic changes"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void scenario_diff_summary_reports_mission_unit_and_event_deltas()
    {
        var before = WriteTempScenario("""
            {
              "metadata": { "tlBranch": "TL-0", "dbRef": "baltic_patrol", "seed": 1, "editVersion": 1 },
              "missions": [ { "id": "m1", "type": "Patrol" } ],
              "sides": [ { "id": "blue", "name": "Blue" } ],
              "orbat": { "units": [ { "id": "u1", "sideId": "blue", "platformId": "p1", "lat": 1, "lon": 2 } ] },
              "operationsTimeline": [ { "missionId": "m1", "activateAtTick": 10 } ],
              "events": [ { "id": "e1", "triggerType": "Time" } ]
            }
            """);
        var after = WriteTempScenario("""
            {
              "metadata": { "tlBranch": "TL-0", "dbRef": "baltic_patrol", "seed": 1, "editVersion": 2 },
              "missions": [ { "id": "m1", "type": "Strike" }, { "id": "m2", "type": "Ferry" } ],
              "sides": [ { "id": "blue", "name": "Blue" }, { "id": "red", "name": "Red" } ],
              "orbat": { "units": [
                { "id": "u1", "sideId": "blue", "platformId": "p1", "lat": 1, "lon": 2 },
                { "id": "u2", "sideId": "red", "platformId": "p2", "lat": 3, "lon": 4 }
              ] },
              "operationsTimeline": [ { "missionId": "m1", "activateAtTick": 20 } ],
              "events": [ { "id": "e1", "triggerType": "Time" }, { "id": "e2", "triggerType": "UnitDestroyed" } ]
            }
            """);
        try
        {
            using var writer = new StringWriter();
            var exit = ScenarioDiffSummaryCommand.Run(before, after, writer);
            Assert.That(exit, Is.EqualTo(0));

            using var doc = JsonDocument.Parse(writer.ToString());
            Assert.That(doc.RootElement.GetProperty("ok").GetBoolean(), Is.True);
            var summary = doc.RootElement.GetProperty("summary").GetString() ?? "";

            Assert.That(summary, Does.Contain("mission +m2 (Ferry)"));
            Assert.That(summary, Does.Contain("mission ~m1 type Patrol→Strike"));
            Assert.That(summary, Does.Contain("side +red"));
            Assert.That(summary, Does.Contain("unit +u2"));
            Assert.That(summary, Does.Contain("event +e2"));
            Assert.That(summary, Does.Contain("timeline ~m1 tick 10→20"));
        }
        finally
        {
            File.Delete(before);
            File.Delete(after);
        }
    }

    [Test]
    public void scenario_diff_summary_missing_before_returns_error()
    {
        var after = WriteTempScenario("""{ "missions": [] }""");
        try
        {
            var missing = Path.Combine(Path.GetTempPath(), $"aegis-missing-{Guid.NewGuid():N}.json");
            using var writer = new StringWriter();
            var exit = ScenarioDiffSummaryCommand.Run(missing, after, writer);
            Assert.That(exit, Is.EqualTo(1));
            Assert.That(writer.ToString(), Does.Contain("NOT_FOUND").IgnoreCase);
            Assert.That(writer.ToString().Replace(" ", ""), Does.Contain("\"ok\":false").IgnoreCase);
        }
        finally
        {
            File.Delete(after);
        }
    }

    [Test]
    public void scenario_diff_summary_missing_after_returns_error()
    {
        var before = WriteTempScenario("""{ "missions": [] }""");
        try
        {
            var missing = Path.Combine(Path.GetTempPath(), $"aegis-missing-{Guid.NewGuid():N}.json");
            using var writer = new StringWriter();
            var exit = ScenarioDiffSummaryCommand.Run(before, missing, writer);
            Assert.That(exit, Is.EqualTo(1));
            Assert.That(writer.ToString(), Does.Contain("NOT_FOUND").IgnoreCase);
        }
        finally
        {
            File.Delete(before);
        }
    }

    /// <summary>
    /// UAT 2026-07-19: command class was wired but Program.cs omitted the verb (exit 1 usage dump).
    /// Spawns the real CLI DLL so dispatch stays covered.
    /// </summary>
    [Test]
    public void scenario_diff_summary_program_dispatch_returns_ok_json()
    {
        var path = WriteTempScenario("""
            {
              "metadata": { "tlBranch": "TL-0", "dbRef": "baltic_patrol", "seed": 1, "editVersion": 1 },
              "missions": [ { "id": "patrol-1", "type": "Patrol", "assignedUnitIds": ["u1"] } ],
              "sides": [ { "id": "blue", "name": "Blue" } ],
              "orbat": { "units": [ { "id": "u1", "sideId": "blue", "platformId": "p1", "lat": 57.0, "lon": 20.0 } ] }
            }
            """);
        try
        {
            var (exit, stdout, stderr) = RunCli("scenario_diff_summary", "--before", path, "--after", path);
            Assert.That(string.IsNullOrEmpty(stderr) || !stderr.Contains("Unknown command", StringComparison.Ordinal), Is.True, $"Unexpected stderr: {stderr}");
            Assert.That(exit, Is.EqualTo(0));
            Assert.That(stdout, Does.Not.Contain("Unknown command").IgnoreCase);
            using var doc = JsonDocument.Parse(stdout);
            Assert.That(doc.RootElement.GetProperty("ok").GetBoolean(), Is.True);
            Assert.That(doc.RootElement.GetProperty("summary").GetString(), Is.EqualTo("no semantic changes"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static (int ExitCode, string Stdout, string Stderr) RunCli(params string[] args)
    {
        var cliDllPath = Path.Combine(AppContext.BaseDirectory, "ProjectAegis.MissionEditor.Cli.dll");
        Assert.That(File.Exists(cliDllPath), Is.True, $"Expected built CLI at {cliDllPath}");

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        psi.ArgumentList.Add(cliDllPath);
        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        using var proc = System.Diagnostics.Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start dotnet");
        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit(60_000);
        return (proc.ExitCode, stdout, stderr);
    }

    private static string WriteTempScenario(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-diff-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json);
        return path;
    }
}
