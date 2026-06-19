namespace ProjectAegis.MissionEditor.Cli.Tests;

using ProjectAegis.MissionEditor.Cli;
using Xunit;

public sealed class ScenarioValidateCliTests
{
    [Fact]
    public void scenario_validate_clean_fixture_returns_exit_0()
    {
        var path = ResolveFixture("golden_clean.json");
        if (path == null)
        {
            return;
        }

        using var writer = new StringWriter();
        Assert.Equal(0, ScenarioValidateCommand.Run(path, quiet: false, writer));
        Assert.Contains("passed", writer.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void scenario_validate_missing_tlBranch_returns_exit_1_with_finding()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-tl-missing-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(
                path,
                """
                {
                  "metadata": { "dbRef": "baltic_patrol" },
                  "missions": [
                    {
                      "id": "patrol-1",
                      "type": "Patrol",
                      "assignedUnitIds": ["u1"],
                      "patrolZone": [
                        { "lat": 57.0, "lon": 20.0 },
                        { "lat": 57.1, "lon": 20.1 },
                        { "lat": 57.2, "lon": 20.2 }
                      ]
                    }
                  ]
                }
                """);

            using var writer = new StringWriter();
            Assert.Equal(1, ScenarioValidateCommand.Run(path, quiet: false, writer));
            Assert.Contains("TL_BRANCH_MISSING", writer.ToString());
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void scenario_validate_missing_release_train_returns_exit_1_with_finding()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-tl-not-found-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(
                path,
                """
                {
                  "metadata": { "tlBranch": "TL-2" },
                  "missions": [
                    {
                      "id": "patrol-1",
                      "type": "Patrol",
                      "assignedUnitIds": ["u1"],
                      "patrolZone": [
                        { "lat": 57.0, "lon": 20.0 },
                        { "lat": 57.1, "lon": 20.1 },
                        { "lat": 57.2, "lon": 20.2 }
                      ]
                    }
                  ]
                }
                """);

            using var writer = new StringWriter();
            Assert.Equal(1, ScenarioValidateCommand.Run(path, quiet: false, writer));
            Assert.Contains("TL_RELEASE_TRAIN_NOT_FOUND", writer.ToString());
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void scenario_validate_unknown_manifest_dbRef_returns_exit_1_with_DB_MISMATCH()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-unified-db-mismatch-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(
                path,
                """
                {
                  "metadata": {
                    "dbRef": "unified-corpus-TL-0-not-published",
                    "tlBranch": "TL-0"
                  },
                  "missions": [
                    {
                      "id": "patrol-1",
                      "type": "Patrol",
                      "assignedUnitIds": ["u1"],
                      "patrolZone": [
                        { "lat": 57.0, "lon": 20.0 },
                        { "lat": 57.1, "lon": 20.1 },
                        { "lat": 57.2, "lon": 20.2 }
                      ]
                    }
                  ]
                }
                """);

            using var writer = new StringWriter();
            Assert.Equal(1, ScenarioValidateCommand.Run(path, quiet: false, writer));
            Assert.Contains("DB_MISMATCH", writer.ToString());
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void scenario_validate_unreachable_fixture_returns_exit_1()
    {
        var path = ResolveFixture("golden_strike_unreachable.json");
        if (path == null)
        {
            return;
        }

        using var writer = new StringWriter();
        Assert.Equal(1, ScenarioValidateCommand.Run(path, quiet: false, writer));
        Assert.Contains("STRIKE_UNREACHABLE", writer.ToString());
    }

    private static string? ResolveFixture(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 10; i++)
        {
            if (dir == null)
            {
                break;
            }

            var candidate = Path.Combine(dir.FullName, "assets", "data", "scenarios", "validation", fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return null;
    }
}